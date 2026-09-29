using KanBagi.Core;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Ekrandaki gun ve saat gostergesi. SADECE DayCycleManager event'lerini dinler;
    /// icinde zaman mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class ClockUI : MonoBehaviour
    {
        [Tooltip("Bos birakilirsa bu objenin uzerindeki TMP_Text kullanilir.")]
        [SerializeField] private TMP_Text clockText;

        [Header("Gorunum")]
        [Tooltip("{0} gun numarasi, {1} saat.")]
        [SerializeField] private string labelFormat = "Gun {0} - {1}";
        [Tooltip("Dukkan kapaliyken saatin yanina eklenir.")]
        [SerializeField] private string closedSuffix = "  (KAPALI)";
        [Tooltip("Son musteri saati gectiginde saatin yanina eklenir.")]
        [SerializeField] private string lastCallSuffix = "  (SON MUSTERI)";

        private DayCycleManager dayCycle;
        private string lastLabel;

        private void Start()
        {
            if (clockText == null) clockText = GetComponent<TMP_Text>();

            if (clockText == null)
            {
                Debug.LogError("[ClockUI] 'Clock Text' atanmamis. Canvas altindaki TMP objesini bagla.", this);
                enabled = false;
                return;
            }

            dayCycle = DayCycleManager.Instance;

            if (dayCycle == null)
            {
                Debug.LogError("[ClockUI] DayCycleManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            dayCycle.OnTimeChanged += HandleTimeChanged;
            dayCycle.OnDayStarted += HandleDayEvent;
            dayCycle.OnShopOpened += HandleDayEvent;
            dayCycle.OnShopClosed += HandleDayEvent;

            Refresh();
        }

        private void HandleTimeChanged(float dayProgress) => Refresh();

        private void HandleDayEvent(int day) => Refresh();

        private void Refresh()
        {
            string label = string.Format(labelFormat, dayCycle.CurrentDay, dayCycle.GetClockLabel());

            if (!dayCycle.IsShopOpen) label += closedSuffix;
            else if (!dayCycle.AcceptsNewCustomers) label += lastCallSuffix;

            // Saat her karede degismiyor; ayni metni tekrar yazip cop uretmeyelim.
            if (label == lastLabel) return;

            lastLabel = label;
            clockText.text = label;
        }

        private void OnDestroy()
        {
            if (dayCycle == null) return;

            dayCycle.OnTimeChanged -= HandleTimeChanged;
            dayCycle.OnDayStarted -= HandleDayEvent;
            dayCycle.OnShopOpened -= HandleDayEvent;
            dayCycle.OnShopClosed -= HandleDayEvent;
        }
    }
}
