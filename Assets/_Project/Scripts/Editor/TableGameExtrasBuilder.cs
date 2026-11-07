using System.Collections.Generic;
using KanBagi.Data;
using KanBagi.Furniture;
using KanBagi.Shop;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// D8a eki:
    ///  1) Kan Bagi > Oyun Parcalarini Yenile: masa prefab'indaki okey/iskambil/domino parcalarini gercekci yapar
    ///     (eskiden masanin kosesinde tek duz kutu - "pizza kutusu" gibiydi). Okey: dort istaka + taslar + ortada
    ///     yigin; iskambil: ortada deste + yelpaze kartlar; domino: ortada tas dizisi. Parca adlari ayni kalir (TableGame).
    ///  2) Kan Bagi > Oyun Rafini Kur: takimlarin birakilip tekrar alindigi raf (6 yuva) -> prefab + ESYALAR.
    /// Parcalar masanin olceginden bagimsiz (metre) kurulur; collider'lari yok. Tekrar calistirilabilir.
    ///
    /// SADECE EDITOR.
    /// </summary>
    public static class TableGameExtrasBuilder
    {
        private const string DataFolder = "Assets/_Project/ScriptableObjects";
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string TablePrefabPath = PrefabFolder + "/Masa.prefab";
        private const string ShelfPrefabPath = PrefabFolder + "/OyunRafi.prefab";
        private const string ShelfDataPath = DataFolder + "/OyunRafiEsya.asset";
        private static readonly string[] GameAssets = { "OkeyTakimiOyun", "IskambilDestesiOyun", "DominoTakimiOyun" };

        private const int ShelfPrice = 80;
        private const int ShelfLimit = 2;

        private static readonly Color Wood = new Color(0.45f, 0.28f, 0.15f);
        private static readonly Color Ivory = new Color(0.95f, 0.92f, 0.82f);
        private static readonly Color Card = new Color(0.97f, 0.97f, 0.97f);
        private static readonly Color CardBack = new Color(0.65f, 0.1f, 0.12f);
        private static readonly Color Ink = new Color(0.08f, 0.08f, 0.08f);

        // ---- 1) Parcalar ----

        [MenuItem("Kan Bagi/Oyun Parcalarini Yenile")]
        public static void RebuildPieces()
        {
            if (EditorApplication.isPlaying) return;

            GameObject table = PrefabUtility.LoadPrefabContents(TablePrefabPath);
            try
            {
                Material wood = TableGamesBuilder.GetOrCreateMaterial("OyunAhsapMat", Wood);
                Material ivory = TableGamesBuilder.GetOrCreateMaterial("OyunFildisiMat", Ivory);
                Material card = TableGamesBuilder.GetOrCreateMaterial("OyunKartMat", Card);
                Material cardBack = TableGamesBuilder.GetOrCreateMaterial("OyunKartArkaMat", CardBack);
                Material ink = TableGamesBuilder.GetOrCreateMaterial("OyunMurekkepMat", Ink);

                BuildOkey(NewPiece(table.transform, "OkeyTakimi"), wood, ivory, ink);
                BuildCards(NewPiece(table.transform, "IskambilDestesi"), card, cardBack);
                BuildDomino(NewPiece(table.transform, "DominoTakimi"), ivory, ink);

                PrefabUtility.SaveAsPrefabAsset(table, TablePrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(table);
            }

            Debug.Log("[TableGameExtrasBuilder] Masa oyun parcalari yenilendi (okey istakalari, iskambil, domino). " +
                      "Oyunda yeni olusan masalarda gorunur.");
        }

        /// <summary>Eski parca silinir; yenisi masanin ustunde, olcegi geri alinmis (metre cinsinden) bir kok olarak kurulur.</summary>
        private static Transform NewPiece(Transform table, string pieceName)
        {
            Transform old = table.Find(pieceName);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var piece = new GameObject(pieceName).transform;
            piece.SetParent(table, false);
            Vector3 s = table.localScale;
            piece.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            piece.localPosition = new Vector3(0f, 0.5f, 0f); // kupun ust yuzu (yerel)
            piece.gameObject.SetActive(false);
            return piece;
        }

        private static void BuildOkey(Transform piece, Material wood, Material ivory, Material ink)
        {
            // Dort istaka, her kenarda bir; ustunde 7'ser tas, taslarin uzerinde kucuk sayi lekesi.
            for (int side = 0; side < 4; side++)
            {
                var rack = new GameObject($"Istaka{side + 1}").transform;
                rack.SetParent(piece, false);
                rack.localRotation = Quaternion.Euler(0f, side * 90f, 0f);
                rack.localPosition = rack.localRotation * new Vector3(0f, 0f, -0.47f);

                Part(rack, PrimitiveType.Cube, "Govde", new Vector3(0f, 0.02f, 0f), new Vector3(0.56f, 0.04f, 0.07f), wood);
                Part(rack, PrimitiveType.Cube, "Sirt", new Vector3(0f, 0.05f, -0.025f), new Vector3(0.56f, 0.03f, 0.02f), wood);

                for (int t = 0; t < 7; t++)
                {
                    float x = -0.21f + t * 0.07f;
                    Part(rack, PrimitiveType.Cube, $"Tas{t + 1}", new Vector3(x, 0.07f, -0.008f), new Vector3(0.045f, 0.06f, 0.012f), ivory);
                    Part(rack, PrimitiveType.Cube, $"Leke{t + 1}", new Vector3(x, 0.075f, 0f), new Vector3(0.012f, 0.02f, 0.002f), ink);
                }
            }

            // Ortada ters cevrilmis tas yigini (3x2 sutun, 3 kat) ve gosterge tasi.
            for (int ix = 0; ix < 3; ix++)
            {
                for (int iz = 0; iz < 2; iz++)
                {
                    for (int level = 0; level < 3; level++)
                    {
                        Part(piece, PrimitiveType.Cube, $"Yigin{ix}{iz}{level}",
                             new Vector3(-0.06f + ix * 0.05f, 0.007f + level * 0.013f, -0.035f + iz * 0.07f),
                             new Vector3(0.045f, 0.012f, 0.065f), ivory);
                    }
                }
            }

            Part(piece, PrimitiveType.Cube, "Gosterge", new Vector3(0.12f, 0.035f, 0f), new Vector3(0.045f, 0.065f, 0.012f), ivory);
            Part(piece, PrimitiveType.Cube, "GostergeLeke", new Vector3(0.12f, 0.04f, -0.007f), new Vector3(0.012f, 0.02f, 0.002f), ink);
        }

        private static void BuildCards(Transform piece, Material card, Material cardBack)
        {
            // Ortada kapali deste, yaninda acik yelpaze; her oyuncunun onunde birkac kapali kart.
            for (int i = 0; i < 6; i++)
                Part(piece, PrimitiveType.Cube, $"Deste{i}", new Vector3(-0.08f, 0.002f + i * 0.003f, 0f), new Vector3(0.065f, 0.003f, 0.095f), cardBack);

            for (int i = 0; i < 5; i++)
            {
                Transform fanned = Part(piece, PrimitiveType.Cube, $"Yelpaze{i}", new Vector3(0.06f + i * 0.012f, 0.003f + i * 0.001f, 0f),
                                        new Vector3(0.065f, 0.002f, 0.095f), card);
                fanned.localRotation = Quaternion.Euler(0f, -30f + i * 15f, 0f);
            }

            for (int side = 0; side < 4; side++)
            {
                Quaternion rotation = Quaternion.Euler(0f, side * 90f, 0f);
                for (int i = 0; i < 3; i++)
                {
                    Transform hand = Part(piece, PrimitiveType.Cube, $"El{side}{i}",
                                          rotation * new Vector3(-0.03f + i * 0.03f, 0.002f, -0.24f), new Vector3(0.065f, 0.002f, 0.095f), cardBack);
                    hand.localRotation = rotation * Quaternion.Euler(0f, -12f + i * 12f, 0f);
                }
            }
        }

        private static void BuildDomino(Transform piece, Material ivory, Material ink)
        {
            // Ortada dizilmis taslar (yatay sira + iki ucta dikey donus), her tasin ortasinda ayirici cizgi.
            var positions = new List<(Vector3 position, float yaw)>();
            for (int i = 0; i < 7; i++) positions.Add((new Vector3(-0.19f + i * 0.063f, 0f, 0f), 90f));
            positions.Add((new Vector3(-0.235f, 0f, 0.045f), 0f));
            positions.Add((new Vector3(0.235f, 0f, -0.045f), 0f));

            for (int i = 0; i < positions.Count; i++)
            {
                var tile = new GameObject($"Tas{i + 1}").transform;
                tile.SetParent(piece, false);
                tile.localPosition = positions[i].position;
                tile.localRotation = Quaternion.Euler(0f, positions[i].yaw, 0f);

                Part(tile, PrimitiveType.Cube, "Govde", new Vector3(0f, 0.007f, 0f), new Vector3(0.03f, 0.012f, 0.06f), ivory);
                Part(tile, PrimitiveType.Cube, "Cizgi", new Vector3(0f, 0.0135f, 0f), new Vector3(0.026f, 0.002f, 0.003f), ink);
                Part(tile, PrimitiveType.Cube, "Nokta1", new Vector3(0f, 0.0135f, 0.016f), new Vector3(0.006f, 0.002f, 0.006f), ink);
                Part(tile, PrimitiveType.Cube, "Nokta2", new Vector3(0f, 0.0135f, -0.016f), new Vector3(0.006f, 0.002f, 0.006f), ink);
            }
        }

        // ---- 2) Raf ----

        [MenuItem("Kan Bagi/Oyun Rafini Kur")]
        public static void BuildShelf()
        {
            if (EditorApplication.isPlaying) return;

            var database = AssetDatabase.LoadAssetAtPath<FurnitureDatabase>(DataFolder + "/EsyaVeritabani.asset");
            var catalog = AssetDatabase.LoadAssetAtPath<FurnitureCatalog>(DataFolder + "/EsyaKatalogu.asset");
            if (database == null || catalog == null)
            {
                Debug.LogError("[TableGameExtrasBuilder] EsyaVeritabani veya EsyaKatalogu bulunamadi.");
                return;
            }

            var root = new GameObject("OyunRafi");
            Material wood = TableGamesBuilder.GetOrCreateMaterial("OyunAhsapMat", Wood);

            // Kok collider rafin ARKA kismi: onune konan kutulara bakinca isin kutuya, bos yuvaya bakinca rafa carpar.
            var box = root.AddComponent<BoxCollider>();
            box.size = new Vector3(1f, 1.4f, 0.16f);
            box.center = new Vector3(0f, 0.7f, 0.12f);

            var modifier = root.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = UnityEngine.AI.NavMesh.GetAreaFromName("Not Walkable");
            modifier.applyToChildren = true;

            Part(root.transform, PrimitiveType.Cube, "YanSol", new Vector3(-0.48f, 0.7f, 0f), new Vector3(0.04f, 1.4f, 0.4f), wood);
            Part(root.transform, PrimitiveType.Cube, "YanSag", new Vector3(0.48f, 0.7f, 0f), new Vector3(0.04f, 1.4f, 0.4f), wood);
            Part(root.transform, PrimitiveType.Cube, "Arka", new Vector3(0f, 0.7f, 0.19f), new Vector3(1f, 1.4f, 0.02f), wood);
            float[] boards = { 0.03f, 0.5f, 1f, 1.38f };
            for (int i = 0; i < boards.Length; i++)
                Part(root.transform, PrimitiveType.Cube, $"Raf{i}", new Vector3(0f, boards[i], 0f), new Vector3(0.92f, 0.03f, 0.38f), wood);

            var slots = new List<Transform>();
            float[] rows = { 0.555f, 1.055f };
            float[] columns = { -0.3f, 0f, 0.3f };
            foreach (float row in rows)
            {
                foreach (float column in columns)
                {
                    var slot = new GameObject($"Yuva{slots.Count + 1}").transform;
                    slot.SetParent(root.transform, false);
                    slot.localPosition = new Vector3(column, row, -0.04f);
                    slots.Add(slot);
                }
            }

            root.AddComponent<PlacedFurniture>();
            var shelf = root.AddComponent<GameShelf>();
            var shelfData = new SerializedObject(shelf);
            SerializedProperty slotList = shelfData.FindProperty("slots");
            slotList.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++) slotList.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];

            SerializedProperty games = shelfData.FindProperty("knownGames");
            games.arraySize = 0;
            foreach (string assetName in GameAssets)
            {
                var game = AssetDatabase.LoadAssetAtPath<TableGameData>($"{DataFolder}/{assetName}.asset");
                if (game == null) continue;

                games.arraySize++;
                games.GetArrayElementAtIndex(games.arraySize - 1).objectReferenceValue = game;
            }

            shelfData.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ShelfPrefabPath);
            Object.DestroyImmediate(root);

            var data = AssetDatabase.LoadAssetAtPath<FurnitureData>(ShelfDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<FurnitureData>();
                AssetDatabase.CreateAsset(data, ShelfDataPath);
            }

            var serialized = new SerializedObject(data);
            SerializedProperty displayName = serialized.FindProperty("displayName");
            if (string.IsNullOrEmpty(displayName.stringValue)) displayName.stringValue = "Oyun Rafi";
            serialized.FindProperty("category").enumValueIndex = (int)FurnitureCategory.Station;
            serialized.FindProperty("prefab").objectReferenceValue = prefab;
            serialized.FindProperty("price").intValue = ShelfPrice;
            serialized.FindProperty("baseLimit").intValue = ShelfLimit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);

            AppendUnique(database, "furniture", data);
            AppendUnique(catalog, "items", data);
            AssetDatabase.SaveAssets();

            Debug.Log($"[TableGameExtrasBuilder] Oyun rafi kuruldu: {ShelfPrefabPath}, {slots.Count} yuva, {ShelfPrice} TL, sinir {ShelfLimit}. " +
                      "ESYALAR'dan alinir.");
        }

        // ---- Yardimcilar ----

        private static Transform Part(Transform parent, PrimitiveType type, string partName, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = partName;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            if (material != null) part.GetComponent<Renderer>().sharedMaterial = material;
            return part.transform;
        }

        private static void AppendUnique(Object owner, string listName, Object value)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty list = serialized.FindProperty(listName);
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == value) return;
            }

            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(owner);
        }
    }
}
