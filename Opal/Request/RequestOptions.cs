namespace Opal.Request
{
    internal class RequestOptions
    {
        public bool AllowRepeat { get; set; }
        public int Depth { get; set; }
        public UploadParameters Upload { get; set; }

        public static RequestOptions Default => new RequestOptions { AllowRepeat = true };
    }
}