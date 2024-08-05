using NLog;
using System.ComponentModel;
using System.Threading;

namespace Service.Clients.Scheduler
{
    public abstract class Job
    {
        internal Logger Log = LogManager.GetLogger(typeof(Job).Name);

        public abstract NamedBackgroundWorker RunWorker();

        public string Name { get; set; }
        public long ObjectId { get; set; }
        internal NamedBackgroundWorker Worker { get; set; }
    }

    public class NamedBackgroundWorker : BackgroundWorker
    {
        public NamedBackgroundWorker(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = Name;

            base.OnDoWork(e);
        }
    }
}
