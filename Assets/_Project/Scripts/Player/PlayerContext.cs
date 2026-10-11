namespace KanBagi.Player
{
    /// <summary>
    /// Bir etkilesimi YAPAN oyuncu: eli ve kilidi.
    /// Istasyonlar "elinde ne var?" sorusunu singleton'a degil buna sorar;
    /// boylece co-op'ta her oyuncu kendi eliyle etkilesir.
    /// PlayerInteractor kendi objesindeki bilesenlerden kurar; sahneye eklenmez.
    /// </summary>
    public sealed class PlayerContext
    {
        public PlayerHands Hands { get; }
        public PlayerLock Lock { get; }

        public PlayerContext(PlayerHands hands, PlayerLock playerLock)
        {
            Hands = hands;
            Lock = playerLock;
        }
    }
}
