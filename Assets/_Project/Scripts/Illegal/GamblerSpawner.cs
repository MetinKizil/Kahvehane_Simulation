using System.Collections;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Customers;
using KanBagi.Furniture;
using UnityEngine;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Gece kumar masasina kumarbaz gonderir ve icerdekilerin kaydini tutar.
    /// Rulet masasinin (prefab, D6d-2) UZERINDE durur; masa ancak Kumar Odasi alininca bodruma konabildigi icin
    /// kumarhane acilmadan hic kumarbaz gelmez. Masa hayaletken kapali kalir (IPlacementPausable).
    /// Sadece "ne zaman ve kac kisi" sorusundan sorumludur (SOLID).
    ///
    /// Kurulum: Kan Bagi > Kumarhaneyi Kur (prefab ve giris noktasini kendisi baglar).
    /// </summary>
    [RequireComponent(typeof(GamblingTable))]
    public class GamblerSpawner : MonoBehaviour, IPlacementPausable
    {
        [Header("Kumarbaz")]
        [SerializeField] private GameObject gamblerPrefab;
        [Tooltip("Kumarbazlarin girip ciktigi nokta (dukkan kapisi).")]
        [SerializeField] private Transform entryPoint;

        [Header("Zamanlama")]
        [Tooltip("Iki kumarbaz arasindaki bekleme araligi (min - max gercek saniye).")]
        [SerializeField] private Vector2 spawnDelayRange = new Vector2(3f, 6f);

        private readonly List<Gambler> activeGamblers = new List<Gambler>();
        private GamblingTable table;
        private DayCycleManager dayCycle;
        private HiddenHatch hatch;

        private void Awake()
        {
            table = GetComponent<GamblingTable>();

            // Masa prefab olarak (ESYALAR sayfasindan) geldiginde sahnedeki kapiyi tutamaz: musterilerin giris noktasi kullanilir.
            if (entryPoint == null)
            {
                CustomerSpawner customers = FindFirstObjectByType<CustomerSpawner>();
                if (customers != null) entryPoint = customers.EntryPoint;
            }

            if (gamblerPrefab == null || entryPoint == null)
            {
                Debug.LogError("[GamblerSpawner] 'Gambler Prefab' veya 'Entry Point' atanmamis. " +
                               "'Kan Bagi > Kumarhaneyi Kur' menusunu calistir.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            dayCycle = DayCycleManager.Instance;

            if (dayCycle != null)
            {
                // Gun sonu panelinde zaman durur; sabah dukkan acilir: iki durumda da herkes kalkar.
                dayCycle.OnDayEnded += HandleEveryoneLeaves;
                dayCycle.OnShopOpened += HandleEveryoneLeaves;
            }

            // Kapak ve masa ayni bodrumla birlikte acilir; kapagin Awake'i bu Start'tan once calismis olur.
            hatch = HiddenHatch.Instance;
            if (hatch != null) hatch.OnStateChanged += HandleHatchChanged;
        }

        private void OnEnable()
        {
            if (gamblerPrefab == null || entryPoint == null) return;

            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(spawnDelayRange.x, spawnDelayRange.y));

                if (CanSpawn()) SpawnGambler();
            }
        }

        /// <summary>Gece olmali, bodrum kapagi acik olmali, bos sandalye olmali, gun sonu paneli acik olmamali.</summary>
        private bool CanSpawn()
        {
            if (!table.IsOpen || !table.HasFreeSeat) return false;
            if (hatch != null && !hatch.IsOpen) return false;

            return dayCycle == null || !dayCycle.IsWaitingForNextDay;
        }

        private void SpawnGambler()
        {
            GameObject gamblerObject = Instantiate(gamblerPrefab, entryPoint.position, entryPoint.rotation);

            if (!gamblerObject.TryGetComponent(out Gambler gambler))
            {
                Debug.LogError($"[GamblerSpawner] '{gamblerPrefab.name}' prefab'inda Gambler scripti yok.", this);
                Destroy(gamblerObject);
                return;
            }

            activeGamblers.Add(gambler);
            gambler.OnLeft += HandleGamblerLeft;
            gambler.Initialize(table, entryPoint.position);
        }

        private void HandleGamblerLeft(Gambler gambler)
        {
            gambler.OnLeft -= HandleGamblerLeft;
            activeGamblers.Remove(gambler);
        }

        /// <summary>Kapak kapaniyor: kumar bitti, herkes kalkip merdivenden cikar (kapak sonra kapanir).</summary>
        private void HandleHatchChanged(HiddenHatch.HatchState state)
        {
            if (state == HiddenHatch.HatchState.Closing) HandleEveryoneLeaves(0);
        }

        private void HandleEveryoneLeaves(int day)
        {
            // Kopya uzerinde donuyoruz: cikan kumarbaz listeden kendini siler.
            foreach (Gambler gambler in activeGamblers.ToArray())
            {
                if (gambler != null) gambler.LeaveNow();
            }
        }

        private void OnDestroy()
        {
            if (hatch != null) hatch.OnStateChanged -= HandleHatchChanged;

            if (dayCycle == null) return;

            dayCycle.OnDayEnded -= HandleEveryoneLeaves;
            dayCycle.OnShopOpened -= HandleEveryoneLeaves;
        }
    }
}
