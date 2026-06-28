using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 13 — Sc.1 — Supprimer un acteur autorisé le retire de la configuration persistée (@nominal, @driver)
//   Tranche BACKEND (tdd-auto) : commande/handler SupprimerActeur + port d'écriture
//   IEditeurConfigurationFoyer.Supprimer qui retire l'entrée NOM ET COULEUR du store, de sorte que la
//   configuration relue n'énumère/ne résout plus l'acteur (NomDe retombe sur l'id brut, couleur sur le
//   neutre) tandis que les autres acteurs restent. L'acceptation RUNTIME (intégration Mongo réel,
//   disparition du store après redémarrage) est menée séparément (SupprimerActeurMongoIntegrationTests).
public class Scenario1_SupprimerActeur
{
    private const string NounouId = "acteur-nounou";
    private const string Nounou = "Nounou";

    // ---------- Test #1 — Driver : une suppression retire l'acteur du référentiel relu, les autres restent ----------
    // Contradiction : aucune commande/handler/port `Supprimer` n'existe — le store ne sait qu'Ajouter /
    // Renommer / Recolorier. Force la création de SupprimerActeurCommand + SupprimerActeurHandler +
    // IEditeurConfigurationFoyer.Supprimer(acteurId), qui retire l'entrée NOM ET COULEUR, de sorte que la
    // configuration relue ne résout plus Nounou (NomDe retombe sur l'id brut, couleur sur le neutre) alors
    // que Parent A / Parent B restent résolus.
    [Fact]
    public void Should_Retirer_l_acteur_du_referentiel_relu_tout_en_conservant_les_autres_acteurs_When_un_parent_supprime_un_acteur_autorise_par_son_identifiant_stable()
    {
        var configuration = new FakeConfigurationFoyer(
            new Dictionary<string, string> { [NounouId] = Nounou, ["parent-a"] = "Parent A", ["parent-b"] = "Parent B" },
            new Dictionary<string, string> { [NounouId] = "vert" });
        var notificateur = new FakeNotificateurPlanning();
        var handler = new SupprimerActeurHandler(configuration, notificateur);

        var resultat = handler.Handle(new SupprimerActeurCommand(NounouId));

        Assert.True(resultat.EstSucces);
        Assert.Equal(NounouId, configuration.NomDe(NounouId));                          // Nounou n'est plus résolu : NomDe retombe sur l'id brut
        Assert.Equal(FakeConfigurationFoyer.Neutre, configuration.CouleurDe(NounouId)); // ... et sa couleur est aussi retirée (repli neutre)
        Assert.Equal("Parent A", configuration.NomDe("parent-a"));                      // les autres acteurs restent résolus
        Assert.Equal("Parent B", configuration.NomDe("parent-b"));
        Assert.Equal(1, notificateur.NombreDeNotifications);                            // diffusion temps réel sur succès (CQRS : write → diffusion lecture seule)
    }
}
