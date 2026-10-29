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
    ///
    /// D5'ten beri PC tasinabilir bir esya (oyun sirasinda olusturulur, kayittan yeniden kurulur):
    /// UI tek bir PC'ye baglanmaz, HERHANGI bir PC'nin ekrani acilinca (ComputerStation.AnyScreenToggled) o PC'ye hizmet eder.
    /// </summary>
    public class ComputerUI : MonoBehaviour
    {
        [Header("Baglantilar")]
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
        // Ekrani su an acik olan PC (kapaliyken null).
        private ComputerStation station;

        private void Start()
        {
            if (screenRoot == null || entryContainer == null || entryPrefab == null)
            {
                Debug.LogError(
                    "[ComputerUI] Eksik baglanti var. 'Screen Root', 'Entry Container' ve 'Entry Prefab' alanlarini doldur.", this);
                enabled = false;
                return;
            }

            ComputerStation.AnyScreenToggled += HandleScreenToggled;

            if (closeButton != null) closeButton.onClick.AddListener(HandleCloseClicked);
            if (saveButton != null) saveButton.onClick.AddListener(HandleSaveClicked);
            if (saveStatusText != null) saveStatusText.text = string.Empty;

            screenRoot.SetActive(false);
        }

        private void HandleCloseClicked()
        {
            if (station != null) station.CloseScreen();
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

        /// <summary>Katalogdaki satirlari ilk acilista bir kere uretir; sonra sadece tazelenir.</summary>
        private void BuildRows(ShopCatalog catalog)
        {
            if (rows.Count > 0 || catalog == null) return;

            foreach (ShopCatalog.CatalogEntry entry in catalog.Entries)
            {
                if (entry == null || !entry.IsValid) continue;

                CatalogEntryUI row = Instantiate(entryPrefab, entryContainer);
                row.Bind(entry);
                rows.Add(row);
            }
        }

        private void HandleScreenToggled(ComputerStation toggled, bool isOpen)
        {
            // Baska bir PC kapandiysa (ve bu ekran onu gostermiyorsa) karisma.
            if (!isOpen && toggled != station) return;

            station = isOpen ? toggled : null;
            screenRoot.SetActive(isOpen);

            if (isOpen)
            {
                BuildRows(toggled.Catalog);
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
            ComputerStation.AnyScreenToggled -= HandleScreenToggled;
            if (closeButton != null) closeButton.onClick.RemoveListener(HandleCloseClicked);

            if (saveButton != null) saveButton.onClick.RemoveListener(HandleSaveClicked);

            Unsubscribe();
        }
    }
}
