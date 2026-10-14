using System;
using KanBagi.Core;
using KanBagi.Economy;
using KanBagi.Interaction;
using System.Collections.Generic;
using KanBagi.Player;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace KanBagi.Illegal
{
    /// <summary>Bir kumar elinin sonucu. Hem tabela hem gunluk ozet bunu okur.</summary>
    public readonly struct GamblingRoundResult
    {
        public readonly bool Won;
        public readonly int Stake;
        public readonly int Payout;

        public GamblingRoundResult(bool won, int stake, int payout)
        {
            Won = won;
            Stake = stake;
            Payout = payout;
        }

        public int Net => Payout - Stake;
    }

    /// <summary>
    /// Arka odadaki gizli kumar masasi. SADECE dukkan KAPALIYKEN (gece) oynanir.
    /// Kendi kurallarini bilir; polisi, mafyayi veya raporu bilmez (SOLID).
    /// Her el suphe biriktirir - 8c'de polis bunu okuyacak.
    /// </summary>
    public class GamblingTable : MonoBehaviour, IInteractable
    {
        [Header("Bahis")]
        [SerializeField] private int betAmount = 100;
        [Tooltip("Kazanma olasiligi (0-1). 0.45 = %45.")]
        [SerializeField] [Range(0f, 1f)] private float winChance = 0.45f;
        [Tooltip("Kazaninca bahsin kac katini geri alir. 2 = bahis + ayni kadar kar.")]
        [SerializeField] private float payoutMultiplier = 2f;

        [Header("Supheli Is")]
        [Tooltip("Her el sonunda suphe bu kadar artar.")]
        [SerializeField] private int suspicionPerRound = 3;

        [Header("Kumarhane (NPC Kumarbazlar)")]
        [Tooltip("Kumarbazlarin oynadigi her elde kasaya giren masa parasi (TL).")]
        [SerializeField] private int houseRakePerRound = 25;
        [Tooltip("Kumarbazlarin oynadigi her elde suphe bu kadar artar.")]
        [SerializeField] private int suspicionPerHouseRound = 1;

        [Header("Gizleme")]
        [Tooltip("Masa ortuluyken acilan obje (ortu/brandu). Polis gelince bunu kapatmis olmalisin.")]
        [SerializeField] private GameObject coverVisual;
        [Tooltip("Masa ortuluyken gizlenecek objeler (tabela, cuha vb.).")]
        [SerializeField] private GameObject[] exposedVisuals;

        [Header("Ipucu Metinleri")]
        [Tooltip("{0} bahis tutari.")]
        [SerializeField] private string playFormat = "Kumar Oyna ({0} TL)";
        [Tooltip("{0} bahis tutari.")]
        [SerializeField] private string noMoneyFormat = "{0} TL gerekli";
        [SerializeField] private string hideMessage = "Masayi Gizle";
        [SerializeField] private string revealMessage = "Ortuyu Ac";

        [Header("Sandalyeler (Kumarbaz NPC)")]
        [Tooltip("En fazla kac kumarbaz otursun.")]
        [SerializeField] private int maxSeats = 2;
        [Tooltip("Sandalyenin masa kenarina uzakligi (metre). NavMesh ajan yaricapindan (0.5) buyuk olmali.")]
        [SerializeField] private float seatClearance = 0.6f;
        [Tooltip("Iki sandalye arasindaki en kucuk mesafe (metre).")]
        [SerializeField] private float seatSpacing = 1f;

        // Sandalyeler elle verilmez: masanin collider'indan, NavMesh'te GERCEKTEN yurunebilir
        // noktalar secilerek hesaplanir. Masa tasininca sandalyeler de kendiliginden dogru yere gelir.
        private Vector3[] seatPositions;
        private Gambler[] seatOccupants;

        public int BetAmount => betAmount;

        /// <summary>Kumarbazlar icin bos sandalye var mi?</summary>
        public bool HasFreeSeat => FindFreeSeat() >= 0;

        /// <summary>Bu gecenin toplam kar/zarari. Her gece basinda sifirlanir.</summary>
        public int NightNet { get; private set; }

        /// <summary>Bir el oynandiginda tetiklenir. Tabela ve gunluk ozet abone olur.</summary>
        public event Action<GamblingRoundResult> OnRoundPlayed;

        /// <summary>Kumarbazlar bir el oynayinca kasaya giren masa parasi ile tetiklenir. Tabela ve gunluk ozet abone olur.</summary>
        public event Action<int> OnHouseRound;

        /// <summary>Kumar su anda oynanabilir mi? (Gece olmali.)</summary>
        public bool IsOpen => DayCycleManager.Instance == null || !DayCycleManager.Instance.IsShopOpen;

        /// <summary>
        /// Masa ortulu mu? Polis baskininda ortusuz yakalanirsan bedeli agir olur.
        /// Guvenli tarafta kalmak icin ORTULU basliyor.
        /// </summary>
        public bool IsHidden { get; private set; } = true;

        /// <summary>Ortu durumu degisince tetiklenir.</summary>
        public event Action<bool> OnHiddenChanged;

        private void Start()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed += HandleShopClosed;

            RefreshVisuals();
        }

        /// <summary>Bos bir sandalyeyi kumarbaza ayirir. Bos yoksa false doner.</summary>
        public bool TryReserveSeat(Gambler gambler, out int seatIndex)
        {
            seatIndex = FindFreeSeat();
            if (seatIndex < 0 || gambler == null) return false;

            seatOccupants[seatIndex] = gambler;
            return true;
        }

        /// <summary>Kumarbaz kalkinca sandalyeyi bosaltir (sadece gercek sahibi bosaltabilir).</summary>
        public void ReleaseSeat(int seatIndex, Gambler gambler)
        {
            if (seatOccupants == null || seatIndex < 0 || seatIndex >= seatOccupants.Length) return;
            if (seatOccupants[seatIndex] == gambler) seatOccupants[seatIndex] = null;
        }

        public Vector3 GetSeatPosition(int seatIndex) => seatPositions[seatIndex];

        private int FindFreeSeat()
        {
            // Ilk ihtiyacta hesaplanir: masa o ana kadar NavMesh'te kendi deligini acmis olur.
            if (seatPositions == null)
            {
                seatPositions = CalculateSeats();
                seatOccupants = new Gambler[seatPositions.Length];
            }

            for (int i = 0; i < seatOccupants.Length; i++)
            {
                // Yok edilmis kumarbaz (Unity null'u) da bos sayilir.
                if (seatOccupants[i] == null) return i;
            }

            return -1;
        }

        /// <summary>
        /// Masanin dort kenarinda aday noktalar uretir; NavMesh'te yurunebilir olanlardan
        /// birbirine yeterince uzak en fazla maxSeats tanesini sandalye yapar.
        /// </summary>
        private Vector3[] CalculateSeats()
        {
            var seats = new List<Vector3>();

            foreach (Vector3 candidate in GetSeatCandidates())
            {
                if (seats.Count >= maxSeats) break;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 0.25f, NavMesh.AllAreas)) continue;
                // %80 pay: NavMesh kenara yakin noktayi birkac santim kaydirabilir.
                if (seats.Exists(seat => Vector3.Distance(seat, hit.position) < seatSpacing * 0.8f)) continue;

                seats.Add(hit.position);
            }

            if (seats.Count == 0)
                Debug.LogWarning($"[GamblingTable] '{name}' etrafinda yurunebilir sandalye yeri yok. " +
                                 "Masa duvara/esyalara cok yakin olabilir; kumarbaz gelmeyecek.", this);

            return seats.ToArray();
        }

        /// <summary>Her kenarda, kenar boyunca iki nokta (ortadan seatSpacing/2 saga ve sola).</summary>
        private IEnumerable<Vector3> GetSeatCandidates()
        {
            Bounds bounds = TryGetComponent(out Collider tableCollider)
                ? tableCollider.bounds
                : new Bounds(transform.position, Vector3.one);

            float y = bounds.min.y;
            float half = seatSpacing * 0.5f;
            float south = bounds.min.z - seatClearance;
            float north = bounds.max.z + seatClearance;
            float west = bounds.min.x - seatClearance;
            float east = bounds.max.x + seatClearance;
            Vector3 c = bounds.center;

            yield return new Vector3(c.x - half, y, south);
            yield return new Vector3(c.x + half, y, south);
            yield return new Vector3(c.x - half, y, north);
            yield return new Vector3(c.x + half, y, north);
            yield return new Vector3(west, y, c.z - half);
            yield return new Vector3(west, y, c.z + half);
            yield return new Vector3(east, y, c.z - half);
            yield return new Vector3(east, y, c.z + half);
        }

        /// <summary>Play'de hesaplanan sandalyeleri, editorde aday noktalari mor kure olarak gosterir.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;

            if (seatPositions != null)
            {
                foreach (Vector3 seat in seatPositions)
                    Gizmos.DrawSphere(seat, 0.2f);
                return;
            }

            foreach (Vector3 candidate in GetSeatCandidates())
                Gizmos.DrawWireSphere(candidate, 0.2f);
        }

        /// <summary>Ortuyu acar/kapatir. Polis gelmeden once kapatmak oyuncunun isi.</summary>
        public void SetHidden(bool hidden)
        {
            if (IsHidden == hidden) return;

            IsHidden = hidden;
            RefreshVisuals();
            OnHiddenChanged?.Invoke(IsHidden);
        }

        private void RefreshVisuals()
        {
            if (coverVisual != null) coverVisual.SetActive(IsHidden);

            if (exposedVisuals == null) return;

            foreach (GameObject exposed in exposedVisuals)
            {
                if (exposed != null) exposed.SetActive(!IsHidden);
            }
        }

        public string GetPrompt(PlayerContext player)
        {
            // Gunduz kumar oynanmaz; o yuzden E tusu gunduz ORTUYU yonetir.
            if (!IsOpen) return IsHidden ? revealMessage : hideMessage;

            if (IsHidden) return revealMessage;

            int money = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney : 0;
            if (money < betAmount) return string.Format(noMoneyFormat, betAmount);

            return string.Format(playFormat, betAmount);
        }

        public void Interact(PlayerContext player)
        {
            // Gunduz: sadece ortuyu ac/kapat. Gece: ortu kapaliysa once ac, sonra oyna.
            if (!IsOpen || IsHidden)
            {
                SetHidden(!IsHidden);
                return;
            }

            if (EconomyManager.Instance == null) return;
            if (!EconomyManager.Instance.TrySpendMoney(betAmount)) return;

            PlayRound();
        }

        /// <summary>
        /// Kumarbazlar bir el oynadi: masa parasi kasaya girer, suphe artar.
        /// Kumarhane parasinin TEK gecis noktasi burasi (ileride co-op'ta RPC siniri).
        /// </summary>
        public void PlayHouseRound()
        {
            // Ortu kapandiysa veya sabah olduysa el sayilmaz (kumarbaz kalkmak uzere).
            if (!IsOpen || IsHidden) return;

            if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(houseRakePerRound);
            NightNet += houseRakePerRound;

            if (SuspicionManager.Instance != null) SuspicionManager.Instance.Add(suspicionPerHouseRound);

            OnHouseRound?.Invoke(houseRakePerRound);
        }

        private void PlayRound()
        {
            bool won = Random.value < winChance;
            int payout = won ? Mathf.RoundToInt(betAmount * payoutMultiplier) : 0;

            if (payout > 0) EconomyManager.Instance.AddMoney(payout);

            var result = new GamblingRoundResult(won, betAmount, payout);
            NightNet += result.Net;

            // Her el mahalledeki supheyi biraz daha artirir.
            if (SuspicionManager.Instance != null) SuspicionManager.Instance.Add(suspicionPerRound);

            OnRoundPlayed?.Invoke(result);
        }

        /// <summary>Dukkan kapandi: yeni gece basliyor, gecelik hesap sifirlanir.</summary>
        private void HandleShopClosed(int day) => NightNet = 0;

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed -= HandleShopClosed;
        }
    }
}
