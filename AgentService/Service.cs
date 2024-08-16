using System;
using System.ComponentModel;
using System.Net;
using System.ServiceProcess;
using Service.Clients.Scheduler;
using NLog;

namespace AgentService {
    public partial class Service : ServiceBase
    {
        private static readonly Logger Logger = LogManager.GetLogger(typeof(Service).Name);
        private static bool isPowerEvent = true;

        public Service()
        {
            InitializeComponent();

            if(!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["OnPowerEvent"]))
            isPowerEvent = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["OnPowerEvent"]);
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            if (!isPowerEvent) return false;
            Logger.Debug("OnPowerEvent ...");
            try
            {
                // Todo: send to api IncidentUtil.PutIncident2Point(IncidentKind.ServiceStatusChange, ObjectId, string.Format("Power({0})", powerStatus.ToString()));
                return base.OnPowerEvent(powerStatus);
            }
            catch (Exception e)
            {
                Logger.Error("OnPowerEvent : {0}", e.Message);
                return false;
            }

        }
        protected override void OnContinue()
        {
            if (!isPowerEvent) return;
            Logger.Debug("OnContinue ...");
            try
            {
                // Todo: send to api IncidentUtil.PutIncident2Point(IncidentKind.ServiceStatusChange, ObjectId, "Continue");
                base.OnContinue();
            }
            catch (Exception e)
            {
                Logger.Error("OnContinue : {0}", e.Message);
            }
        }

        protected override void OnStart(string[] args)
        {
            Logger.Debug(Environment.NewLine + "*****************************************************************************************   OnStart   *************************************************************************************************");

            try {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                ServicePointManager.ServerCertificateValidationCallback = (sender2, certificate, chain, errors) => true;

                // Todo: send to api IncidentUtil.PutIncident2Point(IncidentKind.ServiceStatusChange, ObjectId, "Start");

                var backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += WorkerJob;
                backgroundWorker.RunWorkerAsync();
            }
            catch (Exception e)
            {
                Logger.Error($"OnStart : {e.Message + e.StackTrace}");
            }
        }

        static void WorkerJob(object sender, DoWorkEventArgs e)
        {
            try
            {
                JobManager.Instance.ExecuteAllJobs();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected override void OnShutdown()
        {
            if (!isPowerEvent) return;
            Logger.Debug("OnShutdown ...");
            try
            {
                // Todo: send to api IncidentUtil.PutIncident2Point(IncidentKind.ServiceStatusChange, ObjectId, "Shutdown");
                base.OnShutdown();
            }
            catch (Exception e)
            {
                Logger.Error("OnShutdown : {0}", e.Message);
            }
}

        protected override void OnPause()
        {
            if (!isPowerEvent) return;
            Logger.Debug("OnPause ...");
            try 
            {
                // Todo: send to api IncidentUtil.PutIncident2Point(IncidentKind.ServiceStatusChange, ObjectId, "Pause");
                base.OnPause();
            }
            catch (Exception e)
            {
                Logger.Error("OnPause : {0}", e.Message);
            }
        }

        protected override void OnStop()
        {
            if (!isPowerEvent) return;
            Logger.Debug("OnStop ...");
            try
            {
                // Todo: send to api IncidentUtil.PutIncident2Point(IncidentKind.ServiceStatusChange, ObjectId, "Stop");
                JobManager.Instance.AbortAllJobs();
            }
            catch (Exception e)
            {
                Logger.Error("OnStop : {0}", e.Message);
            }
        }
    }
}