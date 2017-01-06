namespace EEScript.Enums
{
    internal enum TokenType : byte
    {
        Trigger,
        String,
        Number,
        GlobalVariable,
        PrivateVariable,
        Comment,
        Whitespace,
        Word,
        Symbol,
        EOF
    }
}
