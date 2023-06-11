using System.IO;

namespace Opal.Request
{
    internal class UploadParameters
    {
        public Stream Content { get; set; }
        public string Mime { get; set; }
        public string Token { get; set; }
        public int Size { get; set; }
    }
}