using System;

namespace PlanningDeGarde.Domain;

/// <summary>Cycle de vie d'une proposition d'échange de dernière minute (consentement du recevant).</summary>
public enum StatutProposition
{
    /// <summary>En attente : notifiée au recevant, AUCUNE écriture de surcharge tant que non acceptée.</summary>
    Proposee,

    /// <summary>Acceptée : le consentement du recevant a déclenché la composition de la délégation s44.</summary>
    Acceptee,

    /// <summary>Refusée : retirée sans aucune écriture.</summary>
    Refusee,
}

/// <summary>Snapshot immuable d'une proposition d'échange — frontière publique (assertions / persistance).</summary>
public sealed record PropositionEchangeSnapshot(
    string Id, DateOnly Jour, string EnfantId, string DeActeurId, string VersActeurId, StatutProposition Statut);

/// <summary>
/// Agrégat « échange consenti » (s47) : un cédant PROPOSE de confier le jour <c>Jour</c> (enfant
/// <c>EnfantId</c>) au recevant <c>VersActeurId</c>. Contrairement à la délégation s44 (unilatérale, effet
/// immédiat), la proposition n'a AUCUN effet sur la résolution tant qu'elle est <see cref="StatutProposition.Proposee"/> :
/// c'est le consentement du recevant (<see cref="Accepter"/>) qui déclenche l'écriture (composition s44 côté
/// use case). Invariant : recevant renseigné et DISTINCT du cédant (proposer à soi-même est refusé — le
/// recevant est déjà le responsable résolu, la proposition n'apporterait aucun changement).
/// </summary>
public sealed class PropositionEchange
{
    private PropositionEchange(
        string id, DateOnly jour, string enfantId, string deActeurId, string versActeurId, StatutProposition statut)
    {
        Id = id;
        Jour = jour;
        EnfantId = enfantId;
        DeActeurId = deActeurId;
        VersActeurId = versActeurId;
        Statut = statut;
    }

    public string Id { get; }
    public DateOnly Jour { get; }
    public string EnfantId { get; }
    public string DeActeurId { get; }
    public string VersActeurId { get; }
    public StatutProposition Statut { get; private set; }

    public static Result<PropositionEchange> Proposer(DateOnly jour, string enfantId, string deActeurId, string versActeurId)
    {
        if (string.IsNullOrWhiteSpace(versActeurId))
            return Result<PropositionEchange>.Echec("Échange : le recevant est requis.");
        if (versActeurId == deActeurId)
            return Result<PropositionEchange>.Echec(
                "Échange à soi-même : cet acteur récupère déjà ce jour-là, aucun changement n'est nécessaire.");

        return Result<PropositionEchange>.Succes(
            new PropositionEchange(Guid.NewGuid().ToString("N"), jour, enfantId, deActeurId, versActeurId, StatutProposition.Proposee));
    }

    /// <summary>Reconstitue un agrégat depuis son snapshot persisté (relecture store).</summary>
    public static PropositionEchange FromSnapshot(PropositionEchangeSnapshot s)
        => new(s.Id, s.Jour, s.EnfantId, s.DeActeurId, s.VersActeurId, s.Statut);

    /// <summary>Le recevant CONSENT : la proposition passe à « accepté » (l'écriture s44 est composée côté use case).</summary>
    public void Accepter() => Statut = StatutProposition.Acceptee;

    /// <summary>Le recevant décline : la proposition passe à « refusé », aucune écriture.</summary>
    public void Refuser() => Statut = StatutProposition.Refusee;

    public PropositionEchangeSnapshot ToSnapshot() => new(Id, Jour, EnfantId, DeActeurId, VersActeurId, Statut);
}
