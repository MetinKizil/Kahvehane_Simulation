using KanBagi.Furniture;
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
    ///
    /// Tepsi: lavaboya birakilan tepsideki TUM kirli ve dolu (dokulur) bardaklar toplu yikanir;
    /// bardak basi sure tekli yikamadan kisadir. Temiz-bos bardaklar tepside kalir.
    /// Is bitince tepsi tekrar alinabilir (oyuncu tepsiye bakip alir).
    /// </summary>
    public class Sink : TimedStation, IFurnitureMoveCheck
    {
        [Header("Stok")]
        [Tooltip("Yikanan bardagin iade edilecegi raf. Bos birakilirsa sahnedeki ilk raf bulunur.")]
        [SerializeField] private GlassShelf targetShelf;

        [Header("Yikama")]
        [SerializeField] private float washDuration = 2.5f;
        [Tooltip("Tepsiyle toplu yikamada bardak basina sure (tekli yikamadan kisa: toplu yikama indirimi).")]
        [SerializeField] private float trayWashDurationPerGlass = 1.5f;

        [Header("Bardak Yeri")]
        [Tooltip("Bos birakilirsa lavabonun ustunde otomatik bir nokta kullanilir.")]
        [SerializeField] private Transform glassSlot;
        [SerializeField] private Vector3 slotOffset = new Vector3(0f, 0.5f, 0f);

        private HoldableItem glassBeingWashed;
        private Tray trayOnSink;
        private HoldableItem trayHoldable;

        /// <summary>Lavaboda duran tepsi; oyuncu geri aldiysa null.</summary>
        private Tray TrayOnSink
        {
            get
            {
                if (trayOnSink != null && trayHoldable != null && !trayHoldable.IsHeld) return trayOnSink;

                trayOnSink = null;
                trayHoldable = null;
                return null;
            }
        }

        private Vector3 SlotPosition => glassSlot != null ? glassSlot.position : transform.position + slotOffset;
        private Quaternion SlotRotation => glassSlot != null ? glassSlot.rotation : Quaternion.identity;

        /// <summary>
        /// Iade edilecek raf, ihtiyac aninda bulunur: D5'ten beri raf tasinabilir esya, lavabodan sonra
        /// olusturulmus veya kayittan yeniden kurulmus olabilir.
        /// </summary>
        private GlassShelf Shelf
        {
            get
            {
                if (targetShelf == null) targetShelf = FindFirstObjectByType<GlassShelf>();
                return targetShelf;
            }
        }

        /// <summary>Yikama surerken veya ustunde tepsi varken lavabo tasinamaz.</summary>
        public bool CanMove(out string reason)
        {
            reason = IsBusy ? "Lavabo yikiyor, tasinamaz"
                   : TrayOnSink != null ? "Lavabonun ustunde tepsi var"
                   : null;
            return reason == null;
        }

        public override string GetPrompt(PlayerContext player)
        {
            if (IsBusy) return WorkLabel;
            if (TrayOnSink != null) return "Tepsiyi almak icin tepsiye bak";

            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return "Kirli bardak getir";

            if (hands.HeldItem.TryGetComponent(out Tray tray))
            {
                int toWash = CountWashable(tray);
                return toWash > 0 ? $"Tepsiyi Yika ({toWash} bardak)" : "Tepside kirli bardak yok";
            }

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
            if (IsBusy || Shelf == null || TrayOnSink != null) return;

            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return;

            if (hands.HeldItem.TryGetComponent(out Tray tray))
            {
                StartTrayWash(hands, tray);
                return;
            }

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

            Shelf.AddCleanGlass();
        }

        // ---- Tepsi ----

        /// <summary>Kirli ve dolu bardaklar yikanir (dolu olan dokulur); temiz-bos olanlar tepside kalir.</summary>
        private static bool IsWashable(TeaGlass glass) => glass.State != GlassState.Empty;

        private static int CountWashable(Tray tray) =>
            tray.CountByState(GlassState.Dirty) + tray.CountByState(GlassState.Full);

        private void StartTrayWash(PlayerHands hands, Tray tray)
        {
            int toWash = CountWashable(tray);
            if (toWash == 0) return;

            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            // Yikama bitene kadar tepsi alinamaz.
            released.Detach(SlotPosition, SlotRotation, restorePhysics: false);
            released.SetPickupEnabled(false);
            trayOnSink = tray;
            trayHoldable = released;

            StartWork(toWash * trayWashDurationPerGlass, CompleteTrayWash);
        }

        private void CompleteTrayWash()
        {
            if (trayOnSink == null) return;

            // Yikanan bardaklar yok edilir ama stoktan dusmez: temiz olarak rafa doner.
            foreach (TeaGlass glass in trayOnSink.FindAll(IsWashable))
            {
                if (trayOnSink.TryRemoveAndDestroy(glass)) Shelf.AddCleanGlass();
            }

            trayHoldable.SetPickupEnabled(true);
        }
    }
}
