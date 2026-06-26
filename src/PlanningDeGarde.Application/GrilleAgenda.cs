using System;
using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Read model (CQRS) de la grille agenda du hub /planning : une fenêtre de 35 jours datés
/// (5 semaines × 7 jours) à partir de la semaine en cours. Lecture seule.
/// </summary>
public sealed record GrilleAgenda(
    IReadOnlyList<JourCase> Jours,
    IReadOnlyList<SemaineLigne> Semaines);

/// <summary>
/// Une case-jour de la grille (axe vertical : une ligne par semaine), portant la couleur
/// du parent responsable de la période qui couvre ce jour (ou la couleur neutre si aucune
/// période ne le couvre) et les slots rattachés à sa date.
/// </summary>
public sealed record JourCase(DateOnly Date, string CouleurResponsable, IReadOnlyList<SlotCase> Slots);

/// <summary>
/// Un slot positionné dans sa case-jour : libellé (lieu/acteur), bornes horaires de la journée
/// et couleur propre de son acteur (deuxième niveau de couleur, distinct de la couleur de la
/// case-jour : la case porte la responsabilité, le créneau porte l'acteur).
/// </summary>
public sealed record SlotCase(string Libelle, TimeOnly Debut, TimeOnly Fin, string CouleurActeur);

/// <summary>Une ligne-semaine : 7 cases-jour consécutives, du lundi au dimanche.</summary>
public sealed record SemaineLigne(IReadOnlyList<JourCase> Jours);
