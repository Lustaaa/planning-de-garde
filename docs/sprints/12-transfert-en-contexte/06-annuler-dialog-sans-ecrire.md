# Scenario 6 — Annuler la dialog n'émet aucune écriture `@limite 🖥️ IHM 🏷️ caractérisation`

[← Retour au suivi](00-sprint12-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**.
> **⚠️ Probablement early green (câblage IHM partagé) — caractérisation (filet).** Le pattern
> « annuler une dialog → fermeture sans commande » est **acquis s11** (Sc.5 s11) ; transposé au
> transfert, il est acquis **par construction** dès que le Sc.1 câble l'issue `OnAnnule`.
> **Groupable** avec les autres early-greens du sprint et l'invariant « transfert incomplet »
> (absorbé par les `Examples` du Sc.3). **Pas un driver.**

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Fermer_la_dialog_sans_emettre_d_ecriture_ni_accuse_et_laisser_la_grille_intacte_When_un_parent_annule_la_dialog_de_transfert_sans_valider** — ✅ GREEN (`FrontWasmDefinirTransfertAnnulationTests`) — caractérisation early-green **confirmée** (vert d'emblée, anticipé) : store réel vérifié vide AVANT (témoin/baseline) ET après l'annulation, dialog refermée, aucun accusé.

Sur l'app **réellement câblée**, un Parent ouvre la dialog depuis la case du samedi 20/06/2026,
choisit « Parent A » dépositaire et « Parent B » récupérateur, puis **annule sans valider**.
**Observable runtime** : la dialog **se ferme**, **aucune écriture n'est émise** (store réel
inchangé), **aucun accusé « Transfert défini »** ne s'affiche, la grille reste **inchangée**.
*Anti vert-qui-ment* : vérifier sur le **store réel** qu'aucun transfert n'a transité (pas un
spy seul) — l'annulation ne doit laisser **aucune** trace.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Fermer_la_dialog_sans_emettre_de_commande_ni_accuse_When_un_parent_annule_sans_valider | (conditional) issue annulation | ⚠️ probablement early green (câblage IHM partagé) — l'issue `OnAnnule` du s11 (Sc.5) ferme déjà sans émettre ; spy de canal à 0 écriture | ⏳ Pending |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`DefinirTransfertDialog.razor.cs`** — `OnAnnule` ferme la dialog **sans** appeler le canal.
- **`Web.Tests`** — bUnit (spy de canal à 0 écriture) + acceptation runtime (store réel
  inchangé).

## Design notes

- **⚠️ Couvert par l'acquis** : règle 14 (grille lecture seule, annuler n'émet aucune commande)
  + pattern d'annulation vert s11 → `ihm-builder` doit l'attendre **vert d'emblée**
  (caractérisation), pas un défaut.
- **Distinction d'issue** : annulation = fermeture **sans** écriture **et sans accusé** (≠ succès
  Sc.1 : fermeture **avec** accusé). C'est le point protégé.
