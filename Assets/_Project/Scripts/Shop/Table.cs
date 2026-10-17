using KanBagi.Customers;
using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Bir masa: oturma noktasi, bardak koyma noktasi ve doluluk durumu.
    /// Oyuncu elindeki dolu bardagi buraya birakarak musteriye servis eder.
    /// Masa kimin oturdugunu ve uzerinde bardak olup olmadigini bilir;
    /// musteri davranisina veya paraya karismaz (SOLID).
    /// </summary>
    public class Table : MonoBehaviour, IInteractable
    {
        [Header("Oturma Noktasi")]
        [Tooltip("Bos birakilirsa asagidaki ofset kullanilir.")]
        [SerializeField] private Transform seatPoint;
        [Tooltip("Masaya gore oturma noktasi (yerel yon, olcekten etkilenmez).")]
        [SerializeField] private Vector3 seatOffset = new Vector3(0f, 0f, -1.2f);

        [Header("Bardak Noktasi")]
        [Tooltip("Bos birakilirsa asagidaki ofset kullanilir.")]
        [SerializeField] private Transform glassPoint;
        [SerializeField] private Vector3 glassOffset = new Vector3(0f, 0.5f, 0f);

        private HoldableItem placedGlassItem;

        /// <summary>Masada oturan musteri (bossa null).</summary>
        public Customer Occupant { get; private set; }

        public bool IsFree => Occupant == null;

        /// <summary>
        /// Masada bardak duruyor mu? Oyuncu bardagi eline aldiginda veya
        /// bardak lavaboda yok edildiginde yuva kendiliginden bosalir.
        /// </summary>
        public bool HasGlass => placedGlassItem != null && !placedGlassItem.IsHeld;

        public Vector3 SeatPosition => seatPoint != null
            ? seatPoint.position
            : transform.position + transform.rotation * seatOffset;

        public Vector3 GlassPosition => glassPoint != null
            ? glassPoint.position
            : transform.position + transform.rotation * glassOffset;

        public Quaternion GlassRotation => glassPoint != null ? glassPoint.rotation : Quaternion.identity;

        public string GetPrompt(PlayerContext player)
        {
            if (player.Hands != null && TryGetHeldTray(player.Hands, out Tray tray))
                return GetTrayPrompt(tray);

            if (Occupant != null && Occupant.IsWaitingForOrder)
            {
                PlayerHands hands = player.Hands;

                if (hands == null || hands.IsEmpty) return "Siparisi getir";
                if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return "Bu masaya konmaz";
                if (!Occupant.CanAcceptOrder(glass)) return "Musteri bunu istemiyor";

                return HasGlass ? "Once masadaki bardagi kaldir" : "Siparisi Ver";
            }

            if (Occupant != null) return "Musteri oturuyor";

            return HasGlass ? "Masada bardak var" : "Bos masa";
        }

        public void Interact(PlayerContext player)
        {
            PlayerHands hands = player.Hands;
            if (hands == null || hands.IsEmpty) return;

            if (TryGetHeldTray(hands, out Tray tray))
            {
                ServeWithTray(tray);
                return;
            }

            if (HasGlass || Occupant == null || !Occupant.IsWaitingForOrder) return;

            if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return;
            if (!Occupant.CanAcceptOrder(glass)) return;

            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            // Fizik kapali kalir: bardak masanin uzerinde birakildigi yerde durur, yuvarlanmaz.
            released.Detach(GlassPosition, GlassRotation, restorePhysics: false);
            PlaceAndServe(released, glass);
        }

        // ---- Tepsi ----

        private static bool TryGetHeldTray(PlayerHands hands, out Tray tray)
        {
            tray = null;
            return hands.HeldItem != null && hands.HeldItem.TryGetComponent(out tray);
        }

        /// <summary>
        /// Masadaki bardak toplanabilir mi? Musteri icerken bardak doludur ve alinamaz;
        /// icilmis (kirli) veya bos birakilmis bardak toplanir.
        /// </summary>
        private bool TryGetCollectableGlass(out TeaGlass glass)
        {
            glass = null;
            return HasGlass
                   && placedGlassItem.TryGetComponent(out glass)
                   && glass.State != GlassState.Full;
        }

        /// <summary>Tepside bu masadaki musterinin kabul edecegi bir bardak var mi?</summary>
        private TeaGlass FindOrderOnTray(Tray tray) =>
            Occupant != null && Occupant.IsWaitingForOrder ? tray.Find(Occupant.CanAcceptOrder) : null;

        private string GetTrayPrompt(Tray tray)
        {
            bool canCollect = TryGetCollectableGlass(out _) && !tray.IsFull;
            TeaGlass order = FindOrderOnTray(tray);
            // Kirli bardak kalkmadan servis olmaz: toplanamiyorsa (tepsi dolu) servis de yok.
            bool canServe = order != null && (!HasGlass || canCollect);

            if (canCollect && canServe) return "Kirliyi Topla + Servis Et";
            if (canServe) return $"Servis Et ({order.Content.ItemName})";
            if (canCollect) return "Kirli Bardagi Tepsiye Al";

            if (TryGetCollectableGlass(out _)) return "Tepsi dolu";
            if (Occupant != null && Occupant.IsWaitingForOrder && Occupant.Order != null)
                return $"Tepside {Occupant.Order.ItemName} yok";

            return Occupant != null ? "Musteri oturuyor" : "Bos masa";
        }

        /// <summary>Tek basista: once masadaki kirli bardak tepsiye, sonra bekleyen musteriye istedigi bardak.</summary>
        private void ServeWithTray(Tray tray)
        {
            if (TryGetCollectableGlass(out TeaGlass dirty) && tray.TryAdd(dirty))
                placedGlassItem = null;

            if (HasGlass) return;

            TeaGlass order = FindOrderOnTray(tray);
            if (order == null) return;

            // Bardak masaya inince alinabilir olur: icilip kirlenince tekrar toplanacak.
            if (!tray.TryTakeOut(order, GlassPosition, GlassRotation, pickupEnabled: true)) return;

            PlaceAndServe(order.GetComponent<HoldableItem>(), order);
        }

        /// <summary>Masaya inmis bardagi yuvaya kaydeder ve musteriye teslim eder.</summary>
        private void PlaceAndServe(HoldableItem placed, TeaGlass glass)
        {
            // Collider'i yeniden aciyoruz ki oyuncu kirlendikten sonra alabilsin.
            placed.SetPickupEnabled(true);
            placedGlassItem = placed;

            if (!Occupant.TryServe(glass))
                Debug.LogWarning($"[Table] '{name}' uzerindeki musteri siparisi kabul etmedi.", this);
        }

        /// <summary>Masa bossa musteriye ayirir.</summary>
        public bool TryReserve(Customer customer)
        {
            if (!IsFree || customer == null) return false;

            Occupant = customer;
            return true;
        }

        /// <summary>Musteri kalkinca masayi serbest birakir.</summary>
        public void Release() => Occupant = null;

        private void OnEnable() => TableManager.Instance?.Register(this);

        private void OnDisable() => TableManager.Instance?.Unregister(this);

        /// <summary>Sahne penceresinde oturma ve bardak noktalarini gorsellestirir.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(SeatPosition, 0.25f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GlassPosition, 0.1f);
        }
    }
}
