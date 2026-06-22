namespace Trombonomicon.Engine;

public class GameState
{
    public int     WireMeters     { get; set; }
    public int     PaperclipStock { get; set; }
    public decimal Money          { get; set; }
    public decimal SalePrice      { get; set; }
    public int     TotalProduced  { get; set; }
}
