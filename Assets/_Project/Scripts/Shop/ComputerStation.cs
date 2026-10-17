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

        /// <summary>Ekrani acan oyuncunun kilidi. Kapaninca yine o acilir; diger oyuncu yurumeye devam eder.</summary>
        private PlayerLock screenUser;

        public ShopCatalog Catalog => catalog;
        public bool IsScreenOpen { get; private set; }

        /// <summary>Ekrani su an kullanan oyuncunun kilidi (ekran kapaliysa null). PC'den alinan esya ona verilir.</summary>
        public PlayerLock ScreenUser => screenUser;

        /// <summary>Ekran acilip kapandiginda tetiklenir. ComputerUI buna abone olur.</summary>
        public event Action<bool> OnScreenToggled;

        public string GetPrompt(PlayerContext player) => catalog != null ? "Bilgisayari Kullan" : "Bilgisayar ayarlanmamis";

        private void Awake()
        {
            if (catalog == null)
                Debug.LogError("[ComputerStation] 'Catalog' atanmamis. ShopCatalog asset'ini Inspector'a surukle.", this);

            // ESC ile kapatma: UI haritasindaki Cancel action'i. Yoksa sadece Kapat butonu calisir.
            InputActionAsset asset = inputActions != null ? inputActions : InputSystem.actions;
            cancelAction = asset != null ? asset.FindActionMap("UI")?.FindAction("Cancel") : null;
        }

        public void Interact(PlayerContext player)
        {
            // Ekran biri tarafindan aciksa ikinci oyuncu devralamaz.
            if (catalog == null || IsScreenOpen) return;

            screenUser = player.Lock;
            SetScreen(true);
        }

        /// <summary>Kapat butonu ve ESC bunu cagirir.</summary>
        public void CloseScreen() => SetScreen(false);

        private void SetScreen(bool isOpen)
        {
            if (IsScreenOpen == isOpen) return;

            IsScreenOpen = isOpen;

            // Ekran acikken ONU ACAN oyuncu yuruyemez, bakamaz ve etkilesemez; imlec serbest kalir.
            if (screenUser != null) screenUser.SetControlEnabled(!isOpen);
            if (!isOpen) screenUser = null;

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
