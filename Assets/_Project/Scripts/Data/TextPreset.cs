using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>Bir bilesenin bir metin alanina yazilacak deger. Orn: PoliceNotificationUI.warningMessage</summary>
    [Serializable]
    public class FieldText
    {
        [Tooltip("Bilesen sinifinin adi (namespace'siz). Orn: PoliceNotificationUI")]
        [SerializeField] private string component;
        [Tooltip("Inspector'daki alanin kod adi. Orn: warningMessage")]
        [SerializeField] private string field;
        [Tooltip("Bos = bu bilesenin HER kopyasi. Dolu = sadece hiyerarsi yolu bununla biten obje " +
                 "(orn. 'MalzemeSekmeButton/Text (TMP)'). Sabit yazilar (TextMeshProUGUI.m_text) icin gerekli.")]
        [SerializeField] private string objectPath;
        [SerializeField] [TextArea] private string text;

        public string Component => component;
        public string Field => field;
        public string ObjectPath => objectPath;
        public string Text => text;
    }

    /// <summary>
    /// Arayuz metinlerinin TEK kaynagi: sahne ve prefab'lardaki metin alanlarina ne yazilacagi.
    /// Sadece VERIDIR; yazma isini 'Kan Bagi > Metinleri Uygula' (TextPresetApplier) yapar.
    /// Turkce karakterli metin burada durur, kodda degil (kod ASCII kurali).
    /// Menu: Create > Kan Bagi > Text Preset
    /// </summary>
    [CreateAssetMenu(fileName = "NewTextPreset", menuName = "Kan Bagi/Text Preset")]
    public class TextPreset : ScriptableObject
    {
        [SerializeField] private List<FieldText> fields = new List<FieldText>();

        public IReadOnlyList<FieldText> Fields => fields;
    }
}
