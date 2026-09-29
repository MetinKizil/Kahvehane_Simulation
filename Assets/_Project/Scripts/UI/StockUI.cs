using System.Collections.Generic;
using System.Text;
using KanBagi.Data;
using KanBagi.Inventory;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Ekrandaki depo gostergesi. SADECE InventoryManager'in event'ini dinler ve yazar;
    /// icinde hicbir stok mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class StockUI : MonoBehaviour
    {
        [Tooltip("Bos birakilirsa bu objenin uzerindeki TMP_Text kullanilir.")]
        [SerializeField] private TMP_Text stockText;
        [Tooltip("Ekranda takip edilecek malzemeler (CayYapragi vb.).")]
        [SerializeField] private List<ItemData> trackedItems = new List<ItemData>();
        [Tooltip("{0} malzeme adi, {1} adet.")]
        [SerializeField] private string lineFormat = "{0}: {1}";

        private readonly StringBuilder builder = new StringBuilder();

        private void Start()
        {
            if (stockText == null) stockText = GetComponent<TMP_Text>();

            if (stockText == null)
            {
                Debug.LogError("[StockUI] 'Stock Text' atanmamis. Canvas altindaki TMP objesini bagla.", this);
                enabled = false;
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError("[StockUI] InventoryManager bulunamadi. 'Managers' objesinde olmali.", this);
                enabled = false;
                return;
            }

            InventoryManager.Instance.OnStockChanged += HandleStockChanged;
            Refresh();
        }

        private void HandleStockChanged(ItemData item, int newCount)
        {
            // Takip etmedigimiz bir malzeme degistiyse yaziyi bosuna kurmayalim.
            if (!trackedItems.Contains(item)) return;

            Refresh();
        }

        private void Refresh()
        {
            builder.Clear();

            foreach (ItemData item in trackedItems)
            {
                if (item == null) continue;

                if (builder.Length > 0) builder.AppendLine();
                builder.AppendFormat(lineFormat, item.ItemName, InventoryManager.Instance.GetCount(item));
            }

            stockText.text = builder.ToString();
        }

        private void OnDestroy()
        {
            if (InventoryManager.Instance != null)
                InventoryManager.Instance.OnStockChanged -= HandleStockChanged;
        }
    }
}
