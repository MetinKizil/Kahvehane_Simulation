using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Yonetim PC'sinden siparis edilebilecek malzemelerin listesi
    /// (ANAYASA: veriler ScriptableObject'te tutulur).
    /// Yeni urun eklemek icin KOD DEGISMEZ, bu asset'e satir eklenir.
    /// Menu: Create > Kan Bagi > Shop Catalog
    /// </summary>
    [CreateAssetMenu(fileName = "ShopCatalog", menuName = "Kan Bagi/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        /// <summary>Katalogdaki tek bir satir: hangi malzeme, kacli paket.</summary>
        [Serializable]
        public class CatalogEntry
        {
            [SerializeField] private ItemData item;
            [Tooltip("Bir pakette kac olcek/adet var.")]
            [SerializeField] private int unitsPerPack = 10;
            [Tooltip("Isaretliyse malzeme, onu kullanan makine dukkanda olana kadar listede gorunmez " +
                     "(kahve makinen yokken cekirdek alip para harcamayasin). D5b'den beri upgrade kilidi yerine bu.")]
            [SerializeField] private bool requiresStation;

            public ItemData Item => item;

            public bool RequiresStation => requiresStation;

            public int UnitsPerPack => Mathf.Max(1, unitsPerPack);

            /// <summary>Paket fiyati birim maliyetten hesaplanir; fiyat koda gomulmez.</summary>
            public int PackPrice => item != null ? item.PurchaseCost * UnitsPerPack : 0;

            /// <summary>Eksik doldurulmus satirlar ekranda gosterilmez.</summary>
            public bool IsValid => item != null && PackPrice > 0;
        }

        [Header("Urunler")]
        [SerializeField] private List<CatalogEntry> entries = new List<CatalogEntry>();

        public IReadOnlyList<CatalogEntry> Entries => entries;
    }
}
