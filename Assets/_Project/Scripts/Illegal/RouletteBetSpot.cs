using System.Collections.Generic;
using KanBagi.Interaction;
using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Illegal
{
    public enum RouletteBetKind
    {
        Number,
        Red,
        Black
    }

    /// <summary>
    /// Rulet masasindaki bir bahis alani (0-36 hucresi, KIRMIZI veya SIYAH). Bakip E: bu alana bir fis.
    /// Kendi parasini tutmaz; istegi masaya (GamblingTable) iletir, masa ne derse fisleri gosterir.
    /// Collider ile AYNI objede durur (PlayerInteractor isinin carptigi objeye bakar).
    /// </summary>
    public class RouletteBetSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private GamblingTable table;
        [SerializeField] private RouletteBetKind kind;
        [Tooltip("Sadece 'Number' turunde: 0-36.")]
        [SerializeField] private int number;

        [Header("Gorunum")]
        [SerializeField] private Color playerChipColor = new Color(0.95f, 0.8f, 0.2f);
        [SerializeField] private Color gamblerChipColor = new Color(0.25f, 0.45f, 0.9f);
        [SerializeField] private Vector3 chipSize = new Vector3(0.06f, 0.006f, 0.06f);
        [Tooltip("Kazanan alan bu kadar acilir (renk carpani).")]
        [SerializeField] private float highlightBoost = 2.2f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly List<GameObject> chips = new List<GameObject>();
        private Renderer spotRenderer;
        private Color baseColor = Color.white;

        public RouletteBetKind Kind => kind;
        public int Number => number;

        private void Awake()
        {
            if (table == null) table = GetComponentInParent<GamblingTable>();
            spotRenderer = GetComponent<Renderer>();

            if (spotRenderer != null && spotRenderer.sharedMaterial != null)
            {
                Material material = spotRenderer.sharedMaterial;
                baseColor = material.HasProperty(BaseColorId) ? material.GetColor(BaseColorId) : material.color;
            }
        }

        /// <summary>Top bu sayiya dustuyse bu alan kazanir mi?</summary>
        public bool Wins(int result)
        {
            switch (kind)
            {
                case RouletteBetKind.Number: return result == number;
                case RouletteBetKind.Red: return RouletteRules.IsRed(result);
                default: return RouletteRules.IsBlack(result);
            }
        }

        public string GetPrompt(PlayerContext player) => table != null ? table.GetBetPrompt(this) : string.Empty;

        public void Interact(PlayerContext player)
        {
            if (table != null) table.PlaceBet(this);
        }

        /// <summary>Alandaki fisleri yeniden dizer: once oyuncunun (sari), ustune kumarbazlarin (mavi).</summary>
        public void SetChips(int playerChips, int gamblerChips)
        {
            foreach (GameObject chip in chips)
            {
                if (chip != null) Destroy(chip);
            }

            chips.Clear();

            int total = playerChips + gamblerChips;
            for (int i = 0; i < total; i++)
                chips.Add(CreateChip(i, i < playerChips ? playerChipColor : gamblerChipColor));
        }

        public void SetHighlighted(bool highlighted)
        {
            if (spotRenderer == null) return;

            var block = new MaterialPropertyBlock();
            Color color = highlighted ? baseColor * highlightBoost : baseColor;
            color.a = 1f;
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            spotRenderer.SetPropertyBlock(block);
        }

        private GameObject CreateChip(int index, Color color)
        {
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chip.name = "Fis";
            Destroy(chip.GetComponent<Collider>()); // E isini fise degil alana carpsin

            // Alanin ustunde, kucuk bir kayma ile ust uste.
            Bounds bounds = spotRenderer != null ? spotRenderer.bounds : new Bounds(transform.position, Vector3.zero);
            float height = chipSize.y * 2f;
            Vector3 offset = new Vector3((index % 2) * 0.008f, height * (index + 0.5f), (index % 3) * 0.006f);
            chip.transform.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z) + offset;
            chip.transform.localScale = chipSize;
            chip.transform.SetParent(transform, true);

            var block = new MaterialPropertyBlock();
            block.SetColor(BaseColorId, color);
            block.SetColor(ColorId, color);
            chip.GetComponent<Renderer>().SetPropertyBlock(block);
            return chip;
        }
    }
}
