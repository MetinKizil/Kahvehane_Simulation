namespace KanBagi.Interaction
{
    /// <summary>
    /// Oyuncunun "E" ile etkilesebilecegi her seyin sozlesmesi.
    /// Caydanlik, bulasik lavabosu, yonetim PC'si, kapi... hepsi bunu implement edecek.
    /// PlayerInteractor sadece bu interface'i tanir; somut siniflari bilmez (SOLID).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Ekranda gosterilecek ipucu metni. Ornek: "Cay Doldur".</summary>
        string InteractionPrompt { get; }

        /// <summary>Oyuncu E'ye bastiginda cagrilir.</summary>
        void Interact();
    }
}
