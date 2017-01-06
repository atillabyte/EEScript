using System.Collections.Generic;

namespace EEScript.Interfaces
{
    using Lexical;

    internal interface ILexer
    {
        Lexer AddDefinition(TokenDefinition definition);
        IEnumerable<Token> Tokenize(string source);
    }
}
