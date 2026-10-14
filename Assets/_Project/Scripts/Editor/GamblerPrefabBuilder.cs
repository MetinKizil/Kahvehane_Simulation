using KanBagi.Customers;
using KanBagi.Illegal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Gece kumarhanesini tek tikla kurar:
    ///  1) Kumarbaz prefab'ini KOD ILE uretir (kapsul + NavMeshAgent + Gambler)
    ///  2) Sahnedeki her kumar masasina GamblerSpawner ekler (yoksa) ve
    ///     prefab'i + giris noktasini (musteri spawner'inin giris noktasi) baglar.
    /// Kumarbazlar calisma aninda uretilir; sahneye elle konmaz.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Kumarhaneyi Kur
    /// </summary>
    public static class GamblerPrefabBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string MaterialFolder = ProjectFolder + "/Materials";
        private const string PrefabPath = PrefabFolder + "/Kumarbaz.prefab";
        private const string BodyMaterialPath = MaterialFolder + "/KumarbazGovde.mat";

        private static readonly Color BodyColor = new Color(0.18f, 0.32f, 0.22f);

        // Musteri prefab'iyla ayni yurume ayarlari.
        private const float AgentRadius = 0.4f;
        private const float AgentHeight = 2f;
        private const float AgentBaseOffset = 1f;
        private const float AgentSpeed = 2.5f;
        private const float AgentStoppingDistance = 0.2f;

        [MenuItem("Kan Bagi/Kumarhaneyi Kur")]
        public static void SetUpGamblingHall()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[GamblerPrefabBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            GameObject prefab = BuildGamblerPrefab();
            if (prefab == null) return;

            GamblingTable[] tables = Object.FindObjectsByType<GamblingTable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (tables.Length == 0)
            {
                Debug.LogWarning("[GamblerPrefabBuilder] Sahnede GamblingTable bulunamadi. Prefab uretildi ama baglanmadi.");
                return;
            }

            Transform entryPoint = FindEntryPoint();
            if (entryPoint == null)
            {
                Debug.LogError("[GamblerPrefabBuilder] Musteri giris noktasi bulunamadi (CustomerSpawner yok). " +
                               "GamblerSpawner > Entry Point alanini elle bagla.");
            }

            foreach (GamblingTable table in tables)
            {
                WireSpawner(table, prefab, entryPoint);
                EnsureNavMeshObstacle(table);
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[GamblerPrefabBuilder] Kumarbaz prefab'i olusturuldu ({PrefabPath}), " +
                      $"{tables.Length} masaya GamblerSpawner + NavMeshObstacle baglandi. Sahneyi kaydet (Ctrl+S).");
        }

        private static GameObject BuildGamblerPrefab()
        {
            EnsureFolders();

            // Govde: kapsul. Collider kalir - oyuncu icinden gecmesin.
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "Kumarbaz";

            var bodyRenderer = root.GetComponent<MeshRenderer>();
            if (bodyRenderer != null)
                bodyRenderer.sharedMaterial = GetOrCreateMaterial(BodyMaterialPath, "KumarbazGovde", BodyColor);

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = AgentRadius;
            agent.height = AgentHeight;
            agent.baseOffset = AgentBaseOffset;
            agent.speed = AgentSpeed;
            agent.stoppingDistance = AgentStoppingDistance;

            root.AddComponent<Gambler>();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (saved == null) Debug.LogError("[GamblerPrefabBuilder] Prefab kaydedilemedi.");
            return saved;
        }

        /// <summary>Kumarbazlar musterilerle ayni kapidan girip cikar.</summary>
        private static Transform FindEntryPoint()
        {
            var customerSpawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            if (customerSpawner == null) return null;

            var serialized = new SerializedObject(customerSpawner);
            var spawnPoint = serialized.FindProperty("spawnPoint")?.objectReferenceValue as Transform;

            return spawnPoint != null ? spawnPoint : customerSpawner.transform;
        }

        private static void WireSpawner(GamblingTable table, GameObject prefab, Transform entryPoint)
        {
            GamblerSpawner spawner = table.GetComponent<GamblerSpawner>();
            if (spawner == null) spawner = Undo.AddComponent<GamblerSpawner>(table.gameObject);

            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("gamblerPrefab").objectReferenceValue = prefab;
            if (entryPoint != null) serialized.FindProperty("entryPoint").objectReferenceValue = entryPoint;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>
        /// Masa upgrade alinana kadar pasif oldugu icin NavMesh bake'inde delik acmamisti;
        /// kumarbazlar icinden geciyordu. Oyma (carve) yapan engel, masa aktif olunca deligini
        /// CALISMA ANINDA kendisi acar - yeniden bake gerekmez.
        /// </summary>
        private static void EnsureNavMeshObstacle(GamblingTable table)
        {
            NavMeshObstacle obstacle = table.GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = Undo.AddComponent<NavMeshObstacle>(table.gameObject);

            obstacle.shape = NavMeshObstacleShape.Box;

            // Engel boyutu collider'dan alinir (yerel olcu; transform olcegi Unity'de zaten uygulanir).
            if (table.TryGetComponent(out BoxCollider box))
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

        private static Material GetOrCreateMaterial(string path, string materialName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Standard");

            if (shader == null) return null;

            var material = new Material(shader) { name = materialName };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            else
                material.color = color;

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ProjectFolder))
                AssetDatabase.CreateFolder("Assets", "_Project");

            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(ProjectFolder, "Prefabs");

            if (!AssetDatabase.IsValidFolder(MaterialFolder))
                AssetDatabase.CreateFolder(ProjectFolder, "Materials");
        }
    }
}
