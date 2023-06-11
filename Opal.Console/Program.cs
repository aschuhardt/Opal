using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Opal;
using Opal.Authentication.Certificate;
using Opal.Response;

string? _certPath = null;

string GetInput()
{
    return (Console.ReadLine() ?? string.Empty).Trim();
}

Task<IClientCertificate> GetClientCertificate()
{
    if (string.IsNullOrEmpty(_certPath))
        return Task.FromResult<IClientCertificate?>(null);

    var pkcs12 = X509Certificate2.CreateFromPemFile(_certPath).Export(X509ContentType.Pkcs12);
    var cert = new X509Certificate2(pkcs12);
    return Task.FromResult(new ClientCertificate(cert) as IClientCertificate);
}

Task.Run(async () =>
{
    var client = new OpalClient();

    client.GetActiveClientCertificateCallback = GetClientCertificate;

    while (true)
    {
        Console.Write("Enter a Gemini URL or 'exit': ");
        var command = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(command))
            continue;

        if (command.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            break;

        if (command.Equals("cert", StringComparison.InvariantCultureIgnoreCase))
        {
            Console.Write("Enter the client certificate file's path: ");
            _certPath = GetInput();
        }

        IGeminiResponse response;

        if (command.StartsWith("titan"))
        {
            Console.Write("Enter the text to upload: ");
            var contents = GetInput();
            await using var payload = new MemoryStream(Encoding.UTF8.GetBytes(contents));
            response = await client.UploadAsync(command, contents.Length, null, "text/plain; charset=utf-8", payload);
        }
        else
            response = await client.SendRequestAsync(command);

        if (response.IsSuccess && response is SuccessfulResponse successfulResponse)
        {
            if (successfulResponse.IsGemtext && successfulResponse is GemtextResponse gmiResponse)
            {
                foreach (var line in gmiResponse.AsDocument())
                    Console.WriteLine(line);
            }
            else
            {
                // save the file to a temporary location and open it in an OS-dependent fashion
                var tempPath = Path.GetTempFileName();
                await using (var file = File.OpenWrite(tempPath))
                {
                    successfulResponse.Body.CopyTo(file);
                }

                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
            }
        }
        else
            Console.WriteLine(response.ToString());
    }
}).Wait();

Console.WriteLine("Goodbye!");