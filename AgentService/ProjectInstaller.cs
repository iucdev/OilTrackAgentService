using System;
using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.Reflection;

namespace AgentService {
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            string executablePath = Assembly.GetExecutingAssembly().Location;
            DirectoryInfo executableDirInfo = new DirectoryInfo(Path.GetDirectoryName(executablePath));
            string serviceName = executableDirInfo.Name;

            InitializeComponent(serviceName);
        }

        private void serviceProcessInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            try
            {
            }
            catch (Exception ex) {

            }
        }
    }
}
