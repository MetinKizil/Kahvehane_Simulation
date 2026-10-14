using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// NavMesh araclari.
    ///
    /// 1) Kontrol: sahnedeki her esyanin (collider'li kok obje) altinda NavMesh'te DELIK olup olmadigini
    ///    raporlar. Delik yoksa NPC'ler o esyanin icinden/ustunden yurur.
    /// 2) Kilitli esyalara engel: upgrade ile acilan (sahnede PASIF duran) esyalar bake'e giremez,
    ///    delik acamaz. Bunlara "oyan" (carve) NavMeshObstacle eklenir: aktif olunca deligini
    ///    CALISMA ANINDA kendisi acar. (NavMeshSurface engelli objeleri bake'e almaz.)
    /// 3) Yeniden bake (ASIL COZUM): her esya "Not Walkable" isaretlenir, kilitli esyalar bake suresince
    ///    acilir ve engelli objeler de bake'e alinir. Delikler NavMesh dosyasina kalici yazilir;
    ///    calisma anindaki oymaya guvenilmez, esya tepeleri de asla yurunebilir olmaz.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > NavMesh Kontrol Et
    ///                      Kan Bagi > NavMesh Deliklerini Duzelt
    ///                      Kan Bagi > NavMesh'i Yeniden Bake Et
    /// </summary>
    public static class NavMeshChecker
    {
        // Bu boyuttan buyuk objeler zemin/sokak sayilir, kontrol edilmez.
        private const float MaxFurnitureSize = 6f;

        [MenuItem("Kan Bagi/NavMesh Kontrol Et")]
        public static void Check()
        {
            var report = new StringBuilder("[NavMeshChecker] Esya alti NavMesh kontrolu:\n");
            int problems = 0;

            foreach (Collider collider in FindFurniture())
            {
                bool walkableUnder = IsWalkableUnder(collider);

                string state = collider.gameObject.activeInHierarchy ? "aktif" : "PASIF";

                if (!walkableUnder)
                {
                    report.AppendLine($"  OK    {collider.name} ({state}) - altinda delik var");
                    continue;
                }

                if (HasCarvingObstacle(collider.gameObject))
                {
                    report.AppendLine($"  OK    {collider.name} ({state}) - bake'te delik yok ama calisma aninda kendisi oyuyor");
                    continue;
                }

                problems++;
                report.AppendLine($"  SORUN {collider.name} ({state}) - altinda YURUNEBILIR alan var, NPC'ler icinden gecer");
            }

            report.AppendLine(problems == 0
                ? "Sorun yok."
                : $"{problems} esyanin altinda delik yok. 'Kan Bagi > NavMesh Deliklerini Duzelt' calistir.");

            if (problems == 0) Debug.Log(report.ToString());
            else Debug.LogWarning(report.ToString());
        }

        [MenuItem("Kan Bagi/NavMesh Deliklerini Duzelt")]
        public static void FixFurnitureHoles()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[NavMeshChecker] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var report = new StringBuilder("[NavMeshChecker] Deligi olmayan esyalara oyan NavMesh engeli:\n");

            foreach (Collider collider in FindFurniture())
            {
                if (HasCarvingObstacle(collider.gameObject)) continue;

                // Kilitli (pasif) esya bake'e giremez; aktif olup altinda yine de yurunebilir alan kalan
                // esya da (or. yerden yuksekte duran raf) kendi deligini calisma aninda acmali.
                bool needsObstacle = !collider.gameObject.activeSelf || IsWalkableUnder(collider);
                if (!needsObstacle) continue;

                EnsureCarvingObstacle(collider.gameObject);
                report.AppendLine($"  + {collider.name}");
            }

            EditorSceneManager.MarkAllScenesDirty();
            report.AppendLine("Sahneyi kaydet (Ctrl+S). Sonra 'NavMesh Kontrol Et' ile dogrula.");
            Debug.Log(report.ToString());
        }

        [MenuItem("Kan Bagi/NavMesh'i Yeniden Bake Et")]
        public static void BakeWithFurniture()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[NavMeshChecker] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            NavMeshSurface[] surfaces = Object.FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
            if (surfaces.Length == 0)
            {
                Debug.LogError("[NavMeshChecker] Sahnede NavMeshSurface yok (Zemin objesinde olmali).");
                return;
            }

            var report = new StringBuilder("[NavMeshChecker] NavMesh yeniden bake ediliyor. Yurunemez isaretlenen esyalar:\n");
            int notWalkable = NavMesh.GetAreaFromName("Not Walkable");

            Collider[] furniture = FindFurniture();
            foreach (Collider collider in furniture)
            {
                EnsureNotWalkable(collider.gameObject, notWalkable);
                report.AppendLine($"  {collider.name}{(collider.gameObject.activeSelf ? "" : " (kilitli)")}");
            }

            // Engelli objeler (kumar masasi, bardak rafi...) da bake'e girsin.
            foreach (NavMeshSurface surface in surfaces)
            {
                Undo.RecordObject(surface, "NavMesh Bake Ayari");
                surface.ignoreNavMeshObstacle = false;
            }

            // Pasif obje bake'e giremez: kilitli esyalar sadece kaynaklar toplanirken acilir.
            // StartBakingSurfaces kaynaklari hemen toplar, bake'in geri kalani arka planda surer.
            List<GameObject> locked = furniture.Select(c => c.gameObject).Where(go => !go.activeSelf).Distinct().ToList();
            foreach (GameObject go in locked) go.SetActive(true);

            try
            {
                NavMeshAssetManager.instance.StartBakingSurfaces(surfaces.Cast<Object>().ToArray());
            }
            finally
            {
                foreach (GameObject go in locked) go.SetActive(false);
            }

            EditorSceneManager.MarkAllScenesDirty();
            report.AppendLine("Bake bitince sahneyi kaydet (Ctrl+S), sonra 'NavMesh Kontrol Et' ile dogrula.");
            Debug.Log(report.ToString());
        }

        /// <summary>Esyaya "Not Walkable" alanli NavMeshModifier ekler: alti da tepesi de yurunemez olur.</summary>
        private static void EnsureNotWalkable(GameObject target, int notWalkableArea)
        {
            NavMeshModifier modifier = target.GetComponent<NavMeshModifier>();
            if (modifier == null) modifier = Undo.AddComponent<NavMeshModifier>(target);

            modifier.overrideArea = true;
            modifier.area = notWalkableArea;
            modifier.applyToChildren = true;
            EditorUtility.SetDirty(modifier);
        }

        /// <summary>
        /// Objeye, collider'inin boyutunda kutu seklinde ve oyma (carve) acik NavMeshObstacle ekler.
        /// Kumarhane kurulumu da bunu kullanir.
        /// </summary>
        public static void EnsureCarvingObstacle(GameObject target)
        {
            NavMeshObstacle obstacle = target.GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = Undo.AddComponent<NavMeshObstacle>(target);

            obstacle.shape = NavMeshObstacleShape.Box;

            // Engel boyutu collider'dan alinir (yerel olcu; transform olcegi Unity'de zaten uygulanir).
            if (target.TryGetComponent(out BoxCollider box))
            {
                obstacle.center = box.center;
                obstacle.size = box.size;
            }
            else
            {
                obstacle.center = Vector3.zero;
                obstacle.size = Vector3.one;
            }

            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            EditorUtility.SetDirty(obstacle);
        }

        /// <summary>Mobilya sayilan collider'lar: kok obje, NPC/oyuncu degil, zemin kadar buyuk degil.</summary>
        private static Collider[] FindFurniture()
        {
            var result = new System.Collections.Generic.List<Collider>();

            foreach (Collider collider in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Sadece kok objeler (mobilya). Cocuklar (okey takimi, tabela) ve oyuncu atlanir.
                if (collider.transform.parent != null || collider is CharacterController) continue;
                if (collider.GetComponent<NavMeshAgent>() != null) continue;

                Bounds bounds = GetBounds(collider);
                if (bounds.size.x > MaxFurnitureSize || bounds.size.z > MaxFurnitureSize) continue;

                result.Add(collider);
            }

            return result.ToArray();
        }

        /// <summary>Esyanin tam ortasinin altinda (taban hizasinda) yurunebilir NavMesh var mi?</summary>
        private static bool IsWalkableUnder(Collider collider)
        {
            Bounds bounds = GetBounds(collider);
            Vector3 floorPoint = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return NavMesh.SamplePosition(floorPoint, out _, 0.3f, NavMesh.AllAreas);
        }

        private static bool HasCarvingObstacle(GameObject target) =>
            target.TryGetComponent(out NavMeshObstacle obstacle) && obstacle.carving;

        /// <summary>Pasif objelerde Collider/Renderer bounds bos gelir; birim kup varsayimiyla transform'dan hesaplanir.</summary>
        private static Bounds GetBounds(Collider collider)
        {
            if (collider.gameObject.activeInHierarchy) return collider.bounds;

            Transform t = collider.transform;
            return new Bounds(t.position, t.lossyScale);
        }
    }
}
