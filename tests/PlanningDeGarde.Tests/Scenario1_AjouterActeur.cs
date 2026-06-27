using System.Collections.Generic;
using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using PlanningDeGarde.Tests.Fakes;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 09 — Sc.1 — Ajouter la nounou au foyer génère un identifiant stable neuf (@nominal, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : commande/handler AjouterActeur qui génère un IDENTIFIANT STABLE
//   NEUF OPAQUE (jamais dérivé du libellé), persiste l'acteur via le port d'écriture, et l'expose
//   à l'énumération DU STORE (l'écran de config énumère depuis le store, pas la liste statique
//   front Foyer.ActeursEditables). L'acceptation RUNTIME (Carla apparaît dans la liste de l'écran
//   de config réellement câblé, sans recharger — front WASM + API distante + store durable) est
//   menée séparément par ihm-builder. On NE teste PAS ici un rendu Blazor.
public class Scenario1_AjouterActeur
{
    private const string Carla = "Carla";
    private const string Rose = "rose";
    private static readonly string[] IdsSeeds = { "parent-a", "parent-b", "grand-pere" };

    // ---------- Acceptation (boucle externe, frontière Application) ----------
    // Traduit le scénario Gherkin à la frontière Application (sans IHM) : un parent ajoute « Carla »
    // en rose ; le store réel (seedé depuis Foyer) doit ensuite ÉNUMÉRER Carla, la résoudre par son
    // nom ET sa couleur sur un identifiant NEUF, distinct des seeds et NON dérivé du libellé.
    [Fact]
    public void Acceptation_Should_Enumerer_Carla_resolue_par_nom_et_couleur_sur_un_identifiant_neuf_distinct_des_seeds_et_non_derive_du_libelle_When_un_parent_ajoute_Carla_en_rose()
    {
        var store = new ConfigurationFoyerEnMemoire();
        var handler = new AjouterActeurHandler(store);

        var resultat = handler.Handle(new AjouterActeurCommand(Carla, Rose));

        Assert.True(resultat.EstSucces);
        var idCarla = resultat.Valeur!.ActeurId;
        Assert.Contains(idCarla, store.EnumererActeurs()); // Carla apparaît dans la liste énumérée du store
        Assert.Equal(Carla, store.NomDe(idCarla));         // ... résolue par son nom
        Assert.Equal(Rose, store.CouleurDe(idCarla));      // ... et sa couleur
        Assert.DoesNotContain(idCarla, IdsSeeds);          // identifiant distinct d'Alice/Bruno/grand-père
        Assert.NotEqual(Carla, idCarla);                   // ... et non dérivé du libellé « Carla »
    }

    // ---------- Test #1 — Driver : un ajout fait EXISTER l'acteur, résolu par nom + couleur sur un id neuf ----------
    // Contradiction : aucune commande/handler AjouterActeur n'existe — le store ne sait que renommer/
    // recolorier des seeds existants. Force l'orchestration : un ajout génère un identifiant et persiste
    // le nom (+ couleur fournie) via le port d'écriture, de sorte que NomDe(idNeuf)=« Carla » et
    // CouleurDe(idNeuf)=rose.
    [Fact]
    public void Should_Faire_exister_l_acteur_ajoute_resolu_par_son_nom_et_sa_couleur_sur_un_identifiant_neuf_When_un_parent_ajoute_une_actrice_avec_un_nom_et_une_couleur()
    {
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string>());
        var handler = new AjouterActeurHandler(configuration);

        var resultat = handler.Handle(new AjouterActeurCommand(Carla, Rose));

        Assert.True(resultat.EstSucces);
        var idNeuf = resultat.Valeur!.ActeurId;
        Assert.Equal(Carla, configuration.NomDe(idNeuf));    // l'acteur ajouté est résolu par son nom sur l'id généré
        Assert.Equal(Rose, configuration.CouleurDe(idNeuf)); // ... et par sa couleur fournie
    }

    // ---------- Test #2 — Driver : l'identifiant est OPAQUE, distinct du libellé et des seeds ----------
    // Contradiction : l'impl minimale du #1 prend le raccourci « id = nom » (libellé-comme-identité,
    // anti-pattern corrigé au s06) : l'id serait « Carla ». Force un identifiant OPAQUE généré, ≠ libellé
    // ET ≠ les ids des seeds (parent-a, parent-b, grand-pere).
    [Fact]
    public void Should_Porter_un_identifiant_opaque_distinct_du_libelle_et_des_acteurs_deja_presents_When_une_actrice_est_ajoutee_au_foyer()
    {
        var configuration = new FakeConfigurationFoyer(new Dictionary<string, string>());
        var handler = new AjouterActeurHandler(configuration);

        var idNeuf = handler.Handle(new AjouterActeurCommand(Carla, Rose)).Valeur!.ActeurId;

        Assert.NotEqual(Carla, idNeuf);          // identifiant opaque, jamais le libellé (anti-pattern s06)
        Assert.DoesNotContain(idNeuf, IdsSeeds); // ... ni l'identifiant d'un acteur déjà présent
    }

    // ---------- Test #3 — Driver : l'acteur ajouté est restitué par l'énumération du store ----------
    // Contradiction : aucun accès de lecture d'énumération n'existe — l'écran liste une liste statique
    // front (Foyer.ActeursEditables) qui ignore les ajouts. Force un accès d'énumération SUR LE STORE
    // restituant les acteurs dont l'ajouté (sur son id neuf), aux côtés des seeds.
    [Fact]
    public void Should_Restituer_l_acteur_ajoute_parmi_les_acteurs_enumeres_du_foyer_When_l_ecran_de_configuration_enumere_les_acteurs_depuis_le_store()
    {
        var store = new ConfigurationFoyerEnMemoire();
        const string idNeuf = "acteur-neuf-test";

        store.Ajouter(idNeuf, Carla, Rose);

        var enumeres = store.EnumererActeurs();
        Assert.Contains(idNeuf, enumeres);        // l'acteur ajouté est énuméré DEPUIS LE STORE
        Assert.Contains("parent-a", enumeres);    // ... aux côtés des seeds (énumération complète du foyer)
        Assert.Equal(Carla, store.NomDe(idNeuf)); // ... résolu par son nom sur l'id neuf
    }
}
