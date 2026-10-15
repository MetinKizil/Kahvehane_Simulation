using System;
using System.Collections;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Economy;
using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Shop;
using UnityEngine;
using UnityEngine.AI;

namespace KanBagi.Customers
{
    /// <summary>Musterinin icinde bulundugu asama.</summary>
    public enum CustomerState
    {
        Arriving,        // Dukkana giriyor.
        WalkingToTable,  // Ayrilan masaya yuruyor.
        WaitingForOrder, // Oturdu, siparisini bekliyor.
        Drinking,        // Siparisi geldi, iciyor.
        Leaving          // Kalkti, cikisa gidiyor.
    }

    /// <summary>
    /// Bir musterinin yasam dongusu: gelir, masaya oturur, siparis bekler,
    /// servis edilirse icip parasini oder, edilmezse sabri tukenip kizgin gider.
    /// Durum makinesi coroutine ile yurutulur; her asama kendi kodunda durur,
    /// Update icinde if yigini olusmaz.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Customer : MonoBehaviour
    {
        [Header("Siparis")]
        [Tooltip("Musterinin istedigi urun. BOS BIRAKILIRSA dukkanin menusunden rastgele secilir.")]
        [SerializeField] private ItemData desiredOrder;
        [Tooltip("Cayini icme suresi (saniye). Bitince bardak kirlenir ve musteri kalkar.")]
        [SerializeField] private float drinkDuration = 6f;

        [Header("Sabir")]
        [Tooltip("Musterinin siparisini bekleyecegi sure (saniye).")]
        [SerializeField] private float patienceDuration = 25f;

        [Header("Itibar")]
        [Tooltip("Siparis zamaninda teslim edilince kazanilan itibar.")]
        [SerializeField] private int reputationOnServed = 1;
        [Tooltip("Sabri tukenip bos donunce kaybedilen itibar (pozitif yazilir, eksi olarak islenir).")]
        [SerializeField] private int reputationOnAngryLeave = 1;

        [Header("Hareket")]
        [Tooltip("Hedefe bu mesafeye gelince varmis sayilir.")]
        [SerializeField] private float arriveThreshold = 0.4f;
        [Tooltip("Yol bulunamazsa bu sure sonunda takilip kalmamak icin devam edilir.")]
        [SerializeField] private float moveTimeout = 30f;

        // URP Lit ve Standard shader'larin ana renk ozellikleri.
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private NavMeshAgent agent;
        private Table reservedTable;
        private Vector3 exitPosition;
        private TeaGlass servedGlass;
        private CustomerProfile profile;
        private bool hasFixedOrder;

        public CustomerState State { get; private set; } = CustomerState.Arriving;

        /// <summary>Musterinin tipi (atanmamissa null: prefab'daki varsayilan degerler kullanilir).</summary>
        public CustomerProfile Profile => profile;
        public bool IsWaitingForOrder => State == CustomerState.WaitingForOrder;

        /// <summary>Siparisi teslim edildi mi? Gunluk ozet bunu okur.</summary>
        public bool WasServed { get; private set; }

        /// <summary>Bu musterinin kasaya biraktigi para (servis edilmediyse 0).</summary>
        public int PaidAmount { get; private set; }

        /// <summary>Musterinin istedigi urun (atanmamissa null). Siparis balonu bunu okur.</summary>
        public ItemData Order => desiredOrder;

        /// <summary>Asama degistiginde tetiklenir. Siparis balonu buna abone olur.</summary>
        public event Action<CustomerState> OnStateChanged;

        /// <summary>Kalan sabir orani (1 = yeni oturdu, 0 = tukendi).</summary>
        public event Action<float> OnPatienceChanged;

        /// <summary>Musteri dukkandan ciktiginda tetiklenir. Spawner buna abone olur.</summary>
        public event Action<Customer> OnLeft;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        /// <summary>Spawner tarafindan cagrilir; musterinin yasam dongusunu baslatir.</summary>
        public void Initialize(Vector3 exit, CustomerProfile customerProfile = null)
        {
            exitPosition = exit;
            profile = customerProfile;
            hasFixedOrder = desiredOrder != null;

            ApplyBodyColor();
            ResolveOrder();
            StartCoroutine(LifeRoutine());
        }

        /// <summary>Profilin rengini govdeye verir. Malzeme kopyalanmaz (MaterialPropertyBlock).</summary>
        private void ApplyBodyColor()
        {
            if (profile == null || !TryGetComponent(out Renderer body)) return;

            var block = new MaterialPropertyBlock();
            body.GetPropertyBlock(block);
            block.SetColor(BaseColorId, profile.BodyColor);
            block.SetColor(ColorId, profile.BodyColor);
            body.SetPropertyBlock(block);
        }

        /// <summary>
        /// Inspector'da siparis atanmamissa once profilin favorilerinden (menude olanlar), yoksa
        /// dukkanin menusunden rastgele secer. Menu kilitli icecekleri icermez; sadece YAPABILDIGIMIZ seyler istenir.
        /// </summary>
        private void ResolveOrder()
        {
            if (desiredOrder != null) return;

            if (MenuManager.Instance == null || !MenuManager.Instance.HasAnyBeverage)
            {
                Debug.LogWarning($"[Customer] '{name}' icin menu bos. Sahnede MenuManager var mi?", this);
                return;
            }

            desiredOrder = PickFavoriteOnMenu() ?? MenuManager.Instance.GetRandomBeverage();
        }

        /// <summary>Profilin menude bulunan favorileri arasindan agirlikli secim. Hicbiri yoksa null.</summary>
        private ItemData PickFavoriteOnMenu()
        {
            if (profile == null) return null;

            int totalWeight = 0;
            foreach (DrinkPreference preference in profile.FavoriteDrinks)
            {
                if (preference.Weight > 0 && MenuManager.Instance.IsOnMenu(preference.Beverage))
                    totalWeight += preference.Weight;
            }

            if (totalWeight == 0) return null;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (DrinkPreference preference in profile.FavoriteDrinks)
            {
                if (preference.Weight <= 0 || !MenuManager.Instance.IsOnMenu(preference.Beverage)) continue;

                roll -= preference.Weight;
                if (roll < 0) return preference.Beverage;
            }

            return null;
        }

        /// <summary>
        /// Bu bardak su anda bu musteriye verilebilir mi?
        /// Masanin ipucu metni ve teslim ani AYNI kurali kullansin diye disari acildi.
        /// </summary>
        public bool CanAcceptOrder(TeaGlass glass)
        {
            if (!IsWaitingForOrder) return false;
            if (glass == null || glass.State != GlassState.Full || glass.Content == null) return false;

            return desiredOrder != null
                ? glass.Content == desiredOrder
                : glass.Content.Type == ItemType.Beverage;
        }

        /// <summary>
        /// Masaya konan bardagi teslim alir: parasini oder, itibar kazandirir ve icmeye baslar.
        /// Kabul etmezse false doner ve hicbir sey degismez.
        /// </summary>
        public bool TryServe(TeaGlass glass)
        {
            if (!CanAcceptOrder(glass)) return false;

            servedGlass = glass;
            WasServed = true;
            CompleteSale(glass.Content);

            // Durum degisince WaitForOrderRoutine sabir dongusunden kendiliginden cikar.
            SetState(CustomerState.Drinking);
            return true;
        }

        private IEnumerator LifeRoutine()
        {
            // Bos masa yoksa musteri iceri girmeden geri doner.
            if (TableManager.Instance == null ||
                !TableManager.Instance.TryReserveFreeTable(this, out reservedTable))
            {
                yield return LeaveRoutine();
                yield break;
            }

            SetState(CustomerState.WalkingToTable);
            yield return MoveToRoutine(reservedTable.SeatPosition);

            SitDown();
            SetState(CustomerState.WaitingForOrder);
            yield return WaitForOrderRoutine();

            if (State == CustomerState.Drinking)
            {
                yield return DrinkRoutine();

                if (WantsSecondRound()) yield return SecondRoundRoutine();
            }
            else
            {
                LosePatience();
            }

            yield return LeaveRoutine();
        }

        /// <summary>Masa oyunu varsa ictikten sonra bir tur daha ister mi? (dukkan payi x profilin oyun meraki)</summary>
        private bool WantsSecondRound()
        {
            float chance = CustomerModifiers.Instance != null ? CustomerModifiers.Instance.SecondRoundChance : 0f;
            if (profile != null) chance *= profile.GameCuriosity;

            return chance > 0f && UnityEngine.Random.value < chance;
        }

        /// <summary>
        /// En fazla BIR ekstra tur: yeni siparis verir, beklerse icer.
        /// Bu turda sabri biterse itibar cezasi YOK - zaten memnun, sadece israr etmeden kalkar.
        /// Masadaki kirli bardak kaldirilmadan yeni bardak konamaz (Table kurali).
        /// </summary>
        private IEnumerator SecondRoundRoutine()
        {
            if (!hasFixedOrder) desiredOrder = null;
            ResolveOrder();

            SetState(CustomerState.WaitingForOrder);
            yield return WaitForOrderRoutine();

            if (State == CustomerState.Drinking) yield return DrinkRoutine();
        }

        private IEnumerator WaitForOrderRoutine()
        {
            // Sabir upgrade'lerle uzayabilir; toplam sure basta bir kere hesaplanir.
            float total = GetEffectivePatience();
            float remaining = total;

            while (remaining > 0f && State == CustomerState.WaitingForOrder)
            {
                remaining -= Time.deltaTime;
                OnPatienceChanged?.Invoke(Mathf.Clamp01(remaining / total));
                yield return null;
            }
        }

        /// <summary>Temel sabir x profilin sabri x dukkandaki sabir carpani (okey takimi vb.).</summary>
        private float GetEffectivePatience()
        {
            float multiplier = CustomerModifiers.Instance != null
                ? CustomerModifiers.Instance.PatienceMultiplier
                : 1f;

            if (profile != null) multiplier *= profile.PatienceMultiplier;

            return Mathf.Max(0.1f, patienceDuration * multiplier);
        }

        /// <summary>
        /// Kapanis saati geldi: musteri ne yapiyorsa birakip cikar.
        /// Yarim kalan bardak masada kirli olarak kalir, kilitli kalmaz.
        /// </summary>
        public void LeaveNow()
        {
            if (State == CustomerState.Leaving) return;

            StopAllCoroutines();

            // Servis edilmeden kapida kalmak da memnuniyetsizliktir.
            if (!WasServed) LosePatience();

            FinishDrink();
            StartCoroutine(LeaveRoutine());
        }

        private IEnumerator DrinkRoutine()
        {
            // Musteri icerken bardak masadan alinamaz.
            SetServedGlassPickupEnabled(false);

            float duration = profile != null ? drinkDuration * profile.DrinkDurationMultiplier : drinkDuration;
            yield return new WaitForSeconds(duration);

            FinishDrink();
        }

        /// <summary>Icme bitti (veya yarida kesildi): bardak masada KIRLI kalir, alinabilir olur.</summary>
        private void FinishDrink()
        {
            if (servedGlass == null) return;

            servedGlass.MarkDirty();
            SetServedGlassPickupEnabled(true);

            servedGlass = null;
        }

        private void SetServedGlassPickupEnabled(bool isEnabled)
        {
            if (servedGlass == null) return;

            if (servedGlass.TryGetComponent(out HoldableItem holdable))
                holdable.SetPickupEnabled(isEnabled);
        }

        private IEnumerator LeaveRoutine()
        {
            SetState(CustomerState.Leaving);
            ReleaseTable();
            StandUp();

            yield return MoveToRoutine(exitPosition);

            OnLeft?.Invoke(this);
            Destroy(gameObject);
        }

        private IEnumerator MoveToRoutine(Vector3 destination)
        {
            if (!agent.isActiveAndEnabled) yield break;

            agent.isStopped = false;
            agent.SetDestination(destination);

            float elapsed = 0f;

            while (elapsed < moveTimeout)
            {
                elapsed += Time.deltaTime;

                if (!agent.pathPending &&
                    agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveThreshold))
                    yield break;

                yield return null;
            }

            Debug.LogWarning($"[Customer] '{name}' hedefe ulasamadi, yol kapali olabilir.", this);
        }

        /// <summary>
        /// Teslim anindaki para ve itibar islemleri.
        /// Ileride ayri bir SalesManager'a tasinacaksa degistirilecek TEK yer burasi.
        /// </summary>
        private void CompleteSale(ItemData product)
        {
            if (product != null && EconomyManager.Instance != null)
            {
                // Bahsis de ciroya girer: gunluk ozet PaidAmount'u okur. Ikinci turda ustune eklenir.
                int payment = product.SellPrice + (profile != null ? profile.Tip : 0);
                PaidAmount += payment;
                EconomyManager.Instance.AddMoney(payment);
            }

            if (ReputationManager.Instance != null)
                ReputationManager.Instance.AddReputation(reputationOnServed);
        }

        /// <summary>Siparisi gelmedi: mahalledeki itibarimiz zarar gorur (profil varsa onun cezasi gecerli).</summary>
        private void LosePatience()
        {
            int penalty = profile != null ? profile.ReputationOnAngryLeave : reputationOnAngryLeave;
            if (penalty <= 0 || ReputationManager.Instance == null) return;

            ReputationManager.Instance.AddReputation(-penalty);
        }

        /// <summary>Masaya oturur: agent kapatilir ve masaya donulur.</summary>
        private void SitDown()
        {
            if (agent.isActiveAndEnabled) agent.isStopped = true;
            agent.enabled = false;

            if (reservedTable == null) return;

            Vector3 lookTarget = reservedTable.transform.position;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);
        }

        /// <summary>Kalkar: agent tekrar acilir ve NavMesh uzerine oturtulur.</summary>
        private void StandUp()
        {
            if (agent.enabled) return;

            agent.enabled = true;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        private void ReleaseTable()
        {
            if (reservedTable == null) return;

            reservedTable.Release();
            reservedTable = null;
        }

        private void SetState(CustomerState newState)
        {
            State = newState;
            OnStateChanged?.Invoke(State);
        }

        private void OnDestroy()
        {
            // Beklenmedik yok olmalarda masa kilitli kalmasin.
            ReleaseTable();
        }
    }
}
