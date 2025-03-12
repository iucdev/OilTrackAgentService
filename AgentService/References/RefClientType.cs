namespace AgentService.References {
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ClientType {
        UnDefind,
        PiClient,
        OpcClient,
        DboClient,
        VrClient,
        Pv4Client,
        StrunaClient,
        IglaClient,
        SensClient,
        FafnirVrClient,
        OpcHdaClient
    }
}
