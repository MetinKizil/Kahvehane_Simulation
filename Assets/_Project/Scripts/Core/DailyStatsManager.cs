using KanBagi.Customers;
using KanBagi.Economy;
using KanBagi.Illegal;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Gunun muhasebesini tutar: kac musteri, ne kadar ciro, ne kadar gider, itibar farki.
    /// Hicbir sisteme karismaz, sadece event dinler ve sayar (Observer Pattern).
    /// Her gun basinda sifirlanir; gunluk ozet paneli gun sonunda buradan okur.
    /// </summary>
    public class DailyStatsManager : MonoBehaviour
    {
        public static DailyStatsManager Instance { get; private set; }

        [Tooltip("Bos birakilirsa sahnedeki ilk CustomerSpawner bulunur.")]
        [SerializeField] private CustomerSpawner spawner;

        public int CustomersServed { get; private set; }
        public int CustomersLost { get; private set; }
        public int Revenue { get; private set; }
        public int Expenses { get; private set; }

        /// <summary>
        /// Bu gunun basinda kasaya giren kira.
        /// PropertyManager'dan okunuyor; boylece event sirasi hesaba karismaz.
        /// </summary>
        public int RentIncome => PropertyManager.Instance != null ? PropertyManager.Instance.LastRentPaid : 0;

        /// <summary>Bugunku kumar net sonucu (kazanc - bahis). Eksi olabilir.</summary>
        public int GamblingNet { get; private set; }

        /// <summary>Bugun mafyaya odenen harac.</summary>
        public int ProtectionPaid { get; private set; }

        /// <summary>
        /// Bugun polisin kestigi ceza.
        /// PoliceManager'dan okunuyor; ceza TrySpendMoney'den gectigi icin
        /// Expenses'a da yazildi, bu yuzden NetProfit'te tekrar dusulmez.
        /// </summary>
        public int FinePaid => PoliceManager.Instance != null ? PoliceManager.Instance.FinePaidToday : 0;

        public int NetProfit => Revenue + RentIncome + GamblingNet - Expenses - ProtectionPaid;

        /// <summary>Gun basindaki itibar. Gun sonu farki bununla hesaplanir.</summary>
        public int ReputationAtDayStart { get; private set; }

        public int CurrentReputation =>
            ReputationManager.Instance != null ? ReputationManager.Instance.CurrentReputation : 0;

        public int ReputationChange => CurrentReputation - ReputationAtDayStart;

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
            if (spawner == null) spawner = FindFirstObjectByType<CustomerSpawner>();

            if (spawner == null)
                Debug.LogError("[DailyStatsManager] CustomerSpawner bulunamadi; musteri sayilari tutulamaz.", this);
            else
                spawner.OnCustomerSpawned += HandleCustomerSpawned;

            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnMoneySpent += HandleMoneySpent;

            // Rulet masalari oyun sirasinda alinip konuyor (D6d-2): tek tek degil, butun masalarin ortak olayina abone.
            GamblingTable.AnyRoundPlayed += HandleGamblingRound;
            GamblingTable.AnyHouseRound += HandleHouseRound;

            if (MafiaManager.Instance != null)
                MafiaManager.Instance.OnDemandPaid += HandleProtectionPaid;

            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted += HandleDayStarted;

            // Kayit bizden SONRA yukleniyor; itibar geri gelince baslangic degerini tazelemeliyiz.
            if (SaveManager.Instance != null)
                SaveManager.Instance.OnLoaded += ResetStats;

            ResetStats();
        }

        private void HandleDayStarted(int day) => ResetStats();

        private void ResetStats()
        {
            CustomersServed = 0;
            CustomersLost = 0;
            Revenue = 0;
            Expenses = 0;
            GamblingNet = 0;
            ProtectionPaid = 0;
            ReputationAtDayStart = CurrentReputation;
        }

        private void HandleMoneySpent(int amount) => Expenses += amount;

        /// <summary>
        /// Bahis TrySpendMoney'den gectigi icin Expenses'a da yazilmisti;
        /// kumar satirinda gosterilecegi icin oradan geri cikariyoruz (cift sayim olmasin).
        /// </summary>
        private void HandleGamblingRound(GamblingRoundResult result)
        {
            Expenses -= result.Stake;
            GamblingNet += result.Net;
        }

        /// <summary>
        /// Kumarhane masa parasi da "Kumar" satirina eklenir. AddMoney ile girdigi icin
        /// Expenses'a hic yazilmaz; cift sayim duzeltmesi gerekmez.
        /// </summary>
        private void HandleHouseRound(int rake) => GamblingNet += rake;

        /// <summary>Harac da TrySpendMoney'den gecer; kendi satirinda gosterilecegi icin giderden dusulur.</summary>
        private void HandleProtectionPaid(int amount)
        {
            Expenses -= amount;
            ProtectionPaid += amount;
        }

        private void HandleCustomerSpawned(Customer customer)
        {
            if (customer == null) return;

            customer.OnLeft += HandleCustomerLeft;
        }

        private void HandleCustomerLeft(Customer customer)
        {
            customer.OnLeft -= HandleCustomerLeft;

            // 6a'da eklenen WasServed/PaidAmount sayesinde ekstra event'e gerek kalmadi.
            if (customer.WasServed)
            {
                CustomersServed++;
                Revenue += customer.PaidAmount;
            }
            else
            {
                CustomersLost++;
            }
        }

        private void OnDestroy()
        {
            if (spawner != null) spawner.OnCustomerSpawned -= HandleCustomerSpawned;
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneySpent -= HandleMoneySpent;
            if (DayCycleManager.Instance != null) DayCycleManager.Instance.OnDayStarted -= HandleDayStarted;
            if (SaveManager.Instance != null) SaveManager.Instance.OnLoaded -= ResetStats;
            if (MafiaManager.Instance != null) MafiaManager.Instance.OnDemandPaid -= HandleProtectionPaid;

            GamblingTable.AnyRoundPlayed -= HandleGamblingRound;
            GamblingTable.AnyHouseRound -= HandleHouseRound;

            if (Instance == this) Instance = null;
        }
    }
}
