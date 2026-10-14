using System;
using System.Collections.Generic;
using UnityEngine;

namespace KanBagi.Data
{
    /// <summary>Hikayenin tek bir sayfasi: baslik + metin.</summary>
    [Serializable]
    public class StoryPage
    {
        [SerializeField] private string title;
        [SerializeField] [TextArea(4, 10)] private string body;

        public string Title => title;
        public string Body => body;
    }

    /// <summary>
    /// Sayfa sayfa okunan hikaye metni (oyun girisi). Buton yazilari da burada durur.
    /// Sadece VERIDIR; ne zaman gosterilecegini bilmez (ANAYASA: veriler ScriptableObject'te).
    /// Menu: Create > Kan Bagi > Story Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewStory", menuName = "Kan Bagi/Story Data")]
    public class StoryData : ScriptableObject
    {
        [SerializeField] private List<StoryPage> pages = new List<StoryPage>();

        [Header("Butonlar")]
        [SerializeField] private string continueLabel = "Devam";
        [SerializeField] private string skipLabel = "Atla";
        [Tooltip("Son sayfadaki, oyunu baslatan butonun yazisi.")]
        [SerializeField] private string finishLabel = "Basla";

        public IReadOnlyList<StoryPage> Pages => pages;
        public string ContinueLabel => continueLabel;
        public string SkipLabel => skipLabel;
        public string FinishLabel => finishLabel;
    }
}
