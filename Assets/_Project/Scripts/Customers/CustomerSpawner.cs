using System;
using System.Collections;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Furniture;
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
    /// <summary>Mahalledeki bir sokak girisi (10d-1a). Tercih eden profil varsa o profil buradan gelir.</summary>
    [Serializable]
    public class StreetEntry
    {
        public Transform point;
        [Tooltip("Bos = herkes. Dolu = sadece bu profiller (ogrenci okul tarafindan gelir).")]
        public List<CustomerProfile> preferredProfiles = new List<CustomerProfile>();
    }

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

        [Tooltip("Mahalledeki sokak girisleri (10d-1a). Grup bunlardan birinden yurur gelir, ayni yerden gider. " +
                 "Bos birakilirsa giris noktasi kullanilir. Kurulum: Kan Bagi > Mahalleyi Kur.")]
        [SerializeField] private List<StreetEntry> streetEntries = new List<StreetEntry>();

        [Header("Zamanlama")]
        [SerializeField] private float firstSpawnDelay = 3f;
        [Tooltip("Iki musteri arasindaki bekleme araligi (min - max saniye).")]
        [SerializeField] private Vector2 spawnDelayRange = new Vector2(6f, 14f);

        [Header("Limit")]
        [Tooltip("Ayni anda dukkanda bulunabilecek en fazla musteri (kisi). D7b'den beri asil sinir masalar " +
                 "(grup ancak sigacagi bos masa varsa gelir); bu sadece performans emniyeti (NavMesh ajani sayisi).")]
        [SerializeField] private int maxActiveCustomers = 60;

        [Header("Grup")]
        [Tooltip("Grup uyeleri kapida yan yana bu aralikla dogar (metre).")]
        [SerializeField] private float groupSpacing = 0.6f;

        [Header("Oyun istegi (D8b)")]
        [Tooltip("ESYALAR'daki oyun takimlari. Grup bunlardan, buyuklugune uyan ve itibari acilmis olani isteyebilir.")]
        [SerializeField] private List<FurnitureData> gameSetItems = new List<FurnitureData>();
        [Tooltip("2+ kisilik grubun oyun isteme ihtimali (profilin oyun merakiyla carpilir).")]
        [SerializeField] [Range(0f, 1f)] private float gameRequestChance = 0.5f;
        [Tooltip("Istenen oyun gelince grubun bir kez odedigi masa parasi (TL).")]
        [SerializeField] private int tablePayment = 20;
        [Tooltip("Istek karsilaninca kisi basi itibar; getirilmeyip siparis de gelmezse kisi basi ceza.")]
        [SerializeField] private int gameReputationPerPerson = 1;

        private readonly List<Customer> activeCustomers = new List<Customer>();

        /// <summary>Su anda dukkandaki musteri sayisi.</summary>
        public int ActiveCustomers => activeCustomers.Count;

        /// <summary>Yeni musteri uretildiginde tetiklenir. Gunluk istatistikler buna abone olacak.</summary>
        public event Action<Customer> OnCustomerSpawned;

        private Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

        /// <summary>Dukkanin giris noktasi. Prefab olarak gelen kumar masasinin kumarbaz cagiricisi kapiyi buradan bulur (D6d-2).</summary>
        public Transform EntryPoint => spawnPoint != null ? spawnPoint : transform;
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
                if (CanSpawn()) SpawnGroup();

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

        /// <summary>
        /// Bir grup gonderir (D7b): profil secilir, grup buyuklugu profilden (1-4). O buyuklukte bos masa yoksa
        /// grup hic gelmez (siradaki grup denenir). Grubun sandalyeleri burada birden ayrilir.
        /// </summary>
        private void SpawnGroup()
        {
            CustomerProfile profile = PickProfile();
            int size = profile != null ? profile.PickGroupSize() : 1;
            size = Mathf.Min(size, maxActiveCustomers - activeCustomers.Count);
            if (size <= 0 || !TableManager.Instance.HasTableFor(size)) return;

            Transform street = PickStreetEntry(profile);
            Vector3 origin = street != null ? street.position : SpawnPosition;
            Quaternion facing = street != null ? street.rotation : SpawnRotation;
            Vector3 exit = street != null ? street.position : ExitPosition;

            var members = new List<Customer>();
            Vector3 side = facing * Vector3.right;
            for (int i = 0; i < size; i++)
            {
                Vector3 position = origin + side * ((i - (size - 1) * 0.5f) * groupSpacing);
                GameObject customerObject = Instantiate(customerPrefab, position, facing);

                if (!customerObject.TryGetComponent(out Customer customer))
                {
                    Debug.LogError($"[CustomerSpawner] '{customerPrefab.name}' prefab'inda Customer scripti yok.", this);
                    Destroy(customerObject);
                    DestroyAll(members);
                    return;
                }

                members.Add(customer);
            }

            if (!TableManager.Instance.TryReserveTableForGroup(members, out Table table, out int[] seats))
            {
                DestroyAll(members);
                return;
            }

            // Once herkes gruba girer, sonra baslarlar: grup "kac kisi bitirmeli" bilgisini eksiksiz bilsin.
            var group = new CustomerGroup();
            foreach (Customer member in members) group.Add(member);
            group.SetGameRequest(table, PickGameRequest(profile, size), tablePayment, gameReputationPerPerson);

            for (int i = 0; i < members.Count; i++)
            {
                Customer member = members[i];
                activeCustomers.Add(member);
                member.OnLeft += HandleCustomerLeft;
                member.Initialize(exit, profile, table, seats[i], group);

                OnCustomerSpawned?.Invoke(member);
            }
        }

        /// <summary>
        /// Grup oyun ister mi, isterse hangisini? Tek kisi istemez; oyun grubun buyuklugune uymali (okey sadece 4)
        /// ve itibari acilmis olmali (henuz alinamayan oyun istenmez). Uygun yoksa null.
        /// </summary>
        private TableGameData PickGameRequest(CustomerProfile profile, int size)
        {
            if (size < 2 || gameSetItems.Count == 0) return null;

            float chance = gameRequestChance * (profile != null ? profile.GameCuriosity : 1f);
            if (UnityEngine.Random.value >= chance) return null;

            var candidates = new List<TableGameData>();
            foreach (FurnitureData item in gameSetItems)
            {
                TableGameData game = item != null ? item.TableGame : null;
                if (game == null || size < game.MinRequestGroup || size > game.MaxRequestGroup) continue;
                if (FurnitureManager.Instance != null && !FurnitureManager.Instance.MeetsReputation(item)) continue;

                candidates.Add(game);
            }

            return candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
        }

        /// <summary>
        /// Profili tercih eden giris varsa ondan (ogrenci okul tarafindan), yoksa kimseye ozel olmayan girislerden
        /// rastgele biri. Hic giris yoksa null (dukkan kapisindaki eski nokta kullanilir).
        /// </summary>
        private Transform PickStreetEntry(CustomerProfile profile)
        {
            var preferred = new List<Transform>();
            var open = new List<Transform>();
            foreach (StreetEntry entry in streetEntries)
            {
                if (entry == null || entry.point == null) continue;

                if (entry.preferredProfiles.Count == 0) open.Add(entry.point);
                else if (profile != null && entry.preferredProfiles.Contains(profile)) preferred.Add(entry.point);
            }

            List<Transform> pool = preferred.Count > 0 ? preferred : open;
            return pool.Count > 0 ? pool[UnityEngine.Random.Range(0, pool.Count)] : null;
        }

        private static void DestroyAll(List<Customer> members)
        {
            foreach (Customer member in members)
            {
                if (member != null) Destroy(member.gameObject);
            }
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
