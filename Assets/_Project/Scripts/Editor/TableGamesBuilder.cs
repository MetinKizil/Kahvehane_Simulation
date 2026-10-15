using System.Collections.Generic;
using System.Linq;
using System.Text;
using KanBagi.Core;
using KanBagi.Customers;
using KanBagi.Data;
using KanBagi.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Iskambil ve domino masa oyunlarini tek tikla kurar:
    ///  1) Her masaya (kilitli Masa3 dahil) kucuk bir deste ve domino kutusu koyar (collider'siz, baslangicta kapali)
    ///  2) 'Managers'a her oyun icin UpgradeUnlocker (objeleri acar) + TableGameBoost (ikinci tur payi) ekler/gunceller
    ///  3) Iki upgrade'i GelistirmeKatalogu'na ekler (PC'de gorunsun, kayitta eslensin)
    /// Tekrar calistirilabilir: eski objeleri silip yeniden kurar, bilesenleri cogaltmaz.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Masa Oyunlarini Kur
    /// </summary>
    public static class TableGamesBuilder
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string MaterialFolder = "Assets/_Project/Materials";

        /// <summary>Bir masa oyununun verisi ve masadaki gorunumu (masanin YEREL olcusunde).</summary>
        private readonly struct GameSetup
        {
            public readonly string UpgradeAsset;
            public readonly string ObjectName;
            public readonly float SecondRoundChance;
            public readonly Vector3 LocalPosition;
            public readonly Vector3 LocalScale;
            public readonly Color Color;

            public GameSetup(string upgradeAsset, string objectName, float secondRoundChance,
                             Vector3 localPosition, Vector3 localScale, Color color)
            {
                UpgradeAsset = upgradeAsset;
                ObjectName = objectName;
                SecondRoundChance = secondRoundChance;
                LocalPosition = localPosition;
                LocalScale = localScale;
                Color = color;
            }
        }

        // Masanin yerel ustu y = 0.5; obje yuksekliginin yarisi eklenince tam ustune oturur.
        // Okey kutusu (0.3, 0.3) kosesinde, bardak ortada: deste ve domino diger koselere.
        private static readonly GameSetup[] Games =
        {
            new GameSetup("IskambilUpgrade", "IskambilDestesi", 0.3f,
                new Vector3(-0.3f, 0.52f, 0.3f), new Vector3(0.15f, 0.04f, 0.2f), new Color(0.75f, 0.15f, 0.15f)),
            new GameSetup("DominoUpgrade", "DominoTakimi", 0.2f,
                new Vector3(0.3f, 0.53f, -0.3f), new Vector3(0.22f, 0.06f, 0.1f), new Color(0.92f, 0.9f, 0.82f)),
        };

        [MenuItem("Kan Bagi/Masa Oyunlarini Kur")]
        public static void SetUpTableGames()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[TableGamesBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            if (upgradeManager == null)
            {
                Debug.LogError("[TableGamesBuilder] Sahnede UpgradeManager bulunamadi ('Managers' objesi).");
                return;
            }

            Table[] tables = Object.FindObjectsByType<Table>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                                   .OrderBy(table => table.name)
                                   .ToArray();

            var report = new StringBuilder($"[TableGamesBuilder] {tables.Length} masaya kuruldu:\n");

            foreach (GameSetup game in Games)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>($"{DataFolder}/{game.UpgradeAsset}.asset");
                if (upgrade == null)
                {
                    Debug.LogError($"[TableGamesBuilder] Upgrade bulunamadi: {DataFolder}/{game.UpgradeAsset}.asset");
                    return;
                }

                Material material = GetOrCreateMaterial(game.ObjectName, game.Color);
                var pieces = new List<GameObject>();

                foreach (Table table in tables)
                    pieces.Add(CreatePiece(table.transform, game, material));

                WireUnlocker(upgradeManager.gameObject, upgrade, pieces);
                WireBoost(upgradeManager.gameObject, upgrade, game.SecondRoundChance);
                bool addedToCatalog = AddToCatalog(upgradeManager, upgrade);

                report.AppendLine($"  {upgrade.UpgradeName}: {pieces.Count} obje, ikinci tur +%{game.SecondRoundChance * 100f:0}" +
                                  (addedToCatalog ? ", kataloga eklendi" : ""));
            }

            EditorSceneManager.MarkSceneDirty(upgradeManager.gameObject.scene);
            AssetDatabase.SaveAssets();

            report.AppendLine("Sahneyi kaydet (Ctrl+S).");
            Debug.Log(report.ToString());
        }

        /// <summary>Masanin ustune collider'siz kucuk bir kutu koyar (servis isinini engellemesin). Eskisi varsa silinir.</summary>
        private static GameObject CreatePiece(Transform table, GameSetup game, Material material)
        {
            Transform old = table.Find(game.ObjectName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(piece, "Masa Oyunu");
            piece.name = game.ObjectName;
            Object.DestroyImmediate(piece.GetComponent<Collider>());

            piece.transform.SetParent(table, worldPositionStays: false);
            piece.transform.localPosition = game.LocalPosition;
            piece.transform.localRotation = Quaternion.identity;
            piece.transform.localScale = game.LocalScale;

            if (material != null) piece.GetComponent<MeshRenderer>().sharedMaterial = material;

            // Upgrade alinana kadar kapali; UpgradeUnlocker acar.
            piece.SetActive(false);
            return piece;
        }

        private static void WireUnlocker(GameObject managers, UpgradeData upgrade, List<GameObject> pieces)
        {
            UpgradeUnlocker unlocker = FindByUpgrade<UpgradeUnlocker>(managers, upgrade)
                                       ?? Undo.AddComponent<UpgradeUnlocker>(managers);

            var serialized = new SerializedObject(unlocker);
            serialized.FindProperty("requiredUpgrade").objectReferenceValue = upgrade;

            SerializedProperty targets = serialized.FindProperty("targets");
            targets.arraySize = pieces.Count;
            for (int i = 0; i < pieces.Count; i++)
                targets.GetArrayElementAtIndex(i).objectReferenceValue = pieces[i];

            serialized.ApplyModifiedProperties();
        }

        private static void WireBoost(GameObject managers, UpgradeData upgrade, float chance)
        {
            TableGameBoost boost = FindByUpgrade<TableGameBoost>(managers, upgrade)
                                   ?? Undo.AddComponent<TableGameBoost>(managers);

            var serialized = new SerializedObject(boost);
            serialized.FindProperty("requiredUpgrade").objectReferenceValue = upgrade;
            serialized.FindProperty("secondRoundChance").floatValue = chance;
            serialized.ApplyModifiedProperties();
        }

        /// <summary>Ayni upgrade'e bagli bilesen zaten varsa onu doner (tekrar calistirinca cogalmasin).</summary>
        private static T FindByUpgrade<T>(GameObject owner, UpgradeData upgrade) where T : Component
        {
            foreach (T component in owner.GetComponents<T>())
            {
                var serialized = new SerializedObject(component);
                if (serialized.FindProperty("requiredUpgrade").objectReferenceValue == upgrade) return component;
            }

            return null;
        }

        /// <summary>Upgrade'i UpgradeManager'in katalogunda yoksa ekler. Eklendiyse true.</summary>
        private static bool AddToCatalog(UpgradeManager upgradeManager, UpgradeData upgrade)
        {
            var catalog = new SerializedObject(upgradeManager).FindProperty("catalog").objectReferenceValue as UpgradeCatalog;
            if (catalog == null)
            {
                Debug.LogError("[TableGamesBuilder] UpgradeManager > Catalog atanmamis; upgrade kataloga eklenemedi.");
                return false;
            }

            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("upgrades");

            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == upgrade) return false;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = upgrade;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(catalog);
            return true;
        }

        private static Material GetOrCreateMaterial(string materialName, Color color)
        {
            string path = $"{MaterialFolder}/{materialName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) return null;

            var material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            else material.color = color;

            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
