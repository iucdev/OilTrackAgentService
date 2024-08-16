namespace Service.Enums {
    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum QueueTaskType {
        SendTankMeasurements,
        SendTankTransfer,
        SendFlowmeterMeasurements
    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum QueueTaskStatus {
        InProcess,
        Abandon
    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ClientType  {
        UnDefind,
        PiClient ,
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

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ConnectionType  {
        Dbo,
        Ip,
        Com
    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum DboType {
        Default,
        FireBird,
        Oracle,
        MySql
    }
}
