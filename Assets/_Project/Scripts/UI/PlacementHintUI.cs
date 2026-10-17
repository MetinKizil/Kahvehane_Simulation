using System.Collections;
using KanBagi.Furniture;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Yerlestirme modunun ekran ipucu ("Sol tik: Koy | Tekerlek: Dondur...") ve kisa mesajlari ("Masa dolu, tasinamaz").
    /// SADECE FurniturePlacer event'lerini dinler (ANAYASA: UI mantiktan ayri).
    /// Kurulum: Kan Bagi > Yerlestirme Modunu Kur
    /// </summary>
    public class PlacementHintUI : MonoBehaviour
    {
        [Tooltip("Gosterilecek oyuncunun yerlestiricisi. Bos birakilirsa sahnedeki ilk FurniturePlacer.")]
        [SerializeField] private FurniturePlacer placer;
        [Tooltip("Bos birakilirsa bu objenin uzerindeki TMP_Text.")]
        [SerializeField] private TMP_Text hintText;

        [SerializeField] private string placingHint = "Sol tik: Koy   |   Tekerlek: Dondur (Shift: 45)   |   Sag tik: Iptal";
        [Tooltip("Kisa mesajlarin ekranda kalma suresi (saniye).")]
        [SerializeField] private float messageDuration = 2f;

        private Coroutine messageRoutine;

        private void Start()
        {
            if (hintText == null) hintText = GetComponent<TMP_Text>();
            if (placer == null) placer = FindFirstObjectByType<FurniturePlacer>();

            if (hintText == null || placer == null)
            {
                Debug.LogError("[PlacementHintUI] 'Hint Text' veya FurniturePlacer bulunamadi. " +
                               "'Kan Bagi > Yerlestirme Modunu Kur' menusunu calistir.", this);
                enabled = false;
                return;
            }

            placer.OnPlacingChanged += HandlePlacingChanged;
            placer.OnMessage += HandleMessage;
            hintText.enabled = false;
        }

        private void HandlePlacingChanged(bool isPlacing)
        {
            StopMessage();
            hintText.text = placingHint;
            hintText.enabled = isPlacing;
        }

        private void HandleMessage(string message)
        {
            StopMessage();
            messageRoutine = StartCoroutine(ShowMessageRoutine(message));
        }

        private IEnumerator ShowMessageRoutine(string message)
        {
            hintText.text = message;
            hintText.enabled = true;

            yield return new WaitForSeconds(messageDuration);

            messageRoutine = null;

            // Mesaj bitince yerlestirme suruyorsa ipucu geri gelir.
            hintText.text = placingHint;
            hintText.enabled = placer.IsPlacing;
        }

        private void StopMessage()
        {
            if (messageRoutine == null) return;

            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        private void OnDestroy()
        {
            if (placer == null) return;

            placer.OnPlacingChanged -= HandlePlacingChanged;
            placer.OnMessage -= HandleMessage;
        }
    }
}
