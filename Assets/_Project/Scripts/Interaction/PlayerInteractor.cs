using System;
using KanBagi.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KanBagi.Interaction
{
    /// <summary>
    /// Kameradan kisa mesafeli isin atarak oyuncunun baktigi IInteractable'i bulur.
    /// Odak degistiginde OnFocusChanged event'i yayinlar (Observer Pattern);
    /// UI bu event'e abone olur, kimse Update'te bunu poll etmez.
    /// E'ye basilinca odaktaki nesnenin Interact(Context) metodunu cagirir.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Bos birakilirsa proje genelindeki (Project-wide) Input Actions asset'i kullanilir.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Isin Ayarlari")]
        [Tooltip("Bos birakilirsa child'daki Camera otomatik bulunur.")]
        [SerializeField] private Transform rayOrigin;
        [Tooltip("Etkilesim mesafesi (metre).")]
        [SerializeField] private float interactRange = 2.5f;

        private InputActionMap playerMap;
        private InputAction interactAction;
        private IInteractable currentFocus;

        /// <summary>
        /// Bu oyuncu (eli, kilidi). Her etkilesime gecirilir; ipucu UI'i da bununla sorar.
        /// Ayni objedeki PlayerHands / PlayerLock'tan kurulur, sahneye ayrica eklenmez.
        /// </summary>
        public PlayerContext Context { get; private set; }

        /// <summary>Bakilan etkilesilebilir nesne degistiginde tetiklenir (odak yoksa null).</summary>
        public event Action<IInteractable> OnFocusChanged;

        private void Awake()
        {
            Context = new PlayerContext(GetComponent<PlayerHands>(), GetComponent<PlayerLock>());

            if (!ResolveRayOrigin() || !ResolveInput())
                enabled = false;
        }

        private bool ResolveRayOrigin()
        {
            if (rayOrigin == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) rayOrigin = childCamera.transform;
            }

            if (rayOrigin == null)
            {
                Debug.LogError(
                    "[PlayerInteractor] Kamera bulunamadi. Main Camera'yi Player'in ALTINA (child) tasi " +
                    "veya Inspector'daki 'Ray Origin' alanina surukle.", this);
                return false;
            }

            return true;
        }

        private bool ResolveInput()
        {
            if (inputActions == null) inputActions = InputSystem.actions;

            if (inputActions == null)
            {
                Debug.LogError(
                    "[PlayerInteractor] Input Actions asset'i yok. Inspector'daki 'Input Actions' alanina " +
                    "Assets/InputSystem_Actions dosyasini surukle.", this);
                return false;
            }

            playerMap = inputActions.FindActionMap("Player");
            interactAction = playerMap?.FindAction("Interact");

            if (interactAction == null)
            {
                Debug.LogError("[PlayerInteractor] 'Player' haritasinda 'Interact' action'i bulunamadi.", this);
                return false;
            }

            return true;
        }

        private void OnEnable()
        {
            playerMap.Enable();
            // "started" kullaniyoruz: action'daki Hold tanimina ragmen tusa basilan AN tetiklenir.
            interactAction.started += HandleInteractInput;
        }

        private void OnDisable()
        {
            if (interactAction != null)
                interactAction.started -= HandleInteractInput;

            // Kontrol kilitlendiginde (ornegin bilgisayar ekrani acilinca) ekranda asili ipucu kalmasin.
            if (currentFocus == null) return;

            currentFocus = null;
            OnFocusChanged?.Invoke(null);
        }

        private void Update()
        {
            UpdateFocus();
        }

        private void UpdateFocus()
        {
            IInteractable found = null;

            bool hitSomething = Physics.Raycast(
                rayOrigin.position, rayOrigin.forward, out RaycastHit hit,
                interactRange, ~0, QueryTriggerInteraction.Ignore);

            // Oyuncunun kendi collider'ini (CharacterController) ve tasidigi esyalari yok say.
            if (hitSomething && hit.transform.root != transform.root)
                hit.collider.TryGetComponent(out found);

            if (found == currentFocus) return;

            currentFocus = found;
            OnFocusChanged?.Invoke(currentFocus);
        }

        private void HandleInteractInput(InputAction.CallbackContext context)
        {
            if (currentFocus == null) return;

            currentFocus.Interact(Context);

            // Etkilesim nesneyi yok etmis olabilir (ornegin bardak rafa iade edildi).
            if (currentFocus is Component focusComponent && focusComponent == null)
                currentFocus = null;

            // Ipucu metni degismis olabilir ("Bardak Al" -> "Bardagi Rafa Koy").
            // Odak nesnesi ayni kaldigi icin UI'yi elle tazeliyoruz.
            OnFocusChanged?.Invoke(currentFocus);
        }
    }
}
