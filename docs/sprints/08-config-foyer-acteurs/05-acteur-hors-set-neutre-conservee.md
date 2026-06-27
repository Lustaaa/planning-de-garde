# Sc.5 — Éditer un acteur hors set de couleurs : nom suivi, teinte neutre conservée

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **caractérisation backend** (`tdd-auto`, ⚠️ early green attendu) **+** acceptation
**runtime IHM** (`ihm-builder`). Renommer ne **crée pas** de couleur : le nom suit, la teinte
neutre (gris) est conservée pour l'acteur hors set.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime : après renommage de `grand-pere` (hors set couleur) sans lui attribuer de
> teinte, la case + la légende affichent le nouveau nom mais **restent grises**. bUnit seul ne
> prouve pas le rendu réel.

`Should_Afficher_Papy_Jo_dans_la_case_du_17_07_2026_et_en_legende_en_conservant_la_teinte_neutre_grise_When_l_acteur_hors_set_grand_pere_est_renomme_sans_couleur_attribuee`

- **Niveau** : E2E/runtime sur l'app câblée. Store réel : `grand-pere` (hors
  `CouleursParActeur`) renommé « grand-père » → « Papy Jo ».
- **Observable** : la case du 17/07/2026 et l'entrée de légende affichent « Papy Jo » ; la
  teinte reste neutre (grise) — le renommage ne crée pas de couleur.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Suivre_le_nouveau_nom_en_conservant_la_teinte_neutre_When_un_acteur_absent_du_set_de_couleurs_est_renomme_sans_recolorier` | — (caractérisation d'indépendance déjà acquise) | ⚠️ **probablement early green — couvert par Sc.1 #1 + s07 Sc.5 (caractérisation, pas driver)** : `renommer` mute la **seule** surface nom du store ; `CouleurDe` retombe sur le **neutre** par repli (acteur absent du set, s07 Sc.5) **par construction**. Renommer ne touche jamais la couleur. Aucun rouge — filet documentant « renommer ≠ créer une couleur ». | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun fichier backend nouveau.)* Caractérisation sur la chaîne store/handler : renommer
  un id hors set conserve le repli neutre.
- *(Rendu case + légende : hors backend — routé `ihm-builder`.)*

## Design notes

- **Indépendance nom ↔ couleur** (cf. s07 Sc.5, Sc.2 #3) : muter le nom ne crée pas d'entrée
  couleur ; seul `recolorier` vers une teinte du set sort du neutre.
- **Scénario sans driver réel** : observables dérivés de Sc.1 (renommer) + repli neutre s07.
  Vert au 1er passage = **attendu** (`✅ GREEN (caractérisation)`).
