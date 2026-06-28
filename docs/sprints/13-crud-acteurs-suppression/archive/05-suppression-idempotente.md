# Sc.5 — Supprimer un acteur absent ou déjà supprimé : no-op qui réussit `@erreur` `@caractérisation`

← [Retour au suivi](00-sprint13-suivi.md)

> **Backend `tdd-auto`** — **caractérisation** (filet anti-régression de la sémantique DELETE
> idempotente, D3). **⚠️ probablement early green** : l'impl minimale du Sc.1 (`Dictionary.Remove`
> no-op sur clé absente, `Result` toujours succès) couvre déjà l'idempotence — aucun rouge propre
> attendu, conservé pour documenter le `@erreur` (non-refus, règle 6).

## Acceptation (BDD)

`Acceptation_Should_Reussir_sans_lever_d_erreur_ni_modifier_la_configuration_When_un_acteur_absent_ou_deja_supprime_est_supprime`
— à la frontière Application : supprimer un id inexistant (`acteur-inexistant`) renvoie **succès sans
effet** (Parent A / Parent B toujours présents) ; supprimer deux fois Parent B renvoie **succès** aux
deux appels, sans erreur ni effet supplémentaire après le premier.

**✅ GREEN (caractérisation)** — early-green ATTENDU confirmé au 1er passage (acceptation + driver verts,
aucun code de prod neuf : `Dictionary.Remove` no-op sur clé absente + `Result.Succes` inconditionnel de
l'impl minimale Sc.1). Suite complète 192/192.

## Tests unitaires (ordonnés)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Reussir_sans_effet_ni_erreur_When_l_acteur_a_supprimer_est_absent_ou_deja_supprime` | — | **⚠️ probablement early green — couvert par Sc.1 (caractérisation, pas driver)** : `Dictionary.Remove` est naturellement no-op sur clé absente et le handler renvoie `Result.Succes` inconditionnellement → supprimer un id absent **réussit** sans changer la liste, et une seconde suppression du même acteur **réussit** aussi sans effet supplémentaire. Aucun chemin de refus (philosophie non-refus règle 6). `tdd-auto` doit marquer `✅ GREEN (caractérisation)` au 1er passage. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- `tests/PlanningDeGarde.Tests/Scenario5_SuppressionIdempotente.cs`

## Design notes

- **Idempotence assumée** (D3) : aucune dialog d'erreur, aucun refus — l'état final identique (acteur
  absent) est le seul observable. Un test qui exigerait un refus contredirait la règle 6 et serait à
  rejeter.
- Si ce test passait **rouge** (p.ex. le retrait Mongo lève sur document absent), corriger le retrait
  côté adaptateur (le rendre tolérant à l'absence), pas ajouter de garde de refus.
