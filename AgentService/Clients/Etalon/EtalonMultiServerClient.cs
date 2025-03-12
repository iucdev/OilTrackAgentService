using NLog;
using Service.Clients.Client;
using Service.Clients.RSMDB;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Clients.VR;
using Service.Common;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace Nitec.DCM.BusinessLogic.Etalon {
    //public class EtalonMultiServerClient : AMultiServerClient {
    //    private List<TankMeasurementData> _tankMeasurements = new List<TankMeasurementData>();
    //    private List<TankTransferData> _tankTransfers = new List<TankTransferData>();

    //    public EtalonMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
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

    //                foreach (var @object in ObjectSettings.Objects) { 
    //                    // Todo: send to apiIncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, task.Ticket);
    //                }

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
    //        try {
    //            Logger.Debug("Collect data");
    //            var tanksMeasurements = new List<TankMeasurements>();
    //            var tanksTransfers = new List<TankTransfers>();

    //            foreach (var source in ObjectSettings.Objects.First().ObjectSources) {
    //                if (int.TryParse(source.InternalId, out int nom))
    //                    if (Send231Wrapped(nom, ObjectSettings.Objects.First().ObjectId.ToString(), ObjectSettings.IpConnectionConfig.RSMDBConnectionString, ObjectSettings.IpConnectionConfig.DComPwd) <= 0)
    //                        Logger.Debug("F231 return 0");

    //                if (_tankMeasurements.Any()) {
    //                    tanksMeasurements.Add(new TankMeasurements()
    //                    {
    //                        TankId = source.ExternalId.Value,
    //                        Measurements = _tankMeasurements.SetEnums(source)
    //                    });
    //                }

    //                Thread.Sleep(500);

    //                if (Send215Wrapped(nom, ObjectSettings.Objects.First().ObjectId.ToString(), ObjectSettings.IpConnectionConfig.RSMDBConnectionString, ObjectSettings.IpConnectionConfig.DComPwd) <= 0)
    //                    Logger.Debug("F215 return 0");


    //                var transfersWithEndDate = _tankTransfers.Where(t => t.EndDate != DateTime.MinValue).ToList();
    //                if (transfersWithEndDate.Any()) {
    //                    tanksTransfers.Add(new TankTransfers
    //                    {
    //                        TankId = source.ExternalId.Value,
    //                        Transfers = transfersWithEndDate.SetEnums(source)
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
    //        if (!string.IsNullOrEmpty(ObjectSettings.IpConnectionConfig.RSMDBConnectionString)) {
    //            if (bytesRec < 44) {
    //                Logger.Error("Получено необычно короткое сообщение");
    //                return false;
    //            }

    //            if (!verifyHmacSha256(ObjectSettings.IpConnectionConfig.RSMDBConnectionString, buf.Skip(5).Take(bytesRec - 42).ToArray(), buf.Skip(bytesRec - 37).Take(32).ToArray())) {
    //                Logger.Error("Bad sign");
    //                return false;
    //            }

    //            if (buf[5] == 0x02)// todo условие убрать???
    //            {
    //                var length = bytesRec;
    //                var arr = buf.Skip(7).Take(length - 44).ToArray();
    //                Array.Clear(buf, 0, buf.Length);
    //                arr.CopyTo(buf, 0);
    //                bytesRec = arr.Length;

    //                if (bytesRec == 0) return false;
    //            }
    //        }

    //        if (buf[bytesRec - 1] == 0x03) return true;
    //        Logger.Error("Error check summ");
    //        return false;
    //    }

    //    #region Commands

    //    private int Send231Wrapped(int nom, string azsCode, string apiKey, string pass)
    //    {
    //        LastCom = VrCMD.CMD231;
    //        CmdDelay = 2000;
    //        int cmdBufLen;
    //        if (!string.IsNullOrEmpty(pass)) {
    //            cmdBuf[0] = VrCMD.SOH;
    //            cmdBuf[1] = (byte)(48 + pass[5]);
    //            cmdBuf[2] = (byte)(48 + pass[4]);
    //            cmdBuf[3] = (byte)(48 + pass[3]);
    //            cmdBuf[4] = (byte)(48 + pass[2]);
    //            cmdBuf[5] = (byte)(48 + pass[1]);
    //            cmdBuf[6] = (byte)(48 + pass[0]);
    //            cmdBuf[7] = 105;
    //            cmdBuf[8] = 48 + 2;
    //            cmdBuf[9] = 48 + 3;
    //            cmdBuf[10] = 48 + 1;
    //            cmdBuf[11] = 48 + 0;
    //            cmdBuf[12] = (byte)(48 + nom);
    //            cmdBufLen = 13;
    //        } else {
    //            cmdBuf[0] = VrCMD.SOH;
    //            cmdBuf[1] = 105;
    //            cmdBuf[2] = 48 + 2;
    //            cmdBuf[3] = 48 + 3;
    //            cmdBuf[4] = 48 + 1;
    //            cmdBuf[5] = 48 + 0;
    //            cmdBuf[6] = (byte)(48 + nom);
    //            cmdBufLen = 7;
    //        }

    //        Posinbuf = 1;
    //        Logger.Info("F231");
    //        //#BEG#[2][2][байты протокола уровнемера][sign32]#END#
    //        byte[] BEGIN = Encoding.UTF8.GetBytes("#BEG#");
    //        byte[] END = Encoding.UTF8.GetBytes("#END#");
    //        byte[] command = { 0x02 };
    //        byte[] azs = { byte.Parse(azsCode) };
    //        byte[] commandLevelWrap = new byte[] { 0x01 }.Concat(cmdBuf.Take(cmdBufLen)).Concat(new byte[] { 0x03 }).ToArray();
    //        byte[] message = command.Concat(azs).Concat(commandLevelWrap).ToArray();
    //        byte[] sign = signHmacSha256(apiKey, message);

    //        cmdBuf = BEGIN.Concat(message).Concat(sign).Concat(END).ToArray();

    //        return SendBuf(cmdBuf.Length);
    //    }

    //    /// <summary>
    //    /// In-Tank Mass/Density Delivery Report
    //    /// </summary>
    //    /// <param name="nom"></param>
    //    /// <returns></returns>
    //    private int Send215Wrapped(int nom, string azsCode, string apiKey, string pass)
    //    {
    //        LastCom = VrCMD.CMD215;
    //        CmdDelay = 8000;//2000;
    //        int cmdBufLen;
    //        if (!string.IsNullOrEmpty(pass)) {
    //            cmdBuf[0] = VrCMD.SOH;
    //            cmdBuf[1] = (byte)(48 + pass[5]);
    //            cmdBuf[2] = (byte)(48 + pass[4]);
    //            cmdBuf[3] = (byte)(48 + pass[3]);
    //            cmdBuf[4] = (byte)(48 + pass[2]);
    //            cmdBuf[5] = (byte)(48 + pass[1]);
    //            cmdBuf[6] = (byte)(48 + pass[0]);
    //            cmdBuf[7] = 105;
    //            cmdBuf[8] = 48 + 2;
    //            cmdBuf[9] = 48 + 1;
    //            cmdBuf[10] = 48 + 5;
    //            cmdBuf[11] = 48 + 0;
    //            cmdBuf[12] = (byte)(48 + nom);
    //            cmdBufLen = 13;
    //        } else {
    //            cmdBuf[0] = VrCMD.SOH;
    //            cmdBuf[1] = 105;
    //            cmdBuf[2] = 48 + 2;
    //            cmdBuf[3] = 48 + 1;
    //            cmdBuf[4] = 48 + 5;
    //            cmdBuf[5] = 48 + 0;
    //            cmdBuf[6] = (byte)(48 + nom);
    //            cmdBufLen = 7;
    //        }
    //        Posinbuf = 1;
    //        Logger.Info("F215");

    //        //#BEG#[2][2][байты протокола уровнемера][sign32]#END#
    //        byte[] BEGIN = Encoding.UTF8.GetBytes("#BEG#");
    //        byte[] END = Encoding.UTF8.GetBytes("#END#");
    //        byte[] command = { 0x02 };
    //        byte[] azs = { byte.Parse(azsCode) };
    //        byte[] commandLevelWrap = new byte[] { 0x01 }.Concat(cmdBuf.Take(cmdBufLen)).Concat(new byte[] { 0x03 }).ToArray();
    //        byte[] message = command.Concat(azs).Concat(commandLevelWrap).ToArray();
    //        byte[] sign = signHmacSha256(apiKey, message);

    //        cmdBuf = BEGIN.Concat(message).Concat(sign).Concat(END).ToArray();

    //        return SendBuf(cmdBuf.Length);
    //    }

    //    #endregion

    //    #region Fillers

    //    protected override bool Fill(int bytesRec)
    //    {
    //        var result = false;

    //        switch (LastCom) {
    //            case VrCMD.CMD231: {
    //                    _tankMeasurements = Fill231();
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

    //    private List<TankMeasurementData> Fill231()
    //    {
    //        var tankMeasurements = new List<TankMeasurementData>();

    //        try {
    //            if (!((buf[0] == 01) && (buf[1] == 0x69) && (buf[2] == 0x32) && (buf[3] == 0x33) && (buf[4] == 0x31))) {
    //                Logger.Error("Error F231");
    //                return tankMeasurements;
    //            }

    //            var dt = fillDTVR(7);
    //            var pos = 17;

    //            while (buf[pos] != 0x26) {
    //                var measurement = new TankMeasurementData {
    //                    MeasurementDate = dt.Value
    //                };

    //                var n = GetValue(pos + 7, 2);
    //                pos += 9;

    //                //Logger.InfoFormat("N={0} Pos={1}", n, pos);
    //                for (var i = 0; i < n; i++) {
    //                    float si;
    //                    try {
    //                        si = Tofloat(pos);
    //                    } catch (Exception e) {
    //                        Logger.Error(e);
    //                        return tankMeasurements;
    //                    }

    //                    pos += 8;
    //                    var result = decimal.Parse(si.ToString("F4").Replace(',', '.'));

    //                    switch (i) {
    //                        case 0: measurement.Volume = result; break;
    //                        case 3: measurement.Level = result; break;
    //                        case 5: measurement.Temperature = result; break;
    //                        case 10: measurement.Mass = result; break;
    //                        case 11: measurement.Density = result; break;
    //                    }
    //                }

    //                tankMeasurements.Add(measurement);
    //            }
    //        } catch (Exception e) {
    //            Logger.Error(e);
    //        }

    //        return tankMeasurements;
    //    }

    //    private List<TankTransferData> Fill215()
    //    {
    //        var label = new[]
    //        {
    //            "SVol", "SMass", "SDens", "SWat", "STemp", "EVol", "EMass", "EDens", "EWat", "ETemp", "SHeight", "EHeight", "STcDens",
    //            "ETcDens", "STcVol", "ETcVol", "STcOffs", "ETcOff"
    //        };

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

    //                    transfer.StartDate = dt1 != null ? dt1.Value : DateTime.MinValue;
    //                    transfer.EndDate = dt2 != null ? dt2.Value : DateTime.MinValue;

    //                    var line = string.Format("{0,-11}| {1,-10}", transfer.StartDate, transfer.EndDate);

    //                    float sVol = 0, eVol = 0, sMass = 0, eMass = 0, sLevel = 0, eLevel = 0;

    //                    var nn = GetValue(pos, 2);
    //                    nn = int.Parse(nn.ToString(CultureInfo.InvariantCulture), NumberStyles.HexNumber);
    //                    pos += 2;

    //                    for (var j = 0; j < nn; j++) {
    //                        float si;
    //                        try {
    //                            si = Tofloat(pos);
    //                        } catch (Exception e) {
    //                            Logger.Error(e);
    //                            return tankTransfers;
    //                        }
    //                        pos += 8;
    //                        line += string.Format("| {0,-10}", si);

    //                        switch (j) {
    //                            case 0: sVol = si; break;
    //                            case 1: sMass = si; break;
    //                            case 5: eVol = si; break;
    //                            case 6: eMass = si; break;
    //                            case 11: sLevel = si; break;
    //                            case 12: eLevel = si; break;
    //                        }
    //                    }

    //                    transfer.VolumeStart = (decimal)sVol;
    //                    transfer.VolumeEnd = (decimal)eVol;
    //                    transfer.MassStart = (decimal)(sMass);
    //                    transfer.MassEnd = (decimal)(eMass);
    //                    transfer.LevelStart = (decimal)(sLevel);
    //                    transfer.LevelEnd = (decimal)(eLevel);

    //                    line += string.Format("| {0,5}", buf[pos]);

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
    //        int mm, dd, yy, hh, mn;
    //        DateTime? result = null;
    //        try {
    //            yy = GetValue(nom, 2);
    //            mm = GetValue(nom + 2, 2);
    //            dd = GetValue(nom + 4, 2);
    //            hh = GetValue(nom + 6, 2);
    //            mn = GetValue(nom + 8, 2);
    //            result = new DateTime(yy + 2000, mm, dd, hh, mn, 0, 0);
    //        } catch (Exception e) {
    //            Logger.Error($"error on fillDTVR {e}");
    //        }
    //        return result;
    //    }

    //    float Tofloat(int pos)
    //    {
    //        var raw = new byte[4];
    //        raw[3] = Convert.ToByte(((char)buf[pos]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 1]).ToString(CultureInfo.InvariantCulture), 16);
    //        raw[2] = Convert.ToByte(((char)buf[pos + 2]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 3]).ToString(CultureInfo.InvariantCulture), 16);
    //        raw[1] = Convert.ToByte(((char)buf[pos + 4]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 5]).ToString(CultureInfo.InvariantCulture), 16);
    //        raw[0] = Convert.ToByte(((char)buf[pos + 6]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 7]).ToString(CultureInfo.InvariantCulture), 16);

    //        return BitConverter.ToSingle(raw, 0);
    //    }

    //    private static byte[] signHmacSha256(string key, byte[] message)
    //    {
    //        var encoding = new ASCIIEncoding();
    //        var keyByte = encoding.GetBytes(key ?? "");
    //        using (var hmacsha256 = new HMACSHA256(keyByte)) {
    //            return hmacsha256.ComputeHash(message);
    //        }
    //    }

    //    private static bool verifyHmacSha256(string key, byte[] message, byte[] sign)
    //    {
    //        var sign0 = signHmacSha256(key, message);

    //        if (sign.Length != sign0.Length)
    //            return false;

    //        for (var i = 0; i < sign0.Length; i++) {
    //            if (sign0[i] != sign[i])
    //                return false;
    //        }

    //        return true;
    //    }
    //    #endregion
    //}
}
