using UnityEngine;
using UnityEngine.InputSystem;

namespace KanBagi.Player
{
    /// <summary>
    /// Birinci sahis oyuncu kontrolu: WASD yurume + fare ile bakis.
    /// Projedeki InputSystem_Actions asset'indeki "Player" haritasini kullanir.
    /// Sadece hareketten sorumludur; etkilesim isi PlayerInteractor'dadir (SOLID).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Bos birakilirsa proje genelindeki (Project-wide) Input Actions asset'i kullanilir.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Hareket")]
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Bakis")]
        [Tooltip("Bos birakilirsa child'daki Camera otomatik bulunur.")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float lookSensitivity = 0.1f;
        [Tooltip("Kameranin yukari/asagi bakabilecegi maksimum aci.")]
        [SerializeField] private float pitchLimit = 85f;

        private CharacterController controller;
        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private float verticalVelocity;
        private float cameraPitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();

            if (!ResolveCamera() || !ResolveInput())
            {
                enabled = false;
                return;
            }
        }

        /// <summary>Kamera atanmadiysa child'lardan bulmayi dener. Bulunamazsa hata verir.</summary>
        private bool ResolveCamera()
        {
            if (cameraTransform == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) cameraTransform = childCamera.transform;
            }

            if (cameraTransform == null)
            {
                Debug.LogError(
                    "[PlayerController] Kamera bulunamadi. Main Camera'yi Player'in ALTINA (child) tasi " +
                    "veya Inspector'daki 'Camera Transform' alanina surukle.", this);
                return false;
            }

            return true;
        }

        /// <summary>Input asset'ini ve action'lari cozer. Eksikse net hata mesaji basar.</summary>
        private bool ResolveInput()
        {
            if (inputActions == null) inputActions = InputSystem.actions;

            if (inputActions == null)
            {
                Debug.LogError(
                    "[PlayerController] Input Actions asset'i yok. Inspector'daki 'Input Actions' alanina " +
                    "Assets/InputSystem_Actions dosyasini surukle.", this);
                return false;
            }

            playerMap = inputActions.FindActionMap("Player");
            if (playerMap == null)
            {
                Debug.LogError($"[PlayerController] '{inputActions.name}' icinde 'Player' action map'i yok.", this);
                return false;
            }

            moveAction = playerMap.FindAction("Move");
            lookAction = playerMap.FindAction("Look");

            if (moveAction == null || lookAction == null)
            {
                Debug.LogError("[PlayerController] 'Player' haritasinda 'Move' veya 'Look' action'i bulunamadi.", this);
                return false;
            }

            return true;
        }

        private void OnEnable()
        {
            // Tek tek action yerine tum haritayi aciyoruz: composite binding'ler (WASD) boylece garantiye alinir.
            playerMap?.Enable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleLook();
            HandleMovement();
        }

        private void HandleLook()
        {
            Vector2 lookInput = lookAction.ReadValue<Vector2>() * lookSensitivity;

            // Yatay donus govdeye, dikey donus sadece kameraya uygulanir.
            transform.Rotate(Vector3.up * lookInput.x);

            cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -pitchLimit, pitchLimit);
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            Vector3 horizontal = (transform.right * moveInput.x + transform.forward * moveInput.y) * walkSpeed;

            // Zemindeyken hafif asagi kuvvet uygula ki CharacterController zemine yapisik kalsin.
            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;
            else
                verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = horizontal + Vector3.up * verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }
    }
}
