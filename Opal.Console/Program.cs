using System.Diagnostics;
using System.Text;
using Opal;
using Opal.Authentication;
using Opal.Event;
using Opal.Response;

var client = OpalClient.CreateNew(OpalOptions.Default);

client.InputRequired += PromptForInput;
client.ConfirmRedirect += ConfirmRedirect;
client.RemoteCertificateUnrecognized += ConfirmUntrustedCertificate;
client.CertificateRequired += GenerateCertificate;

void GenerateCertificate(object? sender, CertificateRequiredEventArgs e)
{
    var subject = GetInput("Name to associate with this certificate");
    e.Certificate = CertificateHelper.GenerateNew(subject, TimeSpan.FromDays(365));
}

void ConfirmUntrustedCertificate(object? sender, RemoteCertificateUnrecognizedEventArgs e)
{
    e.AcceptAndTrust =
        ConfirmOrDeny("The remote server's certificate has changed.  " +
                      $"The new certificate's SHA-256 hash is: ({e.Fingerprint[..12]}).  " +
                      "Trust this new certificate?");
}

string GetInput(string message)
{
    Console.Write($"{message}: ");
    return (Console.ReadLine() ?? string.Empty).Trim();
}

bool ConfirmOrDeny(string message)
{
    while (true)
    {
        Console.Write($"{message} [Y/n]");
        var command = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(command) ||
            command.Equals("y", StringComparison.InvariantCultureIgnoreCase))
            return true;

        if (command.Equals("n", StringComparison.InvariantCultureIgnoreCase))
            return false;
    }
}

void ConfirmRedirect(object? sender, ConfirmRedirectEventArgs e)
{
    e.FollowRedirect = ConfirmOrDeny($"Follow redirect to {e.Uri}");
}

void PromptForInput(object? sender, InputRequiredEventArgs e)
{
    e.Value = GetInput(e.Prompt);
}

while (true)
{
    Console.Write("Enter a Gemini URL or 'exit': ");
    var command = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(command))
        continue;

    if (command.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
        break;

    var response = client.SendRequest(command);
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
            using (var file = File.OpenWrite(tempPath))
            {
                successfulResponse.Body.CopyTo(file);
            }

            Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
        }
    }
    else
    {
        Console.WriteLine(response.ToString());
    }
}

Console.WriteLine("Goodbye!");