# Product backlog — planning-de-garde

> **Backlog produit permanent** (artefact SCRUM). Deux lectures du même produit :
> une vue **par épic (fonctionnalité)** pour regrouper ce qui est lié et préparer le
> découpage des sprints, et une vue **par palier (séquence de livraison)** pour le
> calendrier d'un coup d'œil. Source de vérité du *quoi/quand* ; le *pourquoi* vit dans
> la spec vivante [`docs/14-specification.md`](14-specification.md).
>
> **Tenue à jour par le pipeline** : `/4-retours` y **ajoute** les besoins issus du
> challenge ; `/6-cloture-sprint` y passe à **✅ fait** ce qui a été livré (gate visuel
> passé), en renseignant le sprint. Statuts : ✅ fait · 🟡 en cours · ⬜ à faire.
> Origine tracée : `spec` (règle/palier v04), `retours sNN` (retours produit d'un sprint),
> `dette` (dette explicitement signalée).

## Sprints livrés

| Sprint | Sujet | Statut | Livré |
|-------:|-------|:------:|-------|
| 01 | Semaine de garde (grille agenda, cycle récurrent, slots/périodes/transferts) | ✅ fait | Modèle de garde + 12 scénarios domaine + grille initiale |
| 02 | Réparer le câblage IHM ↔ actions (render mode interactif) | ✅ fait | Actions d'écriture câblées au front |
| 03 | Calendrier — grille de lecture (5 semaines, lecture seule, 2 niveaux de couleur) | ✅ fait | Projection `GrilleAgendaQuery` + grille 5×7 lecture seule |
| 04 | `controllers-wasm-fondation` — canal d'écriture HTTP (adaptateur de gauche) + recâblage du front via API, SignalR cantonné à la diffusion lecture seule | ✅ fait | Canal HTTP `poser-slot`/`affecter-période` + front câblé + OpenAPI document + code-behind partiel (4 scénarios, 82 verts) |
| 05 | `host-api-separable` — hôte d'API détaché (back démarrable seul) + front Blazor **WASM réel** consommant l'API distante + CORS + UI d'exploration **Scalar** + échec clair si API injoignable | ✅ fait | Projet `PlanningDeGarde.Api` détaché (test d'archi sur ProjectReference) + front `Sdk.BlazorWebAssembly` + Scalar/OpenAPI + CORS + SignalR distant (6 scénarios, 96 verts) — **palier 1 (fondation) refermé** |
| 06 | `saisie-visible` — la saisie réapparaît à la bonne **date** (défaut = aujourd'hui via `IDateTimeProvider`) **et** en **couleur du parent** (identifiant stable bindé + seed) | ✅ fait | Port `IDateTimeProvider` injecté sur PoserSlot/AffecterPeriode/DefinirTransfert + sélecteurs bindant l'id stable + seed (8 scénarios, 108 verts) — **palier 2 (saisie visible) refermé** |
| 07 | `lisibilite-theme` — **nom du responsable** + **légende** couleur dans la grille + **thème métier** (garde d'enfants) ; port nom miroir de la palette | ✅ fait | Port `IReferentielResponsables` (miroir `IPaletteCouleurs`) + composant `Legende` + troncature/survol nom long + repli gris assumé + suivi temps réel + thème CSS (6 scénarios @vert runtime, 120 verts) — **palier 3 (lisibilité & thème) refermé** |
| 08 | `config-foyer-acteurs` — écran de config pour **éditer les acteurs** (renommer + recolorier) en **VOLATILE** (mémoire/session), grille (case + légende) relue immédiatement, convergence temps réel | ✅ fait | Store mutable `ConfigurationFoyerEnMemoire` (singleton derrière `IReferentielResponsables`/`IPaletteCouleurs`/`IEditeurConfigurationFoyer`) + commande/handler `EditerActeur` + écran `ConfigurationFoyer` (4 acteurs, nom pré-rempli) + diffusion SignalR (10 scénarios @vert runtime, 143 verts) — **palier 4 (édition volatile) refermé** |
| 09 | `config-foyer-persistante` — **ajout d'acteurs** (id stable neuf opaque) + **persistance Mongo BORNÉE à la config foyer** (adaptateur de droite `ConfigurationFoyerMongo`, ports inchangés, seed-once) ; survit au redémarrage. Reste du domaine InMemory | ✅ fait | `AjouterActeurHandler` + ports `IEnumerationActeursFoyer`/`IEditeurConfigurationFoyer` + adaptateur durable `ConfigurationFoyerMongo` (Docker) + écran config (ajout + liste + pastille couleur + messages refus/transport) (9 scénarios @vert, 161 verts, pivot Mongo réel) — **palier 5 (config foyer persistante) refermé** |
| 10 | `recurrence-des-periodes` — **cycle de fond** définissable/éditable (N semaines, alternance par parité ISO 8601, mapping index→responsable sur id stable) ; la grille résout le responsable de fond (case + légende) sans saisie de période ; surcharge > fond > neutre ; **EN MÉMOIRE** (durabilité = palier 10) | ✅ fait | `CycleDeFond` (Domain, parité ISO + invariant N≥1) + port `IReferentielCycleDeFond` + `DefinirCycleHandler` + extension `GrilleAgendaQuery` + adaptateur `CycleDeFondEnMemoire` (singleton) + endpoint `POST /definir-cycle` + section « Cycle de fond » de l'écran config (mapping sur acteurs persistés) (8 scénarios @vert end-to-end, 183 verts) — **palier 6 (récurrence des périodes) refermé** |
| 11 | `ecriture-en-contexte` — **écriture en contexte par dialogs** : menu au clic sur une case du planning → dialogs « Poser un slot » / « Affecter une période » pré-remplies sur la **date de la case** ; échec → dialog reste ouverte (message dans la dialog) ; annulation sans écriture ; gating Invité sur le menu ; chevauchement accepté + **bandeau d'avertissement non bloquant** ; **écrans dédiés slot/période retirés** (un seul chemin d'écriture) | ✅ fait | `PoserSlotDialog` + `AffecterPeriodeDialog` + menu clic-case dans `PlanningPartage` + ancrage `DateContexte` (prime sur `IDateTimeProvider`) + avertissement surfacé par le contrat de réponse du canal poser-slot (`PoserSlotReponse`) + suppression des pages/routes/liens poser-slot/affecter-periode (page `definir-transfert` conservée) (7 scénarios @vert runtime, 179 verts) — **palier 7 (écriture en contexte, dialogs) refermé** |
| 12 | `transfert-en-contexte` — **3e dialog « Définir un transfert » en contexte** (menu clic-case, pré-remplie sur la date de la case ; échec → dialog reste ouverte ; annulation sans écriture ; gating Invité) **+ retrait du dernier écran de saisie dédié** (page/route/lien `definir-transfert` supprimés) | ✅ fait | `DefinirTransfertDialog` + 3e entrée menu clic-case + accusé « Transfert défini » à part + ancrage `DateContexte` + réutilise commande/handler `DefinirTransfert` + canal HTTP + SignalR (aucun handler neuf, transfert reste InMemory) + suppression page/route/lien dédiés (6 scénarios @vert runtime, 182 verts) — **palier 7 (écriture en contexte) refermé COMPLET, épic É12 fermé** |
| 13 | `crud-acteurs-suppression` — **suppression d'un acteur** (Delete) sur store Mongo réel + **neutralisation par repli** des cases orphelines (surcharge orpheline → fond → neutre, sans nom fantôme), idempotence, accusé non bloquant « Acteur supprimé » ; IHM bouton supprimer + gating Invité + échec API + temps réel SignalR | ✅ fait | `SupprimerActeurHandler` + port `IEditeurConfigurationFoyer.Supprimer` (InMemory + Mongo) + endpoint `POST /api/canal/supprimer-acteur` + filtre d'existence `Resolvable()` dans `GrilleAgendaQuery` (case + légende, réutilise `IEnumerationActeursFoyer`) + bouton/gating/échec/temps réel dans `ConfigurationFoyer` (9 scénarios @vert, 196 verts, intégration Mongo réel) — **palier 8 tranche 1 (suppression) refermé, cycle de vie acteurs C/R/U/D complet** |

> **Refacto technique HORS pipeline (PR #21, avant s10) : faite** — adaptateurs de droite par techno, `PlanningDeGarde.SignalR` (adapter de gauche), rangement par type, pipeline allégé, outil `test-count.ps1`. Critère de sortie 161/161 tenu.

## En cours

| Sprint | Sujet | Palier (spec v14) | Statut |
|-------:|-------|-------------------|:------:|
| — | *(aucun sprint en cours — prochain : impersonation bornée, cf. ci-dessous)* | 8 tranche 2 (impersonation bornée, spec v14) | ⬜ |

## Prochains sprints envisagés

> **Décision PO (clôture s13, porte G2)** : prochain sujet **make-gherkin** = **impersonation bornée** (É10+É2) : l'admin/parent configurateur **incarne un acteur déjà déclaré** (convenance d'admin), **borne dure** = PAS l'auth réelle (palier 13 : pas d'OAuth/comptes/sessions/prise en main, aucune persistance neuve tirée) ; périmètre exact (lecture seule vs écriture « au nom de », sortie/retour identité) cadré au make-gherkin. Ferme la boucle cycle de vie acteurs (C/R/U/D livrés au s13). **Candidat adjacent** : durcissement du gating config (règle 9 — angle mort Sc.7 s13 : l'écran config ne gate l'Invité que sur le bouton supprimer ; ajout/édition/cycle restent ouverts), à confirmer au make-gherkin. Retours produit s13 VIDE (goal 9/9 atteint) → pilotage au catalogue. Indicatif — confirmé/affiné au démarrage de chaque sprint.

| Rang | Sujet envisagé | Épics | Pourquoi maintenant |
|-----:|----------------|-------|---------------------|
| +1 (P1) | **Impersonation bornée** (admin incarne un acteur déjà déclaré, convenance — pas l'auth réelle du palier 13) ; candidat adjacent = durcissement gating config (règle 9) | É10, É2 | Sujet d'usage élu G2 PO clôture s13 ; dernier maillon du cycle de vie acteurs (C/R/U/D livrés s13), contexte chaud (écran config) |
| +2 (P2) | **Calendrier navigable** (passé/futur, vues prédéfinies semaine/mois/4-sem) + **sélection de plage de cases** pour définir une période | É4, É7 | Prochain palier d'usage après la tranche acteurs ; besoin ancien (retours s02/s03) |
| +3 | **Rétrofit complet du garde déterministe *TempsReel* SignalR** (helper `WaitForState` posé partiellement au s13 ; généraliser à tous les `*TempsReel*` config/grille) — **dette de test**, prérequis de l'édition concurrente | É3 | Déverrouille l'édition concurrente sans driver une fondation temps-réel instable |
| +4 | **Édition concurrente du même jour sous dialog ouverte** (last-write-wins règle 11, à démontrer sous dialog) — DIFFÉRÉE jusqu'à stabilisation SignalR | É7 | Cas limite runtime ; dépend du +3 |
| +5 | **Cycle de fond riche** : choisir le début/ancre + config fine (frontière de jour, plage début/fin, sur-cycle vacances, WE-only). Sujet plein — rouvre la décision CP « ancrage ISO sans ancre » | É7, É1 | Retour PO /configuration s10 |

---

## Épics (par fonctionnalité)

> Regroupement transverse aux paliers : chaque épic réunit les besoins liés, avec leur
> statut, leur sprint/palier de rattachement et leur origine. Les dépendances entre épics
> sont en bas (« Dépendances »). Cette vue sert à **constituer les prochains sprints** ;
> la vue paliers ci-dessous donne l'ordre de livraison.

### Épic 1 — Fondation données & modèle foyer
*Déclarer et persister les données du foyer (acteurs, lieux, cycle, couleurs) au lieu de les figer dans `Foyer.cs`.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Extraire la config foyer de `Foyer.cs` vers persistance (base) | ⬜ | Palier 4 | retours s03 (#11, dette) · spec p4 |
| Déclaration des enfants du foyer (N enfants, ≥1, organisation propre) | 🟡 | s01 socle + Palier 4 | spec règle 1 |
| Familles recomposées (enfants de parents différents, même planning ; parents en couple gérant **leurs enfants respectifs**) | ⬜ | Palier 5-6 | spec règle 2 · retours s07 (idée) |
| **Parents liés entre eux à travers leur(s) enfant(s)** (graphe foyer : un parent ↔ ses enfants ↔ l'autre parent) | ⬜ | Palier 5-6 | retours s07 (idée) · spec règles 2-3 |
| Deux parents (toujours exactement 2 ; le 1er saisit l'autre) | ⬜ | Palier 5 | retours s01 · spec règle 3 |
| Acteurs « autres » éditables (nounou, grands-parents…) | ✅ | s08-s09-s13 (CRUD complet) | spec règle 4 · retours s01 |
| Lieux éditables et persistés (référentiel des sélecteurs) | 🟡 | Palier 4 | spec règle 11 |
| Cycle récurrent multi-semaines **éditable** (EN MÉMOIRE ; persistance durable = palier 10) | 🟡 | s10 (éditable) / Palier 10 (durable) | spec règle 11 · besoins s10 |
| Set de couleurs par défaut persisté (acteur → couleur) | 🟡 | s03 statique + Palier 4 | spec règle 15 |

### Épic 2 — Modèle & configuration d'acteurs
*Déclarer les acteurs réels (Admin / Parent / Autre) avec rôles, responsabilités et accès.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Trois types d'acteurs avec rôles distincts (Admin / Parent / Autre) | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Écran de config foyer — **édition des acteurs (noms + couleurs) en VOLATILE** (mémoire/session, grille relue immédiatement, sans persistance durable) | ✅ | s08 / Palier 4 | retours s07 · spec v08 règle 5 |
| **Ajout d'acteurs (parent/autre/nounou, id stable neuf) + persistance Mongo BORNÉE à la config foyer** (survit au redémarrage) | ✅ | s09 / Palier 5 | retours s08 · spec v09 règle 6 |
| Écran de configuration du foyer complet (acteurs + cycle de fond + couleurs, persisté) | ⬜ | Palier 10 | retours s01 (#7) · spec p5 |
| Édition des acteurs « autres » (ajout/édition/suppression) | ✅ | s08 (édition) + s09 (ajout) + s13 (suppression) / Paliers 4-5-8 | spec règle 4 · retours s08 |
| Affichage/actions adaptés au type d'acteur | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| **Création d'acteurs par le parent configurateur** (nounou / grand-parent / nouveau parent en couple / autre), **email obligatoire** à la création → crée le compte utilisateur (inactif, cf. É10) | ⬜ | Palier 5-6 | retours s08 (idée) · spec règles 4/6-7 |

### Épic 3 — Fondations techniques (architecture & API)
*Socle découplé : API exposée, front WASM, conventions de code, swagger.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Controllers HTTP exposant les commandes d'écriture (adaptateur de gauche) | ✅ | s04 / Palier 1 | retours s03 (#9) · spec p1 |
| Hôte d'API détachable (back démarrable seul, front consomme une API distante) | ✅ | s05 / Palier 1 | spec v05 p1 · besoins s04 |
| Migration front Blazor Server → WASM consommant l'API | ✅ | s05 (`Sdk.BlazorWebAssembly` réel) | retours s03 (#6) · spec p1 |
| SignalR cantonné au push lecture seule (jamais d'écriture) | ✅ | s04 + s05 (hub porté par l'hôte API) | retours s03 · spec p1 (séparation canaux) |
| Convention code-behind systématique (`.razor.cs`, pas de `@code` inline) | 🟡 | s04 partiel (transfert en retrait) | retours s03 (#7, dette) |
| API explorable : document OpenAPI **+** UI interactive (Scalar) | ✅ | s05 (Scalar sur OpenAPI natif .NET) | retours s03 (#8) · spec v05 p1 |
| CORS : origine du front autorisée à appeler l'API distante | ✅ | s05 | spec v06 règle 25 |
| Ports & adaptateurs visibles (hexagonal : gauche/droite/domaine) | 🟡 | s04 (gauche matérialisé) · droite **config foyer durable Mongo** (s09), reste du domaine encore InMemory | retours s03 (#10) · refacto à venir (homogénéiser les frontières) |

### Épic 4 — Calendrier & grille de lecture
*Calendrier navigable (semaine + 4 semaines) lisible d'un coup d'œil.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Grille agenda 5 semaines (35 jours) en lecture seule | ✅ | s03 | spec p3 · retours s02 (#3-5) |
| Positionnement des slots dans les cases jour/horaire | ✅ | s03 | spec règles 12/114 |
| Code couleur par personne sur les cases-jour | ✅ | s03 | spec règles 14/158 |
| Slots empilés dans l'ordre horaire | ✅ | s03 | scénario 5 s03 |
| Fenêtre stricte 35 jours (bornes inf./sup.) | ✅ | s03 | scénarios 1/7 s03 |
| Navigation dans le mois (semaines précédente/suivante) | ⬜ | Palier 3 item 2 | spec p3 · retours s02 (#3) |

### Épic 5 — Lisibilité & identité visuelle
*Rendre la responsabilité explicite (pas seulement une teinte) et habiller l'app.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Libellé + nom du responsable dans les cases (pas que la teinte) | ✅ | s07 / Palier 3 | retours s03 (#3) · spec règle 16 |
| Légende des couleurs (mapping acteur → couleur, dédoublonnée, masquée si vide) | ✅ | s07 / Palier 3 | spec règle 16 · retours s03 (#3) |
| Thème visuel en accord avec le domaine (garde d'enfants) | ✅ | s07 / Palier 3 | retours s01/s02/s03 (« j'aime pas le thème ») · spec règle 20 |
| Nom long lisible (troncature + nom complet au survol) | ✅ | s07 / Palier 3 | spec règle 16 (dérivé) |
| Thème sombre + toggle (avec persistance de la préférence) | ⬜ | backlog (additif) | retours s07 (idée) · spec v08 règle 21 |
| Personnalisation des couleurs par utilisateur authentifié | ⬜ | Palier 13 (auth) | spec règle 16 |

### Épic 6 — Créneaux & slots de localisation
*Poser et gérer les slots (où est l'enfant) : création, validation, affichage.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Poser un slot (enfant → lieu, début/fin, date) | ✅ | s01 + s02 + s04 (API) | spec règles 8/112 |
| Rejet : durée nulle interdite | ✅ | s01 | scénario 2 s01 |
| Slot franchissant minuit (rendu sur deux jours) | ✅ | s01 | scénario 3 s01 |
| Rejet : lieu inexistant | ✅ | s01 + s02 + s04 (API) | scénario 4 s01 |
| Signalement de chevauchement (création acceptée + avertissement) | ✅ | s01 | scénario 5 s01 |
| Droits : seul Parent crée/édite les slots | ✅ | s01 | spec règle 7 |
| Poser un slot en contexte via dialog (depuis une case) | ✅ | s11 / Palier 7 | retours s02 (#10) · spec p3 |
| **Slot imbriqué** — un slot peut en contenir un autre (ex. enfant chez mamie **et** doit aller à son cours de natation) | ⬜ | à séquencer | retours s07 (idée) |

### Épic 7 — Périodes de garde & responsabilité récurrente
*Modéliser la responsabilité de garde sur une période (distincte des slots).*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Affecter une période à un responsable | ✅ | s01 + s02 + s04 (API) | spec règles 5/8/118 |
| Rejet : responsable requis | ✅ | s01 | scénario 8 s01 |
| Bornes de période paramétrables | ✅ | s01 | scénario 9 s01 |
| Édition concurrente — rejet sur état périmé | ✅ | s01 | scénario 10 s01 |
| Suppression de période (depuis dialog) | ⬜ | Palier 3 item 3 | retours s02 (#6) · retours s03 (trou) |
| Affecter période en contexte via dialog | ✅ | s11 / Palier 7 | retours s02 (#7) · spec p3 |
| Responsabilité de fond déclarée en config foyer (le cycle, alternance parité ISO, EN MÉMOIRE) | ✅ | s10 / Palier 6 | spec règles 5/11 · besoins s07/s08 |
| Cycle de fond **riche** (ancre/début explicite, frontière de jour, plage début/fin, sur-cycle vacances, WE-only) | ⬜ | à séquencer (rouvre l'ancrage ISO) | retours s10 (R3/R4) |

### Épic 8 — Transferts & bascule de responsabilité
*Modéliser les transferts (qui dépose, qui récupère, où, quand) bornant les périodes.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Définir un transfert (date, dépositaire, récupérateur, lieu, heure) | ✅ | s01 + s02 + s04 (API) | spec règles 17-18 |
| Rejet : transfert incomplet | ✅ | s01 | scénario 12 s01 |
| Transfert dérivé automatiquement par défaut (saisie réservée au ponctuel) | ⬜ | Palier 5-6 | spec règle 17 · retours s02 (#14) |
| Transfert ponctuel & modifiable | 🟡 | s01 (modèle) + Palier 5+ | spec règle 18 |
| Transfert en contexte via dialog (3e entrée du menu clic-case + retrait page dédiée) | ✅ | s12 / Palier 7 | retours s02 (#8) · spec p3 · G2 PO s11 |
| Transferts exposés dans le panneau cloche | ⬜ | Palier 4 item 6 | spec règle 20 · retours s02 (#8)/s03 (#4) |

### Épic 9 — Notifications & événements à venir
*Exposer transferts, changements et rappels comme événements (panneau cloche).*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Panneau cloche listant les événements à venir | ⬜ | Palier 4 item 6 | spec règles 20/120 · retours s02 (#8)/s03 (#4) |
| Transferts listés comme événements (date, acteurs, lieu, heure) | ⬜ | Palier 4 item 6 | spec règle 20 |
| Changements de planning exposés comme événements | ⬜ | Palier 4 item 6 | spec règle 20 |
| Notifications in-app push temps réel (SignalR, lecture seule) | ✅ | s01 (infra) | spec règles 19-20 |
| « Qui récupère ce soir » — immédiat (qui-quand-où du jour) | ⬜ | Palier 4 item 6 | spec p4 · spec v03 incrément 2 |

### Épic 10 — Authentification & accès utilisateurs
*Authentifier les acteurs réels, lever le risque d'adoption du second parent, ouvrir l'accès.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Landing page (identifie le foyer, invite à s'authentifier) | ⬜ | Palier 13 | retours s01 (#2) · spec p8 |
| Authentification OAuth (Gmail / Apple / Microsoft) | ⬜ | Palier 13 | retours s01 (#2) · spec p8 |
| Gestion des sessions utilisateur (persistance, logout) | ⬜ | Palier 13 | spec p8 |
| Droits d'accès par utilisateur identifié (selon rôle) | ⬜ | Palier 5 + 13 | spec règles 6-7 |
| Personnalisation des couleurs par utilisateur authentifié | ⬜ | Palier 13 | spec règle 16 |
| **Compte utilisateur créé inactif** (à la création d'un acteur avec email, cf. É2) ; le créateur a **tous les droits en modification + impersonation** tant que le compte est inactif | ⬜ | Palier 13 | retours s08 (idée) |
| **Prise en main de son compte** par l'utilisateur réel à sa 1ʳᵉ connexion (via une **demande**) ; après prise en main, il édite ses caractéristiques selon son rôle | ⬜ | Palier 13 | retours s08 (idée) |
| **Droits par rôle après prise en main** : Nounou / Grand-parent = éditer son profil + faire des demandes aux parents ; Second parent = éditer son profil + administrer le planning de l'enfant **sur sa période** + demandes d'adaptation de période / d'ajout de slot | ⬜ | Palier 13 | retours s08 (idée) · spec règles 6-7 |

### Épic 11 — Imprévu & échange
*Gérer les exceptions : malade, retard, échange de dernière minute avec accord.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Signalement d'imprévu (malade, retard…) + notification immédiate | ⬜ | Palier 7 | spec p7 |
| Échange de dernière minute (proposition + accord requis) | ⬜ | Palier 7 | spec p7 |
| Transferts temporaires (exception, non récurrents) | ⬜ | Palier 7 | spec règles 17-18 |

### Épic 12 — Écriture en contexte (recâblage post-API)
*Faire passer les saisies par le canal requête/réponse et vérifier leur réapparition.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Dialogs d'écriture (poser slot + affecter période) depuis les cases (menu clic-case, pré-rempli date case, échec/annulation/chevauchement/gating Invité) | ✅ | s11 / Palier 7 | retours s02 (#7/8/10)/s03 |
| Dialog « Définir un transfert » en contexte + retrait page dédiée (referme l'épic) | ✅ | s12 / Palier 7 | retours s02 (#8) · G2 PO s11 |
| Suppression de période depuis dialog | ⬜ | à séquencer | retours s02 (#6) · retours s03 (trou) |
| Recâblage de l'écriture via API HTTP (au lieu du DI direct) | ✅ | s05 (poser/affecter/transfert via API distante WASM) | retours s03 (#5) · spec p1 |
| Rafraîchissement immédiat : la saisie réapparaît dans la grille | ✅ | s06 / Palier 2 | retours s03 (#5, bug runtime) |

---

## À faire (paliers de la spec vivante v14)

> Vue de séquencement (ordre de livraison). Chaque palier agrège des besoins des épics.
> Numérotation alignée sur la **séquence de livraison de v14** : palier 7
> (écriture-en-contexte) **livré complet** ; **palier 8 = CRUD acteurs** — tranche 1
> **suppression livrée (s13)**, tranche 2 **impersonation bornée = prochain sujet** ;
> **palier 9 = Calendrier navigable** ; paliers suivants décalés d'un cran. Les sujets
> techniques (persistance réelle du reste du domaine, PWA) sont séquencés **derrière
> l'usage** (arbitre : l'usage tranche), Docker en garde-fou d'outillage.

| Palier | Besoin | Épics concernés | Origine | Statut |
|-------:|--------|-----------------|---------|:------:|
| 1 | Fermeture de la fondation — **hôte d'API détachable** (back démarrable seul) + **UI d'exploration interactive** (Scalar) + CORS + échec clair si API injoignable | É3 | spec v05 p1 · besoins s04 | ✅ s05 |
| 2 | **Saisie visible** — la saisie réapparaît à la bonne **date** (défaut = aujourd'hui) **et** en **couleur du parent** (identifiant stable) | É6, É7, É12 | spec v05 p2 · besoins s04 (défaut confirmé) | ✅ s06 |
| 3 | **Lisibilité & thème** — nom + légende des périodes/responsable **+** thème en accord avec le domaine (pris **en bloc**) | É5 | spec v07 p3 · besoins s06 (G1) | ✅ s07 |
| 4 | **Config foyer · édition des acteurs (VOLATILE)** — écran éditant noms + couleurs en mémoire/session, grille relue immédiatement | É2, É1 | spec v08 règle 5 · besoins s07 (G2 PO) | ✅ s08 |
| 5 | **Config foyer PERSISTANTE** — **ajout/édition d'acteurs** (parent/autre/nounou, id stable neuf) **+ persistance Mongo BORNÉE à la config foyer** (adaptateur de droite, ports inchangés) ; survit au redémarrage. Reste du domaine InMemory | É2, É1, É3 | spec v09 règle 6 · besoins s08 (G2 PO, révision d'arbitre bornée) | ✅ s09 |
| 6 | **Récurrence des périodes** (cycle de fond définissable/éditable, alternance parité ISO, EN MÉMOIRE) | É7, É1 | spec v09 règle 10 · besoins s07/s08 (IMPORTANT) | ✅ s10 |
| 7 | **Écriture en contexte (dialogs)** — menu au clic sur une case → « Poser un slot » / « Affecter une période » / « Définir un transfert » pré-remplies sur la date de la case (échec/annulation/chevauchement/gating Invité), tous les écrans dédiés retirés | É6, É7, É8, É12 | spec v12 p7 · besoins s10/s11 | ✅ s11+s12 (refermé complet) |
| 8 | **CRUD acteurs** — tranche 1 **suppression** (Delete d'un acteur, règle 6 + neutralisation par repli des cases orphelines) sur store Mongo réel **✅ s13** ; tranche 2 **impersonation bornée** = prochain make-gherkin (⬜) | É2, É1, É10 | spec v14 p8 · besoins s12/s13 (G2 PO) | 🟡 |
| 9 | **Calendrier navigable** (passé/futur, vues prédéfinies semaine/mois/4-sem) **+ sélection de plage de cases** pour définir une période | É4, É7 | spec v13 p9 · retours s02/s03 | ⬜ |
| 9bis | **Survol → résumé de la journée** (enrichissement après ~1s ; périmètre à cadrer) | É5, É9 | spec v09 · besoins s07 | ⬜ |
| 10 | Alimentation & saisie — **config foyer durable restante** (lieux, set couleurs, cycle de fond) + Admin/Parent/Autre, écran de config complet | É1, É2, É7 | spec v05 p5-6 · retours s01/s03 | ⬜ |
| 11 | Immédiat & événements à venir — panneau cloche (transferts + changements + « qui récupère ce soir ») | É8, É9 | spec v05 p7 · retours s02/s03 | ⬜ |
| 12 | Imprévu & échange — malade/retard/échange + transferts dérivés automatiquement par défaut | É8, É11 | spec v05 p8 · spec règles 19-20 | ⬜ |
| 13 | Ouverture de l'accès (auth OAuth, landing, comptes inactifs + impersonation + prise en main par rôle, personnalisation des couleurs, thème sombre persisté) | É10, É2, É5 | spec v05 p9 · retours s01/s07/s08 | ⬜ |
| 14 | **Adaptateurs de droite — persistance réelle du RESTE du domaine** (slots/périodes/transferts ; la config foyer en a été le premier client, amorcé au palier 5). **Borne anti-cliquet : ne remonte pas devant l'usage** | É1, É3 | spec v09 règle 30 · besoins s05/s08 (séquencé derrière l'usage) | ⬜ |
| 15 | **PWA — saisie hors-ligne** (cache + file d'écritures rejouée au retour de connexion, au-delà de l'échec clair livré au s05) | É12, É3 | spec v06 · besoins s05 (séquencé derrière l'usage) | ⬜ |

> **Séquencement acté (v09, `/5-consolidation` s08) :** la **config foyer persistante** (ajout
> d'acteurs + Mongo borné) passe **devant** la récurrence. **Révision d'arbitre bornée** (G2 PO) :
> Mongo (persistance réelle) est tiré **devant l'usage mais BORNÉ à la config foyer** (premier
> client de la config durable). **Borne anti-cliquet** : la persistance du **reste** du domaine
> (slots/périodes/transferts) reste en queue (palier 14). Corollaire reformulé **« durable ICI
> (config foyer), volatile encore ailleurs »**. **Docker** reste un **garde-fou d'outillage**.

> **Piste technique (PWA)** — *Event sourcing + outbox pattern* comme socle d'une file
> d'écritures rejouable : l'**outbox** garantit qu'une commande acceptée hors-ligne sera
> rejouée puis diffusée **exactement une fois** (couplage écriture→diffusion fiable,
> cohérent avec « l'écriture aboutie déclenche la diffusion »). L'**event sourcing** aide
> à reconstruire/rejouer l'état et résoudre les conflits de rejeu, mais c'est un changement
> de modèle de persistance lourd : à n'adopter que si le besoin offline/rejeu/audit le
> justifie ; sinon **outbox + file côté client (IndexedDB)** suffit pour l'amorce. À trancher
> au palier PWA. (Avis agent make-gherkin, cadrage s05.)

## Dépendances entre épics (pour la découpe des sprints)

- **É3 (Fondations API) → É12 (Écriture en contexte)** : controllers opérationnels avant de recâbler les dialogs.
- **É1 (Config foyer) → É2 (Modèle d'acteurs)** : déclarer les acteurs requiert la persistance.
- **É4 (Calendrier) + É5 (Lisibilité)** traités ensemble (Groupe 1) : la lisibilité enrichit le calendrier déjà livré.
- **É12 (Écriture) → É9 (Cloche)** : les événements apparaissent après que les écritures soient observables.
- **É10 (Auth) → personnalisation couleurs d'É5** : requiert l'identification.
- **É7 (Périodes) + É8 (Transferts) + É9 (Cloche)** forment un bloc « responsabilité + événements ».
- **É11 (Imprévu)** vient en dernier, paliers 1-6 stabilisés.

## Garde-fous structurels (non-paliers, hors observable métier)

> Invariants de structure portés au fil de l'eau, sans scénario codant dédié.

- Convention code-behind systématique (`.razor` + `.razor.cs`, pas de `@code` inline) — sprint 04+.
- API explorable (Scalar/OpenAPI) — livrée au palier 1 (s05).
- Séparation des canaux : écriture = requête/réponse ; diffusion temps réel = lecture seule (jamais d'écriture par la diffusion).
- **Conteneurisation Docker** (hôte API + front WASM + base, montables ensemble via compose) — garde-fou d'outillage hors-incrément, sans observable métier (comme l'API explorable). Débloqué par le palier 1 + la persistance réelle (palier 14). Origine : PO post-s05.

## Dettes explicitement signalées

- Données en dur dans `Foyer.cs` (É1) — persister en base — retours s03 (#11).
- Aucune édition/suppression de période depuis l'IHM (É7) — « trou fonctionnel assumé » — retours s03.
- ~~Saisies invisibles à l'écran (É12)~~ — **éteint au s06 (palier 2)** : faux bug (date par défaut → `IDateTimeProvider`) ET vrai défaut couleur (mapping libellé→identifiant stable + seed) corrigés, 8/8 vert — retours s03 (#5) · consolidation s05 · livré s06.
- Risque d'adoption du second parent (É10) — repoussé au palier 13 (auth), « ne pas laisser glisser ».
- Faux sentiment de progrès — 2 sprints structurels d'affilée (s04, s05) sans besoin produit observable ; **résorbé au s06** : le palier 2 (Saisie visible) a rendu la main à l'usage (8/8 vert). Vigilance maintenue : ne pas remonter les paliers techniques 10/11 devant l'usage.
- `@code` inline restant (`Legende.razor`, `Pages/Home.razor`) + frontières hexagonales à homogénéiser + séparation des projets (É3) — **cible de la refacto technique HORS pipeline décidée à la clôture s09** (iso-comportement, invariant 161/161). Retours s03 (#7).
- ~~Cycle multi-semaines non affiché/éditable (É1)~~ — **éteint au s10 (palier 6)** : cycle de fond affiché (grille + légende) et éditable (section config), EN MÉMOIRE ; durabilité séquencée au palier 10.
- ~~**Dropdown « Acteur du foyer » périmée au renommage** (É2, /configuration)~~ — **résorbée au s13** : le sélecteur lit désormais le store vivant `Foyer.ActeursEditables` (cohérence règle 5 tenue partout, y compris après suppression). Signalée au gate s10, fix embarqué tête de sprint s13.
- **Cycle de fond riche réclamé** (É7) — l'usage (gate s10) demande ancre/début, frontière de jour, plage début/fin, sur-cycles vacances, WE-only : au-delà du plus petit incrément livré, sujet plein séquencé (+5).
- **Flakes temps-réel SignalR** (É3, `FrontWasmConfig*TempsReel*`) — verts en isolation, flaky sous charge parallèle (timing SignalR/Docker) ; **dette de test** (pas un bug `src/`). Convention anti-flake codifiée (rétro s11, `ihm-builder`) ; **garde déterministe `WaitForState(acteur-foyer)` posé sur 7 `*TempsReel*` au s13** (course `UnknownEventHandlerId` rendue déterministe par la touche d'un composant partagé) ; **rétrofit complet = P2** (helper bUnit partagé + audit, rétro s13), prérequis de l'édition concurrente (P3). Constaté s11, partiellement traité s13.
- ~~Dernière saisie hors-contexte restante~~ — **éteinte au s12 (palier 7)** : page/route/lien `/planning/definir-transfert` supprimés, 3e dialog transfert livrée, épic É12 refermé. Constaté s11, résolu s12.

> **Idées PO consolidées (retours s07)** — les 3 idées de la section « Idée pour la suite »
> ont été replacées dans leurs épics : *slot imbriqué* → **É6** ; *parents liés via leurs
> enfants* → **É1** ; *parents recomposés en couple gérant leurs enfants respectifs* → **É1**
> (familles recomposées, enrichie).

> **Idées PO consolidées (retours s08)** — l'idée « gestion des comptes utilisateurs »
> (créer les acteurs avec email → compte inactif, impersonation par le créateur, prise en
> main par demande, droits par rôle) a été replacée dans ses épics : *création d'acteurs avec
> email* → **É2** ; *compte inactif / impersonation / prise en main / droits par rôle (nounou,
> grand-parent, second parent)* → **É10** (Palier 13). Le couplage É2↔É10 (création d'acteur =
> amorce du compte) est à expliciter quand le palier 13 sera pris. 