using System.Collections.Generic;

namespace EEScript.Interfaces
{
    using Lexical;

    internal interface ILexer
    {
        IEnumerable<Token> Tokenize(string source);
    }
}
