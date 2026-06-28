# Sc.3 — Surcharge orpheline sur index non résolu : repli neutre sans nom fantôme `@limite` `@caractérisation`

← [Retour au suivi](00-sprint13-suivi.md)

> **Backend `tdd-auto`** — **caractérisation** (filet anti-régression). **⚠️ probablement early
> green** : aucun rouge propre attendu, conservé pour documenter le `@limite` « repli neutre sans nom
> fantôme » et verrouiller la non-régression.

## Acceptation (BDD)

`Acceptation_Should_Faire_retomber_la_case_sur_la_teinte_neutre_sans_aucun_nom_When_l_acteur_d_une_surcharge_sur_un_index_non_mappe_est_supprime`
— à la frontière Application : foyer (Parent A, Nounou), cycle N=2 mappant index 0 sur Parent A et
laissant l'**index 1 non mappé**, période saisie attribuant le mardi 23/06/2026 (semaine d'index 1)
à Nounou. Après suppression de Nounou, la case du 23/06 retombe sur la **teinte neutre** et **n'affiche
aucun nom**.

**✅ GREEN (caractérisation)** — early-green ATTENDU confirmé au 1er passage (acceptation + driver verts,
aucun code de prod neuf : composition du filtre Sc.2 + `CycleDeFond` index non mappé → `null`). Suite
complète 188/188. NB : 23/06/2026 est en semaine ISO 26 → index `26 % 2 = 0` ; le cycle laisse donc
l'**index 0 non mappé** (mappe l'index 1) pour que le jour de la surcharge tombe sur un index sans fond.

## Tests unitaires (ordonnés)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Faire_retomber_la_case_sur_la_teinte_neutre_sans_aucun_nom_When_l_acteur_d_une_surcharge_sur_un_index_de_cycle_non_mappe_est_supprime` | — | **⚠️ probablement early green — couvert par Sc.2 (caractérisation, pas driver)** : le filtre d'existence du **Sc.2** fait cesser de primer la surcharge orpheline ; le fond à l'**index 1 non mappé** renvoie déjà `null` (contrat `CycleDeFond.ResponsableDeFond`, s10 Sc.4 vert) → `responsableId is null` → `couleur = CouleurNeutre` et `nom = ""`. Aucun mécanisme neuf : `tdd-auto` doit marquer `✅ GREEN (caractérisation)` au 1er passage. | ✅ GREEN (caractérisation) |

## Fichiers à créer

- `tests/PlanningDeGarde.Tests/Scenario3_SurchargeOrphelineRetombeNeutre.cs`

## Design notes

- La combinaison « surcharge orpheline neutralisée (Sc.2) » **+** « index de fond non mappé → `null`
  (existant) » suffit : la case n'a **ni surcharge valide ni fond** → repli neutre, nom vide. Pas de
  nom fantôme car `nom = responsableId is null ? "" : NomDe(...)`.
- Si ce test passait **rouge**, c'est le signal que le filtre du Sc.2 a été posé au **mauvais endroit**
  (filtre sur le `responsableId` combiné plutôt que sur la surcharge seule) — corriger le Sc.2, pas
  ajouter de code ici.
