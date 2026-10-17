using KanBagi.Furniture;
using KanBagi.Player;
using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D3 - Yerlestirme modunu kurar:
    ///  1) Player objesine FurniturePlacer ekler (yoksa)
    ///  2) Canvas'a alt-ortada ipucu yazisi (YerlestirmeIpucu) + PlacementHintUI koyar, oyuncuya baglar
    /// Tekrar calistirilabilir: ipucu yazisini silip yeniden kurar, bileseni cogaltmaz.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Yerlestirme Modunu Kur
    /// </summary>
    public static class PlacementModeBuilder
    {
        private const string HintName = "YerlestirmeIpucu";
        private static readonly Vector2 HintPosition = new Vector2(0f, 140f);
        private static readonly Vector2 HintSize = new Vector2(1400f, 50f);

        [MenuItem("Kan Bagi/Yerlestirme Modunu Kur")]
        public static void SetUp()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[PlacementModeBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            PlayerHands player = Object.FindFirstObjectByType<PlayerHands>(FindObjectsInactive.Include);
            if (player == null)
            {
                Debug.LogError("[PlacementModeBuilder] Sahnede oyuncu (PlayerHands) bulunamadi.");
                return;
            }

            if (!player.TryGetComponent(out FurniturePlacer placer))
                placer = Undo.AddComponent<FurniturePlacer>(player.gameObject);

            Canvas canvas = FindSceneCanvas();
            if (canvas == null)
            {
                Debug.LogError("[PlacementModeBuilder] Sahnede Canvas bulunamadi.");
                return;
            }

            foreach (PlacementHintUI old in Object.FindObjectsByType<PlacementHintUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Undo.DestroyObjectImmediate(old.gameObject);

            var hint = new GameObject(HintName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(hint, "Yerlestirme Ipucu");
            hint.transform.SetParent(canvas.transform, worldPositionStays: false);

            // Alt orta: etkilesim ipucunun ustunde.
            var rect = hint.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = HintPosition;
            rect.sizeDelta = HintSize;

            var text = hint.AddComponent<TextMeshProUGUI>();
            text.text = "Sol tik: Koy   |   Tekerlek: Dondur (Shift: 45)   |   Sag tik: Iptal";
            text.fontSize = 26f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            var hintUI = hint.AddComponent<PlacementHintUI>();
            var serialized = new SerializedObject(hintUI);
            serialized.FindProperty("placer").objectReferenceValue = placer;
            serialized.FindProperty("hintText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Tam ekran panellerin (hikaye, gun sonu) altinda cizilsin.
            hint.transform.SetSiblingIndex(FirstPanelIndex(canvas.transform));

            EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            Debug.Log($"[PlacementModeBuilder] '{player.name}' > FurniturePlacer, '{canvas.name}' > {HintName} kuruldu. Sahneyi kaydet (Ctrl+S).");
        }

        private static int FirstPanelIndex(Transform canvas)
        {
            int index = canvas.childCount - 1;
            foreach (Transform child in canvas)
            {
                if (child.GetComponent<StoryIntroUI>() != null || child.GetComponent<DailyReportUI>() != null)
                    index = Mathf.Min(index, child.GetSiblingIndex());
            }

            return index;
        }

        private static Canvas FindSceneCanvas()
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.isRootCanvas && canvas.renderMode != RenderMode.WorldSpace) return canvas;
            }

            return null;
        }
    }
}
