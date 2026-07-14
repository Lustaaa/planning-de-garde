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
    IReadOnlyList<EntreeLegende> Légende,
    IReadOnlyList<EntreeLegendeMotif>? LégendeMotifs = null);

/// <summary>
/// Une entrée de la légende des <b>motifs</b> de rendu (distincte des responsables) : signale un motif
/// visuel présent dans la fenêtre — ici le motif <b>bicolore = transfert</b>. Présente uniquement quand
/// un transfert couvre la fenêtre affichée, absente sinon.
/// </summary>
public sealed record EntreeLegendeMotif(string Libelle);

/// <summary>
/// Une entrée de la légende : un responsable présent dans la fenêtre affichée, avec son nom
/// d'affichage et sa couleur déjà résolue (palier 2). Dérivée des périodes couvrant un jour de
/// la fenêtre, dédoublonnée par identifiant stable.
/// </summary>
public sealed record EntreeLegende(string IdentifiantStable, string Nom, string Couleur);

/// <summary>
/// Une case-jour de la grille (axe vertical : une ligne par semaine), portant la couleur et le
/// nom du parent responsable de la période qui couvre ce jour (couleur neutre / nom vide si
/// aucune période ne le couvre) et les slots rattachés à sa date. <see cref="Transfert"/> porte
/// l'information <b>bicolore</b> quand un transfert est saisi ce jour-là (sinon <c>null</c> : case
/// unicolore inchangée) — présentation seule, la résolution de responsabilité reste inchangée.
/// </summary>
public sealed record JourCase(
    DateOnly Date, string CouleurResponsable, string NomResponsable, IReadOnlyList<SlotCase> Slots, InfoTransfert? Transfert = null,
    string? ResponsableId = null);

/// <summary>
/// Information bicolore d'une case portant un transfert : couleur de <see cref="CouleurDepart"/>
/// (acteur cédant / déposant) et couleur d'<see cref="CouleurArrivee"/> (acteur recevant /
/// récupérant), résolues sur le référentiel acteurs par identifiant stable. Un acteur supprimé
/// (orphelin) retombe sur la couleur neutre (pas de couleur fantôme).
/// </summary>
public sealed record InfoTransfert(string CouleurDepart, string CouleurArrivee);

/// <summary>
/// Un slot positionné dans sa case-jour : libellé (lieu/acteur), bornes horaires de la journée
/// et couleur propre de son acteur (deuxième niveau de couleur, distinct de la couleur de la
/// case-jour : la case porte la responsabilité, le créneau porte l'acteur).
/// </summary>
public sealed record SlotCase(string Libelle, TimeOnly Debut, TimeOnly Fin, string CouleurActeur);

/// <summary>Une ligne-semaine : 7 cases-jour consécutives, du lundi au dimanche.</summary>
public sealed record SemaineLigne(IReadOnlyList<JourCase> Jours);
