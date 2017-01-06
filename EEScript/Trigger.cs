using System;
using System.Linq;
using System.Collections.Generic;

namespace EEScript
{
    using Enums;

    public class Trigger : IEquatable<Trigger>
    {
        internal Page Page { get; set; }

        public TriggerCategory Category { get; set; } = TriggerCategory.Undefined;
        public Queue<object> Contents { get; set; } = new Queue<object>();
        public Area Area { get; set; } = new Area();

        public int Id { get; set; } = -1;

        /// <summary>
        /// An optional parameter to specify any particular entity which had triggered the <see cref="TriggerCategory.Cause"/>.
        /// </summary>
        public object TriggeringEntity { get; set; }

        /// <summary>
        /// A list of successful <see cref="TriggerCategory.Condition"/>(s) evaluated prior.
        /// <para> Note: This property is only set on the last node, typically being the <see cref="TriggerCategory.Effect"/>. </para>
        /// </summary>
        public List<Trigger> Conditions { get; set; } = new List<Trigger>();

        /// <summary>
        /// A list of successful <see cref="TriggerCategory.Area"/>(s) evaluated prior.
        /// <para> Note: This property is only set on the last node, typically being the <see cref="TriggerCategory.Effect"/>. </para>
        /// </summary>
        public List<Trigger> Areas { get; set; } = new List<Trigger>();

        public Trigger(TriggerCategory category, int triggerId)
        {
            this.Category = category;
            this.Id = triggerId;
        }

        internal T Get<T>(int index)
        {
            var content = this.Contents.ToArray()[index];

            if (content is Variable) {
                var variable = (Variable)content;

                switch (variable.Type) {
                    case VariableType.Global:
                        content = this.Page.Variables.FirstOrDefault(x => x.Type == VariableType.Global && x.Key == variable.Key).Value;
                        break;
                    case VariableType.Private:
                        content = this.Page.VariableHandler(this, variable.Key);
                        break;
                }
            }

            return (T)Convert.ChangeType(content, typeof(T));
        }

        public string GetVariableName(int index)
        {
            var content = this.Contents.ToArray()[index];

            if (content is Variable) {
                return ((Variable)content).Key;
            }

            throw new Exception($"Index {index} in ({this.Category}:{this.Id}) is not a Variable.");
        }

        public bool Equals(Trigger other)
        {
            return other.Category == this.Category && other.Id == this.Id;
        }

        public override bool Equals(object obj)
        {
            if (obj is Trigger) {
                var other = (Trigger)obj;
                return other.Category == this.Category && other.Id == this.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return ((int)this.Category * this.Id);
        }

        public object Get(int index)         => Get<object>(index);
        public int GetInt(int index)         => Get<int>(index);
        public uint GetUInt(int index)       => Get<uint>(index);
        public double GetDouble(int index)   => Get<double>(index);
        public string GetString(int index)   => Get<string>(index);
    }
}