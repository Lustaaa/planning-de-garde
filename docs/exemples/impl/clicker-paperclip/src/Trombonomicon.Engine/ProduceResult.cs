namespace Trombonomicon.Engine;

public class ProduceResult
{
    public int     Produced      { get; init; }
    public int     WireConsumed  { get; init; }
    public bool    Blocked       { get; init; }
    /// <summary>Message à afficher à l'utilisateur. Null si aucun message.</summary>
    public string? Message       { get; init; }
}
