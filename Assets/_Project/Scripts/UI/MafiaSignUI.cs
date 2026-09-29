using KanBagi.Illegal;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Tahsildarin ustundeki tabela: ne istendigi yazar.
    /// SADECE MafiaManager'dan okur; odeme mantigi icermez
    /// (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class MafiaSignUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text amountText;

        [Header("Gorunum")]
        [SerializeField] private string title = "HARAC";
        [Tooltip("{0} istenen tutar.")]
        [SerializeField] private string amountFormat = "{0} TL";
        [SerializeField] private string idleAmount = "-";

        private MafiaManager mafia;

        private void Start()
        {
            mafia = MafiaManager.Instance;

            if (titleText != null) titleText.text = title;

            if (mafia != null) mafia.OnDemandArrived += HandleDemandArrived;

            Refresh();
        }

        private void HandleDemandArrived(int amount) => Refresh();

        private void Refresh()
        {
            if (amountText == null) return;

            amountText.text = mafia != null && mafia.IsDemandActive
                ? string.Format(amountFormat, mafia.CurrentDemand)
                : idleAmount;
        }

        private void OnDestroy()
        {
            if (mafia != null) mafia.OnDemandArrived -= HandleDemandArrived;
        }
    }
}
