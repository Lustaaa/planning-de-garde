namespace Trombonomicon.Engine;

/// <summary>
/// Moteur de règles Trombonomicon — YAGNI : seul ClickProduce est implémenté (Scénario 1).
/// </summary>
public class PaperclipEngine
{
    /// <summary>
    /// Règle 1 : clic manuel avec fil disponible — produit 1 trombone, consomme 1 m de fil,
    /// vend immédiatement au prix courant (vente automatique, règle 6).
    /// Mutates <paramref name="state"/> en place et renvoie un résumé de l'opération.
    /// </summary>
    public ProduceResult ClickProduce(GameState state)
    {
        if (state.WireMeters <= 0)
        {
            return new ProduceResult { Produced = 0, WireConsumed = 0, Blocked = true };
        }

        state.WireMeters--;
        state.TotalProduced++;

        // Vente automatique immédiate (stock reste à 0)
        state.Money += state.SalePrice;

        return new ProduceResult { Produced = 1, WireConsumed = 1, Blocked = false };
    }
}
