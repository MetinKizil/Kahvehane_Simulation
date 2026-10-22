using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>Esyanin turu: katalogda gruplamak ve yerlestirme kurallari icin.</summary>
    public enum FurnitureCategory
    {
        Table,   // Musteri masasi
        Station, // Ocak, makine, lavabo, raf...
        Decor    // Sus esyasi (D9)
    }

    /// <summary>
    /// Dukkana yerlestirilebilen bir esyanin tanimi (masa, ocak, makine...).
    /// Sadece VERIDIR; nerede durdugunu FurnitureManager bilir (ANAYASA: veriler ScriptableObject'te).
    /// Kayitta esya ASSET ADIYLA saklanir; asset adini degistirmek eski kayitlari bozar.
    /// Menu: Create > Kan Bagi > Furniture Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewFurniture", menuName = "Kan Bagi/Furniture Data")]
    public class FurnitureData : ScriptableObject
    {
        [Header("Kimlik")]
        [SerializeField] private string displayName;
        [SerializeField] private FurnitureCategory category;

        [Header("Gorunum")]
        [Tooltip("Sahneye konacak prefab. Kokunde PlacedFurniture olmali.")]
        [SerializeField] private GameObject prefab;

        [Header("Ekonomi")]
        [Tooltip("Satin alma fiyati (TL). Esya yerlestirilince odenir.")]
        [SerializeField] private int price = 100;

        [Header("Sinir")]
        [Tooltip("Dukkanda en fazla kac tane olabilir (ana salon). 0 = sinirsiz. Genisletme bolgeleri (D6) artiracak.")]
        [SerializeField] private int baseLimit;

        public string DisplayName => displayName;
        public FurnitureCategory Category => category;
        public GameObject Prefab => prefab;
        public int Price => price;
        public int BaseLimit => baseLimit;
    }
}
