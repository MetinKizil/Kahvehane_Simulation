using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Yonetim PC'sindeki "Gelistirme" sekmesinde listelenecek upgrade'ler.
    /// Yeni gelistirme eklemek icin KOD DEGISMEZ, bu asset'e satir eklenir.
    /// Menu: Create > Kan Bagi > Upgrade Catalog
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeCatalog", menuName = "Kan Bagi/Upgrade Catalog")]
    public class UpgradeCatalog : ScriptableObject
    {
        [Header("Gelistirmeler")]
        [SerializeField] private List<UpgradeData> upgrades = new List<UpgradeData>();

        public IReadOnlyList<UpgradeData> Upgrades => upgrades;
    }
}
