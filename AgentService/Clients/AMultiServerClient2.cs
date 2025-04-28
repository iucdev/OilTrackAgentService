using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Enums;
using NLog;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using Service.LocalDb;
using AgentService.References;

namespace Service.Clients.Client {
    /// <summary>
    /// MultiServerClient
    /// </summary>
    public abstract class AMultiServerClient2 {
        internal static Logger Logger = LogManager.GetLogger("Reciever");
        internal ObjectSettings ObjectSettings;

        protected Timer _timer;

        private Socket _socket; //Socket
        private SerialPort _sPort; //COM Port

        private const int ReceiveBufferSize = 131072;//65536;
        internal byte[] buf = new byte[ReceiveBufferSize];
        internal byte[] cmdBuf = new byte[256];
        internal int LastCom;
        internal int CmdDelay = 1000;//задержка для команды в мс.
        internal byte Nom;
        internal int Posinbuf = 1;
        private int timeoutCntr = 0;

        protected void Init() {
            Logger.Debug("Base Init");
            try {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            } catch (Exception e) {
                Logger.Error(e);
            }
        }

        public NamedBackgroundWorker Worker { internal get; set; }

        protected void timerJob(object sender, ElapsedEventArgs e) {
            try {
                _timer.Enabled = false;

                if (Thread.CurrentThread.Name == null)
                    Thread.CurrentThread.Name = Worker.Name;

                if (Worker.CancellationPending) {
                    StopCollection();
                    return;
                }

                Logger.Info("Timer");

                if (GetConnectionState()) {
                    try {
                        CollectData();
                    } catch (Exception ex) {
                        Logger.Error("RefreshTags error {0}", ex.Message);
                    }

                    Thread.Sleep(2000);
                    _timer.Enabled = true;
                } else {
                    Logger.Error("Нет связи с сервером источника данных!");
                    StopCollection();

                    // В случае если все задачи будут только в _tasks
                    if (ObjectSettings != null)
                        foreach (var obj in ObjectSettings.Objects) {
                            // Todo: send to api IncidentUtil.PutIncident(IncidentKind.DeviceConnectionLoose, obj.ObjectId);
                        }

                    Thread.Sleep(30000);

                    if (!Worker.CancellationPending)
                        Reconnect();
                }

                if (!Worker.CancellationPending) return;
                StopCollection();
            } catch (Exception ex) {
                Logger.Error(ex);
                StartCollection();
            }
        }

        protected abstract void Reconnect();
        protected abstract void CollectData();

        public void StartCollection() {
            Logger.Debug("StartCollection");
            _timer = new Timer { Interval = 60000 };
            Logger.Debug($"Set update interval={_timer.Interval} ms.");
            _timer.Elapsed += timerJob;
            _timer.Start();
            timerJob(null, null);//force start
        }

        protected virtual bool StopCollection() {
            Logger.Info("Stop collection");
            _timer.Stop();

            // Закрываем сокет
            if (ObjectSettings.ConnectionType == ConnectionType.Ip && _socket != null && _socket.Connected) {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }

            if (ObjectSettings.ConnectionType == ConnectionType.Com && _sPort != null && _sPort.IsOpen) {
                _sPort.Close();
                //_sPort.Dispose();// Check!
            }

            return true;
        }

        /// <summary>
        /// Состояние связи с сервером
        /// </summary>
        /// <returns> связь есть-нет</returns>
        protected virtual bool GetConnectionState() {
            try {
                if (ObjectSettings.ConnectionType == ConnectionType.Com) return _sPort != null && _sPort.IsOpen;
                if (ObjectSettings.ConnectionType == ConnectionType.Ip) return _socket != null && _socket.Connected && SocketExtensions.IsConnected(_socket);
            } catch (Exception e) {
                Logger.Error(e);
                return false;
            }
            return false;
        }

        protected bool Connect() {
            switch (ObjectSettings.ConnectionType) {
                case ConnectionType.Com: return ConnectCom();// Коннект к удаленному устройству COM
                case ConnectionType.Ip: return ConnectIp();  // Коннект к удаленному устройству IP
                default: throw new Exception("Unknown ConnectionType!");
            }
        }

        private bool ConnectCom() {
            try {
                _sPort = new SerialPort {
                    PortName = ObjectSettings.ComConnectionConfig.PortName,
                    BaudRate = ObjectSettings.ComConnectionConfig.BaudRate != null ? ObjectSettings.ComConnectionConfig.BaudRate.Value : 0,
                    DataBits = ObjectSettings.ComConnectionConfig.DataBits != null ? ObjectSettings.ComConnectionConfig.DataBits.Value : 0,
                    Parity = ObjectSettings.ComConnectionConfig.Parity,
                    StopBits = ObjectSettings.ComConnectionConfig.StopBits,
                    ReadTimeout = ObjectSettings.ComConnectionConfig.ReadTimeout != null ? ObjectSettings.ComConnectionConfig.ReadTimeout.Value : 0,
                    WriteTimeout = ObjectSettings.ComConnectionConfig.WriteTimeout != null ? ObjectSettings.ComConnectionConfig.WriteTimeout.Value : 0,
                };
                // настройки порта
                _sPort.Open();
                Logger.Info("Client connected to {0}({2}) State {1}", _sPort.PortName, _sPort.IsOpen, string.Format("{0}, {1}, {2}, {3}, {4}, {5}", _sPort.BaudRate, _sPort.DataBits, _sPort.Parity, _sPort.StopBits, _sPort.ReadTimeout, _sPort.WriteTimeout));

            } catch (Exception e) {
                Logger.Error($"ERROR: невозможно открыть порт:{ObjectSettings.ComConnectionConfig.PortName} {e.Message}");
                return false;
            }
            return true;
        }

        private bool ConnectIp() {
            try {
                if (!IPAddress.TryParse(ObjectSettings.IpConnectionConfig.IpAddress, out IPAddress ipAddress)) {
                    Logger.Error("Invalid IP address: {0}", ObjectSettings.IpConnectionConfig.IpAddress);
                    return false;
                }

                if (!ObjectSettings.IpConnectionConfig.Port.HasValue ||
                    !Regex.IsMatch(ObjectSettings.IpConnectionConfig.Port.Value.ToString(), @"^\d{1,5}$", RegexOptions.None)) {
                    Logger.Error("Invalid port: {0}", ObjectSettings.IpConnectionConfig.Port);
                    return false;
                }

                int port = ObjectSettings.IpConnectionConfig.Port.Value;
                var ep = new IPEndPoint(ipAddress, port);

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                _socket.Connect(ep);
                Logger.Info($"Socket connected to {_socket.RemoteEndPoint}");
            } catch (ArgumentNullException ane) {
                Logger.Error("ArgumentNullException : {0}", ane);
                return false;
            } catch (SocketException se) {
                Logger.Error("{0}:{1} SocketException : {2}", ObjectSettings.IpConnectionConfig.IpAddress, ObjectSettings.IpConnectionConfig.Port.Value, se);
                return false;
            } catch (Exception e) {
                Logger.Error("Unexpected exception : {0}", e.Message + e.StackTrace);
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }

        protected int SendBuf(int len) {
            switch (ObjectSettings.ConnectionType) {
                case ConnectionType.Com: return SendBufCom(len); // COM
                case ConnectionType.Ip: return SendBufIp(len);  // IP
                default: throw new Exception("Unknown ConnectionType!");
            }
        }

        private int SendBufCom(int len) {
            Logger.Debug("Sent {0} byte->{1}", len, BitConverter.ToString(cmdBuf, 0, len).Replace('-', ' '));

            try {
                // Шлем запрос
                if (_sPort != null && _sPort.IsOpen)
                    _sPort.Write(cmdBuf, 0, len);
                else
                    return 0;
            } catch (TimeoutException ex) {
                Logger.Debug("Write Timeout");
                return 0;
            } catch (Exception) {
                return 0;
            }

            if (SkipCmd())
                return 0;

            Thread.Sleep(CmdDelay);

            var bytesRec = 0;
            try {
                bytesRec = _sPort.Read(buf, 0, ReceiveBufferSize);
            } catch (TimeoutException ex) {
                Logger.Debug("Read Timeout");
                return 0;
            } catch (Exception) {
                return 0;
            }
            Logger.Debug("Receive {0} byte <- {1}", bytesRec, BitConverter.ToString(buf, 0, bytesRec).Replace('-', ' '));

            if (!CheckRb(bytesRec))
                return 0;

            return !Fill(bytesRec) ? 0 : bytesRec;
        }

        private int SendBufIp(int len) {
            Logger.Debug("Sent {0} byte->{1}", len, BitConverter.ToString(cmdBuf, 0, len).Replace('-', ' '));

            _socket.Send(cmdBuf, 0, len, SocketFlags.None);

            if (SkipCmd())
                return 0;

            Thread.Sleep(CmdDelay);

            if (!SocketExtensions.IsConnected(_socket)) {
                Logger.Info("sa0");
                return 0;
            }

            int bytesRec = 0;
            int readTimeOut = 1000000;

            if (_socket.Poll(readTimeOut, SelectMode.SelectRead)) {
                bytesRec = _socket.Receive(buf, ReceiveBufferSize, SocketFlags.None);
                timeoutCntr = 0;
            } else {
                Logger.Error("Receive timeout");
                timeoutCntr++;

                if (timeoutCntr >= 3)
                    Reconnect();
            }

            Logger.Debug("Receive {0} byte <- {1}", bytesRec, BitConverter.ToString(buf, 0, bytesRec).Replace('-', ' '));

            if (bytesRec == 0 || !CheckRb(bytesRec))
                return 0;

            return !Fill(bytesRec) ? 0 : bytesRec;
        }

        protected abstract bool Fill(int bytesRec);

        protected abstract bool SkipCmd();

        protected abstract bool CheckRb(int bytesRec);

        #region utils
        protected DateTime? FillDT(int nom) {
            try {
                var mm = GetValue(nom, 2);
                var dd = GetValue(nom + 2, 2);
                var yy = GetValue(nom + 4, 2);
                var hh = GetValue(nom + 6, 2);
                var mn = GetValue(nom + 8, 2);
                if (yy + 2000 > DateTime.Now.Year || mm > 12 || dd > 31 || hh > 24 || mn > 59) {
                    return null;
                }

                return new DateTime(yy + 2000, mm, dd, hh, mn, 0, 0);
            } catch (Exception e) {
                Logger.Error($"error on fillDT {e.Message + e.StackTrace}");
                return null;
            }
        }

        protected int GetValue(int nom, int kol) {
            var result = 0;

            for (var i = 1; i <= kol; i++) {
                if (buf[i - 1 + nom] <= 0x39)
                    result = (int)Math.Round(result + (buf[i - 1 + nom] - 0x30) * Math.Pow(10, kol - i));
                else
                    result = (int)Math.Round(result + (buf[i - 1 + nom] - 0x37) * Math.Pow(10, kol - i));
            }
            return result;
        }

        protected bool Bcc(int pos) {
            var crc = buf[0];
            for (var i = 1; i < pos; i++)
                crc = (byte)(crc ^ buf[i]);

            return crc == 0;
        }

        #endregion

    }
}