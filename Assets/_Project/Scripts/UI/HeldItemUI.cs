using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Player;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Oyuncunun elinde ne oldugunu gosterir.
    /// SADECE event dinler; envanter mantigi icermez (ANAYASA: UI, mantiktan ayri).
    /// Elde bardak varsa onun durum degisimlerine de abone olur.
    /// </summary>
    public class HeldItemUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text heldItemText;
        [Tooltip("Gosterilecek oyuncunun elleri. Bos birakilirsa sahnedeki oyuncu bulunur. " +
                 "Co-op'ta her ekran kendi oyuncusuna baglanir.")]
        [SerializeField] private PlayerHands hands;

        private TeaGlass trackedGlass;
        private Tray trackedTray;

        private void Start()
        {
            if (hands == null) hands = FindFirstObjectByType<PlayerHands>();

            if (hands == null)
            {
                Debug.LogError("[HeldItemUI] PlayerHands bulunamadi. 'Player' objesinde PlayerHands olmali " +
                               "veya Inspector'daki 'Hands' alanina surukle.", this);
                enabled = false;
                return;
            }

            hands.OnHeldItemChanged += HandleHeldItemChanged;
            HandleHeldItemChanged(hands.HeldItem);
        }

        private void OnDestroy()
        {
            if (hands != null)
                hands.OnHeldItemChanged -= HandleHeldItemChanged;

            UnsubscribeFromHeldItem();
        }

        private void HandleHeldItemChanged(HoldableItem item)
        {
            UnsubscribeFromHeldItem();

            if (item != null && item.TryGetComponent(out trackedGlass))
                trackedGlass.OnStateChanged += HandleGlassStateChanged;

            if (item != null && item.TryGetComponent(out trackedTray))
                trackedTray.OnContentsChanged += HandleTrayContentsChanged;

            Refresh(item);
        }

        private void HandleGlassStateChanged(GlassState state)
        {
            Refresh(hands.HeldItem);
        }

        private void HandleTrayContentsChanged()
        {
            Refresh(hands.HeldItem);
        }

        private void Refresh(HoldableItem item)
        {
            bool hasItem = item != null;
            heldItemText.gameObject.SetActive(hasItem);

            if (!hasItem) return;

            if (trackedGlass != null)
                heldItemText.text = $"Elinde: {item.DisplayName} ({trackedGlass.GetStateLabel()})";
            else if (trackedTray != null)
                heldItemText.text = $"Elinde: {item.DisplayName} ({trackedTray.Count}/{trackedTray.Capacity}) - {trackedTray.GetSummary()}";
            else
                heldItemText.text = $"Elinde: {item.DisplayName}";
        }

        private void UnsubscribeFromHeldItem()
        {
            if (trackedGlass != null)
            {
                trackedGlass.OnStateChanged -= HandleGlassStateChanged;
                trackedGlass = null;
            }

            if (trackedTray != null)
            {
                trackedTray.OnContentsChanged -= HandleTrayContentsChanged;
                trackedTray = null;
            }
        }
    }
}
