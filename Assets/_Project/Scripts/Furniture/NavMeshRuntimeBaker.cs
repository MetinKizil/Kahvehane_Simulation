using System.Collections;
using KanBagi.Core;
using KanBagi.Data;
using Unity.AI.Navigation;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// NavMesh'i OYUN SIRASINDA yeniden kurar: esya eklenince/tasininca ve upgrade alininca
    /// (kilitli makine acilinca altinda delik acilsin). Editordeki bake'e artik bagli degiliz.
    ///
    /// Ayni karede gelen bircok istek (yuklemede 5 esya birden) tek bake'e indirilir: bake bir sonraki karede yapilir.
    /// Pasif objeler bake'e girmez; bu yuzden objeler acildiktan SONRA (bir kare sonra) bake edilir.
    /// Yeni NavMesh verisi bellekte olusur, diskteki asset'e dokunulmaz.
    /// </summary>
    public class NavMeshRuntimeBaker : MonoBehaviour
    {
        [Tooltip("Bos birakilirsa sahnedeki ilk NavMeshSurface (Zemin) bulunur.")]
        [SerializeField] private NavMeshSurface surface;

        private static NavMeshRuntimeBaker instance;

        private Coroutine pendingBake;
        private bool bakeRequestedWhileRunning;
        private bool hasBuiltOnce;  // ilk bake tam: diskteki NavMesh asset'i yerine bellekte yeni veri olussun

        /// <summary>Esya/upgrade disinda yolu degistiren seyler (bodrum kapagi, D6c) icin: sahnedeki baker'a bake ister.</summary>
        public static void RequestRebake()
        {
            if (instance != null) instance.RequestBake();
        }

        private void Awake() => instance = this;

        private void Start()
        {
            if (surface == null) surface = FindFirstObjectByType<NavMeshSurface>();

            if (surface == null)
            {
                Debug.LogError("[NavMeshRuntimeBaker] Sahnede NavMeshSurface yok (Zemin objesinde olmali).", this);
                enabled = false;
                return;
            }

            if (FurnitureManager.Instance != null) FurnitureManager.Instance.OnLayoutChanged += RequestBake;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnUpgradePurchased += HandleUpgradePurchased;

            // Ilk duzen bu Start'tan once kurulmus olabilir; bir kez bake edelim.
            RequestBake();
        }

        private void HandleUpgradePurchased(UpgradeData upgrade) => RequestBake();

        /// <summary>Bake'i bir sonraki kareye erteler; o kareye kadar gelen istekler birlesir.</summary>
        public void RequestBake()
        {
            if (!enabled) return;
            if (pendingBake != null)
            {
                bakeRequestedWhileRunning = true;
                return;
            }

            pendingBake = StartCoroutine(BakeNextFrame());
        }

        /// <summary>
        /// Ilk seferde tam bake (acilista; diskteki asset degistirilmesin diye yeni veri). Sonrakiler UpdateNavMesh: arka planda calisir ve sadece degisen
        /// karolari (esyanin oldugu bolgeyi) yeniden kurar - buyuk mahallede takilma olmaz (10d-1a).
        /// Guncelleme surerken gelen istek, bittikten sonra bir tur daha calistirir.
        /// </summary>
        private IEnumerator BakeNextFrame()
        {
            yield return null;

            if (!hasBuiltOnce)
            {
                surface.BuildNavMesh();
                hasBuiltOnce = true;
                pendingBake = null;
                yield break;
            }

            do
            {
                bakeRequestedWhileRunning = false;
                AsyncOperation operation = surface.UpdateNavMesh(surface.navMeshData);
                while (operation != null && !operation.isDone) yield return null;
            }
            while (bakeRequestedWhileRunning);

            pendingBake = null;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
            if (FurnitureManager.Instance != null) FurnitureManager.Instance.OnLayoutChanged -= RequestBake;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
