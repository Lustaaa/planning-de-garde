using Microsoft.AspNetCore.SignalR;
using PlanningDeGarde.Application;
using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Infrastructure;

/// <summary>
/// Adaptateur réel du port <see cref="INotificateurChangement"/> : diffuse le PAYLOAD (snapshot d'un
/// changement / d'une proposition DÉJÀ écrit) à tous les clients via le hub SignalR. Le client reprojette sa
/// cloche depuis ce payload SANS aucun GET sur push (garde-fou anti-flake). Lecture seule : la donnée
/// transportée est une trace de lecture ; l'écriture reste exclusivement sur le canal requête/réponse.
/// </summary>
public sealed class SignalRNotificateurChangement : INotificateurChangement
{
    private readonly IHubContext<PlanningHub> _hub;

    public SignalRNotificateurChangement(IHubContext<PlanningHub> hub) => _hub = hub;

    public void NotifierChangement(EvenementChangementSnapshot evenement)
        => _hub.Clients.All.SendAsync(PlanningHub.EvenementChangement, evenement).GetAwaiter().GetResult();

    public void NotifierProposition(PropositionEchangeSnapshot proposition)
        => _hub.Clients.All.SendAsync(PlanningHub.EvenementProposition, proposition).GetAwaiter().GetResult();
}
