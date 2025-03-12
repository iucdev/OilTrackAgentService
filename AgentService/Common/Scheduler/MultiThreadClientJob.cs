using AgentService.References;
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
        public override NamedBackgroundWorker RunWorker()
        {
            Worker = new NamedBackgroundWorker(Name) { WorkerSupportsCancellation = true };
            Worker.DoWork += DoWork;
            Worker.RunWorkerAsync();
            return Worker;
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            try {
                var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings;
                var clientObjectSettings = objectSettings.CloneObjectSettings();
                clientObjectSettings.Objects = objectSettings.Objects.Where(t => t.ObjectId == ObjectId).ToArray();

                AMultiServerClient client = null;
                AMultiServerClient2 client2 = null;
                switch (clientObjectSettings.ClientType) {
                    case ClientType.PiClient: {
                            Log.Debug("Start collect data form PI system");
                            client = new PiMultiServerClient(clientObjectSettings, Worker);
                            break;
                        }
                    case ClientType.OpcClient: {
                            Log.Debug("Start collect data form OPC system");
                            client = new OpcMultiServerClient(clientObjectSettings, Worker);
                            break;
                        }
                    case ClientType.DboClient: {
                            Log.Debug("Start collect data form dbo");
                            client = new DboMultiServerClientV2(clientObjectSettings, Worker);
                            break;
                        }
                    case ClientType.VrClient: {
                            Log.Debug("Start collect data form Veeder Root");
                            client = new VeederRootMultiServerClient(clientObjectSettings, Worker);
                            break;
                        }
                    case ClientType.Pv4Client: {
                            Log.Debug("Start collect data form PV4");
                            client2 = new Pv4MultiServerClient(clientObjectSettings, Worker);
                            client2.StartCollection();
                            break;
                        }
                    //case ClientType.IglaClient: {
                    //        Log.Debug("Start collect data form Igla");
                    //        client = new IglaMultiServerClient(clientObjectSettings, Worker);
                    //        break;
                    //    }
                    //case ClientType.StrunaClient: {
                    //        Log.Debug("Start collect data form Struna");
                    //        client = new StrunaMultiServerClient(clientObjectSettings, Worker);
                    //        break;
                    //    }
                    //case ClientType.SensClient: {
                    //        Log.Debug("Start collect data form SENS");
                    //        client = new SensMultiServerClient(clientObjectSettings, Worker);
                    //        break;
                    //    }
                    //case ClientType.FafnirVrClient: {
                    //        Log.Debug("Start collect data form Fafnir Veeder Root");
                    //        client = new FafnirMultiServerClient(clientObjectSettings, Worker);
                    //        break;
                    //    }
                    case ClientType.OpcHdaClient: {
                            Log.Debug("Start collect data form OPC HDA system");
                            client = new OpcHdaMultiServerClient(clientObjectSettings, Worker);
                            break;
                        }
                    default: throw new NotImplementedException();
                }
                if (client != null) {
                    client.StartCollection();
                }
            } catch (Exception ex) {
                Log.Error($"MultiThreadClient DoWork error {ex.Message + ex.StackTrace}");
            }
        }
    }
}
