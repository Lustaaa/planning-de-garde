using System;
using System.Collections.Generic;

namespace PlanningDeGarde.Web;

/// <summary>Vue Web d'une activité récurrente d'un enfant (miroir du read model <c>ActiviteRecurrenteVue</c> :
/// id + lieu résolu + set de jours + plage horaire + plages d'exclusion). <b>Partagée</b> entre l'écran de
/// configuration (liste par enfant) et la dialog d'édition de série réutilisée depuis la grille (passe
/// architecte post-s54 : la dialog d'édition d'une série devient un composant partagé hors /configuration).</summary>
public sealed record ActiviteRecurrenteVueWeb(
    string Id, string LieuId, string ActiviteLibelle, List<DayOfWeek> Jours, TimeSpan HeureDebut, TimeSpan HeureFin,
    List<PlageVacancesWeb> Exclusions);

/// <summary>Vue Web d'une plage d'exclusion (vacances) — bornes calendaires incluses.</summary>
public sealed record PlageVacancesWeb(DateOnly Debut, DateOnly Fin);
