# Sprint 15 — Calendrier navigable (palier 9)

> Plan Gherkin monolithique. Sujet `/2-make-gherkin` = `calendrier-navigable` (épics É4 + É7), spec `docs/15-specification.md`.
> Décision CP (option 3) : **navigation complète + 1 scénario plage-preuve**. Variantes plage (plage vide,
> chevauchement, à cheval sur vue/mois, drag riche) **reportées tranche 2** (à consigner au backlog en `/4-retours`).
>
> **Révision PO (hors process, post-conteneurisation).** Le sprint **absorbe le palier 14** : la **borne
> « zéro persistance neuve » est LEVÉE**. Deux blocs s'ajoutent à la navigation :
> 1. **Persistance Mongo de TOUT le domaine** (slots / périodes / transferts / cycle de fond), derrière les
>    ports existants — pas seulement la config foyer (livrée s09).
> 2. **Démarrage sans seed en runtime** : sur store Mongo vierge, l'app ouvre **totalement vide** (ni acteurs,
>    ni items) ; dès qu'on saisit, c'est **durable** et rechargé aux lancements suivants.
>
> **Asymétrie seed (clé).** *Runtime/Mongo* = aucun seed, jamais (vide → durable). *Tests/InMemory* = on
> **garde** le seed de base (acteurs/données). La suite de non-régression reste verte en InMemory ; la
> durabilité se prouve sur **Mongo réel** (Docker, façon s09). L'état de **navigation** (ancre + vue) reste,
> lui, en session/mémoire front et ne persiste pas.

**Feature: Calendrier navigable du hub /planning.** Faire de `/planning` un agenda navigable :
se déplacer dans le passé et le futur (semaine précédente / suivante), choisir une vue prédéfinie
(Semaine, **4 semaines glissantes par défaut**, Mois), revenir d'un geste à la semaine en cours, et
— amorce bornée — sélectionner une plage de cases contiguës pour affecter une période sur l'intervalle.
La grille reste en **lecture seule** : la navigation ne fait que **re-projeter** la fenêtre (les cases se
re-résolvent à la date naviguée — fond par parité ISO, surcharges, slots), et l'affectation par plage
réutilise le canal d'écriture déjà livré. **Le store du domaine devient durable (Mongo)** ; la navigation,
elle, ne persiste rien.

## Analyse technique

Légère, à titre d'orientation — la frontière s'arrête à l'Application (read model) et au canal d'écriture existant.

- **Read / CQRS (cœur du sprint).** `GrilleAgendaQuery.Projeter(dateReference)` fige aujourd'hui une fenêtre
  de **35 j / 5 semaines** à partir du lundi de la semaine de référence. On l'ouvre à un couple **(ancre, vue → span)** :
  - **Semaine** = 7 j / 1 ligne ;
  - **4 semaines glissantes** = 28 j / 4 lignes — **nouveau défaut** (aligne le 5 → 4 semaines) ;
  - **Mois** = les **semaines ISO entières recouvrant le mois calendaire** courant.
  Le **responsable de fond se re-résout à la date naviguée** (`index = semaine ISO modulo N`) : c'est l'observable
  qui distingue une vraie navigation d'un simple décalage d'étiquettes.
- **Endpoint (adaptateur de gauche, lecture seule).** `GET /api/grille/{annee}/{mois}/{jour}` (`CanalLecture`)
  étendu d'un paramètre de **vue/span** ; ne déclenche jamais la diffusion.
- **Front (`PlanningPartage`).** État de navigation en **session/mémoire** (ancre courante + vue) ; contrôles
  préc. / suiv. / **retour semaine courante** + sélecteur de vue. La grille reste en lecture seule.
- **Write / plage (bloc B, borné).** Réutilise la **commande/handler `AffecterPeriode` existants** sur un intervalle
  `[début, fin]` — **aucun handler ni port neuf**. La sélection de 2 cases contiguës émet **une** période couvrant les
  2 jours ; réapparition par **relecture**, diffusion SignalR inchangée. Le **gating règle 9** (déclencheur d'écriture
  réservé aux Parents/Admin) est mutualisé.
- **Persistance (bloc C, NOUVEAU — palier 14 absorbé).** Le **domaine entier** persiste en **Mongo** : 4
  adaptateurs de droite neufs dans `PlanningDeGarde.AdapterDroite.Mongo` (slots, périodes, transferts, cycle de
  fond) implémentant les **ports existants** (impls InMemory déjà présentes, conservées pour les tests). La **DI**
  commute **tout** le domaine droite selon le mode de persistance — **Mongo** en runtime, **InMemory forcé** sous
  l'environnement de test —, généralisant le flag `Foyer:Persistance` aujourd'hui borné à la config foyer.
- **Démarrage sans seed (runtime).** Retrait de l'amorçage de démo (`AmorcerDonneesDemo`) **et** du seed-once des
  acteurs côté `ConfigurationFoyerMongo` → premier lancement Mongo **vide** (ni acteurs, ni items). Les défauts
  **InMemory** sont conservés (tests). L'état de **navigation** (ancre/vue) reste en session/mémoire front et ne
  survit pas au redémarrage.

**Ancrage concret commun aux scénarios.** Cycle de fond **N = 2** : index 0 → **Alice** (vert), index 1 → **Bruno** (bleu).
Date du jour des scénarios = **mercredi 10/06/2026**, donc **semaine en cours = lundi 08/06/2026** (ISO **24**, paire →
index 0 → **Alice**). Semaines adjacentes : lundi 15/06/2026 (ISO 25, impaire → **Bruno**), lundi 01/06/2026 (ISO 23,
impaire → **Bruno**). « Mois » de juin 2026 = **lundi 01/06 → dimanche 05/07/2026** (5 semaines entières recouvrant le mois).

## Scénarios

### Scenario 1 — Naviguer d'une semaine vers le futur ou le passé

`@nominal`

```gherkin
Scenario Outline: Se déplacer d'une semaine décale la fenêtre et re-résout le fond
  Given un foyer dont le cycle de fond compte 2 semaines, mappé "index 0 → Alice (vert)" et "index 1 → Bruno (bleu)"
  And la date du jour est le mercredi 10/06/2026, soit la semaine en cours du lundi 08/06/2026 (semaine ISO 24)
  And le planning affiche la fenêtre par défaut "4 semaines glissantes" à partir du lundi 08/06/2026
  And le responsable de fond affiché pour la semaine en cours est "Alice"
  When je clique sur "<bouton>"
  Then la fenêtre affichée commence au lundi <premier_lundi>
  And les jours portent les dates de la semaine ISO <semaine_iso>
  And le responsable de fond affiché pour cette semaine est "<responsable_fond>"
  And aucune écriture n'est émise

  Examples:
    | bouton             | premier_lundi | semaine_iso | responsable_fond |
    | Semaine suivante   | 15/06/2026    | 25          | Bruno            |
    | Semaine précédente | 01/06/2026    | 23          | Bruno            |
```

### Scenario 2 — Basculer entre les vues prédéfinies

`@nominal` `@vert`

```gherkin
Scenario Outline: Changer de vue redimensionne la fenêtre en gardant l'ancre lundi
  Given un foyer dont le cycle de fond compte 2 semaines, mappé "index 0 → Alice (vert)" et "index 1 → Bruno (bleu)"
  And la date du jour est le mercredi 10/06/2026, soit la semaine en cours du lundi 08/06/2026
  When je sélectionne la vue "<vue>"
  Then la fenêtre affiche <nb_lignes> ligne(s) de semaine
  And elle s'étend du <premier_jour> au <dernier_jour> inclus
  And chaque case reste résolue par priorité "surcharge > fond > neutre" à sa propre date

  Examples:
    | vue                    | nb_lignes | premier_jour | dernier_jour |
    | Semaine                | 1         | 08/06/2026   | 14/06/2026   |
    | 4 semaines glissantes  | 4         | 08/06/2026   | 05/07/2026   |
    | Mois                   | 5         | 01/06/2026   | 05/07/2026   |
```

### Scenario 3 — Fenêtre par défaut à l'ouverture = 4 semaines glissantes

`@limite`

```gherkin
Scenario: À l'ouverture, le planning montre 4 semaines glissantes depuis la semaine en cours
  Given un foyer dont le cycle de fond compte 2 semaines, mappé "index 0 → Alice (vert)" et "index 1 → Bruno (bleu)"
  And la date du jour est le mercredi 10/06/2026, soit la semaine en cours du lundi 08/06/2026
  When j'ouvre le hub /planning sans avoir encore navigué
  Then la fenêtre affiche 4 lignes de semaine, soit 28 jours
  And elle s'étend du lundi 08/06/2026 au dimanche 05/07/2026 inclus
  And la dernière ligne commence au lundi 29/06/2026
  And le responsable de fond de la première semaine affichée est "Alice"
```

### Scenario 4 — Retour à la semaine en cours après navigation

`@limite`

```gherkin
Scenario: Le bouton "Aujourd'hui" ramène la fenêtre sur la semaine en cours
  Given un foyer dont le cycle de fond compte 2 semaines, mappé "index 0 → Alice (vert)" et "index 1 → Bruno (bleu)"
  And la date du jour est le mercredi 10/06/2026, soit la semaine en cours du lundi 08/06/2026
  And j'ai cliqué deux fois sur "Semaine suivante", la fenêtre commençant désormais au lundi 22/06/2026
  When je clique sur "Aujourd'hui"
  Then la fenêtre affichée recommence au lundi 08/06/2026
  And le responsable de fond affiché pour cette semaine est "Alice"
  And aucune écriture n'est émise
```

### Scenario 5 — Affecter une période sur une plage de 2 cases contiguës

`@nominal`

```gherkin
Scenario: Sélectionner deux cases contiguës affecte une période sur l'intervalle
  Given un foyer dont le cycle de fond compte 2 semaines, mappé "index 0 → Alice (vert)" et "index 1 → Bruno (bleu)"
  And la date du jour est le mercredi 10/06/2026, soit la semaine en cours du lundi 08/06/2026
  And je suis connecté en tant que Parent
  And les cases du mardi 09/06/2026 et du mercredi 10/06/2026 affichent le fond "Alice"
  When je sélectionne la plage des cases du mardi 09/06/2026 au mercredi 10/06/2026
  And j'affecte la période à "Bruno" sur cet intervalle
  Then une seule période est enregistrée, couvrant du 09/06/2026 au 10/06/2026, responsable "Bruno"
  And les cases du 09/06/2026 et du 10/06/2026 réapparaissent nommées "Bruno" et colorées en bleu
  And cette surcharge prime sur le fond "Alice" sur ces deux jours
  And aucune autre case de la fenêtre n'est modifiée
```

### Scenario 6 — API distante injoignable pendant la navigation

`@erreur`

```gherkin
Scenario: Une navigation qui échoue laisse la fenêtre courante inchangée
  Given un foyer dont le cycle de fond compte 2 semaines, mappé "index 0 → Alice (vert)" et "index 1 → Bruno (bleu)"
  And la date du jour est le mercredi 10/06/2026, soit la semaine en cours du lundi 08/06/2026
  And le planning affiche la fenêtre par défaut commençant au lundi 08/06/2026
  And l'API distante est injoignable
  When je clique sur "Semaine suivante"
  Then la fenêtre affichée reste celle commençant au lundi 08/06/2026
  And un message d'échec clair s'affiche à l'écran
  And aucune navigation n'est mise en file ni rejouée
```

### Scenario 7 — Sélection de plage indisponible en consultation seule

`@erreur`

```gherkin
Scenario: Un Invité navigue librement mais ne peut affecter aucune période par plage
  Given un foyer dont le cycle de fond compte 2 semaines, mappé "index 0 → Alice (vert)" et "index 1 → Bruno (bleu)"
  And la date du jour est le mercredi 10/06/2026, soit la semaine en cours du lundi 08/06/2026
  And je consulte le planning en lecture seule (Invité, ou acteur "Autre" incarné)
  When je clique sur "Semaine suivante"
  Then la fenêtre affichée commence au lundi 15/06/2026
  When je tente de sélectionner la plage des cases du mardi 16/06/2026 au mercredi 17/06/2026
  Then aucun déclencheur d'écriture ni dialog d'affectation ne s'ouvre
  And aucune période n'est enregistrée
```

### Scenario 8 — Premier lancement sur store Mongo vierge : application vide

`@limite`

```gherkin
Scenario: Au tout premier lancement sur une base vierge, rien n'est seedé
  Given un store Mongo vierge (aucune session précédente)
  And l'application démarre en persistance "Mongo"
  When j'ouvre le hub /planning
  Then aucun acteur n'est listé dans la configuration du foyer
  And la grille n'affiche aucun slot, aucune période ni aucun transfert
  And aucun cycle de fond n'est défini
```

### Scenario 9 — Chaque item du domaine survit au redémarrage (Mongo)

`@nominal` — acceptation runtime sur **store réel** (rempart anti vert-qui-ment).

```gherkin
Scenario Outline: Un item saisi en mode Mongo persiste après redémarrage de l'hôte d'API
  Given l'application démarre en persistance "Mongo" sur un store vierge
  And j'ai créé l'acteur "Alice" et défini un cycle de fond de 2 semaines mappé "index 0 → Alice"
  And j'ai enregistré <item>
  When l'hôte d'API redémarre
  Then <item> est toujours présent et projeté dans la grille
  And l'acteur "Alice" et le cycle de fond sont toujours présents

  Examples:
    | item                                                        |
    | un slot (enfant → lieu, date donnée)                        |
    | une période affectée à "Alice" sur un intervalle de 2 jours |
    | un transfert (dépositaire, récupérateur, lieu, date, heure) |
    | le cycle de fond de 2 semaines lui-même                     |
```

> **Note d'implémentation (BDD).** Le Scénario 9 (outline) est la **boucle externe** qui pilote la création des
> 4 adaptateurs Mongo, un type d'item par ligne. Les scénarios fonctionnels existants (slots/périodes/transferts/
> cycle, déjà verts) ne changent **pas de comportement** — ils sont seulement re-pointés sur le store durable.
> **Garde-fou de séparation** (sans scénario codant dédié) : la suite tourne en **InMemory seedé** ; seuls les
> Scénarios 8-9 exercent **Mongo réel**.
