using Service.Clients.Client;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Common;
using Service.Enums;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Service.Clients.ModBus {
    //public class StrunaMultiServerClient : AMultiServerClient
    //{
    //    public StrunaMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
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
    //        Logger.Debug("Call CollectData");

    //        var tanksMeasurements = new List<TankMeasurements>();
    //        foreach(var objectItem in ObjectSettings.Objects) {
    //            foreach(var source in objectItem.ObjectSources.Where(t => t.TankMeasurementParams != null)) {
    //                var tankMeasurements = new TankMeasurements() { TankId = source.ExternalId.Value };
    //                var measurement = new TankMeasurementData();
    //                try {
    //                    SetChannel(int.Parse(source.InternalId) - 1);
    //                    GetMaskCmd();
    //                    if (FullInfoCMD() <= 0)
    //                        Logger.Info("FullInfoCMD return 0");

    //                    if (buf[8] == 0)
    //                        measurement.Level = decimal.Parse(ToSafeString(Tofloat(3)), CultureInfo.InvariantCulture);
    //                    if (buf[14] == 0)
    //                        measurement.Mass = decimal.Parse(ToSafeString(Tofloat(9)), CultureInfo.InvariantCulture);
    //                    if (buf[20] == 0)
    //                        measurement.Volume = decimal.Parse(ToSafeString(Tofloat(15)), CultureInfo.InvariantCulture);
    //                    if (buf[26] == 0)
    //                        measurement.Density = decimal.Parse(ToSafeString(Tofloat(21) * 1000), CultureInfo.InvariantCulture);
    //                    if (buf[32] == 0)
    //                        measurement.Temperature = decimal.Parse(ToSafeString(Tofloat(27)), CultureInfo.InvariantCulture);

    //                    measurement.MeasurementDate = DateTime.Now; //Т.к. протоколом не предусмотрены метки и ЦБУ отвечает практически моментально то дату можем ставить сами.

    //                    tankMeasurements.Measurements = new[] { measurement }.SetEnums(source);
    //                    tanksMeasurements.Add(tankMeasurements);
    //                } catch (Exception e) {
    //                    Logger.Error($"Error on FullInfoCMD Message-{e.Message + e.StackTrace}");
    //                }
    //            }
    //        }
    //        try
    //        {
    //            QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Error($"SendSourceValues error {ex.Message + ex.StackTrace}");
    //        }
    //    }


    //    protected override bool CheckRb(int bytesRec)
    //    {
    //        if (buf[0] != StrunaCmd.Sl)
    //        {
    //            Logger.Error("Responce bytes is incorrect");
    //            return false;
    //        }

    //        if (!CrcCheck(bytesRec - 2))
    //        {
    //            Logger.Error("Error CRC!");
    //            return false;
    //        }

    //        if ((buf[1] & 0x80) > 0)
    //        {
    //            Logger.Error("ModBus Exception - {0}", MbErrorsHelper.GetMessage(((mbErrors)buf[2])));
    //            return false;
    //        }

    //        return true;
    //    }

    //    protected override bool Fill(int bytesRec)
    //    {
    //        var result = false;

    //        switch (LastCom)
    //        {
    //            case StrunaCmd.GetMask:
    //                {
    //                    result = getMaskFill();
    //                    break;
    //                }
    //            case StrunaCmd.SetChannel:
    //                {
    //                    result = bytesRec > 0;
    //                    break;
    //                }
    //            case StrunaCmd.GetAll:
    //                {
    //                    result = bytesRec > 0;
    //                    break;
    //                }
    //        }
    //        return result;
    //    }

    //    #region Commands
    //    private int SetChannel(long chanNumber, int trunkNumber = 0)
    //    {
    //        //var cmd = string.Format("50 06 00 00 {0:2} {1:2}", trunkNumber, chanNumber);
    //        LastCom = StrunaCmd.SetChannel;
    //        CmdDelay = 1000;

    //        cmdBuf = new byte[8];
    //        cmdBuf[0] = StrunaCmd.Sl;
    //        cmdBuf[1] = StrunaCmd.SlWrite;
    //        cmdBuf[2] = 0;
    //        cmdBuf[3] = 0;
    //        cmdBuf[4] = (byte)(trunkNumber);
    //        cmdBuf[5] = (byte)(chanNumber);
    //        var crc = StrunaHelper.ModbusCrc16(cmdBuf, 6);
    //        var crcBts = new[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
    //        cmdBuf[6] = crcBts[0];
    //        cmdBuf[7] = crcBts[1];

    //        return SendBuf(8);
    //    }
    //    private int GetMaskCmd()
    //    {
    //        //"50 04 00 00 00 03"; // чтение ТОД, масок

    //        LastCom = StrunaCmd.GetMask;
    //        CmdDelay = 1000;

    //        cmdBuf = new byte[8];
    //        cmdBuf[0] = StrunaCmd.Sl;
    //        cmdBuf[1] = StrunaCmd.SlRead;
    //        cmdBuf[2] = 0;
    //        cmdBuf[3] = 0;
    //        cmdBuf[4] = 0;
    //        cmdBuf[5] = 3;
    //        var crc = StrunaHelper.ModbusCrc16(cmdBuf, 6);
    //        var crcBts = new[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
    //        cmdBuf[6] = crcBts[0];
    //        cmdBuf[7] = crcBts[1];

    //        return SendBuf(8);
    //    }
    //    private int FullInfoCMD()
    //    {
    //        //"50 04 00 03 00 2A"; // чтение 13 регистров

    //        LastCom = StrunaCmd.GetAll;
    //        CmdDelay = 1000;

    //        cmdBuf = new byte[8];
    //        cmdBuf[0] = StrunaCmd.Sl;
    //        cmdBuf[1] = StrunaCmd.SlRead;
    //        cmdBuf[2] = 0x00;
    //        cmdBuf[3] = 0x03;
    //        cmdBuf[4] = 0x00;
    //        cmdBuf[5] = 0x2A;
    //        var crc = StrunaHelper.ModbusCrc16(cmdBuf, 6);
    //        var crcBts = new[] { (byte)(crc & 0xFF), (byte)(crc >> 8) };
    //        cmdBuf[6] = crcBts[0];
    //        cmdBuf[7] = crcBts[1];

    //        return SendBuf(8);
    //    }
    //    #endregion

    //    #region utils

    //    private bool CrcCheck(int pos)
    //    {
    //        var bufCrc = StrunaHelper.ModbusCrc16(buf, pos);
    //        return bufCrc != BitConverter.ToUInt16(new byte[2] { buf[pos + 2], buf[pos + 1] }, 0);
    //    }

    //    private byte NibbleToByte(int pos)
    //    {
    //        return Convert.ToByte(((char)buf[pos]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 1]).ToString(CultureInfo.InvariantCulture), 16);
    //    }

    //    float Tofloat(int pos)
    //    {
    //        var raw = new byte[4] { buf[pos + 1], buf[pos], buf[pos + 3], buf[pos + 2] };
    //        return BitConverter.ToSingle(raw, 0);
    //    }

    //    private string ToSafeString(float? val)
    //    {
    //        return val == null ? string.Empty : val.Value.ToString(CultureInfo.InvariantCulture).Replace("\0", string.Empty);
    //    }

    //    private bool getMaskFill()
    //    {
    //        try
    //        {
    //            var todType = buf[3];
    //            var num = buf[4] + 1;

    //            Logger.Info("{0} Channel={1}", todType == 0 ? "PPP" : "others", num);

    //            //var raw = new byte[4] { buf[6], buf[5], buf[8], 0 };
    //            //var k = BitConverter.ToInt32(raw, 0);
    //            //var ddd = (int)PppMask.H & k;

    //            var mask = string.Join("",
    //                new string(Convert.ToString(buf[6], 2).Reverse().ToArray()),
    //                new string(Convert.ToString(buf[5], 2).Reverse().ToArray()),
    //                new string(Convert.ToString(buf[8], 2).Reverse().ToArray())
    //                ).PadRight(24, '0');

    //            for (var i = 4; i < mask.Length; i += 4)
    //                mask = mask.Insert(i++, " ");

    //            var cnt = buf[7];
    //            Logger.Info("Parameter Mask:{0}", mask);
    //            Logger.Info("Parameter count={0}", cnt);
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Error(ex);
    //            return false;
    //        }

    //        return true;
    //    }

    //    private enum PppMask
    //    {
    //        /// <summary>
    //        /// Средняя плотность продукта, Рср
    //        /// </summary>
    //        Psr = 1,
    //        /// <summary>
    //        /// Плотность поверхностного слоя продукта, Рар
    //        /// </summary>
    //        Par = 2,
    //        /// <summary>
    //        /// Плотность паровой фазы продукта, Рпф 
    //        /// </summary>
    //        Ppf = 4,
    //        /// <summary>
    //        /// Средняя температура продукта, Тср
    //        /// </summary>
    //        Tsr = 8,
    //        /// <summary>
    //        /// Температура поверхностного слоя продукта, Тар 
    //        /// </summary>
    //        Tar = 16,
    //        /// <summary>
    //        /// Средняя температура паровой фазы продукта, Тпф 
    //        /// </summary>
    //        Tpf = 32,
    //        /// <summary>
    //        /// Уровень продукта, H 
    //        /// </summary>
    //        H = 64,
    //        /// <summary>
    //        /// Объем продукта, V
    //        /// </summary>
    //        V = 128,
    //        /// <summary>
    //        /// Масса продукта, M
    //        /// </summary>
    //        M = 256,
    //        /// <summary>
    //        /// Уровень подтоварной воды, Нв
    //        /// </summary>
    //        Hw = 512,
    //        /// <summary>
    //        /// Давление паровой фазы, Дпф
    //        /// </summary>
    //        Dpf = 1024,
    //        /// <summary>
    //        /// Максимальный объем продукта , Vмакс
    //        /// </summary>
    //        Vmax = 2048,
    //        /// <summary>
    //        /// Уровень ДУТ, ДУТ
    //        /// </summary>
    //        Dut = 4096,
    //        /// <summary>
    //        /// Резерв 
    //        /// </summary>
    //        Reserv1 = 8192,
    //        /// <summary>
    //        /// Резерв 
    //        /// </summary>
    //        Reserv2 = 16384,
    //        /// <summary>
    //        /// Резерв 
    //        /// </summary>
    //        Reserv3 = 32768
    //    }
    //    #endregion
    //}
}
