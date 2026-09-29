using System;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Mahalledeki "Itibar" puaninin tek sahibi.
    /// Faz 4'un temeli: mafya ve polis sistemleri ileride SADECE bu sinifin
    /// event'lerine abone olacak, mevcut koda dokunulmayacak (Observer Pattern).
    /// </summary>
    public class ReputationManager : MonoBehaviour, ISaveable
    {
        /// <summary>Kayit dosyasindaki itibar bolumu.</summary>
        [Serializable]
        private class ReputationSaveData
        {
            public int reputation;
        }

        public static ReputationManager Instance { get; private set; }

        [Header("Baslangic")]
        [Tooltip("Hapisten yeni cikmis birinin mahalledeki itibari... yani sifir.")]
        [SerializeField] private int startingReputation = 0;

        public int CurrentReputation { get; private set; }

        /// <summary>Itibar her degistiginde yeni deger ile tetiklenir.</summary>
        public event Action<int> OnReputationChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            CurrentReputation = startingReputation;
        }

        /// <summary>Itibar ekler veya dusurur (negatif deger verilebilir). 0'in altina inmez.</summary>
        public void AddReputation(int amount)
        {
            if (amount == 0) return;

            CurrentReputation = Mathf.Max(0, CurrentReputation + amount);
            OnReputationChanged?.Invoke(CurrentReputation);
        }

        public string SaveKey => "reputation";

        public string CaptureState() => JsonUtility.ToJson(new ReputationSaveData { reputation = CurrentReputation });

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<ReputationSaveData>(state);
            if (data == null) return;

            CurrentReputation = Mathf.Max(0, data.reputation);
            OnReputationChanged?.Invoke(CurrentReputation);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
