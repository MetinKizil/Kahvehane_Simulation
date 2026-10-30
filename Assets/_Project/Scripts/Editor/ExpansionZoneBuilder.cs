using System.Text;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Furniture;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D6b - Dukkan genisletme bolgelerini kurar. Her bolge (Zones tablosunda bir satir):
    ///  1) 'Genisletmeler' altinda KAPALI bir oda: zemin (ShopFloor, ana zeminin malzemesi) + dis duvarlar veya 1 m cit
    ///  2) UpgradeData (yoksa olusturulur; fiyat, itibar, onkosul, masa bonusu tablodan) -> GelistirmeKatalogu
    ///  3) 'Managers'ta UpgradeUnlocker: alininca oda acilir, aradaki duvar (hideTargets) kapanir
    /// Olculer ana zeminin Renderer.bounds'undan ve ShopWallBuilder'in duvar kalinligindan hesaplanir; elle koordinat yok.
    /// Tekrar calistirilabilir: oda yeniden kurulur, upgrade/acici cogaltilmaz.
    /// 'Dukkan Duvarlarini Kur' tekrar calistirilirsa duvar baglantisi kopar -> bunu da tekrar calistir.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Genisletme Bolgelerini Kur
    /// </summary>
    public static class ExpansionZoneBuilder
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string MasaDataPath = DataFolder + "/MasaEsya.asset";
        private const string RootName = "Genisletmeler";
        private const string MainWallsName = "Duvarlar";
        private const string OuterWallName = "DuvarDis";
        private const float WallHeight = 3f;
        private const float FenceHeight = 1f;
        private const float WallThickness = 0.2f;
        private static readonly Color WallColor = new Color(0.85f, 0.8f, 0.7f);
        private static readonly Color FenceColor = new Color(0.45f, 0.3f, 0.18f);
        private static readonly Color GrassColor = new Color(0.3f, 0.55f, 0.22f);

        private enum Side { Right, Left, Back }

        private readonly struct Zone
        {
            public readonly string Name;          // Obje ve asset adi (ASCII). Gorunen ad asset'te Turkcelestirilir.
            public readonly string UpgradeAsset;
            public readonly Side Side;
            public readonly int Stage;            // 1 = ana salona bitisik, 2 = 1. asamanin disina
            public readonly float Depth;          // Ana salondan disari dogru derinlik (m)
            public readonly bool IsFence;         // Bahce: catisiz, 1 m cit
            public readonly int Cost;
            public readonly int Reputation;
            public readonly int TableBonus;
            public readonly string[] Prerequisites;  // UpgradeData asset adlari
            public readonly string HiddenWall;       // Alininca kapanan duvar ("Duvarlar/DuvarSag" veya "<onceki bolge>")

            public Zone(string name, string upgradeAsset, Side side, int stage, float depth, bool isFence,
                        int cost, int reputation, int tableBonus, string[] prerequisites, string hiddenWall)
            {
                Name = name;
                UpgradeAsset = upgradeAsset;
                Side = side;
                Stage = stage;
                Depth = depth;
                IsFence = isFence;
                Cost = cost;
                Reputation = reputation;
                TableBonus = tableBonus;
                Prerequisites = prerequisites;
                HiddenWall = hiddenWall;
            }
        }

        // ---- BOLGE TABLOSU (sira onemli: onkosul asset'i kendinden once olusmus olmali) ----
        // Dukkan ici (sag + arka) istenen sirayla; ayni tarafta 2. asama 1.'den sonra. Bahce dukkan ici 4 genisletmeden sonra.
        // Ust Kat (D6f) en sonda, Buyuk Bahce'den sonra.
        private static readonly Zone[] Zones =
        {
            new Zone("YanOda", "YanOdaUpgrade", Side.Right, 1, 5f, false,
                     600, 30, 3, new string[0], "Duvarlar/DuvarSag"),
            new Zone("BuyukYanOda", "BuyukYanOdaUpgrade", Side.Right, 2, 5f, false,
                     1400, 110, 3, new[] { "YanOdaUpgrade" }, "YanOda"),
            new Zone("ArkaSalon", "ArkaSalonUpgrade", Side.Back, 1, 5f, false,
                     900, 60, 3, new string[0], "Duvarlar/DuvarArka"),
            new Zone("GenisArkaSalon", "GenisArkaSalonUpgrade", Side.Back, 2, 5f, false,
                     1800, 150, 3, new[] { "ArkaSalonUpgrade" }, "ArkaSalon"),
            new Zone("Bahce", "BahceUpgrade", Side.Left, 1, 5f, true,
                     2000, 160, 4, new[] { "YanOdaUpgrade", "BuyukYanOdaUpgrade", "ArkaSalonUpgrade", "GenisArkaSalonUpgrade" },
                     "Duvarlar/DuvarSol"),
            new Zone("BuyukBahce", "BuyukBahceUpgrade", Side.Left, 2, 5f, true,
                     2800, 200, 4, new[] { "BahceUpgrade" }, "Bahce"),
        };

        [MenuItem("Kan Bagi/Genisletme Bolgelerini Kur")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[ExpansionZoneBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            var masa = AssetDatabase.LoadAssetAtPath<FurnitureData>(MasaDataPath);

            if (surface == null || !surface.TryGetComponent(out Renderer floorRenderer) || upgradeManager == null || masa == null)
            {
                Debug.LogError("[ExpansionZoneBuilder] Ana zemin (NavMeshSurface + Renderer), UpgradeManager veya MasaEsya bulunamadi.");
                return;
            }

            if (GameObject.Find("/" + MainWallsName) == null)
            {
                Debug.LogError("[ExpansionZoneBuilder] 'Duvarlar' yok. Once 'Kan Bagi > Dukkan Duvarlarini Kur'.");
                return;
            }

            GameObject root = GameObject.Find("/" + RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Genisletmeler");
            }

            Bounds mainFloor = floorRenderer.bounds;
            Material floorMaterial = floorRenderer.sharedMaterial;
            var report = new StringBuilder($"[ExpansionZoneBuilder] {Zones.Length} genisletme bolgesi kuruldu:\n");

            foreach (Zone zone in Zones)
            {
                UpgradeData upgrade = GetOrCreateUpgrade(zone, masa);
                if (upgrade == null) continue;

                GameObject room = BuildRoom(root.transform, zone, mainFloor, floorMaterial);

                GameObject hidden = FindHiddenWall(root.transform, zone.HiddenWall);
                if (hidden == null) report.AppendLine($"  UYARI: kapanacak duvar '{zone.HiddenWall}' bulunamadi.");

                WireUnlocker(upgradeManager.gameObject, upgrade, room, hidden);
                bool addedToCatalog = TableGamesBuilder.AddToCatalog(upgradeManager, upgrade);

                report.AppendLine($"  {zone.Name}: {upgrade.Cost} TL, itibar {upgrade.RequiredReputation}, masa +{zone.TableBonus}, " +
                                  $"kapanan duvar: {(hidden != null ? hidden.name : "-")}, katalog: {(addedToCatalog ? "eklendi" : "zaten vardi")}");
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            report.AppendLine("Sahneyi kaydet (Ctrl+S). Gorunen ad/aciklama asset'te Turkcelestirilir.");
            Debug.Log(report.ToString());
        }

        /// <summary>Asset yoksa olusturur. Ad/aciklama bossa ASCII yer tutucu yazilir (Turkcesi asset'te).</summary>
        private static UpgradeData GetOrCreateUpgrade(Zone zone, FurnitureData masa)
        {
            string path = $"{DataFolder}/{zone.UpgradeAsset}.asset";
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(upgrade, path);
            }

            var serialized = new SerializedObject(upgrade);
            SerializedProperty nameProperty = serialized.FindProperty("upgradeName");
            if (string.IsNullOrEmpty(nameProperty.stringValue)) nameProperty.stringValue = zone.Name;

            SerializedProperty descriptionProperty = serialized.FindProperty("description");
            if (string.IsNullOrEmpty(descriptionProperty.stringValue))
                descriptionProperty.stringValue = $"Masa siniri +{zone.TableBonus}.";

            serialized.FindProperty("cost").intValue = zone.Cost;
            serialized.FindProperty("requiredReputation").intValue = zone.Reputation;

            SerializedProperty prerequisites = serialized.FindProperty("requiredUpgrades");
            prerequisites.arraySize = 0;
            foreach (string prerequisiteName in zone.Prerequisites)
            {
                var prerequisite = AssetDatabase.LoadAssetAtPath<UpgradeData>($"{DataFolder}/{prerequisiteName}.asset");
                if (prerequisite == null)
                {
                    Debug.LogError($"[ExpansionZoneBuilder] Onkosul bulunamadi: {prerequisiteName} ({zone.Name}). Tablo sirasini kontrol et.");
                    continue;
                }

                prerequisites.arraySize++;
                prerequisites.GetArrayElementAtIndex(prerequisites.arraySize - 1).objectReferenceValue = prerequisite;
            }

            SerializedProperty bonuses = serialized.FindProperty("limitBonuses");
            bonuses.arraySize = 1;
            SerializedProperty bonus = bonuses.GetArrayElementAtIndex(0);
            bonus.FindPropertyRelative("furniture").objectReferenceValue = masa;
            bonus.FindPropertyRelative("amount").intValue = zone.TableBonus;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        /// <summary>Bolgenin dikdortgeni (x,z) ve dis duvari hesaplanir; oda kapali olarak kurulur.</summary>
        private static GameObject BuildRoom(Transform root, Zone zone, Bounds main, Material floorMaterial)
        {
            Transform old = root.Find(zone.Name);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            float offset = (zone.Stage - 1) * zone.Depth;
            float minX = main.min.x, maxX = main.max.x, minZ = main.min.z, maxZ = main.max.z;

            switch (zone.Side)
            {
                case Side.Right: minX = main.max.x + offset; maxX = minX + zone.Depth; break;
                case Side.Left: maxX = main.min.x - offset; minX = maxX - zone.Depth; break;
                case Side.Back: minZ = main.max.z + offset; maxZ = minZ + zone.Depth; break;
            }

            var room = new GameObject(zone.Name);
            Undo.RegisterCreatedObjectUndo(room, zone.Name);
            room.transform.SetParent(root, false);
            float y = main.min.y;

            // Zemin: Plane 10x10 m; olcek bolgeye gore.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Zemin";
            floor.transform.SetParent(room.transform, false);
            floor.transform.position = new Vector3((minX + maxX) * 0.5f, y, (minZ + maxZ) * 0.5f);
            floor.transform.localScale = new Vector3((maxX - minX) / 10f, 1f, (maxZ - minZ) / 10f);
            // Bahce cimen; kapali odalar ana salonun zeminiyle ayni.
            Material zoneFloor = zone.IsFence ? TableGamesBuilder.GetOrCreateMaterial("BahceCimMat", GrassColor) : floorMaterial;
            if (zoneFloor != null) floor.GetComponent<Renderer>().sharedMaterial = zoneFloor;
            floor.AddComponent<ShopFloor>();

            // Duvarlar/cit: ana salonun disindaki uc kenar. Koseler kalinlik kadar tasar (bosluk kalmasin).
            var walls = new GameObject(zone.IsFence ? "Cit" : "Duvarlar");
            walls.transform.SetParent(room.transform, false);
            var modifier = walls.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            modifier.applyToChildren = true;

            float height = zone.IsFence ? FenceHeight : WallHeight;
            Material material = zone.IsFence
                ? TableGamesBuilder.GetOrCreateMaterial("CitMat", FenceColor)
                : TableGamesBuilder.GetOrCreateMaterial("DuvarMat", WallColor);
            float t = WallThickness;
            float h = t * 0.5f;

            if (zone.Side == Side.Back)
            {
                CreateWall(walls.transform, "DuvarSol", new Vector3(minX - h, y, (minZ + maxZ) * 0.5f), new Vector3(t, height, maxZ - minZ), material);
                CreateWall(walls.transform, "DuvarSag", new Vector3(maxX + h, y, (minZ + maxZ) * 0.5f), new Vector3(t, height, maxZ - minZ), material);
                CreateWall(walls.transform, OuterWallName, new Vector3((minX + maxX) * 0.5f, y, maxZ + h), new Vector3(maxX - minX + 2f * t, height, t), material);
            }
            else
            {
                float outerX = zone.Side == Side.Right ? maxX + h : minX - h;
                float spanMinX = zone.Side == Side.Right ? minX : minX - t;
                float spanMaxX = zone.Side == Side.Right ? maxX + t : maxX;
                float centerX = (spanMinX + spanMaxX) * 0.5f;
                float spanX = spanMaxX - spanMinX;

                CreateWall(walls.transform, "DuvarOn", new Vector3(centerX, y, minZ - h), new Vector3(spanX, height, t), material);
                CreateWall(walls.transform, "DuvarArka", new Vector3(centerX, y, maxZ + h), new Vector3(spanX, height, t), material);
                CreateWall(walls.transform, OuterWallName, new Vector3(outerX, y, (minZ + maxZ) * 0.5f), new Vector3(t, height, maxZ - minZ + 2f * t), material);

                // Ana salona bitisik citli bolge: ana yan duvar kalkinca binanin on/arka koselerinde 1 m'nin ustu acik
                // kalir; bina yuksekliginde birer kose diregi bu boslugu kapatir.
                if (zone.IsFence && zone.Stage == 1)
                {
                    float pillarX = zone.Side == Side.Right ? main.max.x + h : main.min.x - h;
                    Material wallMaterial = TableGamesBuilder.GetOrCreateMaterial("DuvarMat", WallColor);
                    CreateWall(walls.transform, "KoseOn", new Vector3(pillarX, y, main.min.z - h), new Vector3(t, WallHeight, t), wallMaterial);
                    CreateWall(walls.transform, "KoseArka", new Vector3(pillarX, y, main.max.z + h), new Vector3(t, WallHeight, t), wallMaterial);
                }
            }

            room.SetActive(false);
            return room;
        }

        /// <summary>"Duvarlar/DuvarSag" ana duvarlardan; "YanOda" gibi bir bolge adi o bolgenin dis duvari.</summary>
        private static GameObject FindHiddenWall(Transform root, string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            if (path.StartsWith(MainWallsName + "/"))
            {
                GameObject mainWalls = GameObject.Find("/" + MainWallsName);
                Transform wall = mainWalls != null ? mainWalls.transform.Find(path.Substring(MainWallsName.Length + 1)) : null;
                return wall != null ? wall.gameObject : null;
            }

            Transform zone = root.Find(path);
            if (zone == null) return null;

            foreach (Transform child in zone.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == OuterWallName) return child.gameObject;
            }

            return null;
        }

        /// <summary>Ayni upgrade'e bagli acici varsa guncellenir; yoksa eklenir.</summary>
        private static void WireUnlocker(GameObject managers, UpgradeData upgrade, GameObject room, GameObject hiddenWall)
        {
            UpgradeUnlocker unlocker = TableGamesBuilder.FindByUpgrade<UpgradeUnlocker>(managers, upgrade);
            if (unlocker == null) unlocker = Undo.AddComponent<UpgradeUnlocker>(managers);

            var serialized = new SerializedObject(unlocker);
            serialized.FindProperty("requiredUpgrade").objectReferenceValue = upgrade;

            SerializedProperty targets = serialized.FindProperty("targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = room;

            SerializedProperty hide = serialized.FindProperty("hideTargets");
            hide.arraySize = hiddenWall != null ? 1 : 0;
            if (hiddenWall != null) hide.GetArrayElementAtIndex(0).objectReferenceValue = hiddenWall;

            serialized.ApplyModifiedProperties();
        }

        private static void CreateWall(Transform parent, string wallName, Vector3 basePosition, Vector3 size, Material material)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = wallName;
            wall.transform.SetParent(parent, true);
            wall.transform.position = basePosition + Vector3.up * (size.y * 0.5f);
            wall.transform.localScale = size;

            if (material != null) wall.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
