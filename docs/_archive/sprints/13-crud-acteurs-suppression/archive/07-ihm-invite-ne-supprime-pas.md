# Sc.7 — Un Invité ne peut pas supprimer d'acteur `@erreur` 🖥️ scénario IHM `@caractérisation`

← [Retour au suivi](00-sprint13-suivi.md)

> **Routé vers `ihm-builder`** — **niveau d'acceptation E2E / runtime** (app réellement câblée, DI
> réelle, contexte rôle réel). **PAS** un test backend bUnit-à-doublures. Caractérisation : gating
> règle 9 **mutualisé** sur le déclencheur de contexte rôle existant. **⚠️ probablement early green
> (câblage IHM partagé)** : une fois le gating posé sur le bouton supprimer (Sc.6), aucun rouge propre.

## Acceptation (BDD)

Test **runtime** : sur l'écran de configuration du foyer affiché pour un **Invité en consultation
seule** (foyer Parent A / Parent B / Nounou) →
- **aucun bouton supprimer** n'est proposé pour les acteurs ;
- **aucune commande de suppression** ne peut être émise ;
- la **liste des acteurs reste inchangée**.

Prouvé sur l'app réellement câblée avec un **contexte rôle Invité réel** (gating règle 9 mutualisé sur
`EstParent` / `SessionPlanning` existant), pas par bUnit forçant l'interactivité.

**✅ GREEN** — `FrontWasmConfigInviteNeSupprimePasTempsReelTests` vert (API distante réelle, store réel,
contexte rôle réel `SessionPlanning`). **DRIVER runtime réel, PAS l'early-green planifié** : l'écran de
configuration n'avait **aucun garde de rôle** (les autres actions d'écriture de config — ajout / édition /
cycle — ne sont pas gatées non plus ; le garde `EstParent` n'existait que sur la grille `PlanningPartage`),
donc le câblage Sc.6 ne suffisait pas → **vrai rouge** (cf. design note ci-dessous), exactement l'alternative
anticipée par le scénario. **RED → GREEN** : RED `Assert.Empty() Failure: Collection was not empty` (bouton
supprimer visible pour l'Invité = symptôme) → FIX `@inject SessionPlanning` + `@if (Session.EstParent)` autour
du bouton supprimer (gating mutualisé règle 9). Contrôle positif dans le même test (Parent voit les boutons)
contre le faux vert. Suite complète **194/194** (Docker actif, sans `--no-build` ni filtre). Balayage runtime :
5 tests config préexistants rendant `ConfigurationFoyer` sans contexte rôle ont reçu
`Services.AddSingleton(new SessionPlanning())` (dépendance DI désormais réelle de l'écran).

## Tests unitaires (ordonnés)

_Détail piloté par `ihm-builder`._ Aucun driver neuf : le gating réutilise le déclencheur de rôle
déjà câblé pour les autres écritures de config (ajout / édition) — il s'applique au bouton supprimer
posé au Sc.6.

## Fichiers à créer

- (le cas échéant) ajustement du gating dans `src/PlanningDeGarde.Web/Components/Pages/ConfigurationFoyer.razor` pour englober le bouton supprimer
- `tests/PlanningDeGarde.Web.Tests/FrontWasmConfigInviteNeSupprimePasTempsReelTests.cs`

## Design notes

- **Gating mutualisé règle 9** : le bouton supprimer hérite du même garde de rôle que les autres
  actions d'écriture de config. Vérifier qu'il est bien **sous** ce garde (sinon bouton visible pour
  l'Invité — vrai rouge).
- **⚠️ Cascade early-green** : ce scénario tombera vert **par construction** une fois le câblage Sc.6 +
  gating posés. `tdd-auto` / `ihm-builder` doit le **batcher** comme caractérisation, pas le traiter
  comme un early-green inattendu (round-trip CP évitable).
