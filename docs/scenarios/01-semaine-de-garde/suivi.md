# Suivi TDD — Semaine de garde

> Source : `docs/scenarios/01-semaine-de-garde.md` · produit par tdd-analyse, suivi par tdd-auto.
> Détail par scénario dans les fichiers `NN-slug.md` de ce répertoire.

> **Cadrage scaffolding** — Solution `PlanningDeGarde.sln` : projets `PlanningDeGarde.Domain`,
> `PlanningDeGarde.Application`, `PlanningDeGarde.Infrastructure`, `PlanningDeGarde.Web` (Blazor),
> tests `PlanningDeGarde.Tests` (xUnit). Refus via type `Result<T>` fermé (les `@erreur` assertent
> le verdict + l'absence d'effet de bord). Domaine sans framework, ports en Application.
> SignalR/persistance en Infrastructure ; droits Parent/Invité gardés à l'entrée de l'Application.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|---|---|---|---|---|
| 1 | [Un Parent pose un slot de localisation](01-poser-slot-localisation.md) | `@nominal` | ✅ GREEN | 3/3 | ✅ GREEN |
| 2 | [Slot de durée nulle refusé](02-slot-duree-nulle.md) | `@erreur` | ✅ GREEN | 2/2 | ✅ GREEN |
| 3 | [Slot de nuit franchissant minuit](03-slot-franchissant-minuit.md) | `@limite` | ✅ GREEN | 1/1 | ✅ GREEN |
| 4 | [Lieu inexistant](04-lieu-inexistant.md) | `@erreur` | ✅ GREEN | 3/3 | ✅ GREEN |
| 5 | [Chevauchement de localisation](05-chevauchement-localisation.md) | `@limite` | ✅ GREEN | 3/3 | ✅ GREEN |
| 6 | [Un Invité tente d'éditer un slot](06-invite-edition-refusee.md) | `@erreur` | ✅ GREEN | 3/3 | ✅ GREEN |
| 7 | [Affecter la responsabilité d'une période](07-affecter-periode.md) | `@nominal` | ✅ GREEN | 4/4 | ✅ GREEN |
| 8 | [Période sans responsable refusée](08-periode-sans-responsable.md) | `@erreur` | ✅ GREEN | 2/2 | ✅ GREEN |
| 9 | [Bornes de période paramétrables](09-bornes-parametrables.md) | `@limite` | ✅ GREEN | 1/1 | ✅ GREEN |
| 10 | [Édition concurrente d'une période](10-edition-concurrente.md) | `@erreur` | ⏳ Pending | 0/3 | ⏳ Pending |
| 11 | [Définir le transfert de bascule](11-definir-transfert.md) | `@nominal` | ⏳ Pending | 0/4 | ⏳ Pending |
| 12 | [Transfert incomplet refusé](12-transfert-incomplet.md) | `@erreur` | ⏳ Pending | 0/2 | ⏳ Pending |
