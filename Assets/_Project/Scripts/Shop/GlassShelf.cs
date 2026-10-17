using System;
using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Temiz bardak rafi. Temiz bardak stogunun TEK sahibidir.
    /// Oyuncu buradan bardak alir; bos bardagi geri koyabilir.
    /// Lavabo da yikadigi bardagi buraya iade edecek (Faz 1 - bulasik dongusu).
    /// </summary>
    public class GlassShelf : MonoBehaviour, IInteractable
    {
        [Header("Bardak")]
        [Tooltip("Raftan alindiginda olusturulacak bardak prefab'i.")]
        [SerializeField] private GameObject glassPrefab;
        [Tooltip("Bos birakilirsa rafin kendi konumu kullanilir.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Stok")]
        [SerializeField] private int cleanGlassCount = 10;

        /// <summary>Temiz bardak sayisi degistiginde tetiklenir.</summary>
        public event Action<int> OnCleanGlassCountChanged;

        public int CleanGlassCount => cleanGlassCount;

        public string GetPrompt(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (hands == null) return "Bardak Rafi";

            if (hands.IsEmpty)
                return cleanGlassCount > 0 ? $"Temiz Bardak Al ({cleanGlassCount})" : "Temiz bardak kalmadi";

            if (TryGetHeldTray(hands, out Tray tray))
            {
                if (tray.IsFull) return "Tepsi dolu";
                return cleanGlassCount > 0
                    ? $"Tepsiye Bardak Koy ({tray.Count}/{tray.Capacity})"
                    : "Temiz bardak kalmadi";
            }

            return CanReturnHeldGlass(hands) ? "Bardagi Rafa Koy" : "Once elindekini birak";
        }

        public void Interact(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (hands == null) return;

            if (hands.IsEmpty)
                TakeGlass(hands);
            else if (TryGetHeldTray(hands, out Tray tray))
                PutGlassOnTray(tray);
            else if (CanReturnHeldGlass(hands))
                ReturnGlass(hands);
        }

        private static bool TryGetHeldTray(PlayerHands hands, out Tray tray)
        {
            tray = null;
            return hands.HeldItem != null && hands.HeldItem.TryGetComponent(out tray);
        }

        /// <summary>Her basista tepsiye BIR temiz bardak (kac bardak goturulecegine oyuncu karar verir).</summary>
        private void PutGlassOnTray(Tray tray)
        {
            if (tray.IsFull || cleanGlassCount <= 0) return;

            HoldableItem glassItem = SpawnGlass();
            if (glassItem == null) return;

            if (glassItem.TryGetComponent(out TeaGlass glass) && tray.TryAdd(glass))
                ChangeCount(-1);
            else
                Destroy(glassItem.gameObject);
        }

        /// <summary>Elde temiz ve bos bir bardak varsa rafa geri konabilir.</summary>
        private static bool CanReturnHeldGlass(PlayerHands hands)
        {
            return hands.HeldItem != null
                   && hands.HeldItem.TryGetComponent(out TeaGlass glass)
                   && glass.State == GlassState.Empty;
        }

        private void TakeGlass(PlayerHands hands)
        {
            if (cleanGlassCount <= 0) return;

            HoldableItem holdable = SpawnGlass();
            if (holdable == null) return;

            if (hands.TryHold(holdable))
                ChangeCount(-1);
            else
                Destroy(holdable.gameObject);
        }

        /// <summary>Raf noktasinda yeni bir bardak olusturur (stoktan dusmek cagiranin isi).</summary>
        private HoldableItem SpawnGlass()
        {
            if (glassPrefab == null) return null;

            Transform origin = spawnPoint != null ? spawnPoint : transform;
            GameObject glassObject = Instantiate(glassPrefab, origin.position, origin.rotation);

            if (glassObject.TryGetComponent(out HoldableItem holdable)) return holdable;

            Debug.LogError($"[GlassShelf] '{glassPrefab.name}' prefab'inda HoldableItem yok.", this);
            Destroy(glassObject);
            return null;
        }

        private void ReturnGlass(PlayerHands hands)
        {
            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            Destroy(released.gameObject);
            ChangeCount(+1);
        }

        /// <summary>Lavabo yikadigi bardagi stoga iade ederken kullanir.</summary>
        public void AddCleanGlass() => ChangeCount(+1);

        private void ChangeCount(int delta)
        {
            cleanGlassCount = Mathf.Max(0, cleanGlassCount + delta);
            OnCleanGlassCountChanged?.Invoke(cleanGlassCount);
        }
    }
}
