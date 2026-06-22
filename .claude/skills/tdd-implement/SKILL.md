---
name: tdd-implement
description: À utiliser pour implémenter un fichier de scénarios produit par make-gherkin (docs/init/scenarios/<sujet>.md), UN scénario à la fois, en BDD + TDD (.NET backend, Blazor/SignalR front) — chaque scénario Gherkin devient un test d'acceptation exécutable (boucle externe BDD) piloté par des cycles unitaires rouge/vert (boucle interne TDD), puis commité.
---

# TDD Implement

## Vue d'ensemble

Implémenter un fichier de scénarios `make-gherkin` en **BDD + TDD**, **un scénario
à la fois**. C'est la 3ᵉ pipeline : entrée = `docs/init/scenarios/<sujet>.md`,
sortie = du code testé, commité scénario par scénario.

**Principe central — la double boucle :**
- **Boucle externe (BDD)** : chaque `Scenario N` Gherkin devient un **test
  d'acceptation exécutable** (Given/When/Then mappés 1:1). Il échoue d'abord, et
  ne passe au vert que quand le comportement observable est livré.
- **Boucle interne (TDD)** : pour faire passer l'acceptation, on écrit des tests
  unitaires rouges puis l'implémentation minimale (YAGNI) qui les rend verts.

Cible technique : `.NET` backend, `Blazor` + `SignalR` front, tests `xUnit`
(backend), `bUnit` (composants Blazor), test d'intégration pour le temps réel.

## Quand l'utiliser

- Après `make-gherkin`, pour transformer les scénarios en code.
- Un scénario à la fois — pas d'implémentation en bloc.

## Processus

1. **Lis le fichier de scénarios.** Charge `docs/init/scenarios/<sujet>.md` :
   la section `## Analyse technique` (composants, contrats, points TDD) et la
   section `## Scénarios`. Repère le **prochain scénario non implémenté** =
   **premier scénario sans tag `@vert`** (ordre de numérotation continue), ou le
   scénario demandé.

2. **Vérifie la solution .NET.** Si aucune solution n'existe et que rien n'a
   encore été scaffoldé → **pose la question de scaffolding** (round-trip) :
   structure des projets (backend, Blazor, tests), avant d'écrire le moindre test.
   Ne scaffolde jamais en silence une arborescence structurante.

3. **Boucle externe (BDD) — écris le test d'acceptation rouge.** Traduis le
   scénario cible en un test exécutable :
   - `Given` → arrange (Fakes / Givens, état initial).
   - `When` → act (l'action déclenchée).
   - chaque ligne `Then` observable → un `assert`.
   - Les tags `@nominal` / `@limite` / `@erreur` orientent le type d'assertion.
   - Temps réel `SignalR` → test d'intégration où un **second client** observe
     l'état (le critère de succès est l'état vu par l'autre client, pas « ça se
     met à jour »).

4. **Confirme le ROUGE.** Lance le test → il **doit** échouer (sinon le test
   n'observe rien : réécris-le).

5. **Boucle interne (TDD).** Écris l'**implémentation minimale** (YAGNI) pour
   satisfaire le scénario, en t'appuyant au besoin sur des cycles unitaires
   rouge/vert pour les briques métier (handlers, services, calculs).

6. **Confirme le VERT.** Lance le test d'acceptation **et** la suite complète
   (non-régression). Tout doit être vert.

7. **Marque le scénario vert dans le fichier de scénarios.** Édite
   `docs/init/scenarios/<sujet>.md` : ajoute le tag `@vert` au-dessus du scénario
   livré (à côté de son tag de type) et une ligne `# vert — <commit court>`.
   C'est l'**état d'avancement** : un scénario taggé `@vert` est implémenté et au
   vert ; les autres restent à faire. (Détection du « prochain » à l'étape 1.)

8. **Commit.** Test(s) + implémentation **+ la mise à jour du fichier de
   scénarios**, message référant le scénario
   (ex. `feat: scénario 3 — réservation d'un créneau libre`).

9. **Checkpoint.** Rends la main avec le récap (rouge → vert → commit). Sur une
   **ambiguïté technique réelle** (choix structurant non tranché par l'analyse
   technique), pose une question (round-trip) plutôt que de deviner en silence.

## Mode agent (orchestré)

Quand ce skill est exécuté par un **subagent**, il **ne pose pas** les questions —
il **ne peut pas** appeler `AskUserQuestion`. Il **renvoie** la question au thread
principal (round-trip), qui la pose et lui transmet la réponse. L'implémentation,
elle, est autonome.

Chaque invocation renvoie **uniquement** un objet JSON.

**Cas question** (scaffolding ou ambiguïté technique) :

```json
{
  "type": "question",
  "question": {
    "question": "Question complète, finissant par ?",
    "header": "≤12 car",
    "multiSelect": false,
    "options": [
      { "label": "Choix 1 (Recommandé)", "description": "implication / tradeoff" },
      { "label": "Choix 2", "description": "..." }
    ]
  }
}
```

**Cas résultat** (après implémentation d'un scénario) :

```json
{
  "type": "result",
  "scenario": 3,
  "titre": "Réservation d'un créneau libre",
  "test_files": ["tests/.../ReservationTests.cs"],
  "impl_files": ["src/.../ReservationService.cs"],
  "red": "dotnet test --filter … → 1 failed (attendu)",
  "green": "dotnet test → N passed, 0 failed",
  "scenarios_file": "docs/init/scenarios/<sujet>.md (scénario 3 taggé @vert)",
  "commit": "<hash court> feat: scénario 3 — …",
  "next_scenario": 4,
  "notes": "<bref>"
}
```

Règles : une seule question à la fois ; défaut en 1ʳᵉ option suffixé
` (Recommandé)`. Un seul scénario implémenté par invocation. Aucun texte hors du
JSON.

## Mapping Gherkin → tests

| Gherkin | Test |
|---|---|
| `Given` | arrange (état initial via Fakes/Givens) |
| `When` | act (action unique) |
| `Then` (chaque ligne) | un `assert` observable |
| `@nominal` | chemin heureux, assertion sur le résultat attendu |
| `@limite` | borne (zéro, max, frontière) ; assertion sur le comportement à la borne |
| `@erreur` | violation d'invariant ; assertion sur le refus + message/code observable |
| temps réel `SignalR` | test d'intégration, second client observe l'état final |

## Signaux d'alarme

- **Un test d'acceptation qui passe d'emblée** → il n'observe rien → réécris-le.
- **Then non observable dans le scénario** → reviens à `make-gherkin`, ne devine pas.
- **Implémentation au-delà du scénario courant** → YAGNI, coupe ; chaque scénario
  n'ajoute que ce qu'il exige.
- **Plusieurs scénarios dans un seul run/commit** → casse le « scénario par
  scénario », perds la traçabilité.

## Erreurs fréquentes

- **Sauter le rouge** — sans échec initial, ce n'est ni du BDD ni du TDD.
- **Scaffolder en silence** — la structure des projets est un choix structurant :
  demande au 1er run.
- **Oublier la non-régression** — relance toute la suite avant de committer.
- **Tester l'implémentation au lieu du comportement** — l'acceptation porte sur le
  `Then` observable, pas sur les détails internes.
