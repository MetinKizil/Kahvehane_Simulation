using System.Collections;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Illegal;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Mahalle fisiltilari: oyundaki olaylara intikam tonunda kisa bir satirla eslik eder.
    /// SADECE event dinler ve gosterir; hicbir oyun durumunu degistirmez (ANAYASA: UI mantiktan ayri).
    /// Ayni anda birden fazla mesaj gelirse sirayla gosterilir.
    ///
    /// Order 200: SaveManager (100) kaydi yukledikten SONRA abone olur. Yoksa kayittan geri gelen
    /// mulk/upgrade'ler "yeni alindi", gecilmis itibar barajlari "yeni gecildi" diye fisildanirdi.
    /// Kurulum: Kan Bagi > Fisilti Satirini Olustur
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class WhisperUI : MonoBehaviour
    {
        [Header("Veri")]
        [SerializeField] private WhisperData whispers;

        [Header("Baglantilar")]
        [Tooltip("Bos birakilirsa bu objenin uzerindeki TMP_Text kullanilir.")]
        [SerializeField] private TMP_Text messageText;

        [Header("Gorunum")]
        [Tooltip("Her mesajin ekranda kalma suresi (saniye).")]
        [SerializeField] private float displayDuration = 6f;
        [Tooltip("Sirada bekleyen iki mesaj arasindaki bosluk (saniye).")]
        [SerializeField] private float gapBetween = 0.5f;

        private readonly Queue<string> pending = new Queue<string>();
        private Coroutine showRoutine;
        private int lastReputation;

        private DayCycleManager dayCycle;
        private MafiaManager mafia;
        private PropertyManager properties;
        private UpgradeManager upgrades;
        private ReputationManager reputation;

        private void Start()
        {
            if (messageText == null) messageText = GetComponent<TMP_Text>();

            if (messageText == null || whispers == null)
            {
                Debug.LogError("[WhisperUI] 'Message Text' veya 'Whispers' atanmamis. " +
                               "'Kan Bagi > Fisilti Satirini Olustur' menusunu calistir.", this);
                enabled = false;
                return;
            }

            messageText.enabled = false;

            dayCycle = DayCycleManager.Instance;
            mafia = MafiaManager.Instance;
            properties = PropertyManager.Instance;
            upgrades = UpgradeManager.Instance;
            reputation = ReputationManager.Instance;

            // Eksik manager sadece kendi fisiltisini susturur; digerleri calismaya devam eder.
            if (dayCycle != null) dayCycle.OnShopOpened += HandleShopOpened;

            if (mafia != null)
            {
                mafia.OnDemandArrived += HandleDemandArrived;
                mafia.OnDemandPaid += HandleDemandPaid;
                mafia.OnDemandRefused += HandleDemandRefused;
            }

            if (properties != null) properties.OnPropertyPurchased += HandlePropertyPurchased;
            if (upgrades != null) upgrades.OnUpgradePurchased += HandleUpgradePurchased;

            if (reputation != null)
            {
                lastReputation = reputation.CurrentReputation;
                reputation.OnReputationChanged += HandleReputationChanged;
            }
        }

        private void HandleShopOpened(int day)
        {
            if (day == 1) Enqueue(whispers.FirstMorning);
        }

        private void HandleDemandArrived(int amount) => Enqueue(whispers.MafiaArrived);

        private void HandleDemandPaid(int amount) => Enqueue(whispers.MafiaPaid);

        private void HandleDemandRefused(int amount) => Enqueue(whispers.MafiaRefused);

        private void HandlePropertyPurchased(PropertyData property)
        {
            if (property == null || string.IsNullOrEmpty(whispers.PropertyPurchasedFormat)) return;

            Enqueue(string.Format(whispers.PropertyPurchasedFormat, property.PropertyName));
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            if (upgrade != null && upgrade == whispers.GamblingHallUpgrade) Enqueue(whispers.GamblingHallOpened);
        }

        /// <summary>Sadece baraj ASAGIDAN YUKARI gecilince fisildanir; dususte veya ayni degerde sessiz.</summary>
        private void HandleReputationChanged(int current)
        {
            int previous = lastReputation;
            lastReputation = current;

            foreach (ReputationWhisper whisper in whispers.ReputationWhispers)
            {
                if (previous < whisper.Threshold && current >= whisper.Threshold) Enqueue(whisper.Message);
            }
        }

        private void Enqueue(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            pending.Enqueue(message);
            if (showRoutine == null) showRoutine = StartCoroutine(ShowRoutine());
        }

        private IEnumerator ShowRoutine()
        {
            while (pending.Count > 0)
            {
                messageText.text = pending.Dequeue();
                messageText.enabled = true;

                yield return new WaitForSeconds(displayDuration);

                messageText.enabled = false;

                yield return new WaitForSeconds(gapBetween);
            }

            showRoutine = null;
        }

        private void OnDestroy()
        {
            if (dayCycle != null) dayCycle.OnShopOpened -= HandleShopOpened;

            if (mafia != null)
            {
                mafia.OnDemandArrived -= HandleDemandArrived;
                mafia.OnDemandPaid -= HandleDemandPaid;
                mafia.OnDemandRefused -= HandleDemandRefused;
            }

            if (properties != null) properties.OnPropertyPurchased -= HandlePropertyPurchased;
            if (upgrades != null) upgrades.OnUpgradePurchased -= HandleUpgradePurchased;
            if (reputation != null) reputation.OnReputationChanged -= HandleReputationChanged;
        }
    }
}
