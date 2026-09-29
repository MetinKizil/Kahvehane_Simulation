using System.IO;
using UnityEditor;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Kayit dosyasi icin editor kisayollari. Test sirasinda sifirdan baslamak icin.
    /// SADECE EDITOR. Menu: Kan Bagi > Kaydi Sil / Kayit Klasorunu Ac
    /// </summary>
    public static class SaveMenuItems
    {
        // SaveManager'daki varsayilan dosya adiyla ayni olmali.
        private const string SaveFileName = "kanbagi_save.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        [MenuItem("Kan Bagi/Kaydi Sil")]
        public static void DeleteSave()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log($"[SaveMenuItems] Silinecek kayit yok: {SavePath}");
                return;
            }

            File.Delete(SavePath);
            Debug.Log($"[SaveMenuItems] Kayit silindi: {SavePath}");
        }

        [MenuItem("Kan Bagi/Kayit Klasorunu Ac")]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
