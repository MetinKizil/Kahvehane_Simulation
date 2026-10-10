using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KanBagi.Core;
using KanBagi.Customers;
using KanBagi.Delivery;
using KanBagi.Economy;
using KanBagi.Illegal;
using KanBagi.Inventory;
using KanBagi.Shop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KanBagi.EditorTools
{
    /// <summary>
    /// Sahnedeki manager'lara tek tikla "gercek oyun" veya "test" ayarlarini yazar.
    /// Inspector'da tek tek alan aramaya gerek kalmaz; hangi degerin degistigi Console'a dokulur.
    ///
    /// Ayrica:
    ///  - Ayni singleton'dan birden fazla varsa (orn. iki DayCycleManager) fazlasini siler
    ///  - Managers objesinde BalanceLogger yoksa ekler
    ///
    /// Degerleri degistirmek icin asagidaki Settings tablosunu duzenleyip menuye tekrar tikla.
    /// SADECE EDITOR.
    /// Menu: Kan Bagi > Denge > Gercek Degerleri Uygula
    ///       Kan Bagi > Denge > Test Degerlerini Uygula
    /// </summary>
    public static class BalancePresetApplier
    {
        // SaveManager'daki varsayilan dosya adiyla ayni olmali.
        private const string SaveFileName = "kanbagi_save.json";

        /// <summary>Tek bir Inspector alaninin gercek ve test degeri.</summary>
        private readonly struct Setting
        {
            public readonly Type Component;
            public readonly string Property;
            public readonly float Real;
            public readonly float Test;

            public Setting(Type component, string property, float real, float test)
            {
                Component = component;
                Property = property;
                Real = real;
                Test = test;
            }
        }

        // ---- DENGE TABLOSU (bilesen, alan, GERCEK, TEST) ----
        private static readonly Setting[] Settings =
        {
            new Setting(typeof(EconomyManager), "startingMoney", 250f, 10000f),
            new Setting(typeof(ReputationManager), "startingReputation", 0f, 300f),

            new Setting(typeof(DayCycleManager), "dayDurationMinutes", 6f, 1f),
            new Setting(typeof(DayCycleManager), "nightTimeScale", 4f, 4f),

            new Setting(typeof(DeliveryManager), "deliveryDelay", 20f, 8f),

            new Setting(typeof(CustomerSpawner), "firstSpawnDelay", 3f, 2f),
            new Setting(typeof(CustomerSpawner), "spawnDelayRange.x", 6f, 4f),
            new Setting(typeof(CustomerSpawner), "spawnDelayRange.y", 14f, 8f),
            new Setting(typeof(CustomerSpawner), "maxActiveCustomers", 4f, 4f),

            new Setting(typeof(PoliceManager), "minimumSuspicionRatio", 0.2f, 0f),
            new Setting(typeof(PoliceManager), "maxRaidChance", 0.6f, 1f),
            new Setting(typeof(PoliceManager), "warningSeconds", 20f, 20f),
            new Setting(typeof(PoliceManager), "inspectionSeconds", 12f, 3f),
            new Setting(typeof(PoliceManager), "earliestRaidDay", 4f, 1f),
            new Setting(typeof(PoliceManager), "fineAmount", 1000f, 1000f),
            new Setting(typeof(PoliceManager), "protectedMoney", 150f, 150f),

            new Setting(typeof(MafiaManager), "requiredReputation", 120f, 120f),
            new Setting(typeof(MafiaManager), "requiredMoney", 1000f, 1000f),
            new Setting(typeof(MafiaManager), "baseDemand", 300f, 300f),
        };

        // Sahnede TEK olmasi gereken manager'lar. Fazlasi Awake'te kendini yok etse de
        // hangisinin kalacagi belirsizdir; Inspector'da yanlis kopyayi duzenlemek kolaydir.
        private static readonly Type[] SingletonTypes =
        {
            typeof(EconomyManager), typeof(ReputationManager), typeof(DayCycleManager),
            typeof(DailyStatsManager), typeof(SaveManager), typeof(UpgradeManager), typeof(PropertyManager),
            typeof(DeliveryManager), typeof(PurchaseManager), typeof(InventoryManager),
            typeof(MenuManager), typeof(TableManager), typeof(CustomerModifiers),
            typeof(SuspicionManager), typeof(MafiaManager), typeof(PoliceManager),
            typeof(BalanceLogger)
        };

        [MenuItem("Kan Bagi/Denge/Gercek Degerleri Uygula")]
        public static void ApplyReal() => Apply(useTestValues: false);

        [MenuItem("Kan Bagi/Denge/Test Degerlerini Uygula")]
        public static void ApplyTest() => Apply(useTestValues: true);

        private static void Apply(bool useTestValues)
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[BalancePresetApplier] Play modundayken uygulanamaz (degisiklikler kaybolur). Once Play'i durdur.");
                return;
            }

            string presetName = useTestValues ? "TEST" : "GERCEK";
            var report = new StringBuilder($"[BalancePresetApplier] {presetName} degerleri uygulandi:\n");

            RemoveDuplicateSingletons(report);
            EnsureBalanceLogger(report);

            int changed = 0;
            foreach (Setting setting in Settings)
                changed += ApplySetting(setting, useTestValues, report);

            report.AppendLine($"Toplam {changed} alan degisti. Sahneyi kaydet (File > Save).");

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log(report.ToString());

            string savePath = Path.Combine(Application.persistentDataPath, SaveFileName);
            if (File.Exists(savePath))
            {
                Debug.LogWarning("[BalancePresetApplier] Diskte eski bir KAYIT var. Kayit yuklenince baslangic parasi/itibari " +
                                 "ezilir - sifirdan test icin 'Kan Bagi > Kaydi Sil' calistir.");
            }
        }

        /// <summary>Ayarin hedef bilesenlerinin hepsine (pasifler dahil) degeri yazar. Degisen alan sayisini doner.</summary>
        private static int ApplySetting(Setting setting, bool useTestValues, StringBuilder report)
        {
            Object[] targets = Object.FindObjectsByType(setting.Component, FindObjectsInactive.Include, FindObjectsSortMode.None);

            if (targets.Length == 0)
            {
                report.AppendLine($"  ! {setting.Component.Name} sahnede yok, '{setting.Property}' atlandi");
                return 0;
            }

            float value = useTestValues ? setting.Test : setting.Real;
            int changed = 0;

            foreach (Object target in targets)
            {
                var serialized = new SerializedObject(target);
                SerializedProperty property = serialized.FindProperty(setting.Property);

                if (property == null)
                {
                    report.AppendLine($"  ! {setting.Component.Name}.{setting.Property} alani bulunamadi (adi degismis olabilir)");
                    continue;
                }

                string oldValue = ReadValue(property);
                if (!WriteValue(property, value))
                {
                    report.AppendLine($"  ! {setting.Component.Name}.{setting.Property} desteklenmeyen tur: {property.propertyType}");
                    continue;
                }

                string newValue = ReadValue(property);
                if (oldValue == newValue) continue;

                // ApplyModifiedProperties Undo kaydini da kendisi tutar.
                serialized.ApplyModifiedProperties();
                changed++;

                report.AppendLine($"  {GetOwnerName(target)} > {setting.Component.Name}.{setting.Property}: {oldValue} -> {newValue}");
            }

            return changed;
        }

        private static bool WriteValue(SerializedProperty property, float value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = Mathf.RoundToInt(value);
                    return true;
                case SerializedPropertyType.Float:
                    property.floatValue = value;
                    return true;
                case SerializedPropertyType.Boolean:
                    property.boolValue = value > 0.5f;
                    return true;
                default:
                    return false;
            }
        }

        private static string ReadValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return property.intValue.ToString();
                case SerializedPropertyType.Float: return property.floatValue.ToString("0.###");
                case SerializedPropertyType.Boolean: return property.boolValue.ToString();
                default: return "?";
            }
        }

        /// <summary>
        /// Ayni manager'dan birden fazla varsa ilk objenin ILK bilesenini tutar, gerisini siler (Undo destekli).
        /// </summary>
        private static void RemoveDuplicateSingletons(StringBuilder report)
        {
            foreach (Type type in SingletonTypes)
            {
                Component[] all = Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None)
                                        .OfType<Component>()
                                        .ToArray();

                if (all.Length <= 1) continue;

                Component keeper = all[0].gameObject.GetComponents(type).FirstOrDefault() ?? all[0];
                var removed = new List<string>();

                foreach (Component duplicate in all)
                {
                    if (duplicate == null || duplicate == keeper) continue;

                    removed.Add(GetOwnerName(duplicate));
                    Undo.DestroyObjectImmediate(duplicate);
                }

                report.AppendLine($"  X {type.Name}: {removed.Count} fazla kopya silindi ({string.Join(", ", removed)}), " +
                                  $"'{GetOwnerName(keeper)}' uzerindeki ilk kopya kaldi");
            }
        }

        /// <summary>BalanceLogger'i diger manager'larin yanina (EconomyManager'in objesine) ekler.</summary>
        private static void EnsureBalanceLogger(StringBuilder report)
        {
            if (Object.FindFirstObjectByType<BalanceLogger>(FindObjectsInactive.Include) != null) return;

            var economy = Object.FindFirstObjectByType<EconomyManager>(FindObjectsInactive.Include);
            if (economy == null)
            {
                report.AppendLine("  ! EconomyManager bulunamadi; BalanceLogger eklenemedi (Managers objesine elle ekle)");
                return;
            }

            Undo.AddComponent<BalanceLogger>(economy.gameObject);
            report.AppendLine($"  + BalanceLogger '{economy.gameObject.name}' objesine eklendi");
        }

        private static string GetOwnerName(Object target)
        {
            return target is Component component ? component.gameObject.name : target.name;
        }
    }
}
