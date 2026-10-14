using KanBagi.Data;
using KanBagi.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// "Mahalle fisiltilari" satirini sahnedeki Canvas'a KOD ILE kurar ve veriyi baglar.
    /// Eskisi varsa silip yenisini koyar. Satir, tam ekran panellerin (hikaye girisi, gun sonu)
    /// ALTINDA cizilir ki onlar acikken uste binmesin.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Fisilti Satirini Olustur
    /// </summary>
    public static class WhisperLineBuilder
    {
        private const string WhispersPath = "Assets/_Project/ScriptableObjects/MahalleFisiltilari.asset";

        // Ust orta: soldaki para yazisi ile sagdaki saatin arasinda kalir.
        private static readonly Vector2 Position = new Vector2(0f, -70f);
        private static readonly Vector2 Size = new Vector2(1000f, 60f);
        private const float FontSize = 30f;
        private static readonly Color TextColor = new Color(0.85f, 0.62f, 0.32f);

        [MenuItem("Kan Bagi/Fisilti Satirini Olustur")]
        public static void BuildWhisperLine()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[WhisperLineBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            var whispers = AssetDatabase.LoadAssetAtPath<WhisperData>(WhispersPath);
            if (whispers == null)
            {
                Debug.LogError($"[WhisperLineBuilder] Fisilti verisi bulunamadi: {WhispersPath}");
                return;
            }

            Canvas canvas = FindSceneCanvas();
            if (canvas == null)
            {
                Debug.LogError("[WhisperLineBuilder] Sahnede Canvas bulunamadi.");
                return;
            }

            foreach (WhisperUI old in Object.FindObjectsByType<WhisperUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Undo.DestroyObjectImmediate(old.gameObject);

            var line = new GameObject("FisiltiText", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(line, "Fisilti Satiri");
            line.transform.SetParent(canvas.transform, worldPositionStays: false);

            RectTransform rect = line.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Position;
            rect.sizeDelta = Size;

            TextMeshProUGUI text = line.AddComponent<TextMeshProUGUI>();
            text.text = whispers.FirstMorning;
            text.fontSize = FontSize;
            text.fontStyle = FontStyles.Italic;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            var whisperUI = line.AddComponent<WhisperUI>();
            var serialized = new SerializedObject(whisperUI);
            serialized.FindProperty("whispers").objectReferenceValue = whispers;
            serialized.FindProperty("messageText").objectReferenceValue = text;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PlaceBelowFullScreenPanels(line.transform, canvas.transform);

            EditorSceneManager.MarkSceneDirty(line.scene);
            Debug.Log($"[WhisperLineBuilder] 'FisiltiText' '{canvas.name}' altina kuruldu. Sahneyi kaydet (Ctrl+S).");
        }

        /// <summary>Hikaye girisi ve gun sonu panellerinden hemen once siralanir (onlar ustte cizilsin).</summary>
        private static void PlaceBelowFullScreenPanels(Transform line, Transform canvas)
        {
            int firstPanelIndex = canvas.childCount;

            foreach (Transform child in canvas)
            {
                bool isFullScreenPanel = child.GetComponent<StoryIntroUI>() != null || child.GetComponent<DailyReportUI>() != null;
                if (isFullScreenPanel && child != line) firstPanelIndex = Mathf.Min(firstPanelIndex, child.GetSiblingIndex());
            }

            line.SetSiblingIndex(Mathf.Min(firstPanelIndex, canvas.childCount - 1));
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
    }
}
