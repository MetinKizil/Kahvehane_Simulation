using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Servis tepsisini tek tikla kurar:
    ///  1) 'Tepsi' prefab'ini KOD ILE uretir (olceksiz kok + collider, ince tahta govde, HoldableItem + Tray)
    ///  2) Bardak rafinin ustune yan yana iki tepsi yeri (TraySpot) ve iki tepsi koyar (co-op: oyuncu basina bir)
    /// Konum ve boyutlar rafin Renderer.bounds'undan hesaplanir; elle koordinat verilmez.
    /// Tekrar calistirilabilir: eski tepsi yerlerini ve sahnedeki tepsileri silip yeniden kurar.
    ///
    /// Tepsi yerleri simdilik sabit; D3 (yerlestirme modu) ile tezgahlara da konabilecek.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Tepsiyi Kur
    /// </summary>
    public static class TrayBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Tepsi.prefab";
        private const string SpotName = "TepsiYeri";
        private const int TrayCount = 2;

        private static readonly Vector3 TraySize = new Vector3(0.45f, 0.03f, 0.32f);
        private const float SpotThickness = 0.02f;
        private static readonly Color WoodColor = new Color(0.45f, 0.3f, 0.18f);

        // Bardak prefab'inin yuksekligi okunamazsa kullanilir (CayBardagi: 0.1 m).
        private const float FallbackGlassLift = 0.05f;

        [MenuItem("Kan Bagi/Tepsiyi Kur")]
        public static void SetUpTrays()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[TrayBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            GlassShelf shelf = Object.FindFirstObjectByType<GlassShelf>(FindObjectsInactive.Include);
            if (shelf == null || !shelf.TryGetComponent(out Renderer shelfRenderer))
            {
                Debug.LogError("[TrayBuilder] Sahnede Renderer'li bir GlassShelf (bardak rafi) bulunamadi.");
                return;
            }

            GameObject prefab = BuildTrayPrefab(GetGlassLift(shelf));
            if (prefab == null) return;

            RemoveOldSetup();

            Bounds bounds = shelfRenderer.bounds;
            float slotWidth = bounds.size.x / TrayCount;
            var spotSize = new Vector3(Mathf.Min(TraySize.x, slotWidth - 0.02f), SpotThickness, Mathf.Min(TraySize.z, bounds.size.z));

            for (int i = 0; i < TrayCount; i++)
            {
                var position = new Vector3(bounds.min.x + slotWidth * (i + 0.5f), bounds.max.y, bounds.center.z);
                CreateSpot(i + 1, position, shelf.transform.rotation, spotSize);
                PlaceTray(prefab, position + Vector3.up * SpotThickness, shelf.transform.rotation);
            }

            EditorSceneManager.MarkSceneDirty(shelf.gameObject.scene);
            Debug.Log($"[TrayBuilder] Tepsi prefab'i olusturuldu ({PrefabPath}), '{shelf.name}' ustune " +
                      $"{TrayCount} tepsi yeri + {TrayCount} tepsi kondu. Sahneyi kaydet (Ctrl+S).");
        }

        /// <summary>
        /// Kok OLCEKSIZ tutulur: bardaklar tepsinin cocugu olunca ezilmesin. Gorsel govde ayri cocuk,
        /// collider kokte (HoldableItem collider'lari kokten toplar).
        /// </summary>
        private static GameObject BuildTrayPrefab(float glassLift)
        {
            var root = new GameObject("Tepsi");

            var collider = root.AddComponent<BoxCollider>();
            collider.size = TraySize;
            collider.center = new Vector3(0f, TraySize.y * 0.5f, 0f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Govde";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, worldPositionStays: false);
            body.transform.localPosition = new Vector3(0f, TraySize.y * 0.5f, 0f);
            body.transform.localScale = TraySize;

            Material wood = TableGamesBuilder.GetOrCreateMaterial("TepsiAhsap", WoodColor);
            if (wood != null) body.GetComponent<MeshRenderer>().sharedMaterial = wood;

            var holdable = root.AddComponent<HoldableItem>();
            var holdableData = new SerializedObject(holdable);
            holdableData.FindProperty("displayName").stringValue = "Tepsi";
            holdableData.FindProperty("pickupPrompt").stringValue = "Tepsiyi Al";
            holdableData.ApplyModifiedPropertiesWithoutUndo();

            var tray = root.AddComponent<Tray>();
            var trayData = new SerializedObject(tray);
            trayData.FindProperty("surfaceHeight").floatValue = TraySize.y;
            trayData.FindProperty("glassLift").floatValue = glassLift;
            trayData.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();

            if (saved == null) Debug.LogError("[TrayBuilder] Tepsi prefab'i kaydedilemedi.");
            return saved;
        }

        /// <summary>Bardagin pivotu ortasinda: tepsi yuzeyine oturmasi icin yuksekliginin yarisi kadar kaldirilir.</summary>
        private static float GetGlassLift(GlassShelf shelf)
        {
            var glassPrefab = new SerializedObject(shelf).FindProperty("glassPrefab").objectReferenceValue as GameObject;
            if (glassPrefab == null || !glassPrefab.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
                return FallbackGlassLift;

            return meshFilter.sharedMesh.bounds.extents.y * glassPrefab.transform.localScale.y;
        }

        private static void RemoveOldSetup()
        {
            foreach (TraySpot spot in Object.FindObjectsByType<TraySpot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Undo.DestroyObjectImmediate(spot.gameObject);

            foreach (Tray tray in Object.FindObjectsByType<Tray>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Undo.DestroyObjectImmediate(tray.gameObject);
        }

        /// <summary>Rafin ustunde ince bir collider: tepsi kalkinca isin buraya carpar ve "Tepsiyi Birak" der.</summary>
        private static void CreateSpot(int index, Vector3 position, Quaternion rotation, Vector3 size)
        {
            var spot = new GameObject($"{SpotName}{index}");
            Undo.RegisterCreatedObjectUndo(spot, "Tepsi Yeri");
            spot.transform.SetPositionAndRotation(position, rotation);

            var collider = spot.AddComponent<BoxCollider>();
            collider.size = size;
            collider.center = new Vector3(0f, size.y * 0.5f, 0f);

            spot.AddComponent<TraySpot>();
        }

        private static void PlaceTray(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            var tray = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(tray, "Tepsi");
            tray.transform.SetPositionAndRotation(position, rotation);
        }
    }
}
