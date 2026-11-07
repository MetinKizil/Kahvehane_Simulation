using KanBagi.Shop;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Masanin ustundeki oyun istegi balonu (D8b): grup bir oyun istediyse "Okey Takimi istiyor" yazar,
    /// takim masaya konunca veya grup kalkinca kaybolur. SADECE Table.OnGameRequestChanged'i dinler (ANAYASA: UI ayri).
    /// Masa prefab'inda durur (kok hep acik); acilip kapanan sey balonRoot. Kurulum: Kan Bagi > Oyun Istegi Balonunu Kur.
    /// </summary>
    public class GameRequestBubbleUI : MonoBehaviour
    {
        [SerializeField] private Table table;
        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private TMP_Text requestText;

        [Tooltip("{0} istenen oyunun adi.")]
        [SerializeField] private string requestFormat = "{0} istiyor";

        private void Start()
        {
            if (table == null) table = GetComponentInParent<Table>();
            if (table == null || bubbleRoot == null || requestText == null)
            {
                Debug.LogError($"[GameRequestBubbleUI] '{name}' icin masa veya balon baglantisi eksik.", this);
                enabled = false;
                return;
            }

            table.OnGameRequestChanged += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            bool hasRequest = table.RequestedGame != null;
            bubbleRoot.SetActive(hasRequest);
            if (hasRequest) requestText.text = string.Format(requestFormat, table.RequestedGame.GameName);
        }

        private void OnDestroy()
        {
            if (table != null) table.OnGameRequestChanged -= Refresh;
        }
    }
}
