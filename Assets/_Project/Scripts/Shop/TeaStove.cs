using KanBagi.Data;
using KanBagi.Interaction;
using KanBagi.Inventory;
using KanBagi.Items;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Cay ocagi. Elindeki bos bardagi teslim alir, ItemData'daki sure kadar doldurur,
    /// sonra oyuncunun geri almasini bekler. Doldurma suresi ve icecek bilgisi
    /// ScriptableObject'ten gelir (ANAYASA: veriler ScriptableObject'te).
    /// Her bardak icin depodan sarf malzemesi duser; stok bitince calismaz.
    ///
    /// Tepsi: tepsi ocaga birakilinca ustundeki BIR bos bardak dolmaya baslar; ocak calisirken
    /// her E bir bardak daha siraya ekler (kac kez basilirsa o kadar bardak). Is bitince tepsi
    /// tekrar alinabilir olur (oyuncu tepsiye bakip alir).
    /// </summary>
    public class TeaStove : TimedStation
    {
        [Header("Icecek")]
        [Tooltip("Bu ocagin hazirladigi icecek (Cay ItemData asset'i).")]
        [SerializeField] private ItemData beverageData;

        [Header("Sarf Malzemesi")]
        [Tooltip("Bir bardak icin harcanan malzeme (CayYapragi). Bos birakilirsa hicbir sey harcanmaz.")]
        [SerializeField] private ItemData requiredConsumable;
        [Tooltip("Bir bardak icin depodan dusulecek miktar.")]
        [SerializeField] private int consumeAmount = 1;

        [Header("Bardak Yeri")]
        [Tooltip("Bos birakilirsa ocagin ustunde otomatik bir nokta kullanilir.")]
        [SerializeField] private Transform glassSlot;
        [SerializeField] private Vector3 slotOffset = new Vector3(0f, 0.6f, 0f);

        private TeaGlass glassOnStove;
        private HoldableItem holdableOnStove;

        private Tray trayOnStove;
        private HoldableItem trayHoldable;
        private TeaGlass fillingGlass;
        // Istenen bardak sayisi (su an dolan dahil). Her bardak bitince bir azalir.
        private int pendingFills;

        /// <summary>Ocakta duran tepsi; oyuncu tepsiyi geri aldiysa null.</summary>
        private Tray TrayOnStove
        {
            get
            {
                if (trayOnStove != null && trayHoldable != null && !trayHoldable.IsHeld) return trayOnStove;

                trayOnStove = null;
                trayHoldable = null;
                return null;
            }
        }

        private Vector3 SlotPosition => glassSlot != null ? glassSlot.position : transform.position + slotOffset;
        private Quaternion SlotRotation => glassSlot != null ? glassSlot.rotation : Quaternion.identity;

        /// <summary>Bir bardak daha yapacak kadar malzeme var mi? Malzeme atanmamissa hep true.</summary>
        private bool HasConsumableStock =>
            requiredConsumable == null ||
            (InventoryManager.Instance != null &&
             InventoryManager.Instance.HasStock(requiredConsumable, consumeAmount));

        private void Start()
        {
            // Awake'te bakmiyoruz: InventoryManager'in Awake'i bizden sonra calismis olabilir.
            if (requiredConsumable != null && InventoryManager.Instance == null)
                Debug.LogError("[TeaStove] InventoryManager bulunamadi. 'Managers' objesine InventoryManager ekle.", this);
        }

        public override string GetPrompt(PlayerContext player)
        {
            if (beverageData == null) return "Ocak ayarlanmamis";

            Tray stoveTray = TrayOnStove;
            if (stoveTray != null) return GetTrayOnStovePrompt(stoveTray);

            if (IsBusy) return WorkLabel;

            PlayerHands hands = player.Hands;
            if (glassOnStove != null)
                return hands != null && TryGetHeldTray(hands, out Tray heldTray) && !heldTray.IsFull
                    ? "Bardagi Tepsiye Al"
                    : "Bardagi Al";

            // Stok uyarisi elde bardak olmasa da gorunsun: oyuncu bosuna bardak tasimasin.
            if (!HasConsumableStock) return $"{requiredConsumable.ItemName} bitti";

            if (hands == null || hands.IsEmpty) return "Once bos bardak al";

            if (TryGetHeldTray(hands, out Tray tray))
                return tray.CountByState(GlassState.Empty) > 0
                    ? $"Tepsiyi Birak, {beverageData.ItemName} Doldur"
                    : "Tepside bos bardak yok";

            return TryGetEmptyHeldGlass(hands, out _) ? $"{beverageData.ItemName} Doldur" : "Elinde bos bardak olmali";
        }

        /// <summary>Tepsi ocaktayken: siraya bardak eklenebilir mi, yoksa tepsi alinmayi mi bekliyor?</summary>
        private string GetTrayOnStovePrompt(Tray tray)
        {
            if (CanQueueTrayFill(tray))
                return IsBusy
                    ? $"Bir {beverageData.ItemName} Daha ({pendingFills} sirada)"
                    : $"{beverageData.ItemName} Doldur ({tray.CountByState(GlassState.Empty)} bos)";

            if (IsBusy) return $"{WorkLabel} ({pendingFills} sirada)";

            return !HasConsumableStock && tray.CountByState(GlassState.Empty) > 0
                ? $"{requiredConsumable.ItemName} bitti - tepsiyi al"
                : "Tepsiyi almak icin tepsiye bak";
        }

        public override void Interact(PlayerContext player)
        {
            if (beverageData == null) return;

            PlayerHands hands = player.Hands;
            if (hands == null) return;

            Tray stoveTray = TrayOnStove;
            if (stoveTray != null)
            {
                QueueTrayFill(stoveTray);
                return;
            }

            if (IsBusy) return;

            if (glassOnStove != null)
            {
                TakeGlassBack(hands);
                return;
            }

            if (TryGetHeldTray(hands, out Tray tray))
            {
                PlaceTrayAndFill(hands, tray);
                return;
            }

            if (!TryGetEmptyHeldGlass(hands, out TeaGlass glass)) return;
            if (!TryConsumeIngredient()) return;

            PlaceGlassAndFill(hands, glass);
        }

        private static bool TryGetHeldTray(PlayerHands hands, out Tray tray)
        {
            tray = null;
            return hands.HeldItem != null && hands.HeldItem.TryGetComponent(out tray);
        }

        /// <summary>Tepside henuz siraya alinmamis bos bardak ve en az bir bardaklik malzeme var mi?</summary>
        private bool CanQueueTrayFill(Tray tray) =>
            pendingFills < tray.CountByState(GlassState.Empty) && HasConsumableStock;

        private void PlaceTrayAndFill(PlayerHands hands, Tray tray)
        {
            if (!CanQueueTrayFill(tray)) return;

            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            released.Detach(SlotPosition, SlotRotation, restorePhysics: false);
            trayOnStove = tray;
            trayHoldable = released;

            pendingFills = 1;
            StartNextTrayFill();
        }

        /// <summary>Ocak calisirken siraya bir bardak ekler; bostaysa hemen baslatir.</summary>
        private void QueueTrayFill(Tray tray)
        {
            if (!CanQueueTrayFill(tray)) return;

            if (IsBusy)
            {
                pendingFills++;
                return;
            }

            pendingFills = 1;
            StartNextTrayFill();
        }

        /// <summary>Tepsideki siradaki bos bardagi doldurmaya baslar. Malzeme her bardakta ayri duser.</summary>
        private void StartNextTrayFill()
        {
            fillingGlass = trayOnStove != null ? trayOnStove.FindByState(GlassState.Empty) : null;

            if (fillingGlass == null || !TryConsumeIngredient())
            {
                FinishTrayFills();
                return;
            }

            // Dolum sirasinda tepsi alinamasin.
            trayHoldable.SetPickupEnabled(false);
            StartWork(beverageData.PrepareDuration, CompleteTrayFill);
        }

        private void CompleteTrayFill()
        {
            if (fillingGlass != null) fillingGlass.TryFill(beverageData);
            fillingGlass = null;
            pendingFills--;

            if (pendingFills > 0 && trayOnStove != null)
                StartNextTrayFill();
            else
                FinishTrayFills();
        }

        /// <summary>Sira bitti (veya malzeme tukendi): tepsi tekrar alinabilir.</summary>
        private void FinishTrayFills()
        {
            pendingFills = 0;
            fillingGlass = null;

            if (trayHoldable != null) trayHoldable.SetPickupEnabled(true);
        }

        /// <summary>Elde doldurulmaya hazir (temiz ve bos) bir bardak var mi?</summary>
        private static bool TryGetEmptyHeldGlass(PlayerHands hands, out TeaGlass glass)
        {
            glass = null;
            return hands.HeldItem != null
                   && hands.HeldItem.TryGetComponent(out glass)
                   && glass.State == GlassState.Empty;
        }

        /// <summary>Malzeme doldurma BASLAMADAN dusulur; bardak masaya gitmese de cay yapragi yandi sayilir.</summary>
        private bool TryConsumeIngredient()
        {
            if (requiredConsumable == null) return true;
            if (InventoryManager.Instance == null) return false;

            return InventoryManager.Instance.TryConsume(requiredConsumable, consumeAmount);
        }

        private void PlaceGlassAndFill(PlayerHands hands, TeaGlass glass)
        {
            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            // Fizigi kapali birakiyoruz: dolarken bardak alinamasin ve ocaktan kaymasin.
            released.Detach(SlotPosition, SlotRotation, restorePhysics: false);
            released.SetPickupEnabled(false);

            glassOnStove = glass;
            holdableOnStove = released;

            StartWork(beverageData.PrepareDuration, CompleteFill);
        }

        private void CompleteFill()
        {
            if (glassOnStove == null) return;

            glassOnStove.TryFill(beverageData);
            holdableOnStove.SetPickupEnabled(true);
        }

        /// <summary>Dolan bardagi ele alir; elde tepsi varsa tepsiye koyar.</summary>
        private void TakeGlassBack(PlayerHands hands)
        {
            bool taken = hands.IsEmpty
                ? hands.TryHold(holdableOnStove)
                : TryGetHeldTray(hands, out Tray tray) && tray.TryAdd(glassOnStove);

            if (!taken) return;

            glassOnStove = null;
            holdableOnStove = null;
        }
    }
}
