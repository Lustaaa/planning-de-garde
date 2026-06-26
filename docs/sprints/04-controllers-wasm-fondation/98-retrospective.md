# Rétrospective méthode — Sprint 04 (controllers-wasm-fondation)

> Rétrospective SCRUM de la **méthode** (pipeline d'agents/skills/commands), pas du
> produit (ça, c'est `/4-retours`). Source : sections `# Méthode (agents)` et `## IA` de
> `99-sprint04-retours.md`. Gate `/6` étape 1 — dernier maillon avant push/PR.

## Bilan

### Ce qui a marché

- **Gate rétro non contournable tenu** : `find-retro.ps1` + `/6` étape 1 ont imposé la
  rétrospective méthode avant tout push, comme prévu.
- **Fichier unifié `99-sprint04-retours.md` bien cloisonné** : les sections `# Méthode` et
  `## IA` ont capté les frictions à la volée sans polluer la partie produit ;
  `retours-challenge` a correctement ignoré la partie méthode.
- **Auto-réparation du lanceur documentée** : le fix `run.ps1` (`$PSScriptRoot` au lieu de
  `git rev-parse`) appliqué ET consigné en `## IA` avec sa cause racine (décodage UTF-8 d'un
  chemin accentué).
- **Sprint 04 100 % backend assumé** : tous les scénarios étant domaine/écriture, le
  pipeline TDD (`tdd-analyse` → `tdd-auto`) a tourné sans dépendre de l'IHM ; le suivi
  reflète cet axe.

### À améliorer (avec preuve)

| Friction | Preuve |
|----------|--------|
| BACKLOG consolidé trop tard / hors étape explicite | `99-sprint04-retours.md` §Méthode : le PO veut `docs/BACKLOG.md` consolidé après la rétro, avant la fin du sprint. `/6` étape 4 ne faisait que cocher des lignes ✅ ; aucune étape de consolidation épics/paliers/prochains-sprints avant le handoff `/2`. |
| Agents `ihm-builder` ET `validation-visuelle` absents du registre → fallback `general-purpose` récurrent | `99-sprint04-retours.md` §IA : fallback pour la phase IHM finale et le gate visuel ; déjà signalé au sprint 03 (récurrent). Les fichiers existent mais ne sont pas chargés en session. |
| Lanceur `run.ps1` fragile face aux instances Web zombies | `99-sprint04-retours.md` §IA : instance Web zombie verrouillant les DLL (MSB3027) au gate visuel ; le fix d'encodage est fait, mais le lanceur ne tuait pas les processus résiduels avant build. |
| Rôle de `/4-retours` mal compris + refs internes `#NN` opaques | Friction PO : il pensait qu'on dessinait le sprint ; les refs `#NN` de l'agent (numéros de ligne de classification) n'existent pas dans `docs/BACKLOG.md` → déroutant. |

## Actions appliquées

Les 4 actions ont été **validées par le PO** (priorisation multiSelect) et **appliquées au
thread principal** — le subagent `retro-sprint` a été **bloqué par le classifieur d'auto-mode**
sur l'édition de `.claude/commands/` (autorité PO relayée par le coordinateur non reconnue,
même limite qu'au sprint 03). L'autorité directe du PO (sélection via `AskUserQuestion`) a
permis l'application depuis le thread principal.

| # | Cible | Édition | Origine |
|---|-------|---------|---------|
| 1 | `.claude/commands/6-cloture-sprint.md` | Sous-étape **« 4bis — Consolidation du product backlog »** (après le passage à ✅ fait, avant le handoff `/2`) : relire `BACKLOG.md` et consolider épics + paliers + « Prochains sprints envisagés » depuis le backlog du sprint clos. Note « Boucle complète » mise à jour. | Retour PO direct (§Méthode) |
| 2 | `.claude/agents/ihm-builder.md`, `.claude/agents/validation-visuelle.md`, `.claude/commands/3-tdd-implement.md` | Fallback `general-purpose` **documenté comme régime nominal** (registre non pilotable depuis le dépôt) : ce n'est pas une dégradation à corriger. Choix PO. | §IA (récurrence s03/s04) |
| 3 | `.claude/skills/run/scripts/run.ps1` | Passe d'**arrêt des instances Web résiduelles** avant build (`Get-CimInstance` filtré sur le binaire/projet Web puis `Stop-Process`), + doc `.DESCRIPTION`. | §IA (MSB3027) |
| 4 | `.claude/agents/retours-challenge.md`, `.claude/commands/4-retours.md` | (a) Anti-règle : **pas de refs internes opaques `#NN`** en sortie, citer le libellé court. (b) Cadrage : **`/4-retours` priorise, ne conçoit pas le sprint** (intro + étape Validation). | Friction PO `/4` |

## Notes / dette ouverte

- **Limite du classifieur d'auto-mode sur l'autorité relayée** : pour la 2e fois (s03, s04),
  un subagent du pipeline ne peut pas éditer `.claude/commands/` sur ordre du coordinateur,
  même après validation PO explicite. Contournement appliqué : application au thread
  principal. À surveiller — si récurrent, envisager que les actions de rétro soient
  toujours appliquées par le thread principal plutôt que par `retro-sprint`.
- **Registre d'agents hors dépôt** : la vraie cible (charger `ihm-builder` /
  `validation-visuelle` en session) reste hors de portée du dépôt ; l'action 2 lève
  seulement l'apparence d'anomalie, elle ne charge pas les agents.
