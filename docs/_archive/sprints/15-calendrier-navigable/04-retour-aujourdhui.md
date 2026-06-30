# Scénario 4 — Retour à la semaine en cours après navigation

`@limite` · **🖥️ scénario IHM** — **Routé vers `ihm-builder`** · **acceptation RUNTIME**. ⚠️ **early green
(câblage IHM partagé)** — dépend de l'état de navigation posé par Sc.1.

[← Retour au suivi](00-sprint15-suivi.md)

Après avoir navigué (deux fois « Semaine suivante » → fenêtre au lundi 22/06), cliquer « Aujourd'hui »
**ramène** la fenêtre sur la semaine en cours (lundi 08/06, fond Alice) ; aucune écriture.

## Acceptation (BDD) — niveau RUNTIME — ✅ GREEN

`Should_Ramener_la_fenetre_au_lundi_08_06_2026_avec_le_fond_Alice_sans_emettre_d_ecriture_When_l_utilisateur_clique_Aujourd_hui_apres_avoir_navigue_deux_semaines_en_avant_sur_l_app_reellement_cablee`
(`tests/PlanningDeGarde.Web.Tests/FrontWasmRetourAujourdhuiTempsReelTests.cs`)
— sur l'app réellement câblée : après deux « Semaine suivante » (fenêtre au 22/06), un clic « Aujourd'hui »
re-projette sur la semaine de `Horloge.Aujourdhui` (lundi 08/06, fond Alice bleu, palette réelle) ; canal
d'écriture non sollicité (espion de transport : aucune écriture). **Rouge d'abord** : le bouton
`nav-aujourdhui` étant neuf, le clic ne trouvait pas l'élément (symptôme « Aujourd'hui sans effet »).

## Inner-loop (boucle rapide `ihm-builder`)

| # | Test inner-loop | Contradiction | Status |
|---|-----------------|---------------|--------|
| 1 | `Should_Reinitialiser_l_ancre_a_la_semaine_de_la_date_du_jour_When_l_utilisateur_demande_le_retour_a_aujourd_hui` | ⚠️ early green (câblage IHM partagé) — une fois l'ancre mutable de Sc.1 posée, `RevenirAujourdhui(aujourdHui)` = reset au lundi de l'horloge ; caractérisation de la plomberie de navigation. Le **bouton lui-même était neuf** : c'est lui qui a porté le rouge runtime, pas le reset. | ✅ GREEN |

## Fichiers à créer / modifier

- `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ `.razor.cs`) — contrôle **Aujourd'hui**.
- `src/PlanningDeGarde.Web/State/…` — `RevenirAujourdhui()` (reset de l'ancre à la semaine de `Horloge.Aujourdhui`).

## Design notes

- **Cascade early-green contrôlée** : l'état de navigation (ancre) est posé par **Sc.1** ; « Aujourd'hui »
  réutilise cette plomberie (reset). À **batcher** comme caractérisation chez `ihm-builder`, pas à traiter
  en early-green inattendu. Le bouton + le reset sont neufs mais triviaux une fois Sc.1 vert.
- **Date du jour via le port d'horloge injecté** (`Horloge.Aujourdhui`), jamais `DateTime.Now` — symétrie
  avec la projection déterministe.
