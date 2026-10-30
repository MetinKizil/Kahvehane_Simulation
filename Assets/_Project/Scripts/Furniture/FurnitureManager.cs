using System;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using KanBagi.Shop;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Dukkandaki yerlestirilmis esyalarin TEK sahibi (dunya durumu; co-op'ta server-authoritative).
    /// Esya olusturma/tasima/kaldirma SADECE buradaki metotlardan gecer (ileride RPC siniri).
    /// Her degisiklikte OnLayoutChanged yayinlar; NavMesh'i NavMeshRuntimeBaker gunceller (SOLID).
    ///
    /// Kayit yoksa (yeni oyun) varsayilan duzeni kurar; kayit varsa SaveManager duzeni kayittakiyle degistirir.
    /// Order 50: varsayilan duzen SaveManager'in (100) yuklemesinden ONCE kurulsun.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class FurnitureManager : MonoBehaviour, ISaveable
    {
        [Serializable]
        private class FurnitureSaveEntry
        {
            public string furniture;
            public Vector3 position;
            public float yaw;
        }

        [Serializable]
        private class FurnitureSaveData
        {
            public List<FurnitureSaveEntry> items = new List<FurnitureSaveEntry>();
        }

        public static FurnitureManager Instance { get; private set; }

        [Header("Veri")]
        [Tooltip("Kayittaki esya adlarini tanima ceviren liste.")]
        [SerializeField] private FurnitureDatabase database;
        [Tooltip("Kayit yokken (yeni oyun) kurulacak duzen.")]
        [SerializeField] private FurnitureLayout defaultLayout;

        private readonly List<PlacedFurniture> placed = new List<PlacedFurniture>();

        public IReadOnlyList<PlacedFurniture> Placed => placed;

        /// <summary>Esya eklenince, tasininca veya kaldirilinca tetiklenir. NavMesh guncelleyici abone olur.</summary>
        public event Action OnLayoutChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (database == null)
            {
                Debug.LogError("[FurnitureManager] 'Database' atanmamis. 'Kan Bagi > Esya Altyapisini Kur' calistir.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            if (placed.Count == 0 && defaultLayout != null)
                SpawnLayout(defaultLayout.Entries);
        }

        // ---- Esya islemleri (tek gecis noktasi) ----

        /// <summary>Esyayi verilen yere, verilen aciyla koyar.</summary>
        public PlacedFurniture Spawn(FurnitureData data, Vector3 position, float yaw)
        {
            PlacedFurniture furniture = CreateInstance(data, position, yaw);
            if (furniture != null) OnLayoutChanged?.Invoke();
            return furniture;
        }

        /// <summary>Esyayi yeni yerine ve acisina tasir.</summary>
        public void Move(PlacedFurniture furniture, Vector3 position, float yaw)
        {
            if (furniture == null || !placed.Contains(furniture)) return;

            furniture.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            OnLayoutChanged?.Invoke();
        }

        /// <summary>Esyayi dukkandan kaldirir.</summary>
        public void Remove(PlacedFurniture furniture)
        {
            if (furniture == null || !placed.Remove(furniture)) return;

            DestroyInstance(furniture);
            OnLayoutChanged?.Invoke();
        }

        // ---- Istasyon sorgulari (menu ve malzeme kilidi) ----

        /// <summary>Dukkanda bu icecegi yapan bir makine var mi? Menu buna bakar (D5b: upgrade yerine).</summary>
        public bool HasStationMaking(ItemData beverage)
        {
            if (beverage == null) return false;

            foreach (PlacedFurniture furniture in placed)
            {
                if (furniture != null && furniture.TryGetComponent(out TeaStove stove) && stove.Beverage == beverage) return true;
            }

            return false;
        }

        /// <summary>Dukkanda bu malzemeyi kullanan bir makine var mi? PC'deki malzeme kilidi buna bakar.</summary>
        public bool HasStationUsing(ItemData consumable)
        {
            if (consumable == null) return false;

            foreach (PlacedFurniture furniture in placed)
            {
                if (furniture != null && furniture.TryGetComponent(out TeaStove stove) && stove.RequiredConsumable == consumable) return true;
            }

            return false;
        }

        // ---- Satin alma ----

        /// <summary>Dukkanda bu esyadan kac tane var?</summary>
        public int CountOf(FurnitureData data)
        {
            int count = 0;
            foreach (PlacedFurniture furniture in placed)
            {
                if (furniture != null && furniture.Data == data) count++;
            }

            return count;
        }

        /// <summary>Bu esyadan en fazla kac tane olabilir (0 = sinirsiz). Alinmis genisletmeler temel sinira eklenir (D6b).</summary>
        public int GetLimit(FurnitureData data)
        {
            if (data == null || data.BaseLimit <= 0) return 0;

            int bonus = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetLimitBonus(data) : 0;
            return data.BaseLimit + bonus;
        }

        /// <summary>Bir sonraki kopyanin fiyati: temel fiyat + her mevcut kopya icin artis (rulet: 800, sonra 1000).</summary>
        public int GetPrice(FurnitureData data) => data != null ? data.Price + data.PriceStep * CountOf(data) : 0;

        /// <summary>Esyanin gerektirdigi gelistirme (varsa) alinmis mi?</summary>
        public bool MeetsUpgrade(FurnitureData data) =>
            data != null && (data.RequiredUpgrade == null ||
                             (UpgradeManager.Instance != null && UpgradeManager.Instance.IsPurchased(data.RequiredUpgrade)));

        /// <summary>Esyanin itibar sarti karsilaniyor mu?</summary>
        public bool MeetsReputation(FurnitureData data) =>
            data != null && (data.RequiredReputation <= 0 ||
                             (ReputationManager.Instance != null && ReputationManager.Instance.CurrentReputation >= data.RequiredReputation));

        /// <summary>Sinir, itibar ve para yetiyor mu? Yetmiyorsa oyuncuya gosterilecek sebep doner.</summary>
        public bool CanPurchase(FurnitureData data, out string reason)
        {
            reason = null;
            if (data == null) return false;

            int limit = GetLimit(data);
            if (limit > 0 && CountOf(data) >= limit)
            {
                reason = $"{data.DisplayName} siniri dolu ({limit})";
                return false;
            }

            if (!MeetsUpgrade(data))
            {
                reason = $"{data.RequiredUpgrade.UpgradeName} gerekli";
                return false;
            }

            if (!MeetsReputation(data))
            {
                reason = $"Itibar {data.RequiredReputation} gerekli";
                return false;
            }

            if (EconomyManager.Instance == null || EconomyManager.Instance.CurrentMoney < GetPrice(data))
            {
                reason = "Para yetmiyor";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Satin almanin TEK gecis noktasi (co-op'ta RPC siniri): para yerlestirme aninda duser, sonra esya konur.
        /// Harcama gunluk ozetin "Gider" satirina girer (TrySpendMoney).
        /// </summary>
        public bool TryPurchase(FurnitureData data, Vector3 position, float yaw)
        {
            if (!CanPurchase(data, out _)) return false;
            if (!EconomyManager.Instance.TrySpendMoney(GetPrice(data))) return false;

            return Spawn(data, position, yaw) != null;
        }

        // ---- Ic isler ----

        private void SpawnLayout(IReadOnlyList<FurnitureLayoutEntry> entries)
        {
            foreach (FurnitureLayoutEntry entry in entries)
                CreateInstance(entry.Furniture, entry.Position, entry.Yaw);

            OnLayoutChanged?.Invoke();
        }

        private PlacedFurniture CreateInstance(FurnitureData data, Vector3 position, float yaw)
        {
            if (data == null || data.Prefab == null)
            {
                Debug.LogError($"[FurnitureManager] Esya tanimi veya prefab'i eksik: '{(data != null ? data.name : "null")}'.", this);
                return null;
            }

            GameObject instance = Instantiate(data.Prefab, position, Quaternion.Euler(0f, yaw, 0f));
            instance.name = data.Prefab.name;

            if (!instance.TryGetComponent(out PlacedFurniture furniture))
                furniture = instance.AddComponent<PlacedFurniture>();

            furniture.Initialize(data);
            placed.Add(furniture);
            return furniture;
        }

        /// <summary>
        /// Once kapatilir: OnDisable hemen calisir (masa TableManager'dan hemen cikar),
        /// Destroy ise kare sonunu bekler; arada ayni masa iki kez sayilmasin.
        /// </summary>
        private static void DestroyInstance(PlacedFurniture furniture)
        {
            furniture.gameObject.SetActive(false);
            Destroy(furniture.gameObject);
        }

        private void ClearAll()
        {
            foreach (PlacedFurniture furniture in placed)
            {
                if (furniture != null) DestroyInstance(furniture);
            }

            placed.Clear();
        }

        // ---- Kayit ----

        public string SaveKey => "furniture";

        public string CaptureState()
        {
            var data = new FurnitureSaveData();

            foreach (PlacedFurniture furniture in placed)
            {
                if (furniture == null || furniture.Data == null) continue;

                data.items.Add(new FurnitureSaveEntry
                {
                    furniture = furniture.Data.name,
                    position = furniture.transform.position,
                    yaw = furniture.Yaw
                });
            }

            return JsonUtility.ToJson(data);
        }

        /// <summary>Mevcut duzen tamamen kayittakiyle degistirilir.</summary>
        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<FurnitureSaveData>(state);
            if (data == null || data.items == null) return;

            ClearAll();

            foreach (FurnitureSaveEntry entry in data.items)
            {
                FurnitureData furnitureData = database.FindByName(entry.furniture);

                if (furnitureData == null)
                {
                    Debug.LogWarning($"[FurnitureManager] Kayittaki '{entry.furniture}' esyasi veritabaninda yok, atlandi.", this);
                    continue;
                }

                CreateInstance(furnitureData, entry.position, entry.yaw);
            }

            SpawnMissingDefaults();
            OnLayoutChanged?.Invoke();
        }

        /// <summary>
        /// Varsayilan duzendeki bir esya TURU kayitta hic yoksa (orn. D5'ten once kaydedilmis oyunda ocak/PC/lavabo
        /// esya degildi) varsayilan yerine konur. Yoksa eski kayit ocaksiz, PC'siz acilirdi.
        /// Oyuncunun sonradan aldigi esyalar (masa) varsayilanda zaten oldugu icin etkilenmez.
        /// </summary>
        private void SpawnMissingDefaults()
        {
            if (defaultLayout == null) return;

            var present = new HashSet<FurnitureData>();
            foreach (PlacedFurniture furniture in placed)
            {
                if (furniture != null && furniture.Data != null) present.Add(furniture.Data);
            }

            foreach (FurnitureLayoutEntry entry in defaultLayout.Entries)
            {
                if (entry.Furniture == null || present.Contains(entry.Furniture)) continue;

                CreateInstance(entry.Furniture, entry.Position, entry.Yaw);
                Debug.Log($"[FurnitureManager] Kayitta '{entry.Furniture.name}' yoktu, varsayilan yerine kondu.", this);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
