using System;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KanBagi.Shop
{
    /// <summary>
    /// Dukkanin o an satabildigi iceceklerin TEK sahibi.
    /// Musteriler ne isteyeceklerini buraya sorar; hangi makinenin acik oldugunu bilmez (SOLID).
    /// Menu, satin alinan gelistirmelerle buyur: UpgradeManager'a abone olur (Observer Pattern).
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        /// <summary>Menudeki tek bir icecek ve varsa onu acan gelistirme.</summary>
        [Serializable]
        public class MenuEntry
        {
            [SerializeField] private ItemData beverage;
            [Tooltip("Bos birakilirsa icecek en bastan menudedir. Doluysa o gelistirme alininca menuye girer.")]
            [SerializeField] private UpgradeData requiredUpgrade;

            public ItemData Beverage => beverage;
            public UpgradeData RequiredUpgrade => requiredUpgrade;
            public bool IsValid => beverage != null && beverage.Type == ItemType.Beverage;
        }

        public static MenuManager Instance { get; private set; }

        [Header("Menu")]
        [Tooltip("Dukkanin sunabilecegi tum icecekler. Kilitli olanlar gelistirme alininca acilir.")]
        [SerializeField] private List<MenuEntry> entries = new List<MenuEntry>();

        private readonly List<ItemData> availableBeverages = new List<ItemData>();

        /// <summary>Su anda musterilere sunulabilen icecekler.</summary>
        public IReadOnlyList<ItemData> AvailableBeverages => availableBeverages;

        public bool HasAnyBeverage => availableBeverages.Count > 0;

        /// <summary>Menu degistiginde tetiklenir (yeni makine alindi vb.).</summary>
        public event Action OnMenuChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            // Start'ta kuruyoruz ki UpgradeManager'in Awake'i garantiyle bitmis olsun.
            RebuildMenu();

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;
            else
                Debug.LogError("[MenuManager] UpgradeManager bulunamadi. 'Managers' objesine ekle.", this);
        }

        /// <summary>Musteri ne isteyecegini buradan secer.</summary>
        public ItemData GetRandomBeverage()
        {
            if (availableBeverages.Count == 0) return null;

            return availableBeverages[Random.Range(0, availableBeverages.Count)];
        }

        public bool IsOnMenu(ItemData beverage) => beverage != null && availableBeverages.Contains(beverage);

        private void HandleUpgradePurchased(UpgradeData upgrade) => RebuildMenu();

        private void RebuildMenu()
        {
            availableBeverages.Clear();

            foreach (MenuEntry entry in entries)
            {
                if (entry == null || !entry.IsValid) continue;
                if (!IsUnlocked(entry.RequiredUpgrade)) continue;
                if (availableBeverages.Contains(entry.Beverage)) continue;

                availableBeverages.Add(entry.Beverage);
            }

            OnMenuChanged?.Invoke();
        }

        private static bool IsUnlocked(UpgradeData upgrade)
        {
            if (upgrade == null) return true;

            return UpgradeManager.Instance != null && UpgradeManager.Instance.IsPurchased(upgrade);
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;

            if (Instance == this) Instance = null;
        }
    }
}
