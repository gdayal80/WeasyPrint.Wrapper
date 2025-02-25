
![Nuget](https://img.shields.io/nuget/v/WeasyPrint.Wrapper)

# Introduction
WeasyPrint Wrapper for .Net on Windows / Linux / MacOS (Though not tested on MacOS (due to unavailability of Mac Machine), but it might work) to generate pdf from html. It uses [WeasyPrint](https://weasyprint.org/) to generate pdf from html. This package assumes that WeasyPrint is installed and working on your machine. This package is a modified version of `Balbarak.WeasyPrint`.

`WeasyPrint.Wrapper` simplifies the using of WeasyPrint on Windows / Linux

# Getting started

## Installation

For WeasyPrint installation follow WeasyPrint documentation:

[WeasyPrint Documentation](https://doc.courtbouillon.org/weasyprint/stable/)

From nuget packages

![Nuget](https://img.shields.io/nuget/v/WeasyPrint.Wrapper)

`PM> Install-Package WeasyPrint.Wrapper`

## Usage

### From html text 

```C#
using WeasyPrint.Wrapper;
using System.IO;

string workingDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
var trace = new ConsoleTraceWriter();

using (WeasyPrintClient client = new WeasyPrintClient(trace, workingDir))
{
    var html = "<!DOCTYPE html><html><body><h1>Hello World</h1></body></html>";

    var binaryPdf = await client.GeneratePdfAsync(html);

    File.WriteAllBytes("result.pdf",binaryPdf);
}
```

### From html file
```C#
using (WeasyPrintClient client = new WeasyPrintClient(trace, workingDir))
{
    var input = @"path\to\input.html";
    var output = @"path\to\output.pdf";

    await client.GeneratePdfAsync(input, output);
}
```

### Watch output and errors
```C#
using (WeasyPrintClient client = new WeasyPrintClient(trace, workingDir))
{
    var input = @"path\to\input.html";
    var output = @"path\to\output.pdf";

    client.OnDataError += OnDataError;
    
    await client.GeneratePdfAsync(input, output);
}

private void OnDataError(OutputEventArgs e)
{
    Console.WriteLine(e.Data);
}
```

# Third Parties
* [WeasyPrint](https://weasyprint.org/) - BSD 3-Clause License
