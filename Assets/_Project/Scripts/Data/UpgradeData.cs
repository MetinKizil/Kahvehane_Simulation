using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Tek seferlik bir dukkan gelistirmesi (daha hizli ocak, masa oyunu, dukkan genisletmesi...).
    /// Sadece VERIDIR; ne yaptigini bilmez. Etkiyi sahnedeki UpgradeUnlocker gibi
    /// dinleyiciler uretir (ANAYASA: veriler ScriptableObject'te, mantik sahnede).
    /// Menu: Create > Kan Bagi > Upgrade Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewUpgrade", menuName = "Kan Bagi/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        /// <summary>Gelistirme alininca bir esyanin dukkandaki sinirina eklenen miktar (D6b).</summary>
        [Serializable]
        public class LimitBonus
        {
            [SerializeField] private FurnitureData furniture;
            [SerializeField] private int amount = 1;

            public FurnitureData Furniture => furniture;
            public int Amount => amount;
        }

        [Header("Kimlik")]
        [SerializeField] private string upgradeName;
        [SerializeField] [TextArea] private string description;

        [Header("Ekonomi")]
        [Tooltip("Tek seferlik satin alma bedeli (TL).")]
        [SerializeField] private int cost = 100;

        [Header("Sart")]
        [Tooltip("Bu gelistirmeyi alabilmek icin gereken itibar. 0 = sart yok.")]
        [SerializeField] private int requiredReputation;
        [Tooltip("Once bunlarin HEPSI alinmis olmali (genisletme sirasi: Buyuk Yan Oda icin Yan Oda gibi).")]
        [SerializeField] private List<UpgradeData> requiredUpgrades = new List<UpgradeData>();

        [Header("Esya siniri")]
        [Tooltip("Alininca hangi esyanin siniri kac artar (genisletme bolgeleri: masa +3 gibi).")]
        [SerializeField] private List<LimitBonus> limitBonuses = new List<LimitBonus>();

        public string UpgradeName => upgradeName;
        public string Description => description;
        public int Cost => cost;
        public int RequiredReputation => requiredReputation;
        public IReadOnlyList<UpgradeData> RequiredUpgrades => requiredUpgrades;
        public IReadOnlyList<LimitBonus> LimitBonuses => limitBonuses;

        /// <summary>Eksik doldurulmus upgrade'ler ekranda gosterilmez.</summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(upgradeName) && cost > 0;
    }
}
