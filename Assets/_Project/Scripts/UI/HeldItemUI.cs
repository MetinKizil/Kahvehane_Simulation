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

        private TeaGlass trackedGlass;

        private void Start()
        {
            PlayerHands.Instance.OnHeldItemChanged += HandleHeldItemChanged;
            HandleHeldItemChanged(PlayerHands.Instance.HeldItem);
        }

        private void OnDestroy()
        {
            if (PlayerHands.Instance != null)
                PlayerHands.Instance.OnHeldItemChanged -= HandleHeldItemChanged;

            UnsubscribeFromGlass();
        }

        private void HandleHeldItemChanged(HoldableItem item)
        {
            UnsubscribeFromGlass();

            if (item != null && item.TryGetComponent(out trackedGlass))
                trackedGlass.OnStateChanged += HandleGlassStateChanged;

            Refresh(item);
        }

        private void HandleGlassStateChanged(GlassState state)
        {
            Refresh(PlayerHands.Instance.HeldItem);
        }

        private void Refresh(HoldableItem item)
        {
            bool hasItem = item != null;
            heldItemText.gameObject.SetActive(hasItem);

            if (!hasItem) return;

            heldItemText.text = trackedGlass != null
                ? $"Elinde: {item.DisplayName} ({trackedGlass.GetStateLabel()})"
                : $"Elinde: {item.DisplayName}";
        }

        private void UnsubscribeFromGlass()
        {
            if (trackedGlass == null) return;

            trackedGlass.OnStateChanged -= HandleGlassStateChanged;
            trackedGlass = null;
        }
    }
}
