using AgentService.Models;
using AgentService.References;
using Newtonsoft.Json;
using NLog;
using Service.Dtos;
using Service.Extensions;
using Service.LocalDb;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
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
                
                lock (syncRoot) {

                    var objectSources = ObjectSettingsSingleton.Instance.ObjectSettings.Objects.First().ObjectSources;
                    var newTransferRecordList = new List<TankTransfers>();
                    foreach (var item in items) {
                        _logger.Info($"TankNumber {item.TankId} Contain {item.Transfers.Count()} Transfers");
                        _logger.Info($"item.Transfers.Where(t => t.EndDate > {LastSyncRecord.GetByExternalId(item.TankId, _logger).LastTransfersSyncDate})");
                        _logger.Info($"TankNumber {item.TankId} Contain {item.Transfers.Where(t => t.EndDate > LastSyncRecord.GetByExternalId(item.TankId, _logger).LastTransfersSyncDate).Count()} Transfers");
                        foreach (var tankTransfer in item.Transfers.Where(t => t.EndDate > LastSyncRecord.GetByExternalId(item.TankId, _logger).LastTransfersSyncDate)) {
                            var newTransferRecord = new TankTransferRecord() {
                                InternalTankId = objectSources.First(t => t.ExternalId == item.TankId).InternalId,
                                ExternalTankId = item.TankId,

                                StartDate = tankTransfer.StartDate,
                                EndDate = tankTransfer.EndDate,

                                LevelStart = tankTransfer.LevelStart,
                                LevelEnd = tankTransfer.LevelEnd,
                                LevelUnitType = tankTransfer.LevelUnitType.Value,

                                MassStart = tankTransfer.MassStart,
                                MassEnd = tankTransfer.MassEnd,
                                MassUnitType = tankTransfer.MassUnitType.Value,

                                VolumeStart = tankTransfer.VolumeStart,
                                VolumeEnd = tankTransfer.VolumeEnd,
                                VolumeUnitType = tankTransfer.VolumeUnitType.Value,

                                OperationType = tankTransfer.OperationType.Value,
                                OilProductType = tankTransfer.OilProductType.Value
                            };
                            var result = newTransferRecord.AddToDb(_logger);
                            if (result != null) {
                                newTransferRecordList.Add(result);
                            }
                        }
                    }
                    _logger.Debug($"QueueTaskService->SaveTransfersAsTask call ({JsonConvert.SerializeObject(newTransferRecordList)}");
                    _logger.Debug($"QueueTaskService->SaveTransfersAsTask call ({newTransferRecordList.Count()} items)");

                    var toSave = newTransferRecordList
                        .Where(t => t.Transfers.Any())
                        .ToList();

                    if (!toSave.Any()) {
                        _logger.Debug("QueueTaskService->SaveTransfersAsTask return null");
                        return;
                    }

                    var newItems = new List<TankTransfers>();
                    foreach (var item in toSave) {
                        foreach (var chunk in item.Transfers.Chunk(500)) {
                            newItems.Add(new TankTransfers() {
                                TankId = item.TankId,
                                Transfers = chunk
                            });
                        }

                    }

                    foreach (var newItem in newItems) {
                        var newTask = new QueueTaskRecord {
                            CreateDate = DateTime.Now,
                            Type = QueueTaskType.SendTankTransfer,
                            Status = QueueTaskStatus.InProcess,
                            Items = JsonConvert.SerializeObject(new [] { newItem }),
                            PackageId = Guid.NewGuid().ToString()
                        };
                        newTask.AddToDb(_logger);
                    }
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
                    .Where(t => t.Measurements.Any());
                if (!toSave.Any()) {
                    _logger.Debug("QueueTaskService->SaveMeasurementsAsTask return null");
                    return;
                }
                lock (syncRoot) {

                    var newItems = new List<TankMeasurements>();
                    foreach(var item in toSave) {
                        foreach(var chunk in item.Measurements.Chunk(500)) {
                            newItems.Add(new TankMeasurements() {
                                TankId = item.TankId,
                                Measurements = chunk
                            });
                        }
                        
                    }

                    foreach (var newItem in newItems) {
                        var newTask = new QueueTaskRecord {
                            CreateDate = DateTime.Now,
                            Type = QueueTaskType.SendTankMeasurements,
                            Status = QueueTaskStatus.InProcess,
                            Items = JsonConvert.SerializeObject(new[] { newItem }),
                            PackageId = Guid.NewGuid().ToString()
                        };
                        newTask.AddToDb(_logger);
                    }
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
                    .Where(t => t.Measurements.Any());
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
                        PackageId = Guid.NewGuid().ToString()
                    };
                    newTask.AddToDb(_logger);
                }
                _logger.Debug($"QueueTaskService->SaveFlowmeterAsTask success");
            } catch (Exception ex) {
                _logger.Error($"QueueTaskService->SaveFlowmeterAsTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }

        public void RemoveTask(QueueTaskRecord task, ObjectSettings objectSettings)
        {
            try {
                _logger.Debug($"QueueTaskService->RemoveTask call");
                lock (syncRoot) {
                    task.Status = QueueTaskStatus.Processed;
                    _logger.Debug($"QueueTaskService->Status: {task.Status}");
                    task.UpdateInDb(_logger);

                    if(objectSettings.DeleteFromDbAfterCommit ?? false) {
                        task.DeleteFromDb(_logger);
                    }
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
                _logger.Error($"QueueTaskService->AbandonTask call");
                lock (syncRoot) {
                    task.Status = QueueTaskStatus.Abandon;
                    task.Error = error;
                    task.UpdateInDb(_logger);
                }
                _logger.Error($"QueueTaskService->AbandonTask success. Reason is: {error}");
            } catch (Exception ex) {
                _logger.Error($"QueueTaskService->AbandonTask exception: {ex.Message + ex.StackTrace}");
                throw ex;
            }
        }
    }
}
