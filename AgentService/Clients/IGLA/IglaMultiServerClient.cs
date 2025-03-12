using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Common;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Service.Clients.IGLA {
    //public class IglaMultiServerClient : AMultiServerClient
    //{
    //    public IglaMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
    //    {
    //        ObjectSettings = objectSettings;
    //        Worker = worker;
    //        Init();
    //    }

    //    private new void Init()
    //    {
    //        Logger.Debug("Start Init");
    //        base.Init();
    //        try
    //        {
    //            var counter = 0;
    //            while (!Connect())
    //            {
    //                if (Worker.CancellationPending) return;

    //                // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, Task.ObjectId);

    //                counter++;
    //                Logger.Info("Try connect to server {0}", counter);
    //                Thread.Sleep(60000);
    //            }
    //        }
    //        catch (Exception e)
    //        {
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
    //            Logger.Debug("CollectData");
    //            StartMeasureCMD();
    //            var tanksMeasurements = new List<TankMeasurements>();
    //            foreach (var source in ObjectSettings.Objects.First().ObjectSources.Where(t => t.TankMeasurementParams != null)) {
    //                var tankMeasurements = new TankMeasurements { TankId = source.ExternalId.Value };
    //                try {
    //                    if (FullInfoCMD(int.Parse(source.InternalId) - 1) <= 0) {
    //                        Logger.Info("FullInfoCMD return 0");
    //                    }
    //                    tankMeasurements.Measurements = new[] {
    //                        new TankMeasurementData {
    //                            MeasurementDate = DateTime.Now, //Т.к. протоколом не предусмотрены метки и ЦБУ отвечает практически моментально, то дату можем ставить сами
    //                            Mass = decimal.Parse(ToSafeString(Tofloat6(55)), CultureInfo.InvariantCulture),
    //                            Volume = decimal.Parse(ToSafeString(Tofloat6(43)), CultureInfo.InvariantCulture),
    //                            Density = decimal.Parse(ToSafeString(Tofloat4(35)), CultureInfo.InvariantCulture),
    //                            Temperature = decimal.Parse(ToSafeString(Tofloat4(27)), CultureInfo.InvariantCulture),
    //                            Level = decimal.Parse(ToSafeString(Tofloat4(11)), CultureInfo.InvariantCulture)
    //                        }
    //                    }.SetEnums(source);

    //                    tanksMeasurements.Add(tankMeasurements);
    //                } catch (Exception e) {
    //                    Logger.Error($"Error on FullInfoCMD Message-{e.Message + e.StackTrace}");
    //                }
    //            }
    //            QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
    //        } catch (Exception ex) { 
    //            Logger.Error($"Error on collect data {ex.Message + ex.StackTrace}");
    //        }
    //    }

    //    protected override bool CheckRb(int bytesRec)
    //    {
    //        if (buf[0] != 0x40)
    //        {
    //            Logger.Error("Response bytes is incorrect");
    //            return false;
    //        }

    //        if (!CrcCheck(bytesRec - 4))
    //        {
    //            Logger.Error("Error CRC!");
    //            return false;
    //        }
    //        return true;
    //    }

    //    protected override bool SkipCmd()
    //    {
    //        return LastCom == IglaCmd.StartMeasureCMD;
    //    }

    //    protected override bool Fill(int bytesRec)
    //    {
    //        return true;
    //    }


    //    #region Commands

    //    /// <summary>
    //    /// Version - 0x01
    //    /// </summary>
    //    /// <returns></returns>
    //    private int VersionCMD()
    //    {
    //        LastCom = IglaCmd.Version;
    //        CmdDelay = 2000;

    //        cmdBuf = new byte[11];
    //        cmdBuf[0] = IglaCmd.STX;
    //        cmdBuf[1] = 0x30; //ID H
    //        cmdBuf[2] = 0x30; //ID L
    //        cmdBuf[3] = IglaCmd.Version & 0xF0 >> 2; //TAG H
    //        cmdBuf[4] = IglaCmd.Version & 0x0F;      //TAG L
    //        cmdBuf[5] = 0x30; //LEN H
    //        cmdBuf[6] = 0x30; //LEN L
    //        var crc = (byte)(cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]);
    //        cmdBuf[7] = (byte)(crc & 0xF0 >> 2); //CRC H
    //        cmdBuf[8] = (byte)(crc & 0x0F);      //CRC L
    //        cmdBuf[9] = IglaCmd.ETX; //Terminator T3000 
    //        cmdBuf[10] = IglaCmd.ETX2;

    //        return SendBuf(11);
    //    }

    //    /// <summary>
    //    /// EX1 - 0x8A 
    //    /// Запустить преобразование (начать измерение) - широковещательная команда, гарантирует 10 с. тишины
    //    /// </summary>
    //    /// <returns></returns>
    //    private int StartMeasureCMD()
    //    {
    //        LastCom = IglaCmd.StartMeasureCMD;
    //        CmdDelay = 12000;

    //        cmdBuf = new byte[11];
    //        cmdBuf[0] = IglaCmd.STX;
    //        cmdBuf[1] = 0x46; //ID H
    //        cmdBuf[2] = 0x30; //ID L
    //        cmdBuf[3] = IglaCmd.StartMeasureCMD & 0xF0 >> 2; //TAG H
    //        cmdBuf[4] = IglaCmd.StartMeasureCMD & 0x0F;      //TAG L
    //        cmdBuf[5] = 0x30; //LEN H
    //        cmdBuf[6] = 0x30; //LEN L
    //        var crc = (cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]).ToString("X").PadLeft(2, '0');
    //        cmdBuf[7] = (byte)(0x30 + int.Parse(crc.Substring(0, 1))); //CRC H
    //        cmdBuf[8] = (byte)(0x30 + int.Parse(crc.Substring(1, 1))); //CRC L
    //        cmdBuf[9] = IglaCmd.ETX; //Terminator T3000 
    //        cmdBuf[10] = IglaCmd.ETX2;

    //        return SendBuf(11);
    //    }

    //    /// <summary>
    //    /// Вернуть все - 0x1С
    //    /// </summary>
    //    /// <param name="num"></param>
    //    /// <returns></returns>
    //    public int FullInfoCMD(int num)
    //    {
    //        var ids = num.ToString("##").PadLeft(2, '0');

    //        LastCom = IglaCmd.FullInfoCMD;
    //        CmdDelay = 2000;

    //        cmdBuf = new byte[11];
    //        cmdBuf[0] = IglaCmd.STX;
    //        cmdBuf[1] = (byte)(0x30 + int.Parse(ids.Substring(0, 1))); //ID H
    //        cmdBuf[2] = (byte)(0x30 + int.Parse(ids.Substring(1, 1))); //ID L
    //        cmdBuf[3] = 0x31; //TAG H
    //        cmdBuf[4] = 0x43; //TAG L
    //        cmdBuf[5] = 0x30; //LEN H
    //        cmdBuf[6] = 0x30; //LEN L
    //        var crc = (cmdBuf[0] ^ cmdBuf[1] ^ cmdBuf[2] ^ cmdBuf[3] ^ cmdBuf[4] ^ cmdBuf[5] ^ cmdBuf[6]).ToString("X").PadLeft(2, '0');
    //        cmdBuf[7] = (byte)(0x30 + int.Parse(crc.Substring(0, 1))); //CRC H
    //        cmdBuf[8] = (byte)(0x30 + int.Parse(crc.Substring(1, 1))); //CRC L
    //        cmdBuf[9] = IglaCmd.ETX;
    //        cmdBuf[10] = IglaCmd.ETX2;

    //        return SendBuf(11);
    //    }

    //    #endregion

    //    #region utils

    //    private bool CrcCheck(int pos)
    //    {
    //        var crc = buf[0];

    //        for (var i = 1; i < pos; i++)
    //            crc = (byte)(crc ^ buf[i]);

    //        return crc == NibbleToByte(pos);
    //    }

    //    private byte NibbleToByte(int pos)
    //    {
    //        return Convert.ToByte(((char)buf[pos]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 1]).ToString(CultureInfo.InvariantCulture), 16);
    //    }

    //    private float? Tofloat4(int pos)
    //    {
    //        float? result = null;
    //        try
    //        {
    //            result = Convert.ToSingle(string.Format("{0}.{1}",
    //                Convert.ToInt32(((char)buf[pos]).ToString() + (char)buf[pos + 1] + (char)buf[pos + 2] + (char)buf[pos + 3], 16),
    //                Convert.ToInt32(((char)buf[pos + 4]).ToString() + (char)buf[pos + 5], 16)), CultureInfo.InvariantCulture);
    //        }
    //        catch (Exception e)
    //        {
    //            Logger.Error("Error on Tofloat4 pos={0} Message-{1}", pos, e);
    //        }
    //        return result;

    //    }

    //    private float? Tofloat6(int pos)
    //    {
    //        float? result = null;

    //        try
    //        {
    //            result = Convert.ToSingle(string.Format("{0}.{1}",
    //           Convert.ToInt32(((char)buf[pos]).ToString() + (char)buf[pos + 1] + (char)buf[pos + 2] + (char)buf[pos + 3]
    //           + (char)buf[pos + 4] + (char)buf[pos + 5] + (char)buf[pos + 6] + (char)buf[pos + 7], 16),
    //           Convert.ToInt32(((char)buf[pos + 8]).ToString() + (char)buf[pos + 9], 16)), CultureInfo.InvariantCulture);
    //        }
    //        catch (Exception e)
    //        {
    //            Logger.Error("Error on Tofloat6 pos={0} Message-{1}", pos, e);
    //        }

    //        return result;
    //    }

    //    private int? GetLen(int pos)
    //    {
    //        int? len = null;
    //        try
    //        {
    //            len = Convert.ToInt32(((char)buf[pos]).ToString() + (char)buf[pos + 1], 16);
    //        }
    //        catch (Exception e)
    //        {
    //            Logger.Error("Error on GetLen pos={0} Message-{1}", pos, e);
    //        }
    //        return len;
    //    }

    //    private string ToSafeString(float? val)
    //    {
    //        return val == null ? string.Empty : val.Value.ToString(CultureInfo.InvariantCulture).Replace("\0", string.Empty);
    //    }

    //    #endregion
    //}
}
