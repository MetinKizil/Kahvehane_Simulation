using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>Profilin sevdigi bir icecek ve ne siklikla istedigi (agirlik).</summary>
    [Serializable]
    public class DrinkPreference
    {
        [SerializeField] private ItemData beverage;
        [Tooltip("Ne kadar buyukse o kadar sik istenir.")]
        [SerializeField] private int weight = 1;

        public ItemData Beverage => beverage;
        public int Weight => weight;
    }

    /// <summary>
    /// Bir musteri tipi (emekli dayi, ogrenci, esnaf, kabadayi...): renk, sabir, zevk, bahsis.
    /// Sadece VERIDIR; musteri bunu Initialize'da alip uygular, spawner ise kimin gelecegini secer.
    /// Menu: Create > Kan Bagi > Customer Profile
    /// </summary>
    [CreateAssetMenu(fileName = "NewCustomerProfile", menuName = "Kan Bagi/Customer Profile")]
    public class CustomerProfile : ScriptableObject
    {
        [Header("Gorunum")]
        [SerializeField] private Color bodyColor = Color.white;

        [Header("Davranis")]
        [Tooltip("Temel sabir suresi bununla carpilir (okey takimi bonusu ayrica uygulanir).")]
        [SerializeField] private float patienceMultiplier = 1f;
        [Tooltip("Temel icme suresi bununla carpilir.")]
        [SerializeField] private float drinkDurationMultiplier = 1f;
        [Tooltip("Oyun meraki: masa oyunlarinin ikinci tur ihtimali bununla carpilir. 1.5 = oyun sever, 0.5 = acelesi var.")]
        [SerializeField] private float gameCuriosity = 1f;

        [Header("Zevk")]
        [Tooltip("Siparis bunlarin MENUDE OLANLARI arasindan agirlikli secilir. Hicbiri menude yoksa menuden rastgele.")]
        [SerializeField] private List<DrinkPreference> favoriteDrinks = new List<DrinkPreference>();

        [Header("Para ve Itibar")]
        [Tooltip("Servis edilince fiyatin ustune birakilan bahsis (TL).")]
        [SerializeField] private int tip;
        [Tooltip("Sabri tukenip giderse kaybedilen itibar (pozitif yazilir).")]
        [SerializeField] private int reputationOnAngryLeave = 1;

        [Header("Gelis")]
        [Tooltip("Bu tip musteri dukkana ancak itibar bu degere ulasinca gelmeye baslar.")]
        [SerializeField] private int requiredReputation;
        [Tooltip("Gelebilecek profiller arasinda secilme agirligi.")]
        [SerializeField] private int spawnWeight = 1;

        public Color BodyColor => bodyColor;
        public float PatienceMultiplier => patienceMultiplier;
        public float DrinkDurationMultiplier => drinkDurationMultiplier;
        public float GameCuriosity => gameCuriosity;
        public IReadOnlyList<DrinkPreference> FavoriteDrinks => favoriteDrinks;
        public int Tip => tip;
        public int ReputationOnAngryLeave => reputationOnAngryLeave;
        public int RequiredReputation => requiredReputation;
        public int SpawnWeight => spawnWeight;
    }
}
