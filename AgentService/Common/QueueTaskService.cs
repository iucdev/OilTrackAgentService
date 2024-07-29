using Newtonsoft.Json;
using NLog;
using Service.Dtos;
using Service.Enums;
using Service.LocalDb;
using Sunp.Api.Client;
using System;
using System.IO;
using System.Linq;

namespace Service.Clients.Utils {
    public class QueueTaskService
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static QueueTaskService _instance = null;
        private static object syncRoot = new Object();
        private static object locker = new Object();

        public static QueueTaskService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (locker)
                    {
                        if (_instance == null)
                            _instance = new QueueTaskService();
                    }
                }
                return _instance;
            }
        }

        private QueueTaskService() {}

        public QueueTaskRecord GetNextTask(Logger logger)
        {
            lock (syncRoot) {
                var latestTask = QueueTaskRecord.GetFirstTaskFromDb();

                if (latestTask == null) { 
                    logger.Info($"Task not found");
                    return null;
                }

                return latestTask;
            }
        }

        public void SaveAsTask(TankTransfers[] items) {
            var toSave = items.Where(t => t.Transfers.Any());
            toSave = items.Select(item => {
                var lastSyncDate = LastSyncRecord.GetByExternalId(item.TankId).LastTransfersSyncDate;
                var transfers = item.Transfers.Where(transfer => transfer.EndDate > lastSyncDate).ToList();
                item.Transfers = transfers;
                return item;
            });
            if (!toSave.Any()) {
                return;
            }
            lock (syncRoot) {
                var newTask = new QueueTaskRecord {
                    CreateDate = DateTime.Now,
                    Type = QueueTaskType.SendTankTransfer,
                    Status = QueueTaskStatus.InProcess,
                    Items = JsonConvert.SerializeObject(toSave),
                };
                newTask.AddToDb();
            }
        }

        public void SaveAsTask(TankMeasurements[] items) {
            var toSave = items.Where(t => t.Measurements.Any());
            toSave = items.Select(item => {
                var lastSyncDate = LastSyncRecord.GetByExternalId(item.TankId).LastMeasurementsSyncDate;
                var measurements = item.Measurements.Where(measurement => measurement.MeasurementDate > lastSyncDate).ToList();
                item.Measurements = measurements;
                return item;
            });
            if (!toSave.Any()) {
                return;
            }
            lock (syncRoot) {
                var newTask = new QueueTaskRecord {
                    CreateDate = DateTime.Now,
                    Type = QueueTaskType.SendTankMeasurements,
                    Status = QueueTaskStatus.InProcess,
                    Items = JsonConvert.SerializeObject(toSave),
                };
                newTask.AddToDb();
            }
        }

        public void RemoveTask(QueueTaskRecord task)
        {
            lock (syncRoot)
            {
                try {
                    _logger.Info($"Try to remove done task with Id: {task.Id}");
                    task.DeleteFromDb();
                    _logger.Info($"Task with Id {task.Id} removed");
                } catch (Exception ex) {
                    _logger.Error($"Error on remove task with Id: {task.Id}: {ex.Message + ex.StackTrace}");
                    throw ex;
                }
            }
        }

        public void AbandonTask(QueueTaskRecord task, string error) {
            lock (syncRoot) {
                try {
                    _logger.Info($"Try to abandon task with Id: {task.Id}");
                    task.Status = QueueTaskStatus.Abandon;
                    task.Error = error; 
                    task.UpdateInDb();
                    _logger.Info($"Task with Id {task.Id} abandoned");
                } catch (Exception ex) {
                    _logger.Error($"Error on try to abandon task with Id {task.Id}: {ex.Message + ex.StackTrace}");
                    throw ex;
                }
            }
        }
    }
}
