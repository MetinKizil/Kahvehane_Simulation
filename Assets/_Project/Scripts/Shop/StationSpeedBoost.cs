using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Bir gelistirme alininca istasyonlari hizlandirir (UpgradeUnlocker'in kardesi).
    /// Istasyon kimin hizlandirdigini bilmez, upgrade ne yaptigini bilmez;
    /// ikisini bu kopru birlestirir (SOLID + Observer Pattern).
    ///
    /// ONEMLI: Hedef istasyonun uzerinde DURMAMALI - kilitli istasyon kapaliyken
    /// bu da kapanir ve event'i duymaz. 'Managers' gibi hep acik bir objede dursun.
    /// </summary>
    public class StationSpeedBoost : MonoBehaviour
    {
        [Header("Kilit")]
        [Tooltip("Bu gelistirme alininca hizlanma uygulanir.")]
        [SerializeField] private UpgradeData requiredUpgrade;

        [Header("Etki")]
        [Tooltip("Hizlandirilacak istasyonlar. Kilitli (pasif) olanlar da atanabilir.")]
        [SerializeField] private TimedStation[] targetStations;
        [Tooltip("Is suresi carpani. 0.6 = %40 daha hizli.")]
        [SerializeField] private float durationMultiplier = 0.6f;

        private void Start()
        {
            if (requiredUpgrade == null || targetStations == null || targetStations.Length == 0)
            {
                Debug.LogError("[StationSpeedBoost] 'Required Upgrade' veya 'Target Stations' bos. Inspector'i doldur.", this);
                enabled = false;
                return;
            }

            if (UpgradeManager.Instance == null)
            {
                Debug.LogError("[StationSpeedBoost] UpgradeManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;

            // Gelistirme zaten alinmissa etki oyun basinda uygulanir.
            if (UpgradeManager.Instance.IsPurchased(requiredUpgrade)) ApplyBoost();
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            if (upgrade != requiredUpgrade) return;

            ApplyBoost();
        }

        private void ApplyBoost()
        {
            foreach (TimedStation station in targetStations)
            {
                if (station == null) continue;

                station.SetDurationMultiplier(durationMultiplier);
            }
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
