using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Yeni oyunda (diskte kayit yokken) hikaye girisini sayfa sayfa gosterir.
    /// Acikken saat durur ve sahnedeki tum oyuncular kilitlenir; son sayfada oyun baslar.
    /// Metinleri StoryData'dan okur, kendisi hicbir oyun durumu tutmaz (ANAYASA: UI mantiktan ayri).
    ///
    /// Bu script panelin KOKUNDE durur ve kok HEP AKTIFTIR; acilip kapanan sey 'contentRoot' cocugudur.
    /// Kurulum: Kan Bagi > Hikaye Giris Panelini Olustur
    /// </summary>
    public class StoryIntroUI : MonoBehaviour
    {
        [Header("Veri")]
        [SerializeField] private StoryData story;

        [Header("Baglantilar")]
        [Tooltip("Acilip kapanan panel govdesi.")]
        [SerializeField] private GameObject contentRoot;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text pageText;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueLabel;
        [SerializeField] private Button skipButton;
        [SerializeField] private TMP_Text skipLabel;

        [Header("Gorunum")]
        [Tooltip("{0} su anki sayfa, {1} toplam sayfa.")]
        [SerializeField] private string pageFormat = "{0} / {1}";

        private DayCycleManager dayCycle;
        private int pageIndex;

        private void Start()
        {
            if (contentRoot == null || story == null || story.Pages.Count == 0)
            {
                Debug.LogError("[StoryIntroUI] 'Content Root' veya 'Story' atanmamis ya da hikayede sayfa yok. " +
                               "'Kan Bagi > Hikaye Giris Panelini Olustur' menusunu calistir.", this);
                if (contentRoot != null) contentRoot.SetActive(false);
                enabled = false;
                return;
            }

            dayCycle = DayCycleManager.Instance;

            if (continueButton != null) continueButton.onClick.AddListener(HandleContinueClicked);
            if (skipButton != null) skipButton.onClick.AddListener(Finish);
            if (skipLabel != null) skipLabel.text = story.SkipLabel;

            // Kayit varsa oyun kaldigi yerden devam eder: hikaye sadece yeni oyunda gosterilir.
            bool isNewGame = SaveManager.Instance == null || !SaveManager.Instance.HasSave;

            if (isNewGame) Open();
            else contentRoot.SetActive(false);
        }

        private void Open()
        {
            pageIndex = 0;
            ShowPage();
            contentRoot.SetActive(true);

            if (dayCycle != null) dayCycle.SetPaused(true);
            SetAllPlayersControl(false);
        }

        private void ShowPage()
        {
            StoryPage page = story.Pages[pageIndex];
            bool isLastPage = pageIndex == story.Pages.Count - 1;

            SetText(titleText, page.Title);
            SetText(bodyText, page.Body);
            SetText(pageText, string.Format(pageFormat, pageIndex + 1, story.Pages.Count));
            SetText(continueLabel, isLastPage ? story.FinishLabel : story.ContinueLabel);

            // Son sayfada "Atla" anlamsiz: tek buton oyunu baslatir.
            if (skipButton != null) skipButton.gameObject.SetActive(!isLastPage);
        }

        private void HandleContinueClicked()
        {
            if (pageIndex >= story.Pages.Count - 1)
            {
                Finish();
                return;
            }

            pageIndex++;
            ShowPage();
        }

        private void Finish()
        {
            contentRoot.SetActive(false);

            if (dayCycle != null) dayCycle.SetPaused(false);
            SetAllPlayersControl(true);
        }

        /// <summary>Hikaye herkes icin ayni anda okunur: sahnedeki TUM oyuncular kilitlenir / acilir.</summary>
        private static void SetAllPlayersControl(bool isEnabled)
        {
            foreach (PlayerLock playerLock in FindObjectsByType<PlayerLock>(FindObjectsSortMode.None))
                playerLock.SetControlEnabled(isEnabled);
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private void OnDestroy()
        {
            if (continueButton != null) continueButton.onClick.RemoveListener(HandleContinueClicked);
            if (skipButton != null) skipButton.onClick.RemoveListener(Finish);
        }
    }
}
