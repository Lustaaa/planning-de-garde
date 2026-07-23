# Product backlog — FAIT (planning-de-garde)

> **Archive du livré.** Miroir de [`BACKLOG.md`](BACKLOG.md) (ce qui reste) : ce fichier
> consigne **tout ce qui est livré et clôturé** — sprints, besoins d'épic ✅, paliers ✅ et
> dettes refermées. Source de vérité du *déjà fait* ; le *reste à faire* vit dans `BACKLOG.md`.
> Le *pourquoi* vit dans la spec vivante [`docs/specs/`](specs/index.md).
>
> **54 sprints livrés · suite complète 945/945 verte en série (s54).** Fichiers de sprint clos archivés sous
> [`docs/_archive/sprints/`](_archive/sprints/). *(Lignes s29→s31 non backfillées dans la table
> ci-dessous — trail détaillé dans leurs fichiers de sprint archivés + `BACKLOG.md`.)*

## Sprints livrés

| Sprint | Sujet | Livré |
|-------:|-------|-------|
| 01 | Semaine de garde (grille agenda, cycle récurrent, slots/périodes/transferts) | Modèle de garde + 12 scénarios domaine + grille initiale |
| 02 | Réparer le câblage IHM ↔ actions (render mode interactif) | Actions d'écriture câblées au front |
| 03 | Calendrier — grille de lecture (5 semaines, lecture seule, 2 niveaux de couleur) | Projection `GrilleAgendaQuery` + grille 5×7 lecture seule |
| 04 | `controllers-wasm-fondation` — canal d'écriture HTTP + recâblage front via API, SignalR = diffusion lecture seule | Canal HTTP `poser-slot`/`affecter-période` + OpenAPI (82 verts) |
| 05 | `host-api-separable` — hôte d'API détaché + front WASM réel + CORS + UI Scalar + échec clair si API injoignable | Projet `PlanningDeGarde.Api` détaché + `Sdk.BlazorWebAssembly` + Scalar + SignalR distant (96 verts) — **palier 1 refermé** |
| 06 | `saisie-visible` — réapparition à la bonne date (défaut aujourd'hui via `IDateTimeProvider`) + couleur du parent | Port `IDateTimeProvider` + sélecteurs sur id stable + seed (108 verts) — **palier 2 refermé** |
| 07 | `lisibilite-theme` — nom du responsable + légende couleur + thème métier | Port `IReferentielResponsables` + `Legende` + repli gris + thème CSS (120 verts) — **palier 3 refermé** |
| 08 | `config-foyer-acteurs` — édition acteurs (renommer/recolorier) en VOLATILE, grille relue immédiatement | Store mutable + commande `EditerActeur` + écran `ConfigurationFoyer` + SignalR (143 verts) — **palier 4 refermé** |
| 09 | `config-foyer-persistante` — ajout d'acteurs + persistance Mongo bornée config foyer (seed-once) | `AjouterActeurHandler` + adaptateur `ConfigurationFoyerMongo` (161 verts, pivot Mongo) — **palier 5 refermé** |
| 10 | `recurrence-des-periodes` — cycle de fond éditable (N semaines, parité ISO), résolution grille, EN MÉMOIRE | `CycleDeFond` + `DefinirCycleHandler` + `CycleDeFondEnMemoire` + section config (183 verts) — **palier 6 refermé** |
| 11 | `ecriture-en-contexte` — dialogs Poser slot / Affecter période depuis case, échec = dialog ouverte, écrans dédiés retirés | `PoserSlotDialog` + `AffecterPeriodeDialog` + menu clic-case + ancrage `DateContexte` (179 verts) — **palier 7 (part 1)** |
| 12 | `transfert-en-contexte` — 3ᵉ dialog « Définir un transfert » + retrait dernier écran dédié | `DefinirTransfertDialog` + menu clic-case + suppression page/route (182 verts) — **palier 7 refermé, épic É12 fermé** |
| 13 | `crud-acteurs-suppression` — suppression d'acteur (Mongo réel) + neutralisation orphelins, idempotence | `SupprimerActeurHandler` + filtre `Resolvable()` + IHM (196 verts) — **palier 8 tr.1, cycle de vie C/R/U/D complet** |
| 14 | `impersonation-bornee` — impersonation lecture seule (incarner, vue selon rôle effectif, retour auto sur suppression) | `SessionPlanning` identité réelle/effective + `TypeActeur` read-only + bandeau/gating (214 verts) — **palier 8 tr.2** |
| 15 | `calendrier-navigable` — nav ±semaine, vues Semaine/4-sem/Mois, « Aujourd'hui », affectation par plage **+ persistance Mongo de TOUT le domaine + démarrage sans seed** | `SessionPlanning` ancre+vue + `GrilleAgendaQuery.Projeter` + 4 adaptateurs Mongo + DI `Foyer:Persistance` + retrait seed runtime (234 verts) — **paliers 9 ET 14 refermés. Borne anti-cliquet règle 30 levée** |
| 16 | `supprimer-editer-periode` — suppression de période depuis l'IHM (Mongo réel), repli sans nom fantôme, idempotence | `SupprimerPeriodeHandler` + `PeriodesDuJourQuery` + endpoint + `SupprimerPeriodeDialog` (246 verts) |
| 17 | `editer-periode` — édition de période (re-borner/réaffecter, clé = id stable), rejet sur état périmé | `EditerPeriodeCommand`/`Handler` + endpoint + 5ᵉ usage menu clic-case (258 verts) — **dette édition/suppression période refermée** |
| 18 | `supprimer-slot` — suppression d'un slot sur une journée (Mongo réel), idempotent, slot à cheval sur minuit | `SupprimerSlotCommand`/`Handler` + `SlotsDuJourQuery` + `JoursCouverts` (270 verts) — **épic É6 : suppression slot** |
| 19 | `acteurs-reels-partout` — retrait des « Parent A/B » fictifs, store vivant exclusif, store vide → grille neutre | Résolution grille/légende sur id stable + libellés fictifs purgés + message store-vide (281 verts) — **épic É2 acteurs réels** |
| 20 | `config-foyer-onglets` — config en 3 onglets (Acteurs/Période/Slot récurrent) + convergence du dernier sélecteur | Onglets + sélecteur sur `IEnumerationActeursFoyer` + `Foyer.ActeursEditables` retirée + abonnement SignalR (288 verts) |
| 21 | `modele-de-roles-editable` — référentiel de rôles éditable (CRUD, Mongo), rôle affectable borné, repli neutre | Référentiel rôles + persistance Mongo + affectation `RoleDe` + onglet Acteurs (317 verts) — **retours s17 #3/#4/#5** |
| 22 | `auth-fondation-identite` — `CompteUtilisateur` ↔ acteur (1-1, Mongo) + invariant admin=Parent + désassociation | `CompteUtilisateur` + `AdministrationFoyer` (Domain pur) + ports + adaptateurs (351 verts) — **auth tr.1** |
| 23 | `auth-session-locale-acteur-par-defaut` — connexion locale email + session serveur + logout + acteur par défaut = moi | `SeConnecterCommand`/`Handler` + `SessionOuverte` + `ResoudreActeurParDefautQuery` (367 verts) — **auth tr.2a** |
| 24 | `auth-utilisable-activation-et-page-login` — activation Inactif→Actif + page `/connexion` dédiée + menu utilisateur | `ActiverCompteCommand`/`Handler` + `CompteUtilisateur.Activer()` + `/connexion` landing + `MenuUtilisateur` (380 verts) — **auth E2E** |
| 25 | `terminer-tout-le-login` — protection routes + fix bug rôle≠acteur + mot de passe PBKDF2 + inscription libre-service + récup par jeton + OAuth Google/MS/Apple | Guard routes + PBKDF2 + inscription + jeton reset (`IEnvoiMail`) + `IFournisseurOAuth` + boutons OAuth (412 verts) — **login COMPLET livré. Entorse G2 : volets OAuth/SMTP prouvés par doublure de port ; câblage réel = dette** |
| 26 | `refonte-graphique` — refonte visuelle complète « Studio » (habillage pur, zéro régression comportement) : tokens `--pdg-*`, typo Fraunces/Inter self-hosted, **thème clair/sombre persisté** (`localStorage`, `data-theme`), calendrier en mini-cartes, dialogs/légende/layout habillés, parité Safari iOS | Tokens `--pdg-*` (`:root` + `[data-theme]`) + polices offline + refonte de tous les écrans clair+sombre (14/14 ✅, **458/458**) — **épic É5 (refonte visuelle + thème sombre) livré** |
| 27 | `coherence-config-planning` — lieux hissés en **référentiel foyer éditable + persisté**, pilotant validation de pose ET sélecteurs des dialogs ; ancien `ILieuRepository`/`FoyerLieuRepository` en dur retiré | Référentiel lieux (InMemory seedé / Mongo durable sans seed) + canal vivant unique validation+sélecteurs + couleur config→grille en non-régression (6/6, **466/466**) — **cohérence config→planning (lieux) livrée** |
| 28 | `cablage-auth-reel-reset-et-motdepasse` — câblage auth **réel** : reset mot de passe **E2E** (SMTP dev Smtp4dev + store jetons Mongo durable + expiration 60 min + 2 écrans IHM), **login email+mot de passe** (back+IHM), rapprochement Google **logique**, **seed compte démo par chemin réel** (flag `Demo:SeedCompteDemo`, convergent) | Adaptateurs concrets `IEnvoiMail`/`IReferentielJetonsReset` + DI handlers récup/reset + endpoints + écrans mot-de-passe-oublié/redéfinir-par-jeton + champ mot de passe login + `ConnexionOAuthHandler`/endpoint `oauth/google/demarrer` (doublure+manuel) + seed démo convergent (10/10, **485/485**) — **reset + login MDP OPÉRATIONNELS en runtime réel ; solde la moitié de la dette câblage s25. Reliquat P0 : Google OAuth réel + écran `definir-mot-de-passe`** |
| 32 | `refonte-config-acteurs-crayon-modal` — **1er incrément de l'épic Refonte Config foyer** : onglet Acteurs passé du patron **édition inline** au patron **tableau lecture seule + crayon → modal** (états actif/admin en pastille lecture ; modal éditant nom/couleur/rôle via les commandes CRUD existantes, id stable non éditable ; « Ajouter » = même modal vide) ; refus domaine → modal reste ouverte, motif + saisie conservés ; **Parent-gated** (Invité = lecture seule, ni crayon ni « Ajouter ») ; **temps réel SignalR** (2ᵉ écran converge sans reload) | Sprint pur @ihm (0 @back, réutilise le CRUD acteurs s05→s24, aucun handler/query neuf) : swap atomique inline→modal (Sc.1-4, ~34 fichiers d'acceptation migrés en un commit) + erreur/gating/SignalR en incréments propres (7/7 ✅, **569/569**) — **1er incrément épic Refonte Config foyer livré** |
| 48 | `imprevu-malade-retard` — **signalement d'imprévu DÉDIÉ (malade / retard), palier 15** : cas **NON-négocié / purement INFORMATIF** (« l'enfant EST malade », « je serai en retard »), distinct de l'échange consenti s47. **AUCUNE surface ni store neufs** : réutilise entrée menu clic-case + cloche s47 + journal `IJournalChangements` + diffusion `INotificateurChangement`. Signaler **consigne au journal SANS toucher la résolution** (invariant s47 prouvé : store surcharges intact, case inchangée) ; type inconnu refusé avant écriture (agrégat `Imprevu`) ; motif optionnel vide accepté ; lu/non-lu + marquer-lu idempotent ; deux adaptateurs InMemory + Mongo durable | @back Sc.1-4 (journal + flux + limite + erreur) + @ihm Sc.5-7 (entrée menu Parent-gated + mini-dialog malade/retard Échap=Annuler + notif cloche INFORMATIVE sans action de suivi + temps réel 0-GET) — **7/7 ✅**, gate G3 validé PO, aucun retour produit — **palier 15 imprévu informatif livré** |
| 50 | `cloche-immediat-digest` — **DIGEST « immédiat » DANS LA CLOCHE (palier 2 vision « immédiat & rappels » / palier 14 roadmap COMPLÉTÉ)** : ramène dans la cloche s47 le contenu de lecture retiré s42/s43 en s44 — « qui récupère aujourd'hui / ce soir » (responsable résolu surcharge>fond>neutre + où/slot s29 + transfert saisi OU dérivé s31) + « transferts à venir » des N prochains jours de la fenêtre chargée (chrono croissant). **Query PURE `DigestImmediatQuery`** composant `GrilleAgendaQuery` (records `ResponsableDuJour`/`TransfertDuJour`/`JourDigest`/`DigestImmediat`, miroir des ex-`CarteDuJourQuery` s42 / `AVenirQuery` s43), **AUCUN store neuf, AUCUNE mutation**, identique InMemory + Mongo durable ; replis fidèles ; fenêtre vide / jour courant hors-fenêtre = section vide neutre, store des surcharges intact | @back Sc.1-4 (digest composé + à-venir chrono croissant + replis fidèles + invariant 0-mutation 2 adaptateurs) + @ihm Sc.5-8 (section EN TÊTE du panneau cloche lecture stricte + reprojection `EtatDigestPartage` 0-GET depuis fenêtre grille + convergence temps réel 0-GET sur push + gating Parent) — **8/8 ✅**, suite **857/857**, **gate G3 validé PO DU PREMIER COUP**, aucun retour produit — **palier 2 vision (cloche immédiat) COMPLÉTÉ, anti-cliquet s44 tenu (aucune carte/panneau réintroduit sur la grille)**. Limitation assumée routée backlog : digest borné à la fenêtre grille chargée (persistance hors-fenêtre non rouverte) |
| 51 | `action-suivi-imprevu-proposer-echange` — **ACTION DE SUIVI sur un imprévu : réagir (malade/retard s48) en PROPOSANT UN ÉCHANGE (palier 15, ferme la boucle ouverte s48)**. Depuis la notif d'imprévu s48 dans la cloche, entrée contextuelle « proposer un échange » (jour+enfant hérités) **compose `ProposerEchange` s47**. **AUCUN modèle/store neuf** (réemploi INTÉGRAL `Proposition` s47 + journal s48). Use case de composition `ProposerEchangeSuiteImprevuHandler` + endpoint `/api/canal/proposer-echange-suite-imprevu`. **AMENDE s48 « imprévu = informatif sans action de suivi » SANS la contredire** : l'imprévu reste un FAIT informatif non muté ; on **greffe À CÔTÉ** une **proposition d'échange DISTINCTE** (modèles `Imprevu` s48 / `Proposition` s47 restent séparés). Invariants prouvés : proposer SANS écriture (store surcharges intact), ACCEPTER compose la délégation s44 (R24), REFUSER sans écriture, soi-même/inconnu/orphelin refusés AVANT écriture, last-write-wins R11, hors fenêtre sans crash, deux adaptateurs InMemory + Mongo durable | @back Sc.1-4 (proposition pending pré-remplie sans écriture + accepter/refuser + last-write-wins/hors-fenêtre + erreurs/modèles séparés) + @ihm Sc.5-7 (action dans la notif d'imprévu Parent-gated → `ProposerEchangeDialog` s47 pré-remplie Échap=Annuler + proposition actionnable chez le recevant + temps réel 0-GET par reprojection) — **7/7 ✅**, suite **872/872**, **gate G3 validé PO DU PREMIER COUP** (garde-fou s49 build servi appliqué), aucun retour produit — **boucle imprévu→réaction ouverte s48 FERMÉE** |
| 52 | `echange-plage-jours` — **ÉCHANGE consenti s47 étendu du JOUR UNIQUE à la PLAGE `[J1..J2]` (miroir exact de la progression délégation s44→s45), BORNÉ MONO-ENFANT** (multi-enfants routé backlog). Modèle `Proposition` s47 enrichi d'un `JourFin` (défaut fin=début → parité s47 stricte ; `fin<début` refusé dans l'agrégat) ; `ProposerEchange` accepte l'intervalle ; **`AccepterProposition` COMPOSE la délégation-plage s45** (surcharge multi-jours via s06 + transferts bicolores dérivés s31 aux DEUX frontières) ; `RefuserProposition` sans écriture. **AUCUN store/commande neuf**. Invariant anti-vert-qui-ment prouvé (pending = 0 surcharge, store intact) sur InMemory + Mongo durable ; bornes/erreurs refusées avant écriture ; last-write-wins R11. **IHM : AUCUNE surface neuve** — champ « jusqu'au » ajouté à `ProposerEchangeDialog` s47 (miroir s45) | @back Sc.1-6 (proposer plage pending sans écriture + défaut mono-jour inchangé + accepter compose la plage + refuser sans écriture + bornes refusées avant écriture + last-write-wins/hors-fenêtre, deux adaptateurs InMemory + Mongo durable) + @ihm Sc.7-10 (champ « jusqu'au » Parent-gated + notif de plage actionnable dans la cloche + Échap=Annuler port s33 & refus domaine garde la dialog ouverte + saisie conservée + temps réel toute la plage 0-GET par reprojection) — **10/10 ✅**, suite **889/889**, **gate G3 validé PO DU PREMIER COUP** (garde-fou s49 appliqué), aucun retour produit — **miroir délégation s44→s45 transposé à l'échange consenti** |
| 53 | `multi-enfants-bout-en-bout` — **RISQUE FONDATEUR R1 « N enfants ≥1 » DÉ-RISQUÉ DE BOUT EN BOUT : ISOLATION STRICTE par enfant sur TOUS les chemins d'écriture ET de lecture** (la série s44→s52 était en réalité MONO-ENFANT). `EnfantId` porté/propagé de bout en bout — période (`PeriodeSnapshot.EnfantId`), transfert SAISI (`Transfert.EnfantId`, s29, était dé-scopé), cycle de fond (`DefinirCycle` par enfant), slots « où », reprise/annulation (`AnnulerDelegation`) — **Option A** : `EnfantId` hérité de l'enfant courant du sélecteur s30, affiché **en LECTURE SEULE** dans les dialogs (« Pour : X (sélection courante) »), jamais un champ de choix. **Résolution STRICTEMENT filtrée par enfant** : `GrilleAgendaQuery.Projeter(ancre, vue, enfantId)` (aucun repli global/`''`) ; `CycleCourant(enfant)` NON-NULL lit UNIQUEMENT son cycle, **sans cycle propre → NEUTRE** (repli s13, plus jamais le cycle partagé legacy `''`). **Cloche/journal transverses par design (P3)** ; digest s50 filtré par enfant. Sélecteur d'enfant s30 câblé (aucune surface neuve) ; onglet Cycle config a son propre sélecteur d'enfant | @back Sc.1-6/10/12/14-15/17 (résolution isolée + délégation/échange ciblés sans écriture sur l'autre + pas de LWW entre enfants + digest par enfant + suppression/orphelin + écriture période/transfert/cycle/slot/reprise scopées + enfant sans cycle → NEUTRE, deux adaptateurs InMemory + Mongo durable) + @ihm Sc.7-9/11/13/16 (bascule sélecteur recharge le bon enfant + digest suit l'enfant/cloche transverse + temps réel 0-GET sur A sans toucher B + dialogs enfant lecture seule) — **17/17 ✅**, suite **920/920**, **gate G3 validé PO au 4ᵉ passage** (3 échecs = chemins d'écriture non scopés découverts UN PAR UN, audit exhaustif mené trop tard — cf. rétro s53), aucun retour produit — **R1 multi-enfants de bout en bout LIVRÉ**. CONSÉQUENCE UX : docs Mongo cycle legacy `EnfantId=''`/`undefined` désormais INERTES ; enfant sans cycle configuré par enfant → NEUTRE (le PO doit configurer le cycle de CHAQUE enfant) |
| 49 | `selection-de-plage-grille` — **sélection de plage par DRAG sur la grille (tranche 2 du palier 9, palier 9 COMPLET)** : drag J1→J3 → surbrillance `[min..max]` → dialog « Affecter une période » s06 **pré-remplie** → écriture sur l'intervalle → grille converge. **AUCUNE mécanique/DTO/store neuf** (réemploi strict s06), sélection **VOLATILE** sans persistance. Contrat : seuil clic vs plage, normalisation sens inverse `[min..max]`, bornage à la vue, Échap (port s33 document), Parent-gated. Mécanique = `pointerdown` ancre + `pointermove` **document** (`IEcouteurMouvementPointeur` + `elementFromPoint` → `data-date`) + `pointerup` **document** (`IEcouteurRelachementPointeur`) + `user-select:none` | @back Sc.1-2 (2 filets non-régression du chemin d'écriture `[J1..J3]` et `[J..J]`, deux adaptateurs) + @ihm Sc.3-8 (drag→dialog, clic simple inchangé, sens inverse, bornage vue, Échap, gating) menés RED→GREEN runtime + **nouveau projet E2E Playwright `tests/PlanningDeGarde.Web.E2E` HORS `.slnx`** (2 smoke tests Chromium réel, bUnit aveugle au geste natif) — **8/8 ✅**, suite **836/836**, gate G3 validé PO (**après 3 correctifs dont la cause réelle = build servi périmé**), aucun retour produit — **palier 9 calendrier navigable COMPLET** |
| 54 | `activites-recurrentes-config-foyer` — **« terminer tout ce qui est lié aux activités (slot) » (goal PO imposé, D2 + trou s31 soldés)** : (1) **vocabulaire IHM « slot » → « activité »** + référentiel « Activités » (s35) **re-renommé « Lieux »** ; (2) **routes REST NESTED sous l'enfant** `/api/slots* → /api/enfants/{enfantId}/activites*`, `/api/foyer/activites* → /api/foyer/lieux*` (aucune route `slots` ne survit, `EnfantId` corps→URL, scope défensif 404 hors-propriétaire, lot atomique libellés+routes+client HTTP+tests migrés en un commit) ; (3) **read model activités récurrentes PAR ENFANT** (query scopée `EnfantId`, jours + `LieuId`) ; (4) **récurrence MULTI-JOURS** (set de jours, refus set vide, une occurrence/jour) ; (5) **éditer une série** en place (`PUT`, `EnfantId` préservé, refus ordonnés) ; (6) **config foyer PAR ENFANT** (onglet « Activités récurrentes », sélecteur enfant, liste + créer/éditer/**supprimer** — comble le trou re-signalé s31, gating Invité) ; (7) **exclusion vacances scolaires** (plages d'exclusion par série, `…/exclusions`, projection saute l'intervalle) ; (8) **exceptions d'occurrence « cette occurrence / toute la série »** (`…/occurrences/{a}/{m}/{j}`, idempotent, invite IHM). Isolation par enfant (s53) tenue sur chaque chemin | @back Sc.2-5/7/9 (routes nested + scope défensif + query scopée enfant + multi-jours/refus set vide + édition série `EnfantId` préservé + exclusion vacances + exception par date idempotente) + @ihm Sc.1/6/8/10 (libellés « activité »/« Lieux » lot atomique + config foyer par enfant créer/éditer/supprimer + saisie plages de vacances + choix portée occurrence/série) — **10/10 ✅**, suite **945/945 en série**, **gate G3 validé PO** — **D2 (récurrence multi-jours + config foyer) et trou s31 (suppression récurrent IHM + occurrence vs série) SOLDÉS**. *Retours PO au gate : « plein de retours » traités séparément avec l'architecte (hors pipeline) — passe à planifier (backlog)* |

> **Refacto technique HORS pipeline (PR #21, avant s10)** : adaptateurs de droite par techno,
> `PlanningDeGarde.SignalR` (adapter de gauche), rangement par type, pipeline allégé, `test-count.ps1`.
> **Conteneurisation Docker (avant s15)** : `docker-compose.yml` (mongo + mongo-express + build + api + web),
> docs `README-docker.md`. Origine PO post-s05.

---

## Besoins d'épic livrés (✅)

### É1 — Fondation données & modèle foyer
- Acteurs « autres » éditables (nounou, grands-parents…) — s08-s09-s13 (CRUD complet)
- Cycle récurrent multi-semaines éditable + durable Mongo — s10 (éditable) + s15 (durable)

### É2 — Modèle & configuration d'acteurs
- Édition des acteurs en VOLATILE (mémoire/session, grille relue) — s08
- Ajout d'acteurs + persistance Mongo bornée config foyer — s09
- Édition des acteurs « autres » (ajout/édition/suppression) — s08 + s09 + s13
- « Parent A/B » fictifs supprimés, acteurs réels partout (id stable) — s19
- Rôle affectable à un acteur, borné au référentiel — s21
- Parents créent/gèrent les rôles (référentiel éditable, Mongo) — s21
- Seuls les rôles définis sont utilisés, invariant hors-résolution — s21
- Admin du foyer obligatoirement un parent (invariant Domain, cardinal non borné) — s22
- Compte utilisateur ↔ acteur (fondation identité 1-1, Mongo) — s22
- Refonte config foyer en onglets par thème — s20

### É3 — Fondations techniques (architecture & API)
- Controllers HTTP exposant les commandes d'écriture (adaptateur de gauche) — s04
- Hôte d'API détachable (back démarrable seul) — s05
- Front Blazor Server → WASM consommant l'API — s05
- SignalR cantonné au push lecture seule — s04 + s05
- API explorable OpenAPI + UI Scalar — s05
- CORS origine du front autorisée — s05
- Ports & adaptateurs (hexagonal), tout le domaine durable Mongo, DI `Foyer:Persistance` — s04 + s15

### É4 — Calendrier & grille de lecture
- Grille agenda 5 semaines lecture seule — s03
- Positionnement des slots dans les cases — s03
- Code couleur par personne — s03
- Slots empilés dans l'ordre horaire — s03
- Fenêtre stricte 35 jours — s03
- Navigation mois + vues Semaine/4-sem/Mois + « Aujourd'hui » — s15
- Sélection de plage de cases (clic début+fin) — s15

### É5 — Lisibilité & identité visuelle
- Libellé + nom du responsable dans les cases — s07
- Légende des couleurs (dédoublonnée, masquée si vide) — s07
- Thème visuel en accord avec le domaine — s07
- Nom long lisible (troncature + survol) — s07
- **Sprint de design — refonte visuelle complète « Studio »** — s26
- **Thème sombre + toggle (préférence persistée `localStorage`)** — s26

### É6 — Créneaux & slots de localisation
- Poser un slot (enfant → lieu, début/fin, date) — s01 + s02 + s04
- Rejet durée nulle — s01
- Slot franchissant minuit (`JoursCouverts`) — s01 + s18
- Rejet lieu inexistant — s01 + s02 + s04
- Signalement de chevauchement (accepté + avertissement) — s01
- Droits : seul Parent crée/édite — s01
- Poser un slot en contexte via dialog — s11
- Suppression d'un slot sur une journée — s18

### É7 — Périodes de garde & responsabilité récurrente
- Affecter une période à un responsable — s01 + s02 + s04
- Rejet responsable requis — s01
- Bornes de période paramétrables — s01
- Édition concurrente — rejet sur état périmé — s01
- Suppression de période (dialog) — s16
- Édition de période (re-borner / réaffecter) — s17
- Affecter période en contexte via dialog — s11
- Responsabilité de fond (cycle, parité ISO), durable Mongo — s10 + s15

### É8 — Transferts & bascule de responsabilité
- Définir un transfert (date, dépositaire, récupérateur, lieu, heure) — s01 + s02 + s04
- Rejet transfert incomplet — s01
- Transfert en contexte via dialog + retrait page dédiée — s12

### É9 — Notifications & événements à venir
- Notifications in-app push temps réel (SignalR, lecture seule, infra) — s01

### É10 — Authentification & accès utilisateurs
- Impersonation bornée LECTURE — s14
- Page de connexion dédiée = landing (`/connexion`, `/` redirige) — s24
- Accès config foyer depuis un menu utilisateur — s24
- Facteur mot de passe local (PBKDF2, refus neutre anti-énumération) — s25
- Création de compte en libre-service (logique) — s25
- Récupération de mot de passe par jeton / email (logique par doublure de port) — s25
- Protection d'accès aux routes (garde non authentifié) — s25
- Bug rôle ≠ acteur du compte connecté — CORRIGÉ s25
- OAuth Google / Microsoft / Apple (logique, boutons IHM) — s25
- Fondation identité compte ↔ acteur (Mongo) — s22
- Auth tr.2a : connexion locale + session serveur + logout — s23
- Acteur par défaut config = utilisateur connecté (`ResoudreActeurParDefautQuery`) — s23
- Activation de compte Inactif → Actif — s24
- Admin du foyer obligatoirement parent (invariant Domain) — s22
- Gestion des sessions utilisateur (session serveur + logout) — s23

### É12 — Écriture en contexte (recâblage post-API)
- Dialogs d'écriture (slot + période) depuis les cases — s11
- Dialog « Définir un transfert » + retrait page dédiée — s12
- Suppression de période depuis dialog — s16
- Édition de période depuis dialog — s17
- Recâblage de l'écriture via API HTTP — s05
- Rafraîchissement immédiat : la saisie réapparaît — s06

---

## Paliers livrés (✅)

| Palier | Besoin | Sprint |
|-------:|--------|:------:|
| 1 | Fermeture de la fondation — hôte d'API détachable + UI Scalar + CORS | s05 |
| 2 | Saisie visible — réapparition bonne date + couleur du parent | s06 |
| 3 | Lisibilité & thème — nom + légende + thème métier | s07 |
| 4 | Config foyer · édition des acteurs (VOLATILE) | s08 |
| 5 | Config foyer PERSISTANTE — ajout d'acteurs + Mongo borné | s09 |
| 6 | Récurrence des périodes (cycle de fond, parité ISO, en mémoire) | s10 |
| 7 | Écriture en contexte (dialogs), écrans dédiés retirés | s11 + s12 |
| 8 | CRUD acteurs — suppression + impersonation bornée lecture | s13 + s14 |
| 9 | Calendrier navigable + sélection de plage | s15 |
| 14 | Persistance réelle de tout le domaine + démarrage sans seed | s15 |

---

## Dettes refermées

- **Aucune édition/suppression de période depuis l'IHM** — refermée : suppression s16 + édition s17.
- **Saisies invisibles à l'écran** — éteinte s06 (date défaut `IDateTimeProvider` + mapping id stable + seed).
- **Cycle multi-semaines non affiché/éditable** — éteint s10 (affiché grille+légende, éditable, en mémoire).
- **Dropdown « Acteur du foyer » périmée au renommage** — résorbée s13 (store vivant `Foyer.ActeursEditables`).
- **Sélecteur d'édition config encore sur `Foyer.ActeursEditables`** — résolue s20 (`IEnumerationActeursFoyer` unifié, `Foyer.ActeursEditables` retirée).
- **Dernière saisie hors-contexte restante** — éteinte s12 (page/route/lien `definir-transfert` supprimés, épic É12 refermé).
- **Faux sentiment de progrès (2 sprints structurels s04/s05)** — résorbé s06 (palier 2 a rendu la main à l'usage).
