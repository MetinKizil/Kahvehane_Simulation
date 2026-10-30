namespace KanBagi.Furniture
{
    /// <summary>
    /// Isaret: bu bilesen, esya elde hayaletken (tasinirken veya yeni alinmis, henuz konmamis) KAPALI kalir.
    /// Ornek: rulet masasi ve kumarbaz cagiricisi - hayalet masaya kumarbaz gelmesin, polis hayaleti masa saymasin.
    /// FurniturePlacer, yerlestirme bitince bunlari tekrar acar. MonoBehaviour'lar uygular.
    /// </summary>
    public interface IPlacementPausable
    {
    }
}
