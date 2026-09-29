using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Oyunu diske kaydeder ve geri yukler.
    /// Hicbir sistemin ne tuttugunu bilmez; sahnedeki ISaveable'lari bulup
    /// herkesin kendi JSON'unu ister (SOLID).
    ///
    /// Execution order 100: tum sistemlerin Start'i bittikten SONRA yukler,
    /// boylece kilit acan/etki uygulayan dinleyiciler hazir olur.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SaveManager : MonoBehaviour
    {
        /// <summary>Kayit dosyasindaki tek bir sistemin bolumu.</summary>
        [Serializable]
        private class SaveEntry
        {
            public string key;
            public string json;
        }

        [Serializable]
        private class SaveFile
        {
            public int version = 1;
            public List<SaveEntry> entries = new List<SaveEntry>();
        }

        public static SaveManager Instance { get; private set; }

        [Header("Dosya")]
        [SerializeField] private string fileName = "kanbagi_save.json";

        [Header("Davranis")]
        [Tooltip("Oyun acilirken kayit varsa otomatik yuklensin mi?")]
        [SerializeField] private bool loadOnStart = true;
        [Tooltip("Yeni gun baslarken otomatik kaydedilsin mi?")]
        [SerializeField] private bool saveOnNewDay = true;

        public string SavePath => Path.Combine(Application.persistentDataPath, fileName);
        public bool HasSave => File.Exists(SavePath);

        /// <summary>Kayit basariyla yazildiginda tetiklenir. UI "Kaydedildi" yazmak icin dinler.</summary>
        public event Action OnSaved;

        /// <summary>Kayit yuklendiginde tetiklenir.</summary>
        public event Action OnLoaded;

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
            if (loadOnStart && HasSave) Load();

            // OnDayEnded'a DEGIL OnDayStarted'a baglaniyoruz: gun sayaci ancak oyuncu
            // "Yeni Gun" dedikten sonra artar, bu yuzden gun sonunda kaydetmek
            // diske hep bir onceki gunu yazardi.
            // Ilk gunun OnDayStarted'i DayCycleManager.Start icinde, bizden once (execution order 100)
            // tetiklendigi icin acilista bosuna kayit olusmaz.
            if (saveOnNewDay && DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted += HandleDayStarted;
        }

        private void HandleDayStarted(int day) => Save();

        /// <summary>Tum sistemlerin durumunu diske yazar.</summary>
        public void Save()
        {
            var file = new SaveFile();

            foreach (ISaveable saveable in FindSaveables())
            {
                file.entries.Add(new SaveEntry
                {
                    key = saveable.SaveKey,
                    json = saveable.CaptureState()
                });
            }

            try
            {
                File.WriteAllText(SavePath, JsonUtility.ToJson(file, prettyPrint: true));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveManager] Kayit yazilamadi: {exception.Message}", this);
                return;
            }

            OnSaved?.Invoke();
        }

        /// <summary>Diskteki kaydi yukler. Kayit yoksa veya bozuksa false doner.</summary>
        public bool Load()
        {
            if (!HasSave) return false;

            SaveFile file;

            try
            {
                file = JsonUtility.FromJson<SaveFile>(File.ReadAllText(SavePath));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveManager] Kayit okunamadi: {exception.Message}", this);
                return false;
            }

            if (file == null || file.entries == null) return false;

            var applied = new List<string>();
            var missing = new List<string>();

            foreach (ISaveable saveable in FindSaveables())
            {
                SaveEntry entry = file.entries.Find(candidate => candidate != null && candidate.key == saveable.SaveKey);

                if (entry == null || string.IsNullOrEmpty(entry.json))
                {
                    missing.Add(saveable.SaveKey);
                    continue;
                }

                saveable.RestoreState(entry.json);
                applied.Add(saveable.SaveKey);
            }

            // Teshis: hangi bolum yuklendi, hangisi kayitta yoktu.
            string missingNote = missing.Count > 0 ? $" | Kayitta YOK: {string.Join(", ", missing)}" : string.Empty;
            Debug.Log($"[SaveManager] Yuklendi: {string.Join(", ", applied)}{missingNote}");

            OnLoaded?.Invoke();
            return true;
        }

        public void DeleteSave()
        {
            if (!HasSave) return;

            File.Delete(SavePath);
        }

        /// <summary>
        /// Sahnedeki tum ISaveable'lari bulur. Sadece kayit/yukleme aninda calistigi icin
        /// maliyeti onemsizdir. Pasif objeler de dahil edilir (kilitli istasyonlar vb.).
        /// </summary>
        private static IEnumerable<ISaveable> FindSaveables()
        {
            return FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<ISaveable>();
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayStarted -= HandleDayStarted;

            if (Instance == this) Instance = null;
        }
    }
}
