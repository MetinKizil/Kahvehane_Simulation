using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Urunun oyundaki kategorisi. Musteri siparisi, stok yonetimi ve
    /// masa esyalari bu ture gore ayristirilir.
    /// </summary>
    public enum ItemType
    {
        Beverage,    // Icecek: cay, kahve vb. Musteriye servis edilir.
        Consumable,  // Sarf malzemesi: cay yapragi, seker vb. Stoktan tuketilir.
        TableItem    // Masa esyasi: okey takimi, iskambil vb. Masaya kurulur.
    }

    /// <summary>
    /// Tum urunlerin veri kaynagi (ANAYASA: veriler ScriptableObject'te tutulur).
    /// Kod degismeden yeni urun eklemek icin Project penceresinden
    /// Create > Kan Bagi > Item Data ile yeni asset olusturulur.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Kan Bagi/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Kimlik")]
        [SerializeField] private string itemName;
        [SerializeField] [TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType itemType;

        [Header("Ekonomi")]
        [Tooltip("Musteriye satis fiyati (TL).")]
        [SerializeField] private int sellPrice;
        [Tooltip("Bilgisayardan siparis edildiginde birim alis maliyeti (TL).")]
        [SerializeField] private int purchaseCost;

        [Header("Hazirlik")]
        [Tooltip("Urunun hazirlanma suresi (saniye). Ornek: bir bardak cay doldurmak.")]
        [SerializeField] private float prepareDuration = 1f;

        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public ItemType Type => itemType;
        public int SellPrice => sellPrice;
        public int PurchaseCost => purchaseCost;
        public float PrepareDuration => prepareDuration;
    }
}
