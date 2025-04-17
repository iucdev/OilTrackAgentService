using Service.Clients.Scheduler;
using Service.Clients.Utils;
using Service.Enums;
using NLog;
using System;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;
using AgentService.References;

namespace Service.Clients.Client {
    /// <summary>
    /// MultiServerClient
    /// </summary>
    public abstract class AMultiServerClient {
        internal static Logger Logger = LogManager.GetLogger(typeof(AMultiServerClient).Name);
        internal ObjectSettings ObjectSettings;

        protected Timer _timer;
        private Socket _socket;
        private SerialPort _sPort;

        protected void Init() {
            Logger.Debug("Base Init");
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
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

                Logger.Info("Timer triggered");

                if (GetConnectionState()) {
                    try {
                        CollectData();
                    } catch (Exception ex) {
                        Logger.Error($"CollectData error: {ex.Message}");
                    }

                    Thread.Sleep(2000);
                    _timer.Enabled = true;
                } else {
                    Logger.Error("Нет связи с сервером источника данных!");
                    StopCollection();
                    Thread.Sleep(30000);

                    if (!Worker.CancellationPending)
                        Reconnect();
                }
            } catch (Exception ex) {
                Logger.Error($"Ошибка в TimerJob: {ex.Message}");
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
            timerJob(null, null); // Форсированный запуск
        }

        protected virtual bool StopCollection() {
            Logger.Info("Stop collection");
            if (!ObjectSettings.Mock) {
                _timer?.Stop();

                if (ObjectSettings.ConnectionType == ConnectionType.Ip && _socket != null && _socket.Connected) {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Close();
                    _socket = null;
                }

                if (ObjectSettings.ConnectionType == ConnectionType.Com && _sPort != null && _sPort.IsOpen) {
                    _sPort.Close();
                    _sPort = null;
                }
            }

            return true;
        }

        /// <summary>
        /// Проверка состояния соединения
        /// </summary>
        protected virtual bool GetConnectionState() {
            if (!ObjectSettings.Mock) {
                switch (ObjectSettings.ConnectionType) {
                    case ConnectionType.Com: return _sPort?.IsOpen ?? false;
                    case ConnectionType.Ip: return _socket?.Connected == true && SocketExtensions.IsConnected(_socket);
                    default: return false;
                }
            }

            return true;
        }

        protected bool Connect() {
            if (!ObjectSettings.Mock) {
                switch (ObjectSettings.ConnectionType) {
                    case ConnectionType.Com: return ConnectCom();
                    case ConnectionType.Ip: return ConnectIp();
                    default: throw new Exception("Unknown ConnectionType!");
                }
            }
            Logger.Info("Mock connection Enabled");
            return true;
        }

        private bool ConnectCom() {
            try {
                _sPort = new SerialPort {
                    PortName = ObjectSettings.ComConnectionConfig.PortName,
                    BaudRate = ObjectSettings.ComConnectionConfig.BaudRate ?? 0,
                    DataBits = ObjectSettings.ComConnectionConfig.DataBits ?? 0,
                    Parity = ObjectSettings.ComConnectionConfig.Parity,
                    StopBits = ObjectSettings.ComConnectionConfig.StopBits,
                    ReadTimeout = ObjectSettings.ComConnectionConfig.ReadTimeout ?? 0,
                    WriteTimeout = ObjectSettings.ComConnectionConfig.WriteTimeout ?? 0,
                };
                _sPort.Open();
                Logger.Info($"Подключен к COM-порту {_sPort.PortName} (Статус: {_sPort.IsOpen})");
            } catch (Exception e) {
                Logger.Error($"Ошибка открытия COM-порта: {e.Message}");
                return false;
            }
            return true;
        }

        private bool ConnectIp() {
            try {
                if (!IPAddress.TryParse(ObjectSettings.IpConnectionConfig.IpAddress, out IPAddress ipAddress)) {
                    Logger.Error($"Некорректный IP-адрес: {ObjectSettings.IpConnectionConfig.IpAddress}");
                    return false;
                }

                int port = ObjectSettings.IpConnectionConfig.Port ?? 0;
                var endPoint = new IPEndPoint(ipAddress, port);
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(endPoint);
                Logger.Info($"Подключено к {endPoint}");
            } catch (Exception e) {
                Logger.Error($"Ошибка подключения: {e.Message}");
                return false;
            }

            Thread.Sleep(1000);
            return true;
        }

        protected bool SendCommand(string command, out string response) {
            Logger.Info($"Отправка команды: {command}");
            response = string.Empty;
            if (!ObjectSettings.Mock) {
                try {
                    if (ObjectSettings.ConnectionType == ConnectionType.Ip) {
                        if (_socket == null || !_socket.Connected || !SocketExtensions.IsConnected(_socket)) {
                            Logger.Warn("IP-сокет не подключён. Повторное подключение...");
                            if (!ConnectIp()) return false;
                        }
                    
                        byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                        _socket.Send(commandBytes);
                        Logger.Info("Команда отправлена.");
                    
                        Thread.Sleep(2000);
                    
                        byte[] responseBuffer = new byte[2048];
                        int bytesReceived = _socket.Receive(responseBuffer);
                        if (bytesReceived > 0) {
                            Logger.Debug($"bytesReceived: {bytesReceived}");
                            response = Encoding.ASCII.GetString(responseBuffer, 0, bytesReceived);
                            Logger.Info($"Ответ получен ({bytesReceived} байт): {response}");
                            return true;
                        } else {
                            Logger.Warn("Ответ пуст.");
                            return false;
                        }
                    } else {
                        if (_sPort == null || !_sPort.IsOpen) {
                            Logger.Error("COM-порт не открыт. Повторное подключение...");
                            if (!ConnectCom()) return false;
                        }

                        try {
                            byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                            _sPort.Write(commandBytes, 0, commandBytes.Length);
                            Logger.Info("Команда отправлена.");

                            response = ReadFullResponse(_sPort, 5000); // ожидание до ETX

                            if (!string.IsNullOrEmpty(response)) {
                                Logger.Info($"Ответ получен ({response.Length} байт): {response}");
                                return true;
                            } else {
                                Logger.Warn("Ответ пуст или не получен до таймаута.");
                                return false;
                            }
                        } catch (TimeoutException tex) {
                            Logger.Warn("Таймаут при получении ответа: " + tex.Message);
                            return false;
                        } catch (Exception ex) {
                            Logger.Error("Ошибка при работе с COM-портом: " + ex.Message);
                            return false;
                        }
                    }
                } catch (Exception ex) {
                    Logger.Error($"Ошибка при отправке команды: {ex.Message}");
                    return false;
                }
            }

            return true;
        }

        private string ReadFullResponse(SerialPort port, int timeoutMilliseconds = 5000) {
            var buffer = new StringBuilder();
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < timeoutMilliseconds) {
                try {
                    if (port.BytesToRead > 0) {
                        int readByte = port.ReadByte();
                        if (readByte == -1)
                            continue;

                        char ch = (char)readByte;
                        buffer.Append(ch);

                        if (ch == '\x03') // ETX — конец сообщения
                            break;
                    } else {
                        Thread.Sleep(50); // пауза перед повторной проверкой
                    }
                } catch (TimeoutException) {
                    break;
                }
            }

            return buffer.ToString();
        }
    }

    static class SocketExtensions {
        public static bool IsConnected(Socket socket) {
            try {
                return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            } catch (SocketException) {
                return false;
            }
        }
    }
}
