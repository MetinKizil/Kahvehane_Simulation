using System;
using KanBagi.Core;
using KanBagi.Economy;
using KanBagi.Interaction;
using KanBagi.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KanBagi.Illegal
{
    /// <summary>Bir kumar elinin sonucu. Hem tabela hem gunluk ozet bunu okur.</summary>
    public readonly struct GamblingRoundResult
    {
        public readonly bool Won;
        public readonly int Stake;
        public readonly int Payout;

        public GamblingRoundResult(bool won, int stake, int payout)
        {
            Won = won;
            Stake = stake;
            Payout = payout;
        }

        public int Net => Payout - Stake;
    }

    /// <summary>
    /// Arka odadaki gizli kumar masasi. SADECE dukkan KAPALIYKEN (gece) oynanir.
    /// Kendi kurallarini bilir; polisi, mafyayi veya raporu bilmez (SOLID).
    /// Her el suphe biriktirir - 8c'de polis bunu okuyacak.
    /// </summary>
    public class GamblingTable : MonoBehaviour, IInteractable
    {
        [Header("Bahis")]
        [SerializeField] private int betAmount = 100;
        [Tooltip("Kazanma olasiligi (0-1). 0.45 = %45.")]
        [SerializeField] [Range(0f, 1f)] private float winChance = 0.45f;
        [Tooltip("Kazaninca bahsin kac katini geri alir. 2 = bahis + ayni kadar kar.")]
        [SerializeField] private float payoutMultiplier = 2f;

        [Header("Supheli Is")]
        [Tooltip("Her el sonunda suphe bu kadar artar.")]
        [SerializeField] private int suspicionPerRound = 3;

        [Header("Gizleme")]
        [Tooltip("Masa ortuluyken acilan obje (ortu/brandu). Polis gelince bunu kapatmis olmalisin.")]
        [SerializeField] private GameObject coverVisual;
        [Tooltip("Masa ortuluyken gizlenecek objeler (tabela, cuha vb.).")]
        [SerializeField] private GameObject[] exposedVisuals;

        [Header("Ipucu Metinleri")]
        [Tooltip("{0} bahis tutari.")]
        [SerializeField] private string playFormat = "Kumar Oyna ({0} TL)";
        [Tooltip("{0} bahis tutari.")]
        [SerializeField] private string noMoneyFormat = "{0} TL gerekli";
        [SerializeField] private string hideMessage = "Masayi Gizle";
        [SerializeField] private string revealMessage = "Ortuyu Ac";

        public int BetAmount => betAmount;

        /// <summary>Bu gecenin toplam kar/zarari. Her gece basinda sifirlanir.</summary>
        public int NightNet { get; private set; }

        /// <summary>Bir el oynandiginda tetiklenir. Tabela ve gunluk ozet abone olur.</summary>
        public event Action<GamblingRoundResult> OnRoundPlayed;

        /// <summary>Kumar su anda oynanabilir mi? (Gece olmali.)</summary>
        public bool IsOpen => DayCycleManager.Instance == null || !DayCycleManager.Instance.IsShopOpen;

        /// <summary>
        /// Masa ortulu mu? Polis baskininda ortusuz yakalanirsan bedeli agir olur.
        /// Guvenli tarafta kalmak icin ORTULU basliyor.
        /// </summary>
        public bool IsHidden { get; private set; } = true;

        /// <summary>Ortu durumu degisince tetiklenir.</summary>
        public event Action<bool> OnHiddenChanged;

        private void Start()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed += HandleShopClosed;

            RefreshVisuals();
        }

        /// <summary>Ortuyu acar/kapatir. Polis gelmeden once kapatmak oyuncunun isi.</summary>
        public void SetHidden(bool hidden)
        {
            if (IsHidden == hidden) return;

            IsHidden = hidden;
            RefreshVisuals();
            OnHiddenChanged?.Invoke(IsHidden);
        }

        private void RefreshVisuals()
        {
            if (coverVisual != null) coverVisual.SetActive(IsHidden);

            if (exposedVisuals == null) return;

            foreach (GameObject exposed in exposedVisuals)
            {
                if (exposed != null) exposed.SetActive(!IsHidden);
            }
        }

        public string GetPrompt(PlayerContext player)
        {
            // Gunduz kumar oynanmaz; o yuzden E tusu gunduz ORTUYU yonetir.
            if (!IsOpen) return IsHidden ? revealMessage : hideMessage;

            if (IsHidden) return revealMessage;

            int money = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney : 0;
            if (money < betAmount) return string.Format(noMoneyFormat, betAmount);

            return string.Format(playFormat, betAmount);
        }

        public void Interact(PlayerContext player)
        {
            // Gunduz: sadece ortuyu ac/kapat. Gece: ortu kapaliysa once ac, sonra oyna.
            if (!IsOpen || IsHidden)
            {
                SetHidden(!IsHidden);
                return;
            }

            if (EconomyManager.Instance == null) return;
            if (!EconomyManager.Instance.TrySpendMoney(betAmount)) return;

            PlayRound();
        }

        private void PlayRound()
        {
            bool won = Random.value < winChance;
            int payout = won ? Mathf.RoundToInt(betAmount * payoutMultiplier) : 0;

            if (payout > 0) EconomyManager.Instance.AddMoney(payout);

            var result = new GamblingRoundResult(won, betAmount, payout);
            NightNet += result.Net;

            // Her el mahalledeki supheyi biraz daha artirir.
            if (SuspicionManager.Instance != null) SuspicionManager.Instance.Add(suspicionPerRound);

            OnRoundPlayed?.Invoke(result);
        }

        /// <summary>Dukkan kapandi: yeni gece basliyor, gecelik hesap sifirlanir.</summary>
        private void HandleShopClosed(int day) => NightNet = 0;

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed -= HandleShopClosed;
        }
    }
}
