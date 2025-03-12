using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sunp.Api.Client;

namespace AgentService.References {
    public class RefTransferOperationType {
        public static IReadOnlyDictionary<TransferOperationType, string> TypeVsText => _statusVsText;
        private static readonly Dictionary<TransferOperationType, string> _statusVsText = new Dictionary<TransferOperationType, string>() {
            [TransferOperationType.Income] = "Прием",
            [TransferOperationType.Outcome] = "Отпуск",
        };
    }

    public static class RefTransferOperationTypeExtensions {
        public static string ToDisplayText(this TransferOperationType type) => RefTransferOperationType.TypeVsText[type];
    }
}
