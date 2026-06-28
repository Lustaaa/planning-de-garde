# Sc.4 — Pas d'écriture « au nom de » : l'écriture aboutit sous l'identité réelle `@limite` 🖥️ scénario IHM · caractérisation (⚠️ early green attendu)

← [Retour au suivi](00-sprint14-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation E2E / runtime** (app câblée, **canal d'écriture
> réel**). **PAS** un test backend bUnit-à-doublures. Symptôme PO runtime : écrire **en incarnant** un
> Parent fait aboutir l'écriture, mais la commande part **sous l'identité réelle** (jamais « au nom de »
> l'incarné). **⚠️ probablement early green par construction** : l'impersonation **ne touche pas** le
> canal requête/réponse.

## Acceptation (BDD)

Test **runtime** sur `/planning` (canal d'écriture réel) —
`Should_EnregistrerLeSlotSousLIdentitéRéelleDuConfigurateur_When_OnPoseUnSlotEnIncarnantUnParent` : le
foyer déclare **Bruno** (Parent) ; le configurateur incarne Bruno (menu visible) et **pose un slot le
16/06** depuis la dialog ouverte sur cette case →
- le **slot est enregistré** ;
- la **commande de pose part sous l'identité réelle** du configurateur, **et non sous « Bruno »**.

Prouvé sur l'app réellement câblée (canal d'écriture réel) ; l'auteur de la commande émise est inspecté
côté frontière (jamais doublé).

## Tests unitaires (ordonnés) — inner-loop (acceptation = runtime)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_EnregistrerLeSlotSousLIdentitéRéelleDuConfigurateur_When_OnPoseUnSlotEnIncarnantUnParent` | (aucune — caractérisation) | ⚠️ probablement early green — couvert **par construction** : le canal d'écriture ne lit **jamais** l'identité effective ; l'auteur des commandes reste l'identité réelle (l'impersonation ne touche pas le canal requête/réponse). Filet anti-régression « pas d'écriture au nom de », **pas** un driver | ⏳ Pending |

## Fichiers à créer

- `tests/PlanningDeGarde.Web.Tests/FrontWasmEcritureSousIdentiteReelleTempsReelTests.cs` (acceptation
  runtime — aucun code de prod neuf attendu)

## Design notes

- **Borne dure du sujet** : l'impersonation est **lecture seule** ; elle pilote uniquement la **vue**
  (`EstParent` → menu visible) et **jamais** l'auteur des commandes. Les commandes (`PoserSlot`, etc.)
  conservent l'**identité réelle** — comportement inchangé, d'où l'early-green.
- **⚠️ Cascade early-green** : `ihm-builder` doit **batcher** ce scénario comme caractérisation. S'il
  tombait **rouge**, c'est que l'impersonation a fui dans le canal d'écriture (régression de la borne) —
  alerter le CP.
