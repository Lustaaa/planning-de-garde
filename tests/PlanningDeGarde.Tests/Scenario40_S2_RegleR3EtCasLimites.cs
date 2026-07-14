using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 40 — Sc.2 — Règle R3 explicite + cas limites (décompte fidèle, zéro fantôme) (@back)
//   {complet = un père ET une mère} · {incomplet = 0/1 parent, OU 2 sans le couple père+mère
//   (ex. deux « parent-libre »)} · {vide = racine sans parent}. Orphelin exclu du décompte (miroir
//   Resolvable s13) ; un lien s34 nu compte comme « parent-libre » (défaut neutre s37, ne satisfait pas
//   seul « père ET mère »).
//
// Frontière Application (projection enrichie). Cas limites qui FORCENT les branches de la règle R3 :
// deux parent-libre / un seul parent → Incomplet ; aucun parent → Vide ; orphelin non compté → Incomplet.
public class Scenario40_S2_RegleR3EtCasLimites
{
    private const string EnfantId = "enfant-x";
    private const string AliceId = "acteur-alice";
    private const string BobId = "acteur-bob";
    private const string ChloeId = "acteur-chloe";

    private static FakeReferentielResponsables Noms()
        => new(new Dictionary<string, string> { [AliceId] = "Alice", [BobId] = "Bob", [ChloeId] = "Chloé" });

    private static StatutCoupleR3 StatutDe(FakeReferentielEnfants enfants, FakeEnumerationActeursFoyer acteurs)
        => new GrapheFoyerQuery(enfants, Noms(), acteurs).Lire().Single(e => e.EnfantId == EnfantId).StatutCouple;

    [Fact]
    public void Should_Etre_Complet_When_un_pere_et_une_mere()
    {
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");
        enfants.LierParent(EnfantId, AliceId, RoleDuLien.Mere);
        enfants.LierParent(EnfantId, BobId, RoleDuLien.Pere);

        Assert.Equal(StatutCoupleR3.Complet, StatutDe(enfants, new FakeEnumerationActeursFoyer(AliceId, BobId)));
    }

    [Fact]
    public void Should_Etre_Incomplet_When_deux_parents_libres()
    {
        // Deux parents liés mais AUCUN père/mère explicite → pas le couple père+mère → INCOMPLET
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");
        enfants.LierParent(EnfantId, AliceId, RoleDuLien.ParentLibre);
        enfants.LierParent(EnfantId, BobId, RoleDuLien.ParentLibre);

        Assert.Equal(StatutCoupleR3.Incomplet, StatutDe(enfants, new FakeEnumerationActeursFoyer(AliceId, BobId)));
    }

    [Fact]
    public void Should_Etre_Incomplet_When_un_pere_et_un_parent_libre()
    {
        // 2 parents mais pas le couple père+mère (père + parent-libre) → INCOMPLET
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");
        enfants.LierParent(EnfantId, AliceId, RoleDuLien.Pere);
        enfants.LierParent(EnfantId, BobId, RoleDuLien.ParentLibre);

        Assert.Equal(StatutCoupleR3.Incomplet, StatutDe(enfants, new FakeEnumerationActeursFoyer(AliceId, BobId)));
    }

    [Fact]
    public void Should_Etre_Incomplet_When_un_seul_parent()
    {
        // Un seul parent (peu importe le rôle-du-lien) → moins de deux parents → INCOMPLET
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");
        enfants.LierParent(EnfantId, AliceId, RoleDuLien.Pere);

        Assert.Equal(StatutCoupleR3.Incomplet, StatutDe(enfants, new FakeEnumerationActeursFoyer(AliceId, BobId)));
    }

    [Fact]
    public void Should_Etre_Vide_When_aucun_parent_lie()
    {
        // Racine isolée légitime (0 parent accepté s34) → état neutre VIDE, distinct de « incomplet »
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");

        Assert.Equal(StatutCoupleR3.Vide, StatutDe(enfants, new FakeEnumerationActeursFoyer()));
    }

    [Fact]
    public void Should_Ne_pas_compter_l_orphelin_et_rester_Incomplet_When_le_seul_lien_pointe_un_acteur_supprime()
    {
        // Le seul lien de l'enfant pointe Bob, SUPPRIMÉ du référentiel (orphelin résiduel, nom stale résoluble) :
        // l'orphelin n'est PAS compté (miroir R5/R6, filtre Resolvable s13) → INCOMPLET, pas faussement complet.
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");
        enfants.LierParent(EnfantId, BobId, RoleDuLien.Pere);

        // Bob absent du contrat d'existence (supprimé) — seule Alice existerait, mais elle n'est pas liée ici.
        Assert.Equal(StatutCoupleR3.Incomplet, StatutDe(enfants, new FakeEnumerationActeursFoyer(AliceId)));
    }

    [Fact]
    public void Should_Rester_Incomplet_When_pere_et_mere_mais_la_mere_est_orpheline()
    {
        // Père (Bob) existant + « mère » (Chloé) SUPPRIMÉE : le couple père+mère n'est PAS satisfait car la
        // mère orpheline ne compte pas — pas de fantôme qui rendrait faussement complet.
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");
        enfants.LierParent(EnfantId, BobId, RoleDuLien.Pere);
        enfants.LierParent(EnfantId, ChloeId, RoleDuLien.Mere);

        Assert.Equal(StatutCoupleR3.Incomplet, StatutDe(enfants, new FakeEnumerationActeursFoyer(BobId)));
    }

    [Fact]
    public void Should_Etre_Incomplet_When_le_seul_lien_est_nu_donc_parent_libre()
    {
        // Un lien s34 nu compte comme « parent-libre » (défaut neutre s37) → ne satisfait pas « père ET mère »
        var enfants = new FakeReferentielEnfants().AvecEnfant(EnfantId, "X");
        enfants.LierParent(EnfantId, AliceId); // lien nu → parent-libre

        Assert.Equal(StatutCoupleR3.Incomplet, StatutDe(enfants, new FakeEnumerationActeursFoyer(AliceId)));
    }
}
