# Calendrier — grille de lecture — Analyse & scénarios

## Analyse technique

- **Composants impactés**
  - Blazor — `PlanningPartage.razor` (page `/planning`) refondue en grille
    5 semaines × 7 jours, lecture seule ; un composant `CaseJour` rend les slots
    empilés par horaire. Les routes `/poser-slot`, `/affecter-periode`,
    `/definir-transfert` restent atteignables (non supprimées à cet incrément).
  - Application — nouvelle projection de lecture `GrilleAgendaQuery` : construit un
    ViewModel `GrilleAgenda` (fenêtre 5 semaines = 35 jours datés, à partir de la
    semaine en cours) depuis `ISlotRepository` + `IPeriodeRepository` ; aucun
    handler d'écriture sollicité.
  - Infrastructure — `Foyer` enrichi d'un set de couleurs par défaut (table
    acteur → couleur) couvrant tous les acteurs (parents responsables + acteurs
    non-responsables : nounou, grands-parents, école…). Mapping acteur → couleur
    stable et déterministe, lu par la projection.

- **Couches & dépendances** — la grille (Infrastructure–Blazor) dépend vers
  l'intérieur de la projection `GrilleAgendaQuery` (Application), qui lit les ports
  `ISlotRepository`/`IPeriodeRepository` ; le domaine reste sans `using` de
  framework. Litmus : la projection est testable sans Blazor ; les repositories
  sont remplaçables sans toucher au calcul de fenêtre/couleur.

- **Contrats de données**
  - Entrée : date de référence (aujourd'hui) → fenêtre [lundi de la semaine en
    cours .. dimanche +4 semaines], soit 35 jours.
  - Sortie : `GrilleAgenda` = 35 `JourCase { date, couleurResponsableDuJour
    (couleur du parent responsable de la période couvrant ce jour, ou neutre si
    aucune), slots[] ordonnés par heure de début }`.
  - `SlotCase { libelléActeur/lieu, horaireDebut, horaireFin, couleurActeur }` ;
    `PeriodeResponsable` mappé sur les jours qu'elle couvre dans la fenêtre.
  - Set de couleurs par défaut : acteur → couleur (Parent A = bleu,
    Parent B = orange, Nounou = vert, École = …), source de vérité avant auth
    (règle 15) ; couleur de repli neutre (gris) si acteur absent du set (défense).

- **Write vs read (CQRS)** — la grille est un **besoin de lecture** servi par une
  projection dédiée (`GrilleAgendaQuery`), jamais par un getter de vue sur les
  agrégats `SlotDeLocalisation` / `PeriodeDeGarde`. Aucune modification : pas
  d'agrégat sollicité côté écriture à cet incrément.

- **Invariants**
  - Lecture fidèle et seule (règle 12) : la projection n'a aucune dépendance vers
    un handler/agrégat d'écriture (garanti par construction de la couche, pas par
    un test d'invariance) ; tout slot/période enregistré intersectant la fenêtre
    est rendu, aucun n'est masqué.
  - Fenêtre stricte : exactement 35 jours rendus (5 lignes-semaines) ; un
    slot/période n'apparaît que dans les jours internes qu'il intersecte (frontière
    partielle), jamais hors fenêtre.
  - Deux niveaux de couleur (règles 14-15) : la case-jour porte la couleur du
    parent responsable de la période ; chaque slot porte, sur son créneau horaire
    dans la case, la couleur propre de son acteur ; couleur distincte par personne
    (Parent A ≠ Parent B), issue du set par défaut, repli neutre déterministe sinon.

- **Points d'attention TDD**
  - Doubler uniquement les ports `ISlotRepository`/`IPeriodeRepository` (fakes en
    mémoire) ; tester `GrilleAgendaQuery` sans Blazor d'abord (fenêtre,
    intersection, ordre horaire, mapping couleur), puis le rendu en bUnit.
  - Anti early-green imposé : chaque scénario d'exclusion (hors fenêtre, frontière)
    couple l'assertion d'absence à une assertion de présence d'un élément interne
    dans la **même** grille — une grille vide ou non implémentée échoue ; aucun
    test ne repose sur une seule assertion d'absence.
  - L'invariant « lecture seule » n'est pas piloté par un test d'exécution
    (early-green inévitable sans code d'écriture) mais garanti à la compilation par
    l'absence de dépendance de la projection vers les handlers d'écriture — à
    vérifier en revue d'architecture, pas en scénario.

## Scénarios

Feature: Grille agenda du hub `/planning` en lecture seule — semaine en cours
+ 4 semaines, chaque slot positionné dans la case de son jour et la responsabilité
de garde lue d'un coup d'œil par un code couleur propre à chaque personne. La
grille consomme les slots et périodes déjà enregistrés sans jamais écrire.

### Scenario 1 — La grille structure 5 semaines à partir de la semaine en cours `@nominal` `@vert` <!-- vert — 21369c2 -->

```gherkin
Scenario: La grille structure 5 semaines à partir de la semaine en cours
  Given On est le mercredi 24/06/2026 et le foyer n'a aucun slot ni période enregistrés
  When Un Parent ouvre le hub /planning
  Then La grille agenda affiche exactement 35 cases-jour, la première datée du lundi 22/06/2026 et la dernière du dimanche 26/07/2026, organisées en 5 lignes-semaines de 7 jours
```

### Scenario 2 — Un slot enregistré apparaît dans la case de son jour avec son horaire `@nominal` `@vert` <!-- vert — a6e00bf -->

```gherkin
Scenario: Un slot enregistré apparaît dans la case de son jour avec son horaire
  Given On est le 24/06/2026 et un slot 'école' pour Léa est enregistré le mardi 23/06/2026 de 08h00 à 17h00
  When Un Parent ouvre le hub /planning
  Then La case du mardi 23/06/2026 contient le slot libellé 'école 08h00–17h00', et aucune autre case de la grille ne le contient
```

### Scenario 3 — La case-jour prend la couleur du parent responsable de la période `@nominal`

```gherkin
Scenario: La case-jour prend la couleur du parent responsable de la période
  Given On est le 24/06/2026, le set de couleurs par défaut associe Parent A au bleu et Parent B à l'orange, et une période confie Léa à Parent A du lundi 22/06 au dimanche 28/06/2026
  When Un Parent ouvre le hub /planning
  Then Les cases du lundi 22/06 au dimanche 28/06/2026 portent la couleur de Parent A (bleu), distincte de la couleur de Parent B (orange)
```

### Scenario 4 — Le slot d'un acteur non-responsable porte sa propre couleur sur son créneau `@nominal`

```gherkin
Scenario: Le slot d'un acteur non-responsable porte sa propre couleur sur son créneau
  Given On est le 24/06/2026, le set par défaut associe Parent A au bleu et Nounou au vert, une période confie Léa à Parent A le jeudi 25/06/2026 et un slot 'nounou' est enregistré ce même jour de 17h00 à 19h00
  When Un Parent ouvre le hub /planning
  Then La case du jeudi 25/06/2026 porte la couleur de Parent A (bleu) au niveau de la journée, et le créneau 'nounou 17h00–19h00' à l'intérieur de la case porte la couleur de Nounou (vert)
```

### Scenario 5 — Plusieurs slots d'un même jour sont empilés dans l'ordre horaire `@limite`

```gherkin
Scenario: Plusieurs slots d'un même jour sont empilés dans l'ordre horaire
  Given On est le 24/06/2026 et trois slots de Léa sont enregistrés le vendredi 26/06/2026 : 'domicile A 07h00–08h30', 'école 08h30–16h30', 'nounou 16h30–18h30'
  When Un Parent ouvre le hub /planning
  Then La case du vendredi 26/06/2026 liste les trois slots dans l'ordre 'domicile A 07h00–08h30' puis 'école 08h30–16h30' puis 'nounou 16h30–18h30'
```

### Scenario 6 — Une période à cheval sur la borne de fin n'est colorée que sur ses jours internes `@limite`

```gherkin
Scenario: Une période à cheval sur la borne de fin n'est colorée que sur ses jours internes
  Given On est le 24/06/2026, le set par défaut associe Parent B à l'orange et Parent A au bleu, une période confie Léa à Parent B du lundi 20/07/2026 au dimanche 02/08/2026 et une autre confie Léa à Parent A le lundi 22/06/2026
  When Un Parent ouvre le hub /planning
  Then La case du lundi 20/07 et celle du dimanche 26/07/2026 portent la couleur de Parent B (orange), la case du lundi 22/06/2026 porte la couleur de Parent A (bleu), et la dernière case de la grille reste le dimanche 26/07/2026 (aucune case au-delà)
```

### Scenario 7 — Un slot hors fenêtre est exclu tandis qu'un slot interne du même jour-semaine est rendu `@erreur`

```gherkin
Scenario: Un slot hors fenêtre est exclu tandis qu'un slot interne du même jour-semaine est rendu
  Given On est le 24/06/2026 et deux slots 'école' de Léa sont enregistrés de 08h00 à 17h00 : l'un le mardi 23/06/2026 (dans la fenêtre), l'autre le lundi 03/08/2026 (hors fenêtre)
  When Un Parent ouvre le hub /planning
  Then Le slot 'école 08h00–17h00' apparaît dans la case du mardi 23/06/2026, aucune case n'est rendue pour le 03/08/2026, et le slot du 03/08 n'apparaît dans aucune case de la grille
```

### Scenario 8 — Un acteur absent du set reçoit le repli gris quand un acteur du set garde sa couleur `@erreur`

```gherkin
Scenario: Un acteur absent du set reçoit le repli gris quand un acteur du set garde sa couleur
  Given On est le 24/06/2026, le set de couleurs par défaut associe Nounou au vert mais ne couvre pas 'Grand-mère', et deux slots pour Léa sont enregistrés le mardi 23/06/2026 : 'nounou 09h00–12h00' et 'Grand-mère 14h00–18h00'
  When Un Parent ouvre le hub /planning
  Then Dans la case du mardi 23/06/2026, le créneau 'nounou 09h00–12h00' porte la couleur verte de Nounou et le créneau 'Grand-mère 14h00–18h00' porte une couleur de repli neutre (gris) distincte du vert, les deux restant lisibles
```

## Risques

- **Vert qui ment** — un test bUnit avec doublures peut afficher une grille
  plausible alors que le câblage réel des repositories échoue ; l'acceptation doit
  alimenter des slots/périodes réellement enregistrés et vérifier leur
  positionnement et leur couleur, pas une grille vide statique.
- **Early-green tranché** — l'ancien scénario d'invariance « ouvrir ne modifie
  rien » a été supprimé (passe trivialement sans code d'écriture, ne pilote rien) ;
  les scénarios d'exclusion « hors fenêtre » et « frontière » ont été reformulés
  pour coupler absence et présence dans la même grille, de sorte qu'une grille vide
  échoue ; le slot franchissant minuit hors fenêtre a été absorbé dans le scénario
  d'exclusion couplé.
- **Faux sentiment de progrès** — la grille reste cosmétique tant que l'écriture
  vit dans les routes `/poser-slot` etc. (incrément 3) ; tenir la séquence pour ne
  pas livrer une belle grille non pilotable.
- **Set de couleurs à introduire** — `Foyer.cs` ne porte aujourd'hui que des listes
  de noms (`Lieux`, `Responsables`) ; le set par défaut acteur → couleur (règle 15)
  est à créer, en couvrant aussi les acteurs non-responsables (nounou,
  grands-parents, école comme lieu ET acteur).
- **Personnalisation des couleurs par utilisateur (règle 16)** — explicitement hors
  périmètre, dépend de l'auth (incrément 7) ; ce sujet ne livre que le set par
  défaut.
