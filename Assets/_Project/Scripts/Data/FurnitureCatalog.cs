using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// PC'nin ESYALAR sekmesinde satilan esyalar ve sekme basligi.
    /// EsyaVeritabani'ndan farki: veritabani kayit icin TUM esyalari tutar, katalog sadece satilanlari.
    /// Menu: Create > Kan Bagi > Furniture Catalog
    /// </summary>
    [CreateAssetMenu(fileName = "NewFurnitureCatalog", menuName = "Kan Bagi/Furniture Catalog")]
    public class FurnitureCatalog : ScriptableObject
    {
        [Tooltip("PC'deki sekmenin yazisi (Turkce karakter burada, kodda degil).")]
        [SerializeField] private string tabTitle = "ESYALAR";
        [SerializeField] private List<FurnitureData> items = new List<FurnitureData>();

        public string TabTitle => tabTitle;
        public IReadOnlyList<FurnitureData> Items => items;
    }
}
