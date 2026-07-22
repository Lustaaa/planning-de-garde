using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Bunit;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PlanningDeGarde.Application;
using PlanningDeGarde.Web.Components.Pages;
using Xunit;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Sprint 28 — S5 (@ihm, acceptation de NIVEAU RUNTIME) : l'écran « mot de passe oublié » réellement
/// câblé (<see cref="MotDePasseOublie"/>, API distante RÉELLE <see cref="ApiDistanteFactory"/>, endpoint
/// <c>POST /api/comptes/recuperation</c> + <c>DemanderRecuperationMotDePasseHandler</c> + store
/// réels) émet la demande via le canal et affiche un message NEUTRE fixe. Le chemin observé n'est pas
/// doublé : la commande transite par le canal HTTP réel jusqu'au handler — prouvé par un <b>Spy sur le
/// port de sortie</b> <see cref="IEnvoiMail"/> (l'adaptateur SMTP réel est prouvé séparément en S1, donc
/// remplacé ici par un spy pour découpler le test de Smtp4dev). Anti-énumération : le message affiché est
/// un littéral fixe, identique que l'email soit connu ou non.
/// </summary>
public sealed class FrontWasmMotDePasseOublieRuntimeTests : TestContext
{
    /// <summary>Spy à la main du port de sortie d'envoi de mail : observe que la demande a réellement
    /// atteint le handler côté API distante (le chemin screen → canal HTTP → handler est réel).</summary>
    private sealed class SpyEnvoiMail : IEnvoiMail
    {
        private readonly List<(string Destinataire, string Jeton)> _mails = new();
        public void EnvoyerRecuperationMotDePasse(string destinataire, string jeton) => _mails.Add((destinataire, jeton));
        public int NombreDeMailsEmis => _mails.Count;
        public (string Destinataire, string Jeton)? Dernier => _mails.Count == 0 ? null : _mails.Last();
    }

    [Fact]
    public void Should_emettre_la_demande_via_le_canal_et_afficher_un_message_neutre_When_l_utilisateur_saisit_son_email_et_valide_sur_l_ecran_mot_de_passe_oublie()
    {
        // Given — l'écran câblé à l'API distante RÉELLE ; un compte Actif « connu@foyer.fr » existe (le
        // handler appellera donc le port d'envoi, observé par le spy). L'adaptateur SMTP réel (S1) est
        // remplacé par un spy pour découpler ce test @ihm de l'infra Smtp4dev.
        var spy = new SpyEnvoiMail();
        using var apiBase = new ApiDistanteFactory();
        using var api = apiBase.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<IEnvoiMail>(spy)));
        api.Services.GetRequiredService<IEditeurComptes>()
            .Creer("compte-connu", "connu@foyer.fr", StatutCompte.Actif, "acteur-connu");
        Services.AddSingleton(new HttpClient(api.Server.CreateHandler()) { BaseAddress = api.Server.BaseAddress });

        var ecran = RenderComponent<MotDePasseOublie>();

        // When — l'utilisateur saisit son email puis valide (POST /api/comptes/recuperation réel).
        this.SurDispatcher(() => ecran.Find("[data-testid='champ-email-oubli']").Change("connu@foyer.fr"));
        this.SurDispatcher(() => ecran.Find("[data-testid='bouton-demander-recuperation']").Click());

        // Then — un message NEUTRE fixe est affiché, ET la demande a réellement transité jusqu'au handler
        // (le port de sortie a été appelé pour cet email — chemin runtime non doublé).
        ecran.WaitForAssertion(
            () =>
            {
                Assert.Contains("Si un compte existe, un mail a été envoyé.", ecran.Markup);
                Assert.Equal(1, spy.NombreDeMailsEmis);
                Assert.Equal("connu@foyer.fr", spy.Dernier!.Value.Destinataire);
            },
            TimeSpan.FromSeconds(10));
    }
}
