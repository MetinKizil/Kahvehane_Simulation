using KanBagi.Illegal;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D6d-1 - Bodrumdaki kumar masasini RULET masasina cevirir (ayni yerde, ayni parent altinda):
    ///  1) Kok 'RuletMasasi': BoxCollider (2.4 x 0.8 x 1.2), NavMeshModifier "Not Walkable", GamblingTable,
    ///     GamblerSpawner (eski masadan kopya: kumarbaz prefab'i, giris noktasi, aralik)
    ///  2) Gorunum (collider'siz): govde, yesil cuha; 0 + 3x12 sayi hucresi ve KIRMIZI/SIYAH alanlari
    ///     (her biri ince BoxCollider + RouletteBetSpot + ustunde 3D sayi), +X ucunda cark (37 renkli dilim, top)
    ///  3) Eski masa silinir; tabela 'Kumar Tabelasini Olustur ve Yerlestir' ile yeniden konur
    /// Masanin koku zemindedir (pivot altta). Olculer bu dosyadaki sabitlerden; elle koordinat yok.
    /// Tekrar calistirilabilir: mevcut rulet masasini da yeniden kurar.
    ///
    /// SADECE EDITOR. Menu: Kan Bagi > Rulet Masasini Kur
    /// </summary>
    public static class RouletteTableBuilder
    {
        private const string TableName = "RuletMasasi";

        private const float Length = 2.4f;   // x
        private const float Width = 1.2f;    // z
        private const float Height = 0.8f;   // ustu
        private const float CellHeight = 0.012f;

        // Sayi tablosu (yerel x/z)
        private const float GridStartX = -0.96f;
        private const float ColumnStep = 0.117f;
        private const float CellWidth = 0.11f;
        private const float RowStep = 0.2f;
        private const float CellDepth = 0.18f;

        // Cark
        private static readonly Vector3 WheelCenter = new Vector3(0.85f, Height, 0f);
        private const float WheelRadius = 0.33f;
        private const float PocketRadius = 0.25f;

        private static readonly Color WoodColor = new Color(0.35f, 0.22f, 0.12f);
        private static readonly Color FeltColor = new Color(0.08f, 0.38f, 0.18f);
        private static readonly Color RedColor = new Color(0.7f, 0.08f, 0.08f);
        private static readonly Color BlackColor = new Color(0.08f, 0.08f, 0.08f);
        private static readonly Color GreenColor = new Color(0.1f, 0.6f, 0.25f);
        private static readonly Color BallColor = new Color(0.95f, 0.95f, 0.95f);

        [MenuItem("Kan Bagi/Rulet Masasini Kur")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[RouletteTableBuilder] Play modundayken calismaz. Once Play'i durdur.");
                return;
            }

            GamblingTable old = Object.FindFirstObjectByType<GamblingTable>(FindObjectsInactive.Include);
            if (old == null || !old.TryGetComponent(out GamblerSpawner oldSpawner))
            {
                Debug.LogError("[RouletteTableBuilder] Sahnede GamblingTable + GamblerSpawner olan kumar masasi yok. Once 'Bodrumu Kur'.");
                return;
            }

            // Yeni masa eskinin yerine, zemine oturur. (Kapali objede Renderer.bounds guvenilmez: transform'dan hesaplanir.)
            // Eski masa Cube (pivot ortada); rulet masasi zaten zeminde (pivot altta, 'Govde' cocugu var).
            bool oldIsRoulette = old.transform.Find("Govde") != null;
            float floorY = oldIsRoulette ? old.transform.position.y : old.transform.position.y - old.transform.lossyScale.y * 0.5f;
            Transform parent = old.transform.parent;
            var position = new Vector3(old.transform.position.x, floorY, old.transform.position.z);
            Quaternion rotation = Quaternion.Euler(0f, old.transform.eulerAngles.y, 0f);

            var root = new GameObject(TableName);
            Undo.RegisterCreatedObjectUndo(root, TableName);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(Length, Height, Width);
            box.center = new Vector3(0f, Height * 0.5f, 0f);

            var modifier = root.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            modifier.applyToChildren = true;

            root.AddComponent<GamblingTable>();
            var spawner = root.AddComponent<GamblerSpawner>();
            EditorUtility.CopySerialized(oldSpawner, spawner);

            Material wood = TableGamesBuilder.GetOrCreateMaterial("RuletAhsapMat", WoodColor);
            Material felt = TableGamesBuilder.GetOrCreateMaterial("RuletCuhaMat", FeltColor);
            Material red = TableGamesBuilder.GetOrCreateMaterial("RuletKirmiziMat", RedColor);
            Material black = TableGamesBuilder.GetOrCreateMaterial("RuletSiyahMat", BlackColor);
            Material green = TableGamesBuilder.GetOrCreateMaterial("RuletYesilMat", GreenColor);
            Material ball = TableGamesBuilder.GetOrCreateMaterial("RuletTopMat", BallColor);

            CreateVisual(root.transform, PrimitiveType.Cube, "Govde", new Vector3(0f, (Height - 0.02f) * 0.5f, 0f),
                         new Vector3(Length, Height - 0.02f, Width), wood);
            CreateVisual(root.transform, PrimitiveType.Cube, "Cuha", new Vector3(0f, Height - 0.01f, 0f),
                         new Vector3(Length - 0.1f, 0.02f, Width - 0.1f), felt);

            BuildBetSpots(root.transform, red, black, green);
            BuildWheel(root.transform, wood, red, black, green, ball);

            Undo.DestroyObjectImmediate(old.gameObject);

            // Tabela (KumarTabelasi) masanin ustune yeniden konur ve bu masaya baglanir.
            GamblingSignBuilder.BuildAndPlace();

            // Kapali objede collider sinirlari sifir doner; tabela carkin (krupiye) ucunun ustune elle hesapla konur.
            var sign = root.GetComponentInChildren<KanBagi.UI.GamblingSignUI>(true);
            if (sign != null) sign.transform.position = root.transform.TransformPoint(new Vector3(0.9f, Height + 0.9f, 0f));

            EditorSceneManager.MarkSceneDirty(root.scene);
            Debug.Log($"[RouletteTableBuilder] Rulet masasi kuruldu ({position}). Sahneyi kaydet (Ctrl+S), " +
                      "'Metinleri Uygula' calistir ve tekrar kaydet.");
        }

        // ---- Bahis alanlari ----

        private static void BuildBetSpots(Transform root, Material red, Material black, Material green)
        {
            var group = new GameObject("BahisAlanlari");
            group.transform.SetParent(root, false);

            // 0: tablonun solunda, uc satir boyu
            CreateSpot(group.transform, "Sayi0", RouletteBetKind.Number, 0,
                       new Vector3(GridStartX - 0.08f, 0f, 0f), new Vector2(0.11f, RowStep * 3f - 0.02f), green, "0");

            for (int n = 1; n <= 36; n++)
            {
                int column = (n - 1) / 3;
                int row = (n - 1) % 3;
                float x = GridStartX + column * ColumnStep + CellWidth * 0.5f;
                float z = (row - 1) * RowStep;
                CreateSpot(group.transform, $"Sayi{n}", RouletteBetKind.Number, n, new Vector3(x, 0f, z),
                           new Vector2(CellWidth, CellDepth), RouletteRules.IsRed(n) ? red : black, n.ToString());
            }

            // Renk alanlari: tablonun bir yaninda
            CreateSpot(group.transform, "KirmiziAlan", RouletteBetKind.Red, 0, new Vector3(-0.6f, 0f, 0.45f),
                       new Vector2(0.6f, 0.16f), red, "KIRMIZI");
            CreateSpot(group.transform, "SiyahAlan", RouletteBetKind.Black, 0, new Vector3(0.1f, 0f, 0.45f),
                       new Vector2(0.6f, 0.16f), black, "SIYAH");
        }

        private static void CreateSpot(Transform parent, string spotName, RouletteBetKind kind, int number,
                                       Vector3 center, Vector2 size, Material material, string label)
        {
            GameObject spot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spot.name = spotName;
            spot.transform.SetParent(parent, false);
            spot.transform.localPosition = new Vector3(center.x, Height + CellHeight * 0.5f, center.z);
            spot.transform.localScale = new Vector3(size.x, CellHeight, size.y);
            spot.GetComponent<Renderer>().sharedMaterial = material;

            var betSpot = spot.AddComponent<RouletteBetSpot>();
            var data = new SerializedObject(betSpot);
            data.FindProperty("kind").enumValueIndex = (int)kind;
            data.FindProperty("number").intValue = number;
            data.ApplyModifiedPropertiesWithoutUndo();

            // Yazi: alanin ustunde, yere yatik (yukaridan okunur). Alanin olcegini miras almasin diye ayri cocuk.
            var labelObject = new GameObject("Yazi", typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(center.x, Height + CellHeight + 0.002f, center.z);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            labelObject.name = $"{spotName}Yazi";

            // Sabit punto: masa bodrumla birlikte KAPALI kuruldugu icin TMP otomatik boyutu hic calismiyor, yazi 36 puntoda
            // (metrelerce) kaliyordu. 3D TMP'de punto 1 ~ 0.1 m.
            TextMeshPro text = labelObject.AddComponent<TextMeshPro>();
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableAutoSizing = false;
            text.fontSize = kind == RouletteBetKind.Number ? 0.6f : 0.9f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(size.x * 0.9f, size.y * 0.8f);

            // Yazilar NavMesh'e hic girmesin (Render Meshes topluyor; yazi masanin "yurunemez" alanini buyutuyordu).
            var ignore = labelObject.AddComponent<NavMeshModifier>();
            ignore.ignoreFromBuild = true;
        }

        // ---- Cark ----

        private static void BuildWheel(Transform root, Material wood, Material red, Material black, Material green, Material ballMaterial)
        {
            var wheelObject = new GameObject("Cark");
            wheelObject.transform.SetParent(root, false);
            wheelObject.transform.localPosition = WheelCenter;

            // Kase (sabit) ve donen disk
            CreateVisual(wheelObject.transform, PrimitiveType.Cylinder, "Kase", new Vector3(0f, 0.02f, 0f),
                         new Vector3(WheelRadius * 2f, 0.02f, WheelRadius * 2f), wood);

            var disk = new GameObject("Disk");
            disk.transform.SetParent(wheelObject.transform, false);
            disk.transform.localPosition = new Vector3(0f, 0.045f, 0f);
            CreateVisual(disk.transform, PrimitiveType.Cylinder, "Tabla", Vector3.zero,
                         new Vector3(PocketRadius * 2f + 0.06f, 0.005f, PocketRadius * 2f + 0.06f), wood);

            for (int i = 0; i < RouletteRules.PocketCount; i++)
            {
                int number = RouletteRules.WheelOrder[i];
                float angle = i * 360f / RouletteRules.PocketCount;
                float radians = angle * Mathf.Deg2Rad;
                Material material = number == 0 ? green : RouletteRules.IsRed(number) ? red : black;

                GameObject pocket = CreateVisual(disk.transform, PrimitiveType.Cube, $"Dilim{number}",
                    new Vector3(Mathf.Sin(radians) * PocketRadius, 0.008f, Mathf.Cos(radians) * PocketRadius),
                    new Vector3(0.038f, 0.008f, 0.07f), material);
                pocket.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            GameObject ball = CreateVisual(wheelObject.transform, PrimitiveType.Sphere, "Top",
                                           new Vector3(0f, 0.06f, 0.28f), Vector3.one * 0.03f, ballMaterial);

            var wheel = wheelObject.AddComponent<RouletteWheel>();
            var data = new SerializedObject(wheel);
            data.FindProperty("disk").objectReferenceValue = disk.transform;
            data.FindProperty("ball").objectReferenceValue = ball.transform;
            data.FindProperty("ballHeight").floatValue = 0.065f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---- Yardimcilar ----

        /// <summary>Collider'siz gorsel parca (masanin tek collider'i kokteki kutu + bahis alanlari).</summary>
        private static GameObject CreateVisual(Transform parent, PrimitiveType type, string objectName,
                                               Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = objectName;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

    }
}
