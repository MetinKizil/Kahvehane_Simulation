using KanBagi.Economy;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Harac toplamaya gelen adam. Harac aktifken MafiaManager tarafindan sahneye konur.
    /// Kendi basina karar vermez; odemeyi MafiaManager yapar (SOLID).
    ///
    /// ONEMLI: Collider ile ayni GameObject'te olmali (PlayerInteractor tek isin atar).
    /// </summary>
    public class MafiaCollector : MonoBehaviour, IInteractable
    {
        [Header("Ipucu Metinleri")]
        [Tooltip("{0} istenen harac.")]
        [SerializeField] private string payFormat = "Haraci Ode ({0} TL)";
        [Tooltip("{0} istenen harac.")]
        [SerializeField] private string noMoneyFormat = "{0} TL gerekli";
        [SerializeField] private string idlePrompt = "Adam sana bakiyor";

        public string InteractionPrompt
        {
            get
            {
                MafiaManager mafia = MafiaManager.Instance;
                if (mafia == null || !mafia.IsDemandActive) return idlePrompt;

                int money = EconomyManager.Instance != null ? EconomyManager.Instance.CurrentMoney : 0;
                if (money < mafia.CurrentDemand) return string.Format(noMoneyFormat, mafia.CurrentDemand);

                return string.Format(payFormat, mafia.CurrentDemand);
            }
        }

        public void Interact()
        {
            if (MafiaManager.Instance == null) return;

            MafiaManager.Instance.PayDemand();
        }
    }
}
