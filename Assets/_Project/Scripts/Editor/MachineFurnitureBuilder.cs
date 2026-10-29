using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// D5b - Upgrade ile acilan icecek makinelerini (oralet, kahve, sahlep) satin alinabilir esyaya cevirir:
    ///  1) Her makine icin prefab (PlacedFurniture + NavMeshModifier "Not Walkable") ve FurnitureData
    ///     (ad, fiyat ve itibar sarti eski upgrade'den; Station, sinir 1)
    ///  2) EsyaVeritabani'na ve EsyaKatalogu'na (ucuzdan pahaliya) eklenir; varsayilan duzene GIRMEZ (satin alinir)
    ///  3) Upgrade GelistirmeKatalogu'ndan cikar; malzemesi MalzemeKatalogu'nda "makine varsa gorunsun" olur
    ///  4) Icecek MenuManager listesinde yoksa eklenir (menu artik makinenin varligina bakar)
    ///  5) 'Managers'taki UpgradeUnlocker ve sahnedeki kapali makine silinir
    /// Bir kez calistirilir; makinesi kalmamis unlocker'i atlar. Kumar masasi unlocker'ina dokunmaz (D6).
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Makineleri Esyaya Cevir
    /// </summary>
    public static class MachineFurnitureBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string DatabasePath = DataFolder + "/EsyaVeritabani.asset";
        private const string CatalogPath = DataFolder + "/EsyaKatalogu.asset";
        private const string ShopCatalogPath = DataFolder + "/MalzemeKatalogu.asset";

        private readonly struct Machine
        {
            public readonly UpgradeUnlocker Unlocker;
            public readonly UpgradeData Upgrade;
            public readonly TeaStove Stove;

            public Machine(UpgradeUnlocker unlocker, UpgradeData upgrade, TeaStove stove)
            {
                Unlocker = unlocker;
                Upgrade = upgrade;
                Stove = stove;
            }
        }

        [MenuItem("Kan Bagi/Makineleri Esyaya Cevir")]
        public static void Convert()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[MachineFurnitureBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<FurnitureDatabase>(DatabasePath);
            var catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(CatalogPath);
            var shopCatalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(ShopCatalogPath);
            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            MenuManager menuManager = Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);

            if (database == null || catalog == null || shopCatalog == null || upgradeManager == null || menuManager == null)
            {
                Debug.LogError("[MachineFurnitureBuilder] EsyaVeritabani, EsyaKatalogu, MalzemeKatalogu, UpgradeManager " +
                               "veya MenuManager bulunamadi. Once 'Esya Altyapisini Kur' ve 'Esya Katalogunu Kur'.");
                return;
            }

            List<Machine> machines = FindMachines();
            if (machines.Count == 0)
            {
                Debug.LogWarning("[MachineFurnitureBuilder] Cevrilecek kilitli makine yok; zaten cevrilmis olabilir.");
                return;
            }

            var report = new StringBuilder($"[MachineFurnitureBuilder] {machines.Count} makine esyaya cevrildi:\n");
            var databaseData = new SerializedObject(database);
            var catalogData = new SerializedObject(catalog);
            var shopData = new SerializedObject(shopCatalog);
            var menuData = new SerializedObject(menuManager);
            var upgradeCatalog = new SerializedObject(upgradeManager).FindProperty("catalog").objectReferenceValue as UpgradeCatalog;
            SerializedObject upgradeData = upgradeCatalog != null ? new SerializedObject(upgradeCatalog) : null;

            foreach (Machine machine in machines)
            {
                GameObject target = machine.Stove.gameObject;
                GameObject prefab = BuildPrefab(target);
                if (prefab == null) continue;

                FurnitureData data = CreateFurnitureData(target.name, prefab, machine.Upgrade);
                AppendUnique(databaseData.FindProperty("furniture"), data);
                AppendUnique(catalogData.FindProperty("items"), data);

                bool upgradeRemoved = upgradeData != null && RemoveFromList(upgradeData.FindProperty("upgrades"), machine.Upgrade);
                bool consumableLocked = LockConsumable(shopData.FindProperty("entries"), machine.Stove.RequiredConsumable);
                bool menuAdded = AddToMenu(menuData.FindProperty("entries"), machine.Stove.Beverage);

                report.AppendLine($"  {target.name}: {data.name} ({data.Price} TL, itibar {data.RequiredReputation}) | " +
                                  $"gelistirmeden cikti: {upgradeRemoved} | malzeme kilidi: {consumableLocked} | menuye eklendi: {menuAdded}");

                Undo.DestroyObjectImmediate(machine.Unlocker);
                Undo.DestroyObjectImmediate(target);
            }

            databaseData.ApplyModifiedPropertiesWithoutUndo();
            catalogData.ApplyModifiedPropertiesWithoutUndo();
            shopData.ApplyModifiedPropertiesWithoutUndo();
            menuData.ApplyModifiedPropertiesWithoutUndo();
            upgradeData?.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(database);
            EditorUtility.SetDirty(catalog);
            EditorUtility.SetDirty(shopCatalog);
            if (upgradeCatalog != null) EditorUtility.SetDirty(upgradeCatalog);
            AssetDatabase.SaveAssets();

            EditorSceneManager.MarkAllScenesDirty();
            report.AppendLine("Sahneyi kaydet (Ctrl+S). Makineler artik PC > ESYALAR'dan satin alinir.");
            Debug.Log(report.ToString());
        }

        /// <summary>Hedefinde makine (TeaStove) olan unlocker'lar, ucuzdan pahaliya (katalog sirasi).</summary>
        private static List<Machine> FindMachines()
        {
            var machines = new List<Machine>();

            foreach (UpgradeUnlocker unlocker in Object.FindObjectsByType<UpgradeUnlocker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var serialized = new SerializedObject(unlocker);
                var upgrade = serialized.FindProperty("requiredUpgrade").objectReferenceValue as UpgradeData;
                SerializedProperty targets = serialized.FindProperty("targets");
                if (upgrade == null || targets.arraySize != 1) continue;

                var target = targets.GetArrayElementAtIndex(0).objectReferenceValue as GameObject;
                if (target == null || !target.TryGetComponent(out TeaStove stove)) continue;
                if (target.GetComponent<PlacedFurniture>() != null) continue;

                machines.Add(new Machine(unlocker, upgrade, stove));
            }

            return machines.OrderBy(m => m.Upgrade.Cost).ToList();
        }

        /// <summary>Makine sahnede kapali durur; prefab acik kaydedilir.</summary>
        private static GameObject BuildPrefab(GameObject target)
        {
            GameObject copy = Object.Instantiate(target);
            copy.name = target.name;
            copy.SetActive(true);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            if (!copy.TryGetComponent(out NavMeshModifier modifier)) modifier = copy.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");

            if (!copy.TryGetComponent(out PlacedFurniture _)) copy.AddComponent<PlacedFurniture>();

            string path = $"{PrefabFolder}/{target.name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(copy, path);
            Object.DestroyImmediate(copy);

            if (saved == null) Debug.LogError($"[MachineFurnitureBuilder] Prefab kaydedilemedi: {path}");
            return saved;
        }

        /// <summary>Ad, fiyat ve itibar sarti eski upgrade'den gelir (kullanici karari: fiyatlar ayni kalsin).</summary>
        private static FurnitureData CreateFurnitureData(string objectName, GameObject prefab, UpgradeData upgrade)
        {
            string path = $"{DataFolder}/{objectName}Esya.asset";
            var data = AssetDatabase.LoadAssetAtPath<FurnitureData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<FurnitureData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var serialized = new SerializedObject(data);
            serialized.FindProperty("displayName").stringValue = upgrade.UpgradeName;
            serialized.FindProperty("category").enumValueIndex = (int)FurnitureCategory.Station;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("price").intValue = upgrade.Cost;
            serialized.FindProperty("requiredReputation").intValue = upgrade.RequiredReputation;
            serialized.FindProperty("baseLimit").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        /// <summary>Malzeme, onu kullanan makine dukkanda olana kadar PC'de gorunmez.</summary>
        private static bool LockConsumable(SerializedProperty entries, ItemData consumable)
        {
            bool locked = false;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("item").objectReferenceValue != consumable) continue;

                entry.FindPropertyRelative("requiresStation").boolValue = true;
                locked = true;
            }

            return locked;
        }

        private static bool AddToMenu(SerializedProperty entries, ItemData beverage)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("beverage").objectReferenceValue == beverage) return false;
            }

            entries.arraySize++;
            entries.GetArrayElementAtIndex(entries.arraySize - 1).FindPropertyRelative("beverage").objectReferenceValue = beverage;
            return true;
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

        private static bool RemoveFromList(SerializedProperty list, Object value)
        {
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue != value) continue;

                // Nesne referansli elemanda ilk Delete sadece null'lar; ikincisi elemani siler.
                list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                list.DeleteArrayElementAtIndex(i);
                return true;
            }

            return false;
        }
    }
}
