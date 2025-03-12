using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AgentService.Models;
using OilTrackAgentInterface.Pages.Common;
using OilTrackAgentInterface.ViewModel;
using Service.LocalDb;

namespace OilTrackAgentInterface.ViewModel {
    public class TankTransfersViewModel : AutoRefreshViewModel<TankTransferRecord> {
        public PagedViewModel<TankTransferRecord> PagedTankTransfers { get; }

        public ICommand NextPageCommand => PagedTankTransfers.NextPageCommand;
        public ICommand PreviousPageCommand => PagedTankTransfers.PreviousPageCommand;
        public string PageInfo => PagedTankTransfers.PageInfo;

        public TankTransfersViewModel() : base(60000) { // ⏳ Обновление каждую минуту
            PagedTankTransfers = new PagedViewModel<TankTransferRecord>(Items, 7);
            _ = UpdateDataAsync(); // Запускаем первую загрузку
        }

        protected override async Task<List<TankTransferRecord>> LoadDataFromSourceAsync() {
            return await DatabaseManager.LoadTankTransfersDataAsync();
        }

        protected override void OnDataUpdated() {
            PagedTankTransfers.UpdateSource(Items);
            Debug.WriteLine("[TankTransfersViewModel] Данные обновлены в PagedTankTransfers");
        }
    }

}
