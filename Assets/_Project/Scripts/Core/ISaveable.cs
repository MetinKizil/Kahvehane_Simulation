namespace KanBagi.Core
{
    /// <summary>
    /// Kaydedilebilen her sistemin sozlesmesi.
    /// Her sistem KENDI verisini JSON metnine cevirir; SaveManager kimin ne tuttugunu bilmez (SOLID).
    /// Yeni bir sistem eklemek icin sadece bu arayuzu uygulamak yeterlidir, SaveManager degismez.
    /// </summary>
    public interface ISaveable
    {
        /// <summary>Kayit dosyasindaki benzersiz anahtar ("economy", "inventory"...).</summary>
        string SaveKey { get; }

        /// <summary>Su anki durumu JSON metni olarak verir.</summary>
        string CaptureState();

        /// <summary>Kayittan gelen JSON metnini uygular. Bozuk/eksik veride sessizce vazgecilir.</summary>
        void RestoreState(string state);
    }
}
