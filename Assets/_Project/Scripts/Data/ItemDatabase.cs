using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Oyundaki tum ItemData'larin listesi.
    /// Kayit dosyasi malzemeleri ASSET ADIYLA sakladigi icin yuklerken buradan geri bulunur.
    /// Menu: Create > Kan Bagi > Item Database
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Kan Bagi/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [Header("Urunler")]
        [Tooltip("Oyundaki tum ItemData asset'leri. Yeni urun eklediginde buraya da ekle.")]
        [SerializeField] private List<ItemData> items = new List<ItemData>();

        public IReadOnlyList<ItemData> Items => items;

        /// <summary>Asset adina gore malzemeyi bulur. Bulunamazsa null doner.</summary>
        public ItemData Find(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            foreach (ItemData item in items)
            {
                if (item != null && item.name == assetName) return item;
            }

            return null;
        }
    }
}
