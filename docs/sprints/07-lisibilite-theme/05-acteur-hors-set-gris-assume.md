# Sc.5 — Acteur hors set (gris assumé) : nom conservé, teinte neutre

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint07-suivi.md](00-sprint07-suivi.md)

**Routage** : backend = **caractérisation** (`tdd-auto`, ⚠️ early green) **+** acceptation
**runtime IHM** (`ihm-builder`). **Gris ASSUMÉ** (conforme, permanent), **pas** gris-bug.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

`Should_Afficher_le_nom_grand_pere_sur_fond_gris_neutre_et_une_entree_de_legende_grand_pere_gris_When_un_acteur_hors_set_au_identifiant_stable_valide_est_responsable`

- **Niveau** : E2E/runtime sur l'app câblée + référentiel **réel** déclarant « grand-père »
  (id stable valide, **absent du set couleur**).
- **Observable** : la case du samedi 04/07 affiche « grand-père » sur fond **gris neutre** ;
  la légende contient une entrée « grand-père » (gris). Le gris **traduit un acteur non
  encore colorié**, pas un défaut de résolution.

## Tests unitaires backend (boucle interne, `tdd-auto` sur `GrilleAgendaQuery`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Conserver_le_nom_de_l_acteur_hors_set_avec_une_couleur_neutre_et_une_entree_de_legende_grise_When_un_acteur_au_identifiant_stable_valide_non_colorie_est_responsable` | composition de résolutions (nom + couleur) | ⚠️ probablement early green — couvert par Sc.1 #1 (nom résolu via le référentiel, **indépendant** de la palette) **+** contrat `IPaletteCouleurs.CouleurDe` (repli neutre **déjà garanti** sur clé absente — note s06). **Caractérisation, pas driver.** | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun nouveau fichier backend.)*
- **`Foyer` réel** (Infrastructure) — déclare l'acteur **« grand-père »** : identifiant
  stable valide (responsable connu), **nom** « grand-père » dans le référentiel,
  **absent** de `CouleursParActeur` → repli neutre.
- *(Rendu case grise + nom + entrée légende grise : routé `ihm-builder`.)*

## Design notes

- **Gris ASSUMÉ vs gris-BUG.** Le gris exercé ici est le **gris assumé** (acteur
  légitimement hors set, id valide), **conforme et permanent** (décision CP). Le **gris-bug**
  (libellé fourni à la place de l'identifiant) est **déjà** corrigé à la source et gardé par
  la **caractérisation s06 Sc.8** — **ne pas le redoubler** ici.
- **Le nom ne dépend pas de la couleur.** C'est l'invariant clé du scénario : couleur
  effondrée en neutre, **nom intact** → l'observable « qui garde » (règle 16) tient malgré
  la teinte neutre. Déjà mécaniquement assuré par la résolution indépendante du Sc.1, d'où
  l'early green ; le test reste un **filet** explicite.
