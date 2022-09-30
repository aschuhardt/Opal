using System.Diagnostics;
using Opal;
using Opal.Authentication.Database;
using Opal.Response;
using Opal.Tofu;

string GetInput(string message)
{
    return (Console.ReadLine() ?? string.Empty).Trim();
}

Task<string> PromptForPassword(string s)
{
    return Task.FromResult(GetInput("Password"));
}

Task LogCertificateFailure(string msg)
{
    Console.WriteLine(msg);
    return Task.CompletedTask;
}

Task.Run(async () =>
{
    var client = new OpalClient();

    while (true)
    {
        Console.Write("Enter a Gemini URL or 'exit': ");
        var command = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(command))
            continue;

        if (command.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
            break;

        var response = await client.SendRequestAsync(command);
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
        {
            Console.WriteLine(response.ToString());
        }
    }
}).Wait();

Console.WriteLine("Goodbye!");