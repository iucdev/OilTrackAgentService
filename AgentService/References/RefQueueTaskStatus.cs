namespace AgentService.References {
    internal class RefQueueTaskStatus {
    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum QueueTaskStatus {
        InProcess,
        Abandon,
        Processed
    }
}
