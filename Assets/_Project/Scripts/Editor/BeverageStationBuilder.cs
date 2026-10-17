using System.Text;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Inventory;
using KanBagi.Shop;
using KanBagi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Upgrade ile acilan icecek istasyonlarini (sahlep makinesi vb.) tek tikla kurar.
    /// Yeni bir icecegin oyuna tam girmesi icin gereken TUM baglantilari yapar:
    ///  1) Sahnede makine (TeaStove, baslangicta kapali)       5) Menu (MenuManager, upgrade'e kilitli)
    ///  2) 'Managers'ta UpgradeUnlocker (makineyi acar)         6) Malzeme katalogu (PC, upgrade'e kilitli)
    ///  3) Sanayi ocagi hiz listesi (StationSpeedBoost)         7) Urun veritabani (kayit icin)
    ///  4) Gelistirme katalogu (PC)                             8) Sol ustteki stok yazisi (StockUI)
    /// Tekrar calistirilabilir: var olani bulur, cogaltmaz. Yeni icecek = Stations tablosuna bir satir.
    /// Sonra: Kan Bagi > NavMesh'i Yeniden Bake Et (makinenin altinda delik acilsin).
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Icecek Istasyonlarini Kur
    /// </summary>
    public static class BeverageStationBuilder
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects";

        private readonly struct StationSetup
        {
            public readonly string ObjectName;
            public readonly string BeverageAsset;
            public readonly string ConsumableAsset;
            public readonly string UpgradeAsset;
            public readonly string WorkLabel;
            public readonly int UnitsPerPack;
            public readonly Vector3 Position;
            public readonly Vector3 Scale;
            public readonly Color Color;

            public StationSetup(string objectName, string beverageAsset, string consumableAsset, string upgradeAsset,
                                string workLabel, int unitsPerPack, Vector3 position, Vector3 scale, Color color)
            {
                ObjectName = objectName;
                BeverageAsset = beverageAsset;
                ConsumableAsset = consumableAsset;
                UpgradeAsset = upgradeAsset;
                WorkLabel = workLabel;
                UnitsPerPack = unitsPerPack;
                Position = position;
                Scale = scale;
                Color = color;
            }
        }

        // ---- ISTASYON TABLOSU (yeni icecek icin buraya bir satir ekle) ----
        // Kahve ve oralet elle kuruldu; burada yoklar. Konumlar arka tezgah hattinda (z = 3).
        private static readonly StationSetup[] Stations =
        {
            new StationSetup("SahlepMakinesi", "Sahlep", "SahlepTozu", "SahlepMakinesiUpgrade",
                "Sahlep dolduruluyor...", 10,
                new Vector3(3.5f, 0.5f, 3f), new Vector3(1f, 1f, 0.8f), new Color(0.93f, 0.87f, 0.72f)),
        };

        [MenuItem("Kan Bagi/Icecek Istasyonlarini Kur")]
        public static void SetUpStations()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[BeverageStationBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            if (upgradeManager == null)
            {
                Debug.LogError("[BeverageStationBuilder] Sahnede UpgradeManager bulunamadi ('Managers' objesi).");
                return;
            }

            var report = new StringBuilder("[BeverageStationBuilder] Icecek istasyonlari kuruldu:\n");

            foreach (StationSetup setup in Stations)
            {
                var beverage = LoadData<ItemData>(setup.BeverageAsset);
                var consumable = LoadData<ItemData>(setup.ConsumableAsset);
                var upgrade = LoadData<UpgradeData>(setup.UpgradeAsset);
                if (beverage == null || consumable == null || upgrade == null) return;

                TeaStove stove = FindOrCreateStove(setup, beverage, consumable);
                GameObject managers = upgradeManager.gameObject;

                report.AppendLine($"  {beverage.ItemName} ({stove.name}):");
                report.AppendLine($"    UpgradeUnlocker: {WireUnlocker(managers, upgrade, stove.gameObject)}");
                report.AppendLine($"    Sanayi ocagi listesi: {AddToSpeedBoost(stove)}");
                report.AppendLine($"    Gelistirme katalogu: {Added(TableGamesBuilder.AddToCatalog(upgradeManager, upgrade))}");
                report.AppendLine($"    Menu: {AddToMenu(beverage, upgrade)}");
                report.AppendLine($"    Malzeme katalogu: {AddToShopCatalog(consumable, setup.UnitsPerPack, upgrade)}");
                report.AppendLine($"    Urun veritabani: {AddToDatabase(beverage, consumable)}");
                report.AppendLine($"    Stok yazisi: {AddToStockUI(consumable)}");
            }

            EditorSceneManager.MarkSceneDirty(upgradeManager.gameObject.scene);
            AssetDatabase.SaveAssets();

            report.AppendLine("Sahneyi kaydet (Ctrl+S), sonra 'Kan Bagi > NavMesh'i Yeniden Bake Et'.");
            Debug.Log(report.ToString());
        }

        private static T LoadData<T>(string assetName) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/{assetName}.asset");
            if (asset == null) Debug.LogError($"[BeverageStationBuilder] Veri bulunamadi: {DataFolder}/{assetName}.asset");
            return asset;
        }

        /// <summary>Bu icecegi yapan istasyon zaten varsa onu kullanir; yoksa tezgaha yeni bir makine koyar.</summary>
        private static TeaStove FindOrCreateStove(StationSetup setup, ItemData beverage, ItemData consumable)
        {
            foreach (TeaStove existing in Object.FindObjectsByType<TeaStove>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (new SerializedObject(existing).FindProperty("beverageData").objectReferenceValue == beverage)
                    return existing;
            }

            // Collider kalir: oyuncu makineye bakip etkilesir.
            GameObject machine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(machine, "Icecek Istasyonu");
            machine.name = setup.ObjectName;
            machine.transform.position = setup.Position;
            machine.transform.localScale = setup.Scale;

            Material material = TableGamesBuilder.GetOrCreateMaterial(setup.ObjectName, setup.Color);
            if (material != null) machine.GetComponent<MeshRenderer>().sharedMaterial = material;

            var stove = machine.AddComponent<TeaStove>();
            var serialized = new SerializedObject(stove);
            serialized.FindProperty("workLabel").stringValue = setup.WorkLabel;
            serialized.FindProperty("beverageData").objectReferenceValue = beverage;
            serialized.FindProperty("requiredConsumable").objectReferenceValue = consumable;
            serialized.FindProperty("consumeAmount").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Upgrade alinana kadar kapali; UpgradeUnlocker acar.
            machine.SetActive(false);
            return stove;
        }

        private static string WireUnlocker(GameObject managers, UpgradeData upgrade, GameObject machine)
        {
            UpgradeUnlocker unlocker = TableGamesBuilder.FindByUpgrade<UpgradeUnlocker>(managers, upgrade);
            bool isNew = unlocker == null;
            if (isNew) unlocker = Undo.AddComponent<UpgradeUnlocker>(managers);

            var serialized = new SerializedObject(unlocker);
            serialized.FindProperty("requiredUpgrade").objectReferenceValue = upgrade;
            SerializedProperty targets = serialized.FindProperty("targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = machine;
            serialized.ApplyModifiedProperties();

            return isNew ? "eklendi" : "guncellendi";
        }

        private static string AddToSpeedBoost(TeaStove stove)
        {
            var boost = Object.FindFirstObjectByType<StationSpeedBoost>(FindObjectsInactive.Include);
            if (boost == null) return "StationSpeedBoost yok, atlandi";

            var serialized = new SerializedObject(boost);
            return Added(AppendUnique(serialized.FindProperty("targetStations"), stove), serialized);
        }

        /// <summary>Menuye {icecek, upgrade} kaydi ekler: upgrade alinmadan musteri bunu istemez.</summary>
        private static string AddToMenu(ItemData beverage, UpgradeData upgrade)
        {
            var menu = Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
            if (menu == null) return "MenuManager yok, atlandi";

            var serialized = new SerializedObject(menu);
            SerializedProperty entries = serialized.FindProperty("entries");
            if (ContainsEntry(entries, "beverage", beverage)) return "zaten vardi";

            SerializedProperty entry = AppendElement(entries);
            entry.FindPropertyRelative("beverage").objectReferenceValue = beverage;
            entry.FindPropertyRelative("requiredUpgrade").objectReferenceValue = upgrade;
            serialized.ApplyModifiedProperties();
            return "eklendi";
        }

        /// <summary>PC'deki malzeme listesine ekler; upgrade alinana kadar kilitli gorunur.</summary>
        private static string AddToShopCatalog(ItemData consumable, int unitsPerPack, UpgradeData upgrade)
        {
            var computer = Object.FindFirstObjectByType<ComputerStation>(FindObjectsInactive.Include);
            var catalog = computer != null
                ? new SerializedObject(computer).FindProperty("catalog").objectReferenceValue as ShopCatalog
                : null;
            if (catalog == null) return "ComputerStation > Catalog yok, atlandi";

            var serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            if (ContainsEntry(entries, "item", consumable)) return "zaten vardi";

            SerializedProperty entry = AppendElement(entries);
            entry.FindPropertyRelative("item").objectReferenceValue = consumable;
            entry.FindPropertyRelative("unitsPerPack").intValue = unitsPerPack;
            entry.FindPropertyRelative("requiredUpgrade").objectReferenceValue = upgrade;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            return "eklendi";
        }

        /// <summary>Kayit sistemi stoklari isimle esler: veritabaninda olmayan urun kayittan geri gelmez.</summary>
        private static string AddToDatabase(ItemData beverage, ItemData consumable)
        {
            var inventory = Object.FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
            var database = inventory != null
                ? new SerializedObject(inventory).FindProperty("itemDatabase").objectReferenceValue as ItemDatabase
                : null;
            if (database == null) return "InventoryManager > Item Database yok, atlandi";

            var serialized = new SerializedObject(database);
            SerializedProperty items = serialized.FindProperty("items");
            bool added = AppendUnique(items, beverage) | AppendUnique(items, consumable);

            if (added)
            {
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(database);
            }

            return added ? "eklendi" : "zaten vardi";
        }

        private static string AddToStockUI(ItemData consumable)
        {
            var stock = Object.FindFirstObjectByType<StockUI>(FindObjectsInactive.Include);
            if (stock == null) return "StockUI yok, atlandi";

            var serialized = new SerializedObject(stock);
            return Added(AppendUnique(serialized.FindProperty("trackedItems"), consumable), serialized);
        }

        // ---- Liste yardimcilari ----

        /// <summary>Obje listede yoksa sona ekler. Eklendiyse true (kaydetmek cagirana kalir).</summary>
        private static bool AppendUnique(SerializedProperty list, Object value)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == value) return false;
            }

            AppendElement(list).objectReferenceValue = value;
            return true;
        }

        private static SerializedProperty AppendElement(SerializedProperty list)
        {
            list.arraySize++;
            return list.GetArrayElementAtIndex(list.arraySize - 1);
        }

        private static bool ContainsEntry(SerializedProperty entries, string field, Object value)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative(field).objectReferenceValue == value) return true;
            }

            return false;
        }

        private static string Added(bool added, SerializedObject toApply = null)
        {
            if (added) toApply?.ApplyModifiedProperties();
            return added ? "eklendi" : "zaten vardi";
        }
    }
}
