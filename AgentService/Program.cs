using NLog;
using NLog.Config;
using NLog.Targets;
using System.ServiceProcess;
using System.Text;
using Service.LocalDb;
using Service.Common;
using Service.Clients.Utils;
using System.Reflection;
using System.Threading;
using System;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;

namespace AgentService {
    static class Program {
        static void Main(string[] args)
        {
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("CustomAppender", typeof(CustomAppender));
            var config = new LoggingConfiguration();

            // один таргет — файл, имя которого будет по имени логгера:
            var dynamicFile = new FileTarget("dynamicFile") {
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}",
                FileName = "${basedir}/logs/${logger}-${shortdate}.log",
                ArchiveFileName = "${basedir}/logs/Archive/${logger}_{#}.zip",
                ArchiveNumbering = ArchiveNumberingMode.Date,
                ArchiveDateFormat = "yyyy-MM-dd_HH.mm.ss",
                ArchiveOldFileOnStartup = true,
                MaxArchiveFiles = 0,
                EnableArchiveFileCompression = true,
                ArchiveEvery = FileArchivePeriod.Day,
                ArchiveAboveSize = 20971520,
                Encoding = Encoding.UTF8
            };
            config.AddTarget(dynamicFile);

            var objectSettings = ObjectSettingsSingleton.Instance.ObjectSettings;
            config.AddRule(objectSettings.ShowDebugLogs ? LogLevel.Debug : LogLevel.Info, LogLevel.Fatal, dynamicFile, "*");
            LogManager.Configuration = config;

            DatabaseManager.InitializeDatabase();

            if (args != null && args.Length == 1 && args[0].Length > 1 && (args[0][0] == '-' || args[0][0] == '/')) {
                switch (args[0].Substring(1).ToLower()) {
                    default: RunService(); break;
                    case "console":
                    case "c":
                        var servicesToRun = new ServiceBase[] {
                            new Service(),
                        };
                        RunInteractive(servicesToRun);
                        break;
                };
            } else {
                RunService();
            }
            //RunService();
            var servicesToRun2 = new ServiceBase[] {
                            new Service(),
                        };
            RunInteractive(servicesToRun2);
        }

        private static void RunService()
        {
            var servicesToRun = new ServiceBase[] {
                new Service()
            };
            servicesToRun[0].CanHandlePowerEvent = true;
            ServiceBase.Run(servicesToRun);
        }

        private static void RunInteractive(ServiceBase[] servicesToRun)
        {
            var onStartMethod = typeof(ServiceBase).GetMethod("OnStart", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var service in servicesToRun) {
                Console.Write("Запускается служба {0}...", service.ServiceName);

                onStartMethod.Invoke(service, new object[] { new string[] { } });
                Console.Write("Служба запущена и работает");
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для завершения работы службы...");

            Console.ReadKey();
            Console.WriteLine();

            var onStopMethod = typeof(ServiceBase).GetMethod("OnStop", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var service in servicesToRun) {
                Console.Write("Останавливается служба {0}...", service.ServiceName);
                onStopMethod.Invoke(service, null);
                Console.WriteLine("Служба oстановлена");
            }

            Console.WriteLine("Все службы остановлены.");
            // Keep the console alive for a second to allow the user to see the message.
            Thread.Sleep(1000);
        }
    }
}