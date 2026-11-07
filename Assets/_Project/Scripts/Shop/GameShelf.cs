using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Oyun takimi rafi (D8a): masadan kalkan okey/iskambil/domino kutulari buraya birakilir, sonra tekrar alinir.
    /// Elde kutuyla rafa bakip E -> bos yuvaya konur. Kutu rafta gercek obje olarak durur; ona bakip E ile alinir
    /// (HoldableItem'in kendi etkilesimi). Yuvalar rafin cocugu oldugu icin raf tasininca kutular da gelir.
    /// Raftaki kutular kayda girer (IFurnitureState: yuva sirasiyla takim adlari).
    /// </summary>
    public class GameShelf : MonoBehaviour, IInteractable, IFurnitureState
    {
        [Tooltip("Kutularin duracagi yuvalar (rafin cocuklari).")]
        [SerializeField] private Transform[] slots = new Transform[0];
        [Tooltip("Kayittan geri yuklerken takimi adindan bulmak icin.")]
        [SerializeField] private TableGameData[] knownGames = new TableGameData[0];

        [Header("Ipucu Metinleri")]
        [SerializeField] private string storePrompt = "Rafa koy";
        [SerializeField] private string fullPrompt = "Raf dolu";
        [SerializeField] private string idlePrompt = "Oyun rafi";

        private GameSetItem[] stored;

        private void Awake() => EnsureArray();

        private void EnsureArray()
        {
            if (stored == null || stored.Length != slots.Length) stored = new GameSetItem[slots.Length];
        }

        /// <summary>Yuva bos mu? (Kutu oradan alinmissa yuva bosalmistir.)</summary>
        private bool IsSlotFree(int slot)
        {
            GameSetItem item = stored[slot];
            return item == null || item.transform.parent != slots[slot];
        }

        private int FindFreeSlot()
        {
            EnsureArray();
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && IsSlotFree(i)) return i;
            }

            return -1;
        }

        public string GetPrompt(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty || !hands.HeldItem.TryGetComponent(out GameSetItem _)) return idlePrompt;

            return FindFreeSlot() >= 0 ? storePrompt : fullPrompt;
        }

        public void Interact(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty || !hands.HeldItem.TryGetComponent(out GameSetItem set)) return;

            int slot = FindFreeSlot();
            if (slot < 0) return;

            HoldableItem box = hands.ReleaseHeldItem();
            if (box != null) PutInSlot(slot, box, set);
        }

        private void PutInSlot(int slot, HoldableItem box, GameSetItem set)
        {
            box.Detach(slots[slot].position, slots[slot].rotation, restorePhysics: false);
            box.transform.SetParent(slots[slot], worldPositionStays: true);
            box.SetPickupEnabled(true);
            stored[slot] = set;
        }

        // ---- Kayit ----

        public string CaptureFurnitureState()
        {
            EnsureArray();
            var names = new string[slots.Length];
            for (int i = 0; i < slots.Length; i++)
                names[i] = !IsSlotFree(i) && stored[i].Game != null ? stored[i].Game.name : string.Empty;

            return string.Join(";", names);
        }

        public void RestoreFurnitureState(string state)
        {
            EnsureArray();
            if (string.IsNullOrEmpty(state)) return;

            string[] names = state.Split(';');
            for (int i = 0; i < names.Length && i < slots.Length; i++)
            {
                TableGameData game = FindGame(names[i]);
                if (game == null || game.BoxPrefab == null) continue;

                GameObject box = Instantiate(game.BoxPrefab);
                box.name = game.BoxPrefab.name;
                if (box.TryGetComponent(out HoldableItem item) && box.TryGetComponent(out GameSetItem set))
                {
                    // Elde degilken de fizik kapali olsun: rafin icinde dussun istemiyoruz.
                    if (box.TryGetComponent(out Rigidbody body)) body.isKinematic = true;
                    PutInSlot(i, item, set);
                }
                else Destroy(box);
            }
        }

        private TableGameData FindGame(string gameName)
        {
            if (string.IsNullOrEmpty(gameName)) return null;

            foreach (TableGameData game in knownGames)
            {
                if (game != null && game.name == gameName) return game;
            }

            return null;
        }
    }
}
