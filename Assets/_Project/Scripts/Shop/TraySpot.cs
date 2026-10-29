using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Tepsinin birakilabilecegi yer (simdilik bardak rafinin ustu; ileride tezgah).
    /// Ince bir collider'dir: tepsi ustundeyken isin tepsiye carpar (tepsiyi "Al"),
    /// tepsi kalkinca isin buraya carpar ve elde tepsi varsa "Tepsiyi Birak" der.
    /// Kurulum: Kan Bagi > Tepsiyi Kur
    /// </summary>
    public class TraySpot : MonoBehaviour, IInteractable
    {
        [Tooltip("Tepsinin oturacagi nokta. Bos birakilirsa bu objenin konumu kullanilir.")]
        [SerializeField] private Transform placePoint;

        private Vector3 PlacePosition => placePoint != null ? placePoint.position : transform.position;
        private Quaternion PlaceRotation => placePoint != null ? placePoint.rotation : transform.rotation;

        public string GetPrompt(PlayerContext player) =>
            HasTrayInHands(player.Hands) ? "Tepsiyi Birak" : "Tepsi yeri";

        public void Interact(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (!HasTrayInHands(hands)) return;

            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            // Fizik kapali kalir (tepsi yerinde dursun); collider geri acilir ki tekrar alinabilsin.
            released.Detach(PlacePosition, PlaceRotation, restorePhysics: false);
            released.SetPickupEnabled(true);

            // Tepsi yerin cocugu olur: raf (D5'ten beri tasinabilir) tasininca ustundeki tepsi de gelir.
            released.transform.SetParent(transform, worldPositionStays: true);
        }

        private static bool HasTrayInHands(PlayerHands hands) =>
            hands != null && hands.HeldItem != null && hands.HeldItem.TryGetComponent(out Tray _);
    }
}
