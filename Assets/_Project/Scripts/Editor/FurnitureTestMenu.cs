using KanBagi.Furniture;
using KanBagi.Shop;
using UnityEditor;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// GECICI test araci (D2): yerlestirme modu (D3) gelene kadar masalari oyun sirasinda tasimanin yolu.
    /// Her masayi biraz kaydirir ve dondurur - FurnitureManager.Move uzerinden, yani gercek akisla
    /// (NavMesh yeniden kurulur, kayda yazilir). D3 gelince silinecek.
    ///
    /// SADECE EDITOR + PLAY. Menu: Kan Bagi > Test > Masalari Kaydir
    /// </summary>
    public static class FurnitureTestMenu
    {
        private static readonly Vector3 Offset = new Vector3(0.5f, 0f, 0.3f);
        private const float TurnDegrees = 30f;

        [MenuItem("Kan Bagi/Test/Masalari Kaydir")]
        public static void ShiftTables()
        {
            FurnitureManager manager = FurnitureManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[FurnitureTestMenu] Sadece Play modunda ve FurnitureManager varken calisir.");
                return;
            }

            int moved = 0;

            // Kopya uzerinde: Move listeyi degistirmez ama guvenli olsun.
            foreach (PlacedFurniture furniture in new System.Collections.Generic.List<PlacedFurniture>(manager.Placed))
            {
                if (furniture == null || !furniture.TryGetComponent(out Table table)) continue;

                // Oturan musteri varken masa kaymasin: musteri havada kalir.
                if (!table.IsFree) continue;

                manager.Move(furniture, furniture.transform.position + Offset, furniture.Yaw + TurnDegrees);
                moved++;
            }

            Debug.Log($"[FurnitureTestMenu] {moved} bos masa kaydirildi (+{Offset}, +{TurnDegrees} derece). Dolu masalar atlandi.");
        }

        [MenuItem("Kan Bagi/Test/Masalari Kaydir", true)]
        private static bool ValidateShiftTables() => EditorApplication.isPlaying;
    }
}
