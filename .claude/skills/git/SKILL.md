---
name: git
description: À utiliser pour toute action git courante sur le projet planning-de-garde (état, mise à jour, branche, commit, push, PR) — chaque commande est adossée à un script PowerShell qui porte les garde-fous maison (jamais de commit sur main, jamais de `git add -A`, convention de branche ia-{type}/{slug}, trailers Claude). Préférer ces scripts aux commandes git brutes.
---

# git — Commandes courantes adossées à des scripts

Toutes les opérations git de ce projet passent par les scripts de
`.claude/skills/git/scripts/` (PowerShell 7). Ils encapsulent les garde-fous :
pas de commit sur `main`, staging **sélectif** (jamais `git add -A`), branches
`ia-{type}/{slug}`, trailers `Co-Authored-By` / Claude Code.

> Lance chaque script avec `pwsh .claude/skills/git/scripts/<x>.ps1 …`.
> Branche par défaut = `main`. Remote = `origin`.

## Quand l'utiliser

Dès qu'une action git est demandée en langage naturel : « où en est git »,
« commit », « crée une branche », « pousse », « ouvre une PR », « mets à jour ».
Route vers la commande correspondante ci-dessous au lieu d'exécuter git à la main.

## Commandes

### `status` — état du dépôt
Branche courante, ahead/behind vs upstream, fichiers modifiés.
```
pwsh .claude/skills/git/scripts/status.ps1
```

### `sync` — mettre à jour main
Fast-forward de `main` depuis `origin/main` **sans le checkouter** (ou
`pull --rebase` si tu es déjà dessus). Exige un arbre propre.
```
pwsh .claude/skills/git/scripts/sync.ps1
pwsh .claude/skills/git/scripts/sync.ps1 -RebaseCurrent   # rebase la branche courante sur main
```

### `branch` — créer une branche de travail
Crée `ia-{TYPE}/{slug}` depuis `main` à jour. `TYPE ∈ {fix, feat, refactor, test,
chore}`. Le slug est normalisé en kebab-case.
```
pwsh .claude/skills/git/scripts/branch.ps1 -Type refactor -Slug "git skill scripts"
# -> ia-refactor/git-skill-scripts
```

### `commit` — committer (staging sélectif)
**Refuse `main`/`master`.** Stage uniquement les fichiers passés (`-Files`),
ajoute le trailer `Co-Authored-By` s'il manque.
```
pwsh .claude/skills/git/scripts/commit.ps1 -Message "refactor: extrait le skill git" -Files .claude/skills/git
```
- `-Message` : sujet (+ corps) du commit.
- `-Files` : liste de chemins à stager — **obligatoire**, pas de `git add -A`.
  ⚠️ **Séparés par virgules, sans espace** : `-Files a,b,c`. La forme `-Files a b c`
  (espaces) **échoue** (« A positional parameter cannot be found »).

### `push` — pousser la branche
**Refuse `main`/`master`.** `-u origin` au premier push, sinon push simple.
```
pwsh .claude/skills/git/scripts/push.ps1
pwsh .claude/skills/git/scripts/push.ps1 -ReturnToMain   # revient sur main après
```

### `pr` — ouvrir une pull request
Via `gh`, base `main`. Pousse d'abord si l'upstream manque. Ajoute le trailer
« Generated with Claude Code » au corps.
```
pwsh .claude/skills/git/scripts/pr.ps1 -Title "Skill git" -Body "Ajoute le skill git." [-Draft]
```

## Enchaînement type

```
status  →  branch -Type … -Slug …  →  (édition)  →  commit -Message … -Files …  →  push  →  pr
```

## Chemins non-ASCII (dépôt « privée »)

Le dépôt vit sous un chemin accentué (`…/source/privée/…`) : les six scripts forcent
l'encodage UTF-8 (`$OutputEncoding`/`[Console]::OutputEncoding`) et se repositionnent via
`Set-Location -LiteralPath (git rev-parse --show-toplevel).Trim()` en tête — **ne jamais
retirer ces lignes** en éditant un script (sans elles, les accents se corrompent et tout
le skill devient inutilisable).

Un **commit manuel** (`git add <chemins> && git commit`) n'est légitime qu'en **dernier
recours**, si un script reste cassé : il faut alors tenir les garde-fous à la main
(branche ≠ `main`, staging **sélectif** sans `git add -A`, trailer `Co-Authored-By`).
Ne jamais laisser un contournement silencieux des garde-fous passer pour normal.

**Syntaxe selon le shell du tool.** Un commit manuel multi-ligne doit respecter le shell
qui l'exécute. Le **tool Bash** exécute du **POSIX sh** : la here-string PowerShell
`@'…'@` n'y existe pas et **fuit telle quelle dans le message**. Pour un message
multi-ligne : soit le **tool PowerShell**
avec une vraie here-string `@'…'@` (`'@` collé en colonne 0), soit le **tool Bash** avec
des `-m` répétés (`git commit -m "sujet" -m "corps" -m "trailer"`) ou un heredoc `sh`.
**Ne jamais mélanger** here-string PowerShell et tool Bash. Préférer de toute façon le
script `commit.ps1` (garde-fous portés).

## Garde-fous (portés par les scripts)

- **Jamais de commit/push direct sur `main`/`master`.**
- **Staging sélectif** — `commit` exige `-Files`, jamais `git add -A`.
- **Commit SCOPÉ aux pathspecs + garde anti-index-pollué** *(durci s31)* — `commit` committe avec
  `-- @Files` (pathspec) : un `git commit` nu committe **tout l'index**, donc happerait un fichier
  **étranger pré-stagé** par un **process/agent concurrent** ; scoper garantit qu'on ne committe **que**
  nos fichiers. Le script **signale** aussi un index déjà peuplé avant staging (soupçon de pré-stage
  concurrent). Refuse enfin un **HEAD détaché** (un `git checkout <sha>` concurrent a pu détacher HEAD
  hors de la branche de travail). *(Friction réelle s31 : un process concurrent a basculé hors branche +
  committé pendant un scénario.)*
- **Branche** — toujours `ia-{type}/{slug}`, créée depuis `main` à jour.
- **Arbre propre exigé** par `sync` et `branch` (commit/stash avant).
- **Trailers** — `Co-Authored-By` (commit) et Claude Code (PR) ajoutés si absents.
- **Validation utilisateur** — proposer commit/push/PR à l'utilisateur avant de
  lancer le script ; ne jamais committer/pousser de façon non sollicitée.
