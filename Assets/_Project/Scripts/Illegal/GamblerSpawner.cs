using System.Collections;
using System.Collections.Generic;
using KanBagi.Core;
using UnityEngine;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Gece kumar masasina kumarbaz gonderir ve icerdekilerin kaydini tutar.
    /// Kumar masasinin UZERINDE durur: masa upgrade alinana kadar pasif oldugu icin
    /// kumarhane acilmadan hic kumarbaz gelmez - ekstra kod gerekmez.
    /// Sadece "ne zaman ve kac kisi" sorusundan sorumludur (SOLID).
    ///
    /// Kurulum: Kan Bagi > Kumarhaneyi Kur (prefab ve giris noktasini kendisi baglar).
    /// </summary>
    [RequireComponent(typeof(GamblingTable))]
    public class GamblerSpawner : MonoBehaviour
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

        private void Awake()
        {
            table = GetComponent<GamblingTable>();

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

            table.OnHiddenChanged += HandleHiddenChanged;
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

        /// <summary>Gece olmali, ortu acik olmali, bos sandalye olmali, gun sonu paneli acik olmamali.</summary>
        private bool CanSpawn()
        {
            if (!table.IsOpen || table.IsHidden || !table.HasFreeSeat) return false;

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

        /// <summary>Ortu kapandi: kumar bitti, herkes kalkar.</summary>
        private void HandleHiddenChanged(bool isHidden)
        {
            if (isHidden) HandleEveryoneLeaves(0);
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
            if (table != null) table.OnHiddenChanged -= HandleHiddenChanged;

            if (dayCycle == null) return;

            dayCycle.OnDayEnded -= HandleEveryoneLeaves;
            dayCycle.OnShopOpened -= HandleEveryoneLeaves;
        }
    }
}
