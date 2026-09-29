using System;
using KanBagi.Core;
using UnityEngine;

namespace KanBagi.Economy
{
    /// <summary>
    /// Kasadaki paranin TEK sahibi. Baska hicbir sinif para degerini dogrudan tutmaz.
    /// Para degistiginde OnMoneyChanged event'i yayinlanir (ANAYASA: Observer Pattern);
    /// UI ve diger sistemler bu event'e abone olur, Update'te poll edilmez.
    /// </summary>
    public class EconomyManager : MonoBehaviour, ISaveable
    {
        /// <summary>Kayit dosyasindaki ekonomi bolumu.</summary>
        [Serializable]
        private class MoneySaveData
        {
            public int money;
        }

        public static EconomyManager Instance { get; private set; }

        [Header("Baslangic")]
        [Tooltip("Oyuna baslarken kasadaki para (TL). Hapisten yeni ciktik, fazla bir sey bekleme.")]
        [SerializeField] private int startingMoney = 250;

        public int CurrentMoney { get; private set; }

        /// <summary>Para her degistiginde yeni toplam ile tetiklenir.</summary>
        public event Action<int> OnMoneyChanged;

        /// <summary>
        /// Kasadan para cikinca harcanan tutar ile tetiklenir.
        /// TrySpendMoney tum harcamalarin tek gecis noktasi oldugu icin
        /// malzeme siparisi de gelistirme de buradan yakalanir.
        /// </summary>
        public event Action<int> OnMoneySpent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentMoney = startingMoney;
        }

        /// <summary>Kasaya para ekler (satis, kumar geliri vb.).</summary>
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;

            CurrentMoney += amount;
            OnMoneyChanged?.Invoke(CurrentMoney);
        }

        /// <summary>
        /// Para yeterliyse harcar ve true doner; yetersizse hicbir sey yapmaz, false doner.
        /// Stok siparisi, upgrade ve emlak alimlari bu metodu kullanir.
        /// </summary>
        public bool TrySpendMoney(int amount)
        {
            if (amount <= 0 || CurrentMoney < amount) return false;

            CurrentMoney -= amount;
            OnMoneyChanged?.Invoke(CurrentMoney);
            OnMoneySpent?.Invoke(amount);
            return true;
        }

        public string SaveKey => "economy";

        public string CaptureState() => JsonUtility.ToJson(new MoneySaveData { money = CurrentMoney });

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<MoneySaveData>(state);
            if (data == null) return;

            CurrentMoney = Mathf.Max(0, data.money);
            OnMoneyChanged?.Invoke(CurrentMoney);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
