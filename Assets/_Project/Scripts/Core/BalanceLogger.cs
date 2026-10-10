using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using KanBagi.Data;
using KanBagi.Economy;
using KanBagi.Illegal;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Denge olcumu icin oyun gunlugu tutar. Hicbir sisteme karismaz, sadece event dinler (Observer Pattern).
    ///
    /// Iki tur satir yazar:
    ///   GUN  - her gun sonunda gunun muhasebesi (DailyStatsManager'dan okunur)
    ///   OLAY - kilometre taslari: itibar baraji, upgrade/mulk alimi, mafya, polis
    ///
    /// Editor'de proje klasorundeki DengeLog/ altina, build'de persistentDataPath'e yazar.
    ///
    /// Execution order 200: SaveManager (100) kaydi yukledikten SONRA abone olur.
    /// Yoksa kayittan geri gelen upgrade/mulkler "yeni satin alindi" diye loglanirdi.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class BalanceLogger : MonoBehaviour
    {
        private const string CsvHeader =
            "tur,gun,saat,gercek_dk,aciklama,para,itibar,suphe,servis,kacan,ciro,gider,kira,kumar,harac,ceza,net";

        [Header("Dosya")]
        [SerializeField] private string fileName = "denge_log.csv";
        [Tooltip("Kapaliysa sadece Console'a yazar.")]
        [SerializeField] private bool writeToFile = true;

        [Header("Kilometre Taslari")]
        [Tooltip("Itibar bu degerleri ilk kez gectiginde OLAY satiri yazilir.")]
        [SerializeField] private int[] reputationMilestones = { 10, 20, 30, 50 };

        private readonly HashSet<int> passedMilestones = new HashSet<int>();
        private float sessionStartTime;
        private string filePath;

        private void Start()
        {
            sessionStartTime = Time.realtimeSinceStartup;
            filePath = ResolveFilePath();

            // Kayit zaten yuklendi (order 200). Gecilmis barajlar tekrar loglanmasin.
            int reputation = CurrentReputation;
            foreach (int milestone in reputationMilestones)
            {
                if (reputation >= milestone) passedMilestones.Add(milestone);
            }

            Subscribe();
            WriteEvent($"OTURUM BASLADI {DateTime.Now:yyyy-MM-dd HH:mm}");
        }

        private void Subscribe()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayEnded += HandleDayEnded;
            else
                Debug.LogError("[BalanceLogger] DayCycleManager bulunamadi; gun satirlari yazilamaz.", this);

            if (ReputationManager.Instance != null)
                ReputationManager.Instance.OnReputationChanged += HandleReputationChanged;

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;

            if (PropertyManager.Instance != null)
                PropertyManager.Instance.OnPropertyPurchased += HandlePropertyPurchased;

            if (MafiaManager.Instance != null)
            {
                MafiaManager.Instance.OnDemandArrived += HandleDemandArrived;
                MafiaManager.Instance.OnDemandPaid += HandleDemandPaid;
                MafiaManager.Instance.OnDemandRefused += HandleDemandRefused;
            }

            if (PoliceManager.Instance != null)
            {
                PoliceManager.Instance.OnRaidWarning += HandleRaidWarning;
                PoliceManager.Instance.OnRaidResolved += HandleRaidResolved;
            }
        }

        private void HandleDayEnded(int day)
        {
            DailyStatsManager stats = DailyStatsManager.Instance;
            if (stats == null)
            {
                WriteEvent("GUN BITTI (DailyStatsManager yok)");
                return;
            }

            string[] columns =
            {
                "GUN", day.ToString(), Clock, RealMinutes, "",
                CurrentMoney.ToString(), CurrentReputation.ToString(), CurrentSuspicion.ToString(),
                stats.CustomersServed.ToString(), stats.CustomersLost.ToString(),
                stats.Revenue.ToString(), stats.Expenses.ToString(), stats.RentIncome.ToString(),
                stats.GamblingNet.ToString(), stats.ProtectionPaid.ToString(), stats.FinePaid.ToString(),
                stats.NetProfit.ToString()
            };

            WriteLine(columns,
                $"[Denge] Gun {day} bitti | para {CurrentMoney} | itibar {CurrentReputation} " +
                $"| servis {stats.CustomersServed} / kacan {stats.CustomersLost} | net {stats.NetProfit}");
        }

        private void HandleReputationChanged(int reputation)
        {
            foreach (int milestone in reputationMilestones)
            {
                if (reputation < milestone || passedMilestones.Contains(milestone)) continue;

                passedMilestones.Add(milestone);
                WriteEvent($"Itibar {milestone} gecildi");
            }
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            if (upgrade == null) return;
            WriteEvent($"Upgrade alindi: {upgrade.UpgradeName} ({upgrade.Cost} TL)");
        }

        private void HandlePropertyPurchased(PropertyData property)
        {
            if (property == null) return;
            WriteEvent($"Mulk alindi: {property.PropertyName} ({property.PurchasePrice} TL)");
        }

        private void HandleDemandArrived(int amount) => WriteEvent($"Mafya geldi: {amount} TL harac");
        private void HandleDemandPaid(int amount) => WriteEvent($"Harac odendi: {amount} TL");
        private void HandleDemandRefused(int amount) => WriteEvent($"Harac reddedildi: {amount} TL");

        private void HandleRaidWarning(float seconds) => WriteEvent($"Polis uyarisi ({seconds:0} sn)");

        private void HandleRaidResolved(bool busted, int fine)
        {
            WriteEvent(busted ? $"Polis baskini: YAKALANDI, ceza {fine} TL" : "Polis baskini: temiz gecti");
        }

        /// <summary>Kilometre tasi satiri: sadece aciklama, para, itibar ve suphe dolu.</summary>
        private void WriteEvent(string description)
        {
            string[] columns =
            {
                "OLAY", CurrentDay.ToString(), Clock, RealMinutes, Sanitize(description),
                CurrentMoney.ToString(), CurrentReputation.ToString(), CurrentSuspicion.ToString(),
                "", "", "", "", "", "", "", "", ""
            };

            WriteLine(columns, $"[Denge] Gun {CurrentDay} {Clock} - {description}");
        }

        private void WriteLine(string[] columns, string consoleMessage)
        {
            Debug.Log(consoleMessage, this);

            if (!writeToFile || string.IsNullOrEmpty(filePath)) return;

            try
            {
                var builder = new StringBuilder();
                if (!File.Exists(filePath)) builder.AppendLine(CsvHeader);

                builder.AppendLine(string.Join(",", columns));
                File.AppendAllText(filePath, builder.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BalanceLogger] Log yazilamadi ({filePath}): {exception.Message}", this);
                writeToFile = false;
            }
        }

        private string ResolveFilePath()
        {
            try
            {
#if UNITY_EDITOR
                // Proje klasorunun kokunde DengeLog/ (Assets'in yani). Git bunu yok sayar.
                string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "DengeLog");
#else
                string folder = Application.persistentDataPath;
#endif
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, fileName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[BalanceLogger] Log klasoru olusturulamadi: {exception.Message}", this);
                return null;
            }
        }

        private static string Sanitize(string text) => text.Replace(",", ";");

        private int CurrentDay => DayCycleManager.Instance != null ? DayCycleManager.Instance.CurrentDay : 0;
        private string Clock => DayCycleManager.Instance != null ? DayCycleManager.Instance.GetClockLabel() : "--:--";
        private int CurrentMoney => EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney : 0;
        private int CurrentReputation => ReputationManager.Instance != null ? ReputationManager.Instance.CurrentReputation : 0;
        private int CurrentSuspicion => SuspicionManager.Instance != null ? SuspicionManager.Instance.CurrentSuspicion : 0;

        private string RealMinutes =>
            ((Time.realtimeSinceStartup - sessionStartTime) / 60f).ToString("0.0", CultureInfo.InvariantCulture);

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnDayEnded -= HandleDayEnded;

            if (ReputationManager.Instance != null)
                ReputationManager.Instance.OnReputationChanged -= HandleReputationChanged;

            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;

            if (PropertyManager.Instance != null)
                PropertyManager.Instance.OnPropertyPurchased -= HandlePropertyPurchased;

            if (MafiaManager.Instance != null)
            {
                MafiaManager.Instance.OnDemandArrived -= HandleDemandArrived;
                MafiaManager.Instance.OnDemandPaid -= HandleDemandPaid;
                MafiaManager.Instance.OnDemandRefused -= HandleDemandRefused;
            }

            if (PoliceManager.Instance != null)
            {
                PoliceManager.Instance.OnRaidWarning -= HandleRaidWarning;
                PoliceManager.Instance.OnRaidResolved -= HandleRaidResolved;
            }
        }
    }
}
