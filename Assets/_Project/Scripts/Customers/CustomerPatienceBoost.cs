using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Customers
{
    /// <summary>
    /// Bir gelistirme alininca musterilerin sabrini artirir (okey takimi, gazete, televizyon...).
    /// Ayni upgrade'e hem UpgradeUnlocker (objeyi acar) hem bu (etkisini verir) abone olabilir;
    /// Observer Pattern'in tam da bu yuzden secildiginin ornegi.
    ///
    /// ONEMLI: Acilan objenin uzerinde DURMAMALI - obje kapaliyken event'i duymaz.
    /// 'Managers' gibi hep acik bir objede dursun.
    /// </summary>
    public class CustomerPatienceBoost : MonoBehaviour
    {
        [Header("Kilit")]
        [SerializeField] private UpgradeData requiredUpgrade;

        [Header("Etki")]
        [Tooltip("Sabir carpani. 1.5 = musteriler %50 daha uzun bekler.")]
        [SerializeField] private float patienceMultiplier = 1.5f;

        private bool isApplied;

        private void Start()
        {
            if (requiredUpgrade == null)
            {
                Debug.LogError("[CustomerPatienceBoost] 'Required Upgrade' atanmamis.", this);
                enabled = false;
                return;
            }

            if (UpgradeManager.Instance == null || CustomerModifiers.Instance == null)
            {
                Debug.LogError("[CustomerPatienceBoost] UpgradeManager veya CustomerModifiers bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;

            if (UpgradeManager.Instance.IsPurchased(requiredUpgrade)) ApplyBoost();
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            if (upgrade != requiredUpgrade) return;

            ApplyBoost();
        }

        /// <summary>Carpanlar carpilarak biriktigi icin ayni etki iki kez uygulanmamali.</summary>
        private void ApplyBoost()
        {
            if (isApplied) return;

            isApplied = true;
            CustomerModifiers.Instance.MultiplyPatience(patienceMultiplier);
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
