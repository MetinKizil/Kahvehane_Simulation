using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Neighborhood;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Binanin duvarina yapisik "SATILIK" tabelasi.
    /// Mulk satin alininca tabela tamamen kaybolur (gercek hayatta oldugu gibi).
    /// SADECE PropertyManager event'ini dinler; satin alma mantigi icermez
    /// (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class PropertySignUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [Tooltip("Bos birakilirsa ust objelerden, bulunamazsa EN YAKIN bina otomatik secilir.")]
        [SerializeField] private PropertyBuilding building;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text infoText;

        [Header("Gorunum")]
        [Tooltip("{0} fiyat, {1} gunluk gelir, {2} gereken itibar.")]
        [SerializeField] [TextArea] private string infoFormat = "SATILIK - {0} TL\nGunluk {1} TL   |   Itibar {2}";

        private PropertyManager propertyManager;

        private void Start()
        {
            if (!ResolveBuilding())
            {
                Debug.LogError($"[PropertySignUI] '{name}' icin bina bulunamadi. 'Building' alanina bir PropertyBuilding ata.", this);
                enabled = false;
                return;
            }

            propertyManager = PropertyManager.Instance;

            if (propertyManager != null)
                propertyManager.OnPropertyPurchased += HandlePropertyPurchased;

            // Kayittan yuklenmis mulkun tabelasi hic gorunmesin.
            if (propertyManager != null && propertyManager.IsOwned(building.Property))
            {
                HideSign();
                return;
            }

            Refresh();
        }

        /// <summary>Inspector > ust obje > en yakin bina sirasiyla dener.</summary>
        private bool ResolveBuilding()
        {
            if (building == null) building = GetComponentInParent<PropertyBuilding>();
            if (building == null) building = FindNearestBuilding();

            return building != null && building.Property != null;
        }

        private PropertyBuilding FindNearestBuilding()
        {
            PropertyBuilding[] candidates = FindObjectsByType<PropertyBuilding>(FindObjectsSortMode.None);
            PropertyBuilding nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (PropertyBuilding candidate in candidates)
            {
                if (candidate == null || candidate.Property == null) continue;

                float distance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distance >= nearestDistance) continue;

                nearestDistance = distance;
                nearest = candidate;
            }

            if (nearest != null)
                Debug.Log($"[PropertySignUI] '{name}' en yakin binaya baglandi: '{nearest.name}'.", this);

            return nearest;
        }

        private void HandlePropertyPurchased(PropertyData property)
        {
            if (property != building.Property) return;

            HideSign();
        }

        private void HideSign() => gameObject.SetActive(false);

        private void Refresh()
        {
            PropertyData property = building.Property;

            if (nameText != null) nameText.text = property.PropertyName;

            if (infoText != null)
                infoText.text = string.Format(infoFormat, property.PurchasePrice, property.DailyIncome, property.RequiredReputation);
        }

        private void OnDestroy()
        {
            if (propertyManager != null)
                propertyManager.OnPropertyPurchased -= HandlePropertyPurchased;
        }
    }
}
