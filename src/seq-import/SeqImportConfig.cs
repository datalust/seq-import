namespace seq_import
{
    class SeqImportConfig
    {
        public string ServerUrl { get; set; }
        public ulong EventBodyLimitBytes { get; set; }
        public ulong RawPayloadLimitBytes { get; set; }
        public string ApiKey { get; set; }
    }
}
