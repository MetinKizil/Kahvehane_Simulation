using KanBagi.Interaction;
using KanBagi.Shop;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Ekrandaki etkilesim ipucu ("[E] Cay Doldur" gibi).
    /// PlayerInteractor'un OnFocusChanged event'ini ve istasyonlarin is-bitti olayini (ipucu tazelensin) dinler;
    /// icinde oyun mantigi yoktur (ANAYASA: UI, mantiktan ayri).
    /// NOT: Bu component prompt yazisinin KENDISINE degil, Canvas'a eklenmelidir;
    /// cunku odak yokken prompt objesini kapatir.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private TMP_Text promptText;

        private void Start()
        {
            interactor.OnFocusChanged += HandleFocusChanged;
            // Bir istasyon isini bitirince (ocak bardagi doldurdu, lavabo yikadi) bakilan objenin ipucu degismis olabilir.
            TimedStation.AnyWorkFinished += HandleWorkFinished;
            HandleFocusChanged(null); // Baslangicta gizli.
        }

        private void OnDestroy()
        {
            TimedStation.AnyWorkFinished -= HandleWorkFinished;
            if (interactor != null)
                interactor.OnFocusChanged -= HandleFocusChanged;
        }

        private void HandleWorkFinished(TimedStation station) => interactor.RefreshFocus();

        private void HandleFocusChanged(IInteractable interactable)
        {
            bool hasFocus = interactable != null;
            promptText.gameObject.SetActive(hasFocus);

            if (hasFocus)
                promptText.text = $"[E] {interactable.GetPrompt(interactor.Context)}";
        }
    }
}
