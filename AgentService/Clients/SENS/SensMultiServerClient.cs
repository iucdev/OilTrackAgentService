using Service.Clients.Client;
using Service.Clients.ModBus;
using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Common;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Service.Clients.SENS {
    //public class SensMultiServerClient : AMultiServerClient
    //{
    //    public SensMultiServerClient(ObjectSettings objectSettings, NamedBackgroundWorker worker)
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

    //            if (ObjectSettings.Objects == null || !ObjectSettings.Objects.Any())
    //                throw new Exception("No any sources in task! Exit");
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
    //        Logger.Debug("Collect data called");
    //        try {

    //            var tanksMeasurements = new List<TankMeasurements>();
    //            foreach (var item in ObjectSettings.Objects) {
    //                foreach (var source in item.ObjectSources.Where(t => t.TankMeasurementParams != null)) {
    //                    if (string.IsNullOrEmpty(source.InternalId)) {
    //                        Logger.Info($"Can't find InternalId for source.Id {source.ExternalId.Value}");
    //                        continue;
    //                    }

    //                    if (string.IsNullOrEmpty(source.OilProductType.Value.ToString())) {
    //                        Logger.Info($"Can't find OilProductType for source.Id {source.ExternalId.Value}");
    //                        continue;
    //                    }

    //                    var tankMeasurements = new TankMeasurements() { TankId = source.ExternalId.Value };

    //                    try {
    //                        var measurement = new TankMeasurementData();
    //                        if (int.TryParse(source.InternalId, out int nom))
    //                            if (ReadSpecParamsCMD((byte)nom) <= 0) {
    //                                Logger.Info("ReadSpecParamsCMD return 0");
    //                                continue;
    //                            }

    //                        var procent = string.Empty;

    //                        if (buf[4] == 1)
    //                            measurement.Level = decimal.Parse(ToSafeString(Tofloat(5)), CultureInfo.InvariantCulture);
    //                        if (buf[8] == 2)
    //                            measurement.Temperature = decimal.Parse(ToSafeString(Tofloat(9)), CultureInfo.InvariantCulture);
    //                        if (buf[12] == 3)
    //                            procent = ToSafeString(Tofloat(13));
    //                        if (buf[16] == 4)
    //                            measurement.Volume = decimal.Parse(ToSafeString(Tofloat(17)), CultureInfo.InvariantCulture);
    //                        if (buf[20] == 5)
    //                            measurement.Mass = decimal.Parse(ToSafeString(Tofloat(21)), CultureInfo.InvariantCulture);
    //                        if (buf[24] == 6)
    //                            measurement.Density = decimal.Parse(ToSafeString(Tofloat(25)), CultureInfo.InvariantCulture);

    //                        measurement.MeasurementDate = DateTime.Now; //Т.к. протоколом не предусмотрены метки и ЦБУ отвечает практически моментально то дату можем ставить сами.
    //                        tankMeasurements.Measurements = new[] { measurement }.SetEnums(source);

    //                        tanksMeasurements.Add(tankMeasurements);
    //                    } catch (Exception e) {
    //                        Logger.Error($"Error on FullInfoCMD Message-{e.Message + e.StackTrace}");
    //                    }
    //                }
    //            }

    //            try {
    //                QueueTaskService.Instance.SaveMeasurementsAsTask(tanksMeasurements.ToArray());
    //            } catch (Exception ex) {
    //                Logger.Error($"SendSourceValues error {ex.Message + ex.StackTrace}");
    //            }
    //        } catch (Exception e) { 
    //            Logger.Error($"Collect data error {e.Message + e.StackTrace}");
    //        }
    //    }

    //    protected override bool SkipCmd()
    //    {
    //        return false;
    //    }

    //    protected override bool CheckRb(int bytesRec)
    //    {
    //        if (bytesRec <= 0) return false;

    //        var i = 0;
    //        while (true)
    //        {
    //            if (buf.Skip(i).Take(4).Last() == (128 | SensCmd.SpecRead))
    //            {
    //                var j = i + 4;
    //                while (true)
    //                {
    //                    if (buf[j] == SensCmd.PRE || j == bytesRec) break;
    //                    j++;
    //                }

    //                buf = buf.Skip(i).Take(j).ToArray();
    //                bytesRec = j;
    //                break;
    //            }
    //            else
    //                i += 6;
    //        }

    //        if (!CrcCheck(bytesRec - 1))
    //        {
    //            Logger.Error("Error CRC!");
    //            return false;
    //        }
    //        return true;
    //    }

    //    protected override bool Fill(int bytesRec)
    //    {
    //        var result = false;

    //        switch (LastCom)
    //        {
    //            case SensCmd.SpecRead:
    //                {
    //                    result = bytesRec > 0;
    //                    break;
    //                }
    //        }
    //        return result;
    //    }

    //    #region Commands

    //    private int ReadSpecParamsCMD(byte Adr)
    //    {
    //        //01h Уровень основного поплавка, м 
    //        //02h Средняя температура, оС 
    //        //03h Заполнение, % 
    //        //04h Общий объем, м3 
    //        //05h Масса, т 
    //        //06h Плотность, т/м3 (только для ПМП-201) 
    //        //07h Объем основного продукта, м3 
    //        //08h Уровень подтоварной жидкости, м 

    //        //B5   01  06 2F  01 02 03 04 05 06   4B ; // чтение 6 регистров
    //        LastCom = SensCmd.SpecRead;
    //        CmdDelay = 1000;
    //        cmdBuf = new byte[11];
    //        cmdBuf[0] = SensCmd.PRE;
    //        cmdBuf[1] = Adr;
    //        cmdBuf[2] = 6;
    //        cmdBuf[3] = 32 | SensCmd.SpecRead;
    //        cmdBuf[4] = 0x01;
    //        cmdBuf[5] = 0x02;
    //        cmdBuf[6] = 0x03;
    //        cmdBuf[7] = 0x04;
    //        cmdBuf[8] = 0x05;
    //        cmdBuf[9] = 0x06;
    //        cmdBuf[10] = CRC(cmdBuf, cmdBuf.Length - 1);
    //        return SendBuf(cmdBuf.Length);
    //    }

    //    private int ReadMeasuredParamsCMD(byte Adr)
    //    {
    //        //B5   01  06 2F  01 02 03 04 05 06   4B ; // чтение 6 регистров
    //        LastCom = SensCmd.MeasuredParams;
    //        CmdDelay = 1000;
    //        cmdBuf = new byte[5];
    //        cmdBuf[0] = SensCmd.PRE;
    //        cmdBuf[1] = Adr;
    //        cmdBuf[2] = 0;
    //        cmdBuf[3] = 32 | SensCmd.MeasuredParams;
    //        cmdBuf[4] = CRC(cmdBuf, cmdBuf.Length - 1);
    //        return SendBuf(cmdBuf.Length);
    //    }

    //    #endregion

    //    #region utils

    //    private bool CrcCheck(int pos)
    //    {
    //        var bufCrc = CRC(buf, pos);
    //        return bufCrc == buf[pos];
    //    }

    //    // на входе указатель на пакет данных без CRC // и длина пакета без CRC 
    //    private byte CRC(byte[] buffer, int length)
    //    {
    //        byte result = 0;
    //        for (var i = 1; i < length; i++)
    //            result += buffer[i];
    //        return result;
    //    }


    //    private byte NibbleToByte(int pos)
    //    {
    //        return Convert.ToByte(((char)buf[pos]).ToString(CultureInfo.InvariantCulture) + ((char)buf[pos + 1]).ToString(CultureInfo.InvariantCulture), 16);
    //    }

    //    float Tofloat(int pos)
    //    {
    //        var raw = new byte[4] { 0, buf[pos], buf[pos + 1], buf[pos + 2] };
    //        return BitConverter.ToSingle(raw, 0);
    //    }

    //    private string ToSafeString(float? val)
    //    {
    //        return val == null ? string.Empty : val.Value.ToString(CultureInfo.InvariantCulture).Replace("\0", string.Empty);
    //    }

    //    #endregion
    //}
}
