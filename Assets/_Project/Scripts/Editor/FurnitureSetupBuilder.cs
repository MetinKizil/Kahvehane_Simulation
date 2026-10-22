using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Shop;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D2 - Esya altyapisi kurulumu. Sahnedeki SABIT masalari veriden olusturulan esyalara cevirir:
    ///  1) Masa1'den 'Masa' prefab'i (Table + collider + NavMeshModifier + okey/iskambil/domino parcalari + UpgradeVisual)
    ///  2) Veri: MasaEsya (FurnitureData), EsyaVeritabani (FurnitureDatabase), VarsayilanDuzen (FurnitureLayout)
    ///  3) Managers: FurnitureManager, NavMeshRuntimeBaker
    ///  4) Sahnedeki masalar ve onlari/parcalarini acan eski UpgradeUnlocker'lar silinir
    ///     (okey'in sabir, iskambil/domino'nun ikinci tur ETKILERI ayri bilesenler; dokunulmaz)
    ///
    /// Hangi upgrade'in ne actigi asset adiyla degil, mevcut UpgradeUnlocker hedeflerinden cikarilir.
    /// Bir kez calistirilir; sahnede sabit masa kalmadiysa "zaten kurulu" der.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Esya Altyapisini Kur
    /// </summary>
    public static class FurnitureSetupBuilder
    {
        private const string PrefabPath = "Assets/_Project/Prefabs/Masa.prefab";
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string FurnitureDataPath = DataFolder + "/MasaEsya.asset";
        private const string DatabasePath = DataFolder + "/EsyaVeritabani.asset";
        private const string LayoutPath = DataFolder + "/VarsayilanDuzen.asset";

        private const int TablePrice = 100;

        [MenuItem("Kan Bagi/Esya Altyapisini Kur")]
        public static void SetUp()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[FurnitureSetupBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            if (upgradeManager == null)
            {
                Debug.LogError("[FurnitureSetupBuilder] Sahnede UpgradeManager bulunamadi ('Managers' objesi).");
                return;
            }

            Table[] tables = Object.FindObjectsByType<Table>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                   .Where(table => table.GetComponent<PlacedFurniture>() == null)
                                   .OrderBy(table => table.name)
                                   .ToArray();

            if (tables.Length == 0)
            {
                Debug.LogWarning("[FurnitureSetupBuilder] Sahnede sabit masa yok; esya altyapisi zaten kurulu olabilir.");
                return;
            }

            GameObject managers = upgradeManager.gameObject;
            var report = new StringBuilder("[FurnitureSetupBuilder] Esya altyapisi kuruldu:\n");

            // --- Eski acicilari cozumle: masayi acan (Yeni Masa) ve masa parcalarini acanlar ---
            var tableObjects = new HashSet<GameObject>(tables.Select(table => table.gameObject));
            UpgradeData grantUpgrade = null;
            Table grantTable = null;
            var pieceUpgrades = new Dictionary<string, UpgradeData>();
            var unlockersToRemove = new List<UpgradeUnlocker>();

            foreach (UpgradeUnlocker unlocker in managers.GetComponents<UpgradeUnlocker>())
            {
                var serialized = new SerializedObject(unlocker);
                var upgrade = serialized.FindProperty("requiredUpgrade").objectReferenceValue as UpgradeData;
                SerializedProperty targets = serialized.FindProperty("targets");
                bool touchesTables = false;

                for (int i = 0; i < targets.arraySize; i++)
                {
                    var target = targets.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
                    if (target == null) continue;

                    if (tableObjects.Contains(target))
                    {
                        grantUpgrade = upgrade;
                        grantTable = target.GetComponent<Table>();
                        touchesTables = true;
                    }
                    else if (target.transform.parent != null && tableObjects.Contains(target.transform.parent.gameObject))
                    {
                        pieceUpgrades[PieceName(target.name)] = upgrade;
                        touchesTables = true;
                    }
                }

                if (touchesTables) unlockersToRemove.Add(unlocker);
            }

            // --- Prefab: ilk ACIK masa sablon olur (kilitli masa degil) ---
            Table template = tables.FirstOrDefault(table => table != grantTable);
            if (template == null) template = tables[0];
            GameObject prefab = BuildTablePrefab(template, pieceUpgrades);
            if (prefab == null) return;

            report.AppendLine($"  Prefab: {PrefabPath} (sablon: {template.name}, parcalar: {string.Join(", ", pieceUpgrades.Keys)})");

            // --- Veri ---
            FurnitureData tableData = CreateOrLoad<FurnitureData>(FurnitureDataPath);
            var dataSerialized = new SerializedObject(tableData);
            dataSerialized.FindProperty("displayName").stringValue = "Masa";
            dataSerialized.FindProperty("category").enumValueIndex = (int)FurnitureCategory.Table;
            dataSerialized.FindProperty("prefab").objectReferenceValue = prefab;
            dataSerialized.FindProperty("price").intValue = TablePrice;
            dataSerialized.ApplyModifiedPropertiesWithoutUndo();

            FurnitureDatabase database = CreateOrLoad<FurnitureDatabase>(DatabasePath);
            SetObjectList(new SerializedObject(database), "furniture", new Object[] { tableData });

            FurnitureLayout layout = CreateOrLoad<FurnitureLayout>(LayoutPath);
            var layoutSerialized = new SerializedObject(layout);
            SerializedProperty entries = layoutSerialized.FindProperty("entries");
            Table[] startingTables = tables.Where(table => table != grantTable).ToArray();
            entries.arraySize = startingTables.Length;

            for (int i = 0; i < startingTables.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("furniture").objectReferenceValue = tableData;
                entry.FindPropertyRelative("position").vector3Value = startingTables[i].transform.position;
                entry.FindPropertyRelative("yaw").floatValue = startingTables[i].transform.eulerAngles.y;
            }

            layoutSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tableData);
            EditorUtility.SetDirty(database);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssets();

            report.AppendLine($"  Varsayilan duzen: {string.Join(", ", startingTables.Select(table => table.name))}");

            // --- Managers ---
            FurnitureManager furnitureManager = EnsureComponent<FurnitureManager>(managers);
            var managerSerialized = new SerializedObject(furnitureManager);
            managerSerialized.FindProperty("database").objectReferenceValue = database;
            managerSerialized.FindProperty("defaultLayout").objectReferenceValue = layout;
            managerSerialized.ApplyModifiedProperties();

            NavMeshRuntimeBaker baker = EnsureComponent<NavMeshRuntimeBaker>(managers);
            var bakerSerialized = new SerializedObject(baker);
            bakerSerialized.FindProperty("surface").objectReferenceValue = Object.FindFirstObjectByType<NavMeshSurface>();
            bakerSerialized.ApplyModifiedProperties();

            // D4'ten beri masa PC'deki ESYALAR katalogundan alinir; "Yeni Masa" upgrade'i ve hibesi kalkti.
            if (grantUpgrade != null)
                report.AppendLine($"  Not: '{grantUpgrade.name}' artik kullanilmiyor; masa ESYALAR katalogundan alinir.");

            // --- Eski sabit duzeni sil ---
            foreach (UpgradeUnlocker unlocker in unlockersToRemove)
                Undo.DestroyObjectImmediate(unlocker);

            foreach (Table table in tables)
                Undo.DestroyObjectImmediate(table.gameObject);

            report.AppendLine($"  Silindi: {tables.Length} sahne masasi, {unlockersToRemove.Count} eski UpgradeUnlocker");

            EditorSceneManager.MarkSceneDirty(managers.scene);
            report.AppendLine("Sahneyi kaydet (Ctrl+S). Masalar artik oyun basinda FurnitureManager tarafindan olusturulur.");
            Debug.Log(report.ToString());
        }

        /// <summary>"OkeyTakimi1" -> "OkeyTakimi": sahnede numaralanmis parca adlari prefab'da tek ad olur.</summary>
        private static string PieceName(string objectName) => Regex.Replace(objectName, @"\d+$", "");

        /// <summary>Sablon masanin kopyasindan prefab: parcalar kapali baslar, UpgradeVisual acar.</summary>
        private static GameObject BuildTablePrefab(Table template, Dictionary<string, UpgradeData> pieceUpgrades)
        {
            GameObject copy = Object.Instantiate(template.gameObject);
            copy.name = "Masa";
            copy.SetActive(true);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Eski runtime oyma engeli varsa gereksiz: NavMesh oyun icinde yeniden kuruluyor.
            foreach (var obstacle in copy.GetComponents<UnityEngine.AI.NavMeshObstacle>())
                Object.DestroyImmediate(obstacle);

            if (copy.GetComponent<NavMeshModifier>() == null)
            {
                var modifier = copy.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            }

            copy.AddComponent<PlacedFurniture>();
            var visual = copy.AddComponent<UpgradeVisual>();
            var visualSerialized = new SerializedObject(visual);
            SerializedProperty visualEntries = visualSerialized.FindProperty("entries");
            visualEntries.arraySize = 0;

            foreach (Transform child in copy.transform.Cast<Transform>().ToArray())
            {
                string pieceName = PieceName(child.name);
                if (!pieceUpgrades.TryGetValue(pieceName, out UpgradeData upgrade)) continue;

                child.name = pieceName;
                child.gameObject.SetActive(false);

                visualEntries.arraySize++;
                SerializedProperty entry = visualEntries.GetArrayElementAtIndex(visualEntries.arraySize - 1);
                entry.FindPropertyRelative("upgrade").objectReferenceValue = upgrade;
                entry.FindPropertyRelative("target").objectReferenceValue = child.gameObject;
            }

            visualSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, PrefabPath);
            Object.DestroyImmediate(copy);
            AssetDatabase.SaveAssets();

            if (saved == null) Debug.LogError("[FurnitureSetupBuilder] Masa prefab'i kaydedilemedi.");
            return saved;
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static void SetObjectList(SerializedObject serialized, string property, Object[] values)
        {
            SerializedProperty list = serialized.FindProperty(property);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Bilesen yoksa ekler. (?? kullanilmaz: editorde eksik bilesen "sahte null" doner.)</summary>
        private static T EnsureComponent<T>(GameObject owner) where T : Component
        {
            return owner.TryGetComponent(out T existing) ? existing : Undo.AddComponent<T>(owner);
        }
    }
}
