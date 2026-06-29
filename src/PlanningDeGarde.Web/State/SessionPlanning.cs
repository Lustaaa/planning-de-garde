using System;
using System.Collections.Generic;
using System.Linq;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.State;

/// <summary>
/// Contexte de la session de consultation (scoped = par circuit Blazor). Distingue une <b>identité
/// réelle</b> (fixe = l'utilisateur principal, le configurateur, de type Parent) d'une <b>identité
/// effective</b> (l'acteur <b>incarné</b> s'il y en a un, sinon <b>repli sur l'identité réelle</b>) —
/// impersonation bornée, lecture seule (sprint 14). <see cref="EstParent"/> dérive désormais de
/// l'identité EFFECTIVE (règle 8) : un acteur incarné de type Parent/Admin conserve les actions
/// d'écriture, un acteur de type Autre les masque. Le sélecteur de rôle démo (<see cref="Role"/>
/// Parent/Invité) reste honoré <b>en plus</b> : un Invité reste en consultation seule quelle que soit
/// l'identité effective. L'incarnation est <b>mémoire / session uniquement</b> — zéro persistance
/// neuve (borne anti-cliquet règle 30).
/// </summary>
public sealed class SessionPlanning
{
    /// <summary>Rôle démo (Parent / Invité). Conservé pour le bascule de consultation seule existant ;
    /// composé avec le type de l'identité effective pour calculer <see cref="EstParent"/>.</summary>
    public RoleAuteur Role { get; set; } = RoleAuteur.Parent;

    public string EnfantId { get; set; } = "Léa";

    // --- État de navigation du calendrier (Sc.1) : ancre + vue en SESSION/MÉMOIRE, NE PERSISTE PAS au
    //     redémarrage (distinct de la persistance domaine du bloc C). La navigation ne fait que
    //     re-projeter — lecture seule, aucune écriture. ---

    private DateOnly? _ancre;

    /// <summary>Vue prédéfinie courante de la fenêtre projetée (défaut = 4 semaines glissantes, Sc.3).
    /// En session/mémoire — ne persiste pas. Base réutilisable du sélecteur de vue.</summary>
    public VuePlanning Vue { get; set; } = VuePlanning.QuatreSemaines;

    /// <summary>Initialise l'ancre courante au <b>lundi de la semaine</b> de <paramref name="aujourdHui"/>
    /// si elle ne l'est pas encore (première lecture via l'horloge). Idempotent : une ancre déjà décalée
    /// par la navigation est conservée (l'état de navigation survit aux re-rendus dans la session).</summary>
    public void InitialiserAncre(DateOnly aujourdHui) => _ancre ??= LundiDeLaSemaine(aujourdHui);

    /// <summary>Ancre courante de la fenêtre (lundi de semaine). Non nulle après
    /// <see cref="InitialiserAncre"/>.</summary>
    public DateOnly Ancre => _ancre
        ?? throw new InvalidOperationException("Ancre de navigation non initialisée : appeler InitialiserAncre d'abord.");

    /// <summary>Avance l'ancre d'une semaine (Sc.1, « Semaine suivante ») : +7 jours, l'ancre reste un
    /// lundi. Re-projection pure : la grille se re-résout à la date naviguée, aucune écriture émise.</summary>
    public void SemaineSuivante() => _ancre = Ancre.AddDays(7);

    /// <summary>Recule l'ancre d'une semaine (Sc.1, « Semaine précédente ») : −7 jours, l'ancre reste un
    /// lundi. Re-projection pure (lecture seule).</summary>
    public void SemainePrecedente() => _ancre = Ancre.AddDays(-7);

    /// <summary>Restaure l'ancre à une valeur antérieurement capturée (Sc.6) : après une navigation dont la
    /// re-requête de l'API distante a échoué, l'ancre est ramenée à celle de la fenêtre affichée — l'état de
    /// navigation et l'affichage ne divergent pas, et la navigation échouée n'est ni mise en file ni rejouée.</summary>
    public void RestaurerAncre(DateOnly ancre) => _ancre = ancre;

    /// <summary>Réinitialise l'ancre à la <b>semaine en cours</b> (Sc.4, « Aujourd'hui ») : l'ancre
    /// re-cale sur le lundi de <paramref name="aujourdHui"/> (date du jour via le port d'horloge,
    /// jamais <c>DateTime.Now</c>), quel que soit le décalage de navigation accumulé. Re-projection
    /// pure : la grille se re-résout à la semaine en cours, aucune écriture émise.</summary>
    public void RevenirAujourdhui(DateOnly aujourdHui) => _ancre = LundiDeLaSemaine(aujourdHui);

    /// <summary>Lundi (ISO) de la semaine de la date donnée — l'ancre de navigation est toujours un début
    /// de semaine, comme la fenêtre projetée. Logique de présentation (quelle semaine afficher), pas une
    /// règle métier.</summary>
    private static DateOnly LundiDeLaSemaine(DateOnly date)
    {
        var deltaDepuisLundi = ((int)date.DayOfWeek + 6) % 7; // lundi = 0
        return date.AddDays(-deltaDepuisLundi);
    }

    /// <summary>Identité réelle = l'utilisateur principal (le configurateur), fixe et de type Parent.
    /// C'est l'état de repli quand aucune incarnation n'est active.</summary>
    public IdentiteActeur IdentiteReelle { get; } =
        new("configurateur", "le configurateur", TypeActeur.Parent);

    /// <summary>Identité effective = l'acteur incarné s'il y en a un, sinon l'identité réelle.</summary>
    public IdentiteActeur IdentiteEffective { get; private set; }

    public SessionPlanning() => IdentiteEffective = IdentiteReelle;

    /// <summary>Acteurs incarnables, alimentés par la page depuis le <b>référentiel réel</b> (canal de
    /// lecture HTTP, avec le type surfacé read-only). C'est ce catalogue que résout <see cref="Incarner"/>
    /// (refus silencieux si l'identifiant est absent — Sc.3).</summary>
    public IReadOnlyList<IdentiteActeur> ActeursIncarnables { get; set; } =
        Array.Empty<IdentiteActeur>();

    /// <summary>Vrai si une incarnation est active (l'identité effective n'est plus l'identité réelle).</summary>
    public bool IncarnationActive => IdentiteEffective.Id != IdentiteReelle.Id;

    /// <summary>Libellé du bandeau d'incarnation (« Vous incarnez X »), ou <c>null</c> hors incarnation.</summary>
    public string? LibelleBandeau =>
        IncarnationActive ? $"Vous incarnez {IdentiteEffective.Nom}" : null;

    /// <summary>
    /// Droit d'écriture (règle 9) dérivé de l'identité EFFECTIVE (règle 8) : vrai si son type ∈
    /// {Parent, Admin}, faux si Autre. Composé avec le sélecteur de rôle démo : un Invité reste en
    /// consultation seule. Hors incarnation, l'effective EST la réelle (Parent) → comportement antérieur
    /// préservé (<see cref="Role"/> seul décide).
    /// </summary>
    public bool EstParent =>
        Role == RoleAuteur.Parent
        && IdentiteEffective.Type is TypeActeur.Parent or TypeActeur.Admin;

    /// <summary>
    /// Incarne l'acteur d'identifiant stable <paramref name="acteurId"/> : l'identité effective devient
    /// celle de l'acteur (id, nom, type lu read-only). <b>Refus silencieux</b> si l'identifiant est
    /// absent du catalogue (acteur inconnu / supprimé — Sc.3) : l'identité réelle est conservée. La
    /// résolution se fait sur l'identifiant stable, jamais sur le libellé (règles 5/19).
    /// </summary>
    public void Incarner(string acteurId)
    {
        var acteur = ActeursIncarnables.FirstOrDefault(a => a.Id == acteurId);
        if (acteur is null)
            return; // refus silencieux : identifiant inconnu, identité réelle conservée
        IdentiteEffective = acteur;
    }

    /// <summary>Revient à l'identité réelle : l'incarnation est levée, l'état restauré (Sc.2).</summary>
    public void RevenirIdentiteReelle() => IdentiteEffective = IdentiteReelle;

    /// <summary>
    /// Repli automatique sur l'identité réelle (D2, projection des règles 6/18/19) : si une incarnation est
    /// active mais que l'acteur incarné <b>n'est plus présent</b> dans le catalogue
    /// (<see cref="ActeursIncarnables"/>, rafraîchi depuis le référentiel réel) — typiquement supprimé
    /// concurremment depuis un autre écran —, la référence orpheline cesse de primer → retour à l'identité
    /// réelle, sans nom fantôme. Hors incarnation, ou si l'acteur incarné existe toujours, no-op. Retourne
    /// <c>true</c> si un repli a eu lieu (l'appelant peut re-rendre).
    /// </summary>
    public bool ReplierSiActeurIncarneAbsent()
    {
        if (IncarnationActive && !ActeursIncarnables.Any(a => a.Id == IdentiteEffective.Id))
        {
            RevenirIdentiteReelle();
            return true;
        }
        return false;
    }
}

/// <summary>Une identité d'acteur du foyer : identifiant stable + nom d'affichage + type
/// (Admin / Parent / Autre). Sert l'identité réelle, l'identité effective et le catalogue incarnable.</summary>
public sealed record IdentiteActeur(string Id, string Nom, TypeActeur Type);
