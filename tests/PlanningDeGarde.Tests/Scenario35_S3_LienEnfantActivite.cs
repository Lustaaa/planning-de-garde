using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 35 — Sc.3 — Lien enfant↔activité N-M : lier / délier + rejets (@back, frontière Application)
//   Miroir du lien enfant↔parent s34, porté côté ACTIVITÉ (ActiviteFoyer.EnfantsLies) pour l'onglet
//   Activités « Enfants liés » (Sc.4). Lien N-M (0..N des deux côtés), lier/délier idempotents, rejets
//   enfant/activité inconnu SANS écriture partielle. Durabilité Mongo prouvée à part (S3 Mongo tests).
public class Scenario35_S3_LienEnfantActivite
{
    private const string Ecole = "école";
    private const string DomicileA = "domicile A";
    private const string Lea = "lea";
    private const string Noa = "noa";

    private static (LierEnfantActiviteHandler lier, DelierEnfantActiviteHandler delier, ReferentielActivitesEnMemoire store)
        Monter()
    {
        var store = new ReferentielActivitesEnMemoire(); // seedé : école, domicile A, domicile B, nounou
        var enfants = new FakeReferentielEnfants().AvecEnfant(Lea).AvecEnfant(Noa);
        return (new LierEnfantActiviteHandler(store, enfants, store), new DelierEnfantActiviteHandler(store), store);
    }

    private static System.Collections.Generic.IReadOnlyCollection<string> EnfantsLiesDe(
        ReferentielActivitesEnMemoire store, string activiteId)
        => store.EnumererActivites().Single(a => a.Id == activiteId).EnfantsLies;

    // ---------- Acceptation : lier porte le lien, relu par la query ; ids inchangés ----------
    [Fact]
    public void Acceptation_Should_Porter_le_lien_relu_par_la_query_When_un_enfant_est_lie_a_une_activite()
    {
        var (lier, _, store) = Monter();

        var resultat = lier.Handle(new LierEnfantActiviteCommand(Lea, Ecole));

        Assert.True(resultat.EstSucces);
        Assert.Contains(Lea, EnfantsLiesDe(store, Ecole));                 // lien relu par la query
        Assert.Equal(Ecole, store.EnumererActivites().Single(a => a.Id == Ecole).Id); // id activité inchangé
    }

    // ---------- N-M : plusieurs enfants partagent une activité ; un enfant porte plusieurs activités ----------
    [Fact]
    public void Should_Etre_N_M_When_plusieurs_liens_sont_poses()
    {
        var (lier, _, store) = Monter();

        lier.Handle(new LierEnfantActiviteCommand(Lea, Ecole));
        lier.Handle(new LierEnfantActiviteCommand(Noa, Ecole));      // 2 enfants -> même activité
        lier.Handle(new LierEnfantActiviteCommand(Lea, DomicileA));  // 1 enfant -> 2 activités

        Assert.Equal(new[] { Lea, Noa }, EnfantsLiesDe(store, Ecole).OrderBy(x => x)); // école partagée
        Assert.Contains(DomicileA, store.EnumererActivites().Where(a => a.EnfantsLies.Contains(Lea)).Select(a => a.Id));
        Assert.Contains(Ecole, store.EnumererActivites().Where(a => a.EnfantsLies.Contains(Lea)).Select(a => a.Id));
    }

    // ---------- Lier un enfant DÉJÀ lié est neutre (idempotent, pas de doublon) ----------
    [Fact]
    public void Should_Etre_neutre_When_un_enfant_deja_lie_est_relie()
    {
        var (lier, _, store) = Monter();
        lier.Handle(new LierEnfantActiviteCommand(Lea, Ecole));

        var resultat = lier.Handle(new LierEnfantActiviteCommand(Lea, Ecole));

        Assert.True(resultat.EstSucces);
        Assert.Single(EnfantsLiesDe(store, Ecole)); // un seul « lea », aucun doublon
    }

    // ---------- Délier retire le lien ; délier un lien DÉJÀ absent est idempotent (neutre) ----------
    [Fact]
    public void Should_Retirer_le_lien_puis_etre_idempotent_When_on_delie()
    {
        var (lier, delier, store) = Monter();
        lier.Handle(new LierEnfantActiviteCommand(Lea, Ecole));

        var retrait = delier.Handle(new DelierEnfantActiviteCommand(Lea, Ecole));
        Assert.True(retrait.EstSucces);
        Assert.DoesNotContain(Lea, EnfantsLiesDe(store, Ecole));

        var reretrait = delier.Handle(new DelierEnfantActiviteCommand(Lea, Ecole)); // déjà absent
        Assert.True(reretrait.EstSucces); // idempotent, sans erreur
        Assert.DoesNotContain(Lea, EnfantsLiesDe(store, Ecole));
    }

    // ---------- Refus : activité INCONNUE → domaine refuse, liens existants intacts (pas d'écriture partielle) ----------
    [Fact]
    public void Should_Refuser_sans_ecriture_partielle_When_l_activite_est_inconnue()
    {
        var (lier, _, store) = Monter();
        lier.Handle(new LierEnfantActiviteCommand(Lea, Ecole)); // lien existant à préserver

        var resultat = lier.Handle(new LierEnfantActiviteCommand(Lea, "dojo")); // « dojo » absent du référentiel

        Assert.False(resultat.EstSucces);
        Assert.Empty(store.EnumererActivites().Where(a => a.Id == "dojo"));  // aucune activité fantôme créée
        Assert.Contains(Lea, EnfantsLiesDe(store, Ecole));                  // le lien existant reste intact
    }

    // ---------- Refus : enfant INCONNU → domaine refuse, sans écriture ----------
    [Fact]
    public void Should_Refuser_sans_ecriture_When_l_enfant_est_inconnu()
    {
        var (lier, _, store) = Monter();

        var resultat = lier.Handle(new LierEnfantActiviteCommand("fantome", Ecole)); // enfant absent du référentiel

        Assert.False(resultat.EstSucces);
        Assert.Empty(EnfantsLiesDe(store, Ecole)); // aucun lien écrit
    }
}
