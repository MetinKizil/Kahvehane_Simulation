using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Esya konamayan alan (bodrum merdiveni, D6c). Collider'i yok: uzerinden yurumeyi veya E isinini engellemez,
    /// sadece FurniturePlacer hayaleti buraya degince kirmizi yapar. Alan bu objenin konumunda, 'size' boyutunda bir kutudur.
    /// Kumar Odasi alinmadan once de gecerlidir (merdivenin yeri bos kalsin, alininca ustunde esya kalmasin).
    /// </summary>
    public class ReservedArea : MonoBehaviour
    {
        private static readonly List<ReservedArea> active = new List<ReservedArea>();

        [Tooltip("Alanin dunya boyutu (metre). Y yuksekligi esyanin ust/alt sinirina kadar kapsamali.")]
        [SerializeField] private Vector3 size = new Vector3(1.5f, 3f, 5f);

        private Bounds Area => new Bounds(transform.position, size);

        private void OnEnable() => active.Add(this);

        private void OnDisable() => active.Remove(this);

        /// <summary>Verilen dunya kutusu (eksen hizali) herhangi bir yasak alana degiyor mu?</summary>
        public static bool AnyOverlaps(Bounds bounds)
        {
            foreach (ReservedArea area in active)
            {
                if (area != null && area.Area.Intersects(bounds)) return true;
            }

            return false;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.5f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
