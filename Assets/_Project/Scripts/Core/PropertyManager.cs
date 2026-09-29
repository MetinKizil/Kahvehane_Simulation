using System;
using System.Collections.Generic;
using KanBagi.Data;
using KanBagi.Economy;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Mahalledeki mulklerin TEK sahibi: satin alma ve gunluk kira geliri.
    /// Itibarin ILK ISLEVSEL kullanimi burada - mulkler itibar baraji ister.
    /// Mulkun sahnede neye benzedigini bilmez; 7b'de acilacak objeler
    /// OnPropertyPurchased'a abone olacak (Observer Pattern).
    /// </summary>
    public class PropertyManager : MonoBehaviour, ISaveable
    {
        /// <summary>Kayit dosyasindaki emlak bolumu. Mulkler ASSET ADIYLA saklanir.</summary>
        [Serializable]
        private class PropertySaveData
        {
            public List<string> owned = new List<string>();

            /// <summary>
            /// O gunun basinda yatan kira. Kaydedilmesi SART:
            /// acilista gun 1 basladiginda kira daha yuklenmemis mulklere gore 0 hesaplanir,
            /// bu deger geri yuklenmezse devam edilen gunun ozetinde kira 0 gorunur.
            /// </summary>
            public int lastRentPaid;
        }

        public static PropertyManager Instance { get; private set; }

        [Header("Katalog")]
        [Tooltip("Satin alinabilir mulkler. Kayit yuklenirken de buradan eslenir.")]
        [SerializeField] private PropertyCatalog catalog;

        private readonly HashSet<PropertyData> owned = new HashSet<PropertyData>();

        /// <summary>Sahip olunan mulklerin gunluk toplam geliri.</summary>
        public int TotalDailyIncome
        {
            get
            {
                int total = 0;

                foreach (PropertyData property in owned)
                {
                    if (property != null) total += property.DailyIncome;
                }

                return total;
            }
        }

        /// <summary>
        /// Bu gunun basinda kasaya giren kira. Gunluk ozet bunu okur.
        /// Event yerine deger tutuyoruz ki event sirasi hesaba karismasin.
        /// </summary>
        public int LastRentPaid { get; private set; }

        public int OwnedCount => owned.Count;

        /// <summary>Bir mulk satin alindiginda tetiklenir.</summary>
        public event Action<PropertyData> OnPropertyPurchased;

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
            if (catalog == null)
                Debug.LogError("[PropertyManager] 'Catalog' atanmamis! Mulkler KAYDEDILMEZ. " +
                               "Managers > Property Manager > Catalog alanina EmlakKatalogu'nu surukle.", this);

            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted += HandleDayStarted;
            else
                Debug.LogError("[PropertyManager] DayCycleManager bulunamadi; kira toplanamaz.", this);
        }

        public bool IsOwned(PropertyData property) => property != null && owned.Contains(property);

        /// <summary>Itibar baraji asildi mi?</summary>
        public bool MeetsReputation(PropertyData property)
        {
            if (property == null) return false;

            int reputation = ReputationManager.Instance != null ? ReputationManager.Instance.CurrentReputation : 0;
            return reputation >= property.RequiredReputation;
        }

        /// <summary>Satin alinabilir durumda mi? Buton aktifligi icin UI sorar.</summary>
        public bool CanPurchase(PropertyData property)
        {
            if (property == null || !property.IsValid || IsOwned(property)) return false;
            if (!MeetsReputation(property)) return false;
            if (EconomyManager.Instance == null) return false;

            return EconomyManager.Instance.CurrentMoney >= property.PurchasePrice;
        }

        /// <summary>Mulku satin alir. Sart saglanmazsa hicbir sey yapmaz.</summary>
        public bool TryPurchase(PropertyData property)
        {
            if (property == null || !property.IsValid || IsOwned(property)) return false;
            if (!MeetsReputation(property)) return false;

            if (EconomyManager.Instance == null)
            {
                Debug.LogError("[PropertyManager] EconomyManager bulunamadi.", this);
                return false;
            }

            if (!EconomyManager.Instance.TrySpendMoney(property.PurchasePrice)) return false;

            owned.Add(property);
            OnPropertyPurchased?.Invoke(property);
            return true;
        }

        /// <summary>Gun basinda kira kasaya girer.</summary>
        private void HandleDayStarted(int day)
        {
            LastRentPaid = TotalDailyIncome;

            if (LastRentPaid > 0 && EconomyManager.Instance != null)
                EconomyManager.Instance.AddMoney(LastRentPaid);
        }

        public string SaveKey => "properties";

        public string CaptureState()
        {
            var data = new PropertySaveData { lastRentPaid = LastRentPaid };

            foreach (PropertyData property in owned)
            {
                if (property != null) data.owned.Add(property.name);
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<PropertySaveData>(state);
            if (data == null || data.owned == null) return;

            if (catalog == null)
            {
                // Mevcut mulkleri SILMIYORUZ: katalog eksikken temizlemek veriyi yok ederdi.
                Debug.LogError("[PropertyManager] 'Catalog' atanmamis; mulkler yuklenemiyor. " +
                               "Managers > Property Manager > Catalog alanina EmlakKatalogu'nu surukle.", this);
                return;
            }

            // Devam edilen gunun kirasi: yeniden odenmez, sadece raporda gorunsun diye geri yuklenir.
            LastRentPaid = Mathf.Max(0, data.lastRentPaid);

            owned.Clear();

            foreach (string propertyName in data.owned)
            {
                PropertyData property = catalog.Find(propertyName);

                if (property == null)
                {
                    Debug.LogWarning($"[PropertyManager] Kayittaki '{propertyName}' mulku katalogda yok, atlandi.", this);
                    continue;
                }

                owned.Add(property);
            }

            // 7b'de mulkun sahnedeki gorseli bu event'e abone olacak; simdiden duyuruyoruz.
            foreach (PropertyData property in owned)
                OnPropertyPurchased?.Invoke(property);
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted -= HandleDayStarted;

            if (Instance == this) Instance = null;
        }
    }
}
