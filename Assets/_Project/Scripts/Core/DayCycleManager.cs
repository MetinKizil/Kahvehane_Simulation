using System;
using System.Collections;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Oyun zamaninin TEK sahibi. Gun 00:00'dan 24:00'a kesintisiz akar;
    /// dukkan bunun icindeki acilis-kapanis penceresinde calisir.
    /// Kimin ne yaptigini bilmez, sadece zamani yayinlar (Observer Pattern).
    /// Faz 4'un gece kumarhanesi de bu event'lere abone olacak.
    /// </summary>
    public class DayCycleManager : MonoBehaviour, ISaveable
    {
        /// <summary>Kayit dosyasindaki gun bolumu. Saat kaydedilmez; gun bastan baslar.</summary>
        [Serializable]
        private class DaySaveData
        {
            public int day;
        }

        public static DayCycleManager Instance { get; private set; }

        [Header("Gun Uzunlugu")]
        [Tooltip("Bir oyun gununun gercek suresi (dakika). Gece hizlandirmasi bu sureyi kisaltir.")]
        [SerializeField] private float dayDurationMinutes = 6f;
        [Tooltip("Dukkan kapaliyken zaman kac kat hizli aksin. 1 = hizlandirma yok.")]
        [SerializeField] private float nightTimeScale = 4f;

        [Header("Dukkan Saatleri")]
        [Tooltip("Dukkanin acildigi saat.")]
        [SerializeField] private float openHour = 8f;
        [Tooltip("Dukkanin kapandigi saat. Icerideki musteriler bu saatte kalkip cikar.")]
        [SerializeField] private float closeHour = 22f;
        [Tooltip("Kapanistan kac saat once yeni musteri alinmayi kessin (son musteri saati).")]
        [SerializeField] private float lastCallHours = 1f;

        [Header("Gun Sonu")]
        [Tooltip("Gun bitince oyuncu 'Yeni Gun' diyene kadar beklensin mi? Kapaliysa gunler kesintisiz akar.")]
        [SerializeField] private bool waitForNextDayRequest = true;

        [Header("Baslangic")]
        [SerializeField] private int startingDay = 1;

        private bool isNextDayRequested;

        public int CurrentDay { get; private set; }

        /// <summary>Gunun kacinci saati (0-24 arasi, ondalikli).</summary>
        public float TimeOfDay { get; private set; }

        /// <summary>Gunun ne kadari gecti (0 = gece yarisi, 1 = ertesi gece yarisi).</summary>
        public float DayProgress => TimeOfDay / 24f;

        /// <summary>Dukkan su anda acik mi?</summary>
        public bool IsShopOpen { get; private set; }

        /// <summary>Yeni musteri kabul ediliyor mu? Kapanisa yaklasinca kapanir.</summary>
        public bool AcceptsNewCustomers => IsShopOpen && TimeOfDay < closeHour - lastCallHours;

        /// <summary>Yeni gun (00:00) basladiginda tetiklenir.</summary>
        public event Action<int> OnDayStarted;

        /// <summary>Dukkan acilinca tetiklenir.</summary>
        public event Action<int> OnShopOpened;

        /// <summary>Kapanis saati geldiginde tetiklenir. Musteriler bunu duyunca kalkar.</summary>
        public event Action<int> OnShopClosed;

        /// <summary>Gun 24:00'te bitince BITEN gunun numarasi ile tetiklenir. Gunluk ozet buna abone olur.</summary>
        public event Action<int> OnDayEnded;

        /// <summary>Gun bitti ve oyuncunun "Yeni Gun" demesi bekleniyor mu?</summary>
        public bool IsWaitingForNextDay { get; private set; }

        /// <summary>Saat ilerledikce gunun orani (0-1) ile tetiklenir.</summary>
        public event Action<float> OnTimeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            CurrentDay = Mathf.Max(1, startingDay);
        }

        private void Start()
        {
            StartCoroutine(DayRoutine());
        }

        /// <summary>Ekranda gosterilecek saat metni ("14:30").</summary>
        public string GetClockLabel()
        {
            float clamped = Mathf.Clamp(TimeOfDay, 0f, 23.9999f);
            int hour = Mathf.FloorToInt(clamped);
            int minute = Mathf.FloorToInt((clamped - hour) * 60f);

            return $"{hour:00}:{minute:00}";
        }

        /// <summary>Gunluk ozet paneli "Yeni Gun" butonunda bunu cagirir.</summary>
        public void RequestNextDay()
        {
            isNextDayRequested = true;
        }

        private IEnumerator DayRoutine()
        {
            while (true)
            {
                StartDay();
                yield return RunDayRoutine();

                // Gun numarasi bekleme bitene kadar artmaz: ozet paneli BITEN gunu gostersin.
                OnDayEnded?.Invoke(CurrentDay);
                yield return WaitForNextDayRoutine();

                CurrentDay++;
            }
        }

        private IEnumerator WaitForNextDayRoutine()
        {
            if (!waitForNextDayRequest) yield break;

            // Gun sonunu dinleyen kimse yoksa (ozet paneli sahneden silinmis olabilir)
            // beklemek oyunu sessizce kilitler. Uyarip gunu kendiliginden devrediyoruz.
            if (OnDayEnded == null)
            {
                Debug.LogWarning("[DayCycleManager] Gun sonunu dinleyen yok - 'GunlukOzetPaneli' Canvas altinda mi? " +
                                 "Beklemeden yeni gune geciliyor.", this);
                yield break;
            }

            IsWaitingForNextDay = true;
            isNextDayRequested = false;

            while (!isNextDayRequested)
                yield return null;

            IsWaitingForNextDay = false;
        }

        private void StartDay()
        {
            TimeOfDay = 0f;
            IsShopOpen = false;

            OnDayStarted?.Invoke(CurrentDay);
            OnTimeChanged?.Invoke(DayProgress);
        }

        private IEnumerator RunDayRoutine()
        {
            // Gunun tamami dayDurationMinutes'te akar; gece bolumunde zaman hizlandirilir.
            float hoursPerSecond = 24f / Mathf.Max(1f, dayDurationMinutes * 60f);

            while (TimeOfDay < 24f)
            {
                float scale = IsShopOpen ? 1f : Mathf.Max(1f, nightTimeScale);
                TimeOfDay = Mathf.Min(24f, TimeOfDay + Time.deltaTime * hoursPerSecond * scale);

                RefreshShopState();
                OnTimeChanged?.Invoke(DayProgress);

                yield return null;
            }
        }

        /// <summary>Acilis/kapanis esigi gecildiyse ilgili event'i bir kere yayinlar.</summary>
        private void RefreshShopState()
        {
            bool shouldBeOpen = TimeOfDay >= openHour && TimeOfDay < closeHour;
            if (shouldBeOpen == IsShopOpen) return;

            IsShopOpen = shouldBeOpen;

            if (IsShopOpen)
                OnShopOpened?.Invoke(CurrentDay);
            else
                OnShopClosed?.Invoke(CurrentDay);
        }

        public string SaveKey => "dayCycle";

        public string CaptureState() => JsonUtility.ToJson(new DaySaveData { day = CurrentDay });

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<DaySaveData>(state);
            if (data == null) return;

            CurrentDay = Mathf.Max(1, data.day);
            OnTimeChanged?.Invoke(DayProgress);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
