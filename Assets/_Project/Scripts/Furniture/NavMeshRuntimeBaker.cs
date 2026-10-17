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

        private Coroutine pendingBake;

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
            if (!enabled || pendingBake != null) return;

            pendingBake = StartCoroutine(BakeNextFrame());
        }

        private IEnumerator BakeNextFrame()
        {
            yield return null;

            surface.BuildNavMesh();
            pendingBake = null;
        }

        private void OnDestroy()
        {
            if (FurnitureManager.Instance != null) FurnitureManager.Instance.OnLayoutChanged -= RequestBake;
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
