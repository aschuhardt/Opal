namespace Opal.CallbackArgs
{
    public class InputRequiredArgs
    {
        internal InputRequiredArgs(bool isSensitive, string prompt)
        {
            IsSensitive = isSensitive;
            Prompt = prompt;
            Value = null;
        }

        public string Prompt { get; }
        public bool IsSensitive { get; }
        public string Value { get; set; }
    }
}