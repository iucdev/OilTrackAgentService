using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Sunp.Api.Client;
using System.Threading.Tasks;
using Service.Clients.RSMDB;

namespace AgentService.References {
    public class RefTankStatus {
        public static IReadOnlyDictionary<TankStatus, string> TankStatusVsText => _statusVsText;
        private static readonly Dictionary<TankStatus, string> _statusVsText = new Dictionary<TankStatus, string>() {
            [TankStatus.Connected] = "Подключен",
            [TankStatus.Delayed] = "Задержка в передаче",
            [TankStatus.Disconnected] = "Не подключен",
        };
    }

    public enum TankStatus {
        Connected,
        Delayed,
        Disconnected,
    }

    public static class RefTankStatusExtension {
        public static string ToDisplayText(this TankStatus tankStatus) => RefTankStatus.TankStatusVsText[tankStatus];
    }
}
