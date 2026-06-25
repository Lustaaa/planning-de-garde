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
/// Une case-jour de la grille (axe vertical : une ligne par semaine), portant les slots
/// rattachés à sa date.
/// </summary>
public sealed record JourCase(DateOnly Date, IReadOnlyList<SlotCase> Slots);

/// <summary>
/// Un slot positionné dans sa case-jour : libellé (lieu) et bornes horaires de la journée.
/// (La couleur acteur arrive au scénario 4 — non portée ici.)
/// </summary>
public sealed record SlotCase(string Libelle, TimeOnly Debut, TimeOnly Fin);

/// <summary>Une ligne-semaine : 7 cases-jour consécutives, du lundi au dimanche.</summary>
public sealed record SemaineLigne(IReadOnlyList<JourCase> Jours);
