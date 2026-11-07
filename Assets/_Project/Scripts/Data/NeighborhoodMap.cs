using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>Haritadaki bir parcanin turu (10d-1a). Yukseklik, renk ve davranis buna gore secilir.</summary>
    public enum MapBlockType
    {
        Ev,
        Sus,      // dekor dukkan/bina (hastane, veteriner...)
        Mulk,     // satin alinabilecek dukkan (10d-1b'de hepsi acilacak)
        Dukkan,   // bizim kiraathane: bina kurulmaz, sadece yeri isaretlenir
        Bahce,    // bizim bahce: bina kurulmaz
        Park,
        Avlu,     // ada icindeki kapali arka bahceler (alcak duvar)
        Amca,     // Amcanin Yeri (final)
        Disari,   // "Oyun Disi": koyu arazi, gorunmez duvar
        Giris     // musteri giris noktasi (col/row ondalikli olabilir)
    }

    /// <summary>Binanin sokaga bakan yuzu, CIZIMDEKI yonle (K = cizimin ustu).</summary>
    public enum MapFront { K, G, D, B }

    [Serializable]
    public class MapBlock
    {
        public string name;
        public MapBlockType type;
        [Tooltip("Cizimdeki kare koordinati: sutun (soldan), satir (ustten). 1 kare = 5 m.")]
        public float col;
        public float row;
        public float width = 1;
        public float height = 1;
        public MapFront front;
        public Color color = Color.gray;
        [Tooltip("Mulk ise baglanan PropertyData asset adi (bos olabilir). Giris ise tercih eden profil adi.")]
        public string link;
    }

    /// <summary>
    /// Mahalle haritasi (10d-1a): kullanicinin kagida cizdigi plan, kare kare.
    /// Kan Bagi > Mahalleyi Kur bu listeden sokaklari, binalari ve giris noktalarini kurar.
    /// Bir blogu tasimak icin sadece buradaki sayilari degistirip builder'i tekrar calistir.
    /// </summary>
    [CreateAssetMenu(fileName = "MahalleHaritasi", menuName = "Kan Bagi/Neighborhood Map")]
    public class NeighborhoodMap : ScriptableObject
    {
        [Tooltip("Bir karenin metre karsiligi.")]
        [SerializeField] private float cellSize = 5f;
        [Tooltip("Cizimin toplam boyu (kare): sutun x satir.")]
        [SerializeField] private Vector2Int gridSize = new Vector2Int(39, 51);
        [SerializeField] private List<MapBlock> blocks = new List<MapBlock>();

        public float CellSize => cellSize;
        public Vector2Int GridSize => gridSize;
        public List<MapBlock> Blocks => blocks;
    }
}
