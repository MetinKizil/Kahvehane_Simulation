using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Mahallede satin alinabilecek bir mulk (bakkal, terzi, kirathane...).
    /// Sadece VERIDIR; geliri kimin topladigini bilmez (ANAYASA: veriler ScriptableObject'te).
    /// Menu: Create > Kan Bagi > Property Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewProperty", menuName = "Kan Bagi/Property Data")]
    public class PropertyData : ScriptableObject
    {
        [Header("Kimlik")]
        [SerializeField] private string propertyName;
        [SerializeField] [TextArea] private string description;

        [Header("Ekonomi")]
        [Tooltip("Tek seferlik satin alma bedeli (TL).")]
        [SerializeField] private int purchasePrice = 800;
        [Tooltip("Her gun kasaya giren pasif gelir (TL).")]
        [SerializeField] private int dailyIncome = 60;

        [Header("Sart")]
        [Tooltip("Bu mulku alabilmek icin mahallede gereken itibar.")]
        [SerializeField] private int requiredReputation = 10;

        public string PropertyName => propertyName;
        public string Description => description;
        public int PurchasePrice => purchasePrice;
        public int DailyIncome => dailyIncome;
        public int RequiredReputation => requiredReputation;

        /// <summary>Eksik doldurulmus mulkler ekranda gosterilmez.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(propertyName) && purchasePrice > 0;
    }
}
