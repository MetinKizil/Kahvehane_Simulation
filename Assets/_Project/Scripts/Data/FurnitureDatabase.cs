using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Oyundaki TUM esya tanimlarinin listesi. Kayit sistemi esyalari asset adiyla saklar;
    /// yuklerken adi tanima buradan cevirir (ItemDatabase'in esya karsiligi).
    /// Yeni esya eklenince buraya da eklenmeli.
    /// Menu: Create > Kan Bagi > Furniture Database
    /// </summary>
    [CreateAssetMenu(fileName = "NewFurnitureDatabase", menuName = "Kan Bagi/Furniture Database")]
    public class FurnitureDatabase : ScriptableObject
    {
        [SerializeField] private List<FurnitureData> furniture = new List<FurnitureData>();

        public IReadOnlyList<FurnitureData> Furniture => furniture;

        /// <summary>Asset adina gore esya tanimi; bulunamazsa null.</summary>
        public FurnitureData FindByName(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            foreach (FurnitureData data in furniture)
            {
                if (data != null && data.name == assetName) return data;
            }

            return null;
        }
    }
}
