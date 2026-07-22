using System;

namespace PlanningDeGarde.Domain;

/// <summary>Cycle de vie d'une proposition d'échange de dernière minute (consentement du recevant).</summary>
public enum StatutProposition
{
    /// <summary>En attente : notifiée au recevant, AUCUNE écriture de surcharge tant que non acceptée.</summary>
    Proposee,

    /// <summary>Acceptée : le consentement du recevant a déclenché la composition de la délégation.</summary>
    Acceptee,

    /// <summary>Refusée : retirée sans aucune écriture.</summary>
    Refusee,
}

/// <summary>Snapshot immuable d'une proposition d'échange — frontière publique (assertions / persistance).
/// <c>Jour</c> = début de plage, <c>JourFin</c> = fin INCLUSE ; <c>JourFin == Jour</c> = échange d'UN jour (parité).</summary>
public sealed record PropositionEchangeSnapshot(
    string Id, DateOnly Jour, string EnfantId, string DeActeurId, string VersActeurId, StatutProposition Statut, DateOnly JourFin);

/// <summary>
/// Agrégat « échange consenti » : un cédant PROPOSE de confier le jour <c>Jour</c> (enfant
/// <c>EnfantId</c>) au recevant <c>VersActeurId</c>. Contrairement à la délégation (unilatérale, effet
/// immédiat), la proposition n'a AUCUN effet sur la résolution tant qu'elle est <see cref="StatutProposition.Proposee"/> :
/// c'est le consentement du recevant (<see cref="Accepter"/>) qui déclenche l'écriture (composition côté
/// use case). Invariant : recevant renseigné et DISTINCT du cédant (proposer à soi-même est refusé — le
/// recevant est déjà le responsable résolu, la proposition n'apporterait aucun changement).
/// </summary>
public sealed class PropositionEchange
{
    private PropositionEchange(
        string id, DateOnly jour, DateOnly jourFin, string enfantId, string deActeurId, string versActeurId, StatutProposition statut)
    {
        Id = id;
        Jour = jour;
        JourFin = jourFin;
        EnfantId = enfantId;
        DeActeurId = deActeurId;
        VersActeurId = versActeurId;
        Statut = statut;
    }

    public string Id { get; }
    public DateOnly Jour { get; }

    /// <summary>Fin de plage INCLUSE. Égale à <see cref="Jour"/> pour un échange d'UN jour (parité).</summary>
    public DateOnly JourFin { get; }
    public string EnfantId { get; }
    public string DeActeurId { get; }
    public string VersActeurId { get; }
    public StatutProposition Statut { get; private set; }

    /// <summary>Propose un échange sur la plage <c>[jour.jourFin]</c>. <paramref name="jourFin"/> absent
    /// (null) = plage réduite à UN jour (parité). La borne <c>fin &lt; début</c> (plage vide) est refusée
    /// AVANT toute écriture (règle dans l'agrégat), au même titre que le recevant vide ou l'échange à soi-même.</summary>
    public static Result<PropositionEchange> Proposer(
        DateOnly jour, string enfantId, string deActeurId, string versActeurId, DateOnly? jourFin = null)
    {
        if (string.IsNullOrWhiteSpace(versActeurId))
            return Result<PropositionEchange>.Echec("Échange : le recevant est requis.");
        if (versActeurId == deActeurId)
            return Result<PropositionEchange>.Echec(
                "Échange à soi-même : cet acteur récupère déjà ce jour-là, aucun changement n'est nécessaire.");

        var fin = jourFin ?? jour;
        if (fin < jour)
            return Result<PropositionEchange>.Echec(
                "Échange : la fin de plage est antérieure au début (plage vide).");

        return Result<PropositionEchange>.Succes(
            new PropositionEchange(Guid.NewGuid().ToString("N"), jour, fin, enfantId, deActeurId, versActeurId, StatutProposition.Proposee));
    }

    /// <summary>Reconstitue un agrégat depuis son snapshot persisté (relecture store).</summary>
    public static PropositionEchange FromSnapshot(PropositionEchangeSnapshot s)
        => new(s.Id, s.Jour, s.JourFin, s.EnfantId, s.DeActeurId, s.VersActeurId, s.Statut);

    /// <summary>Le recevant CONSENT : la proposition passe à « accepté » (l'écriture est composée côté use case).</summary>
    public void Accepter() => Statut = StatutProposition.Acceptee;

    /// <summary>Le recevant décline : la proposition passe à « refusé », aucune écriture.</summary>
    public void Refuser() => Statut = StatutProposition.Refusee;

    public PropositionEchangeSnapshot ToSnapshot() => new(Id, Jour, EnfantId, DeActeurId, VersActeurId, Statut, JourFin);
}
