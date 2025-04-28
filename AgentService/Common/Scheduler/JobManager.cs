using Service.Clients.Utils;
using Service.Enums;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Service.Clients.Scheduler {
    public class JobManager {
        private static JobManager _jobInstance = null;
        private Logger log = LogManager.GetLogger("Sender");

        private List<BackgroundWorker> _workers = null;

        private JobManager() {
            _workers = new List<BackgroundWorker>();
        }

        public static JobManager Instance {
            get { return _jobInstance ?? (_jobInstance = new JobManager()); }
        }

        public void ExecuteAllJobs() {
            log.Debug("Begin ExecuteAllJobs");

            var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings;
            try {
                var jobs = new List<Job> {
                    new QueueTaskJob { Name = "QueueTask" }
                };
                jobs.AddRange(
                    objectSettings.Objects
                        .Select(o => new MultiThreadClientJob { 
                            Name = o.ObjectId.ToString(),
                            ObjectId = o.ObjectId.Value
                        })
                );

                if (jobs.Any()) {
                    foreach (var instanceJob in jobs) {
                        try {
                            _workers.Add(instanceJob.RunWorker());
                            log.Debug($"Run worker '{instanceJob.Name}'");
                        } catch (Exception ex) {
                            log.Error($"Error on run worker'{instanceJob.Name}' error: {ex.Message + ex.StackTrace}");
                        }
                    }
                }
            } catch (Exception ex) {
                log.Error($"Error on ExecuteAllJobs: {ex.Message + ex.StackTrace}");
            }

            log.Debug("End ExecuteAllJobs");
        }

        public void AbortAllJobs() {
            log.Debug("AbortAllJobs...");

            if (_workers == null) {
                log.Debug("No workers to stop");
                return;
            }

            foreach (var worker in _workers)
                worker.CancelAsync();

            var counter = 0;

            while (_workers.Any(t => t.IsBusy) && counter <= 61) {
                Thread.Sleep(1000);
                counter++;
            }

            _workers = null;

            log.Debug("__END__" + Environment.NewLine + Environment.NewLine + "**************************************************************************************************************************************************************************************************************");
        }

    }
}
