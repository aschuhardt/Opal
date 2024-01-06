![Opal on Nuget](https://img.shields.io/nuget/v/Opal) ![Opal build](https://img.shields.io/github/actions/workflow/status/aschuhardt/Opal/dotnet.yml?branch=main) ![Last commit](https://img.shields.io/github/last-commit/aschuhardt/Opal) ![MIT](https://img.shields.io/github/license/aschuhardt/Opal)

# Opal
A client library for the Gemini and Titan protocols targeting .NET Standard 2.0, .NET 6+

## Features
- Asynchronous requests
- Typed Gemtext document handling
- Event-based user input
- Configurable redirect behavior
- Optional client certificate support with creation and persistence
- Optional TOFU semantics with persistent certificate caching
- No external dependencies

## Usage
Install the Nuget package

```
Install-Package Opal -Version 1.7.1
```

Create an instance of a client and make requests

```csharp
// the default behavior is to automatically follow redirects and to persit 
// local and remote certificates to disk
var client = new OpalClient();

var response = await client.SendRequestAsync("gemini.circumlunar.space");

if (response is GemtextResponse gmi)
{
  // the response body may accessed directly...
  await using (var reader = new StreamReader(gmi.Body))
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
