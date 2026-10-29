using System.Collections.Generic;
using System.Linq;
using System.Text;
using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Items;
using KanBagi.Shop;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D5a - Dukkanin temel esyalarini (cay ocagi, lavabo, bardak rafi, depo, yonetim PC'si) tasinabilir esyaya cevirir:
    ///  1) Her biri icin prefab (PlacedFurniture + NavMeshModifier "Not Walkable") ve FurnitureData (Station, sinir 1)
    ///  2) EsyaVeritabani'na ve VarsayilanDuzen'e (bugunku yer/aci) eklenir
    ///  3) Bardak rafi: ustundeki tepsi yerleri ve tepsiler rafin COCUGU yapilir (raf tasininca birlikte gider)
    ///  4) Sahnedeki sabit objeler silinir; oyun basinda FurnitureManager olusturur
    /// Kilitli makineler (kahve, oralet, sahlep) ve kumar masasi BU ADIMDA donusmez (D5b / D6).
    /// Bir kez calistirilir; tasinabilir hale gelmis esyayi atlar.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Istasyonlari Esyaya Cevir
    /// </summary>
    public static class StationFurnitureBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string DatabasePath = DataFolder + "/EsyaVeritabani.asset";
        private const string LayoutPath = DataFolder + "/VarsayilanDuzen.asset";
        private const float TrayOnSpotDistance = 0.3f;

        [MenuItem("Kan Bagi/Istasyonlari Esyaya Cevir")]
        public static void Convert()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[StationFurnitureBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<FurnitureDatabase>(DatabasePath);
            var layout = AssetDatabase.LoadAssetAtPath<FurnitureLayout>(LayoutPath);
            if (database == null || layout == null)
            {
                Debug.LogError("[StationFurnitureBuilder] EsyaVeritabani veya VarsayilanDuzen yok. Once 'Esya Altyapisini Kur'.");
                return;
            }

            List<GameObject> targets = FindTargets();
            if (targets.Count == 0)
            {
                Debug.LogWarning("[StationFurnitureBuilder] Donusturulecek sabit istasyon yok; zaten cevrilmis olabilir.");
                return;
            }

            var report = new StringBuilder($"[StationFurnitureBuilder] {targets.Count} istasyon esyaya cevrildi:\n");
            var databaseData = new SerializedObject(database);
            var layoutData = new SerializedObject(layout);

            foreach (GameObject target in targets)
            {
                if (target.TryGetComponent(out GlassShelf shelf)) report.Append(AttachTraySpots(shelf));

                GameObject prefab = BuildPrefab(target);
                if (prefab == null) continue;

                FurnitureData data = CreateFurnitureData(target.name, prefab);
                AppendUnique(databaseData.FindProperty("furniture"), data);
                AppendLayoutEntry(layoutData.FindProperty("entries"), data, target.transform);

                report.AppendLine($"  {target.name}: prefab + {data.name} + varsayilan duzen ({target.transform.position})");
                Undo.DestroyObjectImmediate(target);
            }

            databaseData.ApplyModifiedPropertiesWithoutUndo();
            layoutData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();

            EditorSceneManager.MarkAllScenesDirty();
            report.AppendLine("Sahneyi kaydet (Ctrl+S). Bu esyalar artik oyun basinda FurnitureManager tarafindan olusturulur.");
            Debug.Log(report.ToString());
        }

        /// <summary>Acik (kilitli olmayan) sabit istasyonlar: cay ocagi, lavabo, bardak rafi, depo, PC.</summary>
        private static List<GameObject> FindTargets()
        {
            var targets = new List<GameObject>();

            void AddAll<T>() where T : Component
            {
                foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    GameObject go = component.gameObject;
                    if (go.transform.parent != null || go.GetComponent<PlacedFurniture>() != null) continue;
                    if (!targets.Contains(go)) targets.Add(go);
                }
            }

            AddAll<TeaStove>();
            AddAll<Sink>();
            AddAll<GlassShelf>();
            AddAll<StorageRack>();
            AddAll<ComputerStation>();
            return targets.OrderBy(go => go.name).ToList();
        }

        /// <summary>Tepsi yerleri ve ustlerindeki tepsiler rafin cocugu olur (dunya olcegi korunur, tepsi ezilmez).</summary>
        private static string AttachTraySpots(GlassShelf shelf)
        {
            Tray[] trays = Object.FindObjectsByType<Tray>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int spotCount = 0;
            int trayCount = 0;

            foreach (TraySpot spot in Object.FindObjectsByType<TraySpot>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (spot.transform.parent == shelf.transform) continue;

                Undo.SetTransformParent(spot.transform, shelf.transform, "Tepsi Yeri");
                spotCount++;

                foreach (Tray tray in trays)
                {
                    if (tray.transform.parent != null) continue;
                    if (Vector3.Distance(tray.transform.position, spot.transform.position) > TrayOnSpotDistance) continue;

                    Undo.SetTransformParent(tray.transform, spot.transform, "Tepsi");
                    trayCount++;
                }
            }

            return spotCount > 0 ? $"  {shelf.name}: {spotCount} tepsi yeri + {trayCount} tepsi rafin cocugu yapildi\n" : "";
        }

        private static GameObject BuildPrefab(GameObject target)
        {
            GameObject copy = Object.Instantiate(target);
            copy.name = target.name;
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (!copy.TryGetComponent(out NavMeshModifier modifier)) modifier = copy.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");

            if (!copy.TryGetComponent(out PlacedFurniture _)) copy.AddComponent<PlacedFurniture>();

            string path = $"{PrefabFolder}/{target.name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, path);
            Object.DestroyImmediate(copy);

            if (saved == null) Debug.LogError($"[StationFurnitureBuilder] Prefab kaydedilemedi: {path}");
            return saved;
        }

        /// <summary>Gorunen ad sonradan asset'te Turkcelestirilir (kod ASCII kurali). Satilmiyor: fiyat 0, sinir 1.</summary>
        private static FurnitureData CreateFurnitureData(string objectName, GameObject prefab)
        {
            string path = $"{DataFolder}/{objectName}Esya.asset";
            var data = AssetDatabase.LoadAssetAtPath<FurnitureData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<FurnitureData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var serialized = new SerializedObject(data);
            serialized.FindProperty("displayName").stringValue = objectName;
            serialized.FindProperty("category").enumValueIndex = (int)FurnitureCategory.Station;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("price").intValue = 0;
            serialized.FindProperty("baseLimit").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void AppendUnique(SerializedProperty list, Object value)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;
        }

        private static void AppendLayoutEntry(SerializedProperty entries, FurnitureData data, Transform source)
        {
            entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            entry.FindPropertyRelative("furniture").objectReferenceValue = data;
            entry.FindPropertyRelative("position").vector3Value = source.position;
            entry.FindPropertyRelative("yaw").floatValue = source.eulerAngles.y;
        }
    }
}
