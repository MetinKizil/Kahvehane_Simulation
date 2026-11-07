using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>
    /// Masaya konan bir oyun takimi (okey, iskambil, domino - D8a). Sadece VERIDIR:
    /// etkisi SADECE takimin durdugu masadaki musterilere gelir (TableGame okur, Customer sorar).
    /// Kayitta masa durumu bu asset'in ADIYLA saklanir; asset adini degistirmek eski kayitlari bozar.
    /// Menu: Create > Kan Bagi > Table Game Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewTableGame", menuName = "Kan Bagi/Table Game Data")]
    public class TableGameData : ScriptableObject
    {
        [Header("Kimlik")]
        [SerializeField] private string gameName;
        [Tooltip("Masa prefab'indaki parcanin (cocuk obje) adi: OkeyTakimi, IskambilDestesi, DominoTakimi.")]
        [SerializeField] private string pieceName;
        [Tooltip("Elde tasinan kutu (HoldableItem + GameSetItem). Masadan alininca bu olusur.")]
        [SerializeField] private GameObject boxPrefab;

        [Header("Masadaki etki")]
        [Tooltip("Bu masadaki musterilerin sabir carpani (1 = etkisiz).")]
        [SerializeField] private float patienceMultiplier = 1f;
        [Tooltip("Bu masadaki musterilerin ictikten sonra bir tur daha isteme ihtimali (0-1, profilin oyun merakiyla carpilir).")]
        [SerializeField] [Range(0f, 1f)] private float secondRoundChance;

        [Header("Grup istegi (D8b)")]
        [Tooltip("Bu oyunu en az kac kisilik grup ister (okey 4, iskambil/domino 2).")]
        [SerializeField] private int minRequestGroup = 2;
        [Tooltip("Bu oyunu en fazla kac kisilik grup ister.")]
        [SerializeField] private int maxRequestGroup = 4;
        [Tooltip("Istek karsilaninca grup bu kadar kisiyse (okeyde 4: 'dordu tamam') ikinci tur ihtimaline eklenir. 0 = yok.")]
        [SerializeField] private int fullGroupSize;
        [SerializeField] [Range(0f, 1f)] private float fullGroupSecondRoundBonus;

        public string GameName => gameName;
        public int MinRequestGroup => minRequestGroup;
        public int MaxRequestGroup => maxRequestGroup;
        public int FullGroupSize => fullGroupSize;
        public float FullGroupSecondRoundBonus => fullGroupSecondRoundBonus;
        public string PieceName => pieceName;
        public GameObject BoxPrefab => boxPrefab;
        public float PatienceMultiplier => patienceMultiplier;
        public float SecondRoundChance => secondRoundChance;
    }
}
