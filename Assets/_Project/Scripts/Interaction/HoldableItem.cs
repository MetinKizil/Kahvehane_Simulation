using KanBagi.Player;
using UnityEngine;

namespace KanBagi.Interaction
{
    /// <summary>
    /// Oyuncunun eline alabilecegi fiziksel nesne (bardak, tabak, kutu...).
    /// Sadece "tasinma" davranisindan sorumludur; nesnenin ne oldugunu bilmez (SOLID).
    /// Ele alininca fizigi kapanir ve el noktasina yapisir, birakilinca geri acilir.
    /// </summary>
    [DisallowMultipleComponent]
    public class HoldableItem : MonoBehaviour, IInteractable
    {
        [Header("Tanim")]
        [SerializeField] private string displayName = "Esya";
        [Tooltip("Yere/masaya bakildiginda cikacak ipucu.")]
        [SerializeField] private string pickupPrompt = "Al";

        private Rigidbody body;
        private Collider[] colliders;

        public string DisplayName => displayName;
        public bool IsHeld { get; private set; }

        public string InteractionPrompt => PlayerHands.Instance != null && !PlayerHands.Instance.IsEmpty
            ? "Ellerin dolu"
            : pickupPrompt;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
        }

        public void Interact()
        {
            if (PlayerHands.Instance != null)
                PlayerHands.Instance.TryHold(this);
        }

        /// <summary>
        /// Nesnenin ekranda gorunen adini calisma aninda degistirir.
        /// Ornek: koli doldurulunca "Koli" yerine "Cay Yapragi Kolisi (10)".
        /// </summary>
        public void SetDisplayName(string newName)
        {
            if (string.IsNullOrEmpty(newName)) return;

            displayName = newName;
        }

        /// <summary>Nesneyi el noktasina baglar ve fizigini devre disi birakir. PlayerHands cagirir.</summary>
        public void AttachTo(Transform anchor)
        {
            IsHeld = true;
            SetPhysicsEnabled(false);

            transform.SetParent(anchor, worldPositionStays: false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Nesneyi elden birakip verilen konuma yerlestirir ve fizigini geri acar.</summary>
        public void Detach(Vector3 position, Quaternion rotation, bool restorePhysics = true)
        {
            IsHeld = false;

            transform.SetParent(null, worldPositionStays: false);
            transform.SetPositionAndRotation(position, rotation);

            if (restorePhysics) SetPhysicsEnabled(true);
        }

        /// <summary>
        /// Nesnenin isinla secilebilirligini acar/kapatir.
        /// Ocakta doldurulurken veya lavaboda yikanirken bardagin alinmasini engellemek icin kullanilir.
        /// </summary>
        public void SetPickupEnabled(bool isEnabled)
        {
            foreach (Collider itemCollider in colliders)
                itemCollider.enabled = isEnabled;
        }

        private void SetPhysicsEnabled(bool isEnabled)
        {
            // Collider'lar kapali olmazsa elimizdeki nesne kendi raycast'imizi engelleyebilir.
            foreach (Collider itemCollider in colliders)
                itemCollider.enabled = isEnabled;

            if (body == null) return;

            body.isKinematic = !isEnabled;
            if (isEnabled) body.WakeUp();
        }
    }
}
