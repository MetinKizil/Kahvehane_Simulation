using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Gun sonu ozet panelini KOD ILE uretir; ic baglantilari hazir gelir.
    /// Kullanicinin tek isi prefab'i Canvas'in altina suruklemek.
    ///
    /// Yapi:  Kok (hep aktif, gorunmez)  >  Icerik (karartma, acilip kapanir)  >  Panel (govde)
    /// Kokte gorsel YOKTUR - olsaydi ekrani surekli kapatirdi.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Gunluk Ozet Panelini Olustur
    /// </summary>
    public static class ReportPanelBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string PanelPath = PrefabFolder + "/GunlukOzetPaneli.prefab";

        private const float PanelWidth = 560f;
        private const float PanelHeight = 560f;
        private const float RowHeight = 38f;
        private const float TitleHeight = 52f;
        private const float ButtonHeight = 52f;

        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.55f);
        private static readonly Color PanelColor = new Color(0.14f, 0.14f, 0.16f, 0.98f);
        private static readonly Color ButtonColor = new Color(0.25f, 0.45f, 0.75f, 1f);
        private static readonly Color TextColor = Color.white;

        [MenuItem("Kan Bagi/Gunluk Ozet Panelini Olustur")]
        public static void BuildReportPanel()
        {
            EnsurePrefabFolder();

            // Kok HEP AKTIF kalir (kapali obje event dinleyemez) ve gorseli yoktur.
            GameObject root = CreateStretchedObject("GunlukOzetPaneli", null, withImage: false);

            // Acilip kapanan kisim: tum ekrani karartan katman.
            GameObject content = CreateStretchedObject("Icerik", root.transform, withImage: true);
            content.GetComponent<Image>().color = BackdropColor;

            GameObject panel = CreatePanelBody(content.transform);

            TextMeshProUGUI title = CreateRow(panel.transform, "BaslikText", "GUN 1 OZETI", 30f, TitleHeight, TextAlignmentOptions.Center);
            TextMeshProUGUI served = CreateRow(panel.transform, "ServisText", "Servis edilen musteri: 0", 22f, RowHeight, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI lost = CreateRow(panel.transform, "KacirilanText", "Kacirilan musteri: 0", 22f, RowHeight, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI revenue = CreateRow(panel.transform, "CiroText", "Ciro: 0 TL", 22f, RowHeight, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI rent = CreateRow(panel.transform, "KiraText", "Kira geliri: 0 TL", 22f, RowHeight, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI gambling = CreateRow(panel.transform, "KumarText", "Kumar: 0 TL", 22f, RowHeight, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI expense = CreateRow(panel.transform, "GiderText", "Gider: 0 TL", 22f, RowHeight, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI profit = CreateRow(panel.transform, "NetText", "Net: 0 TL", 26f, RowHeight + 6f, TextAlignmentOptions.MidlineLeft);
            TextMeshProUGUI reputation = CreateRow(panel.transform, "ItibarText", "Itibar: 0  (+0)", 22f, RowHeight, TextAlignmentOptions.MidlineLeft);

            Button nextDayButton = CreateButton(panel.transform, "YeniGunButton", "YENI GUN");

            var report = root.AddComponent<DailyReportUI>();
            var serialized = new SerializedObject(report);
            serialized.FindProperty("contentRoot").objectReferenceValue = content;
            serialized.FindProperty("titleText").objectReferenceValue = title;
            serialized.FindProperty("servedText").objectReferenceValue = served;
            serialized.FindProperty("lostText").objectReferenceValue = lost;
            serialized.FindProperty("revenueText").objectReferenceValue = revenue;
            serialized.FindProperty("rentText").objectReferenceValue = rent;
            serialized.FindProperty("gamblingText").objectReferenceValue = gambling;
            serialized.FindProperty("expenseText").objectReferenceValue = expense;
            serialized.FindProperty("profitText").objectReferenceValue = profit;
            serialized.FindProperty("reputationText").objectReferenceValue = reputation;
            serialized.FindProperty("nextDayButton").objectReferenceValue = nextDayButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[ReportPanelBuilder] Panel olusturuldu: {PanelPath}\n" +
                      "Prefab'i Canvas'in ALTINA surukle. Ic baglantilar hazir, elle alan doldurmana gerek yok.");
        }

        /// <summary>Ebeveynini tam kaplayan bir UI objesi olusturur.</summary>
        private static GameObject CreateStretchedObject(string objectName, Transform parent, bool withImage)
        {
            GameObject target = withImage
                ? new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                : new GameObject(objectName, typeof(RectTransform));

            if (parent != null) target.transform.SetParent(parent, worldPositionStays: false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return target;
        }

        /// <summary>Ortadaki panel govdesi: basliklar ve buton burada dizilir.</summary>
        private static GameObject CreatePanelBody(Transform parent)
        {
            var content = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            content.transform.SetParent(parent, worldPositionStays: false);

            content.GetComponent<Image>().color = PanelColor;

            RectTransform rect = content.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(28, 28, 22, 22);
            layoutGroup.spacing = 8f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            return content;
        }

        private static TextMeshProUGUI CreateRow(Transform parent, string objectName, string placeholder,
                                                 float fontSize, float height, TextAlignmentOptions alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, worldPositionStays: false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = placeholder;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;

            LayoutElement element = textObject.AddComponent<LayoutElement>();
            element.preferredHeight = height;
            element.flexibleHeight = 0f;

            return text;
        }

        private static Button CreateButton(Transform parent, string objectName, string label)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            buttonObject.transform.SetParent(parent, worldPositionStays: false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = ButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            LayoutElement element = buttonObject.AddComponent<LayoutElement>();
            element.preferredHeight = ButtonHeight;
            element.flexibleHeight = 0f;

            var labelObject = new GameObject("Text (TMP)", typeof(RectTransform));
            labelObject.transform.SetParent(buttonObject.transform, worldPositionStays: false);

            TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 24f;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = TextColor;
            labelText.raycastTarget = false;

            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

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
