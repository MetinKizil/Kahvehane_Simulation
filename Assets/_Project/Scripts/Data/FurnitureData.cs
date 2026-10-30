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

        [Tooltip("Dukkanda her kopya icin fiyat bu kadar artar (rulet: 800, 1000). 0 = sabit fiyat.")]
        [SerializeField] private int priceStep;

        [Tooltip("Satin almak icin gereken itibar. 0 = sart yok.")]
        [SerializeField] private int requiredReputation;

        [Tooltip("Satin almak icin once alinmasi gereken gelistirme (rulet: Kumar Odasi). Bos = sart yok.")]
        [SerializeField] private UpgradeData requiredUpgrade;

        [Header("Sinir")]
        [Tooltip("Dukkanda en fazla kac tane olabilir (ana salon). 0 = sinirsiz. Genisletme bolgeleri (D6) artiracak.")]
        [SerializeField] private int baseLimit;

        [Header("Yer")]
        [Tooltip("Hangi bolgenin zeminine konabilir. Kumar odasina sadece bu bolgedeki esyalar konur.")]
        [SerializeField] private FurnitureZone zone = FurnitureZone.Main;

        public string DisplayName => displayName;
        public FurnitureCategory Category => category;
        public GameObject Prefab => prefab;
        public int Price => price;
        public int PriceStep => priceStep;
        public int RequiredReputation => requiredReputation;
        public UpgradeData RequiredUpgrade => requiredUpgrade;
        public int BaseLimit => baseLimit;
        public FurnitureZone Zone => zone;
    }
}
