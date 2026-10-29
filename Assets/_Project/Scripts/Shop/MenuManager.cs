using System;
using System.Collections.Generic;
using KanBagi.Data;
using KanBagi.Furniture;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KanBagi.Shop
{
    /// <summary>
    /// Dukkanin o an satabildigi iceceklerin TEK sahibi.
    /// Musteriler ne isteyeceklerini buraya sorar; hangi makinenin nerede oldugunu bilmez (SOLID).
    ///
    /// D5b'den beri kural: bir icecek, DUKKANDA ONU YAPAN BIR MAKINE VARSA menudedir (upgrade kilidi kalkti).
    /// Esya duzeni degisince (makine kondu, kayit yuklendi) FurnitureManager'a abone olarak menu tazelenir.
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        /// <summary>Dukkanin sunabilecegi bir icecek.</summary>
        [Serializable]
        public class MenuEntry
        {
            [SerializeField] private ItemData beverage;

            public ItemData Beverage => beverage;
            public bool IsValid => beverage != null && beverage.Type == ItemType.Beverage;
        }

        public static MenuManager Instance { get; private set; }

        [Header("Menu")]
        [Tooltip("Dukkanin sunabilecegi tum icecekler. Onu yapan makine dukkana konunca menuye girer.")]
        [SerializeField] private List<MenuEntry> entries = new List<MenuEntry>();

        private readonly List<ItemData> availableBeverages = new List<ItemData>();

        /// <summary>Su anda musterilere sunulabilen icecekler.</summary>
        public IReadOnlyList<ItemData> AvailableBeverages => availableBeverages;

        public bool HasAnyBeverage => availableBeverages.Count > 0;

        /// <summary>Menu degistiginde tetiklenir (yeni makine kondu vb.).</summary>
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
            // Esyalar FurnitureManager.Start'ta (order 50) kurulur; o kurulunca OnLayoutChanged menuyu yeniden kurar.
            if (FurnitureManager.Instance != null)
                FurnitureManager.Instance.OnLayoutChanged += RebuildMenu;
            else
                Debug.LogError("[MenuManager] FurnitureManager bulunamadi. 'Managers' objesine ekle.", this);

            RebuildMenu();
        }

        /// <summary>Musteri ne isteyecegini buradan secer.</summary>
        public ItemData GetRandomBeverage()
        {
            if (availableBeverages.Count == 0) return null;

            return availableBeverages[Random.Range(0, availableBeverages.Count)];
        }

        public bool IsOnMenu(ItemData beverage) => beverage != null && availableBeverages.Contains(beverage);

        private void RebuildMenu()
        {
            availableBeverages.Clear();
            FurnitureManager furniture = FurnitureManager.Instance;

            foreach (MenuEntry entry in entries)
            {
                if (entry == null || !entry.IsValid) continue;
                if (furniture == null || !furniture.HasStationMaking(entry.Beverage)) continue;
                if (availableBeverages.Contains(entry.Beverage)) continue;

                availableBeverages.Add(entry.Beverage);
            }

            OnMenuChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (FurnitureManager.Instance != null)
                FurnitureManager.Instance.OnLayoutChanged -= RebuildMenu;

            if (Instance == this) Instance = null;
        }
    }
}
