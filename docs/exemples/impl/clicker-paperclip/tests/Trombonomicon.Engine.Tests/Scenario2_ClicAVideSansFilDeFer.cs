using Trombonomicon.Engine;
using Xunit;

namespace Trombonomicon.Engine.Tests;

/// <summary>
/// Scénario 2 @limite — Clic à vide sans fil de fer
/// Given : 0 m fil, 5 trombones en stock, argent initial = 0 €
/// When  : clic "Produire"
/// Then  : aucun trombone produit, fil reste 0, message "Fil de fer insuffisant", argent inchangé
/// </summary>
public class Scenario2_ClicAVideSansFilDeFer
{
    [Fact]
    public void ClickProduce_SansFilDeFer_BloquéEtMessageInsuffisant()
    {
        // Given
        var state = new GameState
        {
            WireMeters     = 0,
            PaperclipStock = 5,
            Money          = 0m,
            SalePrice      = 0.10m,
            TotalProduced  = 0
        };
        var moneyAvant = state.Money;

        var engine = new PaperclipEngine();

        // When
        var result = engine.ClickProduce(state);

        // Then — aucun trombone produit
        Assert.Equal(0, result.Produced);
        Assert.Equal(0, state.TotalProduced);

        // Then — fil reste à 0
        Assert.Equal(0, result.WireConsumed);
        Assert.Equal(0, state.WireMeters);

        // Then — message "Fil de fer insuffisant" visible
        Assert.Equal("Fil de fer insuffisant", result.Message);

        // Then — argent inchangé
        Assert.Equal(moneyAvant, state.Money);

        // Then — bloqué
        Assert.True(result.Blocked);
    }
}
