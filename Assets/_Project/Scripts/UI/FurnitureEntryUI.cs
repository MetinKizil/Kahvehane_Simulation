using System;
using KanBagi.Data;
using KanBagi.Furniture;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// PC'deki ESYALAR sayfasinin bir satiri: ad, "Dukkanda 2 / 4", fiyat ve Satin Al butonu.
    /// Sadece gosterir; satin alma istegini FurnitureListUI'a iletir (ANAYASA: UI mantiktan ayri).
    /// Satir prefab'i (EsyaSatiri) 'Kan Bagi > Esya Katalogunu Kur' ile uretilir.
    /// </summary>
    public class FurnitureEntryUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button buyButton;
        [SerializeField] private TMP_Text buyButtonLabel;

        [Header("Gorunum")]
        [SerializeField] private string priceFormat = "{0} TL";
        [Tooltip("{0} dukkandaki sayi, {1} sinir.")]
        [SerializeField] private string countFormat = "Dukkanda {0} / {1}";
        [Tooltip("Sinirsiz esyada. {0} dukkandaki sayi.")]
        [SerializeField] private string unlimitedCountFormat = "Dukkanda {0}";
        [SerializeField] private string limitReachedLabel = "Sinir dolu";

        private FurnitureData furniture;
        private Action<FurnitureData> onBuy;

        /// <summary>FurnitureListUI satiri olusturduktan sonra cagirir.</summary>
        public void Bind(FurnitureData data, Action<FurnitureData> buyCallback)
        {
            furniture = data;
            onBuy = buyCallback;

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(HandleBuyClicked);
                buyButton.onClick.AddListener(HandleBuyClicked);
            }

            Refresh();
        }

        /// <summary>Para veya dukkandaki esya sayisi degisince FurnitureListUI cagirir.</summary>
        public void Refresh()
        {
            FurnitureManager manager = FurnitureManager.Instance;
            if (furniture == null || manager == null) return;

            int count = manager.CountOf(furniture);
            int limit = manager.GetLimit(furniture);
            bool limitReached = limit > 0 && count >= limit;

            if (nameText != null) nameText.text = furniture.DisplayName;
            if (descriptionText != null)
                descriptionText.text = limit > 0
                    ? string.Format(countFormat, count, limit)
                    : string.Format(unlimitedCountFormat, count);

            if (buyButtonLabel != null)
                buyButtonLabel.text = limitReached ? limitReachedLabel : string.Format(priceFormat, furniture.Price);

            if (buyButton != null) buyButton.interactable = manager.CanPurchase(furniture, out _);
        }

        private void HandleBuyClicked() => onBuy?.Invoke(furniture);

        private void OnDestroy()
        {
            if (buyButton != null) buyButton.onClick.RemoveListener(HandleBuyClicked);
        }
    }
}
