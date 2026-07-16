using PlanningDeGarde.Domain;

namespace PlanningDeGarde.Application;

/// <summary>
/// Port de DIFFUSION porteuse de PAYLOAD de la cloche (s47, décision transport SM) : la diffusion temps réel
/// porte l'événement de LECTURE (snapshot d'un changement DÉJÀ écrit) pour que le client REPROJETTE sa cloche
/// SANS aucun GET sur le push (garde-fou anti-flake — nouveau client SignalR = reprojection depuis la diffusion,
/// jamais un GET). Ça ne viole PAS « diffusion = lecture seule » : la donnée transportée est une trace de
/// lecture, l'écriture reste exclusivement sur le canal requête/réponse. Donnée derrière un port (2 adaptateurs :
/// SignalR réel + doublure de test). Distinct de <see cref="INotificateurPlanning"/> (signal sans payload « quelque
/// chose a changé » que la grille recharge) : ici la donnée VOYAGE dans la diffusion.
/// </summary>
public interface INotificateurChangement
{
    /// <summary>Diffuse l'événement du journal (délégation / reprise / transfert) qui vient d'être consigné :
    /// le client concerné l'ajoute en tête de sa cloche et incrémente son compteur (Sc.4), 0 GET.</summary>
    void NotifierChangement(EvenementChangementSnapshot evenement);

    /// <summary>Diffuse une proposition d'échange (créée / acceptée / refusée) : le client destinataire /
    /// émetteur reprojette sa cloche (notification actionnable apparue / statut mis à jour / retirée), 0 GET (Sc.9).</summary>
    void NotifierProposition(PropositionEchangeSnapshot proposition);
}
