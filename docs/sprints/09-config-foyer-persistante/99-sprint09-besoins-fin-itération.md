# Besoins priorisés — Config foyer persistante (sprint 09)

> Source : `99-sprint09-retours.md` (section `# Retours produit (PO)`) · produit par `/4-retours` (retours-challenge).
> **⚠️ Exception de cette itération — NE réamorce PAS `/2-make-gherkin`.** Décision PO
> (revue s09) : la livraison est acceptée telle quelle et le **prochain chantier est une
> refacto technique menée HORS du processus BDD/TDD piloté** (pas de scénarios, pas de
> passage make-gherkin). Le **sujet prioritaire** ci-dessous est un chantier **dette/Tech
> hors-pipeline** ; la reprise du pipeline (palier 6) est séquencée **derrière**.

## Classification des retours

| # | Retour (résumé) | Type | Besoin sous-jacent | Zone IHM/Tech |
|---|---|---|---|---|
| 1 | Livraison s09 acceptée telle quelle (9/9 verts, écran /configuration câblé, persistance Mongo durable, aucun bug produit signalé) | acceptation (pas un besoin) | Aucun — clôture nominale du palier 5 (config foyer persistante) ; rien à ordonner côté produit | — |
| 2 | Sections IHM (général / /configuration / /planning) laissées vides par le PO | absence de retour | Aucune évolution ni bug produit à séquencer cette itération | IHM (vide assumé) |
| 3 | Direction PO : « les sprints prennent trop de temps » → prochain chantier = refacto technique HORS make-gherkin/TDD piloté, avant de reprendre le pipeline | dette / Tech (contrainte injectée — bypass Tech) | Réduire le coût/la lenteur des itérations par une **restructuration du code applicatif** (dette de structure), à iso-comportement, hors pipeline | Tech |
| 4 | Sections « Idée pour la suite » / « Consigne pour la suite » vides | consigne implicite | La seule consigne de séquencement réelle = refacto AVANT reprise du pipeline ; palier 6 (récurrence) reste derrière, inchangé au backlog | — |

> **Aucun `bug`.** Pas de symptôme de défaut rapporté → aucune confrontation HEAD requise,
> aucune réparation ordonnée. Aucun item ne réamorce un `/3` ciblé.
>
> **Hors scope de ce backlog** (relèvent de `retro-sprint`, pas du produit) : la **vélocité
> du pipeline** (motivation PO réelle) consignée dans la table `# Méthode (agents)` ; les
> observations `## IA` (non-régression complète sans `--no-build`, suite runtime « TempsReel »
> flaky sous Docker et son harness de transport déterministe capitalisable).

## Arbitrage

- **Objectif de l'itération suivante** — **Restructurer le code applicatif** (dette de
  structure) pour accélérer les ajouts futurs, **à iso-comportement** : aucun observable
  métier modifié, aucun scénario produit nouveau. Refacto pure.
- **Arbitre (départage)** — aucun besoin produit en compétition (sections IHM vides,
  livraison acceptée). L'arbitre est **de sécurité, NON NÉGOCIABLE** (confirmé PO) : **la
  non-régression prime sur la vitesse de la refacto**. Toute restructuration qui ne peut
  **prouver le vert complet** (cf. critère de sortie) est refusée.

## Séquence de livraison

| Rang | Besoin | Type | Sujet | Dépend de |
|---|---|---|---|---|
| 1 | Restructuration du code applicatif (dette de structure) | dette / Tech **hors-pipeline** | `restructuration-code-applicatif` (**PAS** make-gherkin) | — |
| 2 | Récurrence des périodes / cycle de fond (flaggé IMPORTANT) | nouveau besoin | `recurrence-periodes` (reprise `/2-make-gherkin`) | rang 1 |

> Palier 6 (récurrence des périodes) reste **inchangé** au backlog (`docs/BACKLOG.md`,
> palier 6 / É7-É1) ; il redevient le prochain sujet make-gherkin **une fois la refacto
> refermée**.

## Prochain sujet → chantier hors-pipeline (PAS make-gherkin)

- **Sujet** : `restructuration-code-applicatif` — Restructuration du code applicatif (dette de structure)
- **Nature** : chantier **dette/Tech mené HORS du processus BDD/TDD piloté**. Ce backlog
  **ne réamorce PAS `/2-make-gherkin`** — décision PO explicite (revue s09).
- **Périmètre** (à iso-comportement strict) :
  - Code-behind : éliminer le `@code` inline restant (~7 composants, É3) → `.razor` + `.razor.cs` systématique.
  - Frontières hexagonales gauche/droite : rendre la séparation ports/adaptateurs lisible et homogène.
  - Séparation des projets : clarifier les frontières d'assemblage (Domaine / Application / Infrastructure / Api / Web).
- **Critère de sortie (NON NÉGOCIABLE, confirmé PO)** : la **suite COMPLÈTE reste verte —
  161/161 — avant ET après**, via `dotnet test` **sans `--no-build` ni filtre**, **Docker
  actif** (pivot Mongo Sc.3 inclus). Une restructuration qui ne prouve pas ce vert complet
  est rejetée.
- **Hors périmètre (reporté)** :
  - Tout **ajout / évolution fonctionnelle** ou nouvel observable métier (refacto pure).
  - **Persistance tirée en avant** : slots / périodes / transferts **restent InMemory**
    (borne anti-cliquet, règle 30) ; pas de persistance réelle du reste du domaine (palier 13).
  - Réécriture du **domaine** ou du **CQRS de lecture** (`GrilleAgendaQuery`) qui ne sert
    pas la dette ciblée.
  - **Récurrence des périodes** (palier 6) → rang 2, reprise make-gherkin après la refacto.

## Risques & questions encore ouvertes

- **Refacto hors gate TDD piloté → régression invisible.** Mitigé par le critère de sortie
  161/161 sans `--no-build`, Docker actif, posé comme condition de fin non négociable.
- **Cap « restructuration » potentiellement large → débordement** (effet inverse du but
  vélocité). Mitigé par la borne iso-comportement (aucun observable métier touché,
  persistance non tirée en avant).
- **Filet de test runtime « TempsReel » flaky sous Docker** (notes `## IA`) : si le vert
  complet devient non-déterministe pendant la refacto, **fiabiliser l'observabilité du vert
  AVANT de poursuivre** (point d'appui : le harness de transport déterministe du Sc.9, à
  capitaliser — sujet `retro-sprint`).
- **Vélocité du pipeline** (motivation PO) : à traiter en `retro-sprint` (nombre
  d'allers-retours, coût des gates, granularité scénarios) — **hors de ce backlog produit**.
  Vérifier après la refacto que le levier code a bien servi le but vélocité.
