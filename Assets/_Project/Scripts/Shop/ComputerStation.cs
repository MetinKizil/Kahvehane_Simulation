using System;
using KanBagi.Data;
using KanBagi.Furniture;
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
    public class ComputerStation : MonoBehaviour, IInteractable, IFurnitureMoveCheck
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

        /// <summary>Ekran acilip kapandiginda tetiklenir.</summary>
        public event Action<bool> OnScreenToggled;

        /// <summary>
        /// HERHANGI bir PC'nin ekrani acilip kapandiginda (PC, acik mi) ile tetiklenir. ComputerUI buna abone olur:
        /// D5'ten beri PC tasinabilir bir esya; oyun sirasinda olusturulur ve kayittan yeniden kurulur,
        /// bu yuzden UI tek bir sahne objesine baglanmaz.
        /// </summary>
        public static event Action<ComputerStation, bool> AnyScreenToggled;

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
            AnyScreenToggled?.Invoke(this, isOpen);
        }

        /// <summary>Ekran acikken (biri kullanirken) PC tasinamaz.</summary>
        public bool CanMove(out string reason)
        {
            reason = IsScreenOpen ? "Bilgisayar kullaniliyor" : null;
            return !IsScreenOpen;
        }

        /// <summary>Ekrani acik olan ilk PC (yoksa null). PC'den alinan esya, gun sonu kapatma vb. icin.</summary>
        public static ComputerStation FindOpen()
        {
            foreach (ComputerStation station in FindObjectsByType<ComputerStation>(FindObjectsSortMode.None))
            {
                if (station.IsScreenOpen) return station;
            }

            return null;
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
