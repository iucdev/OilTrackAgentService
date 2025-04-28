using AgentService.Models;
using OilTrackAgentInterface.ViewModel;
using Service.LocalDb;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System;
using System.Linq;
using Service.Clients.Utils;
using System.Windows;
using System.Windows.Input;
using OilTrackAgentInterface;

public class TankIndicatorViewModel : AutoRefreshViewModel<TankIndicatorRecord> {
    public PagedViewModel<TankIndicatorRecord> PagedTankIndicators { get; }
    public ObservableCollection<TankStatusCardViewModel> TankCards { get; }

    private bool _isTableView;
    public bool IsTableView {
        get => _isTableView;
        set {
            if (_isTableView == value) return;
            _isTableView = value;
            OnPropertyChanged(nameof(IsTableView));
        }
    }

    public ICommand ToggleViewCommand { get; }

    public TankIndicatorViewModel() {
        IsTableView = false; // по умолчанию карточки
        ToggleViewCommand = new RelayCommand(_ => {
            IsTableView = !IsTableView;
        });
        PagedTankIndicators = new PagedViewModel<TankIndicatorRecord>(Items, 100);
        TankCards = new ObservableCollection<TankStatusCardViewModel>();
        _ = UpdateDataAsync();
    }

    protected override async Task<List<TankIndicatorRecord>> LoadDataFromSourceAsync() {
        return await DatabaseManager.LoadTankIndicatorsDataAsync();
    }

    protected override async void OnDataUpdated() {
        // 1) Обновляем PagedItems
        PagedTankIndicators.UpdateSource(Items);

        // 2) Пересоздаём карточки
        await PopulateTankCardsAsync();
    }

    private async Task PopulateTankCardsAsync() {
        TankCards.Clear();

        // Подтягиваем максимальные объёмы
        var applicantData = await DatabaseManager.GetApplicantDataAsync();
        var objectConfig = ObjectSettingsSingleton.Instance.ObjectSettings
                .Objects.First();
        var tankVolumes = applicantData.Objects
            .Where(o => o.ObjectId == objectConfig.ObjectId)
            .SelectMany(o => o.Tanks.Where(t => t.TankStatus == "Активный"))
            .Select(t => {
                var tankConfig = objectConfig
                    .ObjectSources.First(o => o.ExternalId == t.TankId);
                var internalTankId = tankConfig.InternalId;
                var tankVolume = t.TankVolume;
                var externalTankId = tankConfig.ExternalId;

                return new {
                    InternalTankId = internalTankId,
                    ExtenalTankId = externalTankId,
                    TankVolume = tankVolume,
                };
            }).ToArray();
        var tankRecords = PagedTankIndicators.PagedItems.GroupBy(t => t.ExternalTankId);
        // Проходим по странице:
        foreach (var rec in tankRecords) {
            // максимальный объём (если не найден — используем 0)
            var tankConfig = tankVolumes.First(t => t.ExtenalTankId == rec.Key);
            double maxVol = (double)tankConfig.TankVolume;


            double curVol = rec.First().TankIndicators.Volume;
            int percent = maxVol > 0
                ? (int)Math.Round(curVol / maxVol * 100)
                : 0;

            if(percent > 100) {
                percent = 0;
            }

            var card = new TankStatusCardViewModel {
                InternalTankId = rec.First().InternalTankId,
                OilProductTypeText = rec.First().TankIndicators.OilProductTypeText,
                LevelPercent = percent,
                CurrentVolume = curVol,
                MaxVolume = maxVol,
                UnitText = rec.First().TankIndicators.VolumeUnitText,
                StatusText = percent == 0 ? "Ошибка" : "Активный",
                StatusColor = percent == 0 ? "#b63234" : "#4CAF50"
            };
            TankCards.Add(card);
        }
    }
}
