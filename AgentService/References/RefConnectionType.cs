namespace AgentService.References {
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ConnectionType {
        Dbo,
        Ip,
        Com
    }
}
