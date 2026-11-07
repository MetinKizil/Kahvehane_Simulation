using System;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using KanBagi.Shop;

namespace KanBagi.Customers
{
    /// <summary>
    /// Birlikte gelen musteriler (D7b, 1-4 kisi): ayni masaya oturur, herkes kendi siparisini verir,
    /// hepsi bitirince birlikte kalkar. Sabri biten uye tek basina gider; grup onu "bitti" sayar.
    /// D8b: grup bir masa oyunu isteyebilir (masanin ortasinda balon). Istenen takim masaya konunca karsilanir:
    /// grup bir kez masa parasi oder, her uye itibar kazandirir. Getirilmezse ve uye siparisini de alamadan
    /// kizip giderse ekstra itibar cezasi (Customer.LosePatience sorar).
    /// Sadece grup bilgisini tutar; yurume, siparis, para musterinin kendi isi (SOLID). Dunya NPC'si (co-op uyumlu).
    /// </summary>
    public class CustomerGroup
    {
        private readonly List<Customer> members = new List<Customer>();
        private readonly HashSet<Customer> done = new HashSet<Customer>();

        private Table table;
        private int tablePayment;
        private int reputationPerPerson;

        public IReadOnlyList<Customer> Members => members;

        /// <summary>Herkes bitirince (veya gidince) bir kez tetiklenir. Bekleyen uyeler kalkar.</summary>
        public event Action OnAllDone;

        public bool IsAllDone { get; private set; }

        /// <summary>Grubun istedigi oyun (yoksa null).</summary>
        public TableGameData RequestedGame { get; private set; }

        public bool IsGameDelivered { get; private set; }

        /// <summary>Istek getirilmediyse ve uye siparisini alamadan giderse ekstra ceza (kisi basi).</summary>
        public int GameFailPenalty => RequestedGame != null && !IsGameDelivered ? reputationPerPerson : 0;

        /// <summary>Istek karsilandi ve grup oyunun "tam" sayisindaysa (okeyde 4) ikinci tur bonusu.</summary>
        public float SecondRoundBonus =>
            IsGameDelivered && RequestedGame.FullGroupSize > 0 && members.Count >= RequestedGame.FullGroupSize
                ? RequestedGame.FullGroupSecondRoundBonus
                : 0f;

        public void Add(Customer customer)
        {
            if (customer != null && !members.Contains(customer)) members.Add(customer);
        }

        /// <summary>Spawner masayi ayirinca cagirir. Istek varsa masada balon belirir; takim konunca karsilanir.</summary>
        public void SetGameRequest(Table groupTable, TableGameData requested, int payment, int reputation)
        {
            table = groupTable;
            if (requested == null || table == null || table.Game == null) return;

            RequestedGame = requested;
            tablePayment = payment;
            reputationPerPerson = reputation;

            table.SetGameRequest(requested);
            table.Game.OnGameChanged += TryDeliver;
        }

        /// <summary>
        /// Istenen takim masada ve en az bir uye oturmussa istek karsilanir (bir kez). Uye oturunca ve takim
        /// degisince cagrilir: takim onceden konmussa grup oturur oturmaz karsilanir.
        /// </summary>
        public void TryDeliver()
        {
            if (IsGameDelivered || RequestedGame == null || table == null || table.Game == null) return;
            if (table.Game.CurrentGame != RequestedGame || !IsAnyoneSeated()) return;

            IsGameDelivered = true;
            table.SetGameRequest(null);

            // Masa parasi bir kez (gunluk ozetin cirosuna girsin diye bir uyenin odemesine eklenir), itibar kisi basi.
            if (members.Count > 0) members[0].AddExtraPayment(tablePayment);
            if (ReputationManager.Instance != null) ReputationManager.Instance.AddReputation(reputationPerPerson * members.Count);
        }

        private bool IsAnyoneSeated()
        {
            foreach (Customer member in members)
            {
                if (member != null && member.IsSeated) return true;
            }

            return false;
        }

        /// <summary>Uye isini bitirdi (icti ve masada bekliyor, kizip gitti veya dukkan kapandi).</summary>
        public void MarkDone(Customer customer)
        {
            if (IsAllDone || customer == null || !done.Add(customer)) return;
            if (done.Count < members.Count) return;

            IsAllDone = true;
            ClearRequest();
            OnAllDone?.Invoke();
        }

        private void ClearRequest()
        {
            if (table == null) return;

            if (table.Game != null) table.Game.OnGameChanged -= TryDeliver;
            if (table.RequestedGame == RequestedGame) table.SetGameRequest(null);
        }
    }
}
