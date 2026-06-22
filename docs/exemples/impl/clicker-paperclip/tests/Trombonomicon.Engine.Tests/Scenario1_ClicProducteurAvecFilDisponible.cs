using Trombonomicon.Engine;
using Xunit;

namespace Trombonomicon.Engine.Tests;

/// <summary>
/// Scénario 1 @nominal — Clic producteur avec fil disponible
/// Given : 100 m fil, 0 trombone, 0 €, prix = 0,10 €
/// When  : clic "Produire"
/// Then  : +1 produit cumulé, −1 fil, +0,10 €, stock trombones = 0 (vendu immédiatement)
/// </summary>
public class Scenario1_ClicProducteurAvecFilDisponible
{
    [Fact]
    public void ClickProduce_AvecFilDisponible_ProduiteVenduImmediatement()
    {
        // Given
        var state = new GameState
        {
            WireMeters    = 100,
            PaperclipStock = 0,
            Money         = 0m,
            SalePrice     = 0.10m,
            TotalProduced = 0
        };

        var engine = new PaperclipEngine();

        // When
        var result = engine.ClickProduce(state);

        // Then — compteur cumulatif
        Assert.Equal(1, state.TotalProduced);

        // Then — fil consommé
        Assert.Equal(1, result.WireConsumed);
        Assert.Equal(99, state.WireMeters);

        // Then — argent (vente automatique)
        Assert.Equal(0.10m, state.Money);

        // Then — stock trombones reste à 0 (vendu immédiatement)
        Assert.Equal(0, state.PaperclipStock);

        // Then — non bloqué
        Assert.False(result.Blocked);
        Assert.Equal(1, result.Produced);
    }
}
