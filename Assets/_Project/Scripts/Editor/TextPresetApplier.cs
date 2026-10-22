using System.Text;
using KanBagi.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// ArayuzMetinleri asset'indeki metinleri prefab'lara ve acik sahneye tek tikla yazar.
    /// Once prefab'lar guncellenir (sahnedeki kopyalar bunu miras alir), sonra sahne.
    /// Ne degistigi Console'a dokulur; tekrar calistirilinca "0 alan degisti" der.
    ///
    /// Bir builder prefab'i yeniden uretirse (orn. tabela, satir) metinler koddaki ASCII
    /// varsayilanlara doner: o zaman bu komutu tekrar calistir.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Metinleri Uygula
    /// </summary>
    public static class TextPresetApplier
    {
        private const string PresetPath = "Assets/_Project/ScriptableObjects/ArayuzMetinleri.asset";
        private const string PrefabFolder = "Assets/_Project/Prefabs";

        [MenuItem("Kan Bagi/Metinleri Uygula")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[TextPresetApplier] Play modundayken uygulanamaz (degisiklikler kaybolur). Once Play'i durdur.");
                return;
            }

            var preset = AssetDatabase.LoadAssetAtPath<TextPreset>(PresetPath);
            if (preset == null)
            {
                Debug.LogError($"[TextPresetApplier] Metin dosyasi bulunamadi: {PresetPath}");
                return;
            }

            var report = new StringBuilder("[TextPresetApplier] Metinler uygulandi:\n");
            int changed = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
                changed += ApplyToPrefab(AssetDatabase.GUIDToAssetPath(guid), preset, report);

            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                changed += ApplyToComponent(behaviour, preset, report, withUndo: true);

            if (changed > 0) EditorSceneManager.MarkAllScenesDirty();

            report.AppendLine($"Toplam {changed} alan degisti." + (changed > 0 ? " Sahneyi kaydet (Ctrl+S)." : ""));
            Debug.Log(report.ToString());
        }

        private static int ApplyToPrefab(string path, TextPreset preset, StringBuilder report)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int changed = 0;

            try
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                    changed += ApplyToComponent(behaviour, preset, report, withUndo: false);

                if (changed > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            return changed;
        }

        /// <summary>Bilesenin tipine uyan her kaydi yazar. Degisen alan sayisini doner.</summary>
        private static int ApplyToComponent(MonoBehaviour behaviour, TextPreset preset, StringBuilder report, bool withUndo)
        {
            if (behaviour == null) return 0;

            string typeName = behaviour.GetType().Name;
            SerializedObject serialized = null;
            int changed = 0;

            foreach (FieldText entry in preset.Fields)
            {
                if (entry.Component != typeName) continue;
                if (!MatchesPath(behaviour.transform, entry.ObjectPath)) continue;

                serialized ??= new SerializedObject(behaviour);
                SerializedProperty property = serialized.FindProperty(entry.Field);

                if (property == null || property.propertyType != SerializedPropertyType.String)
                {
                    report.AppendLine($"  ! {typeName}.{entry.Field} bulunamadi veya metin degil");
                    continue;
                }

                if (property.stringValue == entry.Text) continue;

                report.AppendLine($"  {GetOwnerName(behaviour)} > {typeName}.{entry.Field}: \"{property.stringValue}\" -> \"{entry.Text}\"");
                property.stringValue = entry.Text;
                changed++;
            }

            if (changed == 0) return 0;

            if (withUndo) serialized.ApplyModifiedProperties();
            else serialized.ApplyModifiedPropertiesWithoutUndo();

            return changed;
        }

        /// <summary>Yol bossa her obje; doluysa hiyerarsi yolu ("Ata/Cocuk") bununla biten obje.</summary>
        private static bool MatchesPath(Transform target, string objectPath)
        {
            if (string.IsNullOrEmpty(objectPath)) return true;

            string path = target.name;
            for (Transform parent = target.parent; parent != null; parent = parent.parent)
                path = $"{parent.name}/{path}";

            return path == objectPath || path.EndsWith("/" + objectPath);
        }

        private static string GetOwnerName(Component component)
        {
            Transform root = component.transform.root;
            return root == component.transform ? component.name : $"{root.name}/{component.name}";
        }
    }
}
