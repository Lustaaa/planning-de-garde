using PlanningDeGarde.Application;

namespace PlanningDeGarde.Tests.Fakes;

/// <summary>
/// Doublure à la main du port <see cref="IFournisseurOAuth"/> (volet 4 s25). Le câblage provider réel
/// (Google/Microsoft/Apple : secrets, callbacks, redirections) n'est PAS testable en runtime local
/// (entorse assumée au gate G2) : on prouve la logique Application/frontière contre CETTE doublure, qui
/// court-circuite le protocole OAuth et restitue directement l'identité externe programmée (ou aucune,
/// pour le cas « identité inconnue » Sc.15). Le câblage réel est vérifié MANUELLEMENT au G3.
/// </summary>
public sealed class FakeFournisseurOAuth : IFournisseurOAuth
{
    private readonly IdentiteExterne? _identite;

    public FakeFournisseurOAuth(IdentiteExterne? identite) => _identite = identite;

    public IdentiteExterne? ResoudreIdentite(string callback) => _identite;
}
