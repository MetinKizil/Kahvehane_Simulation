using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Illegal;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D6c - Ana salonun altina Kumar Odasi'ni (bodrum, ana salonla ayni olcu) ve sol arka koseye merdiveni kurar:
    ///  1) Ana zeminin mesh'i merdiven boslugu olan bir mesh'le degisir (dis olculer AYNI: diger builder'lar bounds'a bakiyor)
    ///  2) 'Bodrum' koku: MerdivenAlani (ReservedArea, esya konamaz), MerdivenKapagiYeri (alinana kadar boslugu kapatan zemin),
    ///     KumarOdasi (KAPALI): zemin, duvar, tavan, 25 cm basamaklar, korkuluk, gizli kapak (HiddenHatch), sari isiklar
    ///  3) Sahnedeki kumar masasi bodrumun ortasina tasinir, eski ortusu silinir
    ///  4) Gizli Kumarhane acicisi artik KumarOdasi'ni acar ve MerdivenKapagiYeri'ni gizler
    /// Olculer ana zeminin Renderer.bounds'undan; elle koordinat yok. Tekrar calistirilabilir ('Bodrum' yeniden kurulur).
    /// Sonra: sahneyi kaydet, 'NavMesh'i Yeniden Bake Et' (zemin mesh'i degisti), kaydet.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Bodrumu Kur
    /// </summary>
    public static class BasementBuilder
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string MeshFolder = "Assets/_Project/Meshes";
        private const string HoledMeshPath = MeshFolder + "/ZeminMerdivenBoslugu.asset";
        private const string UpgradePath = DataFolder + "/GizliKumarhaneUpgrade.asset";
        private const string RootName = "Bodrum";

        private const float HoleWidth = 1.5f;    // x (sol duvar boyunca)
        private const float HoleLength = 5f;     // z (arka koseden one dogru)
        private const float Depth = 3f;          // bodrum zemini ana zeminin bu kadar altinda
        private const float StepRise = 0.25f;
        private const float StepRun = 0.35f;     // 11 basamak 3.85 m; dipte ~1.15 m duzluk kalir (NPC yaricapi 0.5)
        private const float SlabGap = 0.05f;     // tavanin ustu ile ana zemin arasi (cakisip titresmesin)
        private const float SlabThickness = 0.2f;
        private const float WallThickness = 0.2f;
        private const float RailHeight = 1f;
        private const float RailThickness = 0.05f;
        private const float HatchThickness = 0.05f;

        private static readonly Color BasementFloorColor = new Color(0.3f, 0.27f, 0.24f);
        private static readonly Color BasementWallColor = new Color(0.42f, 0.38f, 0.33f);
        private static readonly Color WoodColor = new Color(0.45f, 0.3f, 0.18f);
        private static readonly Color LampColor = new Color(1f, 0.75f, 0.45f);

        [MenuItem("Kan Bagi/Bodrumu Kur")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[BasementBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            GamblingTable table = Object.FindFirstObjectByType<GamblingTable>(FindObjectsInactive.Include);
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(UpgradePath);

            if (surface == null || !surface.TryGetComponent(out Renderer floorRenderer) || !surface.TryGetComponent(out MeshFilter floorFilter) ||
                upgradeManager == null || table == null || upgrade == null)
            {
                Debug.LogError("[BasementBuilder] Ana zemin (NavMeshSurface + Renderer + MeshFilter), UpgradeManager, kumar masasi " +
                               $"veya {UpgradePath} bulunamadi.");
                return;
            }

            UpgradeUnlocker unlocker = TableGamesBuilder.FindByUpgrade<UpgradeUnlocker>(upgradeManager.gameObject, upgrade);
            if (unlocker == null)
            {
                Debug.LogError("[BasementBuilder] 'Managers'ta Gizli Kumarhane'ye bagli UpgradeUnlocker yok.");
                return;
            }

            // Eski kurulum silinmeden once masa disari alinir (Bodrum ile birlikte yok olmasin).
            Undo.SetTransformParent(table.transform, null, "Kumar Masasi");
            GameObject old = GameObject.Find("/" + RootName);
            if (old == null) old = FindInactiveRoot(RootName);
            if (old != null) Undo.DestroyObjectImmediate(old);

            Bounds main = floorRenderer.bounds;
            float y = main.min.y;
            var hole = new Rect(main.min.x, main.max.z - HoleLength, HoleWidth, HoleLength); // x, z, genislik, uzunluk
            var holeCenter = new Vector3(hole.center.x, y, hole.center.y);

            ApplyHoledFloor(surface.transform, floorFilter, main, hole);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, RootName);

            var reserved = new GameObject("MerdivenAlani");
            reserved.transform.SetParent(root.transform, false);
            reserved.transform.position = holeCenter;
            var reservedArea = reserved.AddComponent<ReservedArea>();
            var reservedData = new SerializedObject(reservedArea);
            reservedData.FindProperty("size").vector3Value = new Vector3(HoleWidth, 4f, HoleLength);
            reservedData.ApplyModifiedPropertiesWithoutUndo();

            GameObject patch = CreatePlane(root.transform, "MerdivenKapagiYeri", holeCenter, HoleWidth, HoleLength,
                                           floorRenderer.sharedMaterial);

            GameObject room = BuildRoom(root.transform, main, hole, y);
            MoveTable(table, room.transform, main, y);

            var unlockerData = new SerializedObject(unlocker);
            SerializedProperty targets = unlockerData.FindProperty("targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = room;
            SerializedProperty hide = unlockerData.FindProperty("hideTargets");
            hide.arraySize = 1;
            hide.GetArrayElementAtIndex(0).objectReferenceValue = patch;
            unlockerData.ApplyModifiedProperties();

            room.SetActive(false);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[BasementBuilder] Bodrum kuruldu: {main.size.x:0.#} x {main.size.z:0.#} m, {Depth} m asagida; merdiven boslugu " +
                      $"x {hole.xMin:0.##}..{hole.xMax:0.##}, z {hole.yMin:0.##}..{hole.yMax:0.##}. Kumar masasi bodruma tasindi, " +
                      "acici baglandi. Sahneyi kaydet (Ctrl+S), sonra 'NavMesh'i Yeniden Bake Et' ve tekrar kaydet.");
        }

        // ---- Ana zemin ----

        /// <summary>Zemin mesh'i: dis sinirlar ayni, merdiven boslugu delik. En fazla 4 dikdortgen.</summary>
        private static void ApplyHoledFloor(Transform floor, MeshFilter filter, Bounds main, Rect hole)
        {
            var rects = new List<Rect>
            {
                Rect.MinMaxRect(main.min.x, main.min.z, hole.xMin, main.max.z),     // deligin solu
                Rect.MinMaxRect(hole.xMax, main.min.z, main.max.x, main.max.z),     // deligin sagi
                Rect.MinMaxRect(hole.xMin, main.min.z, hole.xMax, hole.yMin),       // deligin onu
                Rect.MinMaxRect(hole.xMin, hole.yMax, hole.xMax, main.max.z),       // deligin arkasi
            };

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            foreach (Rect rect in rects)
            {
                if (rect.width < 0.001f || rect.height < 0.001f) continue;

                int start = vertices.Count;
                Vector2[] corners =
                {
                    new Vector2(rect.xMin, rect.yMin), new Vector2(rect.xMin, rect.yMax),
                    new Vector2(rect.xMax, rect.yMax), new Vector2(rect.xMax, rect.yMin)
                };

                foreach (Vector2 corner in corners)
                {
                    vertices.Add(floor.InverseTransformPoint(new Vector3(corner.x, main.min.y, corner.y)));
                    uvs.Add(new Vector2((corner.x - main.min.x) / main.size.x, (corner.y - main.min.z) / main.size.z));
                }

                // Ustten bakinca saat yonu (Unity on yuzu).
                triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder("Assets/_Project", "Meshes");

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(HoledMeshPath);
            if (mesh == null)
            {
                mesh = new Mesh();
                AssetDatabase.CreateAsset(mesh, HoledMeshPath);
            }

            mesh.Clear();
            mesh.name = "ZeminMerdivenBoslugu";
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            Undo.RecordObject(filter, "Delikli Zemin");
            filter.sharedMesh = mesh;

            if (floor.TryGetComponent(out MeshCollider meshCollider))
            {
                Undo.RecordObject(meshCollider, "Delikli Zemin");
                meshCollider.sharedMesh = mesh;
            }
        }

        // ---- Kumar Odasi ----

        private static GameObject BuildRoom(Transform root, Bounds main, Rect hole, float y)
        {
            var room = new GameObject("KumarOdasi");
            room.transform.SetParent(root, false);

            float floorY = y - Depth;
            float ceilingBottom = y - SlabGap - SlabThickness;
            float t = WallThickness;
            float h = t * 0.5f;

            CreatePlane(room.transform, "Zemin", new Vector3(main.center.x, floorY, main.center.z), main.size.x, main.size.z,
                        TableGamesBuilder.GetOrCreateMaterial("BodrumZeminMat", BasementFloorColor));

            // Duvarlar (zeminin disina oturur, koseler kapali)
            // Duvar ve tavan "Not Walkable" ISARETLENMEZ: ustleri ana zeminin/sokagin hemen altinda; NavMesh bake'te
            // ustteki yuruyus yuzeyiyle birlesip onu da yurunemez yapiyordu (D6c hatasi: musteriler kapida kaldi).
            // Dikey duvarlar zaten engel; tavanin ustunde ise ana zemin oldugu icin yurunecek yer kalmaz.
            Transform walls = CreateGroup(room.transform, "Duvarlar", notWalkable: false);
            Material wallMaterial = TableGamesBuilder.GetOrCreateMaterial("BodrumDuvarMat", BasementWallColor);
            float wallHeight = ceilingBottom - floorY;
            float spanZ = main.size.z + 2f * t;
            CreateBox(walls, "DuvarSol", new Vector3(main.min.x - h, floorY, main.center.z), new Vector3(t, wallHeight, spanZ), wallMaterial);
            CreateBox(walls, "DuvarSag", new Vector3(main.max.x + h, floorY, main.center.z), new Vector3(t, wallHeight, spanZ), wallMaterial);
            CreateBox(walls, "DuvarArka", new Vector3(main.center.x, floorY, main.max.z + h), new Vector3(main.size.x, wallHeight, t), wallMaterial);
            CreateBox(walls, "DuvarOn", new Vector3(main.center.x, floorY, main.min.z - h), new Vector3(main.size.x, wallHeight, t), wallMaterial);

            // Tavan: ana zeminin hemen alti (ana zemin tek yuzlu, alttan gorunmez). Merdiven boslugu acik.
            Transform ceiling = CreateGroup(room.transform, "Tavan", notWalkable: false);
            float slabY = ceilingBottom;
            Rect a = Rect.MinMaxRect(hole.xMax, main.min.z, main.max.x, main.max.z);
            Rect b = Rect.MinMaxRect(main.min.x, main.min.z, hole.xMax, hole.yMin);
            CreateBox(ceiling, "TavanSag", new Vector3(a.center.x, slabY, a.center.y), new Vector3(a.width, SlabThickness, a.height), wallMaterial);
            CreateBox(ceiling, "TavanOn", new Vector3(b.center.x, slabY, b.center.y), new Vector3(b.width, SlabThickness, b.height), wallMaterial);

            // Merdiven: salon tarafindan (deligin onu) arkaya dogru iner; her basamak bodrum zeminine kadar dolu kutu.
            Transform stairs = CreateGroup(room.transform, "Merdiven", notWalkable: false);
            Material wood = TableGamesBuilder.GetOrCreateMaterial("BodrumAhsapMat", WoodColor);
            int stepCount = Mathf.RoundToInt(Depth / StepRise);
            float run = StepRun;
            for (int i = 0; i < stepCount - 1; i++)
            {
                float top = y - StepRise * (i + 1);
                float zMin = hole.yMin + run * i;
                CreateBox(stairs, $"Basamak{i + 1}", new Vector3(hole.center.x, floorY, zMin + run * 0.5f),
                          new Vector3(hole.width, top - floorY, run), wood);
            }

            // Korkuluk (sadece kapak acikken): sag kenar, sol kenar ve arka uc; salon tarafi giris.
            Transform rails = CreateGroup(room.transform, "Korkuluk", notWalkable: true);
            float r = RailThickness * 0.5f;
            CreateBox(rails, "KorkulukSag", new Vector3(hole.xMax + r, y, hole.center.y), new Vector3(RailThickness, RailHeight, hole.height), wood);
            CreateBox(rails, "KorkulukSol", new Vector3(hole.xMin + r, y, hole.center.y), new Vector3(RailThickness, RailHeight, hole.height), wood);
            CreateBox(rails, "KorkulukArka", new Vector3(hole.center.x, y, hole.yMax - r), new Vector3(hole.width, RailHeight, RailThickness), wood);

            // Gizli kapak: mentese sol duvar tarafinda; kapali yatar, acikken duvara dogru dikilir.
            var hinge = new GameObject("Mentese");
            hinge.transform.SetParent(room.transform, false);
            hinge.transform.position = new Vector3(hole.xMin, y, hole.center.y);

            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lid.name = "Kapak";
            lid.transform.SetParent(hinge.transform, false);
            lid.transform.localPosition = new Vector3(hole.width * 0.5f, -HatchThickness * 0.5f, 0f);
            lid.transform.localScale = new Vector3(hole.width, HatchThickness, hole.height);
            lid.GetComponent<Renderer>().sharedMaterial = wood;

            var hatch = lid.AddComponent<HiddenHatch>();
            var hatchData = new SerializedObject(hatch);
            hatchData.FindProperty("hinge").objectReferenceValue = hinge.transform;
            SerializedProperty openOnly = hatchData.FindProperty("openOnlyObjects");
            openOnly.arraySize = 1;
            openOnly.GetArrayElementAtIndex(0).objectReferenceValue = rails.gameObject;
            hatchData.FindProperty("holeCenter").vector3Value = new Vector3(hole.center.x, y - Depth * 0.5f + 0.1f, hole.center.y);
            hatchData.FindProperty("holeSize").vector3Value = new Vector3(hole.width, Depth + 0.2f, hole.height);
            hatchData.ApplyModifiedPropertiesWithoutUndo();

            // Los sari isiklar
            Transform lights = CreateGroup(room.transform, "Isiklar", notWalkable: false);
            float lampY = floorY + (ceilingBottom - floorY) * 0.85f;
            CreateLamp(lights, "Lamba1", new Vector3(main.center.x - main.size.x * 0.2f, lampY, main.center.z));
            CreateLamp(lights, "Lamba2", new Vector3(main.center.x + main.size.x * 0.25f, lampY, main.center.z));

            return room;
        }

        /// <summary>Kumar masasi bodrumun ortasina (merdivenden uzak) iner, ayni yukseklik farkiyla. Eski ortu silinir.</summary>
        private static void MoveTable(GamblingTable table, Transform room, Bounds main, float y)
        {
            Transform cover = table.transform.Find("Ortu");
            if (cover != null) Undo.DestroyObjectImmediate(cover.gameObject);

            Undo.SetTransformParent(table.transform, room, "Kumar Masasi");
            Undo.RecordObject(table.transform, "Kumar Masasi");
            float heightAboveFloor = table.transform.position.y - y;
            table.transform.position = new Vector3(main.center.x + 1f, y - Depth + heightAboveFloor, main.center.z);
            table.gameObject.SetActive(true);
        }

        // ---- Yardimcilar ----

        private static GameObject FindInactiveRoot(string rootName)
        {
            foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (go.name == rootName) return go;
            }

            return null;
        }

        private static Transform CreateGroup(Transform parent, string groupName, bool notWalkable)
        {
            var group = new GameObject(groupName);
            group.transform.SetParent(parent, false);

            if (notWalkable)
            {
                var modifier = group.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
                modifier.applyToChildren = true;
            }

            return group.transform;
        }

        private static GameObject CreatePlane(Transform parent, string planeName, Vector3 center, float sizeX, float sizeZ, Material material)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = planeName;
            plane.transform.SetParent(parent, false);
            plane.transform.position = center;
            plane.transform.localScale = new Vector3(sizeX / 10f, 1f, sizeZ / 10f);
            if (material != null) plane.GetComponent<Renderer>().sharedMaterial = material;
            return plane;
        }

        /// <summary>Taban noktasi verilen kutu (pivot ortada oldugu icin yukari kaydirilir).</summary>
        private static void CreateBox(Transform parent, string boxName, Vector3 basePosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = boxName;
            box.transform.SetParent(parent, true);
            box.transform.position = basePosition + Vector3.up * (size.y * 0.5f);
            box.transform.localScale = size;
            if (material != null) box.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLamp(Transform parent, string lampName, Vector3 position)
        {
            var lamp = new GameObject(lampName);
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = position;

            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LampColor;
            light.intensity = 2f;
            light.range = 7f;
            light.shadows = LightShadows.None;
        }
    }
}
