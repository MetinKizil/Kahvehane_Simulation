using System;
using KanBagi.Core;
using KanBagi.Economy;
using UnityEngine;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Mafyanin harac dongusunun TEK sahibi.
    /// Ilk gunden gelmez: belli bir ITIBAR ve SERVET barajini astiginda devreye girer,
    /// sonra duzenli araliklarla tahsildar gonderir.
    /// Odemezsen ayri bir "reddet" tusu yok - gormezden gelmek reddetmektir.
    /// </summary>
    public class MafiaManager : MonoBehaviour, ISaveable
    {
        /// <summary>Kayit dosyasindaki mafya bolumu.</summary>
        [Serializable]
        private class MafiaSaveData
        {
            public bool isActive;
            public bool isDemandActive;
            public int currentDemand;
            public int nextVisitDay;
            public int refusalCount;
        }

        public static MafiaManager Instance { get; private set; }

        [Header("Tetikleme Baraji")]
        [Tooltip("Mafyanin dikkatini cekmek icin gereken itibar.")]
        [SerializeField] private int requiredReputation = 30;
        [Tooltip("Mafyanin dikkatini cekmek icin gereken para.")]
        [SerializeField] private int requiredMoney = 2000;

        [Header("Zamanlama")]
        [Tooltip("Baraj asildiktan KAC GUN SONRA ilk tahsildar gelsin. Mafya once duyar, sonra gelir.")]
        [SerializeField] private int firstVisitDelayDays = 2;
        [Tooltip("Mafya bu gunden once asla gelmez (oyunun ilk gunleri rahat gecsin).")]
        [SerializeField] private int earliestVisitDay = 3;
        [Tooltip("Iki ziyaret arasindaki gun sayisi.")]
        [SerializeField] private int visitIntervalDays = 3;

        [Header("Harac")]
        [SerializeField] private int baseDemand = 500;
        [Tooltip("Harac reddedilirse sonraki istek bu katsayiyla buyur.")]
        [SerializeField] private float refusalMultiplier = 1.5f;

        [Header("Reddetme Bedeli")]
        [SerializeField] private int reputationPenalty = 5;
        [SerializeField] private int suspicionPenalty = 10;

        [Header("Tahsildar")]
        [Tooltip("Harac isterken sahneye gelen adam.")]
        [SerializeField] private GameObject collectorPrefab;
        [Tooltip("Tahsildarin belirecegi nokta (kapinin yani). Bos ise bu objenin konumu.")]
        [SerializeField] private Transform spawnPoint;

        private GameObject activeCollector;
        private int refusalCount;
        private int nextVisitDay;

        /// <summary>Mafya devreye girdi mi? (Baraj asildi mi?)</summary>
        public bool IsActive { get; private set; }

        /// <summary>Su anda odenmemis bir harac var mi?</summary>
        public bool IsDemandActive { get; private set; }

        public int CurrentDemand { get; private set; }

        /// <summary>Bugun odenen harac. Gunluk ozet bunu okur.</summary>
        public int PaidToday { get; private set; }

        public event Action<int> OnDemandArrived;
        public event Action<int> OnDemandPaid;
        public event Action<int> OnDemandRefused;

        private Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        private Quaternion SpawnRotation => spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

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
            if (collectorPrefab == null)
                Debug.LogError("[MafiaManager] 'Collector Prefab' atanmamis. Tahsildar prefab'ini surukle.", this);

            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted += HandleDayStarted;
            else
                Debug.LogError("[MafiaManager] DayCycleManager bulunamadi.", this);
        }

        /// <summary>Tahsildar ile konusulunca cagrilir.</summary>
        public bool PayDemand()
        {
            if (!IsDemandActive || EconomyManager.Instance == null) return false;
            if (!EconomyManager.Instance.TrySpendMoney(CurrentDemand)) return false;

            int paid = CurrentDemand;
            PaidToday += paid;

            ResolveDemand();
            OnDemandPaid?.Invoke(paid);
            return true;
        }

        private void HandleDayStarted(int day)
        {
            PaidToday = 0;

            // Dunden kalan harac odenmemis: reddedilmis sayilir.
            if (IsDemandActive) RefuseDemand();

            if (!IsActive) TryActivate(day);

            if (IsActive && day >= nextVisitDay) ArriveDemand(day);
        }

        /// <summary>Itibar ve servet barajini asinca mafya bizi fark eder.</summary>
        private void TryActivate(int day)
        {
            int reputation = ReputationManager.Instance != null ? ReputationManager.Instance.CurrentReputation : 0;
            int money = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney : 0;

            if (reputation < requiredReputation || money < requiredMoney) return;

            IsActive = true;

            // Baraji astigin gun kapida bitmez: once haberi olur, birkac gun sonra gelir.
            nextVisitDay = Mathf.Max(day + Mathf.Max(1, firstVisitDelayDays), earliestVisitDay);
        }

        private void ArriveDemand(int day)
        {
            CurrentDemand = CalculateDemand();
            IsDemandActive = true;
            nextVisitDay = day + Mathf.Max(1, visitIntervalDays);

            SpawnCollector();
            OnDemandArrived?.Invoke(CurrentDemand);
        }

        /// <summary>Her reddediste harac buyur.</summary>
        private int CalculateDemand()
        {
            float amount = baseDemand * Mathf.Pow(Mathf.Max(1f, refusalMultiplier), refusalCount);
            return Mathf.RoundToInt(amount);
        }

        private void RefuseDemand()
        {
            int refused = CurrentDemand;
            refusalCount++;

            if (reputationPenalty > 0 && ReputationManager.Instance != null)
                ReputationManager.Instance.AddReputation(-reputationPenalty);

            if (suspicionPenalty > 0 && SuspicionManager.Instance != null)
                SuspicionManager.Instance.Add(suspicionPenalty);

            ResolveDemand();
            OnDemandRefused?.Invoke(refused);
        }

        private void ResolveDemand()
        {
            IsDemandActive = false;
            CurrentDemand = 0;
            DespawnCollector();
        }

        private void SpawnCollector()
        {
            if (collectorPrefab == null) return;

            DespawnCollector();
            activeCollector = Instantiate(collectorPrefab, SpawnPosition, SpawnRotation);
        }

        private void DespawnCollector()
        {
            if (activeCollector == null) return;

            Destroy(activeCollector);
            activeCollector = null;
        }

        public string SaveKey => "mafia";

        public string CaptureState()
        {
            return JsonUtility.ToJson(new MafiaSaveData
            {
                isActive = IsActive,
                isDemandActive = IsDemandActive,
                currentDemand = CurrentDemand,
                nextVisitDay = nextVisitDay,
                refusalCount = refusalCount
            });
        }

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<MafiaSaveData>(state);
            if (data == null) return;

            IsActive = data.isActive;
            IsDemandActive = data.isDemandActive;
            CurrentDemand = Mathf.Max(0, data.currentDemand);
            nextVisitDay = data.nextVisitDay;
            refusalCount = Mathf.Max(0, data.refusalCount);

            // Odenmemis harac varsa tahsildar kapida beklemeye devam eder.
            if (IsDemandActive) SpawnCollector();
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted -= HandleDayStarted;

            if (Instance == this) Instance = null;
        }
    }
}
