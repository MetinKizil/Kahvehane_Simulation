using KanBagi.Illegal;
using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Kumar masasinin ustundeki 3D tabelayi uretir ve masaya yerlestirir.
    /// Konum masanin kendi olculerinden hesaplanir - elle koordinat girilmez.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Kumar Tabelasini Olustur ve Yerlestir
    /// </summary>
    public static class GamblingSignBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string MaterialFolder = ProjectFolder + "/Materials";
        private const string SignPath = PrefabFolder + "/KumarTabelasi.prefab";
        private const string BoardMaterialPath = MaterialFolder + "/KumarPano.mat";

        private static readonly Vector3 BoardScale = new Vector3(2f, 1.1f, 0.06f);
        private const float BoardDepth = -0.04f;

        /// <summary>Tabela masanin ust yuzunden bu kadar yukarida durur.</summary>
        private const float HeightAboveTable = 0.9f;

        /// <summary>
        /// Tabelanin baktigi yon (derece, Y ekseni).
        /// 180 = -Z yonu: masa tezgahin arkasinda oldugu icin oyuncu KAPI tarafindan bakar.
        /// 0 yaparsan +Z'ye doner.
        /// </summary>
        private const float SignYawDegrees = 180f;

        private static readonly Vector2 TextSize = new Vector2(1.85f, 0.5f);
        private static readonly Color BoardColor = new Color(0.12f, 0.08f, 0.1f);

        [MenuItem("Kan Bagi/Kumar Tabelasini Olustur ve Yerlestir")]
        public static void BuildAndPlace()
        {
            EnsureFolders();

            GameObject prefab = BuildPrefab();
            if (prefab == null) return;

            GamblingTable[] tables = Object.FindObjectsByType<GamblingTable>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (tables.Length == 0)
            {
                Debug.LogWarning("[GamblingSignBuilder] Sahnede GamblingTable bulunamadi. " +
                                 "Once kumar masasini kur (Cube + Gambling Table), sonra bu menuyu tekrar calistir.");
                return;
            }

            foreach (GamblingTable table in tables)
            {
                if (table == null) continue;

                RemoveOldSign(table);
                AttachSign(prefab, table);
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[GamblingSignBuilder] {tables.Length} masa icin tabela yerlestirildi. Sahneyi kaydet (Ctrl+S).");
        }

        private static GameObject BuildPrefab()
        {
            var root = new GameObject("KumarTabelasi");

            CreateBoard(root.transform);

            TextMeshPro titleText = CreateText(root.transform, "BaslikText", new Vector3(0f, 0.32f, 0f),
                                              0.32f, Color.white, "BAHIS 100 TL");
            TextMeshPro resultText = CreateText(root.transform, "SonucText", new Vector3(0f, 0f, 0f),
                                              0.28f, new Color(0.8f, 0.8f, 0.8f), "-");
            TextMeshPro totalText = CreateText(root.transform, "ToplamText", new Vector3(0f, -0.3f, 0f),
                                              0.24f, new Color(0.8f, 0.8f, 0.8f), "Gece: 0 TL");

            var sign = root.AddComponent<GamblingSignUI>();
            var serialized = new SerializedObject(sign);
            serialized.FindProperty("titleText").objectReferenceValue = titleText;
            serialized.FindProperty("resultText").objectReferenceValue = resultText;
            serialized.FindProperty("totalText").objectReferenceValue = totalText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, SignPath);
            Object.DestroyImmediate(root);

            if (saved == null) Debug.LogError("[GamblingSignBuilder] Prefab kaydedilemedi.");

            return saved;
        }

        private static void RemoveOldSign(GamblingTable table)
        {
            GamblingSignUI existing = table.GetComponentInChildren<GamblingSignUI>(includeInactive: true);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        private static void AttachSign(GameObject prefab, GamblingTable table)
        {
            var sign = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            sign.name = $"{table.name}Tabelasi";
            sign.transform.SetParent(table.transform, worldPositionStays: true);

            Bounds bounds = GetWorldBounds(table);

            // Masanin tam ustunde, kapi tarafina (oyuncunun durdugu yone) bakar.
            sign.transform.position = new Vector3(bounds.center.x, bounds.max.y + HeightAboveTable, bounds.center.z);
            sign.transform.rotation = Quaternion.Euler(0f, SignYawDegrees, 0f);

            // Masanin olcegi yaziyi buyutmesin.
            Vector3 parentScale = table.transform.lossyScale;
            sign.transform.localScale = new Vector3(
                SafeInverse(parentScale.x), SafeInverse(parentScale.y), SafeInverse(parentScale.z));

            var signUI = sign.GetComponent<GamblingSignUI>();
            if (signUI == null) return;

            var serialized = new SerializedObject(signUI);
            serialized.FindProperty("table").objectReferenceValue = table;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds GetWorldBounds(GamblingTable table)
        {
            var renderer = table.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;

            var tableCollider = table.GetComponent<Collider>();
            if (tableCollider != null) return tableCollider.bounds;

            return new Bounds(table.transform.position, Vector3.one);
        }

        private static void CreateBoard(Transform parent)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Levha";
            board.transform.SetParent(parent, worldPositionStays: false);
            board.transform.localPosition = new Vector3(0f, 0f, BoardDepth);
            board.transform.localScale = BoardScale;

            Collider boardCollider = board.GetComponent<Collider>();
            if (boardCollider != null) Object.DestroyImmediate(boardCollider);

            var renderer = board.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = GetOrCreateBoardMaterial();
        }

        private static Material GetOrCreateBoardMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(BoardMaterialPath);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Universal Render Pipeline/Lit")
                            ?? Shader.Find("Standard");

            if (shader == null) return null;

            var material = new Material(shader) { name = "KumarPano" };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", BoardColor);
            else
                material.color = BoardColor;

            AssetDatabase.CreateAsset(material, BoardMaterialPath);
            return material;
        }

        private static TextMeshPro CreateText(Transform parent, string objectName, Vector3 localPosition,
                                              float fontSize, Color color, string placeholder)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, worldPositionStays: false);
            textObject.transform.localPosition = localPosition;

            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.text = placeholder;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.rectTransform.sizeDelta = TextSize;

            return text;
        }

        private static float SafeInverse(float value) => Mathf.Approximately(value, 0f) ? 1f : 1f / value;

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
