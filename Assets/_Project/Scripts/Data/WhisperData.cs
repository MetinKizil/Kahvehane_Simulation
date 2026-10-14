using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>Itibar bir baraji ilk kez gecince fisildanan cumle.</summary>
    [Serializable]
    public class ReputationWhisper
    {
        [SerializeField] private int threshold;
        [SerializeField] private string message;

        public int Threshold => threshold;
        public string Message => message;
    }

    /// <summary>
    /// "Mahalle fisiltilari": oyundaki olaylara intikam tonunda eslik eden kisa cumleler.
    /// Sadece VERIDIR; hangi olayda gosterilecegine WhisperUI karar verir.
    /// Menu: Create > Kan Bagi > Whisper Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewWhispers", menuName = "Kan Bagi/Whisper Data")]
    public class WhisperData : ScriptableObject
    {
        [Header("Gun")]
        [Tooltip("Ilk gun dukkan acilinca.")]
        [SerializeField] [TextArea] private string firstMorning;

        [Header("Mafya")]
        [SerializeField] [TextArea] private string mafiaArrived;
        [SerializeField] [TextArea] private string mafiaPaid;
        [SerializeField] [TextArea] private string mafiaRefused;

        [Header("Emlak")]
        [Tooltip("{0} alinan mulkun adi.")]
        [SerializeField] [TextArea] private string propertyPurchasedFormat;

        [Header("Kumarhane")]
        [Tooltip("Bu upgrade alininca gamblingHallOpened gosterilir.")]
        [SerializeField] private UpgradeData gamblingHallUpgrade;
        [SerializeField] [TextArea] private string gamblingHallOpened;

        [Header("Itibar")]
        [SerializeField] private List<ReputationWhisper> reputationWhispers = new List<ReputationWhisper>();

        public string FirstMorning => firstMorning;
        public string MafiaArrived => mafiaArrived;
        public string MafiaPaid => mafiaPaid;
        public string MafiaRefused => mafiaRefused;
        public string PropertyPurchasedFormat => propertyPurchasedFormat;
        public UpgradeData GamblingHallUpgrade => gamblingHallUpgrade;
        public string GamblingHallOpened => gamblingHallOpened;
        public IReadOnlyList<ReputationWhisper> ReputationWhispers => reputationWhispers;
    }
}
