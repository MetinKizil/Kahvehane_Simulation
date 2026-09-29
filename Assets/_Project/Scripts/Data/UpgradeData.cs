using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Tek seferlik bir dukkan gelistirmesi (yeni masa, kahve makinesi, daha hizli ocak...).
    /// Sadece VERIDIR; ne yaptigini bilmez. Etkiyi sahnedeki UpgradeUnlocker gibi
    /// dinleyiciler uretir (ANAYASA: veriler ScriptableObject'te, mantik sahnede).
    /// Menu: Create > Kan Bagi > Upgrade Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Kan Bagi/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("Kimlik")]
        [SerializeField] private string upgradeName;
        [SerializeField] [TextArea] private string description;

        [Header("Ekonomi")]
        [Tooltip("Tek seferlik satin alma bedeli (TL).")]
        [SerializeField] private int cost = 100;

        [Header("Sart")]
        [Tooltip("Bu gelistirmeyi alabilmek icin gereken itibar. 0 = sart yok.")]
        [SerializeField] private int requiredReputation;

        public string UpgradeName => upgradeName;
        public string Description => description;
        public int Cost => cost;
        public int RequiredReputation => requiredReputation;

        /// <summary>Eksik doldurulmus upgrade'ler ekranda gosterilmez.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(upgradeName) && cost > 0;
    }
}
