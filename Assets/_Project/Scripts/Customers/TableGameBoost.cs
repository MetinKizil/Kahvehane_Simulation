using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Customers
{
    /// <summary>
    /// Bir masa oyunu upgrade'i (iskambil, domino...) alininca musterilerin ikinci tur
    /// isteme ihtimalini artirir. Objeyi acmak UpgradeUnlocker'in isi; bu sadece etkiyi verir.
    ///
    /// ONEMLI: Acilan objenin uzerinde DURMAMALI - obje kapaliyken event'i duymaz.
    /// 'Managers' gibi hep acik bir objede dursun. Kurulum: Kan Bagi > Masa Oyunlarini Kur
    /// </summary>
    public class TableGameBoost : MonoBehaviour
    {
        [Header("Kilit")]
        [SerializeField] private UpgradeData requiredUpgrade;

        [Header("Etki")]
        [Tooltip("Ikinci tur ihtimaline eklenen pay. 0.3 = %30.")]
        [Range(0f, 1f)]
        [SerializeField] private float secondRoundChance = 0.3f;

        private bool isApplied;

        private void Start()
        {
            if (requiredUpgrade == null)
            {
                Debug.LogError("[TableGameBoost] 'Required Upgrade' atanmamis.", this);
                enabled = false;
                return;
            }

            if (UpgradeManager.Instance == null || CustomerModifiers.Instance == null)
            {
                Debug.LogError("[TableGameBoost] UpgradeManager veya CustomerModifiers bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;

            if (UpgradeManager.Instance.IsPurchased(requiredUpgrade)) ApplyBoost();
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            if (upgrade == requiredUpgrade) ApplyBoost();
        }

        /// <summary>Paylar toplanarak biriktigi icin ayni etki iki kez uygulanmamali.</summary>
        private void ApplyBoost()
        {
            if (isApplied) return;

            isApplied = true;
            CustomerModifiers.Instance.AddSecondRoundChance(secondRoundChance);
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
