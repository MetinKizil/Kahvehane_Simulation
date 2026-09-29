using KanBagi.Customers;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Musterinin basindaki siparis balonu.
    /// SADECE Customer event'lerini dinler; siparis mantigi icermez
    /// (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class OrderBubbleUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [Tooltip("Bos birakilirsa ust objelerden Customer aranir.")]
        [SerializeField] private Customer customer;
        [Tooltip("Balonun tamami. Siparis beklenmiyorken kapatilir.")]
        [SerializeField] private GameObject bubbleRoot;
        [SerializeField] private TMP_Text orderText;

        [Header("Gorunum")]
        [Tooltip("Musteride siparis (ItemData) atanmamissa kullanilacak yedek metin.")]
        [SerializeField] private string orderMessage = "Cay";
        [Tooltip("Sabir tamken yazinin rengi.")]
        [SerializeField] private Color calmColor = Color.white;
        [Tooltip("Sabir tukenmek uzereyken yazinin rengi.")]
        [SerializeField] private Color impatientColor = Color.red;

        private void Start()
        {
            if (customer == null) customer = GetComponentInParent<Customer>();

            if (customer == null)
            {
                Debug.LogError("[OrderBubbleUI] Customer bulunamadi.", this);
                enabled = false;
                return;
            }

            customer.OnStateChanged += HandleStateChanged;
            customer.OnPatienceChanged += HandlePatienceChanged;

            HandleStateChanged(customer.State);
        }

        private void OnDestroy()
        {
            if (customer == null) return;

            customer.OnStateChanged -= HandleStateChanged;
            customer.OnPatienceChanged -= HandlePatienceChanged;
        }

        private void HandleStateChanged(CustomerState state)
        {
            bool isOrdering = state == CustomerState.WaitingForOrder;

            if (bubbleRoot != null) bubbleRoot.SetActive(isOrdering);
            if (isOrdering && orderText != null) orderText.text = GetOrderLabel();
        }

        /// <summary>Siparis adi ScriptableObject'ten okunur (ANAYASA: veri koda gomulmez).</summary>
        private string GetOrderLabel()
        {
            return customer.Order != null ? customer.Order.ItemName : orderMessage;
        }

        private void HandlePatienceChanged(float remainingRatio)
        {
            if (orderText == null) return;

            // Sabir azaldikca yazi kizarir: oyuncu acele etmesi gerektigini gorur.
            orderText.color = Color.Lerp(impatientColor, calmColor, remainingRatio);
        }
    }
}
