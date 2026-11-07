using KanBagi.Data;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Items
{
    /// <summary>
    /// Elde tasinan oyun takimi kutusu (D8a). Masaya bakip E ile konur (Table), masadan bos elle geri alinir.
    /// Kendi basina bir sey yapmaz: hangi takim oldugunu soyler. HoldableItem ile ayni objede durur.
    /// </summary>
    [RequireComponent(typeof(HoldableItem))]
    public class GameSetItem : MonoBehaviour
    {
        [SerializeField] private TableGameData game;

        public TableGameData Game => game;
    }
}
