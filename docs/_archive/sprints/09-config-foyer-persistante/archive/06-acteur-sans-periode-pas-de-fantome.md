# Sc.6 — Un acteur ajouté sans période ne crée pas d'entrée fantôme

`@limite`

↩ Retour : [00-sprint09-suivi.md](00-sprint09-suivi.md)

**Routage** : **caractérisation backend** (⚠️ early green attendu, s07/s08) **+** runtime IHM
(énumération). Un acteur ajouté **sans période** est **présent dans la liste de l'écran config**
(énumération, Sc.1 #3) mais **n'apparaît ni en légende ni en case** : la légende ne liste que les
**présents dans la fenêtre** (périodes intersectant l'intervalle) et la case n'est nommée que si
une période la couvre — invariants **déjà verts** (s07 Sc.3 légende-présents, s08 Sc.6 hors-fenêtre).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Carla vient d'être ajoutée, aucune période dans la fenêtre ; la grille est rendue ; Carla est
> **présente dans la liste** de l'écran de configuration (énumération depuis le store durable) mais
> **n'apparaît dans aucune entrée de légende** ni **aucune case** de la grille. Sur l'app
> réellement câblée.

`Should_Lister_Carla_dans_l_ecran_de_configuration_mais_l_exclure_de_toute_entree_de_legende_et_de_toute_case_When_l_acteur_ajoute_n_a_aucune_periode_dans_la_fenetre` — ⏳ Pending (runtime, routé `ihm-builder`)

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Exclure_l_acteur_ajoute_de_la_legende_et_des_cases_When_aucune_periode_ne_le_porte_dans_la_fenetre` | composition (read model inchangé) | ⚠️ **probablement early green — couvert par s07 Sc.3 (légende = présents dans la fenêtre) + s08 Sc.6 (hors-fenêtre, pas de fantôme) (caractérisation, pas driver)**. `LegendeDesPresents` ne retient que les ids portés par une période intersectant l'intervalle ; un acteur ajouté **sans période** n'y entre pas, et aucune case n'est nommée. Ajouter un acteur ne crée pas de période → aucun fantôme **mécaniquement**. `tdd-auto` marquera ✅ GREEN (caractérisation). | ✅ GREEN (caractérisation) |

> **Pourquoi une caractérisation** — L'invariant « pas de fantôme » découle de la **composition**
> d'invariants déjà verts (légende-par-présence s07 + ajout sans effet de bord période). La
> **présence en liste config** (le vrai observable neuf) est la nouveauté — mais c'est
> l'**énumération** (Sc.1 #3 backend + runtime IHM), pas la légende/case. Aucun rouge backend ici.

## Fichiers à créer / modifier

- **Backend** : néant de neuf (read model s07/s08 inchangé). Test de caractérisation composant
  `AjouterActeurHandler` (Sc.1, sans période) + `GrilleAgendaQuery` (légende/case vides pour Carla)
  + énumération (Carla présente).
- **Volet runtime IHM (routé `ihm-builder`)** — écran config énumérant Carla ; grille sans entrée
  Carla.

## Design notes

- **Énumération ≠ légende.** L'acteur ajouté **existe** dans le foyer (énumération depuis le store)
  **avant** d'avoir une période ; il n'**apparaît dans la grille** (légende/case) que lorsqu'une
  période le porte (Sc.4). Ne pas confondre « exister dans le foyer » et « être présent dans la
  fenêtre affichée ».
- **Cohérent s08 Sc.6** : pas d'entrée fantôme pour un acteur hors fenêtre.
