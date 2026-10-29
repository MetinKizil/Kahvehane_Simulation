using KanBagi.Customers;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D6a - Ana salonun etrafina duvar ve on cepheye kapi boslugu kurar (dukkanin ici/disi ilk kez belli olur).
    /// Olculer dukkan zemininin (NavMeshSurface'li obje) Renderer.bounds'undan hesaplanir; elle koordinat yok.
    /// Duvarlar zeminin DISINA oturur, ic alan aynen kalir. Kapi, musterilerin dogdugu noktanin (CustomerSpawner.spawnPoint)
    /// hizasindadir. Her taraf ayri obje (DuvarOn/Sol/Sag/Arka): D6b'de genisletilen taraf kaldirilabilsin.
    /// Duvarlarin collider'i oyuncuyu durdurur ve yerlestirmeyi engeller; NavMeshModifier yurunemez yapar.
    /// Tekrar calistirilabilir: eski 'Duvarlar' silinip yeniden kurulur.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Dukkan Duvarlarini Kur
    /// </summary>
    public static class ShopWallBuilder
    {
        private const string RootName = "Duvarlar";
        private const float WallHeight = 3f;
        private const float WallThickness = 0.2f;
        private const float DoorWidth = 2f;
        private const float DoorHeight = 2.4f;
        private static readonly Color WallColor = new Color(0.85f, 0.8f, 0.7f);

        [MenuItem("Kan Bagi/Dukkan Duvarlarini Kur")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ShopWallBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            if (surface == null || !surface.TryGetComponent(out Renderer floorRenderer))
            {
                Debug.LogError("[ShopWallBuilder] Dukkan zemini bulunamadi (NavMeshSurface + Renderer olan 'Zemin').");
                return;
            }

            Bounds floor = floorRenderer.bounds;
            float doorX = FindDoorX(floor);

            GameObject old = GameObject.Find("/" + RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Dukkan Duvarlari");
            root.transform.position = new Vector3(floor.center.x, floor.min.y, floor.center.z);

            // Duvarlar yurunemez: ustleri NavMesh'te ada olmasin (zemin "All" topluyor).
            var modifier = root.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            modifier.applyToChildren = true;

            Material material = TableGamesBuilder.GetOrCreateMaterial("DuvarMat", WallColor);
            float y = floor.min.y;
            float half = WallThickness * 0.5f;
            float outerMinX = floor.min.x - WallThickness;
            float outerMaxX = floor.max.x + WallThickness;

            // Yan ve arka duvarlar koseleri kapatacak kadar uzun (on duvar da kalinlik kadar tasar).
            Transform sides = root.transform;
            CreateWall(sides, "DuvarSol", new Vector3(floor.min.x - half, y, floor.center.z),
                       new Vector3(WallThickness, WallHeight, floor.size.z + 2f * WallThickness), material);
            CreateWall(sides, "DuvarSag", new Vector3(floor.max.x + half, y, floor.center.z),
                       new Vector3(WallThickness, WallHeight, floor.size.z + 2f * WallThickness), material);
            CreateWall(sides, "DuvarArka", new Vector3(floor.center.x, y, floor.max.z + half),
                       new Vector3(floor.size.x, WallHeight, WallThickness), material);

            // On duvar: kapinin solu, sagi ve ustu (lento).
            var front = new GameObject("DuvarOn");
            Undo.RegisterCreatedObjectUndo(front, "On Duvar");
            front.transform.SetParent(root.transform, true);

            float doorMin = doorX - DoorWidth * 0.5f;
            float doorMax = doorX + DoorWidth * 0.5f;
            float frontZ = floor.min.z - half;

            CreateWall(front.transform, "KapiSolu", new Vector3((floor.min.x + doorMin) * 0.5f, y, frontZ),
                       new Vector3(doorMin - floor.min.x, WallHeight, WallThickness), material);
            CreateWall(front.transform, "KapiSagi", new Vector3((doorMax + floor.max.x) * 0.5f, y, frontZ),
                       new Vector3(floor.max.x - doorMax, WallHeight, WallThickness), material);
            CreateWall(front.transform, "KapiUstu", new Vector3(doorX, y + DoorHeight, frontZ),
                       new Vector3(DoorWidth, WallHeight - DoorHeight, WallThickness), material);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[ShopWallBuilder] Duvarlar kuruldu: ic alan {floor.size.x:0.#} x {floor.size.z:0.#} m, " +
                      $"kapi x = {doorX:0.##} ({DoorWidth} m), dis olcu {outerMaxX - outerMinX:0.#} m. Sahneyi kaydet (Ctrl+S).");
        }

        /// <summary>Kapi musterilerin dogdugu noktanin hizasinda; bulunamazsa on cephenin ortasi.</summary>
        private static float FindDoorX(Bounds floor)
        {
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>(FindObjectsInactive.Include);
            if (spawner != null)
            {
                var spawnPoint = new SerializedObject(spawner).FindProperty("spawnPoint").objectReferenceValue as Transform;
                if (spawnPoint != null)
                {
                    float min = floor.min.x + DoorWidth * 0.5f + 0.5f;
                    float max = floor.max.x - DoorWidth * 0.5f - 0.5f;
                    return Mathf.Clamp(spawnPoint.position.x, min, max);
                }
            }

            Debug.LogWarning("[ShopWallBuilder] CustomerSpawner > Spawn Point bulunamadi; kapi on cephenin ortasina kondu.");
            return floor.center.x;
        }

        /// <summary>Taban noktasi verilen kutu duvar (pivot ortada oldugu icin yukari kaydirilir).</summary>
        private static void CreateWall(Transform parent, string wallName, Vector3 basePosition, Vector3 size, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(wall, wallName);
            wall.name = wallName;
            wall.transform.SetParent(parent, true);
            wall.transform.position = basePosition + Vector3.up * (size.y * 0.5f);
            wall.transform.localScale = size;

            if (material != null) wall.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
