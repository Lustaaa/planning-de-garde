using System;
using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 13 — Sc.2 — Surcharge orpheline : la case retombe sur le fond (le cycle reprend) (@limite, @driver)
//   Tranche BACKEND (tdd-auto, frontière Application — projection GrilleAgendaQuery). Vrai RED neuf :
//   une surcharge (période saisie) pointant un acteur SUPPRIMÉ doit CESSER de primer ; la case retombe
//   sur le responsable de FOND (le cycle reprend), jamais sur un nom fantôme (id brut). Le filtre
//   d'existence s'appuie sur le port de lecture EXISTANT IEnumerationActeursFoyer (décision CP : pas de
//   nouveau contrat ActeurExiste), injecté en option dans la query (null → pas de filtrage).
public class Scenario2_SurchargeOrphelineRetombeFond
{
    private const string ParentA = "parent-a";
    private const string Alice = "Alice";
    private const string Bleu = "bleu";

    private static readonly DateOnly Lundi_15_06_2026 = new(2026, 6, 15);  // date de référence (fenêtre couvrant le 16/06)
    private static readonly DateOnly Mardi_16_06_2026 = new(2026, 6, 16);  // jour de la surcharge (période saisie → Nounou)

    // Cycle N=2 mappant les deux index sur Parent A : le fond résout toujours Parent A (indépendant de la parité).
    private static IReferentielCycleDeFond CycleToujoursParentA()
        => new FakeReferentielCycleDeFond(new CycleDeFond(2, new Dictionary<int, string> { [0] = ParentA, [1] = ParentA }));

    // ---------- Acceptation (boucle externe, frontière Application — store réel + handlers réels) ----------
    // Traduit le scénario à la frontière Application : un store réel (ConfigurationFoyerEnMemoire) seedé,
    // Nounou ajoutée puis affectée au 16/06 (surcharge), puis SUPPRIMÉE par le handler ; la projection de
    // la case du 16/06 doit afficher Parent A (Alice) et sa couleur de fond (bleu) — la surcharge orpheline
    // ne prime plus. Preuve sur câblage réel (store + handlers d'ajout/suppression + query).
    [Fact]
    public void Acceptation_Should_Faire_retomber_la_case_sur_le_responsable_de_fond_When_l_acteur_d_une_periode_saisie_est_supprime()
    {
        var store = new ConfigurationFoyerEnMemoire(); // seeds : parent-a → Alice / bleu
        var nounouId = new AjouterActeurHandler(store)
            .Handle(new AjouterActeurCommand("Nounou", "vert")).Valeur!.ActeurId;

        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(nounouId,
            new DateTime(2026, 6, 16), new DateTime(2026, 6, 16)).Valeur!); // surcharge au seul 16/06

        new SupprimerActeurHandler(store, new FakeNotificateurPlanning(), new FakeReferentielComptes(), new FakeReferentielComptes())
            .Handle(new SupprimerActeurCommand(nounouId)); // Nounou supprimée : sa surcharge devient orpheline

        var query = new GrilleAgendaQuery(new FakeSlotRepository(), periodes, store, store, CycleToujoursParentA(), store);
        var grille = query.Projeter(Lundi_15_06_2026);

        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_16_06_2026);
        Assert.Equal(Alice, caseMardi.NomResponsable);     // la case retombe sur le fond Parent A (le cycle reprend)
        Assert.Equal(Bleu, caseMardi.CouleurResponsable);  // ... avec sa couleur de fond, jamais un nom/teinte fantôme
    }

    // ---------- Test #1 — Driver : une surcharge orpheline est ignorée AVANT le repli sur le fond ----------
    // Contradiction : aujourd'hui `CaseJourAu` prend `periode?.ResponsableId ?? fond` SANS vérifier
    // l'existence de l'acteur — l'acteur supprimé (absent de l'énumération, nom retombé sur l'id brut)
    // s'afficherait en nom fantôme au lieu du fond. Force un FILTRE d'existence sur la surcharge : une
    // surcharge orpheline est neutralisée AVANT le `?? fond`, de sorte que la case retombe sur Parent A
    // (et NON sur le neutre — le piège du filtre appliqué après combinaison est verrouillé ici).
    [Fact]
    public void Should_Faire_retomber_la_case_sur_le_responsable_de_fond_avec_sa_couleur_When_l_acteur_d_une_surcharge_ponctuelle_est_supprime()
    {
        const string NounouOrpheline = "acteur-nounou"; // référencée par la période mais ABSENTE de l'énumération (supprimée)

        var periodes = new FakePeriodeRepository();
        periodes.Enregistrer(PeriodeDeGarde.Affecter(NounouOrpheline,
            new DateTime(2026, 6, 16), new DateTime(2026, 6, 16)).Valeur!);

        // Référentiel / palette SANS l'acteur supprimé (NomDe retombe sur l'id brut, couleur sur le neutre)
        // ET énumération NE listant que Parent A : le contrat d'existence rend NounouOrpheline orpheline.
        var query = new GrilleAgendaQuery(
            new FakeSlotRepository(), periodes,
            new FakePaletteCouleurs(new Dictionary<string, string> { [ParentA] = Bleu }),
            new FakeReferentielResponsables(new Dictionary<string, string> { [ParentA] = Alice }),
            CycleToujoursParentA(),
            new FakeEnumerationActeursFoyer(ParentA)); // Parent A existe ; NounouOrpheline n'existe plus

        var grille = query.Projeter(Lundi_15_06_2026);

        var caseMardi = grille.Jours.Single(j => j.Date == Mardi_16_06_2026);
        Assert.Equal(Alice, caseMardi.NomResponsable);     // surcharge orpheline ignorée → fond Parent A
        Assert.Equal(Bleu, caseMardi.CouleurResponsable);  // ... couleur de FOND, pas le neutre (filtre AVANT le repli)
    }
}
