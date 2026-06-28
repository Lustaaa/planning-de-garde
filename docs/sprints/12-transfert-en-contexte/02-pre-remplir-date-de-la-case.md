# Scenario 2 — La dialog se pré-remplit sur la date de la case cliquée `@limite 🖥️ IHM`

[← Retour au suivi](00-sprint12-suivi.md)

> **Routé vers `ihm-builder`.** Niveau d'acceptation = **E2E/runtime**.
> **⚠️ Probablement early green (câblage IHM partagé).** L'**ancrage de la date de contexte**
> (règle 17, la date de la case prime sur le défaut horloge) est un **pattern déjà vert s11**
> (Sc.3 s11) : dès que le Sc.1 câble la dialog transfert avec le **paramètre de date de la
> case**, le pré-remplissage est acquis **par construction**. Caractérisation (filet
> anti-régression), **pas un driver** — **batchable** avec les autres early-greens du sprint.

## Acceptation (BDD) — test de NIVEAU RUNTIME

**Should_Pre_remplir_la_date_du_transfert_sur_le_jeudi_25_06_2026_et_non_sur_aujourd_hui_le_15_06_2026_When_un_parent_ouvre_la_dialog_depuis_la_case_du_jeudi_25_06_2026** — ✅ GREEN (caractérisation early-green confirmée — `FrontWasmDefinirTransfertDateContexteTests`, runtime : front WASM réel + API distante réelle + store réel ; pré-remplissage du champ date sur la case **et** date du transfert relue depuis le store distant = 25/06/2026, jamais le défaut horloge 15/06/2026)

Sur l'app **réellement câblée**, la date de référence « aujourd'hui » est figée au lundi
15/06/2026 (`IDateTimeProvider`). Un Parent **clique la case du jeudi 25/06/2026** et ouvre la
3ᵉ entrée « Définir un transfert ». **Observable runtime** : la dialog s'ouvre avec la **date du
transfert pré-remplie sur le jeudi 25/06/2026** — **pas** sur le lundi 15/06/2026. *Anti
vert-qui-ment* : si la dialog retombe sur le défaut horloge, l'assertion sur la date de la case
échoue → rouge.

## Tests bUnit composant (optionnels — pilotés par `ihm-builder`)

| # | Test composant (FLFI) | TPP | Contradiction | Status |
|---|------------------------|-----|---------------|--------|
| 1 | Should_Pre_remplir_la_date_du_transfert_sur_la_date_de_la_case_When_la_dialog_est_ouverte_depuis_une_case_differente_d_aujourd_hui | (variable) date = paramètre de contexte | ⚠️ probablement early green — couvert par #2 du Sc.1 (caractérisation, pas driver) : la dialog reçoit déjà la date de la case en paramètre ; le défaut horloge ne s'applique qu'hors-contexte | ✅ couvert au runtime (bUnit composant non requis — preuve = acceptation runtime) |

## Fichiers à créer / modifier (par `ihm-builder`)

- **`DefinirTransfertDialog.razor.cs`** — la **date de contexte** reçue en paramètre pré-remplit
  le champ date ; le repli `Horloge.Aujourdhui` (`OnInitialized`) **n'est pas supprimé** du port
  (garde-fou règle 17), il devient code mort tant que toute saisie passe par une case.
- **`Web.Tests`** — bUnit composant + acceptation runtime (horloge figée distincte de la date
  de la case).

## Design notes

- **⚠️ Couvert par l'acquis** : l'ancrage de la date de contexte est vert depuis s11 (Sc.3) et
  le paramètre de date est déjà câblé par le Sc.1 → `ihm-builder` doit l'attendre **vert
  d'emblée** (pas un défaut), le marquer `✅ GREEN (caractérisation)`.
- **Règle 17 composée, non révisée** : le défaut nu `IDateTimeProvider` ne vaut que
  **hors-contexte** ; en contexte, la date de la case **prime**. Le test fige « aujourd'hui »
  sur une date **différente** de la case pour que la contradiction soit non-vacuous.
