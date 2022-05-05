# Opal
A client library for the Gemini protocol written for .NET 6

## Features
- Synchronous requests (async coming soon)
- Typed Gemtext document handling
- Event-based user input
- Configurable redirect behavior
- Optional client certificate support with creation and persistence
- Optional TOFU semantics with persistent certificate caching
- No external libraries (only uses what .NET provides)

## Usage
Install the Nuget package
```
Install-Package Opal -Version 1.0.0
```
Create an instance of a client and make requests
```csharp
// the default behavior is to automatically follow redirects and to persit 
// local and remote certificates to disk
var client = OpalClient.CreateNew(OpalOptions.Default);

var response = client.SendRequest("gemini.circumlunar.space");

if (response is GemtextResponse gmi)
{
  // the response body may accessed directly...
  using (var reader = new StreamReader(gmi.Body))
    Console.WriteLine(reader.ReadToEnd());
  
  // ... or as a collection of strongly-typed ILine objects
  foreach (var line in gmi.AsDocument())
  {
    if (line is LinkLine link)
      Console.WriteLine($"Found link to {link.Uri}");
     
    Console.WriteLine(line);
  }
}
```
Subscribe to events in order to control certain protocol features
```csharp
var client = OpalClient.CreateNew(OpalOptions.Default);

// using the default configuration, this certificate will be saved to the disk and
// sent whenever this host asks for it
client.CertificateRequired += (_, e) =>
  e.Certificate = CertificateHelper.GenerateNew($"cool glasses for {e.Host}", TimeSpan.FromDays(100));

client.InputRequired += 
  (_, e) => 
  {
    Console.Write($"{e.Prompt}: ");
    e.Value = Console.ReadLine();
  };
```
