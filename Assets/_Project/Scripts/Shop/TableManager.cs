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

        /// <summary>Bos bir masa bulup musteriye ayirir. Bos masa yoksa false doner.</summary>
        public bool TryReserveFreeTable(Customer customer, out Table reservedTable)
        {
            foreach (Table table in tables)
            {
                if (!table.IsFree) continue;
                if (!table.TryReserve(customer)) continue;

                reservedTable = table;
                return true;
            }

            reservedTable = null;
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
