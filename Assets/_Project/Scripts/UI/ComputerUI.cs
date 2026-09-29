using System.Collections;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using KanBagi.Inventory;
using KanBagi.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Yonetim PC'sinin ekrani. Katalogdaki her urun icin bir satir uretir ve
    /// para/stok event'leri geldikce satirlari tazeler.
    /// Icinde hicbir satin alma mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class ComputerUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [Tooltip("Bos birakilirsa sahnedeki ilk ComputerStation bulunur.")]
        [SerializeField] private ComputerStation station;
        [Tooltip("Ekran paneli. Baslangicta kapali olmali.")]
        [SerializeField] private GameObject screenRoot;
        [Tooltip("Urun satirlarinin ekleneceği obje (genelde Vertical Layout Group'lu bir panel).")]
        [SerializeField] private Transform entryContainer;
        [Tooltip("Her urun icin kopyalanacak satir prefab'i.")]
        [SerializeField] private CatalogEntryUI entryPrefab;
        [SerializeField] private Button closeButton;
        [Tooltip("Ekranin ustundeki kasa yazisi (opsiyonel).")]
        [SerializeField] private TMP_Text moneyText;

        [Header("Kaydet")]
        [Tooltip("PC ekranindaki Kaydet butonu (opsiyonel).")]
        [SerializeField] private Button saveButton;
        [Tooltip("Kaydetme sonrasi kisa bilgi yazisi (opsiyonel).")]
        [SerializeField] private TMP_Text saveStatusText;

        [Header("Gorunum")]
        [SerializeField] private string moneyFormat = "Kasa: {0} TL";
        [SerializeField] private string savedMessage = "Kaydedildi";
        [SerializeField] private float savedMessageDuration = 2f;

        private readonly List<CatalogEntryUI> rows = new List<CatalogEntryUI>();
        private Coroutine saveMessageRoutine;

        private void Start()
        {
            if (station == null) station = FindFirstObjectByType<ComputerStation>();

            if (station == null || screenRoot == null || entryContainer == null || entryPrefab == null)
            {
                Debug.LogError(
                    "[ComputerUI] Eksik baglanti var. 'Screen Root', 'Entry Container' ve 'Entry Prefab' " +
                    "alanlarini doldur; sahnede ComputerStation oldugundan emin ol.", this);
                enabled = false;
                return;
            }

            station.OnScreenToggled += HandleScreenToggled;

            if (closeButton != null) closeButton.onClick.AddListener(station.CloseScreen);
            if (saveButton != null) saveButton.onClick.AddListener(HandleSaveClicked);
            if (saveStatusText != null) saveStatusText.text = string.Empty;

            BuildRows();
            screenRoot.SetActive(false);
        }

        /// <summary>Kaydetme isini SaveManager yapar; UI sadece cagirip sonucu yazar.</summary>
        private void HandleSaveClicked()
        {
            if (SaveManager.Instance == null)
            {
                Debug.LogError("[ComputerUI] SaveManager bulunamadi. 'Managers' objesine ekle.", this);
                return;
            }

            SaveManager.Instance.Save();

            if (saveStatusText == null) return;

            saveStatusText.text = savedMessage;

            if (saveMessageRoutine != null) StopCoroutine(saveMessageRoutine);
            saveMessageRoutine = StartCoroutine(ClearSaveMessageRoutine());
        }

        private IEnumerator ClearSaveMessageRoutine()
        {
            yield return new WaitForSeconds(savedMessageDuration);

            saveStatusText.text = string.Empty;
            saveMessageRoutine = null;
        }

        /// <summary>Katalogdaki satirlari bir kere uretir; sonra sadece tazelenir.</summary>
        private void BuildRows()
        {
            if (station.Catalog == null) return;

            foreach (ShopCatalog.CatalogEntry entry in station.Catalog.Entries)
            {
                if (entry == null || !entry.IsValid) continue;

                CatalogEntryUI row = Instantiate(entryPrefab, entryContainer);
                row.Bind(entry);
                rows.Add(row);
            }
        }

        private void HandleScreenToggled(bool isOpen)
        {
            screenRoot.SetActive(isOpen);

            if (isOpen)
            {
                Subscribe();
                RefreshAll();
            }
            else
            {
                Unsubscribe();
            }
        }

        /// <summary>Ekran kapaliyken bosuna dinlemeyelim.</summary>
        private void Subscribe()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            if (InventoryManager.Instance != null) InventoryManager.Instance.OnStockChanged += HandleStockChanged;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;
        }

        private void Unsubscribe()
        {
            if (EconomyManager.Instance != null) EconomyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
            if (InventoryManager.Instance != null) InventoryManager.Instance.OnStockChanged -= HandleStockChanged;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }

        private void HandleMoneyChanged(int currentMoney) => RefreshAll();

        private void HandleStockChanged(ItemData item, int newCount) => RefreshAll();

        // Yeni makine alininca malzemesi listede belirsin.
        private void HandleUpgradePurchased(UpgradeData upgrade) => RefreshAll();

        private void RefreshAll()
        {
            foreach (CatalogEntryUI row in rows)
                row.Refresh();

            if (moneyText != null && EconomyManager.Instance != null)
                moneyText.text = string.Format(moneyFormat, EconomyManager.Instance.CurrentMoney);
        }

        private void OnDestroy()
        {
            if (station != null)
            {
                station.OnScreenToggled -= HandleScreenToggled;
                if (closeButton != null) closeButton.onClick.RemoveListener(station.CloseScreen);
            }

            if (saveButton != null) saveButton.onClick.RemoveListener(HandleSaveClicked);

            Unsubscribe();
        }
    }
}
