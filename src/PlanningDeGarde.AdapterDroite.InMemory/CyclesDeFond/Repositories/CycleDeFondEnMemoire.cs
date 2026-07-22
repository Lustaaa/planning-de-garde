using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.AdapterDroite.InMemory.CyclesDeFond.Repositories;

/// <summary>
/// Adaptateur InMemory singleton du port <see cref="IReferentielCycleDeFond"/> : conserve le
/// cycle de fond courant en mémoire (volatile — durabilité portée par un palier ultérieur,
/// borne anti-cliquet, PAS Mongo). Source de vérité partagée du foyer ; une nouvelle
/// définition écrase la précédente (dernière écriture gagne, sans version ni rejet).
/// </summary>
public sealed class CycleDeFondEnMemoire : IReferentielCycleDeFond
{
    // Cycle par enfant (s53) — clé = EnfantId ; clé "" = cycle legacy mono-enfant (lu SEULEMENT par enfantId=null).
    private readonly System.Collections.Generic.Dictionary<string, CycleDeFond> _cycles = new();

    // ISOLATION STRICTE s53 (gate G3 4e passage) : la résolution d'un enfant NON-NULL ne voit QUE SON cycle —
    // AUCUN repli sur le bucket partagé "" (c'était la fuite : « Charlie » affichait le cycle global). Un enfant
    // sans cycle propre → null → NEUTRE (repli s13). enfantId null = chemin legacy mono-enfant (lit "").
    public CycleDeFond? CycleCourant(string? enfantId = null)
        => _cycles.TryGetValue(enfantId ?? "", out var cycle) ? cycle : null;

    public void DefinirCycle(CycleDeFond cycle, string? enfantId = null) => _cycles[enfantId ?? ""] = cycle;
}
