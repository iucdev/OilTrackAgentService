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
            try {
                _logger.Debug($"QueueTaskService->GetNextTask call");
                lock (syncRoot) {
                    var latestTask = QueueTaskRecord.GetFirstTaskFromDb(_logger);

                    if (latestTask == null) {
                        logger.Debug($"QueueTaskService->GetNextTask success");
                        return null;
                    }

                    logger.Debug($"QueueTaskService->GetNextTask success");
                    return latestTask;
                }
            } catch (Exception ex) { 
                _logger.Error($"QueueTaskService->GetNextTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void SaveTransfersAsTask(TankTransfers[] items) {
            try {
                _logger.Debug($"QueueTaskService->SaveTransfersAsTask call ({items.Count()} items)");
                var toSave = items
                    .Where(t => t.Transfers.Any())
                    .Select(item => {
                        var lastSyncDate = LastSyncRecord.GetByExternalId(item.TankId, _logger).LastTransfersSyncDate;
                        var transfers = item.Transfers.Where(transfer => transfer.EndDate > lastSyncDate).ToList();
                        item.Transfers = transfers;
                        return item;
                    });
                if (!toSave.Any()) {
                    _logger.Debug("QueueTaskService->SaveTransfersAsTask return null");
                    return;
                }
                lock (syncRoot) {
                    var newTask = new QueueTaskRecord
                    {
                        CreateDate = DateTime.Now,
                        Type = QueueTaskType.SendTankTransfer,
                        Status = QueueTaskStatus.InProcess,
                        Items = JsonConvert.SerializeObject(toSave),
                    };
                    newTask.AddToDb(_logger);
                }
                _logger.Debug($"QueueTaskService->SaveTransfersAsTask success");
            } catch (Exception ex) {
                _logger.Error($"QueueTaskService->SaveTransfersAsTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void SaveMeasurementsAsTask(TankMeasurements[] items) {
            try {
                _logger.Debug($"QueueTaskService->SaveMeasurementsAsTask call ({items.Count()} items)");
                var toSave = items
                    .Where(t => t.Measurements.Any())
                    .Select(item => {
                        var lastSyncDate = LastSyncRecord.GetByExternalId(item.TankId, _logger).LastMeasurementsSyncDate;
                        var measurements = item.Measurements.Where(measurement => measurement.MeasurementDate > lastSyncDate).ToList();
                        item.Measurements = measurements;
                        return item;
                    });
                if (!toSave.Any()) {
                    _logger.Debug("QueueTaskService->SaveMeasurementsAsTask return null");
                    return;
                }
                lock (syncRoot) {
                    var newTask = new QueueTaskRecord
                    {
                        CreateDate = DateTime.Now,
                        Type = QueueTaskType.SendTankMeasurements,
                        Status = QueueTaskStatus.InProcess,
                        Items = JsonConvert.SerializeObject(toSave),
                    };
                    newTask.AddToDb(_logger);
                }
                _logger.Debug($"QueueTaskService->SaveMeasurementsAsTask success");
            } catch (Exception ex){
                _logger.Error($"QueueTaskService->SaveMeasurementsAsTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void SaveFlowmeterAsTask(FlowmeterMeasurements[] items)
        {
            try {
                _logger.Debug($"QueueTaskService->SaveFlowmeterAsTask call ({items.Count()} items)");
                var toSave = items
                    .Where(t => t.Measurements.Any())
                    .Select(item => {
                        var lastSyncDate = LastSyncRecord.GetByExternalId(item.DeviceId, _logger).LastFlowmeterSyncDate;
                        var measurements = item.Measurements.Where(measurement => measurement.MeasurementDate.DateTime > lastSyncDate).ToList();
                        item.Measurements = measurements;
                        return item;
                    });
                if (!toSave.Any()) {
                    _logger.Debug("QueueTaskService->SaveFlowmeterAsTask return null");
                    return;
                }
                lock (syncRoot) {
                    var newTask = new QueueTaskRecord
                    {
                        CreateDate = DateTime.Now,
                        Type = QueueTaskType.SendFlowmeterMeasurements,
                        Status = QueueTaskStatus.InProcess,
                        Items = JsonConvert.SerializeObject(toSave),
                    };
                    newTask.AddToDb(_logger);
                }
                _logger.Debug($"QueueTaskService->SaveFlowmeterAsTask success");
            } catch (Exception ex) {
                _logger.Error($"QueueTaskService->SaveFlowmeterAsTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void RemoveTask(QueueTaskRecord task)
        {
            try {
                _logger.Debug($"QueueTaskService->RemoveTask call");
                lock (syncRoot) {
                    task.DeleteFromDb(_logger);
                }
                _logger.Debug($"QueueTaskService->RemoveTask success");
            } catch (Exception ex) {
                _logger.Error($"QueueTaskService->RemoveTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void AbandonTask(QueueTaskRecord task, string error)
        {
            try {
                _logger.Debug($"QueueTaskService->AbandonTask call");
                lock (syncRoot) {
                    task.Status = QueueTaskStatus.Abandon;
                    task.Error = error;
                    task.UpdateInDb(_logger);
                }
                _logger.Debug($"QueueTaskService->AbandonTask success");
            } catch (Exception ex) {
                _logger.Error($"QueueTaskService->AbandonTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }
    }
}
