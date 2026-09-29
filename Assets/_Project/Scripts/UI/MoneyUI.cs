using KanBagi.Economy;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Ekrandaki para gostergesi. SADECE EconomyManager'in event'ini dinler ve yazar;
    /// icinde hicbir oyun mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class MoneyUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text moneyText;

        private void Start()
        {
            // Start'ta abone oluyoruz ki EconomyManager'in Awake'i garantiyle bitmis olsun.
            EconomyManager.Instance.OnMoneyChanged += UpdateMoneyText;
            UpdateMoneyText(EconomyManager.Instance.CurrentMoney);
        }

        private void UpdateMoneyText(int currentMoney)
        {
            moneyText.text = $"{currentMoney} TL";
        }

        private void OnDestroy()
        {
            if (EconomyManager.Instance != null)
                EconomyManager.Instance.OnMoneyChanged -= UpdateMoneyText;
        }
    }
}
