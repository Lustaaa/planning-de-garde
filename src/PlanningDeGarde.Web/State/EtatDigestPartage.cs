using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.State;

/// <summary>
/// État CLIENT partagé (scoped = par circuit Blazor) du digest « immédiat » de la cloche (s50). La grille
/// <see cref="PlanningDeGarde.Web.Components.Pages.PlanningPartage"/> — seule à effectuer le GET grille —
/// PUBLIE ici le digest REPROJETÉ de la fenêtre déjà chargée (composition pure
/// <see cref="DigestImmediat.Composer"/>) à chaque (re)chargement / navigation / convergence temps réel de la
/// grille. La <see cref="PlanningDeGarde.Web.Components.Cloche"/>, rendue à part dans la barre d'application,
/// s'y ABONNE et rend la section digest — SANS jamais émettre de GET (ni dédié, ni sur push) : le digest est
/// une reprojection de la donnée déjà chargée par le GET grille (Sc.6/Sc.7, garde-fou anti-flake). Canal de
/// LECTURE stricte : aucune écriture ne transite par cet état.
/// </summary>
public sealed class EtatDigestPartage
{
    /// <summary>Dernier digest publié par la grille chargée. Vide neutre tant qu'aucune grille n'a été chargée.</summary>
    public DigestImmediat Digest { get; private set; } = DigestImmediat.Vide;

    /// <summary>Émis à chaque publication : la cloche s'y abonne pour re-rendre la section digest.</summary>
    public event Action? Change;

    /// <summary>Publie le digest reprojeté de la fenêtre de grille chargée et notifie les abonnés (la cloche).</summary>
    public void Publier(DigestImmediat digest)
    {
        Digest = digest;
        Change?.Invoke();
    }
}
