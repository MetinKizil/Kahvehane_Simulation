using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Bir upgrade alininca dukkana BIR KEZ esya koyar ("Yeni Masa" -> masa).
    /// Hangi upgrade'in esyasini verdigi FurnitureManager'in kaydinda tutulur; boylece kayit
    /// yuklenirken upgrade'ler yeniden duyurulsa da esya ikinci kez verilmez.
    /// (Yukleme sirasi ne olursa olsun guvenli: upgrade once gelirse verilen esya, ardindan
    /// yuklenen esya kaydi tarafindan zaten degistirilir.)
    ///
    /// Gecici kopru: D4'te masalar katalogdan satin alinacak, "Yeni Masa" upgrade'i kalkacak.
    /// 'Managers' gibi hep acik bir objede durur.
    /// </summary>
    public class UpgradeFurnitureGrant : MonoBehaviour
    {
        [Header("Kosul")]
        [SerializeField] private UpgradeData upgrade;

        [Header("Verilecek Esya")]
        [SerializeField] private FurnitureData furniture;
        [SerializeField] private Vector3 position;
        [Tooltip("Y ekseni etrafinda donus (derece).")]
        [SerializeField] private float yaw;

        private void Start()
        {
            if (upgrade == null || furniture == null)
            {
                Debug.LogError("[UpgradeFurnitureGrant] 'Upgrade' veya 'Furniture' atanmamis.", this);
                enabled = false;
                return;
            }

            if (UpgradeManager.Instance == null || FurnitureManager.Instance == null)
            {
                Debug.LogError("[UpgradeFurnitureGrant] UpgradeManager veya FurnitureManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;
        }

        private void HandleUpgradePurchased(UpgradeData purchased)
        {
            if (purchased != upgrade) return;

            FurnitureManager manager = FurnitureManager.Instance;
            if (manager == null || manager.IsGranted(upgrade.name)) return;

            manager.MarkGranted(upgrade.name);
            manager.Spawn(furniture, position, yaw);
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
