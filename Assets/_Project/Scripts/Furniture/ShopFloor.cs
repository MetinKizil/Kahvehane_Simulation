using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Isaret: bu objenin (ve cocuklarinin) collider'i dukkan zeminidir, uzerine esya konabilir.
    /// Ana salonun zemini NavMeshSurface'li obje oldugu icin zaten taninir (bolgesi Main); genisletme bolgelerinin
    /// zeminleri (D6b) ve bodrum (D6d-2, GamblingRoom) bunu tasir. Kilitli bolge kapali durdugu icin zemini de yoktur.
    /// Esya sadece kendi bolgesindeki zemine konur (FurnitureData.Zone).
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopFloor : MonoBehaviour
    {
        [SerializeField] private FurnitureZone zone = FurnitureZone.Main;

        public FurnitureZone Zone => zone;
    }
}
