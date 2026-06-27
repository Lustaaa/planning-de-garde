using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Commande de définition / ré-édition du cycle de fond depuis la configuration du foyer :
/// le nombre de semaines et le mapping index→responsable (identifiant stable). Une nouvelle
/// définition remplace intégralement le cycle courant (dernière écriture gagne).
/// </summary>
public sealed record DefinirCycleCommand(int NombreSemaines, IReadOnlyDictionary<int, string> Affectations);

/// <summary>
/// Use case : définir le cycle de fond. Persiste le cycle via le port d'écriture puis déclenche
/// la diffusion temps réel sur succès — jamais d'écriture par le canal de diffusion.
/// </summary>
public sealed class DefinirCycleHandler
{
    private readonly IReferentielCycleDeFond _cycle;
    private readonly INotificateurPlanning _notificateur;

    public DefinirCycleHandler(IReferentielCycleDeFond cycle, INotificateurPlanning notificateur)
    {
        _cycle = cycle;
        _notificateur = notificateur;
    }

    public Result<CycleDeFond> Handle(DefinirCycleCommand commande)
    {
        // Garde conditionnelle N ≥ 1 : un cycle de zéro semaine est insensé (ISOWeek mod 0 indéfini).
        // Refus AVANT toute écriture/diffusion → le cycle précédent reste inchangé. Le nominal N ≥ 1
        // (ex. N=2 défini dès Sc.1) reste accepté : la garde ne vise que N < 1.
        if (commande.NombreSemaines < 1)
            return Result<CycleDeFond>.Echec("le cycle doit compter au moins une semaine");

        var cycle = new CycleDeFond(commande.NombreSemaines, commande.Affectations);
        _cycle.DefinirCycle(cycle);
        _notificateur.NotifierMiseAJour(); // diffusion temps réel sur écriture aboutie
        return Result<CycleDeFond>.Succes(cycle);
    }
}
