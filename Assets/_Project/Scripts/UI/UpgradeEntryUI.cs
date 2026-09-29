using KanBagi.Core;
using KanBagi.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Bilgisayar ekranindaki tek bir gelistirme satiri.
    /// Sadece gosterir ve tiklaninca UpgradeManager'i cagirir;
    /// para hesabi yapmaz (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class UpgradeEntryUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button buyButton;
        [Tooltip("Butonun uzerindeki yazi. Fiyat veya 'Alindi' burada gorunur.")]
        [SerializeField] private TMP_Text buyButtonLabel;

        [Header("Gorunum")]
        [Tooltip("{0} fiyat.")]
        [SerializeField] private string priceFormat = "{0} TL";
        [SerializeField] private string purchasedLabel = "Alindi";
        [Tooltip("{0} gereken itibar.")]
        [SerializeField] private string reputationLockedFormat = "Itibar {0} gerekli";

        private UpgradeData upgrade;

        /// <summary>UpgradeListUI satiri olusturduktan sonra cagirir.</summary>
        public void Bind(UpgradeData upgradeData)
        {
            upgrade = upgradeData;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }

            Refresh();
        }

        /// <summary>Para degisince veya bir gelistirme alininca UpgradeListUI cagirir.</summary>
        public void Refresh()
        {
            if (upgrade == null || !upgrade.IsValid)
            {
                gameObject.SetActive(false);
                return;
            }

            if (nameText != null) nameText.text = upgrade.UpgradeName;
            if (descriptionText != null) descriptionText.text = upgrade.Description;

            UpgradeManager manager = UpgradeManager.Instance;

            if (buyButtonLabel != null) buyButtonLabel.text = GetButtonLabel(manager);

            if (buyButton != null)
                buyButton.interactable = manager != null && manager.CanAfford(upgrade);
        }

        private string GetButtonLabel(UpgradeManager manager)
        {
            if (manager != null && manager.IsPurchased(upgrade)) return purchasedLabel;

            // Itibar yetmiyorsa fiyat yerine sart gosterilir.
            if (manager != null && !manager.MeetsReputation(upgrade))
                return string.Format(reputationLockedFormat, upgrade.RequiredReputation);

            return string.Format(priceFormat, upgrade.Cost);
        }

        private void HandleBuyClicked()
        {
            if (UpgradeManager.Instance == null) return;

            UpgradeManager.Instance.TryPurchase(upgrade);
        }

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }
}
