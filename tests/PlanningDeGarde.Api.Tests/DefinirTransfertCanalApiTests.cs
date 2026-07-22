using System.Net.Http.Json;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Acceptation (intégration) du canal « définir un transfert » porté sur l'hôte d'API détaché.
/// La vue WASM <c>DefinirTransfert</c> n'appelle plus le handler en DI directe (impossible côté
/// navigateur) : elle émet sa commande via ce canal HTTP. On vérifie le succès d'un transfert
/// complet et le refus (avec motif) d'un transfert incomplet — la décision vient du Result du use
/// case, jamais d'une règle dupliquée. Store réel singleton, aucune doublure sur le chemin observé.
/// </summary>
public sealed class DefinirTransfertCanalApiTests
{
    private static readonly object TransfertComplet = new
    {
        DeposeParId = "Parent A",
        RecupereParId = "Parent B",
        LieuId = "école",
        Heure = TimeSpan.FromHours(8.5),
        Date = new DateTime(2025, 7, 21),
    };

    private static readonly object TransfertIncomplet = new
    {
        DeposeParId = "",
        RecupereParId = "",
        LieuId = "",
        Heure = TimeSpan.Zero,
        Date = new DateTime(2025, 7, 21),
    };

    [Fact]
    public async Task Should_Confirmer_le_transfert_par_une_reponse_de_succes_When_un_transfert_complet_est_emis_via_le_canal_de_l_hote_d_API()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/transferts", TransfertComplet);

        Assert.True(reponse.IsSuccessStatusCode, $"statut HTTP attendu de succès, obtenu {(int)reponse.StatusCode}.");
    }

    [Fact]
    public async Task Should_Renvoyer_une_reponse_d_echec_avec_motif_When_un_transfert_incomplet_est_emis_via_le_canal_de_l_hote_d_API()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        var reponse = await client.PostAsJsonAsync("/api/transferts", TransfertIncomplet);

        Assert.False(reponse.IsSuccessStatusCode, $"un transfert incomplet doit être refusé, statut obtenu {(int)reponse.StatusCode}.");
        var motif = await reponse.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(motif), "un motif de refus doit être propagé.");
    }
}
