# Suivi TDD — Calendrier, grille de lecture (sprint 03)

> **Cadrage scaffolding** — Solution .NET 10 existante (`PlanningDeGarde.sln`).
> Projet de tests cible des scénarios backend : `tests/PlanningDeGarde.Tests`
> (xUnit) — déjà présent. **Aucun nouveau projet à créer.**
>
> **Production attendue (côté `tdd-auto`, scénarios backend)** :
> - **Nouvelle projection de lecture** `GrilleAgendaQuery` (Application) lisant les
>   ports `ISlotRepository` + `IPeriodeRepository` (déjà existants). Aucune
>   dépendance vers un handler/agrégat d'écriture (invariant « lecture seule »
>   garanti à la compilation, **pas** par un test — cf. risque early-green tranché).
> - **Read models** (records Application) : `GrilleAgenda { JourCase[] Jours }`,
>   `JourCase { DateOnly Date, string CouleurResponsable, SlotCase[] Slots }`,
>   `SlotCase { string Libelle, TimeOnly Debut, TimeOnly Fin, string CouleurActeur }`
>   (noms indicatifs, l'agent d'implémentation tranche les types exacts).
> - **Set de couleurs par défaut** acteur → couleur à ajouter dans `Foyer`
>   (Infrastructure) : Parent A = bleu, Parent B = orange, Nounou = vert, École = …,
>   + repli neutre (gris) déterministe pour tout acteur absent du set (règle 15).
>   Source de vérité avant auth ; personnalisation par utilisateur (règle 16) **hors
>   périmètre**. Lu par la projection (la projection prend le set en dépendance / le
>   reçoit ; l'agent tranche le port d'accès aux couleurs).
>
> **Fakes / builders réutilisables (déjà présents)** : `FakeSlotRepository`,
> `FakePeriodeRepository` (copy-on-read de snapshots). Les builders existants
> (`SlotBuilder`, `PeriodeBuilder`) produisent des **commandes** (pas des snapshots) ;
> pour peupler les fakes de la projection, soit passer par les handlers `Poser`/
> `Affecter`, soit enregistrer directement des agrégats `SlotDeLocalisation.Poser` /
> `PeriodeDeGarde.Affecter`. L'agent d'implémentation pourra ajouter un mother/builder
> de snapshots si utile.
>
> **Type de test dominant** : test unitaire de projection Application (xUnit, sans
> Blazor) — c'est explicitement le « Points d'attention TDD » du fichier source :
> tester `GrilleAgendaQuery` (fenêtre, intersection, ordre horaire, mapping couleur)
> **sans Blazor d'abord**. La date de référence (« aujourd'hui », mercredi 24/06/2026)
> doit être **injectable** dans la projection (paramètre `dateReference` / horloge),
> sinon les tests ne sont pas déterministes — point de scaffolding à trancher par
> l'implémentation (défaut recommandé : paramètre `DateOnly dateReference` sur la
> méthode de projection).
>
> **Routage backend vs IHM** — les **8 scénarios sont backend** : chacun pilote une
> règle de la projection `GrilleAgendaQuery` (structure de fenêtre, positionnement,
> couleur, exclusion), observable à la frontière de l'Application **sans** rendu. Le
> **rendu** de la grille (refonte `PlanningPartage.razor` en grille 5×7, slots
> empilés, application visuelle des couleurs, `@rendermode`) n'est l'objet d'**aucun
> scénario Gherkin** de ce fichier : il relève de `ihm-builder` après le **gate
> visuel**, piloté par un test de niveau runtime. **Aucun composant Blazor ni câblage
> SignalR n'est produit par les scénarios backend** ; les `NN-slug.md` ci-dessous ne
> couvrent que la projection / les read models / le set de couleurs.
>
> **Anti early-green (imposé par le fichier source)** : chaque scénario d'exclusion
> (Sc.6 frontière, Sc.7 hors fenêtre) **couple absence et présence** dans la **même**
> grille — une grille vide ou non implémentée échoue. Aucun test ne repose sur une
> seule assertion d'absence.

| # | Scénario | Tag | Acceptation | Tests | Statut |
|---|----------|-----|-------------|-------|--------|
| 1 | [La grille structure 5 semaines à partir de la semaine en cours](01-grille-structure-5-semaines.md) | `@nominal` | ✅ GREEN | 3/3 | ✅ GREEN |
| 2 | [Un slot enregistré apparaît dans la case de son jour avec son horaire](02-slot-dans-case-du-jour.md) | `@nominal` | ⏳ Pending | 0/3 | ⏳ Pending |
| 3 | [La case-jour prend la couleur du parent responsable de la période](03-couleur-responsable-case-jour.md) | `@nominal` | ⏳ Pending | 0/3 | ⏳ Pending |
| 4 | [Le slot d'un acteur non-responsable porte sa propre couleur](04-couleur-acteur-sur-creneau.md) | `@nominal` | ⏳ Pending | 0/2 | ⏳ Pending |
| 5 | [Plusieurs slots d'un même jour sont empilés dans l'ordre horaire](05-slots-empiles-ordre-horaire.md) | `@limite` | ⏳ Pending | 0/2 | ⏳ Pending |
| 6 | [Une période à cheval sur la borne de fin n'est colorée que sur ses jours internes](06-periode-a-cheval-borne.md) | `@limite` | ⏳ Pending | 0/2 | ⏳ Pending |
| 7 | [Un slot hors fenêtre est exclu tandis qu'un slot interne est rendu](07-slot-hors-fenetre-exclu.md) | `@erreur` | ⏳ Pending | 0/2 | ⏳ Pending |
| 8 | [Un acteur absent du set reçoit le repli gris](08-repli-gris-acteur-hors-set.md) | `@erreur` | ⏳ Pending | 0/2 | ⏳ Pending |

## Doublons / early green anticipés

- **Aucun test existant** ne couvre `GrilleAgendaQuery` (projection inédite) → la
  plupart des tests sont de vrais drivers. Vérifier toutefois les early-greens
  internes ci-dessous.
- **Sc.1 → Sc.6 (fenêtre)** : la structure de fenêtre (35 jours, lundi de la semaine
  en cours → +4 semaines) est posée par le **driver Sc.1 #1/#2**. Le Sc.6 réutilise
  cette fenêtre ; sa **borne de fin** (« aucune case au-delà du 26/07 ») est
  **probablement couverte** par le calcul de fenêtre du Sc.1 → caractérisation, pas
  driver (voir `06-…md`). Le vrai driver du Sc.6 est l'**intersection partielle** de
  la période (coloration seulement sur jours internes).
- **Sc.2 → Sc.7 (intersection slot)** : le filtrage des slots dans la fenêtre est
  posé par le Sc.2 (slot positionné dans la case de son jour, absent des autres). Le
  Sc.7 « hors fenêtre exclu » peut être **early green** si l'implémentation naturelle
  du Sc.2 filtre déjà par appartenance à la fenêtre — d'où le **couplage présence +
  absence** imposé pour qu'un défaut de filtrage soit visible (driver = la grille
  rend l'interne ET n'a aucune case pour la date hors fenêtre).
- **Sc.3 → Sc.4 / Sc.8 (couleur)** : la coloration acteur d'un slot (Sc.4) et le
  repli gris (Sc.8) sont des branches du **même mapping** acteur → couleur introduit
  au Sc.3/Sc.4. Le repli neutre (Sc.8) ne contredit le mapping nominal que si le set
  ne couvre pas l'acteur — driver réel (distinct du nominal vert).
