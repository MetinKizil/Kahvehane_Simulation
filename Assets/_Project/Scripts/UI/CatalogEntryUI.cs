using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using KanBagi.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Bilgisayar ekranindaki tek bir urun satiri.
    /// Sadece gosterir ve tiklaninca PurchaseManager'i cagirir;
    /// para/stok hesabi yapmaz (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class CatalogEntryUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button buyButton;

        [Header("Gorunum")]
        [Tooltip("{0} paket adedi, {1} fiyat, {2} depodaki mevcut miktar.")]
        [SerializeField] private string detailFormat = "{0} adet - {1} TL   (Depo: {2})";

        private ShopCatalog.CatalogEntry entry;

        /// <summary>ComputerUI satiri olusturduktan sonra cagirir.</summary>
        public void Bind(ShopCatalog.CatalogEntry catalogEntry)
        {
            entry = catalogEntry;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }

            Refresh();
        }

        /// <summary>Para veya stok degisince ComputerUI tarafindan cagrilir.</summary>
        public void Refresh()
        {
            if (entry == null || !entry.IsValid)
            {
                gameObject.SetActive(false);
                return;
            }

            // Gerekli gelistirme alinmadiysa malzeme listede hic gorunmez.
            bool isUnlocked = entry.RequiredUpgrade == null ||
                              (UpgradeManager.Instance != null && UpgradeManager.Instance.IsPurchased(entry.RequiredUpgrade));

            gameObject.SetActive(isUnlocked);
            if (!isUnlocked) return;

            if (nameText != null) nameText.text = entry.Item.ItemName;

            if (detailText != null)
            {
                int stock = InventoryManager.Instance != null ? InventoryManager.Instance.GetCount(entry.Item) : 0;
                detailText.text = string.Format(detailFormat, entry.UnitsPerPack, entry.PackPrice, stock);
            }

            if (buyButton != null)
                buyButton.interactable = PurchaseManager.Instance != null && PurchaseManager.Instance.CanAfford(entry);
        }

        private void HandleBuyClicked()
        {
            if (PurchaseManager.Instance == null) return;

            PurchaseManager.Instance.TryBuy(entry);
        }

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }
}
