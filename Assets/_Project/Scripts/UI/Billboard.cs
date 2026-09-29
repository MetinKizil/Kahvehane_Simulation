using UnityEngine;

namespace KanBagi.UI
{
    /// <summary>
    /// Objeyi her zaman kameraya dondurur (siparis balonu, isim etiketi vb.).
    /// Tek isi budur (SOLID); ne gosterdigini bilmez.
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        private Camera targetCamera;

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null) return;
            }

            // Kameradan objeye bakan yon: yazi ters gorunmez.
            transform.rotation = Quaternion.LookRotation(transform.position - targetCamera.transform.position);
        }
    }
}
