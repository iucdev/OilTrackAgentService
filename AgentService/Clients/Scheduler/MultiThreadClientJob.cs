using Service.Clients.Client;
using Service.Clients.DBO;
using Service.Clients.Fafnir;
using Service.Clients.IGLA;
using Service.Clients.ModBus;
using Service.Clients.OPC;
using Service.Clients.PI;
using Service.Clients.PV4;
using Service.Clients.SENS;
using Service.Clients.Utils;
using Service.Clients.VR;
using Service.Enums;
using System;
using System.ComponentModel;
using System.Linq;

namespace Service.Clients.Scheduler {
    public class MultiThreadClientJob : Job {
        public override NamedBackgroundWorker RunWorker() {
            Worker = new NamedBackgroundWorker(Name) { WorkerSupportsCancellation = true };
            Worker.DoWork += DoWork;
            Worker.RunWorkerAsync();
            return Worker;
        }

        private void DoWork(object sender, DoWorkEventArgs e) {
            try {
                var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings;
                var objectData = objectSettings.Objects.First(t => t.ObjectId == ObjectId);

                objectSettings.Objects = objectSettings.Objects.Where(t => t.ObjectId == ObjectId).ToArray();

                AMultiServerClient client = null;
                switch (objectSettings.ClientType) {
                    case ClientType.PiClient: {
                            Log.Debug("Start collect data form PI system");
                            client = new PiMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.OpcClient: {
                            Log.Debug("Start collect data form OPC system");
                            client = new OpcMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.DboClient: {
                            Log.Debug("Start collect data form dbo");
                            client = new DboMultiServerClientV2(objectSettings, Worker);
                            break;
                        }
                    case ClientType.VrClient: {
                            Log.Debug("Start collect data form Veeder Root");
                            client = new VeederRootMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.Pv4Client: {
                            Log.Debug("Start collect data form PV4");
                            client = new Pv4MultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.IglaClient: {
                            Log.Debug("Start collect data form Igla");
                            client = new IglaMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.StrunaClient: {
                            Log.Debug("Start collect data form Struna");
                            client = new StrunaMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.SensClient: {
                            Log.Debug("Start collect data form SENS");
                            client = new SensMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.FafnirVrClient: {
                            Log.Debug("Start collect data form Fafnir Veeder Root");
                            client = new FafnirMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    case ClientType.OpcHdaClient: {
                            Log.Debug("Start collect data form OPC HDA system");
                            client = new OpcHdaMultiServerClient(objectSettings, Worker);
                            break;
                        }
                    default: throw new NotImplementedException();
                }
                client.StartCollection();
            } catch (Exception ex) {
                Log.Error($"MultiThreadClient DoWork error {ex.Message + ex.StackTrace}");
            }
        }
    }
}
