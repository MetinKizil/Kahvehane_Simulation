using System.Collections.Generic;
using System.Text;
using KanBagi.Core;
using KanBagi.Customers;
using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Interaction;
using KanBagi.Items;
using KanBagi.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D8a - Masa oyunlarini (okey, iskambil, domino) dukkan geneli upgrade'den MASAYA KONAN takima cevirir:
    ///  1) Masa prefab'indaki UpgradeVisual eslemesinden (upgrade -> parca) her oyun icin:
    ///     TableGameData (<Parca>Oyun: ad upgrade'den, masadaki etki tablodan), elde tasinan kutu prefab'i
    ///     (<Parca>Kutusu: HoldableItem + GameSetItem + Rigidbody) ve ESYALAR satiri (<Parca>Esya: fiyat/itibar tablodan)
    ///  2) Masa prefab'i: UpgradeVisual -> TableGame (parcalar baslangicta kapali)
    ///  3) Upgrade'ler GelistirmeKatalogu'ndan cikar; Managers'taki dukkan geneli etkiler (TableGameBoost,
    ///     CustomerPatienceBoost) bu upgrade'lere bagliysa silinir
    /// Bir kez calistirilir; UpgradeVisual'i kalmamis masada bir sey yapmaz.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Masa Oyunlarini Esyaya Cevir
    /// </summary>
    public static class TableGameFurnitureBuilder
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string TablePrefabPath = PrefabFolder + "/Masa.prefab";
        private const string CatalogPath = DataFolder + "/EsyaKatalogu.asset";

        private readonly struct GameSetup
        {
            public readonly int Price;
            public readonly int Reputation;
            public readonly float Patience;
            public readonly float SecondRound;
            public readonly Color Color;

            public GameSetup(int price, int reputation, float patience, float secondRound, Color color)
            {
                Price = price;
                Reputation = reputation;
                Patience = patience;
                SecondRound = secondRound;
                Color = color;
            }
        }

        // ---- TAKIM TABLOSU (anahtar: masa prefab'indaki parca adi) ----
        private static readonly Dictionary<string, GameSetup> Setups = new Dictionary<string, GameSetup>
        {
            { "OkeyTakimi", new GameSetup(150, 0, 1.5f, 0f, new Color(0.9f, 0.85f, 0.6f)) },
            { "IskambilDestesi", new GameSetup(100, 40, 1f, 0.3f, new Color(0.75f, 0.15f, 0.15f)) },
            { "DominoTakimi", new GameSetup(120, 100, 1f, 0.2f, new Color(0.1f, 0.1f, 0.1f)) },
        };

        [MenuItem("Kan Bagi/Masa Oyunlarini Esyaya Cevir")]
        public static void Convert()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[TableGameFurnitureBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(CatalogPath);
            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            if (catalog == null || upgradeManager == null)
            {
                Debug.LogError("[TableGameFurnitureBuilder] EsyaKatalogu veya UpgradeManager bulunamadi.");
                return;
            }

            GameObject table = PrefabUtility.LoadPrefabContents(TablePrefabPath);
            var report = new StringBuilder("[TableGameFurnitureBuilder] Masa oyunlari esyaya cevrildi:\n");

            try
            {
                if (!table.TryGetComponent(out UpgradeVisual visual))
                {
                    Debug.LogWarning("[TableGameFurnitureBuilder] Masa prefab'inda UpgradeVisual yok; zaten cevrilmis olabilir.");
                    return;
                }

                var games = new List<TableGameData>();
                var upgrades = new List<UpgradeData>();
                SerializedProperty entries = new SerializedObject(visual).FindProperty("entries");

                for (int i = 0; i < entries.arraySize; i++)
                {
                    SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                    var upgrade = entry.FindPropertyRelative("upgrade").objectReferenceValue as UpgradeData;
                    var piece = entry.FindPropertyRelative("target").objectReferenceValue as GameObject;
                    if (upgrade == null || piece == null || !Setups.TryGetValue(piece.name, out GameSetup setup)) continue;

                    TableGameData game = CreateGame(piece.name, upgrade.UpgradeName, setup);
                    FurnitureData item = CreateItem(piece.name, upgrade.UpgradeName, game, setup);
                    AppendUnique(catalog, "items", item);

                    piece.SetActive(false);
                    games.Add(game);
                    upgrades.Add(upgrade);
                    report.AppendLine($"  {upgrade.UpgradeName}: {item.Price} TL, itibar {item.RequiredReputation}, " +
                                      $"sabir x{game.PatienceMultiplier}, ikinci tur +{game.SecondRoundChance:0.##}");
                }

                Object.DestroyImmediate(visual, true);
                var tableGame = table.AddComponent<TableGame>();
                var tableGameData = new SerializedObject(tableGame);
                SerializedProperty list = tableGameData.FindProperty("games");
                list.arraySize = games.Count;
                for (int i = 0; i < games.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = games[i];
                tableGameData.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(table, TablePrefabPath);
                report.AppendLine($"  Masa prefab'i: UpgradeVisual -> TableGame ({games.Count} takim)");
                report.AppendLine($"  {RemoveUpgrades(upgradeManager, upgrades)}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(table);
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            report.AppendLine("Sahneyi kaydet (Ctrl+S). Eski kayitlarda alinmis oyun upgrade'leri duser (Kaydi Sil).");
            Debug.Log(report.ToString());
        }

        private static TableGameData CreateGame(string pieceName, string displayName, GameSetup setup)
        {
            string path = $"{DataFolder}/{pieceName}Oyun.asset";
            var game = AssetDatabase.LoadAssetAtPath<TableGameData>(path);
            if (game == null)
            {
                game = ScriptableObject.CreateInstance<TableGameData>();
                AssetDatabase.CreateAsset(game, path);
            }

            GameObject box = CreateBox(pieceName, displayName, game, setup.Color);

            var data = new SerializedObject(game);
            data.FindProperty("gameName").stringValue = displayName;
            data.FindProperty("pieceName").stringValue = pieceName;
            data.FindProperty("boxPrefab").objectReferenceValue = box;
            data.FindProperty("patienceMultiplier").floatValue = setup.Patience;
            data.FindProperty("secondRoundChance").floatValue = setup.SecondRound;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(game);
            return game;
        }

        /// <summary>Elde tasinan kutu: kucuk renkli kutu, yere birakilinca fizikle duser.</summary>
        private static GameObject CreateBox(string pieceName, string displayName, TableGameData game, Color color)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = $"{pieceName}Kutusu";
            box.transform.localScale = new Vector3(0.3f, 0.08f, 0.2f);
            box.GetComponent<Renderer>().sharedMaterial = TableGamesBuilder.GetOrCreateMaterial($"{pieceName}KutuMat", color);

            var body = box.AddComponent<Rigidbody>();
            body.mass = 0.5f;

            var holdable = box.AddComponent<HoldableItem>();
            var holdableData = new SerializedObject(holdable);
            holdableData.FindProperty("displayName").stringValue = displayName;
            holdableData.FindProperty("pickupPrompt").stringValue = $"{displayName} al";
            holdableData.ApplyModifiedPropertiesWithoutUndo();

            var set = box.AddComponent<GameSetItem>();
            var setData = new SerializedObject(set);
            setData.FindProperty("game").objectReferenceValue = game;
            setData.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(box, $"{PrefabFolder}/{box.name}.prefab");
            Object.DestroyImmediate(box);
            return saved;
        }

        private static FurnitureData CreateItem(string pieceName, string displayName, TableGameData game, GameSetup setup)
        {
            string path = $"{DataFolder}/{pieceName}Esya.asset";
            var item = AssetDatabase.LoadAssetAtPath<FurnitureData>(path);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<FurnitureData>();
                AssetDatabase.CreateAsset(item, path);
            }

            var data = new SerializedObject(item);
            data.FindProperty("displayName").stringValue = displayName;
            data.FindProperty("category").enumValueIndex = (int)FurnitureCategory.Decor;
            data.FindProperty("prefab").objectReferenceValue = game.BoxPrefab;
            data.FindProperty("price").intValue = setup.Price;
            data.FindProperty("requiredReputation").intValue = setup.Reputation;
            data.FindProperty("baseLimit").intValue = 0;
            data.FindProperty("tableGame").objectReferenceValue = game;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
            return item;
        }

        /// <summary>Upgrade'ler gelistirme katalogundan, dukkan geneli etkileri Managers'tan kalkar.</summary>
        private static string RemoveUpgrades(UpgradeManager upgradeManager, List<UpgradeData> upgrades)
        {
            int removedFromCatalog = 0;
            var catalog = new SerializedObject(upgradeManager).FindProperty("catalog").objectReferenceValue as UpgradeCatalog;
            if (catalog != null)
            {
                var catalogData = new SerializedObject(catalog);
                SerializedProperty list = catalogData.FindProperty("upgrades");
                for (int i = list.arraySize - 1; i >= 0; i--)
                {
                    if (!upgrades.Contains(list.GetArrayElementAtIndex(i).objectReferenceValue as UpgradeData)) continue;

                    list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    list.DeleteArrayElementAtIndex(i);
                    removedFromCatalog++;
                }

                catalogData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
            }

            int removedBoosts = 0;
            foreach (MonoBehaviour boost in upgradeManager.GetComponents<MonoBehaviour>())
            {
                if (!(boost is TableGameBoost) && !(boost is CustomerPatienceBoost)) continue;

                var upgrade = new SerializedObject(boost).FindProperty("requiredUpgrade").objectReferenceValue as UpgradeData;
                if (!upgrades.Contains(upgrade)) continue;

                Undo.DestroyObjectImmediate(boost);
                removedBoosts++;
            }

            return $"Gelistirme katalogundan {removedFromCatalog} upgrade cikti, Managers'tan {removedBoosts} dukkan geneli etki silindi.";
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
    }
}
