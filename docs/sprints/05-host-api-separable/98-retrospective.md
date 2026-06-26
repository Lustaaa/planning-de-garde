# Rétrospective méthode — Sprint 05 (host-api-separable)

> Rétrospective SCRUM de la **méthode** (pipeline d'agents/skills/commands), pas du
> produit (ça, c'est `/4-retours`). Source : sections `# Méthode (agents)` et `## IA` de
> `99-sprint05-retours.md`. Gate `/6` étape 1 — dernier maillon avant push/PR.

## Bilan

### Ce qui a marché

- **Sprint structurel mené intégralement en autonomie** : 6/6 scénarios `✅ GREEN`,
  96 tests, hôte d'API séparable + front WASM réel livrés sans accroc de méthode, en
  boucle automatique (pas de blocage `AskUserQuestion` entre scénarios).
- **Routage `tdd-analyse` correct** : scénarios backend → `tdd-auto` (Sc.1/3/4/5),
  scénarios IHM → `ihm-builder` fallback (Sc.2/6) menés en **cycle RED→GREEN de niveau
  runtime** (port TCP réel arrêté pour Sc.6, deux-hôtes réels pour Sc.2), jamais un bUnit
  comme preuve.
- **Early greens anticipés traités en caractérisation** : les tests des Sc.3/4 (Scalar +
  OpenAPI livrés au scaffolding Sc.1) ont été marqués caractérisation après preuve du
  pouvoir discriminant du driver — aucune dérive « ment au vert ».
- **Enchaînement gate visuel → `/4-retours` → `/5-consolidation` sans rupture** : aucune
  question reposée, propagation README mécanique (pointeur → v06) tenue.
- **Discipline git tenue malgré l'outil cassé** : branche ≠ `main`, staging sélectif,
  trailer `Co-Authored-By` respectés même en commit manuel — la discipline a survécu au
  contournement de l'outil.

### À améliorer (avec preuve)

| Friction | Preuve |
|----------|--------|
| Skill `git` inutilisable sur ce dépôt à cause du chemin accentué « privée » | `99-sprint05-retours.md` §IA : `Set-Location (git rev-parse --show-toplevel)` échoue (git émet le chemin en UTF-8, PowerShell le décode dans la code page console → « privée » corrompu). Ligne dupliquée dans les 6 scripts (`commit`, `branch`, `pr`, `push`, `status`, `sync`). Conséquence : phase IHM + commits `/4` et `/5` faits à la main, garde-fous outillés contournés à chaque commit. |
| Agents `ihm-builder` et `validation-visuelle` absents du registre → fallback `general-purpose` récurrent | `99-sprint05-retours.md` §IA : 3e sprint consécutif (03, 04, 05). Le fallback est déjà documenté comme régime nominal ; la récurrence en rétro est le bruit à éteindre. |
| Subagent `retro-sprint` bloqué par le classifieur d'auto-mode sur l'édition `.claude/` | Le subagent a refusé l'autorité PO relayée par le coordinateur (même citée verbatim), exigeant un message utilisateur direct qu'il ne peut pas recevoir. 3e occurrence (s03, s04, s05) — la dette prédite au s04 se confirme. |

## Actions appliquées

Les 3 actions ont été **validées par le PO** (priorisation multiSelect + arbitrage 3a/3b)
et **appliquées au thread principal** — le subagent `retro-sprint` a de nouveau été
**bloqué par le classifieur d'auto-mode** sur l'édition de `.claude/` (autorité PO relayée
par le coordinateur non reconnue, même limite qu'aux sprints 03 et 04). L'autorité directe
du PO (sélection via `AskUserQuestion`) a permis l'application depuis le thread principal.

| # | Cible | Édition | Origine |
|---|-------|---------|---------|
| 1 | `.claude/skills/git/scripts/` : `commit.ps1`, `branch.ps1`, `pr.ps1`, `push.ps1`, `status.ps1`, `sync.ps1` | Force l'encodage UTF-8 de la sortie git puis `Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()` — corrige le décodage du chemin accentué et fiabilise le repositionnement. Réarme le skill git au lieu du commit manuel. | §IA (cause racine friction 1) |
| 2 | `.claude/skills/git/SKILL.md` | Section **« Chemins non-ASCII (dépôt « privée ») »** : documente le fix d'encodage + balise le commit manuel comme **dernier recours** tenant les garde-fous à la main. | §IA (traçabilité) |
| 3a | `.claude/agents/ihm-builder.md`, `.claude/agents/validation-visuelle.md` | Fallback `general-purpose` **acté définitif** (« confirmé rétros sprints 03→05, ne plus le relever en rétro »). Clôt le sujet récurrent. Choix PO (3a retenue, 3b « documenter une tentative de chargement » écartée : registre non pilotable depuis le dépôt). | §IA (récurrence s03→s05) |

## Notes / dette ouverte

- **Limite du classifieur d'auto-mode sur l'autorité relayée — confirmée 3 fois (s03, s04,
  s05).** Le contournement « appliquer au thread principal » fonctionne mais reste un
  contournement. La recommandation du s04 se durcit : **les actions de rétro devraient être
  appliquées par défaut par le thread principal**, `retro-sprint` se limitant à produire
  bilan + actions + priorisation (analyse), sans tenter les éditions `.claude/` lui-même.
  Candidat d'édition du pipeline (`.claude/commands/6-cloture-sprint.md` / `retro-sprint`)
  pour un prochain sprint — non appliqué ici faute d'avoir été proposé au PO ce tour.
- **Registre d'agents hors dépôt — clos.** L'action 3a acte définitivement le fallback ;
  on cesse de relever cette non-anomalie. La vraie cible (charger les agents en session)
  reste hors de portée du dépôt, par construction.
