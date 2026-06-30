# Sc.4 — Acteur mappé au cycle de fond : son index devient non mappé → neutre `@limite` `@driver`

← [Retour au suivi](00-sprint13-suivi.md)

> **Backend `tdd-auto`** (frontière Application — projection `GrilleAgendaQuery`). Vrai RED neuf
> **distinct du Sc.2** : ici l'acteur supprimé est porté par le **fond** (le mapping du cycle), pas
> par une surcharge. Le filtre d'existence du Sc.2 ne couvre que la branche surcharge.

## Acceptation (BDD)

`Acceptation_Should_Rendre_l_index_du_cycle_non_mappe_et_la_case_neutre_sans_nom_fantome_When_l_acteur_mappe_au_cycle_de_fond_est_supprime`
— à la frontière Application : foyer (Parent A, Nounou), cycle N=2 mappant index 0 sur Parent A et
**index 1 sur Nounou**, **aucune période** sur la semaine d'index 1 (la case du 23/06/2026 affiche
Nounou au titre du fond). Après suppression de Nounou, l'index 1 se comporte comme **non mappé** : la
case du 23/06 retombe sur la **teinte neutre** et **n'affiche aucun nom fantôme**.

**✅ GREEN** — filtre d'existence étendu à la branche **fond** (helper `Resolvable` appliqué à la
surcharge ET au fond) en **case et en légende** ; acceptation (store réel) + driver verts ; suite
complète 190/190. NB : 23/06/2026 est en semaine ISO 26 → index `26 % 2 = 0` ; le cycle mappe donc
l'**index 0 sur Nounou** (et l'index 1 sur Parent A) pour que le jour observé tombe sur l'index orphelin.

## Tests unitaires (ordonnés)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Rendre_la_case_neutre_sans_nom_fantome_When_l_acteur_porte_par_le_fond_du_cycle_est_supprime` | (constant → conditionnel) extension du filtre d'existence à la branche fond | **Driver** : l'impl minimale du **Sc.2** ne filtre que la **surcharge** ; un fond orphelin (`ResponsableDeFond(date) = id de Nounou`, le mapping du cycle restant inchangé) **résout encore l'id brut** → la case afficherait le nom fantôme. Force l'extension du filtre d'existence à la **branche fond** : un responsable de fond supprimé est traité comme **non mappé** → `null` → neutre. | ✅ GREEN |

## Fichiers à créer

- Modification de `src/PlanningDeGarde.Application/Classes/GrilleAgendaQuery.cs` (filtre d'existence aussi sur le responsable de fond, en case **et en légende**)
- `tests/PlanningDeGarde.Tests/Scenario4_ActeurMappeFondIndexNonMappe.cs`

## Design notes

- **Le mapping du cycle n'est PAS muté** par la suppression (le `CycleDeFond` reste InMemory, règle 30
  — on ne « démappe » pas l'index dans la structure). La neutralisation est **observable à la
  résolution** : un fond orphelin est ignoré à la projection, exactement comme un index non mappé.
- **Cohérence légende** : la `LegendeDesPresents` énumère aussi les responsables de fond — un fond
  orphelin doit également **disparaître de la légende** (pas d'entrée fantôme). Vérifier que le filtre
  d'existence couvre case **et** légende (sinon early-green en case mais fantôme en légende).
- Réutilise le même contrat d'existence que le Sc.2 (cf. design note Sc.2). **→ remonter au CP si le
  contrat d'existence est ambigu.**
