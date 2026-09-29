using KanBagi.Core;
using KanBagi.Data;
using UnityEngine;

namespace KanBagi.Neighborhood
{
    /// <summary>
    /// Mulk satin alininca binanin gorunumunu degistirir
    /// (SATILIK tabelasi kapanir, sahiplik gorseli acilir).
    /// UpgradeUnlocker'in emlak kardesi; PropertyManager event'ine abone olur.
    ///
    /// ONEMLI: Kapatilan objelerin KENDISINDE veya altinda durmamali -
    /// obje kapaliyken bu da kapanir ve event'i duymaz.
    /// Bina kokunde veya 'Managers'ta dursun.
    /// </summary>
    public class PropertyVisualSwitch : MonoBehaviour
    {
        [Header("Mulk")]
        [SerializeField] private PropertyData requiredProperty;

        [Header("Gorseller")]
        [Tooltip("Mulk bize AIT DEGILKEN acik olacak objeler (SATILIK tabelasi vb.).")]
        [SerializeField] private GameObject[] unownedObjects;
        [Tooltip("Mulk BIZIM OLDUGUNDA acik olacak objeler (yeni tabela, isik vb.).")]
        [SerializeField] private GameObject[] ownedObjects;

        private void Start()
        {
            if (requiredProperty == null)
            {
                Debug.LogError($"[PropertyVisualSwitch] '{name}' uzerinde 'Required Property' atanmamis.", this);
                enabled = false;
                return;
            }

            if (PropertyManager.Instance == null)
            {
                Debug.LogError("[PropertyVisualSwitch] PropertyManager bulunamadi. 'Managers' objesine ekle.", this);
                enabled = false;
                return;
            }

            PropertyManager.Instance.OnPropertyPurchased += HandlePropertyPurchased;

            // Kayittan yuklenmis mulkler de dogru duruma gecsin.
            Apply(PropertyManager.Instance.IsOwned(requiredProperty));
        }

        private void HandlePropertyPurchased(PropertyData property)
        {
            if (property != requiredProperty) return;

            Apply(true);
        }

        private void Apply(bool isOwned)
        {
            SetActive(unownedObjects, !isOwned);
            SetActive(ownedObjects, isOwned);
        }

        private static void SetActive(GameObject[] targets, bool isActive)
        {
            if (targets == null) return;

            foreach (GameObject target in targets)
            {
                if (target == null) continue;

                target.SetActive(isActive);
            }
        }

        private void OnDestroy()
        {
            if (PropertyManager.Instance != null)
                PropertyManager.Instance.OnPropertyPurchased -= HandlePropertyPurchased;
        }
    }
}
