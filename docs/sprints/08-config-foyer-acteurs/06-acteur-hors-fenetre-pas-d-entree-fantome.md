# Sc.6 — Éditer un acteur absent de la fenêtre affichée : pas d'entrée fantôme

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint08-suivi.md](00-sprint08-suivi.md)

**Routage** : **caractérisation backend** (`tdd-auto`, ⚠️ early green attendu) **+** acceptation
**runtime IHM** (`ihm-builder` : l'écran de config confirme l'édition, la grille reste
inchangée, la légende n'introduit **aucune entrée fantôme**).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Symptôme runtime : renommer un acteur **sans période dans la fenêtre** est confirmé sur
> l'écran de config, mais la grille courante reste inchangée et la légende **ne montre aucune
> entrée** pour cet acteur (la légende = présents dans la fenêtre, s07 Sc.3). bUnit seul ne
> prouve pas le rendu réel ni la confirmation à l'écran.

`Should_Confirmer_le_renommage_de_parent_c_en_Mathilde_a_l_ecran_de_configuration_sans_modifier_la_grille_ni_introduire_d_entree_de_legende_When_parent_c_n_a_aucune_periode_dans_la_fenetre_de_cinq_semaines`

- **Niveau** : E2E/runtime sur l'app câblée. Store réel : `parent-c` renommé « Marie » →
  « Mathilde », **aucune période** de `parent-c` dans la fenêtre du 13/07/2026.
- **Observable** : l'écran de config affiche désormais « Mathilde » pour `parent-c` ; la
  grille de la fenêtre courante est inchangée ; la légende **ne liste aucune** entrée
  `parent-c`.

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Confirmer_le_renommage_sans_faire_apparaitre_l_acteur_en_legende_When_l_acteur_renomme_n_a_aucune_periode_dans_la_fenetre_affichee` | — (caractérisation de deux invariants déjà acquis) | ⚠️ **probablement early green — couvert par Sc.1 #2 (confirmation, validation = id connu) + s07 Sc.3 (légende = présents dans la fenêtre) (caractérisation, pas driver)** : le handler valide l'**existence de l'id** (pas la présence en fenêtre) → confirme ; la légende dérive des **périodes couvrant la fenêtre** → pas d'entrée fantôme **par construction**. Aucun rouge — filet anti entrée-fantôme. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun fichier backend nouveau.)* Caractérisation : renommage confirmé pour un id sans
  période + légende-présents inchangée (`GrilleAgendaQuery`).
- *(Confirmation écran de config + grille inchangée : hors backend — routé `ihm-builder`.)*

## Design notes

- **Confirmation ≠ présence en grille** : la validation est « id stable connu + nom non
  vide », **indépendante** de la fenêtre affichée. Un acteur peut être édité même sans
  période visible.
- **Pas d'entrée fantôme** : la légende reste **dérivée des présents** (s07 Sc.3), jamais le
  catalogue du foyer. Scénario sans driver réel → vert au 1er passage = **attendu**.
