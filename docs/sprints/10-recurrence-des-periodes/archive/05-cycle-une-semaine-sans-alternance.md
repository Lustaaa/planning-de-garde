# Sc.5 — Cycle d'une seule semaine : aucune alternance, même responsable partout

`@limite`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : tranche **backend = CARACTÉRISATION** (`tdd-auto`, ⚠️ early green **attendu** : N=1 ⇒
`ISOWeek mod 1 = 0` pour **toute** date ⇒ même index 0 partout — **pas** un driver) **+** acceptation
**runtime IHM** (`ihm-builder`). Ce scénario caractérise aussi la **tranche de secours** (cycle à 1
semaine = responsable de fond unique) : la couche de résolution la couvre **par construction**, sans
livraison séparée.

> **Données** : cycle **N=1**, index 0 → `parent-a` (Alice bleu). Aucune période. Date de référence =
> lundi 29/06/2026 (ISO 27). Fenêtre de 5 semaines : ISO 27→31. Attendu : **toutes** les semaines
> affichent Parent A bleu en fond, **aucune alternance** ; la légende ne comporte que Parent A bleu.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Sur l'app câblée, avec un cycle d'**une seule semaine** (index 0 → Parent A) et aucune période, les 5
> semaines affichées (ISO 27 à 31) affichent toutes « Parent A » bleu en fond, sans alternance, et la
> légende ne comporte que « Parent A » bleu.

`Should_Afficher_Parent_A_bleu_sur_toutes_les_semaines_affichees_sans_alternance_et_une_legende_a_un_seul_responsable_When_le_cycle_de_fond_ne_compte_qu_une_seule_semaine` — ⏳ Pending *(runtime, `ihm-builder`)*

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Afficher_le_meme_responsable_de_fond_sur_toutes_les_semaines_sans_alternance_et_une_legende_a_un_seul_responsable_When_le_cycle_ne_compte_qu_une_seule_semaine` | — | ⚠️ **probablement early green — couvert par Sc.1 (`index = ISOWeek mod N`) avec N=1 (caractérisation, pas driver)**. `mod 1 = 0` pour toute date ⇒ index 0 ⇒ Parent A partout, mécaniquement (aucune branche neuve). La légende (présents dédoublonnés par id) ne contient qu'Alice. `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier (backend uniquement ici)

- **Aucun fichier neuf** — comportement mécanique de la formule `ISOWeek mod N` du Sc.1 avec N=1.
- **Doublures tests** — `Fake` du port cycle avec N=1, index 0 → `parent-a` ; `GrilleAgendaQuery` projeté
  sur la fenêtre ISO 27→31, vérifiant l'absence d'alternance sur les 5 semaines.
- **Volet runtime IHM (routé `ihm-builder`)** — grille rendant le même fond sur 5 semaines, légende à une
  entrée.

## Design notes

- **N=1 = responsable de fond unique (tranche de secours).** La couche de résolution couvre ce cas **par
  construction** (`mod 1 = 0`), ce qui confirme que la tranche de secours (cf. cadrage CP / Risques spec)
  n'exige aucun code séparé : elle est un cas dégénéré du cycle N-semaines.
- **Aucune alternance** = même index sur toute la fenêtre : la non-alternance est l'observable, pas une
  règle neuve.
- **Caractérisation conservée** : @limite documentant le cas N=1 ; pas de rouge attendu.
