using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KanBagi.UI
{
    /// <summary>
    /// Basit sekme grubu: her sekmenin bir butonu ve bir sayfasi vardir.
    /// Sadece hangi sayfanin acik oldugunu yonetir; sayfalarin icerigini bilmez (SOLID).
    ///
    /// NOT: Sekme butonlarinin Transition'i 'None' olmali, yoksa Button'un kendi
    /// renk gecisi buradaki aktif/pasif rengin uzerine yazar.
    /// </summary>
    public class TabGroupUI : MonoBehaviour
    {
        /// <summary>Tek bir sekme: buton + acacagi sayfa.</summary>
        [Serializable]
        public class Tab
        {
            [SerializeField] private Button button;
            [SerializeField] private GameObject page;

            public Button Button => button;
            public GameObject Page => page;
            public bool IsValid => button != null && page != null;
        }

        [Header("Sekmeler")]
        [SerializeField] private List<Tab> tabs = new List<Tab>();
        [Tooltip("Ekran her acildiginda hangi sekme secili gelsin.")]
        [SerializeField] private int defaultTabIndex;

        [Header("Renkler")]
        [SerializeField] private Color activeColor = new Color(0.25f, 0.45f, 0.75f, 1f);
        [SerializeField] private Color inactiveColor = new Color(0.18f, 0.18f, 0.18f, 1f);

        private int currentIndex = -1;

        private void Awake()
        {
            // Dinleyiciler Awake'te baglanir; sekme secimi OnEnable'da yapiliyor, sira onemli.
            for (int i = 0; i < tabs.Count; i++)
            {
                if (!tabs[i].IsValid) continue;

                int index = i;
                tabs[i].Button.onClick.AddListener(() => Select(index));
            }
        }

        private void OnEnable()
        {
            // Ekran her acildiginda varsayilan sekmeye donsun.
            currentIndex = -1;
            Select(defaultTabIndex);
        }

        public void Select(int index)
        {
            if (index == currentIndex) return;

            currentIndex = index;

            for (int i = 0; i < tabs.Count; i++)
            {
                if (!tabs[i].IsValid) continue;

                bool isActive = i == index;
                tabs[i].Page.SetActive(isActive);

                if (tabs[i].Button.targetGraphic is Image tabImage)
                    tabImage.color = isActive ? activeColor : inactiveColor;
            }
        }

        private void OnDestroy()
        {
            foreach (Tab tab in tabs)
            {
                if (tab.IsValid) tab.Button.onClick.RemoveAllListeners();
            }
        }
    }
}
