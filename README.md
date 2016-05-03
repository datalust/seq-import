# seq-import.exe

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

### Creating compatible JSON files with Serilog

The import tool uses Serilog's canonical JSON format. To create a file in this format, either:

1. Specify a `Serilog.Formatting.Json.JsonFormatter` as the `ITextFormatter` for a log event sink; or,
2. Use a sink from [Serilog.Sinks.Json](https://github.com/nblumhardt/serilog-sinks-json).
