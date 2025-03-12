using AgentService.References;
using Service.Clients.RSMDB;
using Service.Dtos;
using Service.LocalDb;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OilTrackAgentInterface.ViewModel {
    public class SendPackageViewModel : AutoRefreshViewModel<QueueTaskRecord> {
        public string Title => "This is page Tank Transfers";
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int SuccessfulTasks { get; set; }
        public int FailedTasks { get; set; }
        public Dictionary<string, int> TasksByType { get; set; }

        public PagedViewModel<QueueTaskRecord> PagedQueueTaskRecord { get; }

        public ICommand NextPageCommand => PagedQueueTaskRecord.NextPageCommand;
        public ICommand PreviousPageCommand => PagedQueueTaskRecord.PreviousPageCommand;
        public string PageInfo => PagedQueueTaskRecord.PageInfo;

        public SendPackageViewModel() {
            PagedQueueTaskRecord = new PagedViewModel<QueueTaskRecord>(Items, 6);
            _ = UpdateDataAsync();
        }

        protected override async Task<List<QueueTaskRecord>> LoadDataFromSourceAsync() {
            var data = await DatabaseManager.LoadQueueTaskRecordDataAsync();
            TotalTasks = data.Count;
            PendingTasks = data.Count(t => t.Status == QueueTaskStatus.InProcess);
            SuccessfulTasks = data.Count(t => t.Status == QueueTaskStatus.Processed);
            FailedTasks = data.Count(t => t.Status == QueueTaskStatus.Abandon);

            TasksByType = data
                .GroupBy(t => t.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count());
            return data;
        }

        protected override void OnDataUpdated() {
            PagedQueueTaskRecord.UpdateSource(Items);
            Debug.WriteLine("[SendPackageViewModel] Данные обновлены в PagedQueueTaskRecord");
        }
    }
}
