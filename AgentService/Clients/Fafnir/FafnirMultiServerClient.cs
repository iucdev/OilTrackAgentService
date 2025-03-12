using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Clients.VR;
using Service.Common;
using Service.Dtos;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Service.Clients.Fafnir {
    /// <summary>
    /// Fafnir (TLS) Client
    /// </summary>
    //public class FafnirMultiServerClient : AMultiServerClient {
    //    private List<TankMeasurementData> _tankMeasurements = new List<TankMeasurementData>();
    //    private List<TankTransferData> _tankTransfers = new List<TankTransferData>();

    //    public FafnirMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
    //    {
    //        ObjectSettings = objectSettings;
    //        Worker = worker;
    //        Init();
    //    }

    //    private new void Init()
    //    {
    //        Logger.Debug("Init");
    //        base.Init();
    //        try {
    //            var counter = 0;
    //            while (!Connect()) {
    //                if (Worker.CancellationPending) return;

    //                // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, Task.ObjectId);

    //                Logger.Info("Try connect to server {0}", counter++);
    //                Thread.Sleep(60000);
    //            }
    //        } catch (Exception e) {
    //            Logger.Error(e);
    //        }
    //    }

    //    protected override void Reconnect()
    //    {
    //        Logger.Error("Reconnecting...");
    //        Init();
    //        _timer.Start();
    //    }

    //    protected override void CollectData()
    //    {
    //        Logger.Debug("Collect data called");

    //        try {
    //            var tanksMeasurements = new List<TankMeasurements>();
    //            var tanksTransfers = new List<TankTransfers>();

    //            foreach (var source in ObjectSettings.Objects.First().ObjectSources) {
    //                if (int.TryParse(source.InternalId, out int nom))
    //                    if (Send214(nom, ObjectSettings.IpConnectionConfig.DComPwd) <= 0)
    //                        Logger.Debug("F214 return 0");

    //                if (_tankMeasurements.Any()) {
    //                    tanksMeasurements.Add(new TankMeasurements() {
    //                        TankId = source.ExternalId.Value,
    //                        Measurements = _tankMeasurements.SetEnums(source)
    //                    });
    //                }

    //                Thread.Sleep(500);

    //                if (Send215(nom) <= 0)
    //                    Logger.Debug("F215 return 0");

    //                if (_tankTransfers.Any()) {
    //                    tanksTransfers.Add(new TankTransfers {
    //                        TankId = source.ExternalId.Value,
    //                        Transfers = _tankTransfers.SetEnums(source)
    //                    });
    //                }
    //            }

    //            QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
    //            QueueTaskService.Instance.SaveTransfersAsTask(tanksTransfers.ToArray());
    //        } catch (Exception ex) {
    //            Logger.Error($"Error on collect data {ex.Message + ex.StackTrace}");
    //        }
    //    }

    //    protected override bool SkipCmd()
    //    {
    //        return LastCom == VrCMD.ESC;
    //    }

    //    protected override bool CheckRb(int bytesRec)
    //    {
    //        if (buf[bytesRec - 1] == 0x03) return true;
    //        Logger.Error("Error check summ");
    //        return false;
    //    }

    //    #region Commands
    //    /// <summary>
    //    /// In-Tank Inventory Report
    //    /// </summary>
    //    /// <param name="nom"></param>
    //    /// <returns></returns>
    //    private int Send201(int nom, string pass)
    //    {
    //        if (string.IsNullOrEmpty(pass))
    //            return Send201NP(nom);

    //        LastCom = VrCMD.CMD201;
    //        CmdDelay = 4000;
    //        cmdBuf[0] = VrCMD.SOH;
    //        cmdBuf[1] = (byte)pass[0];
    //        cmdBuf[2] = (byte)pass[1];
    //        cmdBuf[3] = (byte)pass[2];
    //        cmdBuf[4] = (byte)pass[3];
    //        cmdBuf[5] = (byte)pass[4];
    //        cmdBuf[6] = (byte)pass[5];
    //        cmdBuf[7] = 105;
    //        cmdBuf[8] = 48 + 2;
    //        cmdBuf[9] = 48 + 0;
    //        cmdBuf[10] = 48 + 1;
    //        cmdBuf[11] = 48 + 0;
    //        cmdBuf[12] = (byte)(48 + nom);
    //        Posinbuf = 1;
    //        Logger.Info("F201");
    //        return SendBuf(13);
    //    }
    //    private int Send201NP(int nom)
    //    {
    //        LastCom = VrCMD.CMD201;
    //        CmdDelay = 4000;
    //        cmdBuf[0] = VrCMD.SOH;
    //        cmdBuf[1] = 105;
    //        cmdBuf[2] = 48 + 2;
    //        cmdBuf[3] = 48 + 0;
    //        cmdBuf[4] = 48 + 1;
    //        cmdBuf[5] = 48 + 0;
    //        cmdBuf[6] = (byte)(48 + nom);
    //        Posinbuf = 1;
    //        Logger.Info("F201");
    //        return SendBuf(7);
    //    }

    //    /// <summary>
    //    /// In-Tank Mass/Density Inventory Report
    //    /// </summary>
    //    /// <param name="nom"></param>
    //    /// <returns></returns>
    //    private int Send214(int nom, string pass)
    //    {
    //        if (string.IsNullOrEmpty(pass))
    //            return Send214NP(nom);

    //        LastCom = VrCMD.CMD214;
    //        CmdDelay = 4000;
    //        cmdBuf[0] = VrCMD.SOH;
    //        cmdBuf[1] = (byte)pass[0];
    //        cmdBuf[2] = (byte)pass[1];
    //        cmdBuf[3] = (byte)pass[2];
    //        cmdBuf[4] = (byte)pass[3];
    //        cmdBuf[5] = (byte)pass[4];
    //        cmdBuf[6] = (byte)pass[5];
    //        cmdBuf[7] = 105;
    //        cmdBuf[8] = 48 + 2;
    //        cmdBuf[9] = 48 + 1;
    //        cmdBuf[10] = 48 + 4;
    //        cmdBuf[11] = 48 + 0;
    //        cmdBuf[12] = (byte)(48 + nom);
    //        Posinbuf = 1;
    //        Logger.Info("F214");
    //        return SendBuf(13);
    //    }
    //    private int Send214NP(int nom)
    //    {
    //        LastCom = VrCMD.CMD214;
    //        CmdDelay = 4000;
    //        cmdBuf[0] = VrCMD.SOH;
    //        cmdBuf[1] = 105;
    //        cmdBuf[2] = 48 + 2;
    //        cmdBuf[3] = 48 + 1;
    //        cmdBuf[4] = 48 + 4;
    //        cmdBuf[5] = 48 + 0;
    //        cmdBuf[6] = (byte)(48 + nom);
    //        Posinbuf = 1;
    //        Logger.Info("F214");
    //        return SendBuf(7);
    //    }

    //    /// <summary>
    //    /// In-Tank Mass/Density Delivery Report
    //    /// </summary>
    //    /// <param name="nom"></param>
    //    /// <returns></returns>
    //    private int Send215(int nom)
    //    {
    //        LastCom = VrCMD.CMD215;
    //        CmdDelay = 8000;//2000;
    //        cmdBuf[0] = VrCMD.SOH;
    //        cmdBuf[1] = 105;
    //        cmdBuf[2] = 48 + 2;
    //        cmdBuf[3] = 48 + 1;
    //        cmdBuf[4] = 48 + 5;
    //        cmdBuf[5] = 48 + 0;
    //        cmdBuf[6] = (byte)(48 + nom);
    //        Posinbuf = 1;
    //        Logger.Info("F215");
    //        return SendBuf(7);
    //    }

    //    #endregion

    //    #region Fillers

    //    protected override bool Fill(int bytesRec)
    //    {
    //        var result = false;

    //        switch (LastCom) {
    //            case VrCMD.CMD201: {
    //                    _tankMeasurements = Fill201();
    //                    if (_tankMeasurements != null && _tankMeasurements.Count > 0) result = true;
    //                    break;
    //                }
    //            case VrCMD.CMD214: {
    //                    _tankMeasurements = Fill214();
    //                    if (_tankMeasurements != null && _tankMeasurements.Count > 0) result = true;
    //                    break;
    //                }
    //            case VrCMD.CMD215: {
    //                    _tankTransfers = Fill215();
    //                    if (_tankTransfers != null && _tankTransfers.Count > 0) result = true;
    //                    break;
    //                }
    //        }
    //        return result;
    //    }

    //    private List<TankMeasurementData> Fill201()
    //    {
    //        var tankMeasurements = new List<TankMeasurementData>();

    //        try {
    //            if (!((buf[0] == 01) && (buf[1] == 0x69) && (buf[2] == 0x32) && (buf[3] == 0x30) && (buf[4] == 0x31))) {
    //                Logger.Error("Error F201");
    //                return tankMeasurements;
    //            }

    //            var dt = fillDTVR(7);
    //            if (dt is null) { 
    //                return tankMeasurements;
    //            }
    //            var pos = 17;

    //            while (buf[pos] != 0x26) {
    //                var measurement = new TankMeasurementData { MeasurementDate = dt.Value };

    //                var n = GetValue(pos + 7, 2);
    //                pos += 9;

    //                for (var i = 0; i < n; i++) {
    //                    decimal si;
    //                    try {
    //                        si = ToDecimal(pos);
    //                    } catch (Exception e) {
    //                        Logger.Error(e);
    //                        return tankMeasurements;
    //                    }

    //                    pos += 8;

    //                    switch (i) {
    //                        case 0: measurement.Volume = si; break;
    //                        case 3: measurement.Level = si; break;
    //                        case 5: measurement.Temperature = si; break;
    //                        case 10: measurement.Mass = si; break;
    //                        case 11: measurement.Density = si; break;
    //                    }
    //                }
    //                tankMeasurements.Add(measurement);
    //            }
    //        } catch (Exception e) {
    //            Logger.Error(e);
    //        }

    //        return tankMeasurements;
    //    }

    //    private List<TankMeasurementData> Fill214()
    //    {
    //        var tankMeasurements = new List<TankMeasurementData>();

    //        try {
    //            if (!((buf[0] == 01) && (buf[1] == 0x69) && (buf[2] == 0x32) && (buf[3] == 0x31) && (buf[4] == 0x34))) {
    //                Logger.Error("Error F214");
    //                return tankMeasurements;
    //            }

    //            var dt = fillDTVR(7);
    //            if (!dt.HasValue) {
    //                return tankMeasurements;
    //            }
    //            var pos = 17;

    //            while (buf[pos] != 0x26) {
    //                var measurement = new TankMeasurementData { MeasurementDate = dt.Value };

    //                var n = GetValue(pos + 7, 2);
    //                pos += 9;

    //                for (var i = 0; i < n; i++) {
    //                    decimal si;
    //                    try {
    //                        si = ToDecimal(pos);
    //                    } catch (Exception e) {
    //                        Logger.Error(e);
    //                        return tankMeasurements;
    //                    }

    //                    pos += 8;

    //                    switch (i) {
    //                        case 0: measurement.Volume = si; break;
    //                        case 1: measurement.Mass = si; break;
    //                        case 2: measurement.Density = si; break;
    //                        case 3: measurement.Level = si; break;
    //                        case 5: measurement.Temperature = si; break;
    //                    }
    //                }
    //                tankMeasurements.Add(measurement);
    //            }
    //        } catch (Exception e) {
    //            Logger.Error(e);
    //        }

    //        return tankMeasurements;
    //    }

    //    private List<TankTransferData> Fill215() {
    //        var tankTransfers = new List<TankTransferData>();

    //        try {
    //            if (!((buf[0] == 01) && (buf[1] == 0x69) && (buf[2] == 0x32) && (buf[3] == 0x31) && (buf[4] == 0x35))) {
    //                Logger.Error("Error F215");
    //                return tankTransfers;
    //            }

    //            var dt = fillDTVR(7);
    //            var pos = 17;

    //            while (buf[pos] != 0x26) {

    //                var tt = GetValue(pos, 2);
    //                var p = buf[pos + 2];
    //                var dd = GetValue(pos + 3, 2);
    //                pos += 5;

    //                for (var i = 0; i < dd; i++) {
    //                    var transfer = new TankTransferData();

    //                    var dt1 = fillDTVR(pos);
    //                    pos += 10;

    //                    var dt2 = fillDTVR(pos);
    //                    pos += 10;

    //                    if (!dt1.HasValue || !dt2.HasValue) {
    //                        return tankTransfers;
    //                    }

    //                    transfer.StartDate = dt1.Value;
    //                    transfer.EndDate = dt2.Value;

    //                    decimal sVol = 0, eVol = 0, sMass = 0, eMass = 0, sLevel = 0, eLevel = 0;

    //                    var nn = GetValue(pos, 2);
    //                    nn = int.Parse(nn.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
    //                    pos += 2;

    //                    for (var j = 0; j < nn; j++) {
    //                        decimal si;
    //                        try {
    //                            si = ToDecimal(pos);
    //                        } catch (Exception e) {
    //                            Logger.Error(e);
    //                            return tankTransfers;
    //                        }
    //                        pos += 8;

    //                        switch (j) {
    //                            case 0: sVol = si; break;
    //                            case 1: sMass = si; break;
    //                            case 5: eVol = si; break;
    //                            case 6: eMass = si; break;
    //                            case 10: sLevel = si; break;
    //                            case 11: eLevel = si; break;
    //                        }

    //                        if (j == 11) {
    //                            break;
    //                        }
    //                    }

    //                    transfer.VolumeStart = sVol;
    //                    transfer.VolumeEnd = eVol;
    //                    transfer.MassStart = sMass;
    //                    transfer.MassEnd = eMass;
    //                    transfer.LevelStart = sLevel;
    //                    transfer.LevelEnd = eLevel;

    //                    pos++;

    //                    tankTransfers.Add(transfer);
    //                }
    //                pos++; 
    //            }
    //        } catch (Exception e) {
    //            Logger.Error(e);
    //            return tankTransfers;
    //        }

    //        return tankTransfers;
    //    }

    //    #endregion

    //    #region utils

    //    private DateTime? fillDTVR(int nom)
    //    {
    //        try {
    //            var yy = GetValue(nom, 2);
    //            var mm = GetValue(nom + 2, 2);
    //            var dd = GetValue(nom + 4, 2);
    //            var hh = GetValue(nom + 6, 2);
    //            var mn = GetValue(nom + 8, 2);
    //            if (yy+2000 > DateTime.Now.Year || mm > 12 || dd > 31 || hh > 24 || mn > 60) {
    //                return null;
    //            } 
    //            return new DateTime(yy + 2000, mm, dd, hh, mn, 0, 0);
    //        } catch (Exception e) {
    //            Logger.Error($"error on fillDTVR {e.Message + e.StackTrace}");
    //            return null;
    //        }
    //    }

    //    decimal ToDecimal(int pos)
    //    {
    //        var raw = new byte[4];
    //        raw[3] = Convert.ToByte(((char)buf[pos]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 1]).ToString(CultureInfo.InvariantCulture), 16);
    //        raw[2] = Convert.ToByte(((char)buf[pos + 2]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 3]).ToString(CultureInfo.InvariantCulture), 16);
    //        raw[1] = Convert.ToByte(((char)buf[pos + 4]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 5]).ToString(CultureInfo.InvariantCulture), 16);
    //        raw[0] = Convert.ToByte(((char)buf[pos + 6]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 7]).ToString(CultureInfo.InvariantCulture), 16);
    //        var floatValue = BitConverter.ToSingle(raw, 0);
    //        return (decimal) floatValue;
    //    }

    //    #endregion
    //}
}
