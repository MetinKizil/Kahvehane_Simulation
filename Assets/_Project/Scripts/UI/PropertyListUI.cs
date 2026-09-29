using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Bilgisayarin "Emlak" sekmesi. Katalogdaki her mulk icin bir satir uretir,
    /// para / itibar / satin alma degistikce satirlari tazeler.
    /// Icinde hicbir satin alma mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class PropertyListUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [SerializeField] private PropertyCatalog catalog;
        [Tooltip("Bos birakilirsa satirlar bu objenin altina eklenir.")]
        [SerializeField] private Transform entryContainer;
        [Tooltip("Her mulk icin kopyalanacak satir prefab'i.")]
        [SerializeField] private PropertyEntryUI entryPrefab;

        private readonly List<PropertyEntryUI> rows = new List<PropertyEntryUI>();

        private void Start()
        {
            if (entryContainer == null) entryContainer = transform;

            if (catalog == null || entryPrefab == null)
            {
                Debug.LogError("[PropertyListUI] 'Catalog' veya 'Entry Prefab' atanmamis.", this);
                enabled = false;
                return;
            }

            BuildRows();
            Subscribe();
            RefreshAll();
        }

        private void BuildRows()
        {
            foreach (PropertyData property in catalog.Properties)
            {
                if (property == null || !property.IsValid) continue;

                PropertyEntryUI row = Instantiate(entryPrefab, entryContainer);
                row.Bind(property);
                rows.Add(row);
            }
        }

        private void Subscribe()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            if (ReputationManager.Instance != null) ReputationManager.Instance.OnReputationChanged += HandleReputationChanged;
            if (PropertyManager.Instance != null) PropertyManager.Instance.OnPropertyPurchased += HandlePropertyPurchased;
        }

        private void Unsubscribe()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
            if (ReputationManager.Instance != null) ReputationManager.Instance.OnReputationChanged -= HandleReputationChanged;
            if (PropertyManager.Instance != null) PropertyManager.Instance.OnPropertyPurchased -= HandlePropertyPurchased;
        }

        private void HandleMoneyChanged(int currentMoney) => RefreshAll();

        private void HandleReputationChanged(int currentReputation) => RefreshAll();

        private void HandlePropertyPurchased(PropertyData property) => RefreshAll();

        private void RefreshAll()
        {
            foreach (PropertyEntryUI row in rows)
                row.Refresh();
        }

        private void OnDestroy() => Unsubscribe();
    }
}
