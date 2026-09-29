using KanBagi.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Istasyonlarin (ocak, lavabo...) ilerleme cubugu.
    /// SADECE TimedStation event'lerini dinler; is mantigi icermez
    /// (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class ProgressBarUI : MonoBehaviour
    {
        [Header("Gorseller")]
        [Tooltip("Cubugun tamami. Is yokken kapatilir.")]
        [SerializeField] private GameObject barRoot;
        [Tooltip("Image Type = Filled olarak ayarlanmis dolum gorseli.")]
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text labelText;

        [Header("Istasyonlar")]
        [Tooltip("Bos birakilirsa sahnedeki tum istasyonlar otomatik bulunur.")]
        [SerializeField] private TimedStation[] stations;

        private void Start()
        {
            // Inactive'ler de dahil: kilitli istasyonlar (kahve makinesi vb.) sonradan acildiginda
            // yeniden arama yapmadan calisir.
            if (stations == null || stations.Length == 0)
                stations = FindObjectsByType<TimedStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (TimedStation station in stations)
            {
                station.OnProgressChanged += HandleProgressChanged;
                station.OnWorkFinished += HandleWorkFinished;
            }

            SetBarVisible(false);
        }

        private void OnDestroy()
        {
            foreach (TimedStation station in stations)
            {
                if (station == null) continue;

                station.OnProgressChanged -= HandleProgressChanged;
                station.OnWorkFinished -= HandleWorkFinished;
            }
        }

        private void HandleProgressChanged(TimedStation station, float progress)
        {
            SetBarVisible(true);

            if (fillImage != null) fillImage.fillAmount = progress;
            if (labelText != null) labelText.text = $"{station.WorkLabel} %{progress * 100f:F0}";
        }

        private void HandleWorkFinished(TimedStation station)
        {
            SetBarVisible(false);
        }

        private void SetBarVisible(bool isVisible)
        {
            if (barRoot != null) barRoot.SetActive(isVisible);
        }
    }
}
