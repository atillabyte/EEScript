using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EEScript.Lexical
{
    using Enums;
    using Interfaces;

    internal class Lexer : ILexer
    {
        public List<TokenDefinition> TokenDefinitions { get; set; } = new List<TokenDefinition>();
        public Regex EndOfLineRegex = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);

        public Lexer AddDefinition(TokenDefinition definition)
        {
            this.TokenDefinitions.Add(definition);

            return this;
        }

        public IEnumerable<Token> Tokenize(string source)
        {
            var currentIndex = 0;
            var currentLine = 1;
            var currentColumn = 0;

            while (currentIndex < source.Length) {
                TokenDefinition tokenDefinition = null;
                Match tokenMatch = null;

                foreach (var definition in this.TokenDefinitions) {
                    var match = definition.Regex.Match(source, currentIndex);

                    if (match.Success && (match.Index - currentIndex) == 0) {
                        tokenDefinition = definition;
                        tokenMatch = match;

                        break;
                    }
                }

                if (tokenDefinition == null)
                    throw new Exception($"Unrecognized symbol '{source[currentIndex]}' at index {currentIndex} (line {currentLine}, column {currentColumn}).");

                var value = source.Substring(currentIndex, tokenMatch.Length);

                if (!tokenDefinition.Ignored)
                    yield return new Token(tokenDefinition.Type, value, new TokenPosition(currentIndex, currentLine, currentColumn));

                var terminatorMatch = EndOfLineRegex.Match(value);

                if (terminatorMatch.Success) {
                    currentLine += 1;
                    currentColumn = value.Length - (terminatorMatch.Index + terminatorMatch.Length);
                } else {
                    currentColumn += tokenMatch.Length;
                }

                currentIndex += tokenMatch.Length;
            }

            yield return new Token(TokenType.EOF, null, new TokenPosition(currentIndex, currentLine, currentColumn));
        }
    }
}
