using System;
using System.Collections;
using KanBagi.Furniture;
using KanBagi.Interaction;
using KanBagi.Player;
using UnityEngine;
using UnityEngine.AI;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Bodrumdaki Kumar Odasi'nin (D6c) gizli kapagi: polisten saklanmanin TEK yolu.
    /// Kapali: kapak zeminle ayni hizada yatar, uzerinden yurunur, korkuluk gizli; polis kumar odasini bulamaz.
    /// Acik: kapak duvara dogru kalkar, korkuluk gorunur, merdivenden inilir; kumarbazlar sadece bu durumda gelir.
    /// Kapatmak: once "kapaniyor" olur - yeni kumarbaz gelmez, icerdekiler yuruyerek cikar; son kisi
    /// cikinca (ve merdivende kimse kalmayinca) kapak kendiliginden kapanir. Kayittan yuklemede KAPALI baslar.
    ///
    /// Dunya durumu (co-op'ta server'da tek kapak) oldugu icin Instance tutulur; durum SADECE Interact'ten degisir.
    /// Bu script kapagin collider'iyla AYNI objede durmali (PlayerInteractor isinin carptigi objeye bakar).
    /// </summary>
    public class HiddenHatch : MonoBehaviour, IInteractable
    {
        public enum HatchState
        {
            Closed,
            Open,
            Closing
        }

        public static HiddenHatch Instance { get; private set; }

        [Header("Baglantilar")]
        [Tooltip("Kapagin dondugu mentese. Bos birakilirsa bu objenin parent'i.")]
        [SerializeField] private Transform hinge;
        [Tooltip("Acikken menteseye eklenen donus (derece).")]
        [SerializeField] private Vector3 openRotation = new Vector3(0f, 0f, 90f);
        [Tooltip("Sadece kapak acikken gorunen objeler (korkuluk).")]
        [SerializeField] private GameObject[] openOnlyObjects;

        [Header("Merdiven boslugu")]
        [Tooltip("Kapanmadan once bos olmasi gereken hacim (dunya): icinde oyuncu/NPC varsa kapak beklemeye devam eder.")]
        [SerializeField] private Vector3 holeCenter;
        [SerializeField] private Vector3 holeSize = new Vector3(1.5f, 3.2f, 5f);
        [SerializeField] private float closeCheckInterval = 0.5f;

        [Header("Ipucu Metinleri")]
        [SerializeField] private string openMessage = "Kapagi Ac";
        [SerializeField] private string closeMessage = "Kapagi Kapat";
        [Tooltip("{0} hala disari cikmamis kumarbaz sayisi.")]
        [SerializeField] private string closingFormat = "Kapak kapaniyor - {0} kisi cikiyor";

        private Quaternion closedRotation;

        public HatchState State { get; private set; } = HatchState.Closed;
        public bool IsOpen => State == HatchState.Open;
        public bool IsClosed => State == HatchState.Closed;

        /// <summary>Durum degisince tetiklenir. GamblerSpawner "kapaniyor"da herkesi kaldirir.</summary>
        public event Action<HatchState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError("[HiddenHatch] Sahnede ikinci bir kapak var; sadece biri kullanilir.", this);
                enabled = false;
                return;
            }

            Instance = this;
            if (hinge == null) hinge = transform.parent != null ? transform.parent : transform;
            closedRotation = hinge.localRotation;
            ApplyVisuals();
        }

        public string GetPrompt(PlayerContext player)
        {
            switch (State)
            {
                case HatchState.Closed: return openMessage;
                case HatchState.Open: return closeMessage;
                default: return string.Format(closingFormat, Gambler.ActiveCount);
            }
        }

        public void Interact(PlayerContext player)
        {
            if (State == HatchState.Closed)
            {
                SetState(HatchState.Open);
            }
            else if (State == HatchState.Open)
            {
                SetState(HatchState.Closing);
                StartCoroutine(CloseWhenEmpty());
            }
        }

        /// <summary>Kumarbazlar cikana ve merdivende kimse kalmayana kadar bekler, sonra kapatir.</summary>
        private IEnumerator CloseWhenEmpty()
        {
            var wait = new WaitForSeconds(closeCheckInterval);

            while (Gambler.ActiveCount > 0 || IsSomeoneInHole())
                yield return wait;

            SetState(HatchState.Closed);
        }

        /// <summary>Merdiven boslugunda oyuncu (CharacterController) veya NPC (NavMeshAgent) var mi?</summary>
        private bool IsSomeoneInHole()
        {
            foreach (Collider other in Physics.OverlapBox(holeCenter, holeSize * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (other is CharacterController || other.GetComponentInParent<NavMeshAgent>() != null) return true;
            }

            return false;
        }

        private void SetState(HatchState state)
        {
            bool geometryChanged = (State == HatchState.Closed) != (state == HatchState.Closed);

            State = state;
            ApplyVisuals();
            OnStateChanged?.Invoke(State);

            // Kapali kapak yurunebilir zemin, acik kapak merdivene yol acar: NPC yollari yeniden hesaplanir.
            if (geometryChanged) NavMeshRuntimeBaker.RequestRebake();
        }

        private void ApplyVisuals()
        {
            bool closed = State == HatchState.Closed;
            hinge.localRotation = closed ? closedRotation : closedRotation * Quaternion.Euler(openRotation);

            if (openOnlyObjects == null) return;

            foreach (GameObject target in openOnlyObjects)
            {
                if (target != null) target.SetActive(!closed);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(holeCenter, holeSize);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
