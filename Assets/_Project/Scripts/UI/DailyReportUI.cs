using KanBagi.Core;
using KanBagi.Player;
using KanBagi.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Gun sonu ozet paneli. SADECE DailyStatsManager'dan okur ve gosterir;
    /// hicbir sayim veya hesap yapmaz (ANAYASA: UI, oyun mantigindan ayri).
    ///
    /// Bu script panelin KOKUNDE durur ve kok HEP AKTIFTIR - kapali obje event dinleyemez.
    /// Acilip kapanan sey 'contentRoot' cocugudur.
    /// </summary>
    public class DailyReportUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [Tooltip("Acilip kapanan panel govdesi. Baslangicta kapatilir.")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text servedText;
        [SerializeField] private TMP_Text lostText;
        [SerializeField] private TMP_Text revenueText;
        [SerializeField] private TMP_Text rentText;
        [SerializeField] private TMP_Text gamblingText;
        [SerializeField] private TMP_Text expenseText;
        [SerializeField] private TMP_Text profitText;
        [SerializeField] private TMP_Text reputationText;
        [SerializeField] private Button nextDayButton;

        [Tooltip("Bos birakilirsa sahnedeki ilk ComputerStation bulunur. Ozet acilirken PC ekrani kapatilir.")]
        [SerializeField] private ComputerStation computerStation;

        [Header("Gorunum")]
        [SerializeField] private string titleFormat = "GUN {0} OZETI";
        [SerializeField] private string servedFormat = "Servis edilen musteri: {0}";
        [SerializeField] private string lostFormat = "Kacirilan musteri: {0}";
        [SerializeField] private string revenueFormat = "Ciro: {0} TL";
        [SerializeField] private string rentFormat = "Kira geliri: {0} TL";
        [SerializeField] private string gamblingFormat = "Kumar: {0} TL";
        [SerializeField] private string expenseFormat = "Gider: {0} TL";
        [SerializeField] private string profitFormat = "Net: {0} TL";
        [Tooltip("{0} gun sonu itibari, {1} o gunku degisim (+/-).")]
        [SerializeField] private string reputationFormat = "Itibar: {0}  ({1})";

        [SerializeField] private Color profitColor = new Color(0.4f, 0.85f, 0.4f);
        [SerializeField] private Color lossColor = new Color(0.9f, 0.35f, 0.35f);

        private DayCycleManager dayCycle;

        private void Start()
        {
            if (contentRoot == null)
            {
                Debug.LogError("[DailyReportUI] 'Content Root' atanmamis.", this);
                enabled = false;
                return;
            }

            dayCycle = DayCycleManager.Instance;

            if (dayCycle == null)
            {
                Debug.LogError("[DailyReportUI] DayCycleManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            if (computerStation == null) computerStation = FindFirstObjectByType<ComputerStation>();

            dayCycle.OnDayEnded += HandleDayEnded;

            if (nextDayButton != null) nextDayButton.onClick.AddListener(HandleNextDayClicked);

            NormalizeLayout();
            contentRoot.SetActive(false);
        }

        /// <summary>
        /// Prefab yanlis bir ebeveynin altina birakilirsa panel kucucuk bir kosede kalir.
        /// Burada Canvas'in dogrudan cocugu oldugundan ve ekrani kapladigindan emin oluyoruz.
        /// </summary>
        private void NormalizeLayout()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();

            if (parentCanvas == null)
            {
                Debug.LogError("[DailyReportUI] Panel bir Canvas altinda degil. Prefab'i Canvas'in ALTINA tasi.", this);
                return;
            }

            if (transform.parent != parentCanvas.transform)
            {
                transform.SetParent(parentCanvas.transform, worldPositionStays: false);
                Debug.LogWarning("[DailyReportUI] Panel Canvas'in dogrudan cocugu degildi, yukari tasindi.", this);
            }

            // UI'da son cocuk en ustte cizilir: ozet her seyin onunde gorunsun.
            transform.SetAsLastSibling();

            StretchToParent(transform as RectTransform);
            StretchToParent(contentRoot.transform as RectTransform);
        }

        private static void StretchToParent(RectTransform rect)
        {
            if (rect == null) return;

            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void HandleDayEnded(int day)
        {
            // Iki panel ust uste binmesin: PC ekrani acikken gun bittiyse kapatilir.
            if (computerStation != null && computerStation.IsScreenOpen) computerStation.CloseScreen();

            Fill(day);
            contentRoot.SetActive(true);

            if (PlayerLock.Instance != null) PlayerLock.Instance.SetControlEnabled(false);
        }

        private void Fill(int day)
        {
            DailyStatsManager stats = DailyStatsManager.Instance;
            if (stats == null)
            {
                Debug.LogError("[DailyReportUI] DailyStatsManager bulunamadi. 'Managers' objesine ekle.", this);
                return;
            }

            SetText(titleText, string.Format(titleFormat, day));
            SetText(servedText, string.Format(servedFormat, stats.CustomersServed));
            SetText(lostText, string.Format(lostFormat, stats.CustomersLost));
            SetText(revenueText, string.Format(revenueFormat, stats.Revenue));
            SetText(rentText, string.Format(rentFormat, stats.RentIncome));

            SetText(gamblingText, string.Format(gamblingFormat, stats.GamblingNet));
            if (gamblingText != null) gamblingText.color = stats.GamblingNet >= 0 ? profitColor : lossColor;

            SetText(expenseText, string.Format(expenseFormat, stats.Expenses));

            SetText(profitText, string.Format(profitFormat, stats.NetProfit));
            if (profitText != null) profitText.color = stats.NetProfit >= 0 ? profitColor : lossColor;

            int change = stats.ReputationChange;
            SetText(reputationText, string.Format(reputationFormat, stats.CurrentReputation, change >= 0 ? $"+{change}" : change.ToString()));
            if (reputationText != null) reputationText.color = change >= 0 ? profitColor : lossColor;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private void HandleNextDayClicked()
        {
            contentRoot.SetActive(false);

            if (PlayerLock.Instance != null) PlayerLock.Instance.SetControlEnabled(true);

            dayCycle.RequestNextDay();
        }

        private void OnDestroy()
        {
            if (dayCycle != null) dayCycle.OnDayEnded -= HandleDayEnded;
            if (nextDayButton != null) nextDayButton.onClick.RemoveListener(HandleNextDayClicked);
        }
    }
}
