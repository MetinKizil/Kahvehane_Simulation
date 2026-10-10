using System;
using System.Collections;
using KanBagi.Core;
using KanBagi.Economy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Polis baskinlarini yurutur. Baskin ihtimali SUPHE seviyesinden gelir:
    /// ne kadar cok kumar oynarsan o kadar sik gelirler.
    ///
    /// Akis: gun basinda zar atilir -> gun icinde rastgele bir saatte UYARI verilir ->
    /// oyuncunun masayi ortmesi icin sure taninir -> memur gelir -> denetim -> sonuc.
    /// Yani yakalanmak sansa degil, oyuncunun tepkisine baglidir.
    /// </summary>
    public class PoliceManager : MonoBehaviour
    {
        public static PoliceManager Instance { get; private set; }

        [Header("Baskin Ihtimali")]
        [Tooltip("Suphe orani bunun altindayken polis hic gelmez.")]
        [SerializeField] [Range(0f, 1f)] private float minimumSuspicionRatio = 0.2f;
        [Tooltip("Suphe tavandayken gunluk baskin ihtimali.")]
        [SerializeField] [Range(0f, 1f)] private float maxRaidChance = 0.6f;

        [Header("Zamanlama")]
        [Tooltip("Uyaridan sonra memurun gelmesi icin gecen sure (saniye). Ortuyu kapatma suresi.")]
        [SerializeField] private float warningSeconds = 20f;
        [Tooltip("Memurun dukkanda kalip etrafa bakma suresi (saniye).")]
        [SerializeField] private float inspectionSeconds = 12f;
        [Tooltip("Baskin bu gunden once olmaz (oyunun ilk gunleri rahat gecsin).")]
        [SerializeField] private int earliestRaidDay = 4;

        [Header("Ceza")]
        [Tooltip("Kumar masasi ortusuz yakalanirsa kesilen para cezasi.")]
        [SerializeField] private int fineAmount = 1000;
        [Tooltip("Ceza kasada en az bu kadar para birakir. Malzeme alacak para kalsin, oyun kilitlenmesin.")]
        [SerializeField] private int protectedMoney = 150;
        [SerializeField] private int reputationPenalty = 10;
        [Tooltip("Temiz gecen kontrolden sonra suphenin dusecegi miktar.")]
        [SerializeField] private int cleanInspectionRelief = 15;

        [Header("Memur")]
        [SerializeField] private GameObject officerPrefab;
        [Tooltip("Memurun belirecegi nokta. Bos ise bu objenin konumu.")]
        [SerializeField] private Transform spawnPoint;

        private GameObject activeOfficer;

        /// <summary>Bugun kesilen ceza. Gunluk ozet bunu okur.</summary>
        public int FinePaidToday { get; private set; }

        /// <summary>Uyari verildiginde, oyuncunun kac saniyesi oldugu ile tetiklenir.</summary>
        public event Action<float> OnRaidWarning;

        /// <summary>Memur dukkana girdiginde tetiklenir.</summary>
        public event Action OnOfficerArrived;

        /// <summary>Denetim bitince (yakalandi mi, ne kadar ceza) ile tetiklenir.</summary>
        public event Action<bool, int> OnRaidResolved;

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
            if (officerPrefab == null)
                Debug.LogError("[PoliceManager] 'Officer Prefab' atanmamis. Polis prefab'ini surukle.", this);

            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted += HandleDayStarted;
            else
                Debug.LogError("[PoliceManager] DayCycleManager bulunamadi.", this);
        }

        private void HandleDayStarted(int day)
        {
            FinePaidToday = 0;

            // Onceki gunden sarkan bir baskin kalmasin.
            StopAllCoroutines();
            DespawnOfficer();

            if (!ShouldRaid(day)) return;

            StartCoroutine(RaidRoutine());
        }

        /// <summary>Zar: suphe ne kadar yuksekse ihtimal o kadar buyuk.</summary>
        private bool ShouldRaid(int day)
        {
            if (day < earliestRaidDay) return false;
            if (SuspicionManager.Instance == null) return false;

            float ratio = SuspicionManager.Instance.SuspicionRatio;
            if (ratio < minimumSuspicionRatio) return false;

            return Random.value < ratio * maxRaidChance;
        }

        private IEnumerator RaidRoutine()
        {
            DayCycleManager dayCycle = DayCycleManager.Instance;
            if (dayCycle == null) yield break;

            // Dukkan acikken rastgele bir saat sec; uyari ve denetim icin pay birak.
            float targetHour = Random.Range(dayCycle.OpenHour + 1f, Mathf.Max(dayCycle.OpenHour + 2f, dayCycle.CloseHour - 2f));

            while (dayCycle.TimeOfDay < targetHour)
                yield return null;

            // UYARI: oyuncunun masayi ortmesi icin suresi baslar.
            OnRaidWarning?.Invoke(warningSeconds);
            yield return new WaitForSeconds(warningSeconds);

            SpawnOfficer();
            OnOfficerArrived?.Invoke();

            yield return new WaitForSeconds(inspectionSeconds);

            ResolveRaid();
            DespawnOfficer();
        }

        /// <summary>Denetim: ACIK duran (pasif olmayan) ve ortusuz bir kumar masasi var mi?</summary>
        private void ResolveRaid()
        {
            bool busted = false;

            foreach (GamblingTable table in FindObjectsByType<GamblingTable>(FindObjectsSortMode.None))
            {
                if (table == null || table.IsHidden) continue;

                busted = true;
                break;
            }

            int fine = busted ? ApplyPenalty() : ApplyReliefAndGetZero();
            OnRaidResolved?.Invoke(busted, fine);
        }

        private int ApplyPenalty()
        {
            int fine = 0;

            if (EconomyManager.Instance != null && fineAmount > 0)
            {
                // Kasayi tamamen bosaltmazlar: protectedMoney kadari kalir (iflas kilidi olmasin).
                int seizable = Mathf.Max(0, EconomyManager.Instance.CurrentMoney - protectedMoney);
                fine = Mathf.Min(fineAmount, seizable);
                if (fine > 0) EconomyManager.Instance.TrySpendMoney(fine);
            }

            FinePaidToday += fine;

            if (reputationPenalty > 0 && ReputationManager.Instance != null)
                ReputationManager.Instance.AddReputation(-reputationPenalty);

            // Yakalandiktan sonra dosya kapanir: isi sifirlanir.
            if (SuspicionManager.Instance != null)
                SuspicionManager.Instance.Reduce(SuspicionManager.Instance.CurrentSuspicion);

            return fine;
        }

        private int ApplyReliefAndGetZero()
        {
            if (cleanInspectionRelief > 0 && SuspicionManager.Instance != null)
                SuspicionManager.Instance.Reduce(cleanInspectionRelief);

            return 0;
        }

        private void SpawnOfficer()
        {
            if (officerPrefab == null) return;

            DespawnOfficer();
            activeOfficer = Instantiate(officerPrefab, SpawnPosition, SpawnRotation);
        }

        private void DespawnOfficer()
        {
            if (activeOfficer == null) return;

            Destroy(activeOfficer);
            activeOfficer = null;
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted -= HandleDayStarted;

            if (Instance == this) Instance = null;
        }
    }
}
