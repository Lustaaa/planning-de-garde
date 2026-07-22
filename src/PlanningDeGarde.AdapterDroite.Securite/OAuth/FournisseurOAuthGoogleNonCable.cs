using PlanningDeGarde.Application.Comptes.Ports;

namespace PlanningDeGarde.AdapterDroite.Securite;

/// <summary>
/// Réalisation DI du port <see cref="IFournisseurOAuth"/> rendant
/// <c>ConnexionOAuthHandler</c> RÉSOLVABLE et le callback OAuth routable — <b>SANS</b> réaliser l'échange
/// OAuth Google réel (client_secret / token endpoint / résolution de l'email vérifié). C'est une
/// <b>DETTE DE CÂBLAGE assumée</b> (backlog P0, vérifiée MANUELLEMENT au G3, non testable en runtime
/// local) : <see cref="ResoudreIdentite"/> renvoie <c>null</c> tant que l'adaptateur Google réel n'est
/// pas branché — un vrai callback est donc refusé (aucune identité résolue). La LOGIQUE de rapprochement
/// compte local ↔ identité externe est prouvée par doublure à la frontière Application.
/// </summary>
public sealed class FournisseurOAuthGoogleNonCable : IFournisseurOAuth
{
    public IdentiteExterne? ResoudreIdentite(string callback) => null;
}
