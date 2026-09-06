namespace ColdReview.Debugger
{
    internal sealed class CallArgument
    {
        public CallArgument(string name, string type, string value)
        {
            Name = name ?? string.Empty;
            Type = type ?? string.Empty;
            Value = value ?? string.Empty;
        }

        public string Name { get; }
        public string Type { get; }
        public string Value { get; }

        public string Display => string.IsNullOrEmpty(Value)
            ? Name
            : Name + ": " + Truncate(Value, 80);

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= max)
            {
                return text;
            }

            return text.Substring(0, max) + "…";
        }
    }
}
