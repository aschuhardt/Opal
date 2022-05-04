namespace Opal.Event;

public class InputRequiredEventArgs : EventArgs
{
    internal InputRequiredEventArgs(bool isSensitive, string prompt)
    {
        IsSensitive = isSensitive;
        Prompt = prompt;
        Value = null;
    }

    public string Prompt { get; }
    public bool IsSensitive { get; }
    public string Value { get; set; }
}