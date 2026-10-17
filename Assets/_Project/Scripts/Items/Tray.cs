using System;
using System.Collections.Generic;
using KanBagi.Data;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Items
{
    /// <summary>
    /// Servis tepsisi: uzerinde en fazla 'capacity' bardak tasir (dolu, bos veya kirli).
    /// Sadece KENDI icerigini yonetir (SOLID): bardaklari yuvalara dizer, ekler, cikarir, arar.
    /// Kimin doldurdugunu / kime servis edildigini bilmez; bunu istasyonlar (raf, ocak, masa, lavabo) yapar.
    ///
    /// Tepsideki bardaklar "elde tutuluyor" sayilir (HoldableItem.IsHeld) ve collider'lari kapalidir:
    /// boylece isin tepsiye carpar ve masa/ocak yuvalari bardagi "kaldirilmis" gorur.
    /// Kurulum: Kan Bagi > Tepsiyi Kur
    /// </summary>
    [RequireComponent(typeof(HoldableItem))]
    public class Tray : MonoBehaviour
    {
        [Header("Kapasite")]
        [SerializeField] private int capacity = 8;
        [Tooltip("Yuvalarin dizilisi (sutun x satir). sutun x satir >= capacity olmali.")]
        [SerializeField] private Vector2Int grid = new Vector2Int(4, 2);
        [Tooltip("Iki yuva arasindaki mesafe (metre).")]
        [SerializeField] private Vector2 spacing = new Vector2(0.1f, 0.12f);

        [Header("Yukseklik")]
        [Tooltip("Tepsinin ust yuzeyinin yerel yuksekligi.")]
        [SerializeField] private float surfaceHeight = 0.03f;
        [Tooltip("Bardagin pivotu ortasinda oldugu icin yuzeyden bu kadar yukari konur (bardak yuksekliginin yarisi).")]
        [SerializeField] private float glassLift = 0.05f;

        private TeaGlass[] slots;
        private Transform[] anchors;

        public int Capacity => capacity;
        public int Count { get; private set; }
        public bool IsFull => Count >= capacity;
        public bool IsEmpty => Count == 0;

        /// <summary>Bardak eklenince/cikinca veya tepsideki bir bardagin durumu degisince tetiklenir. UI abone olur.</summary>
        public event Action OnContentsChanged;

        private void Awake()
        {
            capacity = Mathf.Max(1, capacity);
            slots = new TeaGlass[capacity];
            anchors = new Transform[capacity];

            for (int i = 0; i < capacity; i++)
                anchors[i] = CreateAnchor(i);
        }

        /// <summary>Yuva noktalari tepsinin cocugu olarak olusturulur; bardak AttachTo ile buraya yapisir.</summary>
        private Transform CreateAnchor(int index)
        {
            int columns = Mathf.Max(1, grid.x);
            int rows = Mathf.Max(1, Mathf.CeilToInt(capacity / (float)columns));
            int column = index % columns;
            int row = index / columns;

            var anchor = new GameObject($"Yuva{index + 1}").transform;
            anchor.SetParent(transform, worldPositionStays: false);
            anchor.localPosition = new Vector3(
                (column - (columns - 1) * 0.5f) * spacing.x,
                surfaceHeight + glassLift,
                (row - (rows - 1) * 0.5f) * spacing.y);
            return anchor;
        }

        /// <summary>Bardagi ilk bos yuvaya koyar. Tepsi doluysa veya bardak zaten tepsideyse false.</summary>
        public bool TryAdd(TeaGlass glass)
        {
            if (glass == null || IsFull || Contains(glass)) return false;
            if (!glass.TryGetComponent(out HoldableItem holdable)) return false;

            int index = Array.IndexOf(slots, null);
            if (index < 0) return false;

            // AttachTo bardagi "tutuluyor" yapar ve collider'larini kapatir.
            holdable.AttachTo(anchors[index]);
            slots[index] = glass;
            Count++;

            glass.OnStateChanged += HandleGlassStateChanged;
            OnContentsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Bardagi tepsiden cikarip verilen yere birakir (masa, ocak...). Fizik kapali kalir;
        /// alinabilir olup olmayacagina cagiran karar verir.
        /// </summary>
        public bool TryTakeOut(TeaGlass glass, Vector3 position, Quaternion rotation, bool pickupEnabled)
        {
            int index = Array.IndexOf(slots, glass);
            if (index < 0 || !glass.TryGetComponent(out HoldableItem holdable)) return false;

            Untrack(index);
            holdable.Detach(position, rotation, restorePhysics: false);
            holdable.SetPickupEnabled(pickupEnabled);

            OnContentsChanged?.Invoke();
            return true;
        }

        /// <summary>Bardagi tepsiden cikarip yok eder (lavabo yikayip rafa iade ederken).</summary>
        public bool TryRemoveAndDestroy(TeaGlass glass)
        {
            int index = Array.IndexOf(slots, glass);
            if (index < 0) return false;

            Untrack(index);
            Destroy(glass.gameObject);

            OnContentsChanged?.Invoke();
            return true;
        }

        public bool Contains(TeaGlass glass) => glass != null && Array.IndexOf(slots, glass) >= 0;

        /// <summary>Bu durumdaki bardak sayisi.</summary>
        public int CountByState(GlassState state)
        {
            int count = 0;
            foreach (TeaGlass glass in slots)
            {
                if (glass != null && glass.State == state) count++;
            }

            return count;
        }

        /// <summary>Bu durumdaki ilk bardak; yoksa null.</summary>
        public TeaGlass FindByState(GlassState state)
        {
            foreach (TeaGlass glass in slots)
            {
                if (glass != null && glass.State == state) return glass;
            }

            return null;
        }

        /// <summary>Kosula uyan ilk bardak (orn. musterinin kabul edecegi dolu bardak); yoksa null.</summary>
        public TeaGlass Find(Predicate<TeaGlass> match)
        {
            foreach (TeaGlass glass in slots)
            {
                if (glass != null && match(glass)) return glass;
            }

            return null;
        }

        /// <summary>Kosula uyan TUM bardaklar (lavabo kirlileri toplu yikarken).</summary>
        public List<TeaGlass> FindAll(Predicate<TeaGlass> match)
        {
            var result = new List<TeaGlass>();
            foreach (TeaGlass glass in slots)
            {
                if (glass != null && match(glass)) result.Add(glass);
            }

            return result;
        }

        /// <summary>UI icin ozet: "3 Cay, 1 Kahve, 2 bos, 1 kirli". Icecek adlari ItemData'dan gelir.</summary>
        public string GetSummary()
        {
            if (IsEmpty) return "bos";

            var drinkCounts = new Dictionary<ItemData, int>();
            var drinkOrder = new List<ItemData>();

            foreach (TeaGlass glass in slots)
            {
                if (glass == null || glass.State != GlassState.Full || glass.Content == null) continue;

                if (!drinkCounts.ContainsKey(glass.Content))
                {
                    drinkCounts[glass.Content] = 0;
                    drinkOrder.Add(glass.Content);
                }

                drinkCounts[glass.Content]++;
            }

            var parts = new List<string>();
            foreach (ItemData drink in drinkOrder)
                parts.Add($"{drinkCounts[drink]} {drink.ItemName}");

            int empty = CountByState(GlassState.Empty);
            int dirty = CountByState(GlassState.Dirty);
            if (empty > 0) parts.Add($"{empty} bos");
            if (dirty > 0) parts.Add($"{dirty} kirli");

            return string.Join(", ", parts);
        }

        private void Untrack(int index)
        {
            TeaGlass glass = slots[index];
            if (glass != null) glass.OnStateChanged -= HandleGlassStateChanged;

            slots[index] = null;
            Count--;
        }

        private void HandleGlassStateChanged(GlassState state) => OnContentsChanged?.Invoke();

        private void OnDestroy()
        {
            if (slots == null) return;

            foreach (TeaGlass glass in slots)
            {
                if (glass != null) glass.OnStateChanged -= HandleGlassStateChanged;
            }
        }
    }
}
