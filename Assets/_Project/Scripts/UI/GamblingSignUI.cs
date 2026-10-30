using KanBagi.Core;
using KanBagi.Illegal;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Kumar masasinin ustundeki 3D tabela: son el sonucu ve gecelik hesap.
    /// SADECE GamblingTable ve DayCycleManager event'lerini dinler;
    /// kumar mantigi icermez (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class GamblingSignUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [Tooltip("Bos birakilirsa ust objelerden, bulunamazsa sahnedeki ilk masa kullanilir.")]
        [SerializeField] private GamblingTable table;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text totalText;

        [Header("Gorunum")]
        [Tooltip("{0} fis tutari. Masa bos/bekliyorken.")]
        [SerializeField] private string openTitleFormat = "RULET  {0} TL";
        [SerializeField] private string closedTitle = "GUNDUZ KAPALI";
        [Tooltip("{0} kalan saniye.")]
        [SerializeField] private string bettingTitleFormat = "BAHISLER ACIK  {0}";
        [SerializeField] private string spinningTitle = "CARK DONUYOR";
        [Tooltip("{0} sayi, {1} renk adi.")]
        [SerializeField] private string resultTitleFormat = "{0}  {1}";
        [SerializeField] private string redName = "KIRMIZI";
        [SerializeField] private string blackName = "SIYAH";
        [SerializeField] private string greenName = "YESIL";
        [Tooltip("{0} kazanilan net tutar.")]
        [SerializeField] private string winFormat = "KAZANDIN  +{0} TL";
        [Tooltip("{0} kaybedilen tutar.")]
        [SerializeField] private string loseFormat = "KAYBETTIN  -{0} TL";
        [SerializeField] private string idleResult = "-";
        [Tooltip("{0} kumarbazlardan gelen masa parasi.")]
        [SerializeField] private string houseFormat = "KASA  +{0} TL";
        [Tooltip("{0} gecelik net.")]
        [SerializeField] private string totalFormat = "Gece: {0} TL";

        [SerializeField] private Color winColor = new Color(0.4f, 0.85f, 0.4f);
        [SerializeField] private Color loseColor = new Color(0.9f, 0.35f, 0.35f);
        [SerializeField] private Color idleColor = new Color(0.8f, 0.8f, 0.8f);

        private DayCycleManager dayCycle;

        private void Start()
        {
            if (table == null) table = GetComponentInParent<GamblingTable>();
            if (table == null) table = FindFirstObjectByType<GamblingTable>();

            if (table == null)
            {
                Debug.LogError($"[GamblingSignUI] '{name}' icin GamblingTable bulunamadi.", this);
                enabled = false;
                return;
            }

            table.OnRoundPlayed += HandleRoundPlayed;
            table.OnHouseRound += HandleHouseRound;
            table.OnPhaseChanged += HandlePhaseChanged;
            table.OnResult += HandleResult;

            dayCycle = DayCycleManager.Instance;

            if (dayCycle != null)
            {
                dayCycle.OnShopOpened += HandleShopStateChanged;
                dayCycle.OnShopClosed += HandleShopStateChanged;
            }

            SetText(resultText, idleResult, idleColor);
            RefreshTitle();
            RefreshTotal();
        }

        private void HandleRoundPlayed(GamblingRoundResult result)
        {
            // Bir turda birden fazla bahis olabilir: net kar/zarar gosterilir.
            if (result.Net > 0)
                SetText(resultText, string.Format(winFormat, result.Net), winColor);
            else
                SetText(resultText, string.Format(loseFormat, -result.Net), loseColor);

            RefreshTotal();
        }

        private void HandlePhaseChanged(RoulettePhase phase, int secondsLeft)
        {
            if (titleText == null) return;

            switch (phase)
            {
                case RoulettePhase.Betting:
                    SetText(titleText, string.Format(bettingTitleFormat, secondsLeft), Color.white);
                    break;
                case RoulettePhase.Spinning:
                    SetText(titleText, spinningTitle, Color.white);
                    break;
                case RoulettePhase.Idle:
                    RefreshTitle();
                    break;
            }
        }

        private void HandleResult(int number)
        {
            string colorName = number == 0 ? greenName : RouletteRules.IsRed(number) ? redName : blackName;
            Color color = number == 0 ? winColor : RouletteRules.IsRed(number) ? loseColor : Color.white;
            SetText(titleText, string.Format(resultTitleFormat, number, colorName), color);
        }

        private void HandleHouseRound(int rake)
        {
            SetText(resultText, string.Format(houseFormat, rake), winColor);
            RefreshTotal();
        }

        /// <summary>Gece basinda hesap sifirlandigi icin tabela da tazelenir.</summary>
        private void HandleShopStateChanged(int day)
        {
            SetText(resultText, idleResult, idleColor);
            RefreshTitle();
            RefreshTotal();
        }

        private void RefreshTitle()
        {
            if (titleText == null) return;

            titleText.text = table.IsOpen ? string.Format(openTitleFormat, table.BetAmount) : closedTitle;
            titleText.color = Color.white;
        }

        private void RefreshTotal()
        {
            if (totalText == null) return;

            totalText.text = string.Format(totalFormat, table.NightNet);
            totalText.color = table.NightNet > 0 ? winColor : table.NightNet < 0 ? loseColor : idleColor;
        }

        private static void SetText(TMP_Text target, string value, Color color)
        {
            if (target == null) return;

            target.text = value;
            target.color = color;
        }

        private void OnDestroy()
        {
            if (table != null)
            {
                table.OnRoundPlayed -= HandleRoundPlayed;
                table.OnHouseRound -= HandleHouseRound;
                table.OnPhaseChanged -= HandlePhaseChanged;
                table.OnResult -= HandleResult;
            }

            if (dayCycle == null) return;

            dayCycle.OnShopOpened -= HandleShopStateChanged;
            dayCycle.OnShopClosed -= HandleShopStateChanged;
        }
    }
}
