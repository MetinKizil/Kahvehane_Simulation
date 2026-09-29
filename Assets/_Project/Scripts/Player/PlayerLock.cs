using System;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Player
{
    /// <summary>
    /// Oyuncu kontrolunu tek noktadan acip kapatir (bilgisayar ekrani, ileride kumar masasi,
    /// polis diyalogu...). Kilitleyen sistemler PlayerController'i tek tek aramaz, buraya soyler.
    /// PlayerController zaten OnEnable/OnDisable'da imleci kilitleyip serbest biraktigi icin
    /// imlec yonetimi icin ekstra kod gerekmez.
    /// </summary>
    public class PlayerLock : MonoBehaviour
    {
        public static PlayerLock Instance { get; private set; }

        [Header("Kilitlenecek Bilesenler")]
        [Tooltip("Bos birakilirsa ayni objedeki PlayerController bulunur.")]
        [SerializeField] private PlayerController playerController;
        [Tooltip("Bos birakilirsa ayni objedeki PlayerInteractor bulunur.")]
        [SerializeField] private PlayerInteractor playerInteractor;

        public bool IsControlEnabled { get; private set; } = true;

        /// <summary>Kontrol acilip kapandiginda tetiklenir.</summary>
        public event Action<bool> OnControlEnabledChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerInteractor == null) playerInteractor = GetComponent<PlayerInteractor>();

            if (playerController == null || playerInteractor == null)
                Debug.LogError("[PlayerLock] PlayerController veya PlayerInteractor bulunamadi. Bu script 'Player' objesinde olmali.", this);
        }

        /// <summary>Hareketi, bakisi ve etkilesimi acar/kapatir.</summary>
        public void SetControlEnabled(bool isEnabled)
        {
            if (IsControlEnabled == isEnabled) return;

            IsControlEnabled = isEnabled;

            if (playerController != null) playerController.enabled = isEnabled;
            if (playerInteractor != null) playerInteractor.enabled = isEnabled;

            OnControlEnabledChanged?.Invoke(isEnabled);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
