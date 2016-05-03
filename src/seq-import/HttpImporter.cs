using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace seq_import
{
    class HttpImporter
    {
        const string ApiKeyHeaderName = "X-Seq-ApiKey";
        const string BulkUploadResource = "api/events/raw";

        readonly LogBuffer _logBuffer;
        readonly SeqImportConfig _importConfig;
        readonly HttpClient _httpClient;

        public HttpImporter(LogBuffer logBuffer, SeqImportConfig importConfig)
        {
            if (logBuffer == null) throw new ArgumentNullException(nameof(logBuffer));
            if (importConfig == null) throw new ArgumentNullException(nameof(importConfig));

            if (string.IsNullOrWhiteSpace(importConfig.ServerUrl))
                throw new ArgumentException("The destination Seq server URL must be provided.");

            _logBuffer = logBuffer;
            _importConfig = importConfig;

            var baseUri = importConfig.ServerUrl;
            if (!baseUri.EndsWith("/"))
                baseUri += "/";

            _httpClient = new HttpClient { BaseAddress = new Uri(baseUri) };
        }

        public async Task Import()
        {
            var sendingSingles = 0;
            var sent = 0L;
            do
            {
                var available = _logBuffer.Peek((int)_importConfig.RawPayloadLimitBytes);
                if (available.Length == 0)
                {
                    break;
                }

                Stream payload;
                ulong lastIncluded;
                MakePayload(available, sendingSingles > 0, out payload, out lastIncluded);
                var len = payload.Length;

                var content = new StreamContent(new UnclosableStreamWrapper(payload));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json")
                {
                    CharSet = Encoding.UTF8.WebName
                };

                if (!string.IsNullOrWhiteSpace(_importConfig.ApiKey))
                    content.Headers.Add(ApiKeyHeaderName, _importConfig.ApiKey);

                var result = await _httpClient.PostAsync(BulkUploadResource, content);
                if (result.IsSuccessStatusCode)
                {
                    sent += len;
                    Log.Information("Sent {TotalBytes} total bytes uploaded", sent);

                    _logBuffer.Dequeue(lastIncluded);
                    if (sendingSingles > 0)
                        sendingSingles--;
                }
                else if (result.StatusCode == HttpStatusCode.BadRequest || 
                    result.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                {
                    if (sendingSingles != 0)
                    {
                        payload.Position = 0;
                        var payloadText = new StreamReader(payload, Encoding.UTF8).ReadToEnd();
                        Log.Error("HTTP shipping failed with {StatusCode}: {Result}; payload was {InvalidPayload}", result.StatusCode, await result.Content.ReadAsStringAsync(), payloadText);
                        _logBuffer.Dequeue(lastIncluded);
                        sendingSingles = 0;
                    }
                    else
                    {
                        Log.Warning("Batch failed with {StatusCode}, breaking out the first hundred events to send individually...", result.StatusCode);

                        // Unscientific (should "binary search" in batches) but sending the next
                        // hundred events singly should flush out the problematic one.
                        sendingSingles = 100;
                    }
                }
                else
                {
                    Log.Error("Received failed HTTP shipping result {StatusCode}: {Result}", result.StatusCode, await result.Content.ReadAsStringAsync());
                    break;
                }
            }
            while (true);
        }

        void MakePayload(LogBufferEntry[] entries, bool oneOnly, out Stream utf8Payload, out ulong lastIncluded)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (entries.Length == 0) throw new ArgumentException("Must contain entries");
            lastIncluded = 0;

            var raw = new MemoryStream();
            var content = new StreamWriter(raw, Encoding.UTF8);
            content.Write("{\"Events\":[");
            content.Flush();
            var contentRemainingBytes = (int)_importConfig.RawPayloadLimitBytes - 13; // Includes closing delims

            var delimStart = "";
            foreach (var logBufferEntry in entries)
            {
                if ((ulong)logBufferEntry.Value.Length > _importConfig.EventBodyLimitBytes)
                {
                    Log.Warning("Oversized event will be skipped, {Payload}", Encoding.UTF8.GetString(logBufferEntry.Value));
                    lastIncluded = logBufferEntry.Key;
                    continue;
                }

                // lastIncluded indicates we've added at least one event
                if (lastIncluded != 0 && contentRemainingBytes - (delimStart.Length + logBufferEntry.Value.Length) < 0)
                    break;

                content.Write(delimStart);
                content.Flush();
                contentRemainingBytes -= delimStart.Length;

                raw.Write(logBufferEntry.Value, 0, logBufferEntry.Value.Length);
                contentRemainingBytes -= logBufferEntry.Value.Length;

                lastIncluded = logBufferEntry.Key;

                delimStart = ",";
                if (oneOnly)
                    break;
            }

            content.Write("]}");
            content.Flush();
            raw.Position = 0;
            utf8Payload = raw;
        }
    }
}
