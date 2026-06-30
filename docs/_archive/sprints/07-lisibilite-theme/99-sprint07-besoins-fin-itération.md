# Besoins priorisés — Lisibilité & thème (nom + légende)

> Source : `99-sprint07-retours.md` (section `# Retours produit (PO)` + sections PO
> `# Idées pour les prochain sprint` et `## Priorité pour la suite`) · produit par
> `/4-retours` (retours-challenge). Réamorce `/2-make-gherkin` sur le **sujet prioritaire**
> ci-dessous. Ne pas confondre avec le journal méthode du même fichier (`# Méthode (agents)`,
> `## IA`) qui relève de `retro-sprint`.

## Classification des retours

> Sprint 07 (lisibilité nom + légende) clos **@vert 6/6** (palier 3, lisibilité). Le seul
> retour né de l'usage de CE sprint porte sur le **survol** ; il a été **confronté au code
> courant (HEAD)** avant classification. Les autres puces PO relèvent des sections
> `# Idées pour les prochain sprint` et `## Priorité pour la suite` (intentions de
> séquencement, pas des défauts sur le livré). `## Tech` = ligne template seule → **bypass**.

| # | Retour (résumé) | Source | Type | Besoin sous-jacent | Destination |
|---|---|---|---|---|---|
| 1 | « Tout est ok sauf le survol » + « le survol d'une case (après 1s) afficherait le résumé de la journée » | `## IHM - général` + `## IHM - /planning` | **évolution** | Survol enrichi : au survol d'une case (déclenché après ~1s), afficher le **résumé de la journée** au lieu du seul nom | nouveau `/2` — **séquencé rang 3** |
| 2 | Renseigner les informations des utilisateurs | `## Priorité pour la suite` | nouveau besoin | **Écran de config foyer** : éditer les acteurs (noms + couleurs) qui alimentent nom + légende livrés au s07 ; le seed devient éditable, relu par la grille | **PROCHAIN `/2`** — rang 1 |
| 3 | Setter de la récurrence sur les périodes | `## Priorité pour la suite` | nouveau besoin | Définir une récurrence (cycle de fond) sur les périodes de garde (É7/É1) | backlog — **séquencé rang 2** |
| 4 | Indicateur que l'autre parent est connecté | `# Idées prochain sprint` | nouveau besoin | Indicateur de présence temps réel de l'autre parent (É9/É10) | backlog — non prioritaire |
| 5 | Gérer plusieurs enfants | `# Idées prochain sprint` | nouveau besoin | Déclaration de N enfants du foyer | backlog (déjà É1, Palier 4) — non prioritaire |
| 6 | Gérer des familles recomposées | `# Idées prochain sprint` | nouveau besoin | Enfants de parents différents, même planning | backlog (déjà É1, Palier 4-5) — non prioritaire |
| 7 | Thème sombre avec toggle | `# Idées prochain sprint` | évolution | Thème sombre + bascule clair/sombre, **avec persistance de la préférence** | backlog (É5, additif au thème métier s07) — non prioritaire |

> **Note `bug` (anti-règle confrontation HEAD)** — Le retour « sauf le survol » a été
> confronté au **code courant (HEAD)** : le survol = attribut `title` natif portant le nom
> complet sur le nom tronqué (`src/PlanningDeGarde.Web/Components/Pages/PlanningPartage.razor:54-55`),
> **conforme à Sc.6 et @vert** (acceptation runtime 6/6). **Aucun défaut localisé** → ce
> n'est **PAS un `bug`** mais une **évolution** (résumé de journée + délai 1s = comportement
> neuf). À ne jamais envoyer en `/3` ciblé comme une réparation : il n'y a rien de cassé.
> `## Tech` vide → **bypass** (aucune contrainte technique à injecter).

## Arbitrage

- **Objectif de l'itération** — Fermer la boucle du sprint 07 (@vert 6/6) en désignant le
  **prochain incrément d'usage**. Le PO a exprimé une priorité stratégique explicite
  (« la suite = gestion des utilisateurs ») ; on en extrait la **plus petite tranche
  cohérente**, le reste séquencé et consigné.
- **Arbitre (départage)** — Règle actée pour ce tour, par ordre :
  1. **La priorité stratégique affichée par le PO l'emporte** sur les retours frais d'usage
     de moindre portée → « gestion des utilisateurs » passe devant le **survol** (rang 3) et
     devant le **Calendrier navigable** (ex-tête de file Palier 4).
  2. **Au sein d'un paquet, la plus petite tranche cohérente d'abord** → « infos
     utilisateurs » (écran config foyer) avant « récurrence des périodes ».
  3. **Le séquencement spec tient** (v06/v07 : persistance réelle = Palier 10, derrière
     l'usage) → on ne tire **pas** la persistance en avant (YAGNI). L'écran édite en
     **volatile**, même dette assumée que le seed actuel (`IPaletteCouleurs` / port nom s07).
- **Cap persistance (G2, tranché)** — L'édition est **volatile** (en mémoire, relue par la
  grille dans la session), **aucune persistance durable construite**. La survie au
  redémarrage et la sortie du dur de `Foyer.cs` restent au **Palier 10**.

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 1 | **Écran de config foyer — édition des acteurs (noms + couleurs)**, édition **volatile**, relue par la grille (nom + légende). AUCUNE persistance durable | nouveau besoin | `config-foyer-acteurs` | palier 3 (livré ✅) |
| 2 | **Récurrence sur les périodes** (cycle de fond) | nouveau besoin | `recurrence-periodes` | rang 1 |
| 3 | **Survol → résumé de la journée** (enrichissement du survol, après ~1s) | évolution | `survol-resume-journee` | rang 1 |
| … | *(backlog non prioritaire)* présence autre parent · multi-enfants · familles recomposées · thème sombre + toggle | nouveau besoin / évolution | — | usage |
| … | *(paliers techniques en queue)* persistance réelle (10), PWA (11), Docker | nouveau besoin | — | tout l'usage |

> **Re-séquencement acté** — L'écran de config foyer (infos utilisateurs) **passe devant
> « Calendrier navigable »** (ancien Palier 4 en tête de file dans `docs/BACKLOG.md`). La
> récurrence des périodes et le survol→résumé sont placés **derrière lui**.

## Prochain sujet → make-gherkin

- **Sujet** : `config-foyer-acteurs` — Écran de configuration du foyer (édition des acteurs)
- **Périmètre** : un écran pour **éditer les acteurs du foyer** — leurs **noms** (port
  `IReferentielResponsables` livré au s07) et leurs **couleurs** (`IPaletteCouleurs`). Le
  **seed devient éditable** et la **grille (case + légende) reflète immédiatement** le
  changement. Observable Gherkin pressenti : « je renomme Alice→Alicia / re-colorie Bruno →
  la case et la légende suivent **dans la session** ». Édition **volatile** (en mémoire),
  **aucune persistance durable** construite.
- **Hors périmètre (reporté)** : la **persistance durable** (survie au redémarrage, sortie
  du dur de `Foyer.cs`) = **Palier 10**, derrière l'usage ; la **récurrence des périodes**
  (rang 2) ; le **survol → résumé de la journée** (rang 3) ; **Calendrier navigable**
  (séquencé derrière). Backlog non prioritaire : présence de l'autre parent, multi-enfants,
  familles recomposées, thème sombre + toggle.

## Risques & questions encore ouvertes

- **T6 — Périmètre « résumé de la journée » (survol) non défini** — périodes ? slots ?
  responsable ? transferts ? Sujet potentiellement plus gros qu'il n'y paraît, proche du
  « qui récupère ce soir » (É9). À **cadrer au make-gherkin** quand le survol (rang 3) sera
  pris ; ne pas le sous-estimer comme « simple tooltip ».
- **T5 — Couplage thème sombre + toggle / préférences utilisateur** — la **persistance d'une
  préférence de thème** rejoint naturellement le futur écran de config / préférences user.
  À arbitrer quand le thème sombre remontera : l'embarquer avec la gestion utilisateurs ou
  le garder isolé.
- **Survol = évolution, PAS un bug** — malgré « sauf le survol », le `title` natif livré
  (PlanningPartage.razor:54-55) est **conforme Sc.6 et vert**. Ne **jamais** l'envoyer en
  `/3` ciblé comme une réparation ; c'est un comportement neuf à scénariser.
- **Dette persistance config foyer (Palier 10) assumée** — l'édition volatile assume
  sciemment que la config foyer reste en dur (`Foyer.cs`) et non persistée, **miroir de la
  dette `IPaletteCouleurs` / port nom s07**. La survie au redémarrage reste au Palier 10,
  derrière l'usage : **ne pas la remonter** (YAGNI, arbitre = l'usage tranche).
- **Risque d'adoption du second parent (mortel)** — repoussé au palier 9 (auth) ; aucun des
  sujets ci-dessus ne le lève. À ne pas laisser glisser indéfiniment.
