namespace ZeroKWeb
{
    /// <summary>
    /// A grid and one of its columns, so Views/Shared/Grid/Column.cshtml can be a partial view.
    ///
    /// The @helper it replaces took two arguments; a partial view takes one model. This is that
    /// model, and it exists for no other reason.
    /// </summary>
    public class GridColumnModel
    {
        public IUniGrid Grid { get; set; }
        public IUniGridCol Col { get; set; }
    }
}
