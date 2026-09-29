using System;
using KanBagi.Core;
using UnityEngine;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Mahalledeki "suphe" (isi) seviyesinin TEK sahibi.
    /// Kumar oynandikca artar, her gun bir miktar duser.
    /// Adim 8c'de POLIS bu degeri okuyup baskin yapacak; simdilik sadece birikiyor.
    /// </summary>
    public class SuspicionManager : MonoBehaviour, ISaveable
    {
        /// <summary>Kayit dosyasindaki suphe bolumu.</summary>
        [Serializable]
        private class SuspicionSaveData
        {
            public int suspicion;
        }

        public static SuspicionManager Instance { get; private set; }

        [Header("Ayarlar")]
        [Tooltip("Suphenin ust siniri. Oran hesabi bununla yapilir.")]
        [SerializeField] private int maxSuspicion = 100;
        [Tooltip("Her gun basinda suphenin dusecegi miktar (isi soguma).")]
        [SerializeField] private int dailyDecay = 5;

        public int CurrentSuspicion { get; private set; }
        public int MaxSuspicion => maxSuspicion;

        /// <summary>0-1 arasi suphe orani. Polis riski buna bakacak.</summary>
        public float SuspicionRatio => maxSuspicion > 0 ? Mathf.Clamp01((float)CurrentSuspicion / maxSuspicion) : 0f;

        /// <summary>Suphe degistiginde yeni deger ile tetiklenir.</summary>
        public event Action<int> OnSuspicionChanged;

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
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted += HandleDayStarted;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;

            SetSuspicion(CurrentSuspicion + amount);
        }

        public void Reduce(int amount)
        {
            if (amount <= 0) return;

            SetSuspicion(CurrentSuspicion - amount);
        }

        private void HandleDayStarted(int day) => Reduce(dailyDecay);

        private void SetSuspicion(int value)
        {
            int clamped = Mathf.Clamp(value, 0, maxSuspicion);
            if (clamped == CurrentSuspicion) return;

            CurrentSuspicion = clamped;
            OnSuspicionChanged?.Invoke(CurrentSuspicion);
        }

        public string SaveKey => "suspicion";

        public string CaptureState() => JsonUtility.ToJson(new SuspicionSaveData { suspicion = CurrentSuspicion });

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<SuspicionSaveData>(state);
            if (data == null) return;

            CurrentSuspicion = Mathf.Clamp(data.suspicion, 0, maxSuspicion);
            OnSuspicionChanged?.Invoke(CurrentSuspicion);
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted -= HandleDayStarted;

            if (Instance == this) Instance = null;
        }
    }
}
