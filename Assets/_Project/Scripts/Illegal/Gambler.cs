using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Gece arka odaya gelen kumarbaz: masaya yurur, oturur, birkac el oynar ve gider.
    /// Oyuncuya ozel hicbir seye dokunmaz (dunya NPC'si, co-op uyumlu).
    /// Ne zaman gelecegine GamblerSpawner, masanin kurallarina GamblingTable karar verir (SOLID).
    /// Durum makinesi coroutine ile yurutulur.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class Gambler : MonoBehaviour
    {
        [Header("Oyun")]
        [Tooltip("Iki el arasindaki sure (gercek saniye).")]
        [SerializeField] private float roundInterval = 4f;
        [Tooltip("Kac el oynayip kalkacagi (min - max, dahil).")]
        [SerializeField] private Vector2Int roundsRange = new Vector2Int(4, 8);

        [Header("Hareket")]
        [Tooltip("Hedefe bu mesafeye gelince varmis sayilir.")]
        [SerializeField] private float arriveThreshold = 0.4f;
        [Tooltip("Yol bulunamazsa bu sure sonunda takilip kalmamak icin devam edilir.")]
        [SerializeField] private float moveTimeout = 30f;

        private NavMeshAgent agent;
        private GamblingTable table;
        private Vector3 exitPosition;
        private int seatIndex = -1;
        private bool isLeaving;

        /// <summary>Kumarbaz dukkandan ciktiginda tetiklenir. Spawner buna abone olur.</summary>
        public event Action<Gambler> OnLeft;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        /// <summary>Spawner tarafindan cagrilir; kumarbazin yasam dongusunu baslatir.</summary>
        public void Initialize(GamblingTable targetTable, Vector3 exit)
        {
            table = targetTable;
            exitPosition = exit;
            StartCoroutine(LifeRoutine());
        }

        /// <summary>Ortu kapandi, gun bitti veya dukkan acildi: ne yapiyorsa birakip cikar.</summary>
        public void LeaveNow()
        {
            if (isLeaving) return;

            StopAllCoroutines();
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

            int rounds = Random.Range(roundsRange.x, roundsRange.y + 1);
            for (int i = 0; i < rounds; i++)
            {
                yield return new WaitForSeconds(roundInterval);

                // Parayi ve supheyi masa isler; kumarbaz sadece "bir el oynandi" der.
                table.PlayHouseRound();
            }

            yield return LeaveRoutine();
        }

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
            ReleaseSeat();
        }
    }
}
