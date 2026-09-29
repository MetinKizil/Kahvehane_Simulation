using KanBagi.Interaction;
using UnityEngine;

namespace KanBagi.Illegal
{
    /// <summary>
    /// Baskina gelen memur. Kendi basina karar vermez; denetimi PoliceManager yurutur (SOLID).
    /// Bu script sadece oyuncuya bir ipucu metni gosterir - konusmak bir sey degistirmez,
    /// oyuncunun isi masayi zamaninda ortmekti.
    /// </summary>
    public class PoliceOfficer : MonoBehaviour, IInteractable
    {
        [Header("Ipucu")]
        [SerializeField] private string idlePrompt = "Memur etrafa bakiyor";

        public string InteractionPrompt => idlePrompt;

        public void Interact()
        {
            // Bilincli olarak bos: memurla pazarlik yok.
        }
    }
}
