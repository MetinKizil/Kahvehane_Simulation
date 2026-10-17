using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>Duzendeki bir esya: ne, nerede, hangi aciyla (sadece Y ekseni; esya hep dik durur).</summary>
    [Serializable]
    public class FurnitureLayoutEntry
    {
        [SerializeField] private FurnitureData furniture;
        [SerializeField] private Vector3 position;
        [Tooltip("Y ekseni etrafinda donus (derece).")]
        [SerializeField] private float yaw;

        public FurnitureLayoutEntry(FurnitureData furniture, Vector3 position, float yaw)
        {
            this.furniture = furniture;
            this.position = position;
            this.yaw = yaw;
        }

        public FurnitureData Furniture => furniture;
        public Vector3 Position => position;
        public float Yaw => yaw;
    }

    /// <summary>
    /// Hazir bir esya duzeni. Simdilik tek kullanim: kayit yokken (yeni oyun) dukkanin baslangic duzeni.
    /// Sadece VERIDIR; esyalari FurnitureManager olusturur.
    /// Menu: Create > Kan Bagi > Furniture Layout
    /// </summary>
    [CreateAssetMenu(fileName = "NewFurnitureLayout", menuName = "Kan Bagi/Furniture Layout")]
    public class FurnitureLayout : ScriptableObject
    {
        [SerializeField] private List<FurnitureLayoutEntry> entries = new List<FurnitureLayoutEntry>();

        public IReadOnlyList<FurnitureLayoutEntry> Entries => entries;
    }
}
