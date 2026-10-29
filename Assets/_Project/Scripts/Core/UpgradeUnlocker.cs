using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Bir gelistirme alinana kadar sahnedeki objeleri kapali tutar, alininca acar.
    /// ScriptableObject sahne objesi tutamayacagi icin UpgradeData ile sahne arasindaki KOPRU budur.
    ///
    /// ONEMLI: Bu script hedef objenin KENDISINDE veya altinda durmamali -
    /// hedef kapaliyken bu da kapanir ve event'i hic duymaz. 'Managers' gibi
    /// hep acik bir objede dursun (ayni objeye birden fazla eklenebilir).
    /// </summary>
    public class UpgradeUnlocker : MonoBehaviour
    {
        [Header("Kilit")]
        [Tooltip("Bu gelistirme alininca hedefler acilir.")]
        [SerializeField] private UpgradeData requiredUpgrade;
        [Tooltip("Gelistirme alinana kadar kapali duracak sahne objeleri.")]
        [SerializeField] private GameObject[] targets;
        [Tooltip("Gelistirme alininca KAPANACAK sahne objeleri (genisletmede yikilan duvar). Bos kalabilir.")]
        [SerializeField] private GameObject[] hideTargets;

        private void Start()
        {
            if (requiredUpgrade == null || targets == null || targets.Length == 0)
            {
                Debug.LogError("[UpgradeUnlocker] 'Required Upgrade' veya 'Targets' bos. Inspector'i doldur.", this);
                enabled = false;
                return;
            }

            if (UpgradeManager.Instance == null)
            {
                Debug.LogError("[UpgradeUnlocker] UpgradeManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;

            // Oyun basinda hedefler kapali; upgrade zaten alinmissa acik kalir.
            SetTargetsActive(UpgradeManager.Instance.IsPurchased(requiredUpgrade));
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            if (upgrade != requiredUpgrade) return;

            SetTargetsActive(true);
        }

        private void SetTargetsActive(bool isActive)
        {
            foreach (GameObject target in targets)
            {
                if (target == null) continue;

                target.SetActive(isActive);
            }

            if (hideTargets == null) return;

            foreach (GameObject target in hideTargets)
            {
                if (target == null) continue;

                target.SetActive(!isActive);
            }
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
