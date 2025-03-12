using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using AgentService.Models;
using Service.LocalDb;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;

namespace OilTrackAgentInterface.ViewModel {
    public class TankIndicatorViewModel : AutoRefreshViewModel<TankIndicatorRecord> {
        public string Title => "This is page Tank Indicators";

        public PagedViewModel<TankIndicatorRecord> PagedTankIndicators { get; }

        public ICommand NextPageCommand => PagedTankIndicators.NextPageCommand;
        public ICommand PreviousPageCommand => PagedTankIndicators.PreviousPageCommand;
        public string PageInfo => PagedTankIndicators.PageInfo;

        public TankIndicatorViewModel() {
            PagedTankIndicators = new PagedViewModel<TankIndicatorRecord>(Items, 7);
            _ = UpdateDataAsync();
        }

        protected override async Task<List<TankIndicatorRecord>> LoadDataFromSourceAsync() {
            return await DatabaseManager.LoadTankIndicatorsDataAsync();
        }

        protected override void OnDataUpdated() {
            PagedTankIndicators.UpdateSource(Items);
            Debug.WriteLine("[TankIndicatorViewModel] Данные обновлены в PagedTankIndicators");
        }
    }
}
