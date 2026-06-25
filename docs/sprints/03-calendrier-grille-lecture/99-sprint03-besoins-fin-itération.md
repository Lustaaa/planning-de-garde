# Besoins priorisés — Fondations techniques (controllers + WASM)

> Source : `99-sprint03-retours.md` (section `# Retours produit (PO)`) · produit par `/4-retours` (retours-challenge).
> Réamorce `/2-make-gherkin` sur le **sujet prioritaire** ci-dessous.

## Classification des retours

| # | Retour (résumé) | Type | Besoin sous-jacent | Zone IHM/Tech |
|---|---|---|---|---|
| 1 | Faire un sprint tech-only de refacto d'abord, puis lecture+écriture de l'alimentation utilisateurs, puis données de garde | question ouverte (séquencement) | Trancher l'ordre des sprints : fondations tech avant de reprendre la séquence produit | Retour humain (tête de section) |
| 2 | Le thème est toujours très moche | évolution (transverse) | Thème en accord avec le domaine (garde d'enfants) | IHM - général |
| 3 | Les périodes de responsabilité des parents ne sont pas représentées / invisibles | évolution (lisibilité IHM) | Rendre la responsabilité de période lisible : libellé/nom du responsable + légende couleur, pas seulement une teinte de fond | IHM - général / /planning |
| 4 | Comment les transferts sont notifiés/affichés ? Rien ne les expose | nouveau besoin (déjà au backlog spec) | Panneau d'événements (cloche) exposant transferts et changements à venir (règle 20, incrément 4 spec) | IHM - général |
| 5 | Toutes les saisies dans les écrans n'apparaissent pas dans l'écran | bug runtime à requalifier | L'écriture doit se refléter dans la grille à l'usage réel ; reporté après le recâblage WASM (écriture via API HTTP) | /planning |
| 6 | Passer l'app en WASM (futur IHM VueJs / MCP attaquant les API) | évolution (architecture) | Découpler le front d'un back exposé en API, consommable par WASM / VueJs / MCP | Tech |
| 7 | Chaque .razor adossé à un .razor.cs (pas de @code inline) | évolution (convention/refacto) | Convention code-behind systématique | Tech |
| 8 | Un swagger sur le backend | nouveau besoin (outillage) | API explorable/documentée, prérequis utile dès que des controllers existent | Tech |
| 9 | Commandes appelées à travers des controllers | évolution (architecture) | Exposer les commandes via controllers HTTP = adaptateur de gauche | Tech |
| 10 | Pas d'adaptateur de gauche/droite visible (port-adapter / hexagonal) | évolution (architecture transverse) | Matérialiser les ports et leurs adaptateurs (gauche = controllers/IHM, droite = persistance) | Tech |
| 11 | Ne plus mettre de données dans les classes (ex. Foyer.cs) ; préférer la base | évolution (persistance) | Persister la config foyer (acteurs, lieux, set de couleurs) en base plutôt qu'en constantes statiques | Tech (recoupe incrément 6 produit) |

> **Confrontation au code courant (HEAD)** — synthèse des vérifications :
> - **WASM** (#6) : `PlanningDeGarde.Web.csproj` = `Sdk.Web` + `SignalR.Client`, render mode `InteractiveServer` (commit `5427754`). Aucun projet WASM. Évolution confirmée.
> - **Controllers** (#9) : aucun `*Controller*.cs` ; les `.razor` injectent et appellent les handlers en DI directe. Adaptateur de gauche absent — confirmé.
> - **Swagger** (#8) : aucune référence `Swagger`/`OpenApi`/`Swashbuckle` dans `src/`. Confirmé.
> - **Code-behind** (#7) : 7 composants ont du `@code` inline (`Home`, `AffecterPeriode`, `PoserSlot`, `DefinirTransfert`, `Weather`, `Error`, `Counter`) ; seul `PlanningPartage.razor.cs` suit déjà la convention. Confirmé.
> - **Données en dur** (#11) : `Foyer.cs:6-36` = classe statique portant `Enfants`/`Lieux`/`Responsables`/`CouleursParActeur` en `readonly`. Confirmé.
> - **Périodes invisibles** (#3) : `PlanningPartage.razor:44-46` applique uniquement `Teinte(jourCase.CouleurResponsable)` en background ; aucun nom de responsable ni libellé de période rendu, aucune légende. Défaut de **lisibilité** confirmé — mais ce n'est **pas un bug** d'un comportement vert : le sprint 03 ne livrait que la coloration par case (Sc.3), jamais un libellé. → évolution, pas régression.
> - **Saisies invisibles** (#5) : **bug runtime à requalifier**, non localisable comme défaut statique. Le câblage de rafraîchissement existe (`PlanningPartage.razor.cs:36-67` : `Charger()` à l'init + abonnement `PlanningHub.EvenementMiseAJour`). Le symptôme est réel à l'usage mais invisible en lecture de code → requalifié, **non ordonné en réparation à l'aveugle** (cf. Requalifications).
> - **Transferts non exposés** (#4) : aucun rendu de transfert ni panneau cloche dans les composants — **conforme** à la séquence spec (incrément 4 non livré). Pas un bug, trou de séquence assumé.

## Arbitrage

- **Objectif de l'itération** — Poser les fondations techniques de l'application **au début du projet**, avant qu'elle ne grossisse et que le coût de refacto explose : extraire un **adaptateur de gauche** (controllers HTTP exposant les commandes d'écriture) et **migrer le front en WASM** consommant cette API, en **conservant SignalR** cantonné au **push temps réel côté lecture**.
- **Arbitre (départage)** — **arbitre contextuel = phase de projet**, distinct de l'arbitre « l'usage réel tranche » de la spec. Quand un besoin tech de fondation et un besoin d'usage immédiat s'opposent **au début du projet**, la **fondation tech gagne**, parce que c'est une fenêtre d'investissement structurel : le coût de l'hexagonalisation / exposition API / migration WASM est minimal maintenant et explose une fois l'app grosse. Verbatim PO : « tech dans ce cas, car on est au début du projet ». L'arbitre « usage réel > tech » de la spec **reprend la main après ce sprint de fondations** (groupes 1 et 2 ci-dessous).
- **Invariant d'architecture (tranché PO)** — la migration WASM **conserve SignalR**, mais cantonné au **push temps réel vers l'utilisateur** (notifications, actualisation du planning en lecture). L'**écriture ne passe JAMAIS par SignalR** : elle passe par les **controllers HTTP / l'API**. Séparation structurante : **écriture = HTTP/controllers ; lecture & rafraîchissement temps réel = SignalR read-only**.

## Séquence de livraison

| Rang | Besoin | Type | Sujet make-gherkin | Dépend de |
|---|---|---|---|---|
| 1 | Controllers (adaptateur de gauche, écriture) + migration WASM consommant l'API, SignalR conservé en push lecture seule | évolution (architecture) | `controllers-wasm-fondation` | — |
| 2 (G1) | Lisibilité des périodes / responsable explicites dans la grille (libellé + légende couleur) | évolution (lisibilité IHM) | `lisibilite-periodes-responsable` | rang 1 |
| 2 (G1) | Thème en accord avec le domaine (garde d'enfants) | évolution (transverse) | `theme-domaine` | rang 1 |
| 3 (G2) | Écriture en contexte / saisies qui réapparaissent à l'écran (recâblé via API HTTP, repro runtime obligatoire) | bug runtime à requalifier + incrément 3 produit | `ecriture-en-contexte` | rang 1, G1 |
| 3 (G2) | Persistance de la config foyer (`Foyer.cs` → base) | évolution (persistance) | `persistance-config-foyer` | rang 1 |

> **Deux groupes après le sprint de fondations (pas 4 étapes linéaires)** :
> - **Groupe 1** — Lisibilité périodes/responsable **+** Thème, traités **ensemble**.
> - **Groupe 2** (ensuite) — Écriture en contexte/saisies **+** Persistance `Foyer.cs`→base, traités **ensemble**.

## Prochain sujet → make-gherkin

- **Sujet** : `controllers-wasm-fondation` — Controllers + migration WASM (SignalR conservé en push lecture seule)
- **Périmètre** :
  - Extraire un **adaptateur de gauche** = **controllers HTTP** exposant les commandes d'écriture existantes (poser slot, affecter période, définir transfert). **Observable Gherkin-codant** : le **contrat des controllers** — un POST de commande aboutit (succès) et son **effet est observable via la projection `GrilleAgendaQuery`**.
  - **Migrer le front** Blazor Server vers **WASM** consommant cette API HTTP pour l'**écriture**.
  - **Conserver SignalR** cantonné au **push temps réel côté lecture** (notifications, actualisation du planning) ; l'écriture passe par l'API HTTP, **jamais** par SignalR.
- **Hors périmètre (reporté)** :
  - **Invariants de structure SANS observable métier** (à porter comme garde-fous de compilation/config, **pas** comme scénarios Gherkin codants — risque early-green / caractérisation pure, cf. Sc.6/Sc.8 retirés au sprint 03) : hosting WASM lui-même, **code-behind systématique** (#7), **swagger** (#8), la séparation des canaux écriture-HTTP / lecture-SignalR en tant que câblage.
  - **Groupe 1** : lisibilité périodes/responsable (#3) + thème (#2).
  - **Groupe 2** : écriture en contexte / saisies (#5) + persistance `Foyer.cs`→base (#11).
  - **Transferts / panneau cloche** (#4) : incrément 4 de la spec, non rapproché.

## Requalifications (à ne pas réparer à l'aveugle)

- **« Les saisies n'apparaissent pas à l'écran » (#5) → bug runtime à requalifier, reporté Groupe 2.** Non localisable comme défaut statique dans HEAD : le câblage de rafraîchissement existe (`PlanningPartage.razor.cs:36-67`). De plus, la **migration WASM (rang 1) va démonter ce câblage** — l'écriture bascule sur **HTTP**, SignalR devient **read-only**. Réparer **avant** serait à refaire. Le besoin est donc **reconstruit** au Groupe 2 (écriture via API) et **vérifié par une repro runtime**, pas par un fix statique sur l'archi actuelle.
- **« Transferts non exposés » (#4) → conforme à la séquence spec.** Le panneau cloche est l'**incrément 4** de `docs/03-specification.md` (règle 20), non encore livré. Pas un bug : trou de séquence assumé, déjà au backlog produit.

## Risques & questions encore ouvertes

- **Bloc WASM volumineux et indivisible** — potentiellement plus gros que tous les incréments produit restants ; surveiller la dérive de périmètre au make-gherkin.
- **CORS / auth / sérialisation** — la bascule front→API HTTP introduit des contraintes (CORS, format de sérialisation des commandes, future authentification) absentes en DI directe Blazor Server.
- **Réécriture du câblage IHM** — les `.razor` passent de l'appel DI direct des handlers à des appels API HTTP ; risque de régression du flux d'écriture.
- **Séparation écriture-HTTP / lecture-SignalR à établir proprement** — garantir que l'écriture (HTTP) **déclenche bien** le push SignalR de rafraîchissement lecture, sans jamais utiliser SignalR pour l'écriture ; point de câblage à valider par **repro runtime**.
- **Sprint à faible valeur d'usage immédiate (assumé)** — aucun incrément produit n'avance pendant ce sprint ; le grief « saisies invisibles » reste non résolu jusqu'au Groupe 2. Tenir la séquence pour ne pas le laisser glisser (cf. risque « faux sentiment de progrès » de la spec).
