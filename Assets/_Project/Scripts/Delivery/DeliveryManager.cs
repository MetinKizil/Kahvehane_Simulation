using System;
using System.Collections;
using KanBagi.Data;
using KanBagi.Items;
using UnityEngine;
using Random = UnityEngine.Random;

namespace KanBagi.Delivery
{
    /// <summary>
    /// Siparisleri kuryeye devreder: bekleme suresi sonunda teslimat alanina koli birakir.
    /// Malzemenin depoya nasil girecegini bilmez (onu StorageRack yapar) ve
    /// paradan haberi yoktur (onu PurchaseManager yapar) - SOLID.
    /// </summary>
    public class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager Instance { get; private set; }

        [Header("Koli")]
        [Tooltip("Teslimatta olusturulacak koli prefab'i (HoldableItem + DeliveryBox).")]
        [SerializeField] private GameObject boxPrefab;

        [Header("Teslimat Alani")]
        [Tooltip("Kolilerin birakilacagi nokta. Bos birakilirsa bu objenin konumu kullanilir.")]
        [SerializeField] private Transform deliveryPoint;
        [Tooltip("Koli bu yukseklikten birakilir ve fizikle yere duser.")]
        [SerializeField] private float dropHeight = 0.6f;
        [Tooltip("Koliler tam ust uste binmesin diye rastgele yatay sapma (metre).")]
        [SerializeField] private float scatterRadius = 0.35f;

        [Header("Zamanlama")]
        [Tooltip("Siparisin kapiya gelmesi icin gecen sure (saniye).")]
        [SerializeField] private float deliveryDelay = 20f;

        /// <summary>Yolda olan siparis sayisi.</summary>
        public int PendingOrders { get; private set; }

        /// <summary>Teslimat yapilabilir durumda mi? PurchaseManager para almadan once sorar.</summary>
        public bool IsReady => boxPrefab != null;

        /// <summary>Siparis kuyruga alindiginda tetiklenir (malzeme, adet).</summary>
        public event Action<ItemData, int> OnOrderQueued;

        /// <summary>Koli kapiya birakildiginda tetiklenir (malzeme, adet).</summary>
        public event Action<ItemData, int> OnDelivered;

        private Vector3 DropCenter => deliveryPoint != null ? deliveryPoint.position : transform.position;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            if (boxPrefab == null)
                Debug.LogError("[DeliveryManager] 'Box Prefab' atanmamis. Koli prefab'ini Inspector'a surukle.", this);
        }

        /// <summary>Siparisi kuyruga alir. Teslimat hazir degilse false doner (para dusulmemeli).</summary>
        public bool QueueOrder(ItemData item, int amount)
        {
            if (!IsReady || item == null || amount <= 0) return false;

            PendingOrders++;
            OnOrderQueued?.Invoke(item, amount);
            StartCoroutine(DeliverRoutine(item, amount));
            return true;
        }

        private IEnumerator DeliverRoutine(ItemData item, int amount)
        {
            yield return new WaitForSeconds(deliveryDelay);

            SpawnBox(item, amount);

            PendingOrders = Mathf.Max(0, PendingOrders - 1);
            OnDelivered?.Invoke(item, amount);
        }

        private void SpawnBox(ItemData item, int amount)
        {
            Vector2 scatter = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPosition = DropCenter + new Vector3(scatter.x, dropHeight, scatter.y);
            Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GameObject boxObject = Instantiate(boxPrefab, spawnPosition, spawnRotation);

            if (!boxObject.TryGetComponent(out DeliveryBox box))
            {
                Debug.LogError($"[DeliveryManager] '{boxPrefab.name}' prefab'inda DeliveryBox scripti yok.", this);
                Destroy(boxObject);
                return;
            }

            box.Fill(item, amount);
        }

        /// <summary>Sahne penceresinde kolilerin dusecegi alani gosterir.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(DropCenter, scatterRadius);
            Gizmos.DrawLine(DropCenter, DropCenter + Vector3.up * dropHeight);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
