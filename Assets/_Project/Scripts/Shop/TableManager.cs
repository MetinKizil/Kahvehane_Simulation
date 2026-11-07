using System.Collections.Generic;
using KanBagi.Customers;
using UnityEngine;

namespace KanBagi.Shop
{
    /// <summary>
    /// Dukkandaki masalarin kaydini tutar ve bos masa bulma servisi sunar.
    /// Musteriler sahneyi tarayarak masa aramaz; buraya sorar (SOLID + performans).
    /// </summary>
    [DefaultExecutionOrder(-100)] // Masalarin OnEnable'indan once hazir olmali.
    public class TableManager : MonoBehaviour
    {
        public static TableManager Instance { get; private set; }

        private readonly List<Table> tables = new List<Table>();

        public int TableCount => tables.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            // Sahnede zaten var olan masalari topla (kendi OnEnable'lari kacirilmis olabilir).
            foreach (Table table in FindObjectsByType<Table>(FindObjectsSortMode.None))
                Register(table);
        }

        public void Register(Table table)
        {
            if (table == null || tables.Contains(table)) return;

            tables.Add(table);
        }

        public void Unregister(Table table) => tables.Remove(table);

        /// <summary>Bos bir masa bulup musteriye bir sandalye ayirir. Bos masa yoksa false doner.</summary>
        public bool TryReserveFreeTable(Customer customer, out Table reservedTable, out int seat)
        {
            foreach (Table table in tables)
            {
                if (!table.IsFree) continue;
                if (!table.TryReserve(customer, out seat)) continue;

                reservedTable = table;
                return true;
            }

            reservedTable = null;
            seat = -1;
            return false;
        }

        /// <summary>Bu kadar kisilik bir grubun oturabilecegi bos masa var mi? (Spawner grubu olusturmadan once sorar.)</summary>
        public bool HasTableFor(int groupSize)
        {
            foreach (Table table in tables)
            {
                if (table.IsFree && table.UsableSeatCount >= groupSize) return true;
            }

            return false;
        }

        /// <summary>Gruba uygun ilk bos masayi bulup her uyeye bir sandalye ayirir.</summary>
        public bool TryReserveTableForGroup(IReadOnlyList<Customer> members, out Table reservedTable, out int[] seats)
        {
            foreach (Table table in tables)
            {
                if (!table.IsFree) continue;
                if (!table.TryReserveGroup(members, out seats)) continue;

                reservedTable = table;
                return true;
            }

            reservedTable = null;
            seats = null;
            return false;
        }

        /// <summary>Su anda bos masa var mi? Spawner musteri gondermeden once sorar.</summary>
        public bool HasFreeTable()
        {
            foreach (Table table in tables)
                if (table.IsFree) return true;

            return false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
