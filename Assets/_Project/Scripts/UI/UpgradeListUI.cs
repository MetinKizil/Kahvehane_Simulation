using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Bilgisayarin "Gelistirme" sekmesi. Katalogdaki her upgrade icin bir satir uretir,
    /// para veya satin alma degistikce satirlari tazeler.
    /// Icinde hicbir satin alma mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class UpgradeListUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [SerializeField] private UpgradeCatalog catalog;
        [Tooltip("Bos birakilirsa satirlar bu objenin altina eklenir.")]
        [SerializeField] private Transform entryContainer;
        [Tooltip("Her gelistirme icin kopyalanacak satir prefab'i.")]
        [SerializeField] private UpgradeEntryUI entryPrefab;

        private readonly List<UpgradeEntryUI> rows = new List<UpgradeEntryUI>();

        private void Start()
        {
            if (entryContainer == null) entryContainer = transform;

            if (catalog == null || entryPrefab == null)
            {
                Debug.LogError("[UpgradeListUI] 'Catalog' veya 'Entry Prefab' atanmamis.", this);
                enabled = false;
                return;
            }

            BuildRows();
            Subscribe();
            RefreshAll();
        }

        /// <summary>Katalogdaki satirlari bir kere uretir; sonra sadece tazelenir.</summary>
        private void BuildRows()
        {
            foreach (UpgradeData upgrade in catalog.Upgrades)
            {
                if (upgrade == null || !upgrade.IsValid) continue;

                UpgradeEntryUI row = Instantiate(entryPrefab, entryContainer);
                row.Bind(upgrade);
                rows.Add(row);
            }
        }

        private void Subscribe()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;
            if (ReputationManager.Instance != null) ReputationManager.Instance.OnReputationChanged += HandleReputationChanged;
        }

        private void Unsubscribe()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
            if (ReputationManager.Instance != null) ReputationManager.Instance.OnReputationChanged -= HandleReputationChanged;
        }

        private void HandleMoneyChanged(int currentMoney) => RefreshAll();

        // Itibar barajli gelistirmeler (gizli kumarhane vb.) itibar artinca acilsin.
        private void HandleReputationChanged(int currentReputation) => RefreshAll();

        private void HandleUpgradePurchased(UpgradeData upgrade) => RefreshAll();

        private void RefreshAll()
        {
            foreach (UpgradeEntryUI row in rows)
                row.Refresh();
        }

        private void OnDestroy() => Unsubscribe();
    }
}
