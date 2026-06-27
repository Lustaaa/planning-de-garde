# Sc.2 — Plusieurs responsables : légende dédoublonnée

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint07-suivi.md](00-sprint07-suivi.md)

**Routage** : 1 **driver backend** (dédoublonnage) + 1 **caractérisation** (`tdd-auto`) **+**
acceptation **runtime IHM** (`ihm-builder`).

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

`Should_Afficher_Alice_sur_ses_deux_cases_et_Bruno_sur_la_sienne_avec_une_legende_de_deux_entrees_Alice_une_seule_fois_et_Bruno_When_la_grille_reellement_cablee_porte_deux_responsables_distincts`

- **Niveau** : E2E/runtime sur l'app câblée (palette + référentiel réels).
- **Observable** : cases du lundi 29/06 et mercredi 01/07 → « Alice » ; case du mardi
  30/06 → « Bruno » ; composant Légende rendu = **exactement deux** entrées, « Alice »
  apparaissant **une seule fois**.

## Tests unitaires backend (boucle interne, `tdd-auto` sur `GrilleAgendaQuery`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | ~~`Should_Lister_exactement_une_entree_par_responsable_distinct_…`~~ | collection → collection **distincte** | **RETIRÉ (décision PO, porte G4)** — early green **inattendu** : le `.Distinct()` par identifiant stable a été livré dès Sc.1 (read model de légende), ce « driver » passait donc vert sans rouge pilotant du code neuf = **doublon** de la garantie Sc.1. Aucun code de production retiré ; dédoublonnage couvert par le read model de Sc.1. | ❌ Retiré (doublon) |
| 2 | `Should_Porter_le_nom_de_chaque_responsable_dans_ses_propres_cases_When_deux_responsables_couvrent_des_jours_differents` | valeur → valeur (résolution par case) | ⚠️ early green **attendu** — couvert par Sc.1 #1 (résolution du nom par identifiant stable de la période ; appliquée à chaque case) — **caractérisation, pas driver**. Filet : empêche une régression mélangeant les noms entre cases. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun nouveau fichier backend — réutilise le port + read model du Sc.1.)*
- **`src/PlanningDeGarde.Application/GrilleAgendaQuery.cs`** — la dérivation de légende
  passe au `distinct` par identifiant stable.
- *(Rendu cases + Légende à 2 entrées : routé `ihm-builder`.)*

## Design notes

- **Clé de dédoublonnage = identifiant stable** (jamais le nom ni le libellé), cohérent
  règle 17. Deux acteurs au même nom mais id distincts resteraient deux entrées (cas non
  exercé ici, mais la clé est l'id).
- Le #2 est un **filet anti-régression** anticipé : `tdd-auto` le verra vert au 1er passage
  (caractérisation attendue, **pas** un défaut).
