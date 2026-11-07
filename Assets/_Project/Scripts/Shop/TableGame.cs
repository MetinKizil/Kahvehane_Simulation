using KanBagi.Data;
using KanBagi.Furniture;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Masanin ustundeki oyun takimi (D8a): okey, iskambil, domino - masa basina en fazla bir tane.
    /// Takimin parcasini (masa prefab'indaki cocuk obje) acar/kapatir ve etkisini SADECE bu masaya verir:
    /// Customer oturdugu masanin sabir carpanini ve ikinci tur ihtimalini buradan sorar.
    /// Takim konup alinmasi Table'in etkilesiminden gelir; burasi sadece durumu tutar. Kayda girer (IFurnitureState).
    /// </summary>
    public class TableGame : MonoBehaviour, IFurnitureState
    {
        [Tooltip("Bu masaya konabilecek takimlar. Parcalar adlariyla (TableGameData.PieceName) cocuklarda bulunur.")]
        [SerializeField] private TableGameData[] games = new TableGameData[0];

        public TableGameData CurrentGame { get; private set; }

        public bool HasGame => CurrentGame != null;

        public float PatienceMultiplier => CurrentGame != null ? CurrentGame.PatienceMultiplier : 1f;

        public float SecondRoundChance => CurrentGame != null ? CurrentGame.SecondRoundChance : 0f;

        private void Awake() => RefreshPieces();

        /// <summary>Takimi koyar (null: kaldirir). Parcalar buna gore gorunur.</summary>
        public void SetGame(TableGameData game)
        {
            CurrentGame = game;
            RefreshPieces();
        }

        private void RefreshPieces()
        {
            foreach (TableGameData game in games)
            {
                if (game == null || string.IsNullOrEmpty(game.PieceName)) continue;

                Transform piece = transform.Find(game.PieceName);
                if (piece != null) piece.gameObject.SetActive(game == CurrentGame);
            }
        }

        public string CaptureFurnitureState() => CurrentGame != null ? CurrentGame.name : string.Empty;

        public void RestoreFurnitureState(string state)
        {
            TableGameData restored = null;
            foreach (TableGameData game in games)
            {
                if (game != null && game.name == state) restored = game;
            }

            SetGame(restored);
        }
    }
}
