using KanBagi.Interaction;
using KanBagi.Inventory;
using KanBagi.Items;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Depo rafi. Oyuncunun getirdigi koliyi bosaltip icerigini stoga yazar.
    /// Kolinin nereden geldigini bilmez; sadece "icinde ne var" diye sorar (SOLID).
    /// Kurye dongusunu kapatan halka.
    /// </summary>
    public class StorageRack : MonoBehaviour, IInteractable
    {
        public string GetPrompt(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return "Koli getir";

            if (!hands.HeldItem.TryGetComponent(out DeliveryBox box)) return "Bu depoya konmaz";
            if (box.IsEmpty) return "Koli bos";

            return $"Koliyi Bosalt ({box.Amount} {box.Content.ItemName})";
        }

        public void Interact(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return;

            if (!hands.HeldItem.TryGetComponent(out DeliveryBox box) || box.IsEmpty) return;

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[StorageRack] InventoryManager bulunamadi. 'Managers' objesine ekle.", this);
                return;
            }

            UnloadBox(hands, box);
        }

        private void UnloadBox(PlayerHands hands, DeliveryBox box)
        {
            InventoryManager.Instance.Add(box.Content, box.Amount);

            HoldableItem released = hands.ReleaseHeldItem();
            if (released != null) Destroy(released.gameObject);
        }
    }
}
