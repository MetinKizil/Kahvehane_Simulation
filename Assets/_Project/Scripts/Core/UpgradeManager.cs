using System;
using System.Collections.Generic;
using KanBagi.Data;
using KanBagi.Economy;
using UnityEngine;

namespace KanBagi.Core
{
    /// <summary>
    /// Satin alinan gelistirmelerin TEK sahibi. Parayi duser ve "alindi" der;
    /// upgrade'in NE yaptigini bilmez (SOLID) - etkiyi dinleyiciler uretir.
    /// Yeni masa, kahve makinesi, ileride kumarhane odasi... hepsi OnUpgradePurchased'a abone olur.
    /// </summary>
    public class UpgradeManager : MonoBehaviour, ISaveable
    {
        /// <summary>
        /// Kayit dosyasindaki gelistirme bolumu.
        /// Gelistirmeler ASSET ADIYLA saklanir; yuklerken katalogdan geri bulunur.
        /// </summary>
        [Serializable]
        private class UpgradeSaveData
        {
            public List<string> purchased = new List<string>();
        }

        public static UpgradeManager Instance { get; private set; }

        [Header("Kayit")]
        [Tooltip("Kayit yuklenirken gelistirmeleri adindan bulmak icin gerekli.")]
        [SerializeField] private UpgradeCatalog catalog;

        private readonly HashSet<UpgradeData> purchased = new HashSet<UpgradeData>();

        /// <summary>Bir gelistirme satin alindiginda tetiklenir.</summary>
        public event Action<UpgradeData> OnUpgradePurchased;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        public bool IsPurchased(UpgradeData upgrade) => upgrade != null && purchased.Contains(upgrade);

        /// <summary>Itibar baraji asildi mi? (0 ise sart yok.)</summary>
        public bool MeetsReputation(UpgradeData upgrade)
        {
            if (upgrade == null) return false;
            if (upgrade.RequiredReputation <= 0) return true;

            int reputation = ReputationManager.Instance != null ? ReputationManager.Instance.CurrentReputation : 0;
            return reputation >= upgrade.RequiredReputation;
        }

        /// <summary>Once alinmasi gereken ama henuz alinmamis ilk gelistirme; hepsi alinmissa null.</summary>
        public UpgradeData GetMissingPrerequisite(UpgradeData upgrade)
        {
            if (upgrade == null) return null;

            foreach (UpgradeData required in upgrade.RequiredUpgrades)
            {
                if (required != null && !IsPurchased(required)) return required;
            }

            return null;
        }

        /// <summary>Alinmis gelistirmelerin bu esyanin sinirina ekledigi toplam (FurnitureManager.GetLimit sorar).</summary>
        public int GetLimitBonus(FurnitureData furniture)
        {
            if (furniture == null) return 0;

            int bonus = 0;
            foreach (UpgradeData upgrade in purchased)
            {
                foreach (UpgradeData.LimitBonus limitBonus in upgrade.LimitBonuses)
                {
                    if (limitBonus != null && limitBonus.Furniture == furniture) bonus += limitBonus.Amount;
                }
            }

            return bonus;
        }

        /// <summary>Kasada bu gelistirmeyi alacak para, yeterli itibar ve onkosullar var mi? Buton aktifligi icin UI sorar.</summary>
        public bool CanAfford(UpgradeData upgrade)
        {
            if (upgrade == null || !upgrade.IsValid || IsPurchased(upgrade)) return false;
            if (!MeetsReputation(upgrade)) return false;
            if (GetMissingPrerequisite(upgrade) != null) return false;
            if (EconomyManager.Instance == null) return false;

            return EconomyManager.Instance.CurrentMoney >= upgrade.Cost;
        }

        /// <summary>Gelistirmeyi satin alir. Zaten alinmissa, itibar yetmezse veya para yetmezse false doner.</summary>
        public bool TryPurchase(UpgradeData upgrade)
        {
            if (upgrade == null || !upgrade.IsValid || IsPurchased(upgrade)) return false;
            if (!MeetsReputation(upgrade)) return false;
            if (GetMissingPrerequisite(upgrade) != null) return false;

            if (EconomyManager.Instance == null)
            {
                Debug.LogError("[UpgradeManager] EconomyManager bulunamadi. 'Managers' objesini kontrol et.", this);
                return false;
            }

            if (!EconomyManager.Instance.TrySpendMoney(upgrade.Cost)) return false;

            purchased.Add(upgrade);
            OnUpgradePurchased?.Invoke(upgrade);
            return true;
        }

        public string SaveKey => "upgrades";

        public string CaptureState()
        {
            var data = new UpgradeSaveData();

            foreach (UpgradeData upgrade in purchased)
            {
                if (upgrade != null) data.purchased.Add(upgrade.name);
            }

            return JsonUtility.ToJson(data);
        }

        public void RestoreState(string state)
        {
            var data = JsonUtility.FromJson<UpgradeSaveData>(state);
            if (data == null || data.purchased == null) return;

            if (catalog == null)
            {
                Debug.LogError("[UpgradeManager] 'Catalog' atanmamis; gelistirmeler yuklenemiyor.", this);
                return;
            }

            purchased.Clear();

            foreach (string upgradeName in data.purchased)
            {
                UpgradeData upgrade = FindInCatalog(upgradeName);

                if (upgrade == null)
                {
                    Debug.LogWarning($"[UpgradeManager] Kayittaki '{upgradeName}' gelistirmesi katalogda yok, atlandi.", this);
                    continue;
                }

                purchased.Add(upgrade);
            }

            // KRITIK: alinmis gelistirmelerin etkileri yeniden duyurulur.
            // Yoksa kahve makinesi kilitli kalir, menu buyumez, hiz upgrade'i uygulanmaz.
            foreach (UpgradeData upgrade in purchased)
                OnUpgradePurchased?.Invoke(upgrade);
        }

        private UpgradeData FindInCatalog(string upgradeName)
        {
            if (string.IsNullOrEmpty(upgradeName)) return null;

            foreach (UpgradeData upgrade in catalog.Upgrades)
            {
                if (upgrade != null && upgrade.name == upgradeName) return upgrade;
            }

            return null;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
