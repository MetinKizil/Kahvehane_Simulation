using System;
using KanBagi.Data;
using KanBagi.Delivery;
using UnityEngine;

namespace KanBagi.Economy
{
    /// <summary>
    /// Malzeme satin almanin TEK yeri: parayi kasadan duser, malzemeyi depoya yazar.
    /// UI sadece TryBuy cagirir; fiyat hesabi ve stok yazimi burada kalir (SOLID).
    /// </summary>
    public class PurchaseManager : MonoBehaviour
    {
        public static PurchaseManager Instance { get; private set; }

        /// <summary>Satin alma basarili olunca (malzeme, gelen adet) ile tetiklenir.</summary>
        public event Action<ItemData, int> OnPurchased;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>Kasada bu paketi alacak para var mi? Buton aktifligi icin UI sorar.</summary>
        public bool CanAfford(ShopCatalog.CatalogEntry entry)
        {
            if (entry == null || !entry.IsValid || EconomyManager.Instance == null) return false;

            return EconomyManager.Instance.CurrentMoney >= entry.PackPrice;
        }

        /// <summary>
        /// Paketi satin alir: para yeterliyse duser ve siparisi kuryeye devreder.
        /// Malzeme depoya dogrudan girmez; koli olarak kapiya birakilir (bkz. DeliveryManager).
        /// </summary>
        public bool TryBuy(ShopCatalog.CatalogEntry entry)
        {
            if (entry == null || !entry.IsValid) return false;

            if (EconomyManager.Instance == null || DeliveryManager.Instance == null)
            {
                Debug.LogError("[PurchaseManager] EconomyManager veya DeliveryManager yok. 'Managers' objesini kontrol et.", this);
                return false;
            }

            // Once teslimatin mumkun oldugunu dogruluyoruz ki para bosa gitmesin.
            if (!DeliveryManager.Instance.IsReady)
            {
                Debug.LogError("[PurchaseManager] DeliveryManager hazir degil ('Box Prefab' atanmamis olabilir).", this);
                return false;
            }

            if (!EconomyManager.Instance.TrySpendMoney(entry.PackPrice)) return false;

            DeliveryManager.Instance.QueueOrder(entry.Item, entry.UnitsPerPack);
            OnPurchased?.Invoke(entry.Item, entry.UnitsPerPack);
            return true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
