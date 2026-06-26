# Sc.6 — Nom long : lisibilité de la case préservée

`@limite` `🖥️ IHM`

↩ Retour : [00-sprint07-suivi.md](00-sprint07-suivi.md)

**Routage** : le **driver réel est IHM** (troncature + survol dans le `.razor`/CSS, routé
`ihm-builder`). Le backend ne porte qu'une **caractérisation** (`tdd-auto`, ⚠️ early green) :
le read model porte le **nom complet**, jamais tronqué.

## Acceptation (BDD) — niveau **runtime/IHM** (routé `ihm-builder`)

`Should_Tronquer_le_nom_long_dans_la_case_avec_le_nom_complet_accessible_au_survol_et_porter_le_nom_complet_dans_la_legende_When_un_responsable_au_nom_long_est_affecte`

- **Niveau** : E2E/runtime (ou rendu) sur l'app câblée + référentiel réel.
- **Observable** : la case du vendredi 03/07 affiche le nom **sans déborder**, tronqué
  (« Marie-Hélène… ») avec le **nom complet au survol** (attribut `title`) ; la légende
  porte le **nom complet** « Marie-Hélène Grand-Dubois ».

## Tests unitaires backend (boucle interne, `tdd-auto` sur `GrilleAgendaQuery`)

| # | Test unitaire (FLFI) | TPP | Contradiction | Status |
|---|----------------------|-----|---------------|--------|
| 1 | `Should_Porter_le_nom_complet_du_responsable_dans_la_case_et_dans_la_legende_When_son_nom_est_long` | valeur → valeur (chaîne intégrale) | ⚠️ probablement early green — couvert par Sc.1 #1/#2 : le référentiel renvoie la **chaîne complète** et le read model **ne tronque pas**. **Caractérisation, pas driver.** La **troncature + survol** est de la **présentation** (`.razor`/CSS), driver `ihm-builder`. | ✅ GREEN (caractérisation) |

## Fichiers à créer / modifier

- *(Aucun fichier backend.)*
- **`Foyer` réel** — un responsable au nom long (« Marie-Hélène Grand-Dubois ») dans le
  référentiel (fixture / seed) pour l'acceptation runtime.
- **Driver IHM** (routé `ihm-builder`) :
  `src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor` (+ CSS) — troncature
  visuelle de la case (`text-overflow`/`title`), légende non tronquée.

## Design notes

- **Séparation read model / présentation.** Le read model est la **source de vérité du nom
  complet** ; la troncature est **uniquement visuelle**. On n'altère jamais la donnée — la
  légende et le `title` exposent le complet, la case affiche le tronqué.
- Scénario d'**ergonomie de surface** (accepté au gate make-gherkin comme dérivé règle 16) :
  aucune règle métier neuve, valeur portée par le rendu.
