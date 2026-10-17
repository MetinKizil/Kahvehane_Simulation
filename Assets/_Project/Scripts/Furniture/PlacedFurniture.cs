using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Sahneye yerlestirilmis bir esyanin kimligi: "ben hangi esyayim".
    /// Esya prefab'inin KOKUNDE durur. Konumu/acisi transform'dadir; kaydi FurnitureManager yapar.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlacedFurniture : MonoBehaviour
    {
        [Tooltip("Bu esyanin tanimi. FurnitureManager olustururken atar.")]
        [SerializeField] private FurnitureData data;

        public FurnitureData Data => data;

        /// <summary>Y ekseni etrafindaki aci (derece). Kayit bunu saklar.</summary>
        public float Yaw => transform.eulerAngles.y;

        /// <summary>FurnitureManager esyayi olusturunca cagirir.</summary>
        public void Initialize(FurnitureData furnitureData) => data = furnitureData;
    }
}
