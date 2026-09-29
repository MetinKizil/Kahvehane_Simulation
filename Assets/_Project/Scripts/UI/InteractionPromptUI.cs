using KanBagi.Interaction;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Ekrandaki etkilesim ipucu ("[E] Cay Doldur" gibi).
    /// SADECE PlayerInteractor'un OnFocusChanged event'ini dinler;
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
            HandleFocusChanged(null); // Baslangicta gizli.
        }

        private void OnDestroy()
        {
            if (interactor != null)
                interactor.OnFocusChanged -= HandleFocusChanged;
        }

        private void HandleFocusChanged(IInteractable interactable)
        {
            bool hasFocus = interactable != null;
            promptText.gameObject.SetActive(hasFocus);

            if (hasFocus)
                promptText.text = $"[E] {interactable.InteractionPrompt}";
        }
    }
}
