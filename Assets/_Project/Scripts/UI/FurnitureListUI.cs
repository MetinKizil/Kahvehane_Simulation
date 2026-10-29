using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using KanBagi.Furniture;
using KanBagi.Shop;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// PC'nin ESYALAR sayfasi: katalogdaki her esya icin bir satir.
    /// "Satin Al"a basilinca PC ekrani kapanir ve esya PC'yi kullanan oyuncunun elinde hayalet olarak belirir
    /// (FurniturePlacer). Para burada DUSMEZ; esya yerlestirilince FurnitureManager.TryPurchase'ta duser.
    /// Kurulum: Kan Bagi > Esya Katalogunu Kur
    /// </summary>
    public class FurnitureListUI : MonoBehaviour
    {
        [Header("Veri")]
        [SerializeField] private FurnitureCatalog catalog;

        [Header("Baglantilar")]
        [Tooltip("Satirlarin dizilecegi obje. Bos birakilirsa bu objenin kendisi.")]
        [SerializeField] private Transform entryContainer;
        [SerializeField] private FurnitureEntryUI entryPrefab;

        private readonly List<FurnitureEntryUI> rows = new List<FurnitureEntryUI>();

        private void Start()
        {
            if (entryContainer == null) entryContainer = transform;

            if (catalog == null || entryPrefab == null)
            {
                Debug.LogError("[FurnitureListUI] 'Catalog' veya 'Entry Prefab' atanmamis. " +
                               "'Kan Bagi > Esya Katalogunu Kur' menusunu calistir.", this);
                enabled = false;
                return;
            }

            foreach (FurnitureData data in catalog.Items)
            {
                if (data == null) continue;

                FurnitureEntryUI row = Instantiate(entryPrefab, entryContainer);
                row.Bind(data, HandleBuy);
                rows.Add(row);
            }

            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            if (FurnitureManager.Instance != null) FurnitureManager.Instance.OnLayoutChanged += RefreshAll;
            if (ReputationManager.Instance != null) ReputationManager.Instance.OnReputationChanged += HandleReputationChanged;
        }

        // Sayfa her acildiginda guncel sayilar gorunsun.
        private void OnEnable() => RefreshAll();

        private void HandleMoneyChanged(int money) => RefreshAll();

        // Itibar kilitli makine (sahlep) itibar yetince acilsin.
        private void HandleReputationChanged(int reputation) => RefreshAll();

        private void RefreshAll()
        {
            foreach (FurnitureEntryUI row in rows)
            {
                if (row != null) row.Refresh();
            }
        }

        /// <summary>Ekrani kapatir ve esyayi PC'yi kullanan oyuncunun eline hayalet olarak verir.</summary>
        private void HandleBuy(FurnitureData data)
        {
            // PC tasinabilir esya (D5): satin alma aninda ekrani acik olan PC bulunur.
            ComputerStation computer = ComputerStation.FindOpen();
            if (computer == null || computer.ScreenUser == null) return;
            if (!computer.ScreenUser.TryGetComponent(out FurniturePlacer placer))
            {
                Debug.LogError("[FurnitureListUI] Oyuncuda FurniturePlacer yok. 'Kan Bagi > Yerlestirme Modunu Kur' calistir.", this);
                return;
            }

            // Once ekran kapanir (oyuncunun kontrolu geri gelir), sonra esya ele verilir.
            computer.CloseScreen();
            placer.BeginPlacingNew(data);
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
            if (FurnitureManager.Instance != null) FurnitureManager.Instance.OnLayoutChanged -= RefreshAll;
            if (ReputationManager.Instance != null) ReputationManager.Instance.OnReputationChanged -= HandleReputationChanged;
        }
    }
}
