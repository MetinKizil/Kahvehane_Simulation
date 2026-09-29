using System;
using KanBagi.Data;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Items
{
    /// <summary>Bardagin icinde bulundugu durum. Bulasik dongusunun temeli.</summary>
    public enum GlassState
    {
        Empty, // Temiz ve bos: doldurulmaya hazir.
        Full,  // Dolu: musteriye servis edilebilir.
        Dirty  // Kirli: lavaboda yikanmasi gerekir.
    }

    /// <summary>
    /// Cay bardagi. Sadece KENDI durumunu ve gorselini yonetir (SOLID);
    /// kimin doldurdugunu, kimin yikadigini bilmez.
    /// Durum degistiginde OnStateChanged event'i yayinlar.
    /// </summary>
    [RequireComponent(typeof(HoldableItem))]
    public class TeaGlass : MonoBehaviour
    {
        [Header("Gorseller")]
        [Tooltip("Bardak doluyken gorunen sivi objesi.")]
        [SerializeField] private GameObject liquidVisual;
        [Tooltip("Bardak kirliyken gorunen artik/leke objesi (opsiyonel).")]
        [SerializeField] private GameObject dirtyVisual;

        public GlassState State { get; private set; } = GlassState.Empty;

        /// <summary>Bardagin icindeki icecek (bos veya kirliyken null).</summary>
        public ItemData Content { get; private set; }

        /// <summary>Durum degistiginde yeni durum ile tetiklenir.</summary>
        public event Action<GlassState> OnStateChanged;

        private void Start()
        {
            RefreshVisuals();
        }

        /// <summary>Bos bardagi verilen icecekle doldurur. Bos degilse false doner.</summary>
        public bool TryFill(ItemData beverage)
        {
            if (State != GlassState.Empty || beverage == null) return false;

            Content = beverage;
            SetState(GlassState.Full);
            return true;
        }

        /// <summary>Icecek tuketildi: bardak kirlenir. Musteri kalkinca cagrilir.</summary>
        public void MarkDirty()
        {
            if (State == GlassState.Dirty) return;

            Content = null;
            SetState(GlassState.Dirty);
        }

        /// <summary>Lavaboda yikandi: bardak temiz ve bos hale doner.</summary>
        public void Clean()
        {
            Content = null;
            SetState(GlassState.Empty);
        }

        private void SetState(GlassState newState)
        {
            State = newState;
            RefreshVisuals();
            OnStateChanged?.Invoke(State);
        }

        private void RefreshVisuals()
        {
            if (liquidVisual != null) liquidVisual.SetActive(State == GlassState.Full);
            if (dirtyVisual != null) dirtyVisual.SetActive(State == GlassState.Dirty);
        }

        /// <summary>UI'da gostermek icin okunabilir durum metni.</summary>
        public string GetStateLabel() => State switch
        {
            GlassState.Full => Content != null ? $"Dolu - {Content.ItemName}" : "Dolu",
            GlassState.Dirty => "Kirli",
            _ => "Bos"
        };
    }
}
