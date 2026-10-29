using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Furniture;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Bir gelistirme alininca icecek istasyonlarini (TeaStove: cay ocagi, kahve/oralet/sahlep makinesi) hizlandirir.
    /// Istasyon kimin hizlandirdigini bilmez, upgrade ne yaptigini bilmez; ikisini bu kopru birlestirir (SOLID + Observer).
    ///
    /// D5'ten beri istasyonlar tasinabilir esya (oyun sirasinda olusturulur, kayittan yeniden kurulur):
    /// sabit hedef listesi YOK; etki dukkandaki TUM TeaStove'lara uygulanir ve esya duzeni degisince
    /// (yeni makine kondu, kayit yuklendi) yeniden uygulanir. SetDurationMultiplier ayni degeri yazdigi icin tekrar zararsiz.
    ///
    /// ONEMLI: Hep acik bir objede ('Managers') durur.
    /// </summary>
    public class StationSpeedBoost : MonoBehaviour
    {
        [Header("Kilit")]
        [Tooltip("Bu gelistirme alininca hizlanma uygulanir.")]
        [SerializeField] private UpgradeData requiredUpgrade;

        [Header("Etki")]
        [Tooltip("Is suresi carpani. 0.6 = %40 daha hizli.")]
        [SerializeField] private float durationMultiplier = 0.6f;

        private bool isApplied;

        private void Start()
        {
            if (requiredUpgrade == null)
            {
                Debug.LogError("[StationSpeedBoost] 'Required Upgrade' bos. Inspector'i doldur.", this);
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
            if (FurnitureManager.Instance != null) FurnitureManager.Instance.OnLayoutChanged += HandleLayoutChanged;

            // Gelistirme zaten alinmissa etki oyun basinda uygulanir.
            if (UpgradeManager.Instance.IsPurchased(requiredUpgrade)) ApplyBoost();
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            if (upgrade == requiredUpgrade) ApplyBoost();
        }

        /// <summary>Yeni konan veya kayittan gelen istasyon da hizli olsun.</summary>
        private void HandleLayoutChanged()
        {
            if (isApplied) ApplyBoost();
        }

        private void ApplyBoost()
        {
            isApplied = true;

            // Kilitli (pasif) makineler de dahil: acildiklarinda zaten hizli olsunlar.
            foreach (TeaStove station in FindObjectsByType<TeaStove>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                station.SetDurationMultiplier(durationMultiplier);
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
            if (FurnitureManager.Instance != null)
                FurnitureManager.Instance.OnLayoutChanged -= HandleLayoutChanged;
        }
    }
}
