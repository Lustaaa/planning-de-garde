# Besoins priorisés — CRUD acteurs (tranche suppression)

> Source : `99-sprint12-retours.md` (section `# Retours produit (PO)`) · produit par `/4-retours` (retours-challenge).
> Réamorce `/2-make-gherkin` sur le **sujet prioritaire** ci-dessous.

## Classification des retours

> **Retours produit VIDE.** Le sprint 12 (« transfert en contexte ») a été livré et validé
> au gate G3 (goal 6/6, épic É12 refermé) **sans aucun retour d'usage déposé** : les
> sous-sections `## IHM - général`, `## IHM - /planning`, `## Tech`, ainsi que
> `# Idée pour la suite` et `# Consigne pour la suite` ne portent que des placeholders.
> **Rien à classer ni à confronter au code HEAD** ; **aucun bypass Tech** à trancher
> (section Tech vide = aucune contrainte technique). La priorisation du prochain sujet
> dérive donc du **backlog seul** (pas d'un signal d'usage frais) — point de vigilance, cf.
> Risques.

| # | Retour (résumé) | Type | Besoin sous-jacent | Zone IHM/Tech |
|---|---|---|---|---|
| — | *(aucun retour produit déposé ce sprint)* | — | — | — |

## Arbitrage

- **Objectif de l'itération** — Le sprint 12 ayant refermé l'épic « écriture en contexte »
  (É12) sans retour d'usage, l'itération suivante avance d'un cran sur **l'appropriation des
  acteurs du foyer** : permettre de **retirer un acteur** de la config foyer et cadrer le
  sort des saisies qui le référencent.
- **Arbitre (départage)** — **« L'usage tranche »** (règle codifiée au backlog : ne pas
  remonter les paliers techniques devant l'usage). Quand un sujet porteur d'usage produit
  (CRUD acteurs, Calendrier navigable) s'oppose à un sujet technique/dette (stabilisation
  SignalR P2, édition concurrente P3), **l'usage gagne** ; le technique reste en queue.

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 0 | Fix /3 ciblé : dropdown « Acteur du foyer » de /configuration lit une liste statique au lieu du store vivant `_acteurs` (périmée au renommage, dette gate s10) | bug (fix /3 ciblé) | **HORS make-gherkin** — embarque en **tête de sprint 13** | — |
| 1 | **Suppression d'un acteur** (règle 6) + cadrage des **cases orphelines** (slots/périodes de l'acteur retiré) + **message** | nouveau besoin | `crud-acteurs-suppression` | rang 0 (fix dropdown stabilisé d'abord) |
| 2 | Amorce d'**impersonation bornée** (admin incarne un acteur, convenance — **pas** l'auth réelle du palier 12) | nouveau besoin | `impersonation-bornee` | rang 1 |
| 3 | **Calendrier navigable** (navigation passé/futur, vues semaine/mois/4-sem, sélection de plage de cases pour définir une période) | nouveau besoin | `calendrier-navigable` (palier 8) | — |
| 4 | **Stabilisation des flakes temps-réel SignalR** (dette de test) **puis** **édition concurrente** du même jour sous dialog (last-write-wins, règle 11) | dette de test → évolution | `stabilisation-signalr` (P2) → `edition-concurrente` (P3) | en queue, derrière l'usage ; P3 dépend de P2 |

## Prochain sujet → make-gherkin

- **Sujet** : `crud-acteurs-suppression` — CRUD acteurs complet, **tranche suppression (Delete)**
- **Périmètre** : suppression d'un acteur de la config foyer (**règle 6**) + **cadrage des
  cases orphelines** (sort des slots / périodes / transferts référençant l'acteur retiré) +
  **message** à l'utilisateur. La config foyer (acteurs) est **persistée Mongo** (palier 5) :
  la suppression touche un **store réel** → acceptation **runtime** obligatoire.
- **Hors périmètre (reporté)** :
  - **Amorce d'impersonation bornée** (rang 2, suite) — l'admin incarne un acteur ; **pas**
    l'auth réelle du palier 12. ~2h IA, à séquencer après la suppression.
  - **Fix /3 dropdown « Acteur du foyer »** (rang 0) — fix ciblé en tête de sprint, **PAS**
    un scénario Gherkin du CRUD ; à ne pas confondre avec le sujet.

## Risques & questions encore ouvertes

- **Politique des cases orphelines à fixer au make-gherkin (candidat G1)** — supprimer un
  acteur portant des slots/périodes/transferts : rejet si références existantes ?
  réaffectation ? neutralisation + message ? La **règle 6** doit être confrontée à la spec
  courante au passage `/2` ; si elle ne tranche pas le sort des références, c'est un **trou
  métier** (candidat à une porte G1).
- **Persistance Mongo réelle** — la suppression d'acteur opère sur un store durable
  (palier 5) ; **acceptation runtime** (front WASM réel + API distante + store réel,
  rempart anti vert-qui-ment), **pas** de doublure comme seule preuve.
- **Ne pas confondre le fix dropdown avec le sujet** — le fix /3 « Acteur du foyer » est un
  fix ciblé hors make-gherkin (rang 0) ; il ne doit **pas** entrer dans les scénarios
  Gherkin de la suppression.
- **Borne impersonation** — l'impersonation est explicitement hors périmètre ; risque de
  glissement si le make-gherkin tire sur É10 — rester sur la **seule tranche suppression**.
- **Pilotage au catalogue** — retours produit **vide** : la priorisation s'appuie sur le
  backlog seul, sans signal d'usage frais ; confirmer le besoin réel de suppression au
  démarrage du sprint 13.
- **Réserve CP (à traiter en `/5-consolidation`)** — recaler le **n° de palier** : la
  suppression d'acteur relève d'**É2 (Modèle & configuration d'acteurs)** et n'est pas
  nécessairement « palier 9 » ; le numéro est à confirmer/ajuster à la consolidation.
