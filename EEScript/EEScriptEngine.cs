using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EEScript
{
    using Lexical;

    using Interfaces;
    using Enums;

    [Serializable]
    public sealed class EEScriptException : Exception
    {
        public EEScriptException() { }

        public EEScriptException(string message) : base(message) { }

        public EEScriptException(string format, params object[] message) : base(String.Format(format, message)) { }

        public EEScriptException(string message, Exception inner) : base(message, inner) { }
    }

    public class EEScriptEngine
    {
        private ILexer _lexer;
        private Parser _parser;
        private Options _options;
        internal List<Page> _pages;

        public Options Options => _options;

        public EEScriptEngine(Options options = null)
        {
            if (options == null)
                _options = new Options();

            _lexer = new Lexer()
                 .AddDefinition(new TokenDefinition(TokenType.Trigger,         new Regex(@"\([0-9]{1}\:[0-9]{1," + Int32.MaxValue + @"}\)",                            RegexOptions.Compiled)))
                 .AddDefinition(new TokenDefinition(TokenType.GlobalVariable,  new Regex($@"\{_options.GlobalVariableDeclarationSymbol}[\ba-zA-Z\d\D][\ba-zA-Z\d_]*",  RegexOptions.Compiled)))
                 .AddDefinition(new TokenDefinition(TokenType.PrivateVariable, new Regex($@"\{_options.PrivateVariableDeclarationSymbol}[\ba-zA-Z\d\D][\ba-zA-Z\d_]*", RegexOptions.Compiled)))
                 .AddDefinition(new TokenDefinition(TokenType.String,          new Regex(@"\" + _options.StringBeginSymbol + @"(.*?)\" + _options.StringEndSymbol,     RegexOptions.Compiled)))
                 .AddDefinition(new TokenDefinition(TokenType.Number,          new Regex(@"[-+]?([0-9]*\.[0-9]+|[0-9]+)",                                              RegexOptions.Compiled)))
                 .AddDefinition(new TokenDefinition(TokenType.Comment,         new Regex(@"\" + _options.CommentSymbol + @".*[\r|\n]",                                 RegexOptions.Compiled), ignored: true))
                 .AddDefinition(new TokenDefinition(TokenType.Word,            new Regex(@"\w+",                                                                       RegexOptions.Compiled), ignored: true))
                 .AddDefinition(new TokenDefinition(TokenType.Symbol,          new Regex(@"\W",                                                                        RegexOptions.Compiled), ignored: true))
                 .AddDefinition(new TokenDefinition(TokenType.Whitespace,      new Regex(@"\s+",                                                                       RegexOptions.Compiled), ignored: true));

            _parser = new Parser(lexer: _lexer);
            _pages = new List<Page>();
        }

        public Page LoadFromString(string source)
        {
            var page = new Page(this)
                    .InsertBlocks(_parser.Parse(source));

            _pages.Add(page);

            return page;
        }
    }

    public class Options
    {
        public Options()
        {

        }

        /// <summary>
        /// Allow an existing TriggerHandler to be overridden by newer TriggerHandler
        /// <para>Default: false</para>
        /// </summary>
        public bool CanOverrideTriggerHandlers { get; set; } = false;

        /// <summary>
        /// Beginning string literal symbol
        /// <para>Default: {</para>
        /// </summary>
        public char StringBeginSymbol { get; set; } = '{';

        /// <summary>
        /// Ending string literal symbol
        /// <para>Default: }</para>
        /// </summary>
        public char StringEndSymbol { get; set; } = '}';

        /// <summary>
        /// Global Variable literal symbol
        /// <para>Default: %</para>
        /// </summary>
        public char GlobalVariableDeclarationSymbol { get; set; } = '~';

        /// <summary>
        /// Private Variable literal symbol
        /// <para>Default: ~</para>
        /// </summary>
        public char PrivateVariableDeclarationSymbol { get; set; } = '%';

        /// <summary>
        /// Comment literal symbol
        /// <para>Default: *</para>
        /// </summary>
        public string CommentSymbol { get; set; } = "*";
    }
}
