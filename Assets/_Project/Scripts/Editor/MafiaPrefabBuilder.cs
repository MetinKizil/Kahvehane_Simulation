using KanBagi.Illegal;
using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Harac tahsildari prefab'ini KOD ILE uretir: kapsul govde + ustunde tabela.
    /// MafiaManager bu prefab'i harac aktifken sahneye koyar, odenince yok eder;
    /// bu yuzden sahneye elle yerlestirilmez.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Tahsildar Prefabini Olustur
    /// </summary>
    public static class MafiaPrefabBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string MaterialFolder = ProjectFolder + "/Materials";
        private const string PrefabPath = PrefabFolder + "/Tahsildar.prefab";
        private const string BodyMaterialPath = MaterialFolder + "/TahsildarGovde.mat";
        private const string BoardMaterialPath = MaterialFolder + "/HaracPano.mat";

        private const float SignHeight = 1.4f;
        private static readonly Vector3 BoardScale = new Vector3(1.4f, 0.8f, 0.05f);

        // POZITIF: Billboard objenin +Z'sini kameradan UZAGA dondurur, yani levha
        // yazinin ARKASINDA kalmali. (Duvar tabelalarinda tam tersi.)
        private const float BoardDepth = 0.04f;

        private static readonly Vector2 TextSize = new Vector2(1.3f, 0.4f);
        private static readonly Color BodyColor = new Color(0.16f, 0.16f, 0.19f);
        private static readonly Color BoardColor = new Color(0.2f, 0.06f, 0.07f);
        private static readonly Color AmountColor = new Color(0.95f, 0.75f, 0.3f);

        [MenuItem("Kan Bagi/Tahsildar Prefabini Olustur")]
        public static void BuildCollectorPrefab()
        {
            EnsureFolders();

            // Govde: kapsul. Collider kapsulle birlikte gelir ve KALMALI - E ile konusulacak.
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "Tahsildar";
            root.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);

            var bodyRenderer = root.GetComponent<MeshRenderer>();
            if (bodyRenderer != null)
                bodyRenderer.sharedMaterial = GetOrCreateMaterial(BodyMaterialPath, "TahsildarGovde", BodyColor);

            root.AddComponent<MafiaCollector>();

            GameObject sign = CreateSign(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[MafiaPrefabBuilder] Tahsildar prefab'i olusturuldu: {PrefabPath}\n" +
                      "Managers > Mafia Manager > Collector Prefab alanina bu prefab'i ata. Sahneye elle koyma.");
        }

        private static GameObject CreateSign(Transform parent)
        {
            var sign = new GameObject("HaracTabelasi");
            sign.transform.SetParent(parent, worldPositionStays: false);

            // Kapsulun olcegi yaziyi bozmasin: ters olcek + dunya yuksekligi.
            Vector3 parentScale = parent.lossyScale;
            sign.transform.localScale = new Vector3(
                SafeInverse(parentScale.x), SafeInverse(parentScale.y), SafeInverse(parentScale.z));
            sign.transform.localPosition = new Vector3(0f, SignHeight / Mathf.Max(0.0001f, parentScale.y), 0f);

            // Tahsildar bir insan: etrafindan dolasabilirsin, o yuzden tabela hep sana doner.
            // (Duvara asili emlak tabelalarinda Billboard YOK - onlar sabit durur.)
            sign.AddComponent<Billboard>();

            CreateBoard(sign.transform);

            TextMeshPro titleText = CreateText(sign.transform, "BaslikText", new Vector3(0f, 0.18f, 0f),
                                              0.26f, Color.white, "HARAC");
            TextMeshPro amountText = CreateText(sign.transform, "TutarText", new Vector3(0f, -0.15f, 0f),
                                               0.3f, AmountColor, "500 TL");

            var signUI = sign.AddComponent<MafiaSignUI>();
            var serialized = new SerializedObject(signUI);
            serialized.FindProperty("titleText").objectReferenceValue = titleText;
            serialized.FindProperty("amountText").objectReferenceValue = amountText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return sign;
        }

        private static void CreateBoard(Transform parent)
        {
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.name = "Levha";
            board.transform.SetParent(parent, worldPositionStays: false);
            board.transform.localPosition = new Vector3(0f, 0f, BoardDepth);
            board.transform.localScale = BoardScale;

            // Tabela isin hedefi olmamali: oyuncu adamin kendisine bakabilsin.
            Collider boardCollider = board.GetComponent<Collider>();
            if (boardCollider != null) Object.DestroyImmediate(boardCollider);

            var renderer = board.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sharedMaterial = GetOrCreateMaterial(BoardMaterialPath, "HaracPano", BoardColor);
        }

        private static Material GetOrCreateMaterial(string path, string materialName, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Universal Render Pipeline/Lit")
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
