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

        public string InteractionPrompt
        {
            get
            {
                if (Occupant != null && Occupant.IsWaitingForOrder)
                {
                    PlayerHands hands = PlayerHands.Instance;

                    if (hands == null || hands.IsEmpty) return "Siparisi getir";
                    if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return "Bu masaya konmaz";
                    if (!Occupant.CanAcceptOrder(glass)) return "Musteri bunu istemiyor";

                    return HasGlass ? "Once masadaki bardagi kaldir" : "Siparisi Ver";
                }

                if (Occupant != null) return "Musteri oturuyor";

                return HasGlass ? "Masada bardak var" : "Bos masa";
            }
        }

        public void Interact()
        {
            if (HasGlass || Occupant == null || !Occupant.IsWaitingForOrder) return;

            PlayerHands hands = PlayerHands.Instance;
            if (hands == null || hands.IsEmpty) return;

            if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return;
            if (!Occupant.CanAcceptOrder(glass)) return;

            ServeToOccupant(hands, glass);
        }

        /// <summary>Bardagi masaya birakir ve musteriye teslim eder.</summary>
        private void ServeToOccupant(PlayerHands hands, TeaGlass glass)
        {
            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            // Fizik kapali kalir: bardak masanin uzerinde birakildigi yerde durur, yuvarlanmaz.
            // Collider'i yeniden aciyoruz ki oyuncu kirlendikten sonra alabilsin.
            released.Detach(GlassPosition, GlassRotation, restorePhysics: false);
            released.SetPickupEnabled(true);
            placedGlassItem = released;

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
