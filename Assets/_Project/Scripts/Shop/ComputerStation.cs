using System;
using KanBagi.Data;
using KanBagi.Interaction;
using KanBagi.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KanBagi.Shop
{
    /// <summary>
    /// Yonetim bilgisayari. Sadece ekrani ACIP KAPAMAKTAN ve oyuncuyu kilitlemekten sorumludur;
    /// ne satin alir ne de arayuz cizer (SOLID). Ekran durumunu event ile yayinlar,
    /// ComputerUI buna abone olur (Observer Pattern).
    /// </summary>
    public class ComputerStation : MonoBehaviour, IInteractable
    {
        [Header("Katalog")]
        [Tooltip("Bu bilgisayardan siparis edilebilecek malzemeler.")]
        [SerializeField] private ShopCatalog catalog;

        [Header("Input")]
        [Tooltip("Bos birakilirsa proje genelindeki (Project-wide) Input Actions asset'i kullanilir.")]
        [SerializeField] private InputActionAsset inputActions;

        private InputAction cancelAction;

        public ShopCatalog Catalog => catalog;
        public bool IsScreenOpen { get; private set; }

        /// <summary>Ekran acilip kapandiginda tetiklenir. ComputerUI buna abone olur.</summary>
        public event Action<bool> OnScreenToggled;

        public string InteractionPrompt => catalog != null ? "Bilgisayari Kullan" : "Bilgisayar ayarlanmamis";

        private void Awake()
        {
            if (catalog == null)
                Debug.LogError("[ComputerStation] 'Catalog' atanmamis. ShopCatalog asset'ini Inspector'a surukle.", this);

            // ESC ile kapatma: UI haritasindaki Cancel action'i. Yoksa sadece Kapat butonu calisir.
            InputActionAsset asset = inputActions != null ? inputActions : InputSystem.actions;
            cancelAction = asset != null ? asset.FindActionMap("UI")?.FindAction("Cancel") : null;
        }

        public void Interact()
        {
            if (catalog == null) return;

            SetScreen(true);
        }

        /// <summary>Kapat butonu ve ESC bunu cagirir.</summary>
        public void CloseScreen() => SetScreen(false);

        private void SetScreen(bool isOpen)
        {
            if (IsScreenOpen == isOpen) return;

            IsScreenOpen = isOpen;

            // Ekran acikken oyuncu yuruyemez, bakamaz ve etkilesemez; imlec serbest kalir.
            if (PlayerLock.Instance != null) PlayerLock.Instance.SetControlEnabled(!isOpen);

            SetCancelListening(isOpen);
            OnScreenToggled?.Invoke(isOpen);
        }

        private void SetCancelListening(bool isListening)
        {
            if (cancelAction == null) return;

            if (isListening)
            {
                cancelAction.Enable();
                cancelAction.performed += HandleCancel;
            }
            else
            {
                cancelAction.performed -= HandleCancel;
            }
        }

        private void HandleCancel(InputAction.CallbackContext context) => CloseScreen();

        private void OnDisable()
        {
            // Sahne kapanirken abonelik asili kalmasin.
            if (IsScreenOpen) SetScreen(false);
        }
    }
}
