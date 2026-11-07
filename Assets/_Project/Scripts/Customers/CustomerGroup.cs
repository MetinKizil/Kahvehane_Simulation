using System;
using System.Collections.Generic;

namespace KanBagi.Customers
{
    /// <summary>
    /// Birlikte gelen musteriler (D7b, 1-4 kisi): ayni masaya oturur, herkes kendi siparisini verir,
    /// hepsi bitirince birlikte kalkar. Sabri biten uye tek basina gider; grup onu "bitti" sayar.
    /// Sadece "kim bitirdi" bilgisini tutar; yurume, siparis, para musterinin kendi isi (SOLID).
    /// Dunya NPC'si; oyuncuya ozel bir sey tutmaz (co-op uyumlu).
    /// </summary>
    public class CustomerGroup
    {
        private readonly List<Customer> members = new List<Customer>();
        private readonly HashSet<Customer> done = new HashSet<Customer>();

        public IReadOnlyList<Customer> Members => members;

        /// <summary>Herkes bitirince (veya gidince) bir kez tetiklenir. Bekleyen uyeler kalkar.</summary>
        public event Action OnAllDone;

        public bool IsAllDone { get; private set; }

        public void Add(Customer customer)
        {
            if (customer != null && !members.Contains(customer)) members.Add(customer);
        }

        /// <summary>Uye isini bitirdi (icti ve masada bekliyor, kizip gitti veya dukkan kapandi).</summary>
        public void MarkDone(Customer customer)
        {
            if (IsAllDone || customer == null || !done.Add(customer)) return;
            if (done.Count < members.Count) return;

            IsAllDone = true;
            OnAllDone?.Invoke();
        }
    }
}
