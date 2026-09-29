using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Neighborhood
{
    /// <summary>
    /// Mahalledeki bir binayi bir PropertyData'ya baglar ve YERINDE satin almayi saglar.
    /// Satin alma mantigi yok; PC ekraniyla AYNI PropertyManager.TryPurchase cagrilir
    /// (tek dogruluk kaynagi).
    ///
    /// ONEMLI: Bu script collider'in bulundugu GameObject'te olmali -
    /// PlayerInteractor tek isin atip hit.collider uzerinde arar.
    /// </summary>
    public class PropertyBuilding : MonoBehaviour, IInteractable
    {
        [Header("Mulk")]
        [Tooltip("Bu binanin temsil ettigi mulk.")]
        [SerializeField] private PropertyData property;

        [Header("Ipucu Metinleri")]
        [Tooltip("{0} mulk adi, {1} fiyat.")]
        [SerializeField] private string forSaleFormat = "{0} - {1} TL";
        [Tooltip("{0} gereken itibar.")]
        [SerializeField] private string reputationLockedFormat = "Itibar {0} gerekli";
        [Tooltip("{0} fiyat.")]
        [SerializeField] private string notEnoughMoneyFormat = "{0} TL gerekli";
        [Tooltip("{0} gunluk gelir.")]
        [SerializeField] private string ownedFormat = "Senin dukkanin - Gunluk {0} TL";

        /// <summary>Tabela UI'i bunu okur.</summary>
        public PropertyData Property => property;

        public string InteractionPrompt
        {
            get
            {
                if (property == null) return "Bos arsa";

                PropertyManager manager = PropertyManager.Instance;
                if (manager == null) return property.PropertyName;

                if (manager.IsOwned(property)) return string.Format(ownedFormat, property.DailyIncome);

                // Neden alamadigini oyuncu ipucunda gorsun.
                if (!manager.MeetsReputation(property))
                    return string.Format(reputationLockedFormat, property.RequiredReputation);

                int money = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney : 0;
                if (money < property.PurchasePrice)
                    return string.Format(notEnoughMoneyFormat, property.PurchasePrice);

                return string.Format(forSaleFormat, property.PropertyName, property.PurchasePrice);
            }
        }

        public void Interact()
        {
            if (property == null || PropertyManager.Instance == null) return;
            if (PropertyManager.Instance.IsOwned(property)) return;

            PropertyManager.Instance.TryPurchase(property);
        }

        private void Start()
        {
            if (property == null)
                Debug.LogError($"[PropertyBuilding] '{name}' uzerinde 'Property' atanmamis. PropertyData asset'ini surukle.", this);
        }
    }
}
