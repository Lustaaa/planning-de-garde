# Sc.1 — Une période affectée affiche le nom et entre dans la légende

`@nominal` `🖥️ IHM`

↩ Retour : [00-sprint07-suivi.md](00-sprint07-suivi.md)

**Routage** : tranche read-model **backend** (`tdd-auto`, 2 drivers) **+** acceptation
**runtime IHM** (`ihm-builder`). C'est le **scénario fondation** : il introduit la
résolution du **nom** (miroir couleur) et la **dérivation de la légende**.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Le symptôme PO est un fait d'usage **runtime** : sur la grille **réellement câblée**
> (front WASM + API distante réelle, palette + référentiel réels), la case affiche le
> **nom** et la légende le restitue. **Pas** un test bUnit à doublure (qui ne prouve ni la
> DI réelle ni le chemin de lecture HTTP).

`Should_Afficher_le_nom_Alice_dans_la_case_du_lundi_29_06_2026_avec_son_fond_de_responsabilite_et_une_entree_de_legende_Alice_When_la_grille_reellement_cablee_est_affichee_avec_une_periode_affectee_a_Alice`

- **Niveau** : E2E/runtime sur l'app câblée (pattern `ApiDistanteFactory` + vue réelle, cf.
  `FrontWasmPeriodeParentColoreeTests`), palette **réelle** (`parent-a → bleu`) et
  référentiel **réel** (`parent-a → « Alice »`). Anti « vert qui ment » : si la case ne
  rend pas le nom, ou si le référentiel n'est pas câblé, l'observable est vide → rouge.
- **Observable** : la case du lundi 29/06/2026 porte le texte « Alice » **et** sa couleur
  de responsabilité ; le composant **Légende** rendu contient **exactement une** entrée
  « Alice ».

## Tests unitaires backend (boucle interne, `tdd-auto` sur `GrilleAgendaQuery`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Porter_le_nom_du_responsable_dans_les_cases_couvertes_par_sa_periode_When_une_periode_lui_est_affectee_dans_la_fenetre` | néant → valeur dérivée (résolution nom via le port, miroir `CouleurDe`) | **Driver** — aujourd'hui `JourCase` ne porte **aucun nom** : la case couverte ne peut exposer « Alice » → rouge. Force l'ajout du champ nom + sa résolution via `IReferentielResponsables` sur l'identifiant stable de la période. | ✅ GREEN |
| 2 | `Should_Inscrire_le_responsable_present_dans_la_legende_avec_son_nom_et_sa_couleur_When_sa_periode_couvre_un_jour_de_la_fenetre` | néant → collection dérivée (légende = projection des présents) | **Driver** — il n'existe **aucune** légende dans le read model : `GrilleAgenda.Légende` est inexistante/vide → rouge. Force la dérivation `{ identifiantStable, nom, couleur }` pour le responsable présent dans la fenêtre. | ✅ GREEN |

## Fichiers à créer / modifier (backend uniquement ici)

- **`src/PlanningDeGarde.Application/IReferentielResponsables.cs`** (nouveau) — port
  `NomDe(string identifiantStable) → string`, miroir d'`IPaletteCouleurs`.
- **`src/PlanningDeGarde.Application/GrilleAgenda.cs`** — `JourCase` enrichi (nom +
  identifiant stable du responsable) ; nouveau record `EntreeLegende(identifiantStable,
  nom, couleur)` ; `GrilleAgenda` gagne `IReadOnlyList<EntreeLegende> Légende`.
- **`src/PlanningDeGarde.Application/GrilleAgendaQuery.cs`** — résout le nom à côté de la
  couleur sur l'identifiant stable ; dérive la légende des responsables présents.
- **`src/PlanningDeGarde.Infrastructure/FoyerReferentielResponsables.cs`** (nouveau) +
  `Foyer` enrichi des noms (`parent-a → « Alice »`, `parent-b → « Bruno »`).
- **`tests/PlanningDeGarde.Tests/Fakes/FakeReferentielResponsables.cs`** (nouveau, miroir
  `FakePaletteCouleurs`).
- *(Rendu case + composant Légende : hors backend — routé `ihm-builder`.)*

## Design notes

- **Miroir strict palette.** Le port nom a la **même forme** que `IPaletteCouleurs`
  (résolution par identifiant stable, repli déterministe pour id inconnu). Ne pas inventer
  d'autre mécanique : la cohérence avec le pattern existant est l'objectif.
- **Nom ≠ couleur, indépendants.** Le nom se résout via le référentiel **même quand** la
  couleur retombe en neutre (préparé pour Sc.5). Aucune dépendance nom↔couleur.
- **Légende = présents dans la fenêtre** (décision CP Q2) — dérivée des périodes couvrant
  un jour de la fenêtre affichée, **pas** le catalogue déclaré du foyer.
- **Couleur dans la légende** = couleur **déjà résolue** (palier 2) ; la légende ne
  recalcule rien, elle surface. En backend, palette injectée (couleurs Gherkin) ; en
  runtime, palette réelle.
