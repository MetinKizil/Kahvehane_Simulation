using KanBagi.Neighborhood;
using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Binanin duvarina yapisacak "SATILIK" tabelasini KOD ILE uretir:
    /// koyu bir levha + uzerinde mulk adi ve bilgileri.
    ///
    /// Billboard YOKTUR: tabela duvara sabit durur, kameraya donmez.
    /// Yazilar +Z yonune bakar; tabela binanin ON YUZUNE (oyuncunun geldigi tarafa) konur.
    /// Collider YOKTUR: oyuncunun binaya bakisini kesmez.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Emlak Tabelasi Prefabini Olustur
    /// </summary>
    public static class SignPrefabBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string MaterialFolder = ProjectFolder + "/Materials";
        private const string SignPath = PrefabFolder + "/EmlakTabelasi.prefab";
        private const string BoardMaterialPath = MaterialFolder + "/TabelaPano.mat";

        // Levha olculeri (dunya birimi). Bir kapinin ustune sigacak boyut.
        private static readonly Vector3 BoardScale = new Vector3(2.2f, 1f, 0.06f);

        // Negatif: levha yazinin ARKASINDA kalir (oyuncu +Z tarafindan bakiyor).
        private const float BoardDepth = -0.04f;

        private const float NameFontSize = 0.42f;
        private const float InfoFontSize = 0.26f;
        private static readonly Vector2 TextSize = new Vector2(2f, 0.75f);

        private static readonly Color BoardColor = new Color(0.11f, 0.12f, 0.16f);
        private static readonly Color NameColor = Color.white;
        private static readonly Color InfoColor = new Color(0.95f, 0.8f, 0.3f);

        [MenuItem("Kan Bagi/Emlak Tabelasi Prefabini Olustur")]
        public static void BuildSignPrefab()
        {
            EnsureFolders();

            var root = new GameObject("EmlakTabelasi");

            CreateBoard(root.transform);

            TextMeshPro nameText = CreateText(root.transform, "AdText", new Vector3(0f, 0.28f, 0f),
                                             NameFontSize, NameColor, "Mulk Adi");
            TextMeshPro infoText = CreateText(root.transform, "BilgiText", new Vector3(0f, -0.14f, 0f),
                                             InfoFontSize, InfoColor, "SATILIK - 800 TL\nGunluk 60 TL   |   Itibar 10");

            var sign = root.AddComponent<PropertySignUI>();
            var serialized = new SerializedObject(sign);
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("infoText").objectReferenceValue = infoText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, SignPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[SignPrefabBuilder] Tabela prefab'i olusturuldu: {SignPath}\n" +
                      "Binanin on yuzune yapistir (rotation 0,0,0). 'Building' alanini bos birakirsan en yakin bina secilir.");
        }

        /// <summary>
        /// Sahnedeki her PropertyBuilding'in ON DUVARINA bir tabela yapistirir.
        /// Konum binanin kendi olculerinden (Renderer.bounds) hesaplanir - elle koordinat girilmez.
        /// Tabela binanin cocugu olur; bina tasinirsa tabela onunla gelir.
        /// </summary>
        [MenuItem("Kan Bagi/Tabelalari Binalara Yerlestir")]
        public static void PlaceSignsOnBuildings()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SignPath);

            if (prefab == null)
            {
                BuildSignPrefab();
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SignPath);
            }

            if (prefab == null)
            {
                Debug.LogError("[SignPrefabBuilder] Tabela prefab'i olusturulamadi.");
                return;
            }

            PropertyBuilding[] buildings = Object.FindObjectsByType<PropertyBuilding>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (buildings.Length == 0)
            {
                Debug.LogWarning("[SignPrefabBuilder] Sahnede PropertyBuilding bulunamadi. Once binalari kur.");
                return;
            }

            int placed = 0;

            foreach (PropertyBuilding building in buildings)
            {
                if (building == null) continue;

                RemoveOldSign(building);
                AttachSign(prefab, building);
                placed++;
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[SignPrefabBuilder] {placed} bina icin tabela yerlestirildi. Sahneyi kaydet (Ctrl+S).");
        }

        private static void RemoveOldSign(PropertyBuilding building)
        {
            PropertySignUI existing = building.GetComponentInChildren<PropertySignUI>(includeInactive: true);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);
        }

        private static void AttachSign(GameObject prefab, PropertyBuilding building)
        {
            var sign = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            sign.name = $"{building.name}Tabelasi";
            sign.transform.SetParent(building.transform, worldPositionStays: true);

            Bounds bounds = GetWorldBounds(building);

            // Binanin +Z yuzu, kapi hizasinin biraz ustu. Oyuncu sokaktan +Z tarafindan gelir.
            sign.transform.position = new Vector3(
                bounds.center.x,
                bounds.min.y + bounds.size.y * 0.55f,
                bounds.max.z + 0.05f);

            sign.transform.rotation = Quaternion.identity;

            // Binanin olcegi (ornegin 3x3x3) yaziyi buyutmesin: tersini uygula.
            Vector3 parentScale = building.transform.lossyScale;
            sign.transform.localScale = new Vector3(
                SafeInverse(parentScale.x),
                SafeInverse(parentScale.y),
                SafeInverse(parentScale.z));

            var signUI = sign.GetComponent<PropertySignUI>();
            if (signUI == null) return;

            var serialized = new SerializedObject(signUI);
            serialized.FindProperty("building").objectReferenceValue = building;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds GetWorldBounds(PropertyBuilding building)
        {
            var renderer = building.GetComponent<Renderer>();
            if (renderer != null) return renderer.bounds;

            var buildingCollider = building.GetComponent<Collider>();
            if (buildingCollider != null) return buildingCollider.bounds;

            return new Bounds(building.transform.position, Vector3.one);
        }

        private static float SafeInverse(float value) => Mathf.Approximately(value, 0f) ? 1f : 1f / value;

        /// <summary>Yazinin arkasindaki koyu levha. Collider'i YOKTUR.</summary>
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

            if (shader == null)
            {
                Debug.LogWarning("[SignPrefabBuilder] Uygun shader bulunamadi; levha varsayilan materyalle kalacak.");
                return null;
            }

            var material = new Material(shader) { name = "TabelaPano" };

            // URP'de renk _BaseColor'dadir; eski shader'larda .color yeterli.
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

            // 3D TextMeshPro (Canvas'a bagli olmayan surum).
            TextMeshPro text = textObject.AddComponent<TextMeshPro>();
            text.text = placeholder;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.rectTransform.sizeDelta = TextSize;

            return text;
        }

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
