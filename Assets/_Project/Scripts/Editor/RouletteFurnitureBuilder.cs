using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Illegal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D6d-2 - Bodrumdaki rulet masasini ESYALAR sayfasindan alinan esyaya cevirir:
    ///  1) Sahnedeki masadan prefab (acik, PlacedFurniture; GamblerSpawner'in sahne baglantisi bos - kapiyi kendisi bulur)
    ///  2) RuletMasasiEsya: 800 TL, her kopya +200, sinir 2, Kumar Odasi gerekli, bolge: kumar odasi
    ///     -> EsyaVeritabani + EsyaKatalogu (varsayilan duzene GIRMEZ, satin alinir)
    ///  3) Bodrum zemini "kumar odasi" bolgesi olarak isaretlenir; sahnedeki masa silinir
    /// Bir kez calistirilir; sahnede esyaya cevrilmemis masa yoksa bir sey yapmaz.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Rulet Masasini Esyaya Cevir
    /// </summary>
    public static class RouletteFurnitureBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string DatabasePath = DataFolder + "/EsyaVeritabani.asset";
        private const string CatalogPath = DataFolder + "/EsyaKatalogu.asset";
        private const string UpgradePath = DataFolder + "/GizliKumarhaneUpgrade.asset";
        private const string DataName = "RuletMasasiEsya";
        private const string BasementFloorPath = "Bodrum/KumarOdasi/Zemin";

        private const int Price = 800;
        private const int PriceStep = 200;
        private const int Limit = 2;

        [MenuItem("Kan Bagi/Rulet Masasini Esyaya Cevir")]
        public static void Convert()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[RouletteFurnitureBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<FurnitureDatabase>(DatabasePath);
            var catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(CatalogPath);
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(UpgradePath);
            GameObject basementFloor = FindInScene(BasementFloorPath);

            if (database == null || catalog == null || upgrade == null || basementFloor == null)
            {
                Debug.LogError("[RouletteFurnitureBuilder] EsyaVeritabani, EsyaKatalogu, GizliKumarhaneUpgrade veya " +
                               $"'{BasementFloorPath}' bulunamadi. Once 'Bodrumu Kur'.");
                return;
            }

            GamblingTable table = null;
            foreach (GamblingTable candidate in Object.FindObjectsByType<GamblingTable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.GetComponent<PlacedFurniture>() == null) table = candidate;
            }

            if (table == null)
            {
                Debug.LogWarning("[RouletteFurnitureBuilder] Sahnede esyaya cevrilecek rulet masasi yok; zaten cevrilmis olabilir.");
                return;
            }

            GameObject prefab = BuildPrefab(table.gameObject);
            if (prefab == null) return;

            FurnitureData data = CreateData(prefab, upgrade);
            AppendUnique(database, "furniture", data);
            AppendUnique(catalog, "items", data);

            BasementBuilder.MarkGamblingFloor(basementFloor);
            Undo.DestroyObjectImmediate(table.gameObject);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(basementFloor.scene);
            Debug.Log($"[RouletteFurnitureBuilder] Rulet masasi esya oldu: {prefab.name}.prefab + {data.name} ({Price} TL, " +
                      $"her kopya +{PriceStep}, sinir {Limit}, Kumar Odasi gerekli, sadece bodruma). Sahnedeki masa silindi. " +
                      "Sahneyi kaydet (Ctrl+S), 'Metinleri Uygula', tekrar kaydet.");
        }

        private static GameObject BuildPrefab(GameObject target)
        {
            GameObject copy = Object.Instantiate(target);
            copy.name = target.name;
            copy.SetActive(true);
            copy.transform.SetParent(null);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (!copy.TryGetComponent(out PlacedFurniture _)) copy.AddComponent<PlacedFurniture>();

            // Prefab sahne objesi tutamaz; bos kalirsa GamblerSpawner musterilerin giris noktasini kendisi bulur.
            if (copy.TryGetComponent(out GamblerSpawner spawner))
            {
                var spawnerData = new SerializedObject(spawner);
                spawnerData.FindProperty("entryPoint").objectReferenceValue = null;
                spawnerData.ApplyModifiedPropertiesWithoutUndo();
            }

            string path = $"{PrefabFolder}/{target.name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, path);
            Object.DestroyImmediate(copy);

            if (saved == null) Debug.LogError($"[RouletteFurnitureBuilder] Prefab kaydedilemedi: {path}");
            return saved;
        }

        /// <summary>Gorunen ad ASCII yer tutucu; Turkcesi asset'te yazilir.</summary>
        private static FurnitureData CreateData(GameObject prefab, UpgradeData upgrade)
        {
            string path = $"{DataFolder}/{DataName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<FurnitureData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<FurnitureData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var serialized = new SerializedObject(data);
            SerializedProperty displayName = serialized.FindProperty("displayName");
            if (string.IsNullOrEmpty(displayName.stringValue)) displayName.stringValue = "Rulet Masasi";
            serialized.FindProperty("category").enumValueIndex = (int)FurnitureCategory.Station;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("price").intValue = Price;
            serialized.FindProperty("priceStep").intValue = PriceStep;
            serialized.FindProperty("requiredUpgrade").objectReferenceValue = upgrade;
            serialized.FindProperty("baseLimit").intValue = Limit;
            serialized.FindProperty("zone").enumValueIndex = (int)FurnitureZone.GamblingRoom;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void AppendUnique(Object owner, string listName, Object value)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty list = serialized.FindProperty(listName);
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }

        /// <summary>Kapali objeler dahil, "Kok/Cocuk/Torun" yoluyla sahnede arar.</summary>
        private static GameObject FindInScene(string path)
        {
            string[] parts = path.Split('/');
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name != parts[0]) continue;

                Transform found = root.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                if (found != null) return found.gameObject;
            }

            return null;
        }
    }
}
