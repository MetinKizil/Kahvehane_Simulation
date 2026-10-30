using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Gece bodruma (Kumar Odasi) gelen kumarbaz: rulet masasina yurur, oturur, birkac tur oynar ve gider.
    /// Oyuncuya ozel hicbir seye dokunmaz (dunya NPC'si, co-op uyumlu).
    /// Ne zaman gelecegine GamblerSpawner, masanin kurallarina GamblingTable karar verir (SOLID).
    /// Durum makinesi coroutine ile yurutulur.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Gambler : MonoBehaviour
    {
        [Header("Oyun")]
        [Tooltip("Kac rulet turu oynayip kalkacagi (min - max, dahil). Turun suresini masa belirler.")]
        [SerializeField] private Vector2Int roundsRange = new Vector2Int(3, 6);

        [Header("Hareket")]
        [Tooltip("Hedefe bu mesafeye gelince varmis sayilir.")]
        [SerializeField] private float arriveThreshold = 0.4f;
        [Tooltip("Yol bulunamazsa bu sure sonunda takilip kalmamak icin devam edilir.")]
        [SerializeField] private float moveTimeout = 30f;

        private NavMeshAgent agent;
        private GamblingTable table;
        private Vector3 exitPosition;
        private int seatIndex = -1;
        private int roundsPlayed;
        private bool isLeaving;

        /// <summary>Kumarbaz dukkandan ciktiginda tetiklenir. Spawner buna abone olur.</summary>
        public event Action<Gambler> OnLeft;

        /// <summary>Sahnedeki kumarbaz sayisi. Bodrum kapagi kapanmadan once hepsinin cikmasini bekler (D6c).</summary>
        public static int ActiveCount { get; private set; }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            ActiveCount++;
        }

        /// <summary>Spawner tarafindan cagrilir; kumarbazin yasam dongusunu baslatir.</summary>
        public void Initialize(GamblingTable targetTable, Vector3 exit)
        {
            table = targetTable;
            exitPosition = exit;
            StartCoroutine(LifeRoutine());
        }

        /// <summary>Kapak kapaniyor, gun bitti veya dukkan acildi: ne yapiyorsa birakip cikar.</summary>
        public void LeaveNow()
        {
            if (isLeaving) return;

            StopAllCoroutines();
            if (table != null) table.OnRoundFinished -= HandleRoundFinished;
            StartCoroutine(LeaveRoutine());
        }

        private IEnumerator LifeRoutine()
        {
            // Bos sandalye yoksa iceri girmeden geri doner.
            if (table == null || !table.TryReserveSeat(this, out seatIndex))
            {
                yield return LeaveRoutine();
                yield break;
            }

            yield return MoveToRoutine(table.GetSeatPosition(seatIndex));
            SitDown();

            // Parayi ve supheyi masa her turda oturanlara gore isler; kumarbaz sadece oynadigi turu sayar.
            int rounds = Random.Range(roundsRange.x, roundsRange.y + 1);
            roundsPlayed = 0;
            table.NotifySeated(seatIndex, this);
            table.OnRoundFinished += HandleRoundFinished;

            while (roundsPlayed < rounds) yield return null;

            table.OnRoundFinished -= HandleRoundFinished;
            yield return LeaveRoutine();
        }

        private void HandleRoundFinished() => roundsPlayed++;

        private IEnumerator LeaveRoutine()
        {
            isLeaving = true;
            ReleaseSeat();
            StandUp();

            yield return MoveToRoutine(exitPosition);

            OnLeft?.Invoke(this);
            Destroy(gameObject);
        }

        private IEnumerator MoveToRoutine(Vector3 destination)
        {
            if (!agent.isActiveAndEnabled) yield break;

            // Sandalye noktasi NavMesh'in tam ustunde olmayabilir: en yakin gecerli noktaya git.
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 1.5f, NavMesh.AllAreas))
                destination = hit.position;

            agent.isStopped = false;
            agent.SetDestination(destination);

            float elapsed = 0f;

            while (elapsed < moveTimeout)
            {
                elapsed += Time.deltaTime;

                if (!agent.pathPending &&
                    agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, arriveThreshold))
                    yield break;

                yield return null;
            }

            Debug.LogWarning($"[Gambler] '{name}' hedefe ulasamadi, yol kapali olabilir.", this);
        }

        /// <summary>Oturur: agent kapatilir ve masaya donulur.</summary>
        private void SitDown()
        {
            if (agent.isActiveAndEnabled) agent.isStopped = true;
            agent.enabled = false;

            if (table == null) return;

            Vector3 lookTarget = table.transform.position;
            lookTarget.y = transform.position.y;
            transform.LookAt(lookTarget);
        }

        /// <summary>Kalkar: agent tekrar acilir ve NavMesh uzerine oturtulur.</summary>
        private void StandUp()
        {
            if (agent.enabled) return;

            agent.enabled = true;

            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                agent.Warp(hit.position);
        }

        private void ReleaseSeat()
        {
            if (table == null || seatIndex < 0) return;

            table.ReleaseSeat(seatIndex, this);
            seatIndex = -1;
        }

        private void OnDestroy()
        {
            // Beklenmedik yok olmalarda sandalye kilitli kalmasin.
            if (table != null) table.OnRoundFinished -= HandleRoundFinished;
            ReleaseSeat();
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }
    }
}
