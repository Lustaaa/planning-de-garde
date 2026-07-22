using System.Collections.Generic;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application.Notifications.Services;

/// <summary>
/// Décorateur du port <see cref="IJournalChangements"/> : consigne l'événement au journal RÉEL (persistance)
/// PUIS le DIFFUSE via <see cref="INotificateurChangement"/> (payload). Ainsi CHAQUE handler d'écriture qui consigne
/// déjà au journal (délégation / plage / reprise / transfert) diffuse l'événement EXACT qu'il vient
/// d'écrire — SANS modifier aucun handler (composition de ports, pas d'ajout de dépendance dans les use cases). La
/// diffusion suit une écriture aboutie (la consignation), jamais l'inverse. Trace de lecture : la donnée
/// diffusée n'est jamais autorité de résolution.
/// </summary>
public sealed class JournalChangementsDiffusant : IJournalChangements
{
    private readonly IJournalChangements _journal;
    private readonly INotificateurChangement _notificateur;

    public JournalChangementsDiffusant(IJournalChangements journal, INotificateurChangement notificateur)
    {
        _journal = journal;
        _notificateur = notificateur;
    }

    public void Consigner(EvenementChangementSnapshot evenement)
    {
        _journal.Consigner(evenement);
        _notificateur.NotifierChangement(evenement);
    }

    public IReadOnlyList<EvenementChangementSnapshot> Tout() => _journal.Tout();
}
