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
            if (IsBusy) return WorkLabel;
            if (glassOnStove != null) return "Bardagi Al";

            // Stok uyarisi elde bardak olmasa da gorunsun: oyuncu bosuna bardak tasimasin.
            if (!HasConsumableStock) return $"{requiredConsumable.ItemName} bitti";

            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return "Once bos bardak al";

            return TryGetEmptyHeldGlass(hands, out _) ? $"{beverageData.ItemName} Doldur" : "Elinde bos bardak olmali";
        }

        public override void Interact(PlayerContext player)
        {
            if (beverageData == null || IsBusy) return;

            PlayerHands hands = player.Hands;
            if (hands == null) return;

            if (glassOnStove != null)
            {
                TakeGlassBack(hands);
                return;
            }

            if (!TryGetEmptyHeldGlass(hands, out TeaGlass glass)) return;
            if (!TryConsumeIngredient()) return;

            PlaceGlassAndFill(hands, glass);
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

        private void TakeGlassBack(PlayerHands hands)
        {
            if (!hands.IsEmpty) return;

            if (hands.TryHold(holdableOnStove))
            {
                glassOnStove = null;
                holdableOnStove = null;
            }
        }
    }
}
