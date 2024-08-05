using NLog;
using NLog.Config;
using NLog.Targets;
using System.ServiceProcess;
using System.Text;
using Service.LocalDb;
using Service.Common;

namespace AgentService {
    static class Program {
        static void Main(string[] args)
        {
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("CustomAppender", typeof(CustomAppender));
            var config = new LoggingConfiguration();

            var target =
                new FileTarget
                {
                    Layout = "${CustomAppender}",
                    FileName = "${basedir}/logs/${shortdate}.log",
                    ArchiveFileName = "${basedir}/logs/Archive/Trace_{#}.zip",
                    ArchiveNumbering = ArchiveNumberingMode.Date,
                    ArchiveDateFormat = "yyyy-MM-dd_HH.mm.ss",
                    ArchiveOldFileOnStartup = true,
                    MaxArchiveFiles = 0,
                    EnableArchiveFileCompression = true,
                    ArchiveEvery = FileArchivePeriod.Day,
                    ArchiveAboveSize = 20971520,
                    Encoding = Encoding.UTF8
                };

            config.AddTarget("logfile", target);
            var rule = new LoggingRule("*", LogLevel.Debug, target);

            config.LoggingRules.Add(rule);
            LogManager.Configuration = config;

            DatabaseManager.InitializeDatabase();

            RunService();
        }

        private static void RunService()
        {
            var servicesToRun = new ServiceBase[] {
                new Service()
            };
            servicesToRun[0].CanHandlePowerEvent = true;
            ServiceBase.Run(servicesToRun);
        }
    }
}