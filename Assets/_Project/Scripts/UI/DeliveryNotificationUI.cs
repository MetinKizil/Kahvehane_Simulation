using System.Collections;
using KanBagi.Data;
using KanBagi.Delivery;
using TMPro;
using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Siparis ve teslimat bildirimi. SADECE DeliveryManager event'lerini dinler;
    /// icinde teslimat mantigi yoktur (ANAYASA: UI, oyun mantigindan ayri).
    /// </summary>
    public class DeliveryNotificationUI : MonoBehaviour
    {
        [Header("Baglantilar")]
        [Tooltip("Bos birakilirsa bu objenin uzerindeki TMP_Text kullanilir.")]
        [SerializeField] private TMP_Text messageText;

        [Header("Gorunum")]
        [Tooltip("Bildirimin ekranda kalma suresi (saniye).")]
        [SerializeField] private float showDuration = 4f;
        [Tooltip("{0} malzeme adi, {1} adet.")]
        [SerializeField] private string queuedFormat = "Siparis verildi: {0} x{1} - yolda";
        [SerializeField] private string deliveredFormat = "Siparisin kapida: {0} x{1}";

        private DeliveryManager deliveryManager;
        private Coroutine hideRoutine;

        private void Start()
        {
            if (messageText == null) messageText = GetComponent<TMP_Text>();

            if (messageText == null)
            {
                Debug.LogError("[DeliveryNotificationUI] 'Message Text' atanmamis. Canvas altindaki TMP objesini bagla.", this);
                enabled = false;
                return;
            }

            deliveryManager = DeliveryManager.Instance;

            if (deliveryManager == null)
            {
                Debug.LogError("[DeliveryNotificationUI] DeliveryManager bulunamadi. 'Managers' objesinde olmali.", this);
                enabled = false;
                return;
            }

            deliveryManager.OnOrderQueued += HandleOrderQueued;
            deliveryManager.OnDelivered += HandleDelivered;

            messageText.enabled = false;
        }

        private void HandleOrderQueued(ItemData item, int amount) => Show(queuedFormat, item, amount);

        private void HandleDelivered(ItemData item, int amount) => Show(deliveredFormat, item, amount);

        private void Show(string format, ItemData item, int amount)
        {
            if (item == null) return;

            messageText.text = string.Format(format, item.ItemName, amount);
            messageText.enabled = true;

            // Ust uste bildirim gelirse sure bastan baslar.
            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideRoutine());
        }

        private IEnumerator HideRoutine()
        {
            yield return new WaitForSeconds(showDuration);

            messageText.enabled = false;
            hideRoutine = null;
        }

        private void OnDestroy()
        {
            if (deliveryManager == null) return;

            deliveryManager.OnOrderQueued -= HandleOrderQueued;
            deliveryManager.OnDelivered -= HandleDelivered;
        }
    }
}
