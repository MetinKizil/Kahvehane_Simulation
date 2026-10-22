using System;
using KanBagi.Data;
using KanBagi.Interaction;
using KanBagi.Player;
using KanBagi.Shop;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Yerlestirme modu: oyuncu bir esyayi (simdilik masa) F ile kaldirir, esya yari saydam HAYALET olur
    /// ve oyuncunun baktigi zemin noktasini takip eder (0.1 m adim). Tekerlek 5 derece, Shift+tekerlek 45 derece dondurur.
    /// Yesil = konabilir, kirmizi = konamaz. Sol tik birakir, sag tik iptal eder (esya eski yerine doner).
    /// PC'den alinan yeni esya da ayni yoldan elde hayalet olarak gelir (BeginPlacingNew): hayalet dukkana
    /// KAYDEDILMEZ; sol tikla konunca FurnitureManager.TryPurchase ile para duser ve esya olusur, iptalde hayalet silinir.
    ///
    /// Player objesinde durur; SINGLETON DEGIL (co-op'ta her oyuncunun kendi yerlestirmesi).
    /// Esyanin yerini SADECE onaylayinca FurnitureManager.Move ile degistirir (dunya durumu, RPC siniri).
    /// </summary>
    public class FurniturePlacer : MonoBehaviour
    {
        [Header("Input")]
        [Tooltip("Bos birakilirsa proje genelindeki (Project-wide) Input Actions asset'i kullanilir.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Isin")]
        [Tooltip("Bos birakilirsa child'daki Camera bulunur.")]
        [SerializeField] private Transform rayOrigin;
        [Tooltip("Esyaya bakip F ile kaldirma mesafesi (metre).")]
        [SerializeField] private float pickUpRange = 3f;
        [Tooltip("Hayaletin en fazla ne kadar uzaga konabilecegi (metre).")]
        [SerializeField] private float placeRange = 5f;

        [Header("Adimlar")]
        [Tooltip("Konum izgarasi (metre).")]
        [SerializeField] private float gridStep = 0.1f;
        [Tooltip("Tekerlegin her tikinda donus (derece).")]
        [SerializeField] private float rotationStep = 5f;
        [Tooltip("Shift basiliyken tekerlegin her tikinda donus (derece).")]
        [SerializeField] private float fastRotationStep = 45f;

        [Header("Gecerlilik")]
        [Tooltip("Masanin oturma noktasinda bos kalmasi gereken alanin yaricapi (metre).")]
        [SerializeField] private float seatClearRadius = 0.3f;
        [Tooltip("Cakisma kontrolunde kutu bu oranla kucultulur (yan yana degmeye izin ver).")]
        [SerializeField] private float overlapShrink = 0.95f;

        [Header("Hayalet")]
        [SerializeField] private Color validColor = new Color(0.3f, 0.9f, 0.4f, 0.45f);
        [SerializeField] private Color invalidColor = new Color(0.95f, 0.3f, 0.3f, 0.45f);

        private InputAction moveAction;
        private InputAction rotateAction;
        private InputAction confirmAction;
        private InputAction cancelAction;
        private InputAction fastRotateAction;

        private PlayerHands hands;
        private PlayerLock playerLock;
        private PlayerInteractor interactor;

        private Material validMaterial;
        private Material invalidMaterial;

        // Tasinan esya ve geri donus bilgisi
        private PlacedFurniture current;
        private Vector3 originalPosition;
        private float originalYaw;
        private float yaw;
        private float pivotHeight; // pivottan esyanin tabanina mesafe
        private bool isValid;
        private Renderer[] renderers;
        private Material[][] originalMaterials;
        private Collider[] colliders;
        private bool[] colliderStates;
        private Table table;
        // Doluysa tasinan sey PC'den yeni alinmis (henuz odenmemis) bir esyanin hayaleti.
        private FurnitureData pendingPurchase;

        public bool IsPlacing => current != null;

        /// <summary>Yerlestirme basladi/bitti. Ipucu UI'i abone olur.</summary>
        public event Action<bool> OnPlacingChanged;

        /// <summary>Kisa bilgi mesaji ("Masa dolu, tasinamaz"). Ipucu UI'i gosterir.</summary>
        public event Action<string> OnMessage;

        private void Awake()
        {
            hands = GetComponent<PlayerHands>();
            playerLock = GetComponent<PlayerLock>();
            interactor = GetComponent<PlayerInteractor>();

            if (rayOrigin == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                if (childCamera != null) rayOrigin = childCamera.transform;
            }

            if (inputActions == null) inputActions = InputSystem.actions;
            InputActionMap map = inputActions != null ? inputActions.FindActionMap("Player") : null;

            moveAction = map?.FindAction("MoveFurniture");
            rotateAction = map?.FindAction("RotateFurniture");
            confirmAction = map?.FindAction("Attack");
            cancelAction = map?.FindAction("CancelPlacement");
            fastRotateAction = map?.FindAction("Sprint");

            if (rayOrigin == null || moveAction == null || rotateAction == null || confirmAction == null || cancelAction == null)
            {
                Debug.LogError("[FurniturePlacer] Kamera veya 'Player' haritasindaki MoveFurniture / RotateFurniture / " +
                               "Attack / CancelPlacement action'lari bulunamadi.", this);
                enabled = false;
                return;
            }

            validMaterial = CreateGhostMaterial(validColor);
            invalidMaterial = CreateGhostMaterial(invalidColor);
        }

        private void OnEnable()
        {
            if (moveAction == null) return;

            moveAction.performed += HandleMovePressed;
            rotateAction.performed += HandleRotate;
            confirmAction.performed += HandleConfirm;
            cancelAction.performed += HandleCancel;
            if (playerLock != null) playerLock.OnControlEnabledChanged += HandleControlChanged;
        }

        private void OnDisable()
        {
            if (moveAction == null) return;

            moveAction.performed -= HandleMovePressed;
            rotateAction.performed -= HandleRotate;
            confirmAction.performed -= HandleConfirm;
            cancelAction.performed -= HandleCancel;
            if (playerLock != null) playerLock.OnControlEnabledChanged -= HandleControlChanged;

            if (IsPlacing) Cancel();
        }

        // ---- Disaridan: PC'den alinan yeni esya ----

        /// <summary>
        /// Satin alinan esyanin hayaletini oyuncunun onunde olusturup eline verir. Para henuz dusmez;
        /// sol tikla konunca odenir, sag tikla iptal edilirse hayalet silinir ve para hic gitmez.
        /// </summary>
        public void BeginPlacingNew(FurnitureData data)
        {
            if (data == null || data.Prefab == null) return;
            if (IsPlacing) Cancel();

            Vector3 start = rayOrigin.position + rayOrigin.forward * 2f;
            GameObject ghost = Instantiate(data.Prefab, start, Quaternion.identity);
            ghost.name = data.Prefab.name;

            if (!ghost.TryGetComponent(out PlacedFurniture furniture))
                furniture = ghost.AddComponent<PlacedFurniture>();
            furniture.Initialize(data);

            pendingPurchase = data;
            Begin(furniture);
        }

        // ---- Girdi ----

        private void HandleMovePressed(InputAction.CallbackContext context)
        {
            if (IsPlacing || !CanControl()) return;

            if (hands != null && !hands.IsEmpty)
            {
                OnMessage?.Invoke("Once elindekini birak");
                return;
            }

            if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, pickUpRange, ~0, QueryTriggerInteraction.Ignore))
                return;

            PlacedFurniture target = hit.collider.GetComponentInParent<PlacedFurniture>();
            if (target == null) return;

            if (!CanPickUp(target, out string reason))
            {
                OnMessage?.Invoke(reason);
                return;
            }

            Begin(target);
        }

        private void HandleRotate(InputAction.CallbackContext context)
        {
            if (!IsPlacing) return;

            float scroll = context.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f)) return;

            float step = fastRotateAction != null && fastRotateAction.IsPressed() ? fastRotationStep : rotationStep;
            yaw = Mathf.Repeat(yaw + Mathf.Sign(scroll) * step, 360f);
        }

        private void HandleConfirm(InputAction.CallbackContext context)
        {
            if (!IsPlacing) return;

            if (!isValid)
            {
                OnMessage?.Invoke("Buraya konamaz");
                return;
            }

            FurnitureManager manager = FurnitureManager.Instance;
            if (manager == null) return;

            Vector3 position = current.transform.position;

            if (pendingPurchase != null)
            {
                // Para/sinir yerlestirme aninda tekrar kontrol edilir (bu arada ceza kesilmis olabilir).
                if (!manager.CanPurchase(pendingPurchase, out string reason))
                {
                    OnMessage?.Invoke(reason);
                    return;
                }

                FurnitureData data = pendingPurchase;
                DiscardGhost();
                manager.TryPurchase(data, position, yaw);
                return;
            }

            PlacedFurniture placed = current;
            End();

            // Tek gecis noktasi: NavMesh guncellenir, kayda girer.
            manager.Move(placed, position, yaw);
        }

        private void HandleCancel(InputAction.CallbackContext context)
        {
            if (IsPlacing) Cancel();
        }

        /// <summary>Gun sonu paneli, PC ekrani vb. oyuncuyu kilitlerse yerlestirme iptal olur.</summary>
        private void HandleControlChanged(bool isEnabled)
        {
            if (!isEnabled && IsPlacing) Cancel();
        }

        private bool CanControl() => playerLock == null || playerLock.IsControlEnabled;

        /// <summary>Dolu masa tasinmaz: musteri oturuyorsa veya ustunde bardak varsa.</summary>
        private static bool CanPickUp(PlacedFurniture furniture, out string reason)
        {
            reason = null;

            if (furniture.TryGetComponent(out Table targetTable) && (!targetTable.IsFree || targetTable.HasGlass))
            {
                reason = "Masa dolu, tasinamaz";
                return false;
            }

            return true;
        }

        // ---- Hayalet ----

        private void Begin(PlacedFurniture furniture)
        {
            current = furniture;
            originalPosition = furniture.transform.position;
            originalYaw = furniture.Yaw;
            yaw = originalYaw;

            // Yeni olusturulan hayaletin collider sinirlari dogru okunsun.
            Physics.SyncTransforms();
            colliders = furniture.GetComponentsInChildren<Collider>(true);
            pivotHeight = MeasurePivotHeight(furniture, colliders);

            // Tasinirken: isin kendine carpmasin, musteri bu masayi ayirmasin.
            colliderStates = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                colliderStates[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            table = furniture.GetComponent<Table>();
            if (table != null) table.enabled = false; // OnDisable -> TableManager'dan cikar

            renderers = furniture.GetComponentsInChildren<Renderer>(true);
            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i].sharedMaterials;

            isValid = false;
            ApplyGhostMaterial(invalidMaterial);

            // E ile baska seye dokunulmasin; ipucu yazisi da gizlenir.
            if (interactor != null) interactor.enabled = false;

            OnPlacingChanged?.Invoke(true);
        }

        private void Update()
        {
            if (!IsPlacing) return;

            bool onFloor = TryGetFloorPoint(out Vector3 point);
            if (onFloor)
            {
                point.x = Snap(point.x);
                point.z = Snap(point.z);
                point.y += pivotHeight;
                current.transform.SetPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
            }
            else
            {
                current.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            }

            bool valid = onFloor && !OverlapsSomething() && IsSeatClear();
            if (valid != isValid)
            {
                isValid = valid;
                ApplyGhostMaterial(isValid ? validMaterial : invalidMaterial);
            }
        }

        /// <summary>Bakis isini dukkan zemininde (NavMeshSurface'li obje ve cocuklari) karsilayan nokta.</summary>
        private bool TryGetFloorPoint(out Vector3 point)
        {
            point = Vector3.zero;

            if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out RaycastHit hit, placeRange, ~0, QueryTriggerInteraction.Ignore))
                return false;

            if (!IsShopFloor(hit.collider)) return false;

            point = hit.point;
            return true;
        }

        private static bool IsShopFloor(Collider collider) => collider.GetComponentInParent<NavMeshSurface>() != null;

        private float Snap(float value) => gridStep > 0f ? Mathf.Round(value / gridStep) * gridStep : value;

        /// <summary>Esyanin ana kutusu baska bir seye (esya, makine, musteri, oyuncu) degiyor mu? Zemin sayilmaz.</summary>
        private bool OverlapsSomething()
        {
            if (!TryGetMainBox(out Vector3 center, out Vector3 halfExtents)) return false;

            Collider[] hits = Physics.OverlapBox(center, halfExtents * overlapShrink, current.transform.rotation, ~0,
                                                 QueryTriggerInteraction.Ignore);

            foreach (Collider other in hits)
            {
                if (!IsShopFloor(other)) return true;
            }

            return false;
        }

        /// <summary>Masanin oturma noktasi zeminde mi ve bos mu? (Sandalye tarafi duvara/tezgaha dayanmasin.)</summary>
        private bool IsSeatClear()
        {
            if (table == null) return true;

            Vector3 seat = table.SeatPosition;
            Vector3 probeTop = new Vector3(seat.x, current.transform.position.y + 1f, seat.z);

            // Zemin var mi?
            if (!Physics.Raycast(probeTop, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore) ||
                !IsShopFloor(hit.collider))
                return false;

            // Oturan musteri boyunda bir hacim bos mu?
            Vector3 bottom = hit.point + Vector3.up * (seatClearRadius + 0.05f);
            Vector3 top = hit.point + Vector3.up * 1.6f;
            foreach (Collider other in Physics.OverlapCapsule(bottom, top, seatClearRadius, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!IsShopFloor(other)) return false;
            }

            return true;
        }

        /// <summary>Esyanin kokundeki BoxCollider'dan dunya uzayinda merkez ve yari boyut (collider kapaliyken de calisir).</summary>
        private bool TryGetMainBox(out Vector3 center, out Vector3 halfExtents)
        {
            center = Vector3.zero;
            halfExtents = Vector3.zero;

            if (!current.TryGetComponent(out BoxCollider box)) return false;

            Transform t = current.transform;
            center = t.TransformPoint(box.center);
            halfExtents = Vector3.Scale(box.size, t.lossyScale) * 0.5f;
            halfExtents = new Vector3(Mathf.Abs(halfExtents.x), Mathf.Abs(halfExtents.y), Mathf.Abs(halfExtents.z));
            return true;
        }

        /// <summary>Pivottan esyanin tabanina yukseklik (masa: pivot ortada, 0.4).</summary>
        private static float MeasurePivotHeight(PlacedFurniture furniture, Collider[] furnitureColliders)
        {
            float bottom = float.MaxValue;
            foreach (Collider furnitureCollider in furnitureColliders)
            {
                if (furnitureCollider.enabled) bottom = Mathf.Min(bottom, furnitureCollider.bounds.min.y);
            }

            return bottom < float.MaxValue ? furniture.transform.position.y - bottom : 0f;
        }

        private void ApplyGhostMaterial(Material ghost)
        {
            foreach (Renderer furnitureRenderer in renderers)
            {
                if (furnitureRenderer == null) continue;

                var materials = new Material[furnitureRenderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = ghost;
                furnitureRenderer.sharedMaterials = materials;
            }
        }

        // ---- Bitis ----

        private void Cancel()
        {
            // Yeni esya: hayalet silinir, para hic dusmemisti.
            if (pendingPurchase != null)
            {
                DiscardGhost();
                return;
            }

            PlacedFurniture furniture = current;
            End();

            // Esya eski yerine/acisina doner; duzen degismedigi icin NavMesh'e dokunulmaz.
            furniture.transform.SetPositionAndRotation(originalPosition, Quaternion.Euler(0f, originalYaw, 0f));
        }

        /// <summary>Henuz satin alinmamis hayaleti yok eder (eski haline dondurmeye gerek yok).</summary>
        private void DiscardGhost()
        {
            GameObject ghost = current.gameObject;

            // Once kapatilir: Table.OnDisable hemen calisir, masa TableManager'a hic geri girmez.
            ghost.SetActive(false);
            Destroy(ghost);

            if (interactor != null && CanControl()) interactor.enabled = true;

            pendingPurchase = null;
            current = null;
            table = null;
            renderers = null;
            colliders = null;

            OnPlacingChanged?.Invoke(false);
        }

        /// <summary>Hayaleti normale dondurur: malzemeler, collider'lar, masa kaydi, etkilesim.</summary>
        private void End()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].sharedMaterials = originalMaterials[i];
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
            }

            if (table != null) table.enabled = true;
            if (interactor != null && CanControl()) interactor.enabled = true;

            current = null;
            table = null;
            renderers = null;
            colliders = null;

            OnPlacingChanged?.Invoke(false);
        }

        /// <summary>URP Lit'in seffaf hali: yari saydam renkli hayalet.</summary>
        private static Material CreateGhostMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = "Hayalet" };

            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f);   // Alpha
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;

            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            material.color = color;
            return material;
        }

        private void OnDestroy()
        {
            if (validMaterial != null) Destroy(validMaterial);
            if (invalidMaterial != null) Destroy(invalidMaterial);
        }
    }
}
