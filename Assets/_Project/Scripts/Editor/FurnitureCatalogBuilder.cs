using System.Linq;
using System.Text;
using KanBagi.Core;
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
    /// D4 - PC'ye ESYALAR sekmesini kurar ve PC panelinin gorunumunu duzeltir:
    ///  1) 'EsyaSatiri' satir prefab'i (UpgradeSatiri'ndan, FurnitureEntryUI ile)
    ///  2) Son sekme kopyalanarak ESYALAR sekme butonu + sayfasi (FurnitureListUI), TabGroupUI'a eklenir
    ///  3) "Yeni Masa" gelistirme katalogundan cikarilir, eski hibe bileseninin kalintisi (Missing Script) silinir
    ///  4) Panel gorunumu: sekme butonlari cubugu doldurur (ince serit degil), kapali sekme rengi acilir,
    ///     sekme yazilari beyaz; Kaydet butonunun gorunmeyen eski (Legacy) yazisi TMP'ye cevrilir
    /// Tekrar calistirilabilir: ESYALAR sekmesi varsa yeniden olusturmaz, sadece gunceller.
    /// Turkce yazilar (DUKKAN YONETIMI, sekme basliklari) icin sonra 'Kan Bagi > Metinleri Uygula'.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Esya Katalogunu Kur
    /// </summary>
    public static class FurnitureCatalogBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string UpgradeRowPath = PrefabFolder + "/UpgradeSatiri.prefab";
        private const string FurnitureRowPath = PrefabFolder + "/EsyaSatiri.prefab";
        private const string CatalogPath = "Assets/_Project/ScriptableObjects/EsyaKatalogu.asset";
        private const string RemovedUpgradePath = "Assets/_Project/ScriptableObjects/YeniMasaUpgrade.asset";

        private const float TabBarHeight = 44f;
        private static readonly Color InactiveTabColor = new Color(0.32f, 0.32f, 0.36f, 1f);
        private static readonly Color TabLabelColor = Color.white;
        private static readonly Color SaveLabelColor = new Color(0.12f, 0.12f, 0.12f, 1f);

        [MenuItem("Kan Bagi/Esya Katalogunu Kur")]
        public static void SetUp()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[FurnitureCatalogBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogError($"[FurnitureCatalogBuilder] Esya katalogu bulunamadi: {CatalogPath}");
                return;
            }

            TabGroupUI tabGroup = Object.FindFirstObjectByType<TabGroupUI>(FindObjectsInactive.Include);
            if (tabGroup == null)
            {
                Debug.LogError("[FurnitureCatalogBuilder] Sahnede TabGroupUI (BilgisayarPaneli) bulunamadi.");
                return;
            }

            var report = new StringBuilder("[FurnitureCatalogBuilder] Kuruldu:\n");

            FurnitureEntryUI rowPrefab = BuildRowPrefab();
            if (rowPrefab == null) return;
            report.AppendLine($"  Satir prefab'i: {FurnitureRowPath}");

            report.AppendLine($"  Sekme: {SetUpTab(tabGroup, catalog, rowPrefab)}");
            report.AppendLine($"  Yeni Masa: {RemoveOldTableUpgrade()}");
            report.AppendLine($"  Panel gorunumu: {FixPanelLook(tabGroup)}");

            EditorSceneManager.MarkSceneDirty(tabGroup.gameObject.scene);
            report.AppendLine("Sahneyi kaydet (Ctrl+S), sonra 'Kan Bagi > Metinleri Uygula'.");
            Debug.Log(report.ToString());
        }

        /// <summary>UpgradeSatiri'nin gorunumu aynen kullanilir; sadece davranis bileseni degisir.</summary>
        private static FurnitureEntryUI BuildRowPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UpgradeRowPath);
            try
            {
                if (!root.TryGetComponent(out UpgradeEntryUI upgradeRow))
                {
                    Debug.LogError($"[FurnitureCatalogBuilder] {UpgradeRowPath} icinde UpgradeEntryUI yok.");
                    return null;
                }

                var source = new SerializedObject(upgradeRow);
                Object nameText = source.FindProperty("nameText").objectReferenceValue;
                Object descriptionText = source.FindProperty("descriptionText").objectReferenceValue;
                Object buyButton = source.FindProperty("buyButton").objectReferenceValue;
                Object buyButtonLabel = source.FindProperty("buyButtonLabel").objectReferenceValue;

                Object.DestroyImmediate(upgradeRow);
                root.name = "EsyaSatiri";

                var row = root.AddComponent<FurnitureEntryUI>();
                var target = new SerializedObject(row);
                target.FindProperty("nameText").objectReferenceValue = nameText;
                target.FindProperty("descriptionText").objectReferenceValue = descriptionText;
                target.FindProperty("buyButton").objectReferenceValue = buyButton;
                target.FindProperty("buyButtonLabel").objectReferenceValue = buyButtonLabel;
                target.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, FurnitureRowPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(FurnitureRowPath);
            return saved != null ? saved.GetComponent<FurnitureEntryUI>() : null;
        }

        /// <summary>Son sekmeyi (EMLAK) kopyalayip ESYALAR sekmesini kurar; varsa sadece baglantilari tazeler.</summary>
        private static string SetUpTab(TabGroupUI tabGroup, FurnitureCatalog catalog, FurnitureEntryUI rowPrefab)
        {
            var tabGroupData = new SerializedObject(tabGroup);
            SerializedProperty tabs = tabGroupData.FindProperty("tabs");

            FurnitureListUI existing = tabGroup.GetComponentsInChildren<FurnitureListUI>(true).FirstOrDefault();
            if (existing != null)
            {
                WireList(existing, catalog, rowPrefab);
                return "zaten vardi, baglantilar guncellendi";
            }

            SerializedProperty last = tabs.GetArrayElementAtIndex(tabs.arraySize - 1);
            var templateButton = last.FindPropertyRelative("button").objectReferenceValue as Button;
            var templatePage = last.FindPropertyRelative("page").objectReferenceValue as GameObject;
            if (templateButton == null || templatePage == null) return "HATA: son sekmenin butonu/sayfasi yok";

            // Buton
            GameObject button = Object.Instantiate(templateButton.gameObject, templateButton.transform.parent);
            Undo.RegisterCreatedObjectUndo(button, "Esya Sekmesi");
            button.name = "EsyaSekmeButton";
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = catalog.TabTitle;

            // Sayfa: sablonun davranis bilesenleri ve cocuklari temizlenir, gorunumu (layout) kalir.
            GameObject page = Object.Instantiate(templatePage, templatePage.transform.parent);
            Undo.RegisterCreatedObjectUndo(page, "Esya Sayfasi");
            page.name = "EsyaSayfasi";

            foreach (MonoBehaviour behaviour in page.GetComponents<MonoBehaviour>())
            {
                if (!(behaviour is LayoutGroup) && !(behaviour is ContentSizeFitter) && !(behaviour is LayoutElement) && !(behaviour is Graphic))
                    Object.DestroyImmediate(behaviour);
            }

            foreach (Transform child in page.transform.Cast<Transform>().ToArray())
                Object.DestroyImmediate(child.gameObject);

            var list = page.AddComponent<FurnitureListUI>();
            WireList(list, catalog, rowPrefab);
            page.SetActive(false);

            tabs.arraySize++;
            SerializedProperty added = tabs.GetArrayElementAtIndex(tabs.arraySize - 1);
            added.FindPropertyRelative("button").objectReferenceValue = button.GetComponent<Button>();
            added.FindPropertyRelative("page").objectReferenceValue = page;
            tabGroupData.ApplyModifiedProperties();

            return $"'{catalog.TabTitle}' eklendi ({tabs.arraySize}. sekme)";
        }

        private static void WireList(FurnitureListUI list, FurnitureCatalog catalog, FurnitureEntryUI rowPrefab)
        {
            var data = new SerializedObject(list);
            data.FindProperty("catalog").objectReferenceValue = catalog;
            data.FindProperty("entryContainer").objectReferenceValue = list.transform;
            data.FindProperty("entryPrefab").objectReferenceValue = rowPrefab;
            data.ApplyModifiedProperties();
        }

        /// <summary>Masa artik ESYALAR'dan alinir: "Yeni Masa" katalogdan cikar, silinen hibe bileseninin kalintisi temizlenir.</summary>
        private static string RemoveOldTableUpgrade()
        {
            var parts = new StringBuilder();

            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            var oldUpgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(RemovedUpgradePath);
            var catalog = upgradeManager != null
                ? new SerializedObject(upgradeManager).FindProperty("catalog").objectReferenceValue as UpgradeCatalog
                : null;

            if (catalog != null && oldUpgrade != null)
            {
                var catalogData = new SerializedObject(catalog);
                SerializedProperty list = catalogData.FindProperty("upgrades");
                bool removed = false;

                for (int i = list.arraySize - 1; i >= 0; i--)
                {
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue != oldUpgrade) continue;

                    list.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    list.DeleteArrayElementAtIndex(i);
                    removed = true;
                }

                catalogData.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssets();
                parts.Append(removed ? "gelistirme katalogundan cikarildi" : "katalogda zaten yoktu");
            }

            if (upgradeManager != null)
            {
                int missing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(upgradeManager.gameObject);
                if (missing > 0) parts.Append($", {missing} kayip bilesen (eski hibe) silindi");
            }

            return parts.Length > 0 ? parts.ToString() : "yapilacak bir sey yok";
        }

        /// <summary>Sekme cubugu ve Kaydet butonu: okunur ve dolgun gorunsun.</summary>
        private static string FixPanelLook(TabGroupUI tabGroup)
        {
            var tabGroupData = new SerializedObject(tabGroup);
            tabGroupData.FindProperty("inactiveColor").colorValue = InactiveTabColor;
            tabGroupData.ApplyModifiedProperties();

            SerializedProperty tabs = tabGroupData.FindProperty("tabs");
            Transform tabBar = null;

            for (int i = 0; i < tabs.arraySize; i++)
            {
                var button = tabs.GetArrayElementAtIndex(i).FindPropertyRelative("button").objectReferenceValue as Button;
                if (button == null) continue;

                tabBar = button.transform.parent;

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;

                Undo.RecordObject(label, "Sekme Yazisi");
                label.color = TabLabelColor;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                EditorUtility.SetDirty(label);
            }

            // Butonlar cubugun yuksekligini doldursun ve esit genislikte olsun.
            if (tabBar != null)
            {
                if (tabBar.TryGetComponent(out HorizontalLayoutGroup layout))
                {
                    Undo.RecordObject(layout, "Sekme Cubugu");
                    layout.childControlHeight = true;
                    layout.childForceExpandHeight = true;
                    layout.childControlWidth = true;
                    layout.childForceExpandWidth = true;
                    EditorUtility.SetDirty(layout);
                }

                if (tabBar.TryGetComponent(out LayoutElement element))
                {
                    Undo.RecordObject(element, "Sekme Cubugu");
                    element.preferredHeight = TabBarHeight;
                    element.minHeight = TabBarHeight;
                    // KRITIK: childForceExpandHeight acik bir layout grubu kendini "esnek" bildirir; panelde bos yer
                    // kalinca (az urunlu sayfa) cubuk da buyur ve sekmeler dev kareye doner. Esneklik 0'a kilitlenir.
                    element.flexibleHeight = 0f;
                    EditorUtility.SetDirty(element);
                }
            }

            return $"sekme cubugu {TabBarHeight} px, kapali sekme acik gri, yazilar beyaz; Kaydet: {FixSaveButton(tabGroup)}";
        }

        /// <summary>Kaydet butonundaki eski Legacy Text Unity 6'da gorunmuyor: TMP yaziyla degistirilir.</summary>
        private static string FixSaveButton(TabGroupUI tabGroup)
        {
            Transform saveButton = tabGroup.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "KaydetButton");
            if (saveButton == null) return "buton bulunamadi";

            if (saveButton.GetComponentInChildren<TMP_Text>(true) != null) return "zaten TMP";

            foreach (Text legacy in saveButton.GetComponentsInChildren<Text>(true))
                Undo.DestroyObjectImmediate(legacy.gameObject);

            var labelObject = new GameObject("Text (TMP)", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(labelObject, "Kaydet Yazisi");
            labelObject.transform.SetParent(saveButton, worldPositionStays: false);

            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.text = "Kaydet";
            label.fontSize = 20f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = SaveLabelColor;
            label.raycastTarget = false;

            return "yazi TMP'ye cevrildi";
        }
    }
}
