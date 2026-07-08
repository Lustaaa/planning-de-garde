using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;

namespace PlanningDeGarde.Api.Tests;

/// <summary>
/// Sprint 28 — S7 (câblage du chemin « définir un mot de passe »). L'endpoint d'écriture
/// <c>POST /api/canal/definir-mot-de-passe</c> porté sur l'hôte d'API détaché (store réel singleton,
/// InMemory par défaut) pose un mot de passe sur un compte via <see cref="DefinirMotDePasseHandler"/>
/// (DI réelle) : après quoi la connexion « email + mot de passe » réussit avec le bon couple et échoue
/// avec le mauvais (vérifiée par le <see cref="SeConnecterHandler"/> réel résolu de l'hôte). Aucune
/// doublure sur le chemin observé.
/// </summary>
public sealed class DefinirMotDePasseCanalApiTests
{
    private const string Email = "maman@foyer.fr";
    private const string BonMotDePasse = "bon-secret";
    private const string MauvaisMotDePasse = "mauvais";

    [Fact]
    public async Task Should_Rendre_le_compte_connectable_par_email_mot_de_passe_When_un_mot_de_passe_est_pose_via_l_endpoint_definir()
    {
        using var hote = new ApiHoteFactory();
        var client = hote.CreateClient();

        // Un compte Actif email-only est seedé dans le store réel singleton de l'hôte.
        hote.Services.GetRequiredService<ReferentielComptesEnMemoire>()
            .Creer("compte-maman", Email, StatutCompte.Actif, "acteur-maman");

        // When — on pose un mot de passe via l'endpoint du canal d'écriture.
        var reponse = await client.PostAsJsonAsync(
            "/api/canal/definir-mot-de-passe", new { CompteId = "compte-maman", MotDePasse = BonMotDePasse });
        Assert.True(reponse.IsSuccessStatusCode, $"la définition doit aboutir, statut {(int)reponse.StatusCode}.");

        // Then — la connexion « email + mot de passe » réussit avec le bon couple, échoue avec le mauvais
        // (SeConnecterHandler réel résolu de l'hôte, lisant le store réel où le condensat a été posé).
        using var scope = hote.Services.CreateScope();
        var connexion = scope.ServiceProvider.GetRequiredService<SeConnecterHandler>();
        Assert.True(connexion.Handle(new SeConnecterCommand(Email, BonMotDePasse)).EstSucces);
        Assert.False(connexion.Handle(new SeConnecterCommand(Email, MauvaisMotDePasse)).EstSucces);
    }
}
