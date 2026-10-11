using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Lavabo. Kirli (veya dolu) bardagi teslim alir, yikar ve TEMIZ STOGA iade eder.
    /// Bardagin nereye gittigini bilmez; sadece rafa "bir temiz bardak eklendi" der.
    /// Faz 1'in bulasik dongusunu kapatan halka.
    /// </summary>
    public class Sink : TimedStation
    {
        [Header("Stok")]
        [Tooltip("Yikanan bardagin iade edilecegi raf. Bos birakilirsa sahnedeki ilk raf bulunur.")]
        [SerializeField] private GlassShelf targetShelf;

        [Header("Yikama")]
        [SerializeField] private float washDuration = 2.5f;

        [Header("Bardak Yeri")]
        [Tooltip("Bos birakilirsa lavabonun ustunde otomatik bir nokta kullanilir.")]
        [SerializeField] private Transform glassSlot;
        [SerializeField] private Vector3 slotOffset = new Vector3(0f, 0.5f, 0f);

        private HoldableItem glassBeingWashed;

        private Vector3 SlotPosition => glassSlot != null ? glassSlot.position : transform.position + slotOffset;
        private Quaternion SlotRotation => glassSlot != null ? glassSlot.rotation : Quaternion.identity;

        private void Awake()
        {
            if (targetShelf == null)
                targetShelf = FindFirstObjectByType<GlassShelf>();

            if (targetShelf == null)
                Debug.LogError("[Sink] Temiz bardak rafi bulunamadi. Sahneye GlassShelf ekle.", this);
        }

        public override string GetPrompt(PlayerContext player)
        {
            if (IsBusy) return WorkLabel;

            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return "Kirli bardak getir";

            if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return "Bu yikanmaz";

            return glass.State switch
            {
                GlassState.Dirty => "Bulasigi Yika",
                GlassState.Full => "Dok ve Yika",
                _ => "Bardak zaten temiz"
            };
        }

        public override void Interact(PlayerContext player)
        {
            if (IsBusy || targetShelf == null) return;

            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return;

            if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return;
            if (glass.State == GlassState.Empty) return;

            StartWashing(hands);
        }

        private void StartWashing(PlayerHands hands)
        {
            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            released.Detach(SlotPosition, SlotRotation, restorePhysics: false);
            released.SetPickupEnabled(false);
            glassBeingWashed = released;

            StartWork(washDuration, CompleteWash);
        }

        private void CompleteWash()
        {
            if (glassBeingWashed == null) return;

            // Bardak fiziksel olarak yok edilir ama STOKTAN DUSMEZ: temiz olarak rafa geri doner.
            Destroy(glassBeingWashed.gameObject);
            glassBeingWashed = null;

            targetShelf.AddCleanGlass();
        }
    }
}
