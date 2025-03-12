using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgentService.References {
    internal class RefQueueTaskType {
    }

    [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum QueueTaskType {
        SendTankMeasurements,
        SendTankTransfer,
        SendFlowmeterMeasurements
    }
}
