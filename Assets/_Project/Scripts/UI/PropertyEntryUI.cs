using KanBagi.Core;
using KanBagi.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Emlak sekmesindeki tek bir mulk satiri.
    /// Sadece gosterir ve tiklaninca PropertyManager'i cagirir;
    /// para/itibar hesabi yapmaz (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class PropertyEntryUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button buyButton;
        [Tooltip("Butonun uzerindeki yazi: fiyat, 'Alindi' veya itibar sarti burada gorunur.")]
        [SerializeField] private TMP_Text buyButtonLabel;

        [Header("Gorunum")]
        [Tooltip("{0} gunluk gelir, {1} aciklama.")]
        [SerializeField] private string detailFormat = "Gunluk {0} TL   -   {1}";
        [SerializeField] private string priceFormat = "{0} TL";
        [SerializeField] private string ownedLabel = "Alindi";
        [Tooltip("{0} gereken itibar.")]
        [SerializeField] private string reputationLockedFormat = "Itibar {0} gerekli";

        private PropertyData property;

        /// <summary>PropertyListUI satiri olusturduktan sonra cagirir.</summary>
        public void Bind(PropertyData propertyData)
        {
            property = propertyData;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }

            Refresh();
        }

        /// <summary>Para, itibar veya satin alma degisince PropertyListUI cagirir.</summary>
        public void Refresh()
        {
            if (property == null || !property.IsValid)
            {
                gameObject.SetActive(false);
                return;
            }

            if (nameText != null) nameText.text = property.PropertyName;
            if (detailText != null) detailText.text = string.Format(detailFormat, property.DailyIncome, property.Description);

            PropertyManager manager = PropertyManager.Instance;

            if (buyButtonLabel != null) buyButtonLabel.text = GetButtonLabel(manager);
            if (buyButton != null) buyButton.interactable = manager != null && manager.CanPurchase(property);
        }

        private string GetButtonLabel(PropertyManager manager)
        {
            if (manager != null && manager.IsOwned(property)) return ownedLabel;

            // Itibar yetmiyorsa fiyat yerine sart gosterilir: oyuncu neden alamadigini gorsun.
            if (manager != null && !manager.MeetsReputation(property))
                return string.Format(reputationLockedFormat, property.RequiredReputation);

            return string.Format(priceFormat, property.PurchasePrice);
        }

        private void HandleBuyClicked()
        {
            if (PropertyManager.Instance == null) return;

            PropertyManager.Instance.TryPurchase(property);
        }

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }
}
