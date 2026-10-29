using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Isaret: bu objenin (ve cocuklarinin) collider'i dukkan zeminidir, uzerine esya konabilir.
    /// Ana salonun zemini NavMeshSurface'li obje oldugu icin zaten taninir; genisletme bolgelerinin
    /// zeminleri (D6b) bunu tasir. Kilitli bolge kapali durdugu icin zemini de yoktur, oraya esya konamaz.
    /// </summary>
    [DisallowMultipleComponent]
    public class ShopFloor : MonoBehaviour
    {
    }
}
