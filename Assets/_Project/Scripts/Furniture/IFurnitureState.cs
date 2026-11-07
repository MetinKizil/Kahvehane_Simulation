namespace KanBagi.Furniture
{
    /// <summary>
    /// Esyanin konumu disinda kaydedilecek kendi durumu (D8a: masadaki oyun takimi).
    /// FurnitureManager kayitta CaptureState'i esya satirina yazar, yuklerken RestoreState'e verir.
    /// Esyanin kokunde, PlacedFurniture ile ayni objede durur.
    /// </summary>
    public interface IFurnitureState
    {
        string CaptureFurnitureState();
        void RestoreFurnitureState(string state);
    }
}
