using Newtonsoft.Json;
using Service.Clients.Utils;
using NLog;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using Service.Enums;
using Service.LocalDb;

namespace Service.Clients.Scheduler {
    public class QueueTaskJob : Job
    {
        private readonly Logger _logger = LogManager.GetLogger(typeof(QueueTaskJob).Name);
        private SunpApiClient _sunpApiClient = SunpApiClientSingleton.Instance.SunpApiClient;
        private Timer _timer;

        public override NamedBackgroundWorker RunWorker()
        {
            Worker = new NamedBackgroundWorker(Name) { WorkerSupportsCancellation = true };
            Worker.DoWork += DoWork;
            Worker.RunWorkerAsync();
            return Worker;
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                _logger.Debug("Start QueueTaskWorker");
                _timer = new Timer { Interval = 60000 };
                _logger.Debug($"Set update interval={_timer.Interval} ms.");
                _timer.Elapsed += timerJob;
                _timer.Start();
                timerJob(null, null);//force start
            }
            catch (Exception ex)
            {
                _logger.Error($"DoWork error {ex.Message + ex.StackTrace}");
            }
        }

        private void timerJob(object sender, ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;

                if (Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = Worker.Name;

                if (Worker.CancellationPending) return;

                executeTasks();
            }
            catch (Exception ex)
            {
                _logger.Error($"QueueTask Error {ex.Message + ex.StackTrace}");
            }
            finally
            {
                _timer.Enabled = true;
            }
        }

        private void executeTasks() {
            while (true) {
                var task = QueueTaskService.Instance.GetNextTask(_logger);

                if (task == null) {
                    return;
                }

                try {
                    ResponseBodyBase response = null;

                    switch (task.Type) {
                        case QueueTaskType.SendTankMeasurements:
                            var tanksMeasurements = JsonConvert.DeserializeObject<TankMeasurements[]>(task.Items);
                            var tanksMeasurementsRequestBody = new SendTankIndicatorsRequestBody {
                                RequestGuid = Guid.NewGuid().ToString(),
                                PackageId = Guid.NewGuid().ToString(),
                                TanksMeasurements = tanksMeasurements
                            };
                            response = _sunpApiClient.TankSendTankIndicatorsAsync(tanksMeasurementsRequestBody)
                                .GetAwaiter()
                                .GetResult();
                            break;
                        case QueueTaskType.SendTankTransfer:
                            var tanksTransfers = JsonConvert.DeserializeObject<TankTransfers[]>(task.Items);
                            var tanksTransfersRequestBody = new SendTankTransfersRequestBody {
                                RequestGuid = Guid.NewGuid().ToString(),
                                PackageId = Guid.NewGuid().ToString(),
                                TanksTransfers = tanksTransfers
                            };
                            response = _sunpApiClient.TankSendTankTransfersAsync(tanksTransfersRequestBody)
                                .GetAwaiter()
                                .GetResult();
                            break;
                    }

                    if (response.Success) {
                        QueueTaskService.Instance.RemoveTask(task);
                    } else {
                        QueueTaskService.Instance.AbandonTask(task, response.Error);
                    }

                    LastSyncRecord.Update(task);
                    _logger.Debug($"Task execution with id: {task.Id} succeeded");
                } catch (Exception exception) {
                    _logger.Error($"Error occured on task execution with id: {task.Id}. Error: {exception.Message}");
                    continue;
                }
            }
        }
    }
}
