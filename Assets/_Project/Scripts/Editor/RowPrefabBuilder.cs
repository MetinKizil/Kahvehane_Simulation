using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Bilgisayar ekranindaki satir prefablarini KOD ILE uretir.
    /// Elle onlarca alan doldurmak yerine menuden tek tikla dogru olculerle olusturulur;
    /// prefab bozulursa veya olculer degisirse menu yeniden calistirilir.
    ///
    /// SADECE EDITOR: 'Editor' klasorundeki kodlar oyun derlemesine dahil edilmez.
    /// Menu: Kan Bagi > Satir Prefablarini Olustur
    /// </summary>
    public static class RowPrefabBuilder
    {
        private const string ProjectFolder = "Assets/_Project";
        private const string PrefabFolder = ProjectFolder + "/Prefabs";
        private const string ItemRowPath = PrefabFolder + "/UrunSatiri.prefab";
        private const string UpgradeRowPath = PrefabFolder + "/UpgradeSatiri.prefab";
        private const string PropertyRowPath = PrefabFolder + "/EmlakSatiri.prefab";

        // Satir olculeri. Iki prefab da ayni iskeleti kullanir; buradan degistirilir.
        private const float RowWidth = 640f;
        private const float RowHeight = 56f;
        private const float NameWidth = 170f;
        private const float ButtonWidth = 120f;
        private const float PropertyButtonWidth = 180f;
        private const float ButtonHeight = 40f;
        private const float NameFontSize = 20f;
        private const float DetailFontSize = 18f;
        private const float ButtonFontSize = 18f;

        private static readonly Color RowColor = new Color(0.16f, 0.16f, 0.18f, 0.85f);
        private static readonly Color ButtonColor = new Color(0.25f, 0.45f, 0.75f, 1f);
        private static readonly Color TextColor = Color.white;

        [MenuItem("Kan Bagi/Satir Prefablarini Olustur")]
        public static void BuildRowPrefabs()
        {
            EnsurePrefabFolder();

            CreateItemRow();
            CreateUpgradeRow();
            CreatePropertyRow();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RowPrefabBuilder] Satir prefablari olusturuldu:\n{ItemRowPath}\n{UpgradeRowPath}\n{PropertyRowPath}\n" +
                      "Unutma: ComputerUI, UpgradeListUI ve PropertyListUI'daki 'Entry Prefab' alanlarini yeniden bagla.");
        }

        /// <summary>Malzeme satiri: ad, detay, satin al butonu.</summary>
        private static void CreateItemRow()
        {
            GameObject root = CreateRowRoot("UrunSatiri");

            TextMeshProUGUI nameText = CreateText(root.transform, "AdText", NameFontSize, TextAlignmentOptions.MidlineLeft, "Malzeme");
            AddLayoutElement(nameText.gameObject, NameWidth, flexibleWidth: 0f);

            TextMeshProUGUI detailText = CreateText(root.transform, "DetayText", DetailFontSize, TextAlignmentOptions.MidlineLeft, "10 adet - 20 TL");
            AddLayoutElement(detailText.gameObject, preferredWidth: -1f, flexibleWidth: 1f);

            Button buyButton = CreateButton(root.transform, "SatinAlButton", "Satin Al", out _);

            var entry = root.AddComponent<CatalogEntryUI>();
            var serialized = new SerializedObject(entry);
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("detailText").objectReferenceValue = detailText;
            serialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SaveAndCleanup(root, ItemRowPath);
        }

        /// <summary>Gelistirme satiri: ad, aciklama, fiyat/Alindi butonu.</summary>
        private static void CreateUpgradeRow()
        {
            GameObject root = CreateRowRoot("UpgradeSatiri");

            TextMeshProUGUI nameText = CreateText(root.transform, "AdText", NameFontSize, TextAlignmentOptions.MidlineLeft, "Gelistirme");
            AddLayoutElement(nameText.gameObject, NameWidth, flexibleWidth: 0f);

            TextMeshProUGUI descriptionText = CreateText(root.transform, "AciklamaText", DetailFontSize, TextAlignmentOptions.MidlineLeft, "Aciklama");
            AddLayoutElement(descriptionText.gameObject, preferredWidth: -1f, flexibleWidth: 1f);

            Button buyButton = CreateButton(root.transform, "SatinAlButton", "100 TL", out TextMeshProUGUI buttonLabel);

            var entry = root.AddComponent<UpgradeEntryUI>();
            var serialized = new SerializedObject(entry);
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("descriptionText").objectReferenceValue = descriptionText;
            serialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            serialized.FindProperty("buyButtonLabel").objectReferenceValue = buttonLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SaveAndCleanup(root, UpgradeRowPath);
        }

        /// <summary>Emlak satiri: ad, gunluk gelir + aciklama, fiyat/itibar-sarti butonu.</summary>
        private static void CreatePropertyRow()
        {
            GameObject root = CreateRowRoot("EmlakSatiri");

            TextMeshProUGUI nameText = CreateText(root.transform, "AdText", NameFontSize, TextAlignmentOptions.MidlineLeft, "Mulk");
            AddLayoutElement(nameText.gameObject, NameWidth, flexibleWidth: 0f);

            TextMeshProUGUI detailText = CreateText(root.transform, "DetayText", DetailFontSize, TextAlignmentOptions.MidlineLeft, "Gunluk 60 TL");
            AddLayoutElement(detailText.gameObject, preferredWidth: -1f, flexibleWidth: 1f);

            // "Itibar 35 gerekli" yazisi sigsin diye bu satirin butonu daha genis.
            Button buyButton = CreateButton(root.transform, "SatinAlButton", "800 TL", out TextMeshProUGUI buttonLabel, PropertyButtonWidth);

            var entry = root.AddComponent<PropertyEntryUI>();
            var serialized = new SerializedObject(entry);
            serialized.FindProperty("nameText").objectReferenceValue = nameText;
            serialized.FindProperty("detailText").objectReferenceValue = detailText;
            serialized.FindProperty("buyButton").objectReferenceValue = buyButton;
            serialized.FindProperty("buyButtonLabel").objectReferenceValue = buttonLabel;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SaveAndCleanup(root, PropertyRowPath);
        }

        /// <summary>Satirin govdesi: arka plan + yatay dizilim + sabit yukseklik.</summary>
        private static GameObject CreateRowRoot(string objectName)
        {
            var root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

            root.GetComponent<Image>().color = RowColor;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(RowWidth, RowHeight);

            HorizontalLayoutGroup layoutGroup = root.AddComponent<HorizontalLayoutGroup>();
            layoutGroup.padding = new RectOffset(12, 12, 6, 6);
            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = true;

            LayoutElement element = root.AddComponent<LayoutElement>();
            element.preferredHeight = RowHeight;
            element.flexibleHeight = 0f;

            return root;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string objectName, float fontSize,
                                                  TextAlignmentOptions alignment, string placeholder)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, worldPositionStays: false);

            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.text = placeholder;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = TextColor;
            text.raycastTarget = false;

            return text;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, out TextMeshProUGUI labelText,
                                           float width = ButtonWidth)
        {
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            buttonObject.transform.SetParent(parent, worldPositionStays: false);

            Image image = buttonObject.GetComponent<Image>();
            image.color = ButtonColor;

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;

            AddLayoutElement(buttonObject, width, flexibleWidth: 0f, preferredHeight: ButtonHeight);

            labelText = CreateText(buttonObject.transform, "Text (TMP)", ButtonFontSize, TextAlignmentOptions.Center, label);

            // Buton yazisi butonu tam kaplasin (butonun altinda layout group yok, anchor ile).
            RectTransform labelRect = labelText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return button;
        }

        private static void AddLayoutElement(GameObject target, float preferredWidth, float flexibleWidth,
                                             float preferredHeight = -1f)
        {
            LayoutElement element = target.AddComponent<LayoutElement>();
            element.preferredWidth = preferredWidth;
            element.flexibleWidth = flexibleWidth;
            element.preferredHeight = preferredHeight;
        }

        private static void SaveAndCleanup(GameObject root, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
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
