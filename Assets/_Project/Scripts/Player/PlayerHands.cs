using System;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Player
{
    /// <summary>
    /// Oyuncunun elleri: ayni anda TEK bir nesne tasinabilir.
    /// Elindeki nesne degistiginde OnHeldItemChanged event'i yayinlanir (Observer Pattern);
    /// UI ve dukkan istasyonlari bu event'e abone olur, kimse Update'te poll etmez.
    /// Singleton DEGIL: co-op'ta her oyuncunun kendi eli var. Istasyonlar eli PlayerContext'ten alir.
    /// </summary>
    public class PlayerHands : MonoBehaviour
    {
        [Header("Tutma Noktasi")]
        [Tooltip("Bos birakilirsa kameranin altinda otomatik olusturulur.")]
        [SerializeField] private Transform holdAnchor;
        [Tooltip("Otomatik olusturulan tutma noktasinin kameraya gore konumu.")]
        [SerializeField] private Vector3 autoAnchorOffset = new Vector3(0.3f, -0.25f, 0.55f);

        public HoldableItem HeldItem { get; private set; }
        public bool IsEmpty => HeldItem == null;

        /// <summary>Eldeki nesne degistiginde tetiklenir (el bosaldiysa null).</summary>
        public event Action<HoldableItem> OnHeldItemChanged;

        private void Awake()
        {
            EnsureHoldAnchor();
        }

        /// <summary>Tutma noktasi atanmadiysa kameranin altinda bir tane olusturur.</summary>
        private void EnsureHoldAnchor()
        {
            if (holdAnchor != null) return;

            Camera childCamera = GetComponentInChildren<Camera>();
            Transform parent = childCamera != null ? childCamera.transform : transform;

            var anchorObject = new GameObject("HoldAnchor");
            anchorObject.transform.SetParent(parent, worldPositionStays: false);
            anchorObject.transform.localPosition = autoAnchorOffset;
            holdAnchor = anchorObject.transform;
        }

        /// <summary>Eller bossa nesneyi ele alir. Basarili olursa true doner.</summary>
        public bool TryHold(HoldableItem item)
        {
            if (item == null || !IsEmpty) return false;

            HeldItem = item;
            item.AttachTo(holdAnchor);
            OnHeldItemChanged?.Invoke(HeldItem);
            return true;
        }

        /// <summary>
        /// Eldeki nesneyi birakip cagirana verir; nesne sahnede kalir ama artik elde degildir.
        /// Raf/lavabo gibi istasyonlar nesneyi teslim alirken bunu kullanir.
        /// </summary>
        public HoldableItem ReleaseHeldItem()
        {
            if (IsEmpty) return null;

            HoldableItem released = HeldItem;
            HeldItem = null;
            OnHeldItemChanged?.Invoke(null);
            return released;
        }

        /// <summary>Eldeki nesneyi verilen konuma birakir (masaya koyma, yere birakma).</summary>
        public bool TryDropAt(Vector3 position, Quaternion rotation, bool restorePhysics = true)
        {
            HoldableItem released = ReleaseHeldItem();
            if (released == null) return false;

            released.Detach(position, rotation, restorePhysics);
            return true;
        }
    }
}
