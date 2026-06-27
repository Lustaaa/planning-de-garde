# Rétrospective méthode — Sprint 09 (config foyer persistante)

> Rétro SCRUM de la **méthode** (agents / skills / commands du pipeline), pas du produit
> (ça, c'est `/4-retours`). Source : sections `# Méthode (agents)`, `## IA` et `# Décisions
> autonomes (chef de projet)` de `99-sprint09-retours.md` + `00-sprint09-suivi.md`.

## Bilan SCRUM

### Ce qui a marché

- **Pivot durabilité Sc.3 prouvé sur Mongo RÉEL via Docker** (case + légende nommée après
  redémarrage), jamais une doublure — garde-fou anti vert-qui-ment (R4) tenu, skip propre si
  Docker absent.
- **Routage backend (`tdd-auto`) vs IHM/intégration (`ihm-builder`)** tranché par scénario et
  respecté de bout en bout (Sc.1/2/3/8/9 runtime/intégration, Sc.4–7 caractérisations backend).
- **Early-green ANTICIPÉ par `tdd-analyse`** : Sc.4–7 annotés « probablement early green » →
  marqués ✅ GREEN (caractérisation) sans fausse alarme G4. La leçon s07 a tenu.
- **Non-régression complète SANS `--no-build`** a fini par révéler la régression : le RED de
  Sc.2 a attrapé la casse introduite en Sc.1 — le garde-fou a joué son rôle.
- **Garde-fou découpe CP** : fusion ajout + édition durable bornée (règle 30 anti-cliquet,
  reste du domaine InMemory), tranche de secours documentée, sans escalade PO inutile.
- **Harness de transport déterministe au Sc.9** (`GrilleRuntimeHarness.ClientVersAvecEcritureInjoignable`
  + `WaitForState`) a résolu la flakiness Docker, suite stabilisée 161/161 prouvée ≥5× Docker actif.

### À améliorer (avec preuve)

- **Vélocité** — la durée d'un sprint est jugée excessive par le PO (friction prioritaire).
  Preuve : 9 scénarios dont 4 caractérisations early-green (Sc.4–7) chacune menée en round-trip
  orchestrateur↔agent séparé (dispatch + checkpoint + commit) sans piloter de code neuf
  (~5 drivers réels pour 9 scénarios).
- **Régression sur composant partagé révélée tardivement** : l'énumération async ajoutée à
  `ConfigurationFoyer` en Sc.1 a cassé/rendu flaky un test runtime s08 (handler stale
  `UnknownEventHandlerIdException`), non détecté au commit Sc.1, révélé seulement au RED de Sc.2.
- **Suite runtime bUnit « TempsReel » flaky sous Docker** (sockets réels + timing) :
  non-déterministe quand Docker tourne, brouille la non-régression de Sc.5/Sc.8/Sc.9 ; HEAD
  propre = 37/37. Résolue seulement au Sc.9 par un pattern réinventé sur place, non capitalisé.
- **Fallback `ihm-builder` systématique avec prompt de rappel lourd** : le type n'étant pas
  chargeable, tous les scénarios IHM tombent en `general-purpose` ; le rappel non-régression est
  recopié à l'identique aux étapes 4/7/8 de `3-tdd-implement.md`.

## Actions

| # | Action | Cibles | Statut |
|---|--------|--------|--------|
| A1 | **Batch des caractérisations early-green** anticipées consécutives en un seul run/commit ; STOP G4 immédiat si early-green inattendu ; un scénario = un commit reste la règle générale ; un driver réel rompt le lot. | `.claude/agents/tdd-auto.md` (§ Lot de caractérisations early-green) + `.claude/commands/3-tdd-implement.md` (note étape 6) | ✅ Appliquée |
| A2 | **Budget de vélocité make-gherkin** : annoter drivers réels vs caractérisations, regrouper en `Scenario Outline`/`Examples`, viser un nombre resserré de scénarios, signaler les lots groupables. | `.claude/skills/make-gherkin/SKILL.md` (étape 5) + `.claude/agents/make-gherkin.md` (étape 4) | ✅ Appliquée |
| A3 | **Balayage runtime après composant partagé** : relancer nommément la suite runtime `Web.Tests` existante après tout ajout touchant un composant partagé, avant le commit du scénario. | `.claude/agents/tdd-auto.md` (GREEN_PHASE) + `.claude/agents/ihm-builder.md` (VERIFY) | ✅ Appliquée |
| A4 | **Convention harness runtime déterministe anti-flake Docker** : handler de transport déterministe (vs port loopback réel altéré par le proxy Docker) + `WaitForState` + prérequis Docker documenté. | `.claude/skills/tdd-implement/SKILL.md` (§ Scénario IHM) + `.claude/agents/ihm-builder.md` (ACCEPT_RED) | ✅ Appliquée |
| A5 | **Factoriser le rappel fallback IHM** répété 3× (étapes 4/7/8) en un bloc nommé unique ; alléger le prompt de dispatch general-purpose. | `.claude/commands/3-tdd-implement.md` | ⏳ Reportée (non présentée au PO ce tour — limite d'affichage ; à reproposer) |

## Note de procédure

- Le garde **self-modification** du subagent `retro-sprint` a refusé d'éditer les fichiers
  `.claude/*` malgré l'aval relayé : un subagent exige une action de **première main** du thread
  principal. Les éditions A1–A4 ont donc été **appliquées par le thread principal** sur
  autorisation directe du PO (sélection `AskUserQuestion`). À garder en tête pour les prochaines
  rétros : l'application des actions méthode aux fichiers `.claude/` revient au thread principal,
  pas au subagent.

## Friction PO de fond (renvoyée hors méthode)

- La **vélocité du pipeline** motive la décision PO d'une **refacto technique HORS processus**
  (cap : restructuration du code applicatif, iso-comportement, invariant 161/161). A1+A2
  attaquent la vélocité côté méthode ; la refacto, elle, est un chantier produit/technique
  séparé (cf. `99-sprint09-besoins-fin-itération.md`).
