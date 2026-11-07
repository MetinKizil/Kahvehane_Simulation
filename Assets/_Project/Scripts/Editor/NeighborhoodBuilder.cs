using System.Collections.Generic;
using System.Globalization;
using KanBagi.Customers;
using KanBagi.Data;
using KanBagi.Neighborhood;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// 10d-1a - Mahalleyi kullanicinin cizdigi haritadan kurar (MahalleHaritasi.asset; yoksa DefaultTable'dan olusur).
    /// Cizim dukkanin etrafinda 90 derece dondurulur: cizimin DOGUSU (dukkan kapisi) oyunda -z, GUNEYI (bahce) -x.
    /// Hizalama dukkan zemininden (NavMeshSurface'li 'Zemin') hesaplanir: 'Dukkan' blogunun dogu kenari = on duvar,
    /// guney kenari = ana salonun bahce tarafi. Elle koordinat yok.
    ///
    /// Kurulanlar 'Mahalle/Harita' altinda (tekrar calistirilinca silinip yeniden kurulur): asfalt zemin, kaldirimlar,
    /// binalar (Not Walkable), park, avlular, Oyun Disi arazi + gorunmez duvarlar, harita siniri, musteri giris noktalari.
    /// Eski 'Mahalle/Sokak' silinir. Mevcut PropertyBuilding'ler (Bakkal, Terzi, Kirathane->Eczane) haritadaki
    /// yerlerine tasinir, tabelalari yeniden asilir. CustomerSpawner'in sokak girisleri baglanir.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Mahalleyi Kur
    /// </summary>
    public static class NeighborhoodBuilder
    {
        private const string MapPath = "Assets/_Project/ScriptableObjects/MahalleHaritasi.asset";
        private const string RootName = "Mahalle";
        private const string MapRootName = "Harita";
        private const string OldStreetName = "Sokak";

        private const float GroundTop = -0.03f;
        private const float GroundThickness = 0.2f;
        private const float SidewalkTop = 0.05f;
        private const float SidewalkWidth = 1.5f;
        private const float ParkTop = 0.06f;
        private const float BoundaryHeight = 6f;

        private static readonly Color AsphaltColor = new Color(0.33f, 0.33f, 0.35f);
        private static readonly Color SidewalkColor = new Color(0.7f, 0.68f, 0.64f);

        // ---- VARSAYILAN HARITA (kullanicinin cizimi, 10d-1a) ----
        // Ad;Tur;sutun;satir;genislik;yukseklik;on;renk;baglanti   (kare cinsinden, cizimin sol ustunden; 1 kare = 5 m)
        // on: binanin sokaga bakan yuzu (K kuzey/ust, G guney/alt, D dogu/sag, B bati/sol - CIZIMDEKI yon)
        // baglanti: Mulk icin PropertyData asset adi, Giris icin tercih eden CustomerProfile asset adi
        // Cizimdeki yerler aynen (Kuyumcu kapinin karsisinda); tekrarlanan 'Dukkan'lar dolgu.
        internal const string DefaultTable = @"
EvBati1;Ev;0;2;2;8;D;a2daf9;
EvBati2;Ev;0;10;2;11;D;a2daf9;
EvBati3;Ev;0;21;2;19;D;a2daf9;
EvBati4;Ev;0;40;2;11;D;a2daf9;
KoseAvlu;Avlu;0;0;2;2;G;565656;
EvKuzey1;Ev;2;0;9;2;G;a2daf9;
EvKuzey2;Ev;11;0;9;2;G;a2daf9;
EvKuzey3;Ev;20;0;8;2;G;a2daf9;
OyunDisiKuzey;Disari;28;0;11;2;G;3c3c3c;
OyunDisiDogu1;Disari;36;2;3;10;B;3c3c3c;
OyunDisiDogu2;Disari;36;25;3;26;B;3c3c3c;
EvDogu1;Ev;36;12;3;5;B;a2daf9;
EvDogu2;Ev;36;21;3;4;B;a2daf9;
EvHavuzlu;Ev;28;2;4;15;B;a2daf9;
Pavyon;Mulk;33;2;3;5;B;cebfd9;
AvluPavyon;Avlu;33;7;3;5;B;565656;
EvKoseDogu;Ev;32;12;4;5;G;a2daf9;
Hastane;Sus;4;4;6;13;G;b55c6a;
EvlerKuzey;Ev;12;4;8;5;G;a2daf9;
Park;Park;20;4;6;5;G;91b589;
Veteriner;Sus;12;11;4;4;B;f4c0c6;
Petshop;Sus;12;15;4;2;G;f9e9a3;
Halici;Sus;20;11;3;3;B;d291a4;
Ayakkabici;Sus;20;14;3;3;B;e1d98e;
GymMagazasi;Sus;23;11;3;6;D;fbb682;
Eczane2;Sus;4;21;4;2;K;e58b8e;
Cicekci;Mulk;8;21;3;2;K;91b589;
Dukkan1;Sus;11;21;2;2;K;d2a59a;
Pastane;Sus;13;21;3;5;D;f9e9a3;
Emlakci;Mulk;13;26;3;4;D;fbb682;
Kiraathane;Dukkan;12;30;4;4;D;d2a59a;
Bahce;Bahce;12;34;4;2;G;a9c19d;
Mobilyaci;Mulk;6;32;6;4;G;dfadbe;
EvlerBati;Ev;4;23;2;13;B;a2daf9;
AvluBati1;Avlu;6;23;7;7;K;565656;
AvluBati2;Avlu;6;30;6;2;K;565656;
Kasap;Mulk;20;21;2;4;B;d291a4;
Dukkan2;Sus;20;25;2;2;B;d2a59a;
Eczane;Mulk;22;21;4;2;K;e58b8e;KirathaneMulk
Manav2;Sus;24;23;2;4;D;a9d18e;
Dukkan3;Sus;22;25;2;2;G;d2a59a;
AvluOrta1;Avlu;22;23;2;2;K;565656;
Donerci;Sus;20;28;3;2;K;d2a59a;
EvErEr;Ev;23;28;3;2;K;a2daf9;
InternetCafe;Mulk;26;28;2;6;D;cebfd9;
Kuyumcu;Mulk;20;30;4;6;B;f9e9a3;
Berber;Mulk;24;34;4;2;G;e58b8e;
AvluOrta2;Avlu;24;30;2;4;K;565656;
EvlerDogu;Ev;27;21;9;4;K;fbb682;
TekelBayi;Sus;27;25;2;2;B;fbb682;
AvluDogu1;Avlu;29;25;7;2;K;565656;
Iddia;Mulk;29;27;3;2;B;91b589;
BeyazEsya;Sus;29;29;2;3;B;d0d4a8;
Antikaci;Sus;29;32;4;4;B;c8cca8;
Dukkan4;Sus;33;34;3;2;G;cebfd9;
AvluDogu2;Avlu;31;29;5;3;K;565656;
AvluDogu3;Avlu;33;32;3;2;K;565656;
EvGuney;Ev;4;40;4;11;K;a2daf9;
Okul;Sus;8;40;8;8;K;a1b3e0;
AvluOkul;Avlu;8;48;6;3;K;565656;
Cigkofte;Sus;14;48;2;3;D;f4c0c6;
Bakkal;Mulk;20;40;4;4;B;f4c0e0;BakkalMulk
Dukkan5;Sus;20;44;4;2;B;d2a59a;
Terzi;Mulk;24;40;3;3;K;c0c8e8;TerziMulk
Dukkan6;Sus;24;43;3;3;D;d2a59a;
Firin;Mulk;20;47;4;4;B;e0c0b0;
Manav;Mulk;24;47;3;4;D;a9d18e;
AmcaninYeri;Amca;29;40;7;11;B;fd7501;
GirisKuzey;Giris;18;26;0;0;K;ffffff;
GirisGuney;Giris;18;39;0;0;K;ffffff;
GirisOkul;Giris;14;39.6;0;0;K;ffffff;Ogrenci
";

        [MenuItem("Kan Bagi/Mahalleyi Kur")]
        public static void Build()
        {
            NeighborhoodMap map = LoadOrCreateMap();
            if (map == null) return;

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            Renderer floorRenderer = surface != null ? surface.GetComponent<Renderer>() : null;
            if (floorRenderer == null)
            {
                Debug.LogError("[NeighborhoodBuilder] Dukkan zemini bulunamadi (NavMeshSurface + Renderer olan 'Zemin').");
                return;
            }

            MapBlock shop = map.Blocks.Find(b => b.type == MapBlockType.Dukkan);
            if (shop == null)
            {
                Debug.LogError("[NeighborhoodBuilder] Haritada 'Dukkan' turunde blok yok; hizalama yapilamaz.");
                return;
            }

            Bounds floor = floorRenderer.bounds;
            var grid = new MapGrid(map.CellSize, floor.min.z, shop.col + shop.width, floor.min.x, shop.row + shop.height);

            GameObject root = GameObject.Find(RootName) ?? new GameObject(RootName);
            DestroyChild(root.transform, OldStreetName);
            DestroyChild(root.transform, MapRootName);

            Transform mapRoot = NewGroup(MapRootName, root.transform);
            Transform groundRoot = NewGroup("Zemin", mapRoot);
            Transform sidewalkRoot = NewGroup("Kaldirimlar", mapRoot);
            Transform buildingRoot = NewGroup("Binalar", mapRoot);
            Transform boundaryRoot = NewGroup("Sinirlar", mapRoot);
            Transform entryRoot = NewGroup("GirisNoktalari", mapRoot);
            AddNotWalkable(buildingRoot.gameObject);

            Rect mapRect = grid.ToWorld(0, 0, map.GridSize.x, map.GridSize.y);
            var shopRects = new List<Rect>();
            foreach (MapBlock block in map.Blocks)
            {
                if (block.type == MapBlockType.Dukkan || block.type == MapBlockType.Bahce)
                    shopRects.Add(grid.ToWorld(block));
            }

            BuildGround(groundRoot, mapRect, floor);
            BuildBoundary(boundaryRoot, mapRect);

            Dictionary<string, PropertyBuilding> properties = FindPropertyBuildings();
            int buildingCount = 0;
            var entries = new List<(Transform point, string profile)>();
            CustomerSpawner spawner = Object.FindFirstObjectByType<CustomerSpawner>();
            Vector3 door = spawner != null ? spawner.EntryPoint.position : new Vector3(0f, 0f, floor.min.z - 1f);

            foreach (MapBlock block in map.Blocks)
            {
                switch (block.type)
                {
                    case MapBlockType.Giris:
                        entries.Add((BuildEntry(entryRoot, block, grid, door), block.link));
                        continue;
                    case MapBlockType.Disari:
                        BuildOutside(groundRoot, boundaryRoot, block, grid);
                        continue;
                }

                Rect rect = grid.ToWorld(block);
                BuildSidewalk(sidewalkRoot, rect, mapRect, shopRects);

                if (block.type == MapBlockType.Dukkan || block.type == MapBlockType.Bahce) continue;

                if (block.type == MapBlockType.Park)
                {
                    BuildSlab(buildingRoot, block.name, rect, ParkTop, MaterialFor(block.color), keepCollider: true);
                    continue;
                }

                BuildBuilding(buildingRoot, block, rect, properties);
                buildingCount++;
            }

            LinkSpawner(spawner, entries);

            // Tasinan binalarin tabelalari yeni on yuze asilir.
            if (properties.Count > 0) SignPrefabBuilder.PlaceSignsOnBuildings();

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[NeighborhoodBuilder] Mahalle kuruldu: {buildingCount} bina, {entries.Count} giris noktasi, " +
                      $"{properties.Count} mulk binasi tasindi. Simdi 'NavMesh'i Yeniden Bake Et' + kaydet (Ctrl+S).");
        }

        // ---------------- harita verisi ----------------

        private static NeighborhoodMap LoadOrCreateMap()
        {
            var map = AssetDatabase.LoadAssetAtPath<NeighborhoodMap>(MapPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<NeighborhoodMap>();
                AssetDatabase.CreateAsset(map, MapPath);
            }

            if (map.Blocks.Count == 0)
            {
                map.Blocks.AddRange(ParseTable(DefaultTable));
                EditorUtility.SetDirty(map);
                AssetDatabase.SaveAssets();
                Debug.Log($"[NeighborhoodBuilder] MahalleHaritasi varsayilan cizimle dolduruldu ({map.Blocks.Count} satir).");
            }

            return map;
        }

        internal static List<MapBlock> ParseTable(string table)
        {
            var blocks = new List<MapBlock>();
            foreach (string rawLine in table.Split('\n'))
            {
                string line = rawLine.Trim();
                if (line.Length == 0) continue;

                string[] f = line.Split(';');
                if (f.Length < 8)
                {
                    Debug.LogError($"[NeighborhoodBuilder] Harita satiri eksik: '{line}'");
                    continue;
                }

                ColorUtility.TryParseHtmlString("#" + f[7], out Color color);
                blocks.Add(new MapBlock
                {
                    name = f[0],
                    type = (MapBlockType)System.Enum.Parse(typeof(MapBlockType), f[1]),
                    col = Parse(f[2]),
                    row = Parse(f[3]),
                    width = Parse(f[4]),
                    height = Parse(f[5]),
                    front = (MapFront)System.Enum.Parse(typeof(MapFront), f[6]),
                    color = color,
                    link = f.Length > 8 ? f[8].Trim() : ""
                });
            }

            return blocks;
        }

        private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Cizim -> dunya donusumu. Cizimde sutun saga (dogu), satir asagi (guney) artar.
        /// Dogu = -z, guney = -x (cizim dukkanin etrafinda saat yonunde 90 derece doner).
        /// </summary>
        private readonly struct MapGrid
        {
            private readonly float cell;
            private readonly float zAtAnchorCol;
            private readonly float anchorCol;
            private readonly float xAtAnchorRow;
            private readonly float anchorRow;

            public MapGrid(float cell, float zAtAnchorCol, float anchorCol, float xAtAnchorRow, float anchorRow)
            {
                this.cell = cell;
                this.zAtAnchorCol = zAtAnchorCol;
                this.anchorCol = anchorCol;
                this.xAtAnchorRow = xAtAnchorRow;
                this.anchorRow = anchorRow;
            }

            public float Z(float col) => zAtAnchorCol - (col - anchorCol) * cell;
            public float X(float row) => xAtAnchorRow - (row - anchorRow) * cell;

            /// <summary>Dunya dikdortgeni: Rect.x = dunya x, Rect.y = dunya z.</summary>
            public Rect ToWorld(float col, float row, float width, float height) =>
                Rect.MinMaxRect(X(row + height), Z(col + width), X(row), Z(col));

            public Rect ToWorld(MapBlock block) => ToWorld(block.col, block.row, block.width, block.height);

            public Vector3 Point(float col, float row) => new Vector3(X(row), 0f, Z(col));

            /// <summary>Cizimdeki yonun dunyadaki karsiligi.</summary>
            public static Vector3 Direction(MapFront front)
            {
                switch (front)
                {
                    case MapFront.K: return Vector3.right;
                    case MapFront.G: return Vector3.left;
                    case MapFront.D: return Vector3.back;
                    default: return Vector3.forward;
                }
            }
        }

        // ---------------- kurulum ----------------

        /// <summary>Haritanin tamami asfalt; ana salon zemininin altina dokunulmaz (bodrum merdiveni orada).</summary>
        private static void BuildGround(Transform parent, Rect mapRect, Bounds floor)
        {
            Rect hole = Rect.MinMaxRect(floor.min.x, floor.min.z, floor.max.x, floor.max.z);
            Material asphalt = TableGamesBuilder.GetOrCreateMaterial("MahalleAsfaltMat", AsphaltColor);

            int index = 0;
            foreach (Rect piece in Subtract(mapRect, hole))
                BuildSlab(parent, $"Asfalt{++index}", piece, GroundTop, asphalt, keepCollider: true, thickness: GroundThickness);
        }

        /// <summary>Haritanin dort kenarinda gorunmez duvar: oyuncu haritadan cikamaz.</summary>
        private static void BuildBoundary(Transform parent, Rect r)
        {
            const float t = 1f;
            BuildInvisibleWall(parent, "SinirKuzey", Rect.MinMaxRect(r.xMax, r.yMin - t, r.xMax + t, r.yMax + t));
            BuildInvisibleWall(parent, "SinirGuney", Rect.MinMaxRect(r.xMin - t, r.yMin - t, r.xMin, r.yMax + t));
            BuildInvisibleWall(parent, "SinirDogu", Rect.MinMaxRect(r.xMin, r.yMin - t, r.xMax, r.yMin));
            BuildInvisibleWall(parent, "SinirBati", Rect.MinMaxRect(r.xMin, r.yMax, r.xMax, r.yMax + t));
        }

        private static void BuildOutside(Transform groundRoot, Transform boundaryRoot, MapBlock block, MapGrid grid)
        {
            Rect rect = grid.ToWorld(block);
            BuildSlab(groundRoot, block.name, rect, 0.01f, MaterialFor(block.color), keepCollider: false);
            BuildInvisibleWall(boundaryRoot, block.name + "Duvari", rect);
        }

        /// <summary>Blogun cevresinde 1.5 m kaldirim; dukkan ve bahcenin icine tasan kisim kesilir.</summary>
        private static void BuildSidewalk(Transform parent, Rect r, Rect mapRect, List<Rect> shopRects)
        {
            float w = SidewalkWidth;
            var strips = new List<Rect>
            {
                Rect.MinMaxRect(r.xMax, r.yMin - w, r.xMax + w, r.yMax + w),
                Rect.MinMaxRect(r.xMin - w, r.yMin - w, r.xMin, r.yMax + w),
                Rect.MinMaxRect(r.xMin, r.yMin - w, r.xMax, r.yMin),
                Rect.MinMaxRect(r.xMin, r.yMax, r.xMax, r.yMax + w)
            };

            Material material = TableGamesBuilder.GetOrCreateMaterial("MahalleKaldirimMat", SidewalkColor);
            foreach (Rect strip in strips)
            {
                Rect clipped = Intersect(strip, mapRect);
                if (clipped.width <= 0.01f || clipped.height <= 0.01f) continue;

                var pieces = new List<Rect> { clipped };
                foreach (Rect shop in shopRects)
                {
                    var next = new List<Rect>();
                    foreach (Rect piece in pieces) next.AddRange(Subtract(piece, shop));
                    pieces = next;
                }

                foreach (Rect piece in pieces)
                    BuildSlab(parent, "Kaldirim", piece, SidewalkTop, material, keepCollider: true);
            }
        }

        /// <summary>Bina: on yuzu (yerel +Z) sokaga bakan kutu. Mulk binasi sahnede varsa o tasinir.</summary>
        private static void BuildBuilding(Transform parent, MapBlock block, Rect rect,
                                          Dictionary<string, PropertyBuilding> properties)
        {
            GameObject building;
            if (!string.IsNullOrEmpty(block.link) && properties.TryGetValue(block.link, out PropertyBuilding existing))
            {
                building = existing.gameObject;
                building.name = block.name + "Binasi";
                building.transform.SetParent(parent, worldPositionStays: true);
            }
            else
            {
                building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = block.name;
                building.transform.SetParent(parent, worldPositionStays: false);
            }

            float height = HeightFor(block);
            Vector3 front = MapGrid.Direction(block.front);
            bool facesAlongZ = Mathf.Abs(front.z) > 0.5f;

            building.transform.position = new Vector3(rect.center.x, height * 0.5f, rect.center.y);
            building.transform.rotation = Quaternion.LookRotation(front, Vector3.up);
            building.transform.localScale = facesAlongZ
                ? new Vector3(rect.width, height, rect.height)
                : new Vector3(rect.height, height, rect.width);

            var renderer = building.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = MaterialFor(block.color);
        }

        private static Transform BuildEntry(Transform parent, MapBlock block, MapGrid grid, Vector3 door)
        {
            var entry = new GameObject(block.name);
            entry.transform.SetParent(parent, worldPositionStays: false);
            entry.transform.position = grid.Point(block.col, block.row);

            Vector3 look = door - entry.transform.position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) entry.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
            return entry.transform;
        }

        private static void LinkSpawner(CustomerSpawner spawner, List<(Transform point, string profile)> entries)
        {
            if (spawner == null)
            {
                Debug.LogError("[NeighborhoodBuilder] Sahnede CustomerSpawner yok; giris noktalari baglanamadi.");
                return;
            }

            var serialized = new SerializedObject(spawner);
            SerializedProperty list = serialized.FindProperty("streetEntries");
            list.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("point").objectReferenceValue = entries[i].point;

                CustomerProfile profile = FindProfile(entries[i].profile);
                SerializedProperty preferred = element.FindPropertyRelative("preferredProfiles");
                preferred.arraySize = profile != null ? 1 : 0;
                if (profile != null) preferred.GetArrayElementAtIndex(0).objectReferenceValue = profile;
                else if (!string.IsNullOrEmpty(entries[i].profile))
                    Debug.LogWarning($"[NeighborhoodBuilder] '{entries[i].profile}' profili bulunamadi; {entries[i].point.name} herkese acik kaldi.");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static CustomerProfile FindProfile(string assetName)
        {
            if (string.IsNullOrEmpty(assetName)) return null;

            foreach (string guid in AssetDatabase.FindAssets($"{assetName} t:CustomerProfile"))
            {
                var profile = AssetDatabase.LoadAssetAtPath<CustomerProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile != null && profile.name == assetName) return profile;
            }

            return null;
        }

        private static Dictionary<string, PropertyBuilding> FindPropertyBuildings()
        {
            var result = new Dictionary<string, PropertyBuilding>();
            foreach (PropertyBuilding building in Object.FindObjectsByType<PropertyBuilding>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (building.Property != null) result[building.Property.name] = building;
            }

            return result;
        }

        // ---------------- yardimcilar ----------------

        private static float HeightFor(MapBlock block)
        {
            switch (block.type)
            {
                case MapBlockType.Ev:
                    // 6-9 m arasi, isme gore sabit (her kurulumda ayni cikar).
                    return 6f + StableHash(block.name) % 4;
                case MapBlockType.Avlu: return 2.5f;
                case MapBlockType.Amca: return 14f;
                default:
                    if (block.name == "Hastane") return 12f;
                    if (block.name == "Okul") return 9f;
                    if (block.name == "Pavyon") return 5f;
                    return 4f;
            }
        }

        private static int StableHash(string text)
        {
            int sum = 0;
            foreach (char c in text) sum += c;
            return sum;
        }

        private static Material MaterialFor(Color color) =>
            TableGamesBuilder.GetOrCreateMaterial("Mahalle_" + ColorUtility.ToHtmlStringRGB(color), color);

        /// <summary>Ustu 'top' yuksekliginde ince kutu (zemin, kaldirim, park).</summary>
        private static void BuildSlab(Transform parent, string name, Rect rect, float top, Material material,
                                      bool keepCollider, float thickness = 0.1f)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetParent(parent, worldPositionStays: false);
            slab.transform.position = new Vector3(rect.center.x, top - thickness * 0.5f, rect.center.y);
            slab.transform.localScale = new Vector3(rect.width, thickness, rect.height);
            slab.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!keepCollider) Object.DestroyImmediate(slab.GetComponent<Collider>());
        }

        /// <summary>Sadece collider: renderer olmadigi icin NavMesh'e girmez (Render Meshes), oyuncuyu durdurur.</summary>
        private static void BuildInvisibleWall(Transform parent, string name, Rect rect)
        {
            var wall = new GameObject(name);
            wall.transform.SetParent(parent, worldPositionStays: false);
            wall.transform.position = new Vector3(rect.center.x, BoundaryHeight * 0.5f, rect.center.y);
            var box = wall.AddComponent<BoxCollider>();
            box.size = new Vector3(rect.width, BoundaryHeight, rect.height);
        }

        private static void AddNotWalkable(GameObject target)
        {
            var modifier = target.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
        }

        private static Transform NewGroup(string name, Transform parent)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, worldPositionStays: false);
            return group.transform;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) return;

            // Icindeki mulk binalari kaybolmasin: once disari al (yeniden tasinacaklar).
            foreach (PropertyBuilding building in child.GetComponentsInChildren<PropertyBuilding>(true))
                building.transform.SetParent(parent, worldPositionStays: true);

            Object.DestroyImmediate(child.gameObject);
        }

        private static Rect Intersect(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
                            Mathf.Min(a.xMax, b.xMax), Mathf.Min(a.yMax, b.yMax));

        /// <summary>a - b: en fazla 4 dikdortgen.</summary>
        private static List<Rect> Subtract(Rect a, Rect b)
        {
            var result = new List<Rect>();
            Rect overlap = Intersect(a, b);
            if (overlap.width <= 0f || overlap.height <= 0f)
            {
                result.Add(a);
                return result;
            }

            AddIfValid(result, Rect.MinMaxRect(a.xMin, a.yMin, overlap.xMin, a.yMax));
            AddIfValid(result, Rect.MinMaxRect(overlap.xMax, a.yMin, a.xMax, a.yMax));
            AddIfValid(result, Rect.MinMaxRect(overlap.xMin, a.yMin, overlap.xMax, overlap.yMin));
            AddIfValid(result, Rect.MinMaxRect(overlap.xMin, overlap.yMax, overlap.xMax, a.yMax));
            return result;
        }

        private static void AddIfValid(List<Rect> list, Rect rect)
        {
            if (rect.width > 0.01f && rect.height > 0.01f) list.Add(rect);
        }
    }
}
