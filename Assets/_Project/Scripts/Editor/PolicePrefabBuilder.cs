using KanBagi.Illegal;
using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Polis memuru prefab'ini uretir (PoliceManager baskin aninda sahneye koyar).
    /// Kumar masasinin ortusu D6c'de kalkti: polisten saklanma artik bodrum kapaginda (HiddenHatch).
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Polis Prefabini Olustur
    /// </summary>
    public static class PolicePrefabBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string MaterialFolder = ProjectFolder + "/Materials";
        private const string OfficerPath = PrefabFolder + "/PolisMemuru.prefab";
        private const string BodyMaterialPath = MaterialFolder + "/PolisGovde.mat";
        private const string BoardMaterialPath = MaterialFolder + "/PolisPano.mat";

        private const float SignHeight = 1.4f;
        private static readonly Vector3 BoardScale = new Vector3(1.3f, 0.7f, 0.05f);

        // Billboard objenin +Z'sini kameradan uzaga cevirir; levha yazinin ARKASINDA kalmali.
        private const float BoardDepth = 0.04f;

        private static readonly Vector2 TextSize = new Vector2(1.2f, 0.4f);
        private static readonly Color BodyColor = new Color(0.12f, 0.2f, 0.42f);
        private static readonly Color BoardColor = new Color(0.07f, 0.11f, 0.24f);

        [MenuItem("Kan Bagi/Polis Prefabini Olustur")]
        public static void BuildOfficerPrefab()
        {
            EnsureFolders();

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "PolisMemuru";
            root.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);

            var bodyRenderer = root.GetComponent<MeshRenderer>();
            if (bodyRenderer != null)
                bodyRenderer.sharedMaterial = GetOrCreateMaterial(BodyMaterialPath, "PolisGovde", BodyColor);

            root.AddComponent<PoliceOfficer>();
            CreateSign(root.transform);

            PrefabUtility.SaveAsPrefabAsset(root, OfficerPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PolicePrefabBuilder] Polis prefab'i olusturuldu: {OfficerPath}\n" +
                      "Managers > Police Manager > Officer Prefab alanina ata. Sahneye elle koyma.");
        }

        private static void CreateSign(Transform parent)
        {
            var sign = new GameObject("PolisTabelasi");
            sign.transform.SetParent(parent, worldPositionStays: false);

            Vector3 parentScale = parent.lossyScale;
            sign.transform.localScale = new Vector3(
                SafeInverse(parentScale.x), SafeInverse(parentScale.y), SafeInverse(parentScale.z));
            sign.transform.localPosition = new Vector3(0f, SignHeight / Mathf.Max(0.0001f, parentScale.y), 0f);

            // Memur bir insan: etrafindan dolasilir, tabela hep oyuncuya doner.
            sign.AddComponent<Billboard>();

            CreateBoard(sign.transform);

            CreateText(sign.transform, "BaslikText", new Vector3(0f, 0.14f, 0f), 0.26f, Color.white, "POLIS");
            CreateText(sign.transform, "AltText", new Vector3(0f, -0.16f, 0f), 0.2f,
                       new Color(0.8f, 0.85f, 1f), "RUTIN KONTROL");
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
            if (renderer != null)
                renderer.sharedMaterial = GetOrCreateMaterial(BoardMaterialPath, "PolisPano", BoardColor);
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
