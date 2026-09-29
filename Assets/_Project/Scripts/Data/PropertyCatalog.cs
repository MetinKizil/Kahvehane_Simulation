using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Yonetim PC'sindeki "Emlak" sekmesinde listelenecek mulkler.
    /// Yeni mulk eklemek icin KOD DEGISMEZ, bu asset'e satir eklenir.
    /// Kayit yuklenirken mulkler asset adindan burada aranir.
    /// Menu: Create > Kan Bagi > Property Catalog
    /// </summary>
    [CreateAssetMenu(fileName = "PropertyCatalog", menuName = "Kan Bagi/Property Catalog")]
    public class PropertyCatalog : ScriptableObject
    {
        [Header("Mulkler")]
        [SerializeField] private List<PropertyData> properties = new List<PropertyData>();

        public IReadOnlyList<PropertyData> Properties => properties;

        /// <summary>Asset adina gore mulku bulur. Bulunamazsa null doner.</summary>
        public PropertyData Find(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            foreach (PropertyData property in properties)
            {
                if (property != null && property.name == assetName) return property;
            }

            return null;
        }
    }
}
