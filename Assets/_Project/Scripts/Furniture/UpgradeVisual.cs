using System;
using System.Collections.Generic;
using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Furniture
{
    /// <summary>
    /// Bir esyanin uzerindeki, upgrade alininca beliren parcalari acar (masadaki okey, iskambil, domino).
    /// UpgradeUnlocker'in esya ici karsiligi: sahnedeki sabit objeler yerine HER esya kendi parcalarini acar,
    /// boylece oyun sirasinda olusturulan masalarda da gorunurler.
    ///
    /// Esyanin KOKUNDE durur (kok hep acik); acilip kapanan sey cocuk parcalardir.
    /// </summary>
    public class UpgradeVisual : MonoBehaviour
    {
        [Serializable]
        private class Entry
        {
            [SerializeField] private UpgradeData upgrade;
            [SerializeField] private GameObject target;

            public UpgradeData Upgrade => upgrade;
            public GameObject Target => target;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private void Start()
        {
            UpgradeManager upgrades = UpgradeManager.Instance;
            if (upgrades == null)
            {
                Debug.LogError("[UpgradeVisual] UpgradeManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            upgrades.OnUpgradePurchased += HandleUpgradePurchased;

            // Sonradan olusturulan esya: alinmis upgrade'lerin parcalari hemen gorunsun.
            foreach (Entry entry in entries)
            {
                if (entry.Target != null) entry.Target.SetActive(upgrades.IsPurchased(entry.Upgrade));
            }
        }

        private void HandleUpgradePurchased(UpgradeData upgrade)
        {
            foreach (Entry entry in entries)
            {
                if (entry.Upgrade == upgrade && entry.Target != null) entry.Target.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradePurchased -= HandleUpgradePurchased;
        }
    }
}
