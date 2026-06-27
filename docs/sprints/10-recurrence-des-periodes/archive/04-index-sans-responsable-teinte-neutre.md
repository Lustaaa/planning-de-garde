# Sc.4 — Un index de cycle sans responsable retombe sur la teinte neutre

`@limite`

↩ Retour : [00-sprint10-suivi.md](00-sprint10-suivi.md)

**Routage** : tranche **backend = CARACTÉRISATION** (`tdd-auto`, ⚠️ early green **attendu** si la
résolution du fond suit le **contrat de repli** `mapping[index] absent → pas de fond → neutre`, miroir de
`IPaletteCouleurs.CouleurDe` / `IReferentielResponsables.NomDe` — **leçon s03/s09 Sc.5**) **+** acceptation
**runtime IHM** (`ihm-builder`). **Driver SEULEMENT si** la résolution du fond indexe en dur (exception
sur index non mappé) — alors l'ex.1 force la garde index-non-mappé.

> **Scenario Outline** (3 exemples) : `parent-a` = Alice bleu, `parent-b` = Bruno orange, aucune période.
> | mapping | semaine | rendu attendu |
> |---|---|---|
> | pair → Parent A, impair **non affecté** | ISO 27 (impaire) | gris neutre, sans nom, **aucune** entrée de légende |
> | pair → Parent A, impair **non affecté** | ISO 28 (paire) | « Parent A » bleu (**contrôle positif** = pur Sc.1) |
> | **aucun** index affecté (cycle vide) | ISO 27 (impaire) | gris neutre, sans nom, **aucune** entrée de légende |

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

> Sur l'app câblée, avec un cycle dont l'index de la semaine courante n'est **pas** affecté (ou un cycle
> vide), les cases de fond s'affichent en **gris neutre, sans nom**, et **aucune entrée de légende** n'est
> ajoutée pour cet index — tandis qu'un index affecté (contrôle positif) reste nommé et coloré.

`Should_Afficher_les_cases_de_fond_en_gris_neutre_sans_nom_ni_entree_de_legende_When_l_index_de_la_semaine_n_est_associe_a_aucun_responsable_de_fond` — ⏳ Pending *(runtime, `ihm-builder`)*

Frontière Application (`tdd-auto`) : ✅ GREEN — `Acceptation_Should_Retomber_en_gris_neutre_sans_nom_ni_entree_de_legende_sur_la_semaine_d_index_non_affecte_…` (suite complète 172/172)

## Tests unitaires backend (boucle interne, `tdd-auto`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Retomber_sur_la_teinte_neutre_sans_nom_ni_entree_de_legende_When_l_index_de_la_semaine_n_est_associe_a_aucun_responsable_de_fond` *(Theory, 3 exemples de l'Outline)* | — | ⚠️ **DRIVER confirmé (PAS early green)**. `CycleDeFond.ResponsableDeFond` indexe en DUR via `_affectations[index]` → `KeyNotFoundException` sur index non mappé. Le repli neutre n'existe pas encore → vrai rouge. Émergence minimale forcée : `TryGetValue → null` (priorité fond > neutre, branche neutre existante du `CaseJourAu` prend le relais : gris, nom vide, légende sans entrée fantôme). Contrôle positif (ISO 28 → Parent A) inchangé (pur Sc.1). **Émergence : `CycleDeFond.ResponsableDeFond` → `TryGetValue → null` (1 ligne).** | ✅ GREEN |

## Fichiers à créer / modifier (backend uniquement ici)

- **Aucun fichier neuf attendu** si la résolution du fond suit le contrat de repli (`mapping[index]`
  absent → null → branche neutre existante). Sinon, la résolution gagne la garde « index non mappé → pas
  de fond ».
- **Doublures tests** — `Fake` du port cycle avec mapping **partiel** (un seul index affecté) et **cycle
  vide** (aucun index) ; `GrilleAgendaQuery` projeté sur ISO 27 (impaire) et ISO 28 (paire).
- **Volet runtime IHM (routé `ihm-builder`)** — grille rendant les cases neutres + légende sans entrée
  fantôme pour l'index non affecté.

## Design notes

- **Contrat de repli homogène** (leçon s03/s09 Sc.5) : comme `CouleurDe` (clé absente → `CouleurNeutre`)
  et `NomDe` (clé absente → repli), la résolution du fond retombe sur « pas de responsable » → neutre,
  sans nom, sans légende. C'est un **contrat**, pas un calcul neuf — d'où l'early green.
- **Aucune entrée de légende fantôme** : la légende n'agrège que les responsables de fond **réellement
  présents** (mappés) dans la fenêtre — un index non mappé ne produit aucune entrée (cohérent s07 Sc.3 /
  s09 Sc.6).
- **Contrôle positif intégré** (ex.2) : verrouille que le repli ne casse pas la résolution d'un index
  **affecté** sur la même semaine voisine — pur Sc.1, gardé dans le même Theory.
- **Caractérisation conservée** : @limite documentant le repli ; pas de rouge attendu sauf indexation en
  dur. `tdd-auto` confirmera (si rouge → driver de la garde index-non-mappé, à ne pas supprimer).
