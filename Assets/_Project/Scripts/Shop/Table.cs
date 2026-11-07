using System.Collections.Generic;
using KanBagi.Customers;
using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Player;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace KanBagi.Shop
{
    /// <summary>
    /// Bir masa: DORT sandalye (D7a), her sandalyenin onunde bir bardak yeri ve doluluk durumu.
    /// Bir masaya tek bir musteri veya grup oturur (yabancilar masa paylasmaz); gruplar D7b'de gelecek.
    /// Oyuncu elindeki dolu bardagi buraya birakarak o icecegi bekleyen kisiye servis eder; tepsiyle tek basista
    /// butun kirli bardaklar toplanir ve bekleyen herkese uygun icecek verilir.
    /// Masa kimin nerede oturdugunu ve hangi yerde bardak oldugunu bilir; musteri davranisina veya paraya karismaz (SOLID).
    /// </summary>
    public class Table : MonoBehaviour, IInteractable, IFurnitureMoveCheck
    {
        [Header("Sandalyeler")]
        [Tooltip("Masanin merkezine gore oturma noktalari (yerel yon, olcekten etkilenmez). Sayisi = sandalye sayisi.")]
        [SerializeField] private Vector3[] seatOffsets =
        {
            new Vector3(0f, 0f, -1.2f), new Vector3(1.2f, 0f, 0f), new Vector3(0f, 0f, 1.2f), new Vector3(-1.2f, 0f, 0f)
        };

        [Tooltip("Her sandalyenin onundeki bardak yeri (ayni sira, yerel yon).")]
        [SerializeField] private Vector3[] glassOffsets =
        {
            new Vector3(0f, 0.5f, -0.35f), new Vector3(0.35f, 0.5f, 0f), new Vector3(0f, 0.5f, 0.35f), new Vector3(-0.35f, 0.5f, 0f)
        };

        private Customer[] occupants;
        private HoldableItem[] placedGlasses;

        public int SeatCount => seatOffsets.Length;

        /// <summary>Masa bos mu? (Kimse oturmuyor/yuruyor. Ustunde kirli bardak olabilir.) Masa paylasilmaz.</summary>
        public bool IsFree
        {
            get
            {
                EnsureArrays();
                foreach (Customer occupant in occupants)
                {
                    if (occupant != null) return false;
                }

                return true;
            }
        }

        private TableGame game;

        /// <summary>Masadaki oyun takimi (D8a). Musteri sabir ve ikinci tur etkisini buradan sorar. Yoksa null.</summary>
        public TableGame Game => game;

        /// <summary>Masadaki grubun istedigi (henuz getirilmemis) oyun (D8b). Balon bunu gosterir. Yoksa null.</summary>
        public TableGameData RequestedGame { get; private set; }

        /// <summary>Oyun istegi baslayinca/karsilaninca/grup kalkinca.</summary>
        public event System.Action OnGameRequestChanged;

        /// <summary>Grubun oyun istegini masaya yazar (null: istek yok). Sadece CustomerGroup cagirir.</summary>
        public void SetGameRequest(TableGameData requested)
        {
            if (RequestedGame == requested) return;

            RequestedGame = requested;
            OnGameRequestChanged?.Invoke();
        }

        private void Awake()
        {
            EnsureArrays();
            game = GetComponent<TableGame>();
        }

        private void EnsureArrays()
        {
            if (occupants != null && occupants.Length == SeatCount) return;

            occupants = new Customer[SeatCount];
            placedGlasses = new HoldableItem[SeatCount];
        }

        /// <summary>Bu yerde (oyuncunun elinde olmayan) bardak var mi?</summary>
        private bool HasGlass(int seat) => placedGlasses[seat] != null && !placedGlasses[seat].IsHeld;

        private bool HasAnyGlass()
        {
            for (int i = 0; i < SeatCount; i++)
            {
                if (HasGlass(i)) return true;
            }

            return false;
        }

        /// <summary>Dolu masa tasinmaz: musteri oturuyor/yuruyorsa veya ustunde bardak varsa.</summary>
        public bool CanMove(out string reason)
        {
            EnsureArrays();
            reason = !IsFree || HasAnyGlass() ? "Masa dolu, tasinamaz" : null;
            return reason == null;
        }

        public Vector3 GetSeatPosition(int seat) => transform.position + transform.rotation * seatOffsets[seat];

        /// <summary>Butun oturma noktalari (yerlestirmede hepsinin bos olmasi gerekir).</summary>
        public IEnumerable<Vector3> SeatPositions
        {
            get
            {
                for (int i = 0; i < SeatCount; i++) yield return GetSeatPosition(i);
            }
        }

        private Vector3 GetGlassPosition(int seat) =>
            transform.position + transform.rotation * glassOffsets[Mathf.Min(seat, glassOffsets.Length - 1)];

        // ---- Oyuncu ----

        public string GetPrompt(PlayerContext player)
        {
            EnsureArrays();
            if (player.Hands != null && TryGetHeldTray(player.Hands, out Tray tray))
                return GetTrayPrompt(tray);

            string gamePrompt = GetGameSetPrompt(player.Hands);
            if (gamePrompt != null) return gamePrompt;

            // Bos elle, bos masadaki takim geri alinabilir.
            if (game != null && game.HasGame && IsFree && player.Hands != null && player.Hands.IsEmpty)
                return $"{game.CurrentGame.GameName} al";

            if (IsAnyoneWaiting())
            {
                PlayerHands hands = player.Hands;

                if (hands == null || hands.IsEmpty) return "Siparisi getir";
                if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return "Bu masaya konmaz";

                int seat = FindSeatFor(glass, requireEmptySpot: false);
                if (seat < 0) return "Musteri bunu istemiyor";

                return HasGlass(seat) ? "Once masadaki bardagi kaldir" : "Siparisi Ver";
            }

            if (!IsFree) return "Musteri oturuyor";

            return HasAnyGlass() ? "Masada bardak var" : "Bos masa";
        }

        public void Interact(PlayerContext player)
        {
            EnsureArrays();
            PlayerHands hands = player.Hands;
            if (hands == null) return;

            if (hands.IsEmpty)
            {
                TakeGameSet(hands);
                return;
            }

            if (TryPlaceGameSet(hands)) return;

            if (TryGetHeldTray(hands, out Tray tray))
            {
                ServeWithTray(tray);
                return;
            }

            if (!hands.HeldItem.TryGetComponent(out TeaGlass glass)) return;

            int seat = FindSeatFor(glass, requireEmptySpot: true);
            if (seat < 0) return;

            HoldableItem released = hands.ReleaseHeldItem();
            if (released == null) return;

            // Fizik kapali kalir: bardak masanin uzerinde birakildigi yerde durur, yuvarlanmaz.
            released.Detach(GetGlassPosition(seat), Quaternion.identity, restorePhysics: false);
            PlaceAndServe(seat, released, glass);
        }

        private bool IsAnyoneWaiting()
        {
            foreach (Customer occupant in occupants)
            {
                if (occupant != null && occupant.IsWaitingForOrder) return true;
            }

            return false;
        }

        /// <summary>
        /// Bu bardagi bekleyen ilk kisinin yeri. requireEmptySpot: onundeki yer bos olmali (servis icin);
        /// degilse yer dolu olsa da doner (ipucu "once bardagi kaldir" diyebilsin diye).
        /// </summary>
        private int FindSeatFor(TeaGlass glass, bool requireEmptySpot)
        {
            int fallback = -1;
            for (int i = 0; i < SeatCount; i++)
            {
                Customer occupant = occupants[i];
                if (occupant == null || !occupant.IsWaitingForOrder || !occupant.CanAcceptOrder(glass)) continue;
                if (!HasGlass(i)) return i;
                if (fallback < 0) fallback = i;
            }

            return requireEmptySpot ? -1 : fallback;
        }

        // ---- Oyun takimi (D8a) ----

        /// <summary>Elde oyun takimi varsa onun ipucu; yoksa null.</summary>
        private string GetGameSetPrompt(PlayerHands hands)
        {
            if (game == null || hands == null || hands.IsEmpty || !hands.HeldItem.TryGetComponent(out GameSetItem set)) return null;
            if (set.Game == null) return "Bu masaya konmaz";
            if (game.CurrentGame == set.Game) return $"Masada zaten {set.Game.GameName} var";

            return game.HasGame ? "Once masadaki takimi al" : $"{set.Game.GameName} koy";
        }

        /// <summary>Eldeki takimi masaya koyar (kutu yok olur, masadaki parca gorunur). Musteri otururken de konabilir.</summary>
        private bool TryPlaceGameSet(PlayerHands hands)
        {
            if (game == null || game.HasGame || !hands.HeldItem.TryGetComponent(out GameSetItem set) || set.Game == null) return false;

            HoldableItem box = hands.ReleaseHeldItem();
            if (box == null) return false;

            game.SetGame(set.Game);
            Destroy(box.gameObject);
            return true;
        }

        /// <summary>Bos masadan takimi alip ele verir (masada kimse yokken).</summary>
        private void TakeGameSet(PlayerHands hands)
        {
            if (game == null || !game.HasGame || !IsFree || game.CurrentGame.BoxPrefab == null) return;

            GameObject box = Instantiate(game.CurrentGame.BoxPrefab, transform.position + Vector3.up, Quaternion.identity);
            box.name = game.CurrentGame.BoxPrefab.name;
            if (!box.TryGetComponent(out HoldableItem item) || !hands.TryHold(item))
            {
                Destroy(box);
                return;
            }

            game.SetGame(null);
        }

        // ---- Tepsi ----

        private static bool TryGetHeldTray(PlayerHands hands, out Tray tray)
        {
            tray = null;
            return hands.HeldItem != null && hands.HeldItem.TryGetComponent(out tray);
        }

        /// <summary>
        /// Bu yerdeki bardak toplanabilir mi? Musteri icerken bardak doludur ve alinamaz;
        /// icilmis (kirli) veya bos birakilmis bardak toplanir.
        /// </summary>
        private bool TryGetCollectableGlass(int seat, out TeaGlass glass)
        {
            glass = null;
            return HasGlass(seat)
                   && placedGlasses[seat].TryGetComponent(out glass)
                   && glass.State != GlassState.Full;
        }

        private string GetTrayPrompt(Tray tray)
        {
            bool anyCollectable = false;
            bool canServe = false;
            string missingOrder = null;

            for (int i = 0; i < SeatCount; i++)
            {
                bool collectable = TryGetCollectableGlass(i, out _);
                anyCollectable |= collectable;

                Customer occupant = occupants[i];
                if (occupant == null || !occupant.IsWaitingForOrder) continue;

                // Kirli bardak kalkmadan servis olmaz: toplanamiyorsa (tepsi dolu) servis de yok.
                bool spotClear = !HasGlass(i) || (collectable && !tray.IsFull);
                if (tray.Find(occupant.CanAcceptOrder) != null && spotClear) canServe = true;
                else if (missingOrder == null && occupant.Order != null) missingOrder = occupant.Order.ItemName;
            }

            bool canCollect = anyCollectable && !tray.IsFull;
            if (canCollect && canServe) return "Kirliyi Topla + Servis Et";
            if (canServe) return "Servis Et";
            if (canCollect) return "Kirli Bardaklari Tepsiye Al";

            if (anyCollectable) return "Tepsi dolu";
            if (missingOrder != null) return $"Tepside {missingOrder} yok";

            return IsFree ? "Bos masa" : "Musteri oturuyor";
        }

        /// <summary>Tek basista: once butun kirli bardaklar tepsiye, sonra bekleyen herkese istedigi bardak.</summary>
        private void ServeWithTray(Tray tray)
        {
            for (int i = 0; i < SeatCount; i++)
            {
                if (TryGetCollectableGlass(i, out TeaGlass dirty) && tray.TryAdd(dirty))
                    placedGlasses[i] = null;
            }

            for (int i = 0; i < SeatCount; i++)
            {
                Customer occupant = occupants[i];
                if (occupant == null || !occupant.IsWaitingForOrder || HasGlass(i)) continue;

                TeaGlass order = tray.Find(occupant.CanAcceptOrder);
                if (order == null) continue;

                // Bardak masaya inince alinabilir olur: icilip kirlenince tekrar toplanacak.
                if (!tray.TryTakeOut(order, GetGlassPosition(i), Quaternion.identity, pickupEnabled: true)) continue;

                PlaceAndServe(i, order.GetComponent<HoldableItem>(), order);
            }
        }

        /// <summary>Masaya inmis bardagi o sandalyenin yerine kaydeder ve oradaki musteriye teslim eder.</summary>
        private void PlaceAndServe(int seat, HoldableItem placed, TeaGlass glass)
        {
            // Collider'i yeniden aciyoruz ki oyuncu kirlendikten sonra alabilsin.
            placed.SetPickupEnabled(true);
            placedGlasses[seat] = placed;

            if (!occupants[seat].TryServe(glass))
                Debug.LogWarning($"[Table] '{name}' uzerindeki musteri siparisi kabul etmedi.", this);
        }

        // ---- Musteri ----

        /// <summary>
        /// Masa bossa musteriye bir sandalye ayirir (NavMesh'te ulasilabilir ilk yer). Masa paylasilmaz.
        /// D7b'de gruplar ayni masada birden fazla sandalye ayiracak.
        /// </summary>
        public bool TryReserve(Customer customer, out int seat)
        {
            seat = -1;
            if (customer == null || !IsFree) return false;

            for (int i = 0; i < SeatCount; i++)
            {
                if (!IsSeatUsable(i)) continue;

                occupants[i] = customer;
                seat = i;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sandalye kullanilabilir mi? Yerinde NavMesh olmali, masa ile sandalye arasinda duvar olmamali ve sandalyenin
        /// yeri bos olmali. (Duvara yakin masada sandalye duvarin ARKASINA - bahce/sokak NavMesh'ine - dusebiliyordu;
        /// musteri oraya varamayip duvara yapisik oturuyordu.)
        /// </summary>
        private bool IsSeatUsable(int seat)
        {
            Vector3 seatPosition = GetSeatPosition(seat);
            if (!NavMesh.SamplePosition(seatPosition, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas)) return false;

            Vector3 from = transform.position + Vector3.up * 0.3f;
            Vector3 to = new Vector3(seatPosition.x, from.y, seatPosition.z);
            foreach (RaycastHit hit in Physics.RaycastAll(from, to - from, Vector3.Distance(from, to), ~0, QueryTriggerInteraction.Ignore))
            {
                if (!IsIgnorable(hit.collider)) return false;
            }

            Vector3 bottom = navHit.position + Vector3.up * 0.4f;
            Vector3 top = navHit.position + Vector3.up * 1.4f;
            foreach (Collider other in Physics.OverlapCapsule(bottom, top, 0.25f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!IsIgnorable(other)) return false;
            }

            return true;
        }

        /// <summary>Sandalye kontrolunde sayilmayanlar: masanin kendisi (ve ustundekiler), zemin, insanlar, tasinan esyalar.</summary>
        private bool IsIgnorable(Collider other) =>
            other.transform.IsChildOf(transform)
            || other.GetComponentInParent<NavMeshSurface>() != null
            || other.GetComponentInParent<ShopFloor>() != null
            || other is CharacterController
            || other.GetComponentInParent<NavMeshAgent>() != null
            || other.GetComponentInParent<HoldableItem>() != null;

        /// <summary>Bos masada kullanilabilir sandalye sayisi (masa doluysa 0). Grup buna sigmali (D7b).</summary>
        public int UsableSeatCount
        {
            get
            {
                if (!IsFree) return 0;

                int count = 0;
                for (int i = 0; i < SeatCount; i++)
                {
                    if (IsSeatUsable(i)) count++;
                }

                return count;
            }
        }

        /// <summary>Bos masada gruptaki her kisiye bir kullanilabilir sandalye ayirir. Yetmezse hic ayirmaz.</summary>
        public bool TryReserveGroup(IReadOnlyList<Customer> members, out int[] seats)
        {
            seats = null;
            if (members == null || members.Count == 0 || !IsFree) return false;

            var usable = new List<int>();
            for (int i = 0; i < SeatCount && usable.Count < members.Count; i++)
            {
                if (IsSeatUsable(i)) usable.Add(i);
            }

            if (usable.Count < members.Count) return false;

            seats = new int[members.Count];
            for (int m = 0; m < members.Count; m++)
            {
                occupants[usable[m]] = members[m];
                seats[m] = usable[m];
            }

            return true;
        }

        /// <summary>Musteri kalkinca sandalyesini serbest birakir.</summary>
        public void Release(Customer customer)
        {
            EnsureArrays();
            for (int i = 0; i < SeatCount; i++)
            {
                if (occupants[i] == customer) occupants[i] = null;
            }
        }

        private void OnEnable() => TableManager.Instance?.Register(this);

        private void OnDisable() => TableManager.Instance?.Unregister(this);

        /// <summary>Sahne penceresinde oturma ve bardak noktalarini gorsellestirir.</summary>
        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < seatOffsets.Length; i++)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(GetSeatPosition(i), 0.25f);

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(GetGlassPosition(i), 0.1f);
            }
        }
    }
}
