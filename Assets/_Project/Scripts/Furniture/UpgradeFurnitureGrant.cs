using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Player;
using KanBagi.Shop;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Bir upgrade alininca dukkana BIR KEZ esya verir ("Yeni Masa" -> masa).
    /// PC'den alindiysa: PC ekrani kapanir ve esya ALAN OYUNCUNUN ELINDE HAYALET olarak belirir (FurniturePlacer);
    /// oyuncu istedigi yere koyar. Iptal ederse esya 'position' noktasinda kalir.
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
        [Tooltip("Esyanin ilk belirdigi yer; yerlestirme iptal edilirse burada kalir.")]
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
            PlacedFurniture placed = manager.Spawn(furniture, position, yaw);

            GiveToBuyer(placed);
        }

        /// <summary>PC'yi kullanan oyuncu varsa ekrani kapatip esyayi onun eline hayalet olarak verir.</summary>
        private static void GiveToBuyer(PlacedFurniture placed)
        {
            if (placed == null) return;

            ComputerStation computer = FindFirstObjectByType<ComputerStation>();
            PlayerLock buyer = computer != null ? computer.ScreenUser : null;
            if (buyer == null || !buyer.TryGetComponent(out FurniturePlacer placer)) return;

            // Once ekran kapanir (oyuncunun kontrolu geri gelir), sonra esya ele verilir.
            computer.CloseScreen();
            placer.BeginPlacingNew(placed);
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
