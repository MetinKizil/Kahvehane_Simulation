using KanBagi.Player;

namespace KanBagi.Interaction
{
    /// <summary>
    /// Oyuncunun "E" ile etkilesebilecegi her seyin sozlesmesi.
    /// Caydanlik, bulasik lavabosu, yonetim PC'si, kapi... hepsi bunu implement edecek.
    /// PlayerInteractor sadece bu interface'i tanir; somut siniflari bilmez (SOLID).
    /// Her cagri etkilesimi YAPAN oyuncuyu tasir: co-op'ta singleton'a sorulmaz.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Bu oyuncuya ekranda gosterilecek ipucu metni. Ornek: "Cay Doldur".</summary>
        string GetPrompt(PlayerContext player);

        /// <summary>Oyuncu E'ye bastiginda cagrilir.</summary>
        void Interact(PlayerContext player);
    }
}
