# Scenario 4 — Un Invité ne peut pas ouvrir le menu depuis une case `@erreur 🖥️ IHM`

[← Retour au suivi](00-sprint12-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**.
> **⚠️ Probablement early green (câblage IHM partagé).** Le **gating Invité** du déclencheur
> d'écriture est **mutualisé** sur `Session.EstParent` (règle 9) et **déjà vert s11** (Sc.6
> s11) : ajouter une 3ᵉ entrée au menu **ne change pas** le point d'application du droit. Dès que
> le Sc.1 réutilise le menu gaté existant, ce contrôle négatif est acquis **par construction**.
> Caractérisation (filet, **contrôle négatif** en regard du contrôle positif Parent porté par le
> Sc.1), **pas un driver** — **batchable**.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_N_ouvrir_aucun_menu_ni_dialog_de_transfert_et_garder_la_grille_en_lecture_seule_When_un_invite_en_consultation_seule_clique_une_case** — ✅ GREEN (caractérisation early-green confirmée — `FrontWasmDefinirTransfertGatingInviteTests`, runtime sur app réellement câblée. Contrôle positif Parent **dans le même test** (le Parent ouvre bien le menu) puis bascule Invité : aucun menu, aucune dialog, case non cliquable → gating mutualisé sur `EstParent`, règle 9)

Sur l'app **réellement câblée**, le planning est affiché pour un **Invité en consultation
seule**. L'Invité **clique la case du mardi 16/06/2026**. **Observable runtime** : **aucun menu
d'actions** ne s'ouvre, **aucune dialog** « Définir un transfert » ne s'ouvre, le **déclencheur
d'écriture de la case est désactivé**, la grille reste en **lecture seule**. *Anti
vert-qui-ment* : contrôle positif Parent **en regard** (le Sc.1 prouve que le Parent, lui,
ouvre le menu et voit la 3ᵉ entrée) pour que l'absence ne soit pas un faux vert dû à un menu
cassé pour tous.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Ne_pas_ouvrir_le_menu_ni_la_dialog_de_transfert_When_un_invite_clique_une_case | (conditional) gating sur EstParent | ⚠️ probablement early green (câblage IHM partagé) — le déclencheur est gaté sur `Session.EstParent` (règle 9), acquis s11 ; la 3ᵉ entrée hérite du même gating | ✅ couvert au runtime (bUnit composant non requis — preuve = acceptation runtime) |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`PlanningPartage.razor` / `.razor.cs`** — le déclencheur du menu reste **rendu
  conditionnel** sur `SessionPlanning.EstParent` ; la 3ᵉ entrée n'introduit **aucun** nouveau
  point de gating.
- **`Web.Tests`** — bUnit (session Invité) + acceptation runtime (contrôle négatif Invité vs
  positif Parent du Sc.1).

## Design notes

- **⚠️ Couvert par l'acquis** : gating mutualisé sur `EstParent` (règle 9) vert s11 →
  `ihm-builder` doit l'attendre **vert d'emblée** (caractérisation), pas un défaut.
- **Gating au déclencheur, pas par entrée** : on ne gate pas la 3ᵉ entrée individuellement mais
  le **menu/déclencheur unique** (point d'application inchangé). C'est le point de conception
  protégé. → remonter au CP si une exigence demandait un gating par entrée.
