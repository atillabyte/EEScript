namespace EEScript
{
    using Enums;

    public class Variable
    {
        public VariableType Type { get; set; }

        public string Key { get; set; }
        public object Value { get; set; }

        public Variable(VariableType type, string key, object value)
        {
            this.Type = type;

            this.Key = key;
            this.Value = value;
        }
    }
}