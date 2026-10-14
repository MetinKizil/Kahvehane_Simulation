using KanBagi.Data;
using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Hikaye giris panelini KOD ILE uretir, GirisHikayesi verisini baglar ve
    /// sahnedeki Canvas'in son cocugu olarak yerlestirir (eskisi varsa yenisiyle degistirir).
    ///
    /// Yapi:  Kok (hep aktif, gorunmez)  >  Icerik (siyah perde, acilip kapanir)  >  Panel (metin + butonlar)
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Hikaye Giris Panelini Olustur
    /// </summary>
    public static class StoryPanelBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string PanelPath = PrefabFolder + "/HikayeGirisPaneli.prefab";
        private const string StoryPath = ProjectFolder + "/ScriptableObjects/GirisHikayesi.asset";

        private const float PanelWidth = 1300f;
        private const float PanelHeight = 720f;
        private const float ButtonWidth = 260f;
        private const float ButtonHeight = 64f;

        private static readonly Color BackdropColor = Color.black;
        private static readonly Color TitleColor = new Color(0.85f, 0.62f, 0.32f);
        private static readonly Color BodyColor = new Color(0.9f, 0.88f, 0.84f);
        private static readonly Color PageColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color ContinueColor = new Color(0.55f, 0.18f, 0.14f);
        private static readonly Color SkipColor = new Color(0.22f, 0.22f, 0.24f);

        [MenuItem("Kan Bagi/Hikaye Giris Panelini Olustur")]
        public static void BuildStoryPanel()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[StoryPanelBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var story = AssetDatabase.LoadAssetAtPath<StoryData>(StoryPath);
            if (story == null)
            {
                Debug.LogError($"[StoryPanelBuilder] Hikaye verisi bulunamadi: {StoryPath}");
                return;
            }

            Canvas canvas = FindSceneCanvas();
            if (canvas == null)
            {
                Debug.LogError("[StoryPanelBuilder] Sahnede Canvas bulunamadi.");
                return;
            }

            GameObject prefab = BuildPrefab(story);
            if (prefab == null) return;

            PlaceInScene(prefab, canvas);

            Debug.Log($"[StoryPanelBuilder] Panel olusturuldu ({PanelPath}) ve '{canvas.name}' altina yerlestirildi. " +
                      "Sahneyi kaydet (Ctrl+S). Test icin once 'Kan Bagi > Kaydi Sil'.");
        }

        private static GameObject BuildPrefab(StoryData story)
        {
            EnsurePrefabFolder();

            // Kok HEP AKTIF kalir (gorseli yok, ekrani kapatmaz); acilip kapanan Icerik'tir.
            GameObject root = CreateStretchedObject("HikayeGirisPaneli", null, withImage: false);

            GameObject content = CreateStretchedObject("Icerik", root.transform, withImage: true);
            content.GetComponent<Image>().color = BackdropColor;

            GameObject panel = CreatePanelBody(content.transform);

            TextMeshProUGUI title = CreateText(panel.transform, "BaslikText", 60f, TitleColor,
                                               TextAlignmentOptions.MidlineLeft, preferredHeight: 84f, flexible: false);
            title.fontStyle = FontStyles.Bold;

            TextMeshProUGUI body = CreateText(panel.transform, "MetinText", 38f, BodyColor,
                                              TextAlignmentOptions.TopLeft, preferredHeight: 0f, flexible: true);
            body.lineSpacing = 12f;

            TextMeshProUGUI page = CreateText(panel.transform, "SayfaText", 24f, PageColor,
                                              TextAlignmentOptions.MidlineRight, preferredHeight: 28f, flexible: false);

            Transform buttonRow = CreateButtonRow(panel.transform);
            Button skipButton = CreateButton(buttonRow, "AtlaButton", SkipColor, out TextMeshProUGUI skipLabel);
            Button continueButton = CreateButton(buttonRow, "DevamButton", ContinueColor, out TextMeshProUGUI continueLabel);

            // Ilk sayfa prefab onizlemesinde de okunur gorunsun.
            if (story.Pages.Count > 0)
            {
                title.text = story.Pages[0].Title;
                body.text = story.Pages[0].Body;
            }
            page.text = $"1 / {story.Pages.Count}";
            skipLabel.text = story.SkipLabel;
            continueLabel.text = story.ContinueLabel;

            var intro = root.AddComponent<StoryIntroUI>();
            var serialized = new SerializedObject(intro);
            serialized.FindProperty("story").objectReferenceValue = story;
            serialized.FindProperty("contentRoot").objectReferenceValue = content;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("bodyText").objectReferenceValue = body;
            serialized.FindProperty("pageText").objectReferenceValue = page;
            serialized.FindProperty("continueButton").objectReferenceValue = continueButton;
            serialized.FindProperty("continueLabel").objectReferenceValue = continueLabel;
            serialized.FindProperty("skipButton").objectReferenceValue = skipButton;
            serialized.FindProperty("skipLabel").objectReferenceValue = skipLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();

            if (saved == null) Debug.LogError("[StoryPanelBuilder] Prefab kaydedilemedi.");
            return saved;
        }

        /// <summary>Eski paneli siler, yenisini Canvas'in son cocugu (en ustte cizilen) yapar.</summary>
        private static void PlaceInScene(GameObject prefab, Canvas canvas)
        {
            foreach (StoryIntroUI old in Object.FindObjectsByType<StoryIntroUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Undo.DestroyObjectImmediate(old.gameObject);

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Hikaye Giris Paneli");

            instance.transform.SetAsLastSibling();
            Stretch(instance.GetComponent<RectTransform>());

            EditorSceneManager.MarkSceneDirty(instance.scene);
        }

        /// <summary>Sahnedeki ana (kok) Canvas. Dunya uzayindaki tabela canvas'lari atlanir.</summary>
        private static Canvas FindSceneCanvas()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace) return canvas;
            }

            return null;
        }

        private static GameObject CreateStretchedObject(string objectName, Transform parent, bool withImage)
        {
            GameObject target = withImage
                ? new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                : new GameObject(objectName, typeof(RectTransform));

            if (parent != null) target.transform.SetParent(parent, worldPositionStays: false);

            Stretch(target.GetComponent<RectTransform>());
            return target;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.localScale = Vector3.one;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Ortadaki seffaf govde: baslik, metin, sayfa no ve butonlar alt alta dizilir.</summary>
        private static GameObject CreatePanelBody(Transform parent)
        {
            var panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(parent, worldPositionStays: false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string objectName, float fontSize, Color color,
                                                  TextAlignmentOptions alignment, float preferredHeight, bool flexible)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, worldPositionStays: false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            LayoutElement element = textObject.AddComponent<LayoutElement>();
            if (flexible) element.flexibleHeight = 1f;
            else element.preferredHeight = preferredHeight;

            return text;
        }

        /// <summary>Butonlar sag altta yan yana: [Atla] [Devam].</summary>
        private static Transform CreateButtonRow(Transform parent)
        {
            var row = new GameObject("ButonSatiri", typeof(RectTransform));
            row.transform.SetParent(parent, worldPositionStays: false);

            HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            LayoutElement element = row.AddComponent<LayoutElement>();
            element.preferredHeight = ButtonHeight;

            return row.transform;
        }

        private static Button CreateButton(Transform parent, string objectName, Color color, out TextMeshProUGUI label)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            buttonObject.transform.SetParent(parent, worldPositionStays: false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = color;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            LayoutElement element = buttonObject.AddComponent<LayoutElement>();
            element.preferredWidth = ButtonWidth;
            element.preferredHeight = ButtonHeight;

            var labelObject = new GameObject("Text (TMP)", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, worldPositionStays: false);

            label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = 30f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            Stretch(label.rectTransform);

            return button;
        }

        private static void EnsurePrefabFolder()
        {
            if (AssetDatabase.IsValidFolder(PrefabFolder)) return;

            if (!AssetDatabase.IsValidFolder(ProjectFolder))
                AssetDatabase.CreateFolder("Assets", "_Project");

            AssetDatabase.CreateFolder(ProjectFolder, "Prefabs");
        }
    }
}
