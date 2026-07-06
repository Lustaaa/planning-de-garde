# Product backlog — FAIT (planning-de-garde)

> **Archive du livré.** Miroir de [`BACKLOG.md`](BACKLOG.md) (ce qui reste) : ce fichier
> consigne **tout ce qui est livré et clôturé** — sprints, besoins d'épic ✅, paliers ✅ et
> dettes refermées. Source de vérité du *déjà fait* ; le *reste à faire* vit dans `BACKLOG.md`.
> Le *pourquoi* vit dans la spec vivante [`docs/specs/`](specs/index.md).
>
> **26 sprints livrés · suite complète 458/458 verte.** Fichiers de sprint clos archivés sous
> [`docs/_archive/sprints/`](_archive/sprints/).

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
