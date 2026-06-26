# Scénario 1 — La grille structure 5 semaines à partir de la semaine en cours `@nominal`

[← Retour au suivi](00-sprint03-suivi.md)

> **Routage : backend → `tdd-auto`.** Comportement de la projection
> `GrilleAgendaQuery` (calcul de fenêtre), observable sans Blazor. Le rendu de la
> grille (refonte `PlanningPartage.razor`) est hors périmètre ici (couvert par
> `ihm-builder` après gate visuel).

> **Acceptation (BDD)** —
> `Should_Structurer_une_fenetre_de_35_jours_du_lundi_22_06_au_dimanche_26_07_2026_en_5_semaines_de_7_jours_When_un_Parent_consulte_la_grille_le_mercredi_24_06_2026_sans_aucun_slot_ni_periode`
> Test unitaire de projection : `GrilleAgendaQuery` sur des `FakeSlotRepository` /
> `FakePeriodeRepository` **vides**, date de référence = 24/06/2026 → la grille
> expose exactement 35 `JourCase`, la première datée du **lundi 22/06/2026**, la
> dernière du **dimanche 26/07/2026**, ordonnées et regroupables en 5 semaines de 7
> jours. (Driver de la structure de fenêtre réutilisée par tous les autres scénarios.)
>
> **Statut acceptation : ✅ GREEN**

## Tests unitaires (ordonnés TPP)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Produire_exactement_35_cases_jour_When_un_Parent_consulte_la_grille_un_jour_donne_sans_donnee` | nil → tableau constant (cardinalité) | Driver : une projection qui renvoie une grille vide / une longueur quelconque échoue ; force la génération des 35 jours de la fenêtre. | ✅ GREEN |
| 2 | `Should_Demarrer_au_lundi_22_06_2026_et_finir_au_dimanche_26_07_2026_When_la_date_de_reference_est_le_mercredi_24_06_2026` | constant → calcul (ancrage au lundi de la semaine + fenêtre datée) | Driver : un démarrage à la date de référence (24/06) au lieu du lundi de sa semaine (22/06), ou une fin qui n'est pas 35 jours datés plus tard, contredit le test #1 (qui n'impose pas les dates). Force le calcul « lundi de la semaine en cours » + énumération datée jour à jour. | ✅ GREEN |
| 3 | `Should_Regrouper_les_35_cases_en_5_semaines_de_7_jours_consecutifs_When_la_grille_est_structuree` | calcul → partition (5×7) | Driver : une liste plate de 35 jours sans structure de semaine (ou des semaines non consécutives lundi→dimanche) ne satisfait pas le regroupement 5×7 ; force l'exposition de la grille par lignes-semaines (ou une projection vérifiable « jour N appartient à la semaine N/7 »). | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Application/GrilleAgendaQuery.cs` — **nouvelle** projection
  (constructeur prend `ISlotRepository` + `IPeriodeRepository` ; méthode de
  projection prenant la **date de référence** en paramètre, ex. `DateOnly`).
- `src/PlanningDeGarde.Application/GrilleAgenda.cs` (ou records co-localisés) —
  read models `GrilleAgenda` / `JourCase` (au minimum `Date`, et le squelette des
  semaines).
- `tests/PlanningDeGarde.Tests/Scenario_GrilleStructure5Semaines.cs` (nom à
  l'appréciation de l'agent) — tests #1→#3 + test d'acceptation.

## Design notes

- **Date de référence injectable** : ne **jamais** lire `DateTime.Now` dans la
  projection — sinon les tests ne sont pas déterministes. Recommandé : la méthode de
  projection prend `DateOnly dateReference` (ou une `TimeProvider`). C'est le point de
  scaffolding à trancher en premier.
- « Lundi de la semaine en cours » : le 24/06/2026 est un **mercredi** ; le lundi de
  sa semaine est le **22/06/2026**. Fenêtre = [22/06 .. 22/06 + 34 jours] = dimanche
  **26/07/2026** inclus (5×7 = 35 jours).
- Doubler **uniquement** les ports `ISlotRepository`/`IPeriodeRepository` (fakes en
  mémoire déjà présents). Pas de Blazor, pas de SignalR.
- Le test #3 ne doit pas devenir un doublon du #2 : il porte sur la **structure**
  (partition en semaines), pas sur les bornes. Si la projection expose déjà
  `Jours[35]` ordonnés, vérifier que la semaine est dérivable (index / 7) ou exposée.
