using System.Collections.Generic;
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
    /// D6g - Ust Kat: dukkanin kapali kisminin (ana salon + yan odalar + arka salonlar + kose salon, 20x20 m) ustune kat.
    ///  - On yari (kapi tarafindan 10 m) TERAS: catisiz, 1 m korkuluk. Arka yari KAPALI: 3 m duvar, cati, lambalar;
    ///    terasla arasinda kapi boslugu olan ara duvar.
    ///  - L merdiven: bahceye bakan sol duvar boyunca arkaya cikar, sol arka kosede sahanlik, Genis Arka Salon'un
    ///    arka duvari boyunca saga doner (11 basamak + sahanlik, 25 cm). Ust kat doseme bu L'nin ustunde bostur, kenarlari korkuluklu.
    ///  - Zemin katin tavanina lambalar (doseme gunesi keser).
    ///  - Merdivenin yeri (MerdivenAlani, iki ReservedArea) satin almadan once de esyaya kapali.
    ///  - UstKatUpgrade: onkosul Kose Salon + Kose Bahce, masa +8, her makine (cay ocagi, oralet, kahve, sahlep) +1.
    ///    Cay ocagi ikinci kopyasi alinabilsin diye ESYALAR'a eklenir (fiyat CayOcagiPrice).
    /// Doseme ve cati "Not Walkable" ISARETLENMEZ (D6c dersi: alttaki/ustteki yuruyus yuzeyini bozar).
    /// Olculer ana zeminin Renderer.bounds'undan; elle koordinat yok. Tekrar calistirilabilir.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Ust Kati Kur
    /// </summary>
    public static class UpperFloorBuilder
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string UpgradePath = DataFolder + "/UstKatUpgrade.asset";
        private const string CatalogPath = DataFolder + "/EsyaKatalogu.asset";
        private const string RootName = "Genisletmeler";
        private const string FloorName = "UstKat";
        private const string StairAreaName = "UstKatMerdivenAlani";

        private const float Extension = 10f;     // Yan oda/arka salon asamalari toplam derinligi (5 + 5)
        private const float FloorHeight = 3f;    // Ust kat zemini (zemin kat duvar yuksekligi)
        private const float StoryHeight = 3f;    // Ust kat duvar yuksekligi
        private const float SlabThickness = 0.2f;
        private const float WallThickness = 0.2f;
        private const float RailHeight = 1f;
        private const float RailThickness = 0.05f;
        private const float TerraceDepth = 10f;  // Kapi tarafindan teras derinligi
        private const float DoorWidth = 2f;
        private const float DoorHeight = 2.4f;

        private const float StairWidth = 1.5f;
        private const float StepRise = 0.25f;
        private const float StepRun = 0.35f;
        private const int FirstRunSteps = 6;     // Sol duvar boyunca (sahanliga kadar)

        private const int UpgradeCost = 5000;
        private const int UpgradeReputation = 250;
        private const int TableBonus = 8;
        private const int MachineBonus = 1;
        private const int CayOcagiPrice = 150;

        private static readonly string[] Prerequisites = { "KoseSalonUpgrade", "KoseBahceUpgrade" };
        private static readonly string[] Machines = { "CayOcagiEsya", "OraletSemaveriEsya", "KahveMakinesiEsya", "SahlepMakinesiEsya" };

        private static readonly Color WallColor = new Color(0.85f, 0.8f, 0.7f);
        private static readonly Color WoodColor = new Color(0.45f, 0.3f, 0.18f);
        private static readonly Color LampColor = new Color(1f, 0.85f, 0.6f);

        [MenuItem("Kan Bagi/Ust Kati Kur")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[UpperFloorBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();
            UpgradeManager upgradeManager = Object.FindFirstObjectByType<UpgradeManager>(FindObjectsInactive.Include);
            GameObject root = GameObject.Find("/" + RootName);
            var masa = AssetDatabase.LoadAssetAtPath<FurnitureData>(DataFolder + "/MasaEsya.asset");

            if (surface == null || !surface.TryGetComponent(out Renderer floorRenderer) || upgradeManager == null || root == null || masa == null)
            {
                Debug.LogError("[UpperFloorBuilder] Ana zemin, UpgradeManager, 'Genisletmeler' veya MasaEsya bulunamadi. " +
                               "Once 'Genisletme Bolgelerini Kur'.");
                return;
            }

            Bounds main = floorRenderer.bounds;
            float y = main.min.y;
            var area = Rect.MinMaxRect(main.min.x, main.min.z, main.max.x + Extension, main.max.z + Extension);

            // L merdiven: kol 1 sol duvar boyunca (+z), sahanlik sol arka kosede, kol 2 arka duvar boyunca (+x).
            float landingMinZ = area.yMax - StairWidth;
            float run1MinZ = landingMinZ - FirstRunSteps * StepRun;
            float run2MinX = area.xMin + StairWidth;
            int secondRunSteps = Mathf.RoundToInt(FloorHeight / StepRise) - FirstRunSteps - 1; // sahanlik bir basamak
            float run2MaxX = run2MinX + secondRunSteps * StepRun;
            var run1 = Rect.MinMaxRect(area.xMin, run1MinZ, area.xMin + StairWidth, area.yMax);   // sahanlik dahil
            var run2 = Rect.MinMaxRect(run2MinX, landingMinZ, run2MaxX, area.yMax);

            // Eski kurulum silinir
            DestroyChild(root.transform, FloorName);
            DestroyChild(root.transform, StairAreaName);

            BuildStairArea(root.transform, run1, run2, y);
            GameObject floor = BuildFloor(root.transform, area, run1, run2, y);

            UpgradeData upgrade = GetOrCreateUpgrade(masa);
            if (upgrade == null) return;

            WireUnlocker(upgradeManager.gameObject, upgrade, floor);
            bool added = TableGamesBuilder.AddToCatalog(upgradeManager, upgrade);
            string cayOcagi = AddCayOcagiToCatalog();

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[UpperFloorBuilder] Ust kat kuruldu: {area.width:0.#} x {area.height:0.#} m, y {y + FloorHeight}; teras on {TerraceDepth} m, " +
                      $"kapali arka {area.height - TerraceDepth} m. L merdiven: kol1 z {run1MinZ:0.##}..{area.yMax:0.##}, kol2 x {run2MinX:0.##}..{run2MaxX:0.##}. " +
                      $"Upgrade {upgrade.Cost} TL / itibar {upgrade.RequiredReputation}, katalog: {(added ? "eklendi" : "zaten vardi")}. {cayOcagi} " +
                      "Sahneyi kaydet (Ctrl+S).");
        }

        // ---- Merdiven alani (satin almadan once de esyaya kapali) ----

        private static void BuildStairArea(Transform root, Rect run1, Rect run2, float y)
        {
            var area = new GameObject(StairAreaName);
            Undo.RegisterCreatedObjectUndo(area, StairAreaName);
            area.transform.SetParent(root, false);

            AddReserved(area.transform, "Kol1", run1, y);
            AddReserved(area.transform, "Kol2", run2, y);
        }

        private static void AddReserved(Transform parent, string objectName, Rect rect, float y)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(rect.center.x, y + 1f, rect.center.y);

            var reserved = go.AddComponent<ReservedArea>();
            var data = new SerializedObject(reserved);
            data.FindProperty("size").vector3Value = new Vector3(rect.width, 2f, rect.height);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Kat ----

        private static GameObject BuildFloor(Transform root, Rect area, Rect run1, Rect run2, float y)
        {
            var floor = new GameObject(FloorName);
            Undo.RegisterCreatedObjectUndo(floor, FloorName);
            floor.transform.SetParent(root, false);

            float top = y + FloorHeight;
            float t = WallThickness;
            float h = t * 0.5f;
            float outMinX = area.xMin - t, outMaxX = area.xMax + t, outMinZ = area.yMin - t, outMaxZ = area.yMax + t;
            float splitZ = area.yMin + TerraceDepth;

            Material wall = TableGamesBuilder.GetOrCreateMaterial("DuvarMat", WallColor);
            Material wood = TableGamesBuilder.GetOrCreateMaterial("CitMat", WoodColor);
            Material slabMaterial = TableGamesBuilder.GetOrCreateMaterial("UstKatDosemeMat", new Color(0.62f, 0.55f, 0.47f));

            // Doseme: dis sinirlar (duvarlarin ustu dahil) eksi L merdiven boslugu. Isaretsiz (yurunebilir).
            Transform slab = Group(floor.transform, "Doseme", notWalkable: false);
            var slabRects = new List<Rect>
            {
                Rect.MinMaxRect(outMinX, outMinZ, outMaxX, run1.yMin),
                Rect.MinMaxRect(run1.xMax, run1.yMin, outMaxX, run2.yMin),
                Rect.MinMaxRect(run2.xMax, run2.yMin, outMaxX, outMaxZ),
                Rect.MinMaxRect(outMinX, run1.yMin, area.xMin, outMaxZ),
                Rect.MinMaxRect(area.xMin, area.yMax, run2.xMax, outMaxZ),
            };
            int index = 0;
            foreach (Rect rect in slabRects)
            {
                if (rect.width < 0.001f || rect.height < 0.001f) continue;
                GameObject piece = Box(slab, $"Doseme{++index}", new Vector3(rect.center.x, top - SlabThickness, rect.center.y),
                                       new Vector3(rect.width, SlabThickness, rect.height), slabMaterial);
                piece.AddComponent<ShopFloor>();
            }

            // Merdiven (zemin kattan), isaretsiz: basamaklar yurunur.
            Transform stairs = Group(floor.transform, "Merdiven", notWalkable: false);
            for (int i = 0; i < FirstRunSteps; i++)
            {
                float stepTop = y + StepRise * (i + 1);
                float zMin = run1.yMin + StepRun * i;
                Box(stairs, $"Basamak{i + 1}", new Vector3(run1.center.x, y, zMin + StepRun * 0.5f),
                    new Vector3(run1.width, stepTop - y, StepRun), wood);
            }

            float landingTop = y + StepRise * (FirstRunSteps + 1);
            Box(stairs, "Sahanlik", new Vector3(run1.center.x, y, (run2.yMin + run2.yMax) * 0.5f),
                new Vector3(run1.width, landingTop - y, run2.height), wood);

            int secondRun = Mathf.RoundToInt((run2.width) / StepRun);
            for (int i = 0; i < secondRun; i++)
            {
                float stepTop = landingTop + StepRise * (i + 1);
                float xMin = run2.xMin + StepRun * i;
                Box(stairs, $"Basamak{FirstRunSteps + 2 + i}", new Vector3(xMin + StepRun * 0.5f, y, run2.center.y),
                    new Vector3(StepRun, stepTop - y, run2.height), wood);
            }

            // Ust katta bosluk korkulugu: kol 1'in ic kenari, kol 1'in on ucu, kol 2'nin ic kenari. Cikis (kol 2 sonu) acik.
            Transform holeRails = Group(floor.transform, "BoslukKorkulugu", notWalkable: true);
            float r = RailThickness * 0.5f;
            Box(holeRails, "Ic1", new Vector3(run1.xMax + r, top, (run1.yMin + run2.yMin) * 0.5f),
                new Vector3(RailThickness, RailHeight, run2.yMin - run1.yMin), wood);
            Box(holeRails, "On1", new Vector3(run1.center.x, top, run1.yMin - r), new Vector3(run1.width, RailHeight, RailThickness), wood);
            Box(holeRails, "Ic2", new Vector3(run2.center.x, top, run2.yMin - r), new Vector3(run2.width, RailHeight, RailThickness), wood);

            // Teras korkulugu (on yari): on, sol, sag.
            Transform terrace = Group(floor.transform, "TerasKorkulugu", notWalkable: true);
            Box(terrace, "On", new Vector3((outMinX + outMaxX) * 0.5f, top, area.yMin - h), new Vector3(outMaxX - outMinX, RailHeight, t), wood);
            Box(terrace, "Sol", new Vector3(area.xMin - h, top, (area.yMin + splitZ) * 0.5f), new Vector3(t, RailHeight, splitZ - area.yMin), wood);
            Box(terrace, "Sag", new Vector3(area.xMax + h, top, (area.yMin + splitZ) * 0.5f), new Vector3(t, RailHeight, splitZ - area.yMin), wood);

            // Kapali yari: dis duvarlar, kapili ara duvar, cati. Duvarlar engel (isaretli), cati isaretsiz.
            Transform walls = Group(floor.transform, "Duvarlar", notWalkable: true);
            float closedSpan = outMaxZ - splitZ;
            Box(walls, "DuvarSol", new Vector3(area.xMin - h, top, (splitZ + outMaxZ) * 0.5f), new Vector3(t, StoryHeight, closedSpan), wall);
            Box(walls, "DuvarSag", new Vector3(area.xMax + h, top, (splitZ + outMaxZ) * 0.5f), new Vector3(t, StoryHeight, closedSpan), wall);
            Box(walls, "DuvarArka", new Vector3((outMinX + outMaxX) * 0.5f, top, area.yMax + h), new Vector3(outMaxX - outMinX, StoryHeight, t), wall);

            float doorX = area.center.x;
            float doorMin = doorX - DoorWidth * 0.5f, doorMax = doorX + DoorWidth * 0.5f;
            Box(walls, "AraDuvarSol", new Vector3((outMinX + doorMin) * 0.5f, top, splitZ + h), new Vector3(doorMin - outMinX, StoryHeight, t), wall);
            Box(walls, "AraDuvarSag", new Vector3((doorMax + outMaxX) * 0.5f, top, splitZ + h), new Vector3(outMaxX - doorMax, StoryHeight, t), wall);
            Box(walls, "KapiUstu", new Vector3(doorX, top + DoorHeight, splitZ + h), new Vector3(DoorWidth, StoryHeight - DoorHeight, t), wall);

            Transform roof = Group(floor.transform, "Cati", notWalkable: false);
            Box(roof, "Cati", new Vector3((outMinX + outMaxX) * 0.5f, top + StoryHeight, (splitZ + outMaxZ) * 0.5f),
                new Vector3(outMaxX - outMinX, SlabThickness, closedSpan), wall);

            // Lambalar: zemin kat tavaninda 4x4, ust kat kapali yarisinda 4x2.
            Transform lamps = Group(floor.transform, "Lambalar", notWalkable: false);
            float stepX = area.width / 4f, stepZ = area.height / 4f;
            for (int ix = 0; ix < 4; ix++)
            {
                for (int iz = 0; iz < 4; iz++)
                    Lamp(lamps, $"Zemin{ix}{iz}", new Vector3(area.xMin + stepX * (ix + 0.5f), top - SlabThickness - 0.3f, area.yMin + stepZ * (iz + 0.5f)));

                for (int iz = 0; iz < 2; iz++)
                    Lamp(lamps, $"Ust{ix}{iz}", new Vector3(area.xMin + stepX * (ix + 0.5f), top + StoryHeight - 0.4f,
                                                           splitZ + (area.yMax - splitZ) * (iz + 0.5f) / 2f));
            }

            floor.SetActive(false);
            return floor;
        }

        // ---- Veri ----

        private static UpgradeData GetOrCreateUpgrade(FurnitureData masa)
        {
            var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(UpgradePath);
            if (upgrade == null)
            {
                upgrade = ScriptableObject.CreateInstance<UpgradeData>();
                AssetDatabase.CreateAsset(upgrade, UpgradePath);
            }

            var serialized = new SerializedObject(upgrade);
            SerializedProperty nameProperty = serialized.FindProperty("upgradeName");
            if (string.IsNullOrEmpty(nameProperty.stringValue)) nameProperty.stringValue = "UstKat";
            SerializedProperty description = serialized.FindProperty("description");
            if (string.IsNullOrEmpty(description.stringValue)) description.stringValue = $"Masa siniri +{TableBonus}, her makine +{MachineBonus}.";

            serialized.FindProperty("cost").intValue = UpgradeCost;
            serialized.FindProperty("requiredReputation").intValue = UpgradeReputation;

            SerializedProperty prerequisites = serialized.FindProperty("requiredUpgrades");
            prerequisites.arraySize = 0;
            foreach (string assetName in Prerequisites)
            {
                var prerequisite = AssetDatabase.LoadAssetAtPath<UpgradeData>($"{DataFolder}/{assetName}.asset");
                if (prerequisite == null)
                {
                    Debug.LogError($"[UpperFloorBuilder] Onkosul bulunamadi: {assetName}. Once 'Genisletme Bolgelerini Kur'.");
                    return null;
                }

                prerequisites.arraySize++;
                prerequisites.GetArrayElementAtIndex(prerequisites.arraySize - 1).objectReferenceValue = prerequisite;
            }

            SerializedProperty bonuses = serialized.FindProperty("limitBonuses");
            bonuses.arraySize = 0;
            AddBonus(bonuses, masa, TableBonus);
            foreach (string machineName in Machines)
            {
                var machine = AssetDatabase.LoadAssetAtPath<FurnitureData>($"{DataFolder}/{machineName}.asset");
                if (machine != null) AddBonus(bonuses, machine, MachineBonus);
                else Debug.LogWarning($"[UpperFloorBuilder] Makine bulunamadi: {machineName}");
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        private static void AddBonus(SerializedProperty bonuses, FurnitureData furniture, int amount)
        {
            bonuses.arraySize++;
            SerializedProperty bonus = bonuses.GetArrayElementAtIndex(bonuses.arraySize - 1);
            bonus.FindPropertyRelative("furniture").objectReferenceValue = furniture;
            bonus.FindPropertyRelative("amount").intValue = amount;
        }

        /// <summary>Cay ocagi simdiye kadar satilmiyordu (varsayilan duzende 1 tane). Ust Kat'la ikincisi alinabilsin.</summary>
        private static string AddCayOcagiToCatalog()
        {
            var cayOcagi = AssetDatabase.LoadAssetAtPath<FurnitureData>($"{DataFolder}/CayOcagiEsya.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(CatalogPath);
            if (cayOcagi == null || catalog == null) return "Cay ocagi kataloga eklenemedi.";

            var data = new SerializedObject(cayOcagi);
            if (data.FindProperty("price").intValue <= 0) data.FindProperty("price").intValue = CayOcagiPrice;
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cayOcagi);

            var catalogData = new SerializedObject(catalog);
            SerializedProperty items = catalogData.FindProperty("items");
            for (int i = 0; i < items.arraySize; i++)
            {
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == cayOcagi) return "Cay ocagi katalogda zaten var.";
            }

            items.arraySize++;
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = cayOcagi;
            catalogData.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return $"Cay ocagi ESYALAR'a eklendi ({cayOcagi.Price} TL).";
        }

        private static void WireUnlocker(GameObject managers, UpgradeData upgrade, GameObject floor)
        {
            UpgradeUnlocker unlocker = TableGamesBuilder.FindByUpgrade<UpgradeUnlocker>(managers, upgrade);
            if (unlocker == null) unlocker = Undo.AddComponent<UpgradeUnlocker>(managers);

            var serialized = new SerializedObject(unlocker);
            serialized.FindProperty("requiredUpgrade").objectReferenceValue = upgrade;
            SerializedProperty targets = serialized.FindProperty("targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = floor;
            serialized.FindProperty("hideTargets").arraySize = 0;
            serialized.ApplyModifiedProperties();
        }

        // ---- Yardimcilar ----

        private static void DestroyChild(Transform parent, string childName)
        {
            Transform old = parent.Find(childName);
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        }

        private static Transform Group(Transform parent, string groupName, bool notWalkable)
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

        /// <summary>Taban noktasi verilen kutu (pivot ortada oldugu icin yukari kaydirilir).</summary>
        private static GameObject Box(Transform parent, string boxName, Vector3 basePosition, Vector3 size, Material material)
        {
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = boxName;
            box.transform.SetParent(parent, true);
            box.transform.position = basePosition + Vector3.up * (size.y * 0.5f);
            box.transform.localScale = size;
            if (material != null) box.GetComponent<Renderer>().sharedMaterial = material;
            return box;
        }

        private static void Lamp(Transform parent, string lampName, Vector3 position)
        {
            var lamp = new GameObject(lampName);
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = position;

            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = LampColor;
            light.intensity = 1.5f;
            light.range = 6f;
            light.shadows = LightShadows.None;
        }
    }
}
