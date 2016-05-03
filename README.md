# seq-import.exe [![Chocolatey](https://img.shields.io/chocolatey/v/seq-import.svg?maxAge=2592000)](https://chocolatey.org/packages/seq-import)

A CLI tool for importing JSON-formatted log files directly into Seq.

```
seq-import: Import JSON log files into Seq.

Usage:
    seq-import.exe <file> <server> [--apikey=<k>]
    seq-import.exe (-h | --help)

Options:
    -h --help     Show this screen.
    <file>        The file to import.
    <server>      The Seq server URL.
    --apikey=<k>  Seq API key.
```

Example:

```
seq-import.exe myapp.json https://my-seq --apikey=jhfaty89thfajkafhkl
```

The command will print a GUID `ImportId`, which will also be attached to the imported events in Seq.

### Installation

Requires .NET 4.5 or better.

With [Chocolatey](https://chocolatey.org/packages/seq-import):

```powershell
choco install seq-import
```

Or, download a [zip file of the source code](https://github.com/datalust/seq-import/archive/master.zip) and build/run locally.

### File format

Newline-separated JSON is the only format currently supported. The fields are:

 * `Timestamp` - an ISO 8601 timestamp with optional timezone
 * `Level` - (optional) either `Verbose`, `Debug`, `Information`, `Warning`, `Error`, `Fatal`
 * `MessageTemplate` - a Serilog-compatible [message template](https://github.com/adamchester/messagetemplates)
 * `Properties` (optional) - a dictionary of property values
 * `Renderings` (optional) - a dictionary of programming-language-specific formattings of properties that appear in the message template

### Creating compatible JSON files with Serilog

The import tool uses Serilog's canonical JSON format. To create a file in this format, either:

1. Specify a `Serilog.Formatting.Json.JsonFormatter` as the `ITextFormatter` for a log event sink; or,
2. Use a sink from [Serilog.Sinks.Json](https://github.com/nblumhardt/serilog-sinks-json).
