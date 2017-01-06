namespace EEScript.Lexical
{
    using Enums;

    internal class Token
    {
        public TokenPosition Position { get; set; }
        public TokenType Type { get; set; }
        public string Value { get; set; }

        public Token(TokenType type, string value, TokenPosition position)
        {
            this.Type = type;
            this.Value = value;
            this.Position = position;
        }

        public override string ToString()
        {
            return $"Token: {{ Type: \"{Type}\", Value: \"{Value}\", Position: {{ Index: \"{Position.Index}\", Line: \"{Position.Line}\", Column: \"{Position.Column}\" }} }}";
        }
    }
}
