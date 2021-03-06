﻿using System.Collections.Generic;
using System.Linq;

namespace EEScript
{
    using Enums;

    public class Page
    {
        private List<List<Trigger>> TriggerBlocks { get; set; }
        private Dictionary<Trigger, TriggerHandler> Handlers { get; set; }

        internal EEScriptEngine Engine { get; set; }

        /// <summary>
        /// A list of variables set globally accessible to any <see cref="Trigger"/>.
        /// </summary>
        public List<Variable> Variables { get; set; }

        /// <summary>
        /// The default area if there are none specified during an <see cref="TriggerCategory.Effect"/>.
        /// </summary>
        public Area DefaultArea { get; set; } = new Area();

        /// <returns> If true, continue to the next <see cref="Trigger"/>, otherwise stop execution of the current block. </returns>
        public delegate bool TriggerHandler(Trigger trigger, object player, object args);

        public delegate object PrivateVariableHandler(Trigger trigger, string key);
        public PrivateVariableHandler VariableHandler { get; set; }

        public Page(EEScriptEngine engine)
        {
            this.Engine = engine;

            this.TriggerBlocks = new List<List<Trigger>>();
            this.Handlers = new Dictionary<Trigger, TriggerHandler>();
            this.Variables = new List<Variable>();
        }

        /// <summary>
        /// Set the specified global variable, overriding any already existant.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetGlobalVariable(string key, object value)
        {
            if (this.Variables.Any(x => x.Type == VariableType.Global && x.Key == key)) {
                this.Variables.Find(x => x.Type == VariableType.Global && x.Key == key).Value = value;
            } else {
                this.Variables.Add(new Variable(VariableType.Global, key, value));
            }
        }

        /// <summary>
        /// Assigns the specified TriggerHandler to <paramref name="trigger"/>.
        /// </summary>
        /// <remarks> By default, a non-set <see cref="TriggerCategory.Cause"/> trigger returns true. </remarks>
        /// <param name="trigger"><see cref="Trigger"/></param>
        /// <param name="handler"><see cref="TriggerHandler"/></para
        public void SetTriggerHandler(Trigger trigger, TriggerHandler handler, string description = null)
        {
            if (this.Handlers.ContainsKey(trigger)) {
                if (this.Engine.Options.CanOverrideTriggerHandlers)
                    this.Handlers[trigger] = handler;
                else throw new EEScriptException($"A trigger handler for ({trigger.Category}:{trigger.Id} already exists.");
            }

            this.Handlers.Add(trigger, handler);
        }

        public void RemoveTriggerHandler(TriggerCategory category, int triggerId)
        {
            var triggers = from trigger in this.Handlers
                           where trigger.Key.Category == category
                           where trigger.Key.Id == triggerId
                           select trigger.Key;

            foreach (var trigger in triggers)
                this.Handlers.Remove(trigger);
        }

        internal Page InsertBlocks(List<List<Trigger>> triggerBlocks)
        {
            this.TriggerBlocks.AddRange(triggerBlocks);

            return this;
        }

        private void ExecuteBlock<T>(T triggerBlock, object triggeringEntity, object additionalArgs) where T : IList<Trigger>
        {
            var causeTrigger = triggerBlock[0];

            // set the current page for variable handling
            causeTrigger.Page = this;

            // if there is a handler set for the cause category, invoke before proceeding
            // cause triggers, if not handled, return true by default.
            if (this.Handlers.ContainsKey(causeTrigger)) {
                if (!this.Handlers[causeTrigger](causeTrigger, triggeringEntity, additionalArgs))
                    return;
            }

            foreach (var t in triggerBlock)
                t.TriggeringEntity = triggeringEntity;

            // check if there is any additional conditions to evaluate
            for (var i = 1; i < triggerBlock.Count; i++) {
                var trigger = triggerBlock[i];

                // set the current page for variable handling
                trigger.Page = this;

                switch (trigger.Category) {
                    case TriggerCategory.Cause:
                        throw new EEScriptException("You cannot have sibling causes.");
                    case TriggerCategory.Condition:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new EEScriptException("You do not have a handler for this trigger.", trigger);
                        if (!this.Handlers[trigger](trigger, triggeringEntity, additionalArgs))
                            return;

                        triggerBlock.Last().Conditions.Add(trigger);
                        break;
                    case TriggerCategory.Area:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new EEScriptException("You do not have a handler for this trigger.", trigger);

                        if (!this.Handlers[trigger](trigger, triggeringEntity, additionalArgs))
                            return;

                        triggerBlock.Last().Areas.Add(trigger);
                        break;
                    case TriggerCategory.Filter:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new EEScriptException("You do not have a handler for this trigger.", trigger);

                        trigger.Areas = triggerBlock.LastOrDefault().Areas;
                        if (!this.Handlers[trigger](trigger, triggeringEntity, additionalArgs))
                            return;

                        triggerBlock.Last().Filters.Add(trigger);
                        break;
                    case TriggerCategory.Effect:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new EEScriptException("You do not have a handler for this trigger.", trigger);

                        trigger.TriggeringEntity = triggeringEntity;
                        trigger.Area = triggerBlock.LastOrDefault(x => x.Category == TriggerCategory.Area)?.Area ?? DefaultArea;

                        // every condition has been met, execute the effect
                        this.Handlers[trigger](trigger, triggeringEntity, additionalArgs);
                        break;
                }
            }
        }

        /// <summary>
        /// Executes trigger blocks containing <see cref="TriggerCategory.Cause"/> with the specified <see cref="Trigger.Id"/>(s)
        /// </summary>
        /// <param name="triggeringEntity"> An object representing an entity which caused the trigger, optional. </param>
        /// <param name="additionalArgs"> An object representing any additional information to carry to the next trigger. </param>
        /// <param name="triggerIds"> The specified <see cref="TriggerCategory.Cause"/>(s) to execute. </param>
        public void Execute(object triggeringEntity = null, object additionalArgs = null, params int[] triggerIds)
        {
            foreach (var triggerId in triggerIds)
                foreach (var triggerBlock in this.TriggerBlocks)
                    if (triggerBlock[0].Id == triggerId)
                        ExecuteBlock(triggerBlock, triggeringEntity, additionalArgs);
        }
    }
}