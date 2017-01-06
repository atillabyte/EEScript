using System.Collections.Generic;

namespace EEScript.Lexical
{
    using Interfaces;
    using Enums;
    using Helpers;

    internal class Parser
    {
        public ILexer Lexer { get; set; }

        public Parser(ILexer lexer)
        {
            this.Lexer = lexer;
        }

        public List<List<Trigger>> Parse(string source)
        {
            var triggerBlocks = new List<List<Trigger>>();
            var block = new List<Trigger>();

            Trigger currentTrigger = null,
                    previousTrigger = null;

            using (var iterator = this.Lexer.Tokenize(source).GetEnumerator()) {
                while (iterator.MoveNext()) {
                    var token = iterator.Current;

                    switch (token.Type) {
                        case TokenType.Trigger:
                            if (currentTrigger != null) {
                                if (previousTrigger != null) {
                                    if (previousTrigger.Category == TriggerCategory.Effect && currentTrigger.Category == TriggerCategory.Cause) {
                                        triggerBlocks.Add(block);
                                        block = new List<Trigger>();
                                    }
                                }
                                block.Add(currentTrigger);
                                previousTrigger = currentTrigger;
                            }

                            var category = (TriggerCategory)Helpers.IntParse(token.Value.Substring(1, token.Value.IndexOf(':') - 1));
                            var triggerId = token.Value.Substring(token.Value.IndexOf(':') + 1);
                                triggerId = triggerId.Substring(0, triggerId.Length - 1);

                            currentTrigger = new Trigger(category, Helpers.IntParse(triggerId));
                            break;
                        case TokenType.String:
                            token.Value = token.Value.Substring(1, token.Value.Length - 2);

                            currentTrigger.Contents.Enqueue(token.Value);
                            break;
                        case TokenType.GlobalVariable:
                            var globalVariable = new Variable(VariableType.Global, token.Value.Substring(1, token.Value.Length - 1), null);

                            currentTrigger.Contents.Enqueue(globalVariable);
                            break;
                        case TokenType.PrivateVariable:
                            var privateVariable = new Variable(VariableType.Private, token.Value.Substring(1, token.Value.Length - 1), null);

                            currentTrigger.Contents.Enqueue(privateVariable);
                            break;
                        case TokenType.Number:
                            var value = double.Parse(token.Value, System.Globalization.NumberStyles.AllowDecimalPoint);

                            currentTrigger.Contents.Enqueue(value);
                            break;
                        case TokenType.EOF:
                            if (currentTrigger != null) {
                                if (currentTrigger.Category != TriggerCategory.Undefined) {
                                    block.Add(currentTrigger);
                                    triggerBlocks.Add(block);
                                }
                            }
                            break;
                    }
                }
            }

            return triggerBlocks;
        }
    }
}
