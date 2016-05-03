using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DocoptNet;
using Serilog;

namespace seq_import
{
    class Program
    {
        const string Usage = @"seq-import: Import JSON log files into Seq.
Usage:
    seq-import.exe <file> <server> [--apikey=<k>]
    seq-import.exe (-h | --help)
Options:
    -h --help     Show this screen.
    <file>        The file to import.
    <server>      The Seq server URL.
    --apikey=<k>  Seq API key.
    ";

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole(outputTemplate: "seq-import {Level} {Message}{NewLine}{Exception}")
                .CreateLogger();

            Task.Run(async () =>
            {
                try
                {
                    var arguments = new Docopt().Apply(Usage, args, version: "Seq Import 0.1", exit: true);

                    var server = arguments["<server>"].ToString();
                    var file = Normalize(arguments["<file>"]);
                    var apiKey = Normalize(arguments["--apikey"]);

                    await Run(server, apiKey, file, 256*1024, 1024*1024);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Could not complete import");
                    Environment.Exit(-1);
                }
            }).Wait();
        }

        static string Normalize(ValueObject v)
        {
            if (v == null) return null;
            var s = v.ToString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        static async Task Run(string server, string apiKey, string file, ulong bodyLimitBytes, ulong payloadLimitBytes)
        {
            var originalFilename = Path.GetFileName(file);
            Log.Information("Opening JSON log file {OriginalFilename}", originalFilename);

            var importId = Guid.NewGuid();
            var tags = new Dictionary<string, object>
            {
                ["ImportId"] = importId
            };

            var logBuffer = new LogBuffer(file, tags);

            var shipper = new HttpImporter(logBuffer, new SeqImportConfig
            {
                ServerUrl = server,
                ApiKey = apiKey,
                EventBodyLimitBytes = bodyLimitBytes,
                RawPayloadLimitBytes = payloadLimitBytes
            });

            var sw = Stopwatch.StartNew();
            Log.Information("Starting import {ImportId}", importId);
            await shipper.Import();
            sw.Stop();
            Log.Information("Import {ImportId} completes in {Elapsed:0.0} ms", importId, sw.Elapsed.TotalMilliseconds);
        }
    }
}
