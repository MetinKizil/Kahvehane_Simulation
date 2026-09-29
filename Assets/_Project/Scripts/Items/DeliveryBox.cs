using KanBagi.Data;
using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Items
{
    /// <summary>
    /// Kuryenin biraktigi koli. SADECE icinde ne oldugunu bilir (SOLID);
    /// kimin siparis ettigini, nereye bosaltilacagini bilmez.
    /// Icerik teslimat aninda DeliveryManager tarafindan doldurulur.
    /// </summary>
    [RequireComponent(typeof(HoldableItem))]
    public class DeliveryBox : MonoBehaviour
    {
        [Header("Icerik")]
        [Tooltip("Kutunun icindeki malzeme. Oyun icinde DeliveryManager doldurur.")]
        [SerializeField] private ItemData content;
        [SerializeField] private int amount;

        [Header("Gorunum")]
        [Tooltip("Elde tutulurken gorunecek isim. {0} malzeme adi, {1} adet.")]
        [SerializeField] private string labelFormat = "{0} Kolisi ({1})";

        public ItemData Content => content;
        public int Amount => amount;
        public bool IsEmpty => content == null || amount <= 0;

        /// <summary>Koliyi doldurur ve elde gorunecek ismini gunceller.</summary>
        public void Fill(ItemData item, int count)
        {
            content = item;
            amount = Mathf.Max(0, count);

            if (TryGetComponent(out HoldableItem holdable))
                holdable.SetDisplayName(GetLabel());
        }

        /// <summary>UI'da gostermek icin okunabilir icerik metni.</summary>
        public string GetLabel() => IsEmpty ? "Bos Koli" : string.Format(labelFormat, content.ItemName, amount);
    }
}
