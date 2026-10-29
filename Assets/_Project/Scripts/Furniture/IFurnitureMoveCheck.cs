namespace KanBagi.Furniture
{
    /// <summary>
    /// Bir esya su an tasinabilir mi? Esyanin uzerindeki bilesenler (masa, ocak, lavabo, PC...) kendi kuralini soyler;
    /// FurniturePlacer F'ye basilinca hepsine sorar, biri "hayir" derse esya kalkmaz ve sebep gosterilir.
    /// Ornek: masada musteri var, ocak calisiyor, lavaboda tepsi var, PC ekrani acik.
    /// </summary>
    public interface IFurnitureMoveCheck
    {
        bool CanMove(out string reason);
    }
}
