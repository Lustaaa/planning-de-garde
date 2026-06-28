# Rétrospective méthode — sprint 13 (CRUD acteurs — suppression)

> Rétro de la **méthode** (pipeline d'agents/skills/commands), pas du produit.
> 9/9 scénarios livrés, suite 196/196 verte (Docker actif). 6 actions retenues
> par le CP (palier 0, journal D10), **toutes appliquées** après autorisation
> directe du PO (garde Self-Modification sur les fichiers `.claude/`).

## Bilan

### Ce qui a bien marché

- **Backend-d'abord tenu de bout en bout** : 9/9 scénarios, suite 196/196 stable ≥3×
  (sans `--no-build` ni filtre, Docker actif), acceptation runtime IHM 4/4 + intégration
  Mongo réel Sc.1 — rempart anti vert-qui-ment respecté.
- **Autonomie CP effective** : D1→D9 tranchées sans déranger le PO (2 portes seulement),
  toutes journalisées.
- **Collision méthode/spec résolue de façon déterministe** : D8/D9 (gating config règle 9)
  traitée « Option 1 » (signal Risques + séquençage candidat impersonation), convention
  « révisions de règle hors boucle » appliquée sans pré-arbitrer la spec.
- **Prédiction de cascade early-green payante sur 4 scénarios** : Sc.3/Sc.5 backend et
  Sc.8/Sc.9 IHM tombés early-green ATTENDUS (caractérisations batchées), sans escalade.

### À améliorer (avec preuve)

1. **Course `*TempsReel*` masquée par la suite complète.** La touche IHM d'un composant
   partagé (`ConfigurationFoyer` + `@inject SessionPlanning`) a rendu déterministe une course
   `UnknownEventHandlerId` latente dans 7 tests `*TempsReel*` préexistants (select manipulé
   sans attendre l'énumération async). Le warmup de la suite complète la masquait ; seuls des
   runs **en isolation** l'ont révélée. (99-sprint13-retours.md §IA L16.)
2. **Flake bUnit Sc.3** observé 1× puis vert en rerun — même cause racine ; garde
   `WaitForState` ajouté à la main, pas extrait en helper partagé. (§IA L17.)
3. **Pronostic early-green « câblage partagé » faux sur Sc.7.** `ConfigurationFoyer` n'avait
   aucun garde de rôle (`EstParent` n'existait que sur la grille) → Sc.7 = vrai driver
   RED→GREEN. La cascade supposait le prérequis sans le vérifier sur l'écran ciblé. (§IA L18.)
4. **Deux `Write` ont écrasé le fichier de retours existant** (validation-visuelle ET
   spec-consolidation), reconstruit à l'identique — risque réel de perte des décisions CP
   D1→D9 et du journal IA.

## Actions appliquées (CP D10, autorisées PO)

| # | Cible | Édition |
|---|-------|---------|
| A1 | `.claude/agents/ihm-builder.md` | Balayage `*TempsReel*` **EN ISOLATION** après composant Razor partagé ; symptôme `UnknownEventHandlerId` nommé ; vert-en-suite / rouge-en-isolation = course à garder. |
| A1 | `.claude/skills/tdd-implement/SKILL.md` | Garde d'énumération `WaitForState(acteur-foyer)` codifié en **helper bUnit partagé** + audit que tout `*TempsReel*` config/grille le porte. |
| A2 | `.claude/agents/tdd-analyse.md` | Contrôle préalable obligatoire : vérifier par exploration code que le câblage prérequis (garde de rôle) existe **sur l'écran ciblé** avant d'étiqueter early-green câblage partagé ; sinon `@driver`. |
| A3 | `.claude/agents/validation-visuelle.md` | `Edit` ajouté aux `tools` ; édits ciblés/append, **jamais** de full `Write` sur un retours existant. |
| A3 | `.claude/agents/spec-consolidation.md` | Interdiction explicite nommée de `Write`/`Edit` sur `99-sprint<NN>-retours.md` (seule sortie = `nextSpec`). |
| A4 | `.claude/agents/chef-de-projet.md` | Codification : gating de rôle partiel → angle mort en Risques du backlog + séquencer candidat adjacent, sans G1 si l'intention métier est actée. |

## Note de procédure

Le garde **Self-Modification** a bloqué l'agent `retro-sprint` sur l'édition des fichiers
`.claude/` (une direction CP ne porte pas l'autorité utilisateur). Les 6 éditions ont été
appliquées **par le thread principal après autorisation directe du PO** via `AskUserQuestion`.
À reconduire pour toute auto-modification du pipeline : l'autorité doit venir du PO lui-même.
