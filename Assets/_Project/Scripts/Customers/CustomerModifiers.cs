using System;
using UnityEngine;

namespace KanBagi.Customers
{
    /// <summary>
    /// Musteri statlarini etkileyen carpanlarin TEK sahibi (sabir, ileride bahsis vb.).
    /// Musteri kendi sabrini burada sorar; kimin neden artirdigini bilmez (SOLID).
    /// Carpanlar CARPILARAK birikir: iki ayri upgrade ust uste etki eder.
    /// </summary>
    public class CustomerModifiers : MonoBehaviour
    {
        public static CustomerModifiers Instance { get; private set; }

        /// <summary>Sabir carpani (1 = normal, 1.5 = %50 daha uzun bekler).</summary>
        public float PatienceMultiplier { get; private set; } = 1f;

        /// <summary>Herhangi bir carpan degistiginde tetiklenir.</summary>
        public event Action OnModifiersChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        /// <summary>Sabir carpanini buyutur (masa oyunlari, ikram vb.).</summary>
        public void MultiplyPatience(float multiplier)
        {
            if (multiplier <= 0f) return;

            PatienceMultiplier *= multiplier;
            OnModifiersChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
