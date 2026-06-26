# Besoins priorisés — Hub /planning en calendrier agenda (lecture)

> Source : `99-sprint02-retours.md` (section `# Retours produit (PO)`) · produit par `/4-retours` (retours-challenge).
> Réamorce `/2-make-gherkin` sur le **sujet prioritaire** ci-dessous.

> **Aucun bug confirmé.** Le sprint 2 était un `fix` de câblage IHM : le vrai défaut
> (render mode manquant) est corrigé, App.razor:14,18 portent désormais `InteractiveServer`
> et les 4 scénarios sont verts (`00-sprint02-suivi.md`). Confronté au code HEAD, **aucun**
> retour produit ne décrit un comportement vert qui casse — tous sont des évolutions /
> nouveaux besoins déjà cadrés par `docs/02-specification.md` mais **non encore incarnés**
> par l'IHM courante (`PlanningPartage.razor` en tableaux + routes `/poser-slot`,
> `/affecter-periode`, `/definir-transfert` séparées). La sortie est donc un **nouveau
> sujet `/2-make-gherkin`**, pas une réparation `/3` ciblée.

## Classification des retours

| # | Retour (résumé) | Type | Besoin sous-jacent | Zone IHM/Tech |
|---|---|---|---|---|
| 1 | « J'aime pas le thème » — un truc en accord avec le sujet (garde d'enfants) | évolution | Identité visuelle cohérente avec le domaine ; ergonomie de surface, subordonnée à l'usage (thème Bootstrap par défaut, App.razor:9-11) | IHM - général |
| 2 | Landing page + connexion email (Gmail / Apple / Microsoft) | nouveau besoin | Ouverture de l'accès : landing + auth fournisseurs OAuth (aujourd'hui rôle = simple `<select>` Parent/Invité, PlanningPartage.razor:19-22) | IHM - général |
| 3 | Dropdown du tableau « Localisation — slots de Léa » incomprise ; vision attendue = calendrier semaine + 4 semaines, navigation type agenda | évolution | Remplacer la vue tableau par un calendrier navigable façon agenda (la dropdown « Déplacer vers… » fonctionne, PlanningPartage.razor:66-73 — rejet du paradigme, pas un défaut) | IHM - /planning |
| 4 | Tableau « Responsabilité — périodes de garde » incompris ; voudrait un code couleur dans le calendrier ; manque la suppression ; page de paramétrage des parents (admin configure 2 parents, ≥1 enfant, N acteurs autres) | évolution + nouveau besoin | (a) responsabilité par code couleur dans le calendrier ; (b) suppression des périodes ; (c) écran de config du foyer + 3 rôles (édition de période câblée, PlanningPartage.razor:155 — pas une régression) | IHM - /planning |
| 5 | Tableau « Transferts de bascule » à mettre dans un dialog « événements à venir » avec cloche de notifications | évolution | Transferts/changements à venir présentés comme événements dans un panneau cloche, pas un tableau permanent (le tableau s'affiche, PlanningPartage.razor:166-184) | IHM - /planning |
| 6 | /planning/poser-slot devrait être un composant dans une dialog de /planning | évolution | Poser un slot en contexte via dialog depuis le calendrier (route /poser-slot verte, scénario 1 — changement d'enveloppe IHM) | IHM - /planning/poser-slot |
| 7 | /planning/affecter-periode devrait faire partie d'un workflow de config et d'info sur les acteurs | question ouverte | Articuler affectation de période et écran de config des acteurs (route verte, scénario 2 ; la spec règle 5 sépare config vs affectation, le PO tend à les fusionner) | IHM - /planning/affecter-periode |
| 8 | Les transferts devraient être ponctuels (urgence, changement) et calculés automatiquement dans la majorité des cas ; sinon composant d'une dialog de /planning | évolution | Transfert dérivé automatiquement par défaut, saisie réservée au ponctuel/exception, via dialog en contexte (route verte, scénario 3 ; aligné règles 12-13) | IHM - /planning/definir-transfert |

## Arbitrage

- **Objectif de l'itération** — Faire incarner par l'IHM le modèle mental « agenda » déjà promis par la spec v02 : remplacer les tableaux que le PO ne comprend pas par un calendrier navigable. Aucun bug à réparer ; tous les retours sont des évolutions / nouveaux besoins déjà cadrés par `docs/02-specification.md` mais non encore rendus par le code HEAD.
- **Arbitre (départage)** — **L'usage réel tranche** (spec v02 §Objectif & arbitrage) : données / câblage qui débloquent l'usage > ergonomie de surface > ouverture de l'accès. **Corollaire de découpe acté avec le PO** : si le périmètre d'un sujet déborde, on coupe au plus petit incrément qui rend la grille lisible — on ne reporte jamais tout, on séquence. Application directe : le calendrier-lecture (rang 1) prime sur les enrichissements d'écriture/cloche (rangs suivants) ; l'auth (risque mortel) reste en fin de séquence par l'arbitre.

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 1 | Grille agenda en **lecture pure** : semaine en cours + 4 semaines, slots positionnés dans les cases + responsabilité par code couleur (remplace les tableaux « Localisation » et « Responsabilité ») | évolution | `calendrier-grille-lecture` | — |
| 2 | Navigation dans le mois : avancer / reculer les semaines comme un agenda | évolution | `calendrier-navigation` | rang 1 |
| 3 | Dialogs d'écriture en contexte déclenchées depuis une case (poser-slot / affecter-periode), remplaçant les routes dédiées | évolution | `calendrier-dialogs-ecriture` | rang 2 |
| 4 | Cloche / panneau d'événements à venir : transferts & changements à venir, plus de tableau permanent (phase 2 spec) | évolution | `cloche-evenements-a-venir` | rang 1 |
| 5 | Transferts dérivés automatiquement par défaut + saisie ponctuelle à l'exception (règles 12-13, phase 5 spec) | évolution | `transferts-derives-auto` | rang 4 |
| 6 | Modèle d'acteurs & foyer : écran de config (≥1 enfant, 2 parents dont un saisi par l'autre, N autres, 3 rôles Admin/Parent/Autre) ; prérequis de l'ouverture | nouveau besoin | `modele-acteurs-config-foyer` | rang 3 |
| 7 | Ouverture de l'accès : landing + auth Gmail / Apple / Microsoft (phase 6 spec, traite le risque mortel d'adoption) | nouveau besoin | `landing-auth-oauth` | rang 6 |
| — | **Transverse (pas un sujet dédié)** : thème en accord avec le domaine — ergonomie de surface, absorbée au fil des incréments calendrier | évolution | — | — |

## Prochain sujet → make-gherkin

- **Sujet** : `calendrier-grille-lecture` — Hub /planning en grille agenda, lecture seule (semaine + 4 semaines)
- **Périmètre** :
  - Afficher `/planning` sous forme de grille calendaire : **semaine en cours + 4 semaines suivantes** (5 semaines visibles, statiques).
  - **Positionner chaque slot** existant dans sa case jour/horaire de la grille (à la place du tableau « Localisation »).
  - **Lire la responsabilité** de garde par un **code couleur** dans la grille (à la place du tableau « Responsabilité — périodes de garde »).
  - **Lecture seule** : la grille consomme les slots/périodes déjà enregistrés (`ISlotRepository`, `IPeriodeRepository`) ; **aucune écriture nouvelle**.
  - Conserver l'accès aux actions existantes : les routes `/poser-slot`, `/affecter-periode`, `/definir-transfert` restent atteignables le temps de la migration ; la grille **ne les supprime pas encore**.
- **Hors périmètre (reporté)** :
  - Navigation dans le mois (avancer/reculer) → rang 2 (`calendrier-navigation`).
  - Dialogs d'écriture en contexte depuis une case → rang 3 (`calendrier-dialogs-ecriture`) ; poser-slot/affecter-periode restent des routes.
  - Cloche / panneau d'événements à venir et refonte des transferts → rangs 4-5.
  - Transferts dérivés automatiquement → rang 5.
  - Écran de config du foyer, 3 rôles, multi-enfants, auth/landing → rangs 6-7.
  - Thème / identité visuelle → transverse, pas dans ce sujet.
  - **Suppression des périodes** (demandée par le PO, retour 4) → reste une action d'écriture, rattachée aux dialogs (rang 3+), hors lecture seule.

## Risques & questions encore ouvertes

- **Bloc indivisible (signalé par la spec)** — la grille touche slots ET responsabilité couleur d'un coup. Mitigation = lecture pure, zéro écriture, navigation exclue : borne stricte du sujet rang 1.
- **Faux sentiment de progrès** — la grille reste cosmétique tant que l'écriture vit dans les anciennes routes ; tenir la séquence pour que les rangs 2-3 suivent vite, sinon le PO a une « belle grille » qu'il ne peut pas piloter.
- **Vert qui ment** (cf. note IA Cause C du fichier de retours, hors scope ici mais à porter côté `/3`) — un test bUnit de composant avec fakes peut afficher la grille alors que le câblage réel échoue. Le rouge d'acceptation devrait vérifier que des slots/périodes **réellement enregistrés** apparaissent positionnés/colorés, pas une grille vide statique.
- **Définition du code couleur ambiguë** — « responsabilité par couleur » suppose un mapping responsable → couleur stable. À fixer dans le make-gherkin (couleur par responsable du référentiel `Foyer.Responsables`), sinon scénario sous-spécifié.
- **Question ouverte (retour 7)** — faut-il fondre l'affectation de période dans un workflow de configuration des acteurs ? La spec (règle 5) sépare config vs affectation ; à reposer au rang 3 / rang 6.
- **Risque mortel d'adoption (auth) repoussé au rang 7** — conforme à l'arbitre, mais déjà signalé par la spec : ne pas le laisser glisser indéfiniment.
- **Bypass Tech assumé** — la sous-section `## Tech (optionnel)` ne contenait que le placeholder ; aucune contrainte technique injectée pour cette revue.
