using System.Collections;
using KanBagi.Illegal;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Polis baskini bildirimleri: uyari, memurun gelisi ve sonuc.
    /// SADECE PoliceManager event'lerini dinler (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class PoliceNotificationUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [Tooltip("Bos birakilirsa bu objenin uzerindeki TMP_Text kullanilir.")]
        [SerializeField] private TMP_Text messageText;

        [Header("Mesajlar")]
        [SerializeField] private string warningMessage = "DEVRIYE MAHALLEDE! Kumar masasini ort!";
        [SerializeField] private string arrivedMessage = "POLIS DUKKANDA - etrafa bakiyor";
        [Tooltip("{0} kesilen ceza.")]
        [SerializeField] private string bustedFormat = "YAKALANDIN! Ceza: {0} TL";
        [SerializeField] private string cleanMessage = "Kontrol temiz gecti";

        [Header("Gorunum")]
        [Tooltip("Sonuc mesajinin ekranda kalma suresi (saniye).")]
        [SerializeField] private float resultDuration = 5f;
        [SerializeField] private Color warningColor = new Color(0.95f, 0.75f, 0.2f);
        [SerializeField] private Color dangerColor = new Color(0.9f, 0.3f, 0.3f);
        [SerializeField] private Color calmColor = new Color(0.4f, 0.85f, 0.4f);

        private PoliceManager police;
        private Coroutine hideRoutine;

        private void Start()
        {
            if (messageText == null) messageText = GetComponent<TMP_Text>();

            if (messageText == null)
            {
                Debug.LogError("[PoliceNotificationUI] 'Message Text' atanmamis. Canvas altindaki TMP objesini bagla.", this);
                enabled = false;
                return;
            }

            police = PoliceManager.Instance;

            if (police == null)
            {
                Debug.LogError("[PoliceNotificationUI] PoliceManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            police.OnRaidWarning += HandleRaidWarning;
            police.OnOfficerArrived += HandleOfficerArrived;
            police.OnRaidResolved += HandleRaidResolved;

            messageText.enabled = false;
        }

        // Uyari, memur gelene kadar ekranda kalir: oyuncu sureyi hissetsin.
        private void HandleRaidWarning(float seconds) => Show(warningMessage, warningColor, seconds);

        private void HandleOfficerArrived() => Show(arrivedMessage, dangerColor, resultDuration);

        private void HandleRaidResolved(bool busted, int fine)
        {
            if (busted)
                Show(string.Format(bustedFormat, fine), dangerColor, resultDuration);
            else
                Show(cleanMessage, calmColor, resultDuration);
        }

        private void Show(string message, Color color, float duration)
        {
            messageText.text = message;
            messageText.color = color;
            messageText.enabled = true;

            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideRoutine(duration));
        }

        private IEnumerator HideRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);

            messageText.enabled = false;
            hideRoutine = null;
        }

        private void OnDestroy()
        {
            if (police == null) return;

            police.OnRaidWarning -= HandleRaidWarning;
            police.OnOfficerArrived -= HandleOfficerArrived;
            police.OnRaidResolved -= HandleRaidResolved;
        }
    }
}
