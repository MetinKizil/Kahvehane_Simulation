using System;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Inventory
{
    /// <summary>
    /// Depodaki sarf malzemelerinin TEK sahibi (cay yapragi, seker, kahve...).
    /// Baska hicbir sinif stok sayisi tutmaz; herkes buraya sorar.
    /// Stok degisince OnStockChanged yayinlanir (ANAYASA: Observer Pattern);
    /// UI ve ileride yonetim PC'si bu event'e abone olur, Update'te poll edilmez.
    /// </summary>
    public class InventoryManager : MonoBehaviour, ISaveable
    {
        /// <summary>
        /// Kayit dosyasindaki stok bolumu.
        /// Malzemeler ASSET ADIYLA saklanir; yuklerken ItemDatabase'den geri bulunur.
        /// </summary>
        [Serializable]
        private class StockSaveData
        {
            public List<string> items = new List<string>();
            public List<int> amounts = new List<int>();
        }

        /// <summary>Inspector'dan baslangic stogu girmek icin satir.</summary>
        [Serializable]
        public class StockEntry
        {
            [SerializeField] private ItemData item;
            [SerializeField] private int amount;

            public ItemData Item => item;
            public int Amount => amount;
        }

        public static InventoryManager Instance { get; private set; }

        [Header("Baslangic Stogu")]
        [Tooltip("Oyuna baslarken depodaki malzemeler. Amcadan kalan ne varsa...")]
        [SerializeField] private List<StockEntry> startingStock = new List<StockEntry>();

        [Header("Kayit")]
        [Tooltip("Kayit yuklenirken malzemeleri adindan bulmak icin gerekli.")]
        [SerializeField] private ItemDatabase itemDatabase;

        private readonly Dictionary<ItemData, int> stock = new Dictionary<ItemData, int>();

        /// <summary>Bir malzemenin adedi degistiginde (malzeme, yeni adet) ile tetiklenir.</summary>
        public event Action<ItemData, int> OnStockChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadStartingStock();
        }

        private void LoadStartingStock()
        {
            foreach (StockEntry entry in startingStock)
            {
                if (entry == null || entry.Item == null) continue;

                // Ayni malzeme iki kez yazildiysa toplanir.
                stock[entry.Item] = GetCount(entry.Item) + Mathf.Max(0, entry.Amount);
            }
        }

        /// <summary>Depodaki adet (hic yoksa 0).</summary>
        public int GetCount(ItemData item)
        {
            return item != null && stock.TryGetValue(item, out int count) ? count : 0;
        }

        /// <summary>Istenen miktar depoda var mi? Sadece sorar, dusurmez.</summary>
        public bool HasStock(ItemData item, int amount = 1) => GetCount(item) >= amount;

        /// <summary>
        /// Yeterliyse malzemeyi dusurur ve true doner; yetersizse hicbir sey yapmaz, false doner.
        /// Cay ocagi ve ileride kahve makinesi bunu kullanir.
        /// </summary>
        public bool TryConsume(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return false;

            int current = GetCount(item);
            if (current < amount) return false;

            SetCount(item, current - amount);
            return true;
        }

        /// <summary>Depoya malzeme ekler (kurye teslimati, baslangic stogu takviyesi).</summary>
        public void Add(ItemData item, int amount = 1)
        {
            if (item == null || amount <= 0) return;

            SetCount(item, GetCount(item) + amount);
        }

        private void SetCount(ItemData item, int newCount)
        {
            int clamped = Mathf.Max(0, newCount);

            stock[item] = clamped;
            OnStockChanged?.Invoke(item, clamped);
        }

        public string SaveKey => "inventory";

        public string CaptureState()
        {
            var data = new StockSaveData();

            foreach (KeyValuePair<ItemData, int> pair in stock)
            {
                if (pair.Key == null) continue;

                data.items.Add(pair.Key.name);
                data.amounts.Add(pair.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<StockSaveData>(state);
            if (data == null || data.items == null || data.amounts == null) return;

            if (itemDatabase == null)
            {
                Debug.LogError("[InventoryManager] 'Item Database' atanmamis; stok yuklenemiyor.", this);
                return;
            }

            stock.Clear();

            int count = Mathf.Min(data.items.Count, data.amounts.Count);

            for (int i = 0; i < count; i++)
            {
                ItemData item = itemDatabase.Find(data.items[i]);

                if (item == null)
                {
                    Debug.LogWarning($"[InventoryManager] Kayittaki '{data.items[i]}' malzemesi ItemDatabase'de yok, atlandi.", this);
                    continue;
                }

                stock[item] = Mathf.Max(0, data.amounts[i]);
            }

            // UI'lar yeni degerleri duysun. Event sirasinda sozlugu degistirmemek icin kopya uzerinde donuyoruz.
            foreach (KeyValuePair<ItemData, int> pair in new List<KeyValuePair<ItemData, int>>(stock))
                OnStockChanged?.Invoke(pair.Key, pair.Value);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
