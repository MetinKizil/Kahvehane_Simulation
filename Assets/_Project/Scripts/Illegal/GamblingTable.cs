using System;
using System.Collections;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Economy;
using KanBagi.Furniture;
using KanBagi.Interaction;
using KanBagi.Player;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace KanBagi.Illegal
{
    /// <summary>Bir rulet turunun oyuncu icin sonucu. Hem tabela hem gunluk ozet bunu okur.</summary>
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

    public enum RoulettePhase
    {
        Idle,      // Kimse yok veya gunduz
        Betting,   // Bahisler acik (geri sayim)
        Spinning,  // Cark donuyor, bahis kapali
        Result     // Sonuc gosteriliyor
    }

    /// <summary>
    /// Bodrumdaki (Kumar Odasi) RULET masasi (D6d). SADECE dukkan KAPALIYKEN (gece) oynanir.
    /// Tur: masada oturan kumarbaz veya oyuncu bahsi varsa bahis suresi (geri sayim) -> cark doner -> sonuc.
    /// Oyuncu masadaki alanlara (RouletteBetSpot) bakip E ile 100 TL'lik fis koyar: sayi 35 kat, renk 2 kat (0 gelirse renkler kaybeder).
    /// Oturan her kumarbaz her turda kasaya pay birakir ve suphe biriktirir.
    /// Polisten saklanma masada degil, bodrumun kapaginda (HiddenHatch). Kendi kurallarini bilir; polisi/raporu bilmez (SOLID).
    /// </summary>
    public class GamblingTable : MonoBehaviour, IInteractable, IFurnitureMoveCheck, IPlacementPausable
    {
        [Header("Bahis")]
        [SerializeField] private int betAmount = 100;
        [Tooltip("Sayiya bahis kazaninca fisin kac katini geri alir (36 = bahis + 35 kat kar).")]
        [SerializeField] private int straightMultiplier = 36;
        [Tooltip("Renge bahis kazaninca fisin kac katini geri alir (2 = bahis + ayni kadar kar).")]
        [SerializeField] private int colorMultiplier = 2;
        [Tooltip("Oyuncunun bahis koydugu her turda suphe bu kadar artar.")]
        [SerializeField] private int suspicionPerRound = 3;

        [Header("Tur")]
        [SerializeField] private int bettingSeconds = 8;
        [SerializeField] private float spinSeconds = 5f;
        [SerializeField] private float resultSeconds = 3f;

        [Header("Kumarhane (NPC Kumarbazlar)")]
        [Tooltip("Oturan her kumarbazin her turda kasaya biraktigi pay (TL).")]
        [SerializeField] private int houseRakePerGambler = 80;
        [Tooltip("Oturan her kumarbaz icin her turda suphe bu kadar artar.")]
        [SerializeField] private int suspicionPerGambler = 3;

        [Header("Baglantilar")]
        [Tooltip("Carkin gorunumu. Bos birakilirsa cocuklarda aranir.")]
        [SerializeField] private RouletteWheel wheel;

        [Header("Ipucu Metinleri")]
        [Tooltip("{0} sayi, {1} fis tutari.")]
        [SerializeField] private string numberBetFormat = "{0} sayisina {1} TL koy (35 kat)";
        [Tooltip("{0} fis tutari.")]
        [SerializeField] private string redBetFormat = "Kirmiziya {0} TL koy (2 kat)";
        [Tooltip("{0} fis tutari.")]
        [SerializeField] private string blackBetFormat = "Siyaha {0} TL koy (2 kat)";
        [Tooltip("Alanda zaten bahis varsa sona eklenir. {0} alandaki toplam.")]
        [SerializeField] private string stakeSuffixFormat = "  [masada {0} TL]";
        [SerializeField] private string spinningMessage = "Cark donuyor";
        [SerializeField] private string tableHint = "Bahis icin bir sayiya veya renge bak";
        [Tooltip("{0} fis tutari.")]
        [SerializeField] private string noMoneyFormat = "{0} TL gerekli";
        [Tooltip("Gunduz masaya bakinca.")]
        [SerializeField] private string closedMessage = "Gunduz kumar oynanmaz";
        [Tooltip("Masada kumarbaz/bahis varken F ile kaldirmaya calisinca.")]
        [SerializeField] private string moveBlockedMessage = "Masada oyun var, tasinamaz";

        [Header("Sandalyeler (Kumarbaz NPC)")]
        [Tooltip("En fazla kac kumarbaz otursun.")]
        [SerializeField] private int maxSeats = 8;
        [Tooltip("Sandalyenin masa kenarina uzakligi (metre). NavMesh ajan yaricapindan (0.5) buyuk olmali.")]
        [SerializeField] private float seatClearance = 0.6f;
        [Tooltip("Iki sandalye arasindaki en kucuk mesafe (metre).")]
        [SerializeField] private float seatSpacing = 0.75f;
        [Tooltip("Masanin +X ucunda carkin/krupiyenin kapladigi uzunluk: buraya sandalye konmaz.")]
        [SerializeField] private float wheelEndLength = 0.6f;

        // Sandalyeler elle verilmez: masanin collider'indan, NavMesh'te GERCEKTEN yurunebilir
        // noktalar secilerek hesaplanir.
        private Vector3[] seatPositions;
        private Gambler[] seatOccupants;
        private bool[] seated;

        private readonly Dictionary<RouletteBetSpot, int> stakes = new Dictionary<RouletteBetSpot, int>();
        private readonly Dictionary<RouletteBetSpot, int> gamblerChips = new Dictionary<RouletteBetSpot, int>();
        private RouletteBetSpot[] spots;

        public int BetAmount => betAmount;

        public RoulettePhase Phase { get; private set; } = RoulettePhase.Idle;

        /// <summary>Kumarbazlar icin bos sandalye var mi?</summary>
        public bool HasFreeSeat => FindFreeSeat() >= 0;

        /// <summary>Bu gecenin toplam kar/zarari (oyuncu + kasa payi). Her gece basinda sifirlanir.</summary>
        public int NightNet { get; private set; }

        /// <summary>Oyuncunun bahisli turu bitince tetiklenir. Tabela ve gunluk ozet abone olur.</summary>
        public event Action<GamblingRoundResult> OnRoundPlayed;

        /// <summary>Turda kumarbazlardan kasaya giren toplam pay ile tetiklenir. Tabela ve gunluk ozet abone olur.</summary>
        public event Action<int> OnHouseRound;

        /// <summary>Tur asamasi degisince (ve bahis suresinde her saniye) tetiklenir. Ikinci deger: kalan saniye.</summary>
        public event Action<RoulettePhase, int> OnPhaseChanged;

        /// <summary>Top bir sayiya dustugunde tetiklenir.</summary>
        public event Action<int> OnResult;

        /// <summary>Tur tamamen bitince tetiklenir. Kumarbazlar oynadiklari turu buradan sayar.</summary>
        public event Action OnRoundFinished;

        /// <summary>Herhangi bir rulet masasinda OnRoundPlayed. Masalar oyun sirasinda alinip konuldugu icin gunluk ozet buna abone (D6d-2).</summary>
        public static event Action<GamblingRoundResult> AnyRoundPlayed;

        /// <summary>Herhangi bir rulet masasinda OnHouseRound.</summary>
        public static event Action<int> AnyHouseRound;

        /// <summary>Kumar su anda oynanabilir mi? (Gece olmali.)</summary>
        public bool IsOpen => DayCycleManager.Instance == null || !DayCycleManager.Instance.IsShopOpen;

        private int SeatedCount
        {
            get
            {
                if (seated == null) return 0;

                int count = 0;
                for (int i = 0; i < seated.Length; i++)
                {
                    if (seated[i] && seatOccupants[i] != null) count++;
                }

                return count;
            }
        }

        private int TotalStake
        {
            get
            {
                int total = 0;
                foreach (int amount in stakes.Values) total += amount;
                return total;
            }
        }

        private void Awake()
        {
            if (wheel == null) wheel = GetComponentInChildren<RouletteWheel>(true);
            spots = GetComponentsInChildren<RouletteBetSpot>(true);
        }

        private void Start()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed += HandleShopClosed;
            if (FurnitureManager.Instance != null)
                FurnitureManager.Instance.OnLayoutChanged += HandleLayoutChanged;
        }

        /// <summary>Masada kumarbaz (yolda olan dahil), bahis veya donen tur varsa kalkmaz.</summary>
        public bool CanMove(out string reason)
        {
            bool busy = stakes.Count > 0 || Phase == RoulettePhase.Spinning || Phase == RoulettePhase.Result;
            if (!busy && seatOccupants != null)
            {
                foreach (Gambler occupant in seatOccupants)
                {
                    if (occupant != null) busy = true;
                }
            }

            reason = busy ? moveBlockedMessage : null;
            return !busy;
        }

        /// <summary>Masa tasindiysa (veya etrafina esya kondu) sandalyeler bir sonraki ihtiyacta yeniden hesaplanir.</summary>
        private void HandleLayoutChanged()
        {
            if (seatOccupants != null)
            {
                foreach (Gambler occupant in seatOccupants)
                {
                    if (occupant != null) return; // dolu masada sandalyeler kaymasin
                }
            }

            seatPositions = null;
        }

        // Masa bodrumla birlikte acilir/kapanir; tur dongusu acik oldugu surece calisir.
        private void OnEnable() => StartCoroutine(RoundLoop());

        // ---- Oyuncu ----

        public string GetPrompt(PlayerContext player)
        {
            if (!IsOpen) return closedMessage;
            return Phase == RoulettePhase.Spinning || Phase == RoulettePhase.Result ? spinningMessage : tableHint;
        }

        public void Interact(PlayerContext player) { }

        public string GetBetPrompt(RouletteBetSpot spot)
        {
            if (!IsOpen) return closedMessage;
            if (Phase == RoulettePhase.Spinning || Phase == RoulettePhase.Result) return spinningMessage;

            int money = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney : 0;
            if (money < betAmount) return string.Format(noMoneyFormat, betAmount);

            string prompt;
            switch (spot.Kind)
            {
                case RouletteBetKind.Number: prompt = string.Format(numberBetFormat, spot.Number, betAmount); break;
                case RouletteBetKind.Red: prompt = string.Format(redBetFormat, betAmount); break;
                default: prompt = string.Format(blackBetFormat, betAmount); break;
            }

            return stakes.TryGetValue(spot, out int stake) ? prompt + string.Format(stakeSuffixFormat, stake) : prompt;
        }

        /// <summary>
        /// Oyuncu bir alana fis koyar. Bahisler sadece gece ve cark donmuyorken alinir.
        /// Para bahis aninda duser (TrySpendMoney); gunluk ozet bunu tur sonunda giderden geri alir.
        /// </summary>
        public void PlaceBet(RouletteBetSpot spot)
        {
            if (spot == null || !IsOpen || EconomyManager.Instance == null) return;
            if (Phase == RoulettePhase.Spinning || Phase == RoulettePhase.Result) return;
            if (!EconomyManager.Instance.TrySpendMoney(betAmount)) return;

            stakes.TryGetValue(spot, out int stake);
            stakes[spot] = stake + betAmount;
            RefreshChips(spot);
        }

        // ---- Tur dongusu ----

        private IEnumerator RoundLoop()
        {
            var idleWait = new WaitForSeconds(0.5f);
            var second = new WaitForSeconds(1f);

            while (true)
            {
                SetPhase(RoulettePhase.Idle, 0);
                while (!IsOpen || (SeatedCount == 0 && stakes.Count == 0)) yield return idleWait;

                PlaceGamblerChips();
                for (int left = bettingSeconds; left > 0; left--)
                {
                    SetPhase(RoulettePhase.Betting, left);
                    yield return second;
                }

                // Basladigi tur gunduze tassa bile tamamlanir (alinmis bahis iade edilmez, odenir).
                int result = Random.Range(0, RouletteRules.PocketCount);
                SetPhase(RoulettePhase.Spinning, 0);
                if (wheel != null) wheel.Spin(result, spinSeconds);
                yield return new WaitForSeconds(spinSeconds);

                Resolve(result);
                SetPhase(RoulettePhase.Result, 0);
                OnResult?.Invoke(result);
                SetHighlight(result, true);

                yield return new WaitForSeconds(resultSeconds);

                SetHighlight(result, false);
                ClearChips();
                OnRoundFinished?.Invoke();
            }
        }

        /// <summary>
        /// Oyuncu bahisleri ve kasa payi. Kumarhane parasinin TEK gecis noktasi burasi (ileride co-op'ta RPC siniri).
        /// </summary>
        private void Resolve(int result)
        {
            int stake = 0;
            int payout = 0;

            foreach (KeyValuePair<RouletteBetSpot, int> bet in stakes)
            {
                stake += bet.Value;
                if (bet.Key.Wins(result))
                    payout += bet.Value * (bet.Key.Kind == RouletteBetKind.Number ? straightMultiplier : colorMultiplier);
            }

            if (stake > 0)
            {
                if (payout > 0 && EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(payout);

                var roundResult = new GamblingRoundResult(payout > stake, stake, payout);
                NightNet += roundResult.Net;
                if (SuspicionManager.Instance != null) SuspicionManager.Instance.Add(suspicionPerRound);

                OnRoundPlayed?.Invoke(roundResult);
                AnyRoundPlayed?.Invoke(roundResult);
            }

            int gamblers = SeatedCount;
            if (gamblers <= 0) return;

            int rake = gamblers * houseRakePerGambler;
            if (EconomyManager.Instance != null) EconomyManager.Instance.AddMoney(rake);
            NightNet += rake;
            if (SuspicionManager.Instance != null) SuspicionManager.Instance.Add(gamblers * suspicionPerGambler);

            OnHouseRound?.Invoke(rake);
            AnyHouseRound?.Invoke(rake);
        }

        private void SetPhase(RoulettePhase phase, int secondsLeft)
        {
            Phase = phase;
            OnPhaseChanged?.Invoke(phase, secondsLeft);
        }

        /// <summary>Gorsellik: oturan her kumarbaz rastgele bir alana fis koyar (para hesabina girmez).</summary>
        private void PlaceGamblerChips()
        {
            if (spots == null || spots.Length == 0) return;

            for (int i = 0; i < SeatedCount; i++)
            {
                RouletteBetSpot spot = spots[Random.Range(0, spots.Length)];
                gamblerChips.TryGetValue(spot, out int count);
                gamblerChips[spot] = count + 1;
                RefreshChips(spot);
            }
        }

        private void RefreshChips(RouletteBetSpot spot)
        {
            stakes.TryGetValue(spot, out int stake);
            gamblerChips.TryGetValue(spot, out int npc);
            spot.SetChips(stake / Mathf.Max(1, betAmount), npc);
        }

        private void ClearChips()
        {
            var touched = new HashSet<RouletteBetSpot>(stakes.Keys);
            touched.UnionWith(gamblerChips.Keys);
            stakes.Clear();
            gamblerChips.Clear();

            foreach (RouletteBetSpot spot in touched)
            {
                if (spot != null) spot.SetChips(0, 0);
            }
        }

        private void SetHighlight(int result, bool highlighted)
        {
            if (spots == null) return;

            foreach (RouletteBetSpot spot in spots)
            {
                if (spot != null && spot.Kind == RouletteBetKind.Number && spot.Number == result)
                    spot.SetHighlighted(highlighted);
            }
        }

        // ---- Sandalyeler ----

        /// <summary>Bos bir sandalyeyi kumarbaza ayirir. Bos yoksa false doner.</summary>
        public bool TryReserveSeat(Gambler gambler, out int seatIndex)
        {
            seatIndex = FindFreeSeat();
            if (seatIndex < 0 || gambler == null) return false;

            seatOccupants[seatIndex] = gambler;
            seated[seatIndex] = false;
            return true;
        }

        /// <summary>Kumarbaz sandalyesine vardi: bundan sonraki turlarda kasaya pay birakir.</summary>
        public void NotifySeated(int seatIndex, Gambler gambler)
        {
            if (seatOccupants == null || seatIndex < 0 || seatIndex >= seatOccupants.Length) return;
            if (seatOccupants[seatIndex] == gambler) seated[seatIndex] = true;
        }

        /// <summary>Kumarbaz kalkinca sandalyeyi bosaltir (sadece gercek sahibi bosaltabilir).</summary>
        public void ReleaseSeat(int seatIndex, Gambler gambler)
        {
            if (seatOccupants == null || seatIndex < 0 || seatIndex >= seatOccupants.Length) return;
            if (seatOccupants[seatIndex] != gambler) return;

            seatOccupants[seatIndex] = null;
            seated[seatIndex] = false;
        }

        public Vector3 GetSeatPosition(int seatIndex) => seatPositions[seatIndex];

        private int FindFreeSeat()
        {
            // Ilk ihtiyacta hesaplanir: masa o ana kadar NavMesh'te kendi deligini acmis olur.
            if (seatPositions == null)
            {
                seatPositions = CalculateSeats();
                seatOccupants = new Gambler[seatPositions.Length];
                seated = new bool[seatPositions.Length];
            }

            for (int i = 0; i < seatOccupants.Length; i++)
            {
                // Yok edilmis kumarbaz (Unity null'u) da bos sayilir.
                if (seatOccupants[i] == null) return i;
            }

            return -1;
        }

        /// <summary>
        /// Aday noktalardan NavMesh'te yurunebilir olanlardan birbirine yeterince uzak en fazla maxSeats tanesini sandalye yapar.
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

        /// <summary>
        /// Masanin yerel eksenlerinde (masa donse de dogru): iki uzun kenarda 3'er, carkin karsi ucunda 2 nokta.
        /// Carkin oldugu +X ucuna sandalye konmaz (krupiye yeri).
        /// </summary>
        private IEnumerable<Vector3> GetSeatCandidates()
        {
            if (!TryGetComponent(out BoxCollider box))
            {
                yield return transform.position;
                yield break;
            }

            Vector3 half = box.size * 0.5f;
            float y = box.center.y - half.y;
            float side = half.z + seatClearance;
            float firstX = -half.x + 0.35f;
            float lastX = half.x - wheelEndLength;

            for (int i = 0; i < 3; i++)
            {
                float x = Mathf.Lerp(firstX, lastX, i / 2f);
                yield return transform.TransformPoint(new Vector3(x, y, -side));
                yield return transform.TransformPoint(new Vector3(x, y, side));
            }

            float endX = -half.x - seatClearance;
            yield return transform.TransformPoint(new Vector3(endX, y, -seatSpacing * 0.5f));
            yield return transform.TransformPoint(new Vector3(endX, y, seatSpacing * 0.5f));
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

        /// <summary>Dukkan kapandi: yeni gece basliyor, gecelik hesap sifirlanir.</summary>
        private void HandleShopClosed(int day) => NightNet = 0;

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed -= HandleShopClosed;
            if (FurnitureManager.Instance != null)
                FurnitureManager.Instance.OnLayoutChanged -= HandleLayoutChanged;
        }
    }
}
