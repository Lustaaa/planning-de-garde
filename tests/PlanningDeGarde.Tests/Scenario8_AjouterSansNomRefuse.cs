using PlanningDeGarde.Application;
using PlanningDeGarde.Infrastructure;
using Xunit;

namespace PlanningDeGarde.Tests;

// Sprint 09 — Sc.8 — Ajouter un acteur sans nom est refusé (@erreur, 🖥️ IHM)
//   Tranche BACKEND (tdd-auto) : garde CONDITIONNELLE « nom non vide » sur le handler d'ajout neuf
//   (AjouterActeurHandler, Sc.1). Le nominal d'ajout (Sc.1) est déjà vert → la garde ne refuse QUE
//   le nom fourni vide / tout-espaces, jamais inconditionnellement (sinon régression Sc.1). Sur
//   refus : AUCUN identifiant généré, store NON muté (aucun acteur fantôme), liste inchangée, motif
//   métier clair (« le nom ne peut pas être vide », réutilisé d'EditerActeurHandler).
//   L'acceptation RUNTIME (message clair affiché à l'écran de config réellement câblé) est menée
//   séparément par ihm-builder — on NE teste PAS ici un rendu Blazor.
public class Scenario8_AjouterSansNomRefuse
{
    private const string Rose = "rose";
    private const string MotifNomVide = "le nom ne peut pas être vide";

    // ---------- Test #1 — Driver : un nom vide est refusé, sans id généré ni acteur fantôme ----------
    // Contradiction : le handler d'ajout (Sc.1) génère un id et persiste TOUT nom, y compris « "" » :
    // une chaîne vide créerait un acteur fantôme énuméré sans nom utile. Force la garde « nom non vide »
    // qui refuse (Result.Echec porteur du motif métier), AVANT toute génération d'id et toute mutation
    // du store (liste inchangée).
    [Fact]
    public void Should_Refuser_l_ajout_avec_un_motif_clair_sans_generer_d_identifiant_ni_modifier_la_liste_When_le_nom_demande_est_une_chaine_vide()
    {
        var store = new ConfigurationFoyerEnMemoire();
        var handler = new AjouterActeurHandler(store);
        var nombreAvant = store.EnumererActeurs().Count;

        var resultat = handler.Handle(new AjouterActeurCommand("", Rose));

        Assert.False(resultat.EstSucces);                          // ajout refusé
        Assert.Equal(MotifNomVide, resultat.Motif);                // motif métier clair (jamais technique)
        Assert.Null(resultat.Valeur);                              // AUCUN identifiant généré
        Assert.Equal(nombreAvant, store.EnumererActeurs().Count);  // liste inchangée (aucun acteur fantôme)
    }

    // ---------- Test #2 — Driver : un nom tout-espaces est refusé (garde sur le nom UTILE) ----------
    // Contradiction : la garde minimale du #1 (« chaîne vide » via IsNullOrEmpty) laisse PASSER un nom
    // tout-espaces (« "   " » ≠ ""), qui créerait un acteur sans nom utile. Force la garde sur le nom
    // UTILE (espaces ignorés, à la EditerActeurHandler:38), contredisant l'impl minimale du #1.
    [Fact]
    public void Should_Refuser_l_ajout_et_laisser_la_liste_inchangee_When_le_nom_demande_ne_contient_que_des_espaces()
    {
        var store = new ConfigurationFoyerEnMemoire();
        var handler = new AjouterActeurHandler(store);
        var nombreAvant = store.EnumererActeurs().Count;

        var resultat = handler.Handle(new AjouterActeurCommand("   ", Rose));

        Assert.False(resultat.EstSucces);                          // ajout refusé sur nom non utile
        Assert.Equal(MotifNomVide, resultat.Motif);                // même motif métier que le nom vide
        Assert.Null(resultat.Valeur);                              // AUCUN identifiant généré
        Assert.Equal(nombreAvant, store.EnumererActeurs().Count);  // liste inchangée (aucun acteur fantôme)
    }

    // ---------- Test #3 — Caractérisation : un ajout refusé ne diffuse rien (aucun effet de bord) ----------
    // Filet de non-régression (early green attendu, couvert par #1/#2) : le handler d'ajout retourne le
    // refus AVANT toute mutation. Le handler ne diffuse pas par lui-même (un acteur ajouté sans période
    // ne change aucune case — la grille ne bouge pas ; le refus n'a donc rien à diffuser). L'observable
    // du « rien diffusé » est l'ABSENCE D'EFFET DE BORD AVAL : le store / l'énumération restent
    // strictement inchangés (aucune entrée fantôme injectée), aucun id émis. Verrouille « pas de
    // diffusion / pas d'effet de bord sur ajout refusé » contre une régression de la garde.
    [Fact]
    public void Should_Ne_declencher_aucune_diffusion_temps_reel_When_un_ajout_est_refuse()
    {
        var store = new ConfigurationFoyerEnMemoire();
        var handler = new AjouterActeurHandler(store);
        var enumAvant = store.EnumererActeurs();

        var resultat = handler.Handle(new AjouterActeurCommand("", null));

        Assert.False(resultat.EstSucces);                                // ajout refusé
        Assert.Null(resultat.Valeur);                                    // aucun id émis (rien à diffuser)
        Assert.Equal(enumAvant.Count, store.EnumererActeurs().Count);    // aucun effet de bord aval : énumération inchangée
        Assert.Equal(enumAvant, store.EnumererActeurs());                // ... exactement les mêmes acteurs (aucune entrée fantôme)
    }
}
