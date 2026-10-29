using System;
using System.Collections;
using KanBagi.Interaction;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Suren bir is yapan istasyonlarin ortak temeli (cay ocagi, lavabo, kahve makinesi...).
    /// SADECE zamanlama ve ilerleme bildiriminden sorumludur; isin NE oldugunu bilmez (SOLID).
    /// Ilerlemeyi event ile yayinlar; UI bu event'e abone olur (Observer Pattern).
    /// </summary>
    public abstract class TimedStation : MonoBehaviour, IInteractable
    {
        [Header("Istasyon")]
        [Tooltip("Is surerken ekranda gorunecek metin.")]
        [SerializeField] private string workLabel = "Hazirlaniyor...";

        /// <summary>Istasyon su anda bir is yapiyor mu?</summary>
        public bool IsBusy { get; private set; }

        public string WorkLabel => workLabel;

        /// <summary>
        /// Is suresi carpani (1 = normal, 0.6 = %40 daha hizli).
        /// Upgrade etkileri kucultur; istasyon kimin hizlandirdigini bilmez (SOLID).
        /// </summary>
        public float DurationMultiplier { get; private set; } = 1f;

        /// <summary>Is ilerledikce (istasyon, 0-1 arasi oran) ile tetiklenir.</summary>
        public event Action<TimedStation, float> OnProgressChanged;

        /// <summary>Is bittiginde tetiklenir.</summary>
        public event Action<TimedStation> OnWorkFinished;

        /// <summary>
        /// HERHANGI bir istasyonun isi ilerleyince / bitince. Ilerleme cubugu buna abone olur:
        /// D5'ten beri istasyonlar tasinabilir esya, oyun sirasinda olusturulur; tek tek baglanmak yetmez.
        /// </summary>
        public static event Action<TimedStation, float> AnyProgressChanged;
        public static event Action<TimedStation> AnyWorkFinished;

        public abstract string GetPrompt(PlayerContext player);
        public abstract void Interact(PlayerContext player);

        /// <summary>Hiz upgrade'leri bunu cagirir. Cok kucuk/buyuk degerler kirpilir.</summary>
        public void SetDurationMultiplier(float multiplier)
        {
            DurationMultiplier = Mathf.Clamp(multiplier, 0.05f, 10f);
        }

        private void ReportProgress(float progress)
        {
            OnProgressChanged?.Invoke(this, progress);
            AnyProgressChanged?.Invoke(this, progress);
        }

        /// <summary>Verilen sure boyunca isi yurutur, bitince onComplete'i cagirir.</summary>
        protected void StartWork(float duration, Action onComplete)
        {
            if (IsBusy) return;

            StartCoroutine(WorkRoutine(duration * DurationMultiplier, onComplete));
        }

        private IEnumerator WorkRoutine(float duration, Action onComplete)
        {
            IsBusy = true;
            ReportProgress(0f);

            // Sure sifir/negatif ise anlik bitir; bolume girip hata almayalim.
            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    ReportProgress(Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
            }

            IsBusy = false;
            onComplete?.Invoke();
            OnWorkFinished?.Invoke(this);
            AnyWorkFinished?.Invoke(this);
        }
    }
}
