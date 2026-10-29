using System.Linq;
using System.Text;
using KanBagi.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D6b - PC panelinin her sayfasini fare tekerlegiyle kaydirilabilir yapar (GELISTIRME listesi genisletmelerle uzadi).
    /// Sayfa (sekmenin actigi obje) ScrollRect olur; icine Viewport (RectMask2D) > Icerik (VLG + ContentSizeFitter) kurulur.
    /// Sayfanin dikey dizilim ayarlari Icerik'e tasinir, mevcut cocuklar Icerik'e alinir ve satirlari ureten
    /// listelerin 'entryContainer' alani Icerik'e cevrilir (sayfayi gosteren veya bos olanlar).
    /// Tekrar calistirilabilir: ScrollRect'i olan sayfayi atlar.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > PC Sayfalarina Kaydirma Ekle
    /// </summary>
    public static class PcScrollBuilder
    {
        private const float ScrollSensitivity = 30f;

        [MenuItem("Kan Bagi/PC Sayfalarina Kaydirma Ekle")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PcScrollBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            TabGroupUI tabGroup = Object.FindFirstObjectByType<TabGroupUI>(FindObjectsInactive.Include);
            if (tabGroup == null)
            {
                Debug.LogError("[PcScrollBuilder] Sahnede TabGroupUI (BilgisayarPaneli) bulunamadi.");
                return;
            }

            var report = new StringBuilder("[PcScrollBuilder] PC sayfalari:\n");
            SerializedProperty tabs = new SerializedObject(tabGroup).FindProperty("tabs");

            for (int i = 0; i < tabs.arraySize; i++)
            {
                var page = tabs.GetArrayElementAtIndex(i).FindPropertyRelative("page").objectReferenceValue as GameObject;
                if (page == null) continue;

                report.AppendLine($"  {page.name}: {MakeScrollable(page)}");
            }

            EditorSceneManager.MarkSceneDirty(tabGroup.gameObject.scene);
            report.AppendLine("Sahneyi kaydet (Ctrl+S).");
            Debug.Log(report.ToString());
        }

        private static string MakeScrollable(GameObject page)
        {
            if (page.GetComponent<ScrollRect>() != null) return "zaten kaydiriliyor, atlandi";

            // Sayfanin dizilim ayarlari Icerik'e tasinacak.
            var pageLayout = page.GetComponent<VerticalLayoutGroup>();
            RectOffset padding = pageLayout != null ? new RectOffset(pageLayout.padding.left, pageLayout.padding.right,
                                                                     pageLayout.padding.top, pageLayout.padding.bottom)
                                                    : new RectOffset();
            float spacing = pageLayout != null ? pageLayout.spacing : 4f;
            TextAnchor alignment = pageLayout != null ? pageLayout.childAlignment : TextAnchor.UpperCenter;

            Transform[] oldChildren = page.transform.Cast<Transform>().ToArray();

            if (pageLayout != null) Undo.DestroyObjectImmediate(pageLayout);
            if (page.TryGetComponent(out ContentSizeFitter pageFitter)) Undo.DestroyObjectImmediate(pageFitter);

            // Viewport: sayfayi tamamen kaplar, tasan satirlari keser. Saydam Image bos alanda da tekerlegi yakalar.
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            Undo.RegisterCreatedObjectUndo(viewport, "Viewport");
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.SetParent(page.transform, false);
            Stretch(viewportRect);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

            // Icerik: ustten baslar, boyu satirlar kadar uzar.
            var content = new GameObject("Icerik", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            Undo.RegisterCreatedObjectUndo(content, "Icerik");
            var contentRect = (RectTransform)content.transform;
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = padding;
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            foreach (Transform child in oldChildren)
                Undo.SetTransformParent(child, contentRect, "Icerik");

            var scroll = Undo.AddComponent<ScrollRect>(page);
            scroll.content = contentRect;
            scroll.viewport = viewportRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = ScrollSensitivity;

            int rewired = RewireContainers(page, contentRect);
            return $"kaydirma eklendi, {oldChildren.Length} cocuk tasindi, {rewired} liste Icerik'e baglandi";
        }

        /// <summary>Satir ureten listelerin 'entryContainer'i sayfaysa (veya sayfadaki listede bossa) Icerik olur.</summary>
        private static int RewireContainers(GameObject page, Transform content)
        {
            int count = 0;

            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null) continue;

                var serialized = new SerializedObject(behaviour);
                SerializedProperty container = serialized.FindProperty("entryContainer");
                if (container == null || container.propertyType != SerializedPropertyType.ObjectReference) continue;

                Object current = container.objectReferenceValue;
                bool pointsToPage = current == page.transform;
                bool emptyOnPage = current == null && behaviour.gameObject == page;
                if (!pointsToPage && !emptyOnPage) continue;

                container.objectReferenceValue = content;
                serialized.ApplyModifiedProperties();
                count++;
            }

            return count;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
