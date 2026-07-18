using System;
using System.Collections.Generic;

namespace PlanningDeGarde.Application;

/// <summary>
/// Le responsable RÉSOLU d'un jour restitué par le digest cloche (s50) : identifiant stable de l'acteur
/// (ou <c>null</c> si personne n'est assigné), son <b>nom</b> et sa <b>couleur</b> tels que la grille les
/// résout, et le flag <see cref="EstAssigne"/> (faux = état neutre « personne assignée », sans nom ni
/// couleur fantôme, repli s13 R5/R6). Présentation seule. Miroir des ex-read-models s42/s43 retirés s44.
/// </summary>
public sealed record ResponsableDuJour(string? ActeurId, string Nom, string Couleur, bool EstAssigne);

/// <summary>
/// Le transfert cédant → recevant d'un jour du digest (s50) : nom + couleur du <b>cédant</b> (déposant) et
/// du <b>recevant</b> (récupérant), résolus par la grille composée (saisi OU dérivé s31, un acteur orphelin
/// retombe sur le repli neutre sans nom fantôme). Absent (<c>null</c>) = jour sans transfert.
/// </summary>
public sealed record TransfertDuJour(string CedantNom, string CedantCouleur, string RecevantNom, string RecevantCouleur);

/// <summary>
/// Un jour du digest cloche (s50) : sa <see cref="Date"/>, le <see cref="Responsable"/> résolu, le(s)
/// <see cref="Slots"/> du jour (le « où » de l'enfant sélectionné) et le <see cref="Transfert"/> éventuel
/// (null = jour sans transfert). Lecture seule, composée de la grille.
/// </summary>
public sealed record JourDigest(
    DateOnly Date, ResponsableDuJour Responsable, IReadOnlyList<SlotCase> Slots, TransfertDuJour? Transfert);

/// <summary>
/// Payload du digest « immédiat » de la cloche (s50) : la section <see cref="Immediat"/> « qui récupère
/// aujourd'hui / ce soir » (null = jour courant hors de la fenêtre de grille chargée) et la liste
/// <see cref="AVenir"/> des jours à venir de la fenêtre chargée, en ordre chronologique croissant. Lecture
/// stricte, composée de <see cref="GrilleAgendaQuery"/> — aucune mutation, aucun store neuf.
/// </summary>
public sealed record DigestImmediat(JourDigest? Immediat, IReadOnlyList<JourDigest> AVenir);
