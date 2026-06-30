# Suivi TDD — Semaine de garde

> Source : `docs/sprints/01-semaine-de-garde.md` · produit par tdd-analyse, suivi par tdd-auto.
> Détail par scénario dans les fichiers `NN-slug.md` de ce répertoire.

> **Cadrage scaffolding** — Solution `PlanningDeGarde.sln` : projets `PlanningDeGarde.Domain`,
> `PlanningDeGarde.Application`, `PlanningDeGarde.Infrastructure`, `PlanningDeGarde.Web` (Blazor),
> tests `PlanningDeGarde.Tests` (xUnit). Refus via type `Result<T>` fermé (les `@erreur` assertent
> le verdict + l'absence d'effet de bord). Domaine sans framework, ports en Application.
> SignalR/persistance en Infrastructure ; droits Parent/Invité gardés à l'entrée de l'Application.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|---|---|---|---|---|
| 1 | [Un Parent pose un slot de localisation](archive/01-poser-slot-localisation.md) | `@nominal` | ✅ GREEN | 3/3 | ✅ GREEN |
| 2 | [Slot de durée nulle refusé](archive/02-slot-duree-nulle.md) | `@erreur` | ✅ GREEN | 2/2 | ✅ GREEN |
| 3 | [Slot de nuit franchissant minuit](archive/03-slot-franchissant-minuit.md) | `@limite` | ✅ GREEN | 1/1 | ✅ GREEN |
| 4 | [Lieu inexistant](archive/04-lieu-inexistant.md) | `@erreur` | ✅ GREEN | 3/3 | ✅ GREEN |
| 5 | [Chevauchement de localisation](archive/05-chevauchement-localisation.md) | `@limite` | ✅ GREEN | 3/3 | ✅ GREEN |
| 6 | [Un Invité tente d'éditer un slot](archive/06-invite-edition-refusee.md) | `@erreur` | ✅ GREEN | 3/3 | ✅ GREEN |
| 7 | [Affecter la responsabilité d'une période](archive/07-affecter-periode.md) | `@nominal` | ✅ GREEN | 4/4 | ✅ GREEN |
| 8 | [Période sans responsable refusée](archive/08-periode-sans-responsable.md) | `@erreur` | ✅ GREEN | 2/2 | ✅ GREEN |
| 9 | [Bornes de période paramétrables](archive/09-bornes-parametrables.md) | `@limite` | ✅ GREEN | 1/1 | ✅ GREEN |
| 10 | [Édition concurrente d'une période](archive/10-edition-concurrente.md) | `@erreur` | ✅ GREEN | 3/3 | ✅ GREEN |
| 11 | [Définir le transfert de bascule](archive/11-definir-transfert.md) | `@nominal` | ✅ GREEN | 4/4 | ✅ GREEN |
| 12 | [Transfert incomplet refusé](archive/12-transfert-incomplet.md) | `@erreur` | ✅ GREEN | 2/2 | ✅ GREEN |

## IHM Blazor (phase finale)

> Interface donnée au comportement déjà couvert. Les composants appellent les use cases
> et rendent leur `Result<T>` — aucune règle métier dans l'UI, aucune dépendance inverse.

Le planning partagé est le **hub** : lecture (slots / périodes / transferts), avertissements,
responsable courant, et toutes les actions d'écriture accessibles (boutons + édition inline).

| Vue / composant | Use case(s) / read model(s) consommé(s) | Scénarios servis |
|---|---|---|
| `PlanningPartage.razor` (`/planning`) | `JourneeEnfantQuery` (avertissement chevauchement) + `ResponsabiliteQuery` (« Responsable maintenant ») ; `DeplacerSlotHandler` (déplacement inline + garde Invité) ; `ModifierPeriodeHandler` (édition inline d'une période + motif « recharger » sur état périmé) | 1, 3, 5, 6, 7, 9, 10, 11 |
| `PoserSlot.razor` (`/planning/poser-slot`) | `PoserSlotHandler` | 1, 2, 3, 4 |
| `AffecterPeriode.razor` (`/planning/affecter-periode`) | `AffecterPeriodeHandler` | 7, 8, 9 |
| `DefinirTransfert.razor` (`/planning/definir-transfert`) | `DefinirTransfertHandler` | 11, 12 |

**Exhaustivité (tous câblés depuis l'UI)** — handlers : `PoserSlotHandler`, `DeplacerSlotHandler`,
`AffecterPeriodeHandler`, `ModifierPeriodeHandler`, `DefinirTransfertHandler` ; read models :
`JourneeEnfantQuery`, `ResponsabiliteQuery`.

**Port temps réel réel** — `SignalRNotificateurPlanning : INotificateurPlanning` (Infrastructure),
poussant l'évènement `MiseAJour` via `PlanningHub` (mappé sur `/hubs/planning`). Remplace le
fake des scénarios. Persistance réelle : repos `InMemory*` (singletons = source de vérité du
foyer) + `Foyer*Repository` (référentiel lieux/responsables). DI : `AjouterPlanningDeGarde()`.

**Tests UI** — projet `PlanningDeGarde.Web.Tests` (bUnit) : 8 tests verts — pose réussie +
notification, lieu inexistant via Result, garde Invité (pose), transfert incomplet via Result,
édition de période sur état à jour, édition rejetée sur état périmé (motif « recharger »),
avertissement de chevauchement, responsable actuel affiché. On ne double que les ports.

**Build** : `dotnet build PlanningDeGarde.slnx` → 0 erreur · **Suite** : 51/51 verts (43 backend + 8 UI).
**Lancement** : `pwsh .claude/skills/run/scripts/run.ps1`.
