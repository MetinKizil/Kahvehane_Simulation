using System;
using System.Collections;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Shop;
using UnityEngine;

namespace KanBagi.Customers
{
    /// <summary>
    /// Mahalleden dukkana musteri gonderir ve dukkandakilerin kaydini tutar.
    /// Sadece "ne zaman ve kac musteri" sorusundan sorumludur;
    /// musterinin ne yaptigini bilmez (SOLID).
    /// Son musteri saatinden sonra gondermeyi keser, kapanista icerideki musterileri cikarir.
    /// </summary>
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Musteri")]
        [SerializeField] private GameObject customerPrefab;
        [Tooltip("Gelebilecek musteri tipleri. Itibari yetenler arasindan agirlikli secilir. " +
                 "Bos birakilirsa herkes prefab'daki varsayilan musteri olur.")]
        [SerializeField] private List<CustomerProfile> profiles = new List<CustomerProfile>();

        [Header("Noktalar")]
        [Tooltip("Musterinin dukkana girdigi nokta. Bos birakilirsa spawner'in kendi konumu.")]
        [SerializeField] private Transform spawnPoint;
        [Tooltip("Musterinin ciktigi nokta. Bos birakilirsa giris noktasi kullanilir.")]
        [SerializeField] private Transform exitPoint;

        [Header("Zamanlama")]
        [SerializeField] private float firstSpawnDelay = 3f;
        [Tooltip("Iki musteri arasindaki bekleme araligi (min - max saniye).")]
        [SerializeField] private Vector2 spawnDelayRange = new Vector2(6f, 14f);

        [Header("Limit")]
        [Tooltip("Ayni anda dukkanda bulunabilecek en fazla musteri sayisi.")]
        [SerializeField] private int maxActiveCustomers = 4;

        private readonly List<Customer> activeCustomers = new List<Customer>();

        /// <summary>Su anda dukkandaki musteri sayisi.</summary>
        public int ActiveCustomers => activeCustomers.Count;

        /// <summary>Yeni musteri uretildiginde tetiklenir. Gunluk istatistikler buna abone olacak.</summary>
        public event Action<Customer> OnCustomerSpawned;

        private Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        private Quaternion SpawnRotation => spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        private Vector3 ExitPosition => exitPoint != null ? exitPoint.position : SpawnPosition;

        private void Start()
        {
            if (customerPrefab == null)
            {
                Debug.LogError("[CustomerSpawner] 'Customer Prefab' atanmamis.", this);
                enabled = false;
                return;
            }

            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed += HandleShopClosed;

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            yield return new WaitForSeconds(firstSpawnDelay);

            while (true)
            {
                if (CanSpawn()) SpawnCustomer();

                yield return new WaitForSeconds(UnityEngine.Random.Range(spawnDelayRange.x, spawnDelayRange.y));
            }
        }

        /// <summary>Son musteri saati gectiyse, dukkan doluysa veya bos masa yoksa musteri gonderilmez.</summary>
        private bool CanSpawn()
        {
            if (DayCycleManager.Instance != null && !DayCycleManager.Instance.AcceptsNewCustomers) return false;
            if (activeCustomers.Count >= maxActiveCustomers) return false;

            return TableManager.Instance != null && TableManager.Instance.HasFreeTable();
        }

        private void SpawnCustomer()
        {
            GameObject customerObject = Instantiate(customerPrefab, SpawnPosition, SpawnRotation);

            if (!customerObject.TryGetComponent(out Customer customer))
            {
                Debug.LogError($"[CustomerSpawner] '{customerPrefab.name}' prefab'inda Customer scripti yok.", this);
                Destroy(customerObject);
                return;
            }

            activeCustomers.Add(customer);
            customer.OnLeft += HandleCustomerLeft;
            customer.Initialize(ExitPosition, PickProfile());

            OnCustomerSpawned?.Invoke(customer);
        }

        /// <summary>Itibari yeten profiller arasindan agirlikli secim. Uygun profil yoksa null (varsayilan musteri).</summary>
        private CustomerProfile PickProfile()
        {
            int reputation = ReputationManager.Instance != null ? ReputationManager.Instance.CurrentReputation : 0;

            int totalWeight = 0;
            foreach (CustomerProfile profile in profiles)
            {
                if (IsEligible(profile, reputation)) totalWeight += profile.SpawnWeight;
            }

            if (totalWeight == 0) return null;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            foreach (CustomerProfile profile in profiles)
            {
                if (!IsEligible(profile, reputation)) continue;

                roll -= profile.SpawnWeight;
                if (roll < 0) return profile;
            }

            return null;
        }

        private static bool IsEligible(CustomerProfile profile, int reputation) =>
            profile != null && profile.SpawnWeight > 0 && reputation >= profile.RequiredReputation;

        private void HandleCustomerLeft(Customer customer)
        {
            customer.OnLeft -= HandleCustomerLeft;
            activeCustomers.Remove(customer);
        }

        /// <summary>Kapanis saati: icerideki herkes kalkip cikar.</summary>
        private void HandleShopClosed(int day)
        {
            // Kopya uzerinde donuyoruz: cikan musteri listeden kendini siler.
            foreach (Customer customer in activeCustomers.ToArray())
            {
                if (customer == null) continue;

                customer.LeaveNow();
            }
        }

        private void OnDestroy()
        {
            if (DayCycleManager.Instance != null)
                DayCycleManager.Instance.OnShopClosed -= HandleShopClosed;
        }
    }
}
