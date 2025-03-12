using AgentService.References;
using Newtonsoft.Json;
using NLog;
using Service.Enums;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Reflection;

namespace Service.Clients.Utils {
    public sealed class ObjectSettingsSingleton {
        private static readonly Lazy<ObjectSettingsSingleton> lazy =
            new Lazy<ObjectSettingsSingleton>(() => new ObjectSettingsSingleton());

        public static ObjectSettingsSingleton Instance { get { return lazy.Value; } }

        public ObjectSettings ObjectSettings { get; private set; }
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private ObjectSettingsSingleton()
        {
            try {
                _logger.Info($"Try init ObjectSettingsSingleton");
                string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var jsonFilePath = Path.Combine(directoryPath, "objectSettings.json");

                string json = File.ReadAllText(jsonFilePath);
                ObjectSettings = JsonConvert.DeserializeObject<ObjectSettings>(json);

                var error = string.Empty;

                if (ObjectSettings is null) {
                    error = "ObjectSettings->ObjectSettings cannot be null";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                if (!ObjectSettings.Objects.Any()) {
                    error = "ObjectSettings->Objects must have items";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                if (string.IsNullOrEmpty(ObjectSettings.ApiUrl) || string.IsNullOrEmpty(ObjectSettings.ApiToken)) {
                    error = "ObjectSettings->Objects->ApiUrl/ApiToken cannot be null";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                if (ObjectSettings.Objects.Any(o => !o.ObjectSources.Any() || o.ObjectId == null)) {
                    error = "ObjectSettings->Objects->ObjectId cannot be null and ObjectSources must have items";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                if (ObjectSettings.Objects.Any(o => o.ObjectSources.Any(os => string.IsNullOrEmpty(os.InternalId) || os.ExternalId == null))) {
                    error = "ObjectSettings->Objects->InternalId/ExternalId cannot be null";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                if (ObjectSettings.Objects.Any(o => o.ObjectSources.Any(os => os.LevelUnitType == null || os.VolumeUnitType == null || os.MassUnitType == null))) {
                    error = "ObjectSettings->Objects->LevelUnitType/VolumeUnitType/MassUnitType cannot be null";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                if (ObjectSettings.ConnectionType != ConnectionType.Dbo && ObjectSettings.Objects.Any(o => o.ObjectSources.Any(os => os.OilProductType == null))) {
                    error = "ObjectSettings->Objects->OilProductType cannot be null";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                switch (ObjectSettings.ConnectionType) {
                    case ConnectionType.Dbo:
                        if (ObjectSettings.DatabaseConnectionConfig is null) {
                            error = "ObjectSettings->DatabaseConnectionConfig cannot be null";
                            _logger.Error(error);
                            throw new Exception(error);
                        }
                        if (ObjectSettings.DatabaseConnectionConfig.DboType == null || string.IsNullOrEmpty(ObjectSettings.DatabaseConnectionConfig.ConnectionString)) {
                            error = "ObjectSettings->DatabaseConnectionConfig->DboType/ConnectionString cannot be null";
                            _logger.Error(error);
                            throw new Exception(error);
                        }

                        break;
                    case ConnectionType.Ip:
                        if (ObjectSettings.IpConnectionConfig is null) {
                            error = "ObjectSettings->IpConnectionConfig cannot be null";
                            _logger.Error(error);
                            throw new Exception(error);
                        }
                        if (string.IsNullOrEmpty(ObjectSettings.IpConnectionConfig.IpAddress)) {
                            error = "ObjectSettings->IpConnectionConfig->IpAddress cannot be null or empty";
                            _logger.Error(error);
                            throw new Exception(error);
                        }
                        break;
                    case ConnectionType.Com:
                        if (ObjectSettings.ComConnectionConfig is null) {
                            error = "ObjectSettings->ComConnectionConfig cannot be null";
                            _logger.Error(error);
                            throw new Exception(error);
                        }
                        if (string.IsNullOrEmpty(ObjectSettings.ComConnectionConfig.PortName)) {
                            error = "ObjectSettings->ComConnectionConfig->PortName cannot be null or empty";
                            _logger.Error(error);
                            throw new Exception(error);
                        }
                        break;
                }
            } catch (Exception ex) {
                _logger.Error($"Try init ObjectSettingsSingleton failed. Error: {ex.Message}");
                throw ex;
            }
        }
    }

    public class ObjectSettings {
        public bool Mock {  get; set; }
        public bool ShowDebugLogs { get; set; }
        public string ApiUrl { get; set; }
        public string ApiToken { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public IpConnectionConfig IpConnectionConfig { get; set; }
        public ComConnectionConfig ComConnectionConfig { get; set; }
        public DatabaseConnectionConfig DatabaseConnectionConfig { get; set; }
        public ClientType? ClientType { get; set; }
        public ObjectData[] Objects { get; set; }
        public DateTime? StartFrom { get; set; }
        public bool? DeleteFromDbAfterCommit { get; set; }
        public Dictionary<string, OilProductType> OilProductTypeMapping { get; set; }
        public ObjectSettings CloneObjectSettings()
        {
            return new ObjectSettings
            {
                Mock = Mock,
                ApiUrl = ApiUrl,
                ApiToken = ApiToken,
                ConnectionType = ConnectionType,
                IpConnectionConfig = IpConnectionConfig,
                ComConnectionConfig = ComConnectionConfig,
                DatabaseConnectionConfig = DatabaseConnectionConfig,
                ClientType = ClientType,
                Objects = Objects,
                StartFrom = StartFrom,
                DeleteFromDbAfterCommit = DeleteFromDbAfterCommit,
                OilProductTypeMapping = OilProductTypeMapping
            };
        }
    }

    public class DatabaseConnectionConfig {
        public string ConnectionString { get; set; }
        public DboType? DboType { get; set; }
        public string MeasurementTable { get; set; }
        public string TransferTable { get; set; }
        public string FlowmeterTable { get; set; }
    }

    public class IpConnectionConfig {
        public string IpAddress { get; set; }
        public int? Port { get; set; }
        public string RSMDBConnectionString { get; set; }
        public string DComUid { get; set; }
        public string DComPwd { get; set; }
    }

    public class ComConnectionConfig {
        public string PortName { get; set; }
        public int? BaudRate { get; set; }
        public int? DataBits { get; set; }
        public Parity Parity { get; set; }
        public StopBits StopBits { get; set; }
        public int? ReadTimeout { get; set; }
        public int? WriteTimeout { get; set; }
    }

    public class ObjectData {
        public long? ObjectId { get; set; }
        public List<ObjectSource> ObjectSources { get; set; }
    }

    public class ObjectSource {
        public string InternalId { get; set; }
        public long? ExternalId { get; set; }
        public OilProductType? OilProductType { get; set; }
        public VolumeUnitType? VolumeUnitType { get; set; }
        public MassUnitType? MassUnitType { get; set; }
        public LevelUnitType? LevelUnitType { get; set; }
        public TankMeasurementParams TankMeasurementParams { get; set; }
        public TankTransferParams TankTransferParams { get; set; }
        public FlowmeterIndicatorParams FlowmeterIndicatorParams { get; set; }
        public string TransferCondition { get; set; }
        public string MeasurementCondition { get; set; }
        public string FlowmeterCondition { get; set; }
    }

    public class FlowmeterIndicatorParams {
        public string TotalMass { get; set; }
        public string FlowMass { get; set; }
        public string TotalVolume { get; set; }
        public string CurrentDensity { get; set; }
        public string CurrentTemperature { get; set; }
        public string DateTimeStamp { get; set; }
        public string OilProductType { get; set; }
        public string OperationType { get; set; }
        public string SourceTankId { get; set; }
        public string RenterXin { get; set; }
    }

    public class TankMeasurementParams {
        public string Temperature { get; set; }
        public string Density { get; set; }
        public string Volume { get; set; }
        public string Mass { get; set; }
        public string Level { get; set; }
        public string DateTimeStamp { get; set; }
        public string OilProductType { get; set; }
    }

    public class TankTransferParams {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string MassStart { get; set; }
        public string MassFinish { get; set; }
        public string LevelStart { get; set; }
        public string LevelFinish { get; set; }
        public string VolumeStart { get; set; }
        public string VolumeFinish { get; set; }
        public string OilProductType { get; set; }
    }
}
