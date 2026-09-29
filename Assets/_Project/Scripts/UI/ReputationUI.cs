using KanBagi.Core;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Ekrandaki itibar gostergesi. SADECE ReputationManager'in event'ini dinler ve yazar;
    /// icinde hicbir oyun mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class ReputationUI : MonoBehaviour
    {
        [Tooltip("Bos birakilirsa bu objenin uzerindeki TMP_Text kullanilir.")]
        [SerializeField] private TMP_Text reputationText;
        [Tooltip("{0} yerine guncel itibar yazilir.")]
        [SerializeField] private string labelFormat = "Itibar: {0}";

        private void Start()
        {
            if (reputationText == null) reputationText = GetComponent<TMP_Text>();

            if (reputationText == null)
            {
                Debug.LogError("[ReputationUI] 'Reputation Text' atanmamis. Canvas altindaki TMP objesini bagla.", this);
                enabled = false;
                return;
            }

            if (ReputationManager.Instance == null)
            {
                Debug.LogError("[ReputationUI] ReputationManager bulunamadi. 'Managers' objesinde olmali.", this);
                enabled = false;
                return;
            }

            // Start'ta abone oluyoruz ki ReputationManager'in Awake'i garantiyle bitmis olsun.
            ReputationManager.Instance.OnReputationChanged += UpdateReputationText;
            UpdateReputationText(ReputationManager.Instance.CurrentReputation);
        }

        private void UpdateReputationText(int currentReputation)
        {
            reputationText.text = string.Format(labelFormat, currentReputation);
        }

        private void OnDestroy()
        {
            if (ReputationManager.Instance != null)
                ReputationManager.Instance.OnReputationChanged -= UpdateReputationText;
        }
    }
}
