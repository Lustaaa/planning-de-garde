using System;
using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Read model (CQRS) de la grille agenda du hub /planning : une fenêtre de 35 jours datés
/// (5 semaines × 7 jours) à partir de la semaine en cours, et la <see cref="Légende"/> des
/// responsables présents dans cette fenêtre. Lecture seule.
/// </summary>
public sealed record GrilleAgenda(
    IReadOnlyList<JourCase> Jours,
    IReadOnlyList<SemaineLigne> Semaines,
    IReadOnlyList<EntreeLegende> Légende);

/// <summary>
/// Une entrée de la légende : un responsable présent dans la fenêtre affichée, avec son nom
/// d'affichage et sa couleur déjà résolue (palier 2). Dérivée des périodes couvrant un jour de
/// la fenêtre, dédoublonnée par identifiant stable.
/// </summary>
public sealed record EntreeLegende(string IdentifiantStable, string Nom, string Couleur);

/// <summary>
/// Une case-jour de la grille (axe vertical : une ligne par semaine), portant la couleur et le
/// nom du parent responsable de la période qui couvre ce jour (couleur neutre / nom vide si
/// aucune période ne le couvre) et les slots rattachés à sa date.
/// </summary>
public sealed record JourCase(DateOnly Date, string CouleurResponsable, string NomResponsable, IReadOnlyList<SlotCase> Slots);

/// <summary>
/// Un slot positionné dans sa case-jour : libellé (lieu/acteur), bornes horaires de la journée
/// et couleur propre de son acteur (deuxième niveau de couleur, distinct de la couleur de la
/// case-jour : la case porte la responsabilité, le créneau porte l'acteur).
/// </summary>
public sealed record SlotCase(string Libelle, TimeOnly Debut, TimeOnly Fin, string CouleurActeur);

/// <summary>Une ligne-semaine : 7 cases-jour consécutives, du lundi au dimanche.</summary>
public sealed record SemaineLigne(IReadOnlyList<JourCase> Jours);
