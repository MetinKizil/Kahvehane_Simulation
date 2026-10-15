using System.Linq;
using System.Text;
using KanBagi.Customers;
using KanBagi.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Projedeki TUM CustomerProfile asset'lerini sahnedeki CustomerSpawner'in listesine baglar
    /// (itibar sirasina gore). Yeni profil eklenince tekrar calistirmak yeterli.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Musteri Profillerini Bagla
    /// </summary>
    public static class CustomerProfileLinker
    {
        [MenuItem("Kan Bagi/Musteri Profillerini Bagla")]
        public static void LinkProfiles()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[CustomerProfileLinker] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            CustomerProfile[] profiles = AssetDatabase.FindAssets("t:CustomerProfile")
                .Select(guid => AssetDatabase.LoadAssetAtPath<CustomerProfile>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(profile => profile != null)
                .OrderBy(profile => profile.RequiredReputation)
                .ThenBy(profile => profile.name)
                .ToArray();

            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            if (spawner == null)
            {
                Debug.LogError("[CustomerProfileLinker] Sahnede CustomerSpawner bulunamadi.");
                return;
            }

            var serialized = new SerializedObject(spawner);
            SerializedProperty list = serialized.FindProperty("profiles");
            list.arraySize = profiles.Length;

            var report = new StringBuilder($"[CustomerProfileLinker] '{spawner.name}' > {profiles.Length} profil baglandi:\n");
            for (int i = 0; i < profiles.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = profiles[i];
                report.AppendLine($"  {profiles[i].name}  (itibar {profiles[i].RequiredReputation}, agirlik {profiles[i].SpawnWeight})");
            }

            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);

            report.AppendLine("Sahneyi kaydet (Ctrl+S).");
            Debug.Log(report.ToString());
        }
    }
}
