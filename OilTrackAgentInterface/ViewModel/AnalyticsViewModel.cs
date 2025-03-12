﻿using AgentService.Models;
using AgentService.References;
using Service.Clients.RSMDB;
using Service.Clients.Utils;
using Service.Dtos;
using Service.LocalDb;
using Sunp.Api.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace OilTrackAgentInterface.ViewModel {

    public class TankData {
        public DateTime? LastConnectionTime { get; set; }
        public TankStatus Status { get; set; }
    }

    public class TankConnectionRecord {
        public string StationName { get; set; }
        public int TotalTanks => GreenTanksCount + YellowTanksCount + RedTanksCount;

        public int GreenTanksCount { get; set; } // 🟢
        public int YellowTanksCount { get; set; } // 🟡
        public int RedTanksCount { get; set; } // 🔴

        public TankConnectionRecord(string stationName, List<TankData> tanks) {
            StationName = stationName;
            GreenTanksCount = tanks.Count(t => t.Status == TankStatus.Connected);
            //YellowTanksCount = tanks.Count(t => t.Status == TankStatus.Delayed);
            RedTanksCount = tanks.Count(t => t.Status == TankStatus.Disconnected);
        }
    }

    public class AnalyticsViewModel : AutoRefreshViewModel<TankConnectionRecord> {

        private SunpApiClient _sunpApiClient = SunpApiClientSingleton.Instance.SunpApiClient;

        public PagedViewModel<TankConnectionRecord> GroupedTankData { get; set; }

        public AnalyticsViewModel() : base(100000) {
            GroupedTankData = new PagedViewModel<TankConnectionRecord>(Items, 10000);
            _ = UpdateDataAsync();

            Debug.WriteLine("🔥 AnalyticsViewModel инициализирован");
        }

        protected override async Task<List<TankConnectionRecord>> LoadDataFromSourceAsync() {
            return await LoadTankDataAsync();
        }

        protected override void OnDataUpdated() {
            GroupedTankData.UpdateSource(Items);
            Debug.WriteLine("[AnalyticsViewModel] Данные обновлены в GroupedTankData");
        }

        /// <summary>
        /// Получает данные о резервуарах от API.
        /// </summary>
        private async Task<List<TankConnectionRecord>> LoadTankDataAsync() {
            try {
                var data = await GetApplicantDataAsync();
                if(data == null) {
                    Debug.WriteLine($"Ошибка загрузки данных. Null");
                    return new List<TankConnectionRecord>();
                }
                var groupedData = data.Objects
                    .Select(objectData => new TankConnectionRecord(
                        objectData.ObjectName,
                        objectData.Tanks.Select(tankData => new TankData {
                            LastConnectionTime = tankData.LastConnectionTime,
                            Status = GetTankStatus(tankData.LastConnectionTime)
                        }).ToList()
                    )).ToList();
                Debug.WriteLine($"🔥 Загружено {groupedData.Count} объектов");
                return groupedData;
            } catch (Exception ex) {
                Debug.WriteLine($"Ошибка загрузки данных: {ex.Message}");
                return new List<TankConnectionRecord>(); // Возвращаем пустой список при ошибке
            }
        }

        private async Task<CollectedApplicantData> GetApplicantDataAsync() {
            var request = new RequestBodyBase() {
                RequestGuid = Guid.NewGuid().ToString()
            };

            Debug.WriteLine($"🛠️ Проверяем _sunpApiClient: {_sunpApiClient != null}");
            Debug.WriteLine($"🛠️ Базовый URL клиента: {_sunpApiClient?.BaseUrl}");
            Debug.WriteLine($"🚀 Отправка запроса: {ObjectSettingsSingleton.Instance.ObjectSettings.ApiUrl}");
            Debug.WriteLine($"🛠️ Токен: {ObjectSettingsSingleton.Instance.ObjectSettings.ApiToken}");
            Debug.WriteLine($"📌 RequestGuid: {request.RequestGuid}");

            if (_sunpApiClient == null) {
                Debug.WriteLine("⚠️ _sunpApiClient не инициализирован. Повторная попытка...");
                SunpApiClientSingleton.Instance.InitializeClient();
                _sunpApiClient = SunpApiClientSingleton.Instance.SunpApiClient;
            }

            try {
                var data = await _sunpApiClient.ProviderGetApplicantDataAsync(request);
                Debug.WriteLine("✅ Запрос успешно выполнен!");
                return data.ApplicantData;
            } catch (HttpRequestException httpEx) {
                Debug.WriteLine($"❌ Ошибка HTTP: {httpEx.Message}");
            } catch (Exception ex) {
                Debug.WriteLine($"💥 Общая ошибка: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Определяет статус резервуара на основе времени последнего соединения.
        /// </summary>
        private TankStatus GetTankStatus(DateTime? lastConnectionTime) {
            if (!lastConnectionTime.HasValue)
                return TankStatus.Disconnected; // 🔴 Красный (больше 5 часов)

            var timeDifference = (DateTime.Now - lastConnectionTime.Value).TotalHours;

            if (timeDifference <= 2)
                return TankStatus.Connected; // 🟢 Зеленый (≤ 2 часа)
            if (timeDifference > 2 && timeDifference < 5)
                return TankStatus.Disconnected;
                //return TankStatus.Delayed; // 🟡 Желтый (2-5 часов)


            return TankStatus.Disconnected; // 🔴 Красный (больше 5 часов)
        }
    }
}
