# Product backlog — planning-de-garde

> **Backlog produit permanent** (artefact SCRUM). Deux lectures du même produit :
> une vue **par épic (fonctionnalité)** pour regrouper ce qui est lié et préparer le
> découpage des sprints, et une vue **par palier (séquence de livraison)** pour le
> calendrier d'un coup d'œil. Source de vérité du *quoi/quand* ; le *pourquoi* vit dans
> la spec vivante éclatée [`docs/specs/`](specs/index.md).
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
| 14 | `impersonation-bornee` — **impersonation bornée lecture seule** : incarner un acteur déjà déclaré (bandeau « Vous incarnez X »), vue selon le rôle de l'**identité effective** (gating règle 9 piloté par l'incarné), retour identité réelle, retour AUTO sur suppression concurrente (repli règle 6 + SignalR), pas d'écriture « au nom de », **durcissement complet du gating config** ; type d'acteur read-only depuis le seed, zéro persistance neuve | ✅ fait | `SessionPlanning` identité réelle/effective (`Incarner`/`RevenirIdentiteReelle`, `EstParent` dérivé de l'effective) + `TypeActeur` surfacé read-only via `IEnumerationActeursFoyer.TypeDe` + bandeau/sélecteur dans `PlanningPartage` + gating effectif grille & config (6 scénarios @vert runtime, 214 verts) — **palier 8 tranche 2 (impersonation lecture) refermé, palier 8 clos côté usage** |
| 15 | `calendrier-navigable` — **calendrier navigable** (navigation ±semaine, sélecteur de vues **Semaine / 4 semaines glissantes (défaut) / Mois**, retour « Aujourd'hui », **affectation par plage de cases** clic début+fin gardée Parent, échec navigation → fenêtre préservée + message clair, gating Invité) **+ absorption du palier 14 : persistance Mongo de TOUT le domaine** (slots/périodes/transferts/cycle) **+ démarrage runtime SANS seed** (app vide au 1er lancement, durable ensuite) | ✅ fait | `SessionPlanning` ancre+vue (session, non persistée) + nav préc./suiv./Aujourd'hui + `GrilleAgendaQuery.Projeter(ancre, vue)` (défaut 28 j) + endpoint lecture `?vue=` + sélection de plage → `AffecterPeriode` réutilisé (aucun handler neuf) + 4 adaptateurs `AdapterDroite.Mongo` (slots/périodes/transferts/cycle) derrière ports existants + DI commutant **tout** le domaine droite via `Foyer:Persistance` (Mongo runtime / InMemory test) + retrait seed runtime (`AmorcerDonneesDemo` + seed-once acteurs) (9 scénarios @vert runtime, 234 verts, pivot Mongo réel) — **paliers 9 (calendrier navigable) ET 14 (persistance réelle du domaine) refermés. Révision PO hors process : borne anti-cliquet règle 30 levée ; asymétrie seed assumée (Mongo jamais seedé / InMemory gardé)** |
| 16 | `supprimer-editer-periode` — **suppression de période depuis l'IHM** (4ᵉ usage du menu clic-case → dialog listant les périodes couvrant la date → supprimer) sur **store Mongo réel** ; **repli surcharge > fond > neutre sans nom fantôme** à la re-résolution, **idempotence** (absent/déjà supprimé = no-op qui réussit), accusé non bloquant « Période supprimée », gating Invité, échec API (dialog reste ouverte), temps réel SignalR. Comble la dette « trou fonctionnel » (retours s02 #6 · s03). **Édition de période (re-borner/réaffecter) hors scope (tranche suivante)** | ✅ fait | `SupprimerPeriodeHandler` (idempotent) + `PeriodesDuJourQuery` (lecture pour la dialog) + port `Supprimer(periodeId)` sur le dépôt de périodes (InMemory + `AdapterDroite.Mongo`) + endpoint `POST /api/canal/supprimer-periode` (diffusion sur succès) + `SupprimerPeriodeDialog` (4ᵉ usage menu clic-case, accusé à part, gating/échec/temps réel) ; repli réutilise la priorité surcharge>fond>neutre acquise au palier 6 (10 scénarios @vert runtime, 246 verts, pivot Mongo réel) — **dette « édition/suppression de période depuis l'IHM » à demi refermée (suppression livrée)** |
| 17 | `editer-periode` — **édition de période depuis l'IHM** (5ᵉ usage du menu clic-case → bouton « Éditer » par ligne → formulaire pré-rempli → **re-borner et/ou réaffecter**) sur **store Mongo réel** ; clé = **identifiant stable** ; re-résolution **surcharge > fond > neutre sans nom fantôme** (portion libérée → fond/neutre, portion couverte → nouveau responsable), invariant **fin > début**, **rejet sur état périmé** (concurrence, agrégat période), accusé non bloquant « Période modifiée », gating Invité, échec API (dialog reste ouverte), temps réel SignalR | ✅ fait | `EditerPeriodeCommand`/`EditerPeriodeHandler` (Result succès/échec, rejet bornes/concurrence) + méthode `Editer(...)` sur le dépôt de périodes (InMemory + `AdapterDroite.Mongo`) + endpoint `POST /api/canal/editer-periode` (diffusion sur succès) + 5ᵉ usage menu clic-case (formulaire pré-rempli bornes+responsable, accusé à part, annulation/gating/échec/temps réel) ; re-résolution réutilise priorité surcharge>fond>neutre (palier 6) + filtre `Resolvable()` (s13), aucune règle de résolution neuve (11 scénarios @vert runtime, 258 verts, pivot Mongo réel) — **dette « édition/suppression de période depuis l'IHM » ENTIÈREMENT refermée (suppression s16 + édition s17)** |
| 18 | `supprimer-slot` — **suppression d'un slot sur une journée depuis l'IHM** (6ᵉ usage du menu clic-case → dialog listant les slots couvrant la date → supprimer) sur **store Mongo réel** ; handler **idempotent** (absent/déjà supprimé = no-op qui réussit), `SlotsDuJourQuery` (lecture pour la dialog, slot à cheval sur minuit listé sur les deux jours), accusé non bloquant « Slot supprimé », gating Invité, échec API (dialog reste ouverte), temps réel SignalR. **Aucune règle de résolution ouverte** (un slot est une localisation, pas une responsabilité). Comble le **retour s17 #1**, miroir de la suppression de période (s16) | ✅ fait | `SupprimerSlotCommand`/`SupprimerSlotHandler` (idempotent) + `SlotsDuJourQuery` + port `Supprimer(slotId)` sur le dépôt de slots (InMemory + `AdapterDroite.Mongo`) + endpoint `POST /api/canal/supprimer-slot` (diffusion sur succès) + `SupprimerSlotDialog` (6ᵉ usage menu clic-case, accusé à part, gating/échec/temps réel) ; **découverte** : un slot franchissant minuit est rendu sur **tous les jours calendaires qu'il couvre** (`JoursCouverts`), sa suppression l'efface de chacun (10 scénarios @vert runtime, 270 verts, pivot Mongo réel) — **épic É6 : suppression de slot livrée** |
| 19 | `acteurs-reels-partout` — **retrait des « Parent A / Parent B » fictifs, acteurs réels partout** : sélecteurs (dialogs), grille et légende lisent **exclusivement** le store vivant des acteurs déclarés (id stable), jamais un libellé en dur ; store vide → grille neutre + légende vide + message « Aucun acteur, ajoutez-en. », **zéro** fantôme ; référence orpheline → repli **surcharge > fond > neutre sans nom fantôme** (filtre `Resolvable()` s13) ; runtime Mongo démarre vide, **aucune constante de domaine** n'expose « Parent A/B », fixtures de test renommées (asymétrie seed s15 préservée) ; temps réel SignalR sans fantôme. **Aucun handler d'écriture neuf** (s'appuie sur le CRUD acteurs s08/s09/s13) | ✅ fait | Résolution grille/légende via store vivant (`IEnumerationActeursFoyer`/`IReferentielResponsables`/`IPaletteCouleurs`) sur **id stable** + repli surcharge>fond>neutre (palier 6 + `Resolvable()` s13) + libellés fictifs purgés du domaine + fixtures InMemory renommées (neutres, id stables) + `PlanningPartage` **source unique** des acteurs passés aux dialogs + message store-vide « Aucun acteur, ajoutez-en. » + propagation SignalR (7 scénarios @vert runtime, 281 verts, pivot Mongo réel) — **retour s17 #2 livré, épic É2 (acteurs réels partout) refermé** |
| 20 | `config-foyer-onglets` — **écran config réorganisé en 3 onglets par thème** (Acteurs / Période de garde / Slot récurrent), **Acteurs actif par défaut**, contenu existant réparti (rien perdu/dupliqué), cloisonné par rendu conditionnel ; CRUD acteurs et cycle de fond **iso-fonctionnels** (aucun handler neuf) ; **gating identité effective préservé par onglet** ; onglet Slot récurrent = **placeholder réservé** (aucune écriture/persistance) **+ convergence du dernier sélecteur** : le sélecteur d'édition config lit désormais le **store vivant unifié** `IEnumerationActeursFoyer` (`Foyer.ActeursEditables` **retirée**), un **seul chemin de lecture** du référentiel ; écran config **abonné au hub SignalR lecture** (ré-énumération temps réel, propagation d'un 2ᵉ écran sans rechargement) | ✅ fait | Onglets par rendu conditionnel dans `ConfigurationFoyer` (Acteurs par défaut) + sélecteur d'édition config sur `IEnumerationActeursFoyer` (id stable, source unique config↔dialogs↔grille↔légende) + `Foyer.ActeursEditables` supprimée + abonnement SignalR de l'écran config (ré-énumération) + placeholder « à venir » onglet Slot récurrent + gating effectif par onglet (non-régression s14) (7 scénarios @vert runtime, 288 verts, pivot Mongo réel) — **retour s17 #6 (config en onglets) livré + dette de cohérence s19 (convergence du dernier sélecteur) RÉSOLUE, épic É2 avancé** |
| 21 | `modele-de-roles-editable` — **référentiel de rôles éditable par le parent** (créer/renommer/supprimer, id stable opaque, **persisté Mongo** comme la config foyer) ; rejet **sans écriture** libellé vide/doublon ; **rôle affectable à un acteur borné au référentiel** (id hors référentiel rejeté sans écriture) ; **acteur sans rôle = neutre assumé** (`RoleDe=null`) ; **suppression d'un rôle → acteurs porteurs retombent sans rôle** (repli neutre) puis rôle retiré ; gestion+affectation dans l'**onglet Acteurs Parent-gated** ; **temps réel SignalR** (2 écrans convergent) ; **invariant : le rôle n'intervient PAS dans la résolution grille/légende** (caractéristique d'acteur, pas responsabilité) | ✅ fait | Référentiel de rôles (Domain, id stable opaque, invariants libellé non-vide/unique) + persistance Mongo (adaptateur de droite, ports) + handlers créer/renommer/supprimer rôle + affectation `RoleDe` bornée au référentiel + repli neutre à la suppression + onglet Acteurs (gestion+affectation, Parent-gated) + propagation SignalR (11 scénarios @vert runtime, 317 verts, pivot Mongo réel) — **retours s17 #3/#4/#5 (modèle de rôles) LIVRÉS, épic É2 avancé. Friction rétro s21 : régression `*TempsReel*` étiquetée « flake » à tort (garde-fou triage durci, cf. JOURNAL-METHODE)** |

> **Refacto technique HORS pipeline (PR #21, avant s10) : faite** — adaptateurs de droite par techno, `PlanningDeGarde.SignalR` (adapter de gauche), rangement par type, pipeline allégé, outil `test-count.ps1`. Critère de sortie 161/161 tenu.

## En cours

| Sprint | Sujet | Palier (spec v15) | Statut |
|-------:|-------|-------------------|:------:|
| — | *(aucun sprint en cours ; s21 `modele-de-roles-editable` livré et clôturé — gate G3 validé PO, 11/11, 317/317. PO satisfait « vraiment pas mal » ; 3 nouveaux retours : **auth URGENT**, cohérence config→planning, sprint de design)* | 9 + 14 livrés (s15) ; période suppr./édit. (s16/s17) ; suppr. slot (s18) ; acteurs réels partout (s19) ; config en onglets (s20) ; modèle de rôles (s21) | — |

## Prochains sprints envisagés

> **Décision PO (clôture s14, porte G2)** : prochain sujet **make-gherkin** = **calendrier navigable** (É4+É7) : navigation passé/futur (semaines préc./suiv., vues prédéfinies) + amorce sélection de plage de cases pour définir une période. Suite d'usage naturelle après la tranche acteurs (palier 8 clos). **Steer** (orientation, non figé) : plus petit incrément = navigation seule ; la sélection de plage est un sujet plein cuttable en tranche 2 si ça déborde ~2h ; périmètre exact tranché au make-gherkin (CP option 1, corollaire de découpe). Retours produit s14 VIDE (goal 6/6 atteint) → pilotage au catalogue.

| Rang | Sujet envisagé | Épics | Pourquoi maintenant |
|-----:|----------------|-------|---------------------|
| ~~+1 (P1)~~ ✅ | ~~**Calendrier navigable** + amorce sélection de plage~~ — **LIVRÉ s15** (avec le palier 14 absorbé : persistance Mongo du domaine + démarrage vide). Variantes plage (drag riche, plage vide/chevauchement, à cheval vue/mois) **reportées tranche 2** | É4, É7 | livré |
| ~~+1 (P1)~~ ✅ | ~~**Édition de période depuis la dialog** (re-borner / réaffecter)~~ — **LIVRÉ s17** : 5ᵉ usage menu clic-case, formulaire pré-rempli, rejet sur état périmé, re-résolution surcharge>fond>neutre, gating/échec/temps réel (11/11 vert, 258 verts). Dette « édition/suppression de période depuis l'IHM » **entièrement refermée** | É7, É12 | livré |
| ~~+1 (P1)~~ ✅ | ~~**Modèle de rôles éditable** (référentiel créable + affectable à un acteur, borné)~~ — **LIVRÉ s21** : CRUD rôles persisté Mongo, affectation bornée au référentiel, repli neutre à la suppression, onglet Acteurs Parent-gated, temps réel, invariant hors-résolution (11/11 vert, 317 verts) | É2 | livré |
| **+1 (P1 — URGENT PO s21)** | **Authentification & utilisateurs** — page de connexion **custom** + **OAuth Google / Apple / Microsoft** ; comptes utilisateurs réels ; **acteur par défaut config = utilisateur connecté** ; **admin du foyer obligatoirement parent** (ou les deux). Le PO monte l'auth **explicitement URGENT** au s21 (le plus gros palier — landing, OAuth, sessions, droits par rôle après prise en main) | É10, É2, É5 | **URGENT PO** — lève le risque d'adoption du second parent et débloque « acteur par défaut = moi » + droits par rôle (le modèle de rôles s21 attend son couplage droits) ; palier lourd, à cadrer/découper au make-gherkin (auth avant droits par rôle) |
| **+1 (P1 — PO s21)** | **Sprint de design — refonte visuelle complète** — le PO juge le design actuel « une catastrophe » ; refonte d'ensemble (pas un thème additif). Sujet transverse à cadrer (direction visuelle, système de composants, thème) | É5 | Demande PO directe s21 ; l'app est fonctionnellement riche mais la surface visuelle freine l'usage/adoption ; à arbitrer G2 contre l'auth |
| **+2 (P1 — priorité maintenue, near-miss s21)** | **Rétrofit complet du garde *TempsReel* SignalR** — cibler la **convergence SignalR multi-clients** (distincte de la course d'énumération déjà gardée s13). **VRAI flake résiduel = intermittent sous charge parallèle** (`FrontWasm*TempsReel*` sur tests SignalR multi-clients, ~1/3 runs, vert isolé) — **dette de test réelle**, aucune cause `src/`. **NB s21 (near-miss)** : une **régression déterministe** (`FrontWasmConfigSupprimerActeurTempsReelTests`, rouge **3/3 en isolation** — `RechargerRoles()` diffusait contre l'accusé de suppression) a été **étiquetée « flake » à tort** par dev-team et a **failli passer le gate** (rattrapée au G3, corrigée commit `37bced4`). Garde-fou triage **durci** (rétro s21 : re-run **en isolation x2-3**, N/N rouge = régression, jamais « flake ») → le passe-droit flake ne masque plus une régression. Reste à traiter : le **vrai** flake intermittent multi-clients | É3 | Déverrouille l'édition concurrente sans driver une fondation temps-réel instable ; **priorité P1 maintenue** — le flake intermittent croît à chaque nouveau client SignalR ; le near-miss s21 confirme le coût réel (à arbitrer G2 contre auth URGENT et design) |
| +3 | **Convergence `EditerPeriodeHandler` / `ModifierPeriodeHandler`** — deux handlers de mutation de période coexistent (le second legacy s02, même port d'écriture + même modèle de concurrence sur l'agrégat période) ; à converger pour un seul chemin d'écriture — **dette de code** (DDD : un seul modèle de concurrence par agrégat) | É7 | Évite la dérive de deux chemins d'édition divergents ; ménage hygiénique post-s17 |

> **Retours produit s17 (7 items, consommés à la clôture)** — replacés dans leurs épics : *suppression
> d'un slot sur une journée* → **É6** *(LIVRÉ s18)* ; *« Parent A / Parent B » → acteurs partout* (suppression des
> acteurs fictifs, usage systématique des acteurs réels) → **É1/É2** *(LIVRÉ s19)* ; *rôle affectable à un acteur
> (Nounou / Grand-parent)* + *parents créent des rôles* + *seuls les rôles définis sont utilisés dans
> l'app* → **É2** (modèle de rôles) ; *refonte config foyer en onglets par thème (Acteurs / Période de
> garde / Slot récurrent), proposition attendue* → **É2** ; *transferts matérialisés sur le planning
> (case bicolore, séparation diagonale)* → **É8/É5**. Aucun bug : 6 évolutions/nouveaux besoins + 0
> question. Détails dans les tableaux d'épic ci-dessous.
> **Retours produit s21 (3 items, consommés à la clôture)** — PO satisfait (« vraiment pas mal »),
> aucun bug : 3 nouveaux besoins. *Auth : page de connexion custom + OAuth Google/Apple/Microsoft,
> acteur par défaut config = utilisateur connecté, admin du foyer obligatoirement parent* → **É10**
> (**marqué URGENT par le PO**, monté +1) ; *cohérence config foyer → planning (le configuré doit être
> effectif)* → dette de cohérence (ci-dessous) ; *sprint de design — refonte visuelle complète (« c'est
> une catastrophe »)* → **É5** (monté +1). Détails dans les tableaux d'épic.
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
| Cycle récurrent multi-semaines **éditable** + **durable Mongo** (s15) | ✅ | s10 (éditable) + s15 (durable) / Paliers 6+14 | spec règle 11 · besoins s10 |
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
| **« Parent A / Parent B » fictifs supprimés — acteurs réels partout** : éliminer les acteurs de démo (Parent A/B) du domaine ET de l'IHM ; tous les sélecteurs, cases, légendes, formulaires consomment les **acteurs déclarés** (id stable). Recoupe l'asymétrie seed s15 (InMemory seedé / Mongo vide) | ✅ | s19 / Palier 5 | retours s17 (#2) |
| **Rôle affectable à un acteur** (ex. Nounou, Grand-parent) — rôle borné au référentiel, id hors référentiel rejeté sans écriture, acteur sans rôle = neutre (`RoleDe=null`) | ✅ | s21 / Palier 5 | retours s17 (#3) |
| **Les parents créent/gèrent les rôles** (référentiel de rôles éditable : créer/renommer/supprimer, id stable opaque, persisté Mongo, rejet libellé vide/doublon, suppression → repli neutre des porteurs) | ✅ | s21 / Palier 5 | retours s17 (#4) |
| **Seuls les rôles définis sont utilisés dans l'app** (le référentiel borne les valeurs affectables ; pas de rôle en dur ; **invariant** : le rôle n'intervient PAS dans la résolution grille/légende — caractéristique d'acteur, pas responsabilité) | ✅ | s21 / Palier 5 | retours s17 (#5) |
| **URGENT — Acteur par défaut config = utilisateur connecté** (une fois l'auth en place, l'acteur sélectionné par défaut dans la config foyer est l'utilisateur authentifié) | ⬜ | Palier 13 (auth) | retours s21 (URGENT) |
| **URGENT — L'admin du foyer est obligatoirement un parent** (ou les deux parents quand les deux sont utilisateurs) | ⬜ | Palier 13 (auth) | retours s21 (URGENT) |
| **Cohérence config foyer → planning** : ce qui est configuré dans le foyer doit être **effectif pour le planning** (config → planning appliquée de bout en bout) | ⬜ | à séquencer | retours s21 |
| **Refonte de l'écran config foyer en onglets par thème** (Acteurs / Période de garde / Slot récurrent), **Acteurs actif par défaut**, contenu cloisonné par rendu conditionnel, gating par onglet, onglet Slot récurrent réservé (placeholder) | ✅ | s20 / Palier 10 | retours s17 (#6) |

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
| Ports & adaptateurs visibles (hexagonal : gauche/droite/domaine) | ✅ | s04 (gauche) · droite **tout le domaine durable Mongo** (config foyer s09, puis slots/périodes/transferts/cycle s15), DI commutant Mongo/InMemory via `Foyer:Persistance` | retours s03 (#10) · s15 |

### Épic 4 — Calendrier & grille de lecture
*Calendrier navigable (semaine + 4 semaines) lisible d'un coup d'œil.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Grille agenda 5 semaines (35 jours) en lecture seule | ✅ | s03 | spec p3 · retours s02 (#3-5) |
| Positionnement des slots dans les cases jour/horaire | ✅ | s03 | spec règles 12/114 |
| Code couleur par personne sur les cases-jour | ✅ | s03 | spec règles 14/158 |
| Slots empilés dans l'ordre horaire | ✅ | s03 | scénario 5 s03 |
| Fenêtre stricte 35 jours (bornes inf./sup.) | ✅ | s03 | scénarios 1/7 s03 |
| Navigation dans le mois (semaines précédente/suivante) + vues prédéfinies Semaine/4-sem/Mois + retour « Aujourd'hui » | ✅ | s15 / Palier 9 | spec p3 · retours s02 (#3) |
| Sélection de plage de cases pour affecter une période (clic début+fin ; drag riche tranche 2) | ✅ | s15 / Palier 9 | retours s02/s03 |

### Épic 5 — Lisibilité & identité visuelle
*Rendre la responsabilité explicite (pas seulement une teinte) et habiller l'app.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Libellé + nom du responsable dans les cases (pas que la teinte) | ✅ | s07 / Palier 3 | retours s03 (#3) · spec règle 16 |
| Légende des couleurs (mapping acteur → couleur, dédoublonnée, masquée si vide) | ✅ | s07 / Palier 3 | spec règle 16 · retours s03 (#3) |
| Thème visuel en accord avec le domaine (garde d'enfants) | ✅ | s07 / Palier 3 | retours s01/s02/s03 (« j'aime pas le thème ») · spec règle 20 |
| Nom long lisible (troncature + nom complet au survol) | ✅ | s07 / Palier 3 | spec règle 16 (dérivé) |
| **Sprint de design dédié — refonte visuelle complète de l'app** (le PO juge le design « une catastrophe » ; refonte d'ensemble, pas un thème additif) | ⬜ | à séquencer (P1) | retours s21 |
| Thème sombre + toggle (avec persistance de la préférence) | ⬜ | backlog (additif) | retours s07 (idée) · spec v08 règle 21 |
| Personnalisation des couleurs par utilisateur authentifié | ⬜ | Palier 13 (auth) | spec règle 16 |

### Épic 6 — Créneaux & slots de localisation
*Poser et gérer les slots (où est l'enfant) : création, validation, affichage.*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Poser un slot (enfant → lieu, début/fin, date) | ✅ | s01 + s02 + s04 (API) | spec règles 8/112 |
| Rejet : durée nulle interdite | ✅ | s01 | scénario 2 s01 |
| Slot franchissant minuit (rendu sur **tous les jours calendaires couverts**, `JoursCouverts`) | ✅ | s01 + s18 (explicité) | scénario 3 s01 · découverte s18 |
| Rejet : lieu inexistant | ✅ | s01 + s02 + s04 (API) | scénario 4 s01 |
| Signalement de chevauchement (création acceptée + avertissement) | ✅ | s01 | scénario 5 s01 |
| Droits : seul Parent crée/édite les slots | ✅ | s01 | spec règle 7 |
| Poser un slot en contexte via dialog (depuis une case) | ✅ | s11 / Palier 7 | retours s02 (#10) · spec p3 |
| **Suppression d'un slot sur une journée** (6ᵉ usage menu clic-case → dialog liste → supprimer ; idempotente, store Mongo réel, gating/échec/temps réel ; aucune règle de résolution ouverte) | ✅ | s18 | retours s17 (#1) |
| **Slot imbriqué** — un slot peut en contenir un autre (ex. enfant chez mamie **et** doit aller à son cours de natation) | ⬜ | à séquencer | retours s07 (idée) |

### Épic 7 — Périodes de garde & responsabilité récurrente
*Modéliser la responsabilité de garde sur une période (distincte des slots).*

| Besoin | Statut | Sprint/Palier | Origine |
|--------|:------:|---------------|---------|
| Affecter une période à un responsable | ✅ | s01 + s02 + s04 (API) | spec règles 5/8/118 |
| Rejet : responsable requis | ✅ | s01 | scénario 8 s01 |
| Bornes de période paramétrables | ✅ | s01 | scénario 9 s01 |
| Édition concurrente — rejet sur état périmé | ✅ | s01 | scénario 10 s01 |
| Suppression de période (depuis dialog) | ✅ | s16 / Palier 7 | retours s02 (#6) · retours s03 (trou) |
| **Édition de période (re-borner / réaffecter le responsable depuis la dialog)** | ✅ | s17 / Palier 7 | retours s02 (#6) · titre goal s16 (hors scope G2) |
| Affecter période en contexte via dialog | ✅ | s11 / Palier 7 | retours s02 (#7) · spec p3 |
| Responsabilité de fond déclarée en config foyer (le cycle, alternance parité ISO) — **durable Mongo depuis s15** | ✅ | s10 (mémoire) + s15 (durable) / Paliers 6+14 | spec règles 5/11 · besoins s07/s08 |
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
| **Transfert matérialisé sur le planning** : case **bicolore** (deux responsables) avec **séparation en diagonale** (départ → arrivée visibles d'un coup d'œil) — rendu lisibilité, recoupe É5 | ⬜ | à séquencer | retours s17 (#7) |
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
| **Impersonation bornée LECTURE** (incarner un acteur déjà déclaré, convenance admin, vue selon rôle effectif, retour auto sur suppression concurrente — PAS l'auth réelle) | ✅ | s14 / Palier 8 tr.2 | spec v15 règle 8 · G2 PO s13 |
| Landing page (identifie le foyer, invite à s'authentifier) | ⬜ | Palier 13 | retours s01 (#2) · spec p8 |
| **URGENT — Page de connexion custom + OAuth Google / Apple / Microsoft** (le PO monte l'auth en tête ; comptes utilisateurs réels) | ⬜ | Palier 13 (P1) | retours s21 (**URGENT**) |
| **URGENT — Acteur par défaut config = utilisateur connecté** (défaut de sélection = l'utilisateur authentifié) | ⬜ | Palier 13 (P1) | retours s21 (**URGENT**) |
| **URGENT — Admin du foyer obligatoirement parent** (ou les deux parents quand les deux sont utilisateurs) | ⬜ | Palier 13 (P1) | retours s21 (**URGENT**) |
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
| Suppression de période depuis dialog | ✅ | s16 / Palier 7 | retours s02 (#6) · retours s03 (trou) |
| Édition de période depuis dialog (re-borner / réaffecter) | ✅ | s17 / Palier 7 | retours s02 (#6) · titre goal s16 (hors scope) |
| Recâblage de l'écriture via API HTTP (au lieu du DI direct) | ✅ | s05 (poser/affecter/transfert via API distante WASM) | retours s03 (#5) · spec p1 |
| Rafraîchissement immédiat : la saisie réapparaît dans la grille | ✅ | s06 / Palier 2 | retours s03 (#5, bug runtime) |

---

## À faire (paliers de la spec vivante v15)

> Vue de séquencement (ordre de livraison). Chaque palier agrège des besoins des épics.
> Numérotation alignée sur la **séquence de livraison de v15** : palier 7
> (écriture-en-contexte) **livré complet** ; **palier 8 = CRUD acteurs** — suppression
> (s13) **+ impersonation lecture (s14) LIVRÉS, palier clos côté usage** ;
> **palier 9 = Calendrier navigable = PROCHAIN SUJET** ; paliers suivants décalés d'un cran. Les sujets
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
| 8 | **CRUD acteurs** — suppression (Delete, repli orphelins) **✅ s13** + **impersonation bornée lecture** (incarner, vue selon rôle effectif, retour auto, gating config durci) **✅ s14** | É2, É1, É10 | spec v15 p8 · besoins s12/s13/s14 (G2 PO) | ✅ |
| 9 | **Calendrier navigable** (passé/futur, vues prédéfinies semaine/mois/4-sem) **+ sélection de plage de cases** pour définir une période (clic début+fin ; drag riche reporté tranche 2) | É4, É7 | spec v15 p9 · retours s02/s03 | ✅ s15 |
| 9bis | **Survol → résumé de la journée** (enrichissement après ~1s ; périmètre à cadrer) | É5, É9 | spec v09 · besoins s07 | ⬜ |
| 10 | Alimentation & saisie — **config foyer durable restante** (lieux, set couleurs, cycle de fond) + Admin/Parent/Autre, écran de config complet | É1, É2, É7 | spec v05 p5-6 · retours s01/s03 | ⬜ |
| 11 | Immédiat & événements à venir — panneau cloche (transferts + changements + « qui récupère ce soir ») | É8, É9 | spec v05 p7 · retours s02/s03 | ⬜ |
| 12 | Imprévu & échange — malade/retard/échange + transferts dérivés automatiquement par défaut | É8, É11 | spec v05 p8 · spec règles 19-20 | ⬜ |
| 13 | Ouverture de l'accès (auth OAuth, landing, comptes inactifs + impersonation + prise en main par rôle, personnalisation des couleurs, thème sombre persisté) | É10, É2, É5 | spec v05 p9 · retours s01/s07/s08 | ⬜ |
| 14 | **Adaptateurs de droite — persistance réelle du RESTE du domaine** (slots/périodes/transferts/cycle) **+ démarrage runtime sans seed** (app vide, durable ensuite ; InMemory seedé conservé pour les tests). Borne anti-cliquet règle 30 **levée par révision PO hors process** (absorbé dans s15) | É1, É3 | spec v09 règle 30 · révision PO hors process s15 | ✅ s15 |
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
- ~~**Conteneurisation Docker**~~ — **FAITE hors process (avant s15)** : `docker-compose.yml` monte mongo + mongo-express + build + api + web (images dotnet/sdk montées, `--artifacts-path` isolant les binaires) ; docs `README-docker.md` + `LANCEMENT.md`. Origine : PO post-s05.

## Dettes explicitement signalées

- Données en dur dans `Foyer.cs` (É1) — persister en base — retours s03 (#11).
- ~~Aucune édition/suppression de période depuis l'IHM (É7)~~ — **ENTIÈREMENT refermée** : suppression au **s16** (4ᵉ usage menu clic-case, idempotente) **+ édition au s17** (5ᵉ usage : re-borner / réaffecter, formulaire pré-rempli, rejet sur état périmé, re-résolution surcharge>fond>neutre, 11/11 vert, store Mongo réel). « trou fonctionnel assumé » — retours s03. **Clos.**
- ~~Saisies invisibles à l'écran (É12)~~ — **éteint au s06 (palier 2)** : faux bug (date par défaut → `IDateTimeProvider`) ET vrai défaut couleur (mapping libellé→identifiant stable + seed) corrigés, 8/8 vert — retours s03 (#5) · consolidation s05 · livré s06.
- Risque d'adoption du second parent (É10) — repoussé au palier 13 (auth), « ne pas laisser glisser ».
- Faux sentiment de progrès — 2 sprints structurels d'affilée (s04, s05) sans besoin produit observable ; **résorbé au s06** : le palier 2 (Saisie visible) a rendu la main à l'usage (8/8 vert). Vigilance maintenue : ne pas remonter les paliers techniques 10/11 devant l'usage.
- `@code` inline restant (`Legende.razor`, `Pages/Home.razor`) + frontières hexagonales à homogénéiser + séparation des projets (É3) — **cible de la refacto technique HORS pipeline décidée à la clôture s09** (iso-comportement, invariant 161/161). Retours s03 (#7).
- ~~Cycle multi-semaines non affiché/éditable (É1)~~ — **éteint au s10 (palier 6)** : cycle de fond affiché (grille + légende) et éditable (section config), EN MÉMOIRE ; durabilité séquencée au palier 10.
- ~~**Dropdown « Acteur du foyer » périmée au renommage** (É2, /configuration)~~ — **résorbée au s13** : le sélecteur lit désormais le store vivant `Foyer.ActeursEditables` (cohérence règle 5 tenue partout, y compris après suppression). Signalée au gate s10, fix embarqué tête de sprint s13.
- ~~**Sélecteur d'édition de l'écran config encore sur `Foyer.ActeursEditables` (É2, hors-scope s19)**~~ — **RÉSOLUE au s20** : le sélecteur d'édition config lit désormais **exclusivement** le **store vivant unifié** `IEnumerationActeursFoyer` (id stable) et `Foyer.ActeursEditables` a été **retirée** → **un seul chemin de lecture** du référentiel acteurs (config↔dialogs↔grille↔légende), y compris sous propagation SignalR (écran config abonné au hub lecture). Signalée à la clôture s19, refermée dans le goal `config-foyer-onglets`.
- **Cycle de fond riche réclamé** (É7) — l'usage (gate s10) demande ancre/début, frontière de jour, plage début/fin, sur-cycles vacances, WE-only : au-delà du plus petit incrément livré, sujet plein séquencé (+5).
- **Flakes temps-réel SignalR** (É3, `FrontWasmConfig*TempsReel*`) — verts en isolation, flaky sous charge parallèle (timing SignalR/Docker) ; **dette de test** (pas un bug `src/`). Convention anti-flake codifiée (rétro s11, `ihm-builder`) ; **garde déterministe `WaitForState(acteur-foyer)` posé sur 7 `*TempsReel*` au s13** (course `UnknownEventHandlerId` rendue déterministe par la touche d'un composant partagé) ; **rétrofit complet = P2** (helper bUnit partagé + audit, rétro s13), prérequis de l'édition concurrente (P3). Constaté s11, partiellement traité s13, **flake résiduel reconstaté s17 puis s18 avec visibilité en HAUSSE sous charge** (`FrontWasmInvitePlageIndisponibleTempsReel` rouge **2/3 runs full-suite** au s18, vert isolé + re-run) — menace désormais le gate de non-régression (le P2 existe au catalogue, +2 ; triage du flake codifié dans `dev-team` à la rétro s18, cf. `JOURNAL-METHODE.md`). **Montée de sévérité s19** : la charge **multi-clients** du test runtime Sc.7 (ajout d'acteur propagé à 2 écrans) **étend** le flake à des tests **hors `*TempsReel*`** (~**2/10 runs** : ex. `FrontWasmConfigRenommerActeurGrilleTempsReel`, `FrontWasmDefinirTransfertGatingInvite`), re-render races bUnit/SignalR multi-clients — toujours **verts en isolation + re-run ciblé**, **aucune cause logique dans `src/`**. **2ᵉ montée de sévérité s20** : l'écran config devient un **2ᵉ client SignalR** dans les tests grille+config → le flake de convergence multi-clients monte à **~1-2 tests par run complet** (toujours **un seul *TempsReel* à la fois**, vert au re-run ciblé, tout vert hors *TempsReel*, aucune cause `src/`). **Chaque sprint ajoutant un client SignalR aggrave le flake et rapproche le gate 288/288 du rouge** → priorité du rétrofit **montée à P1** (cf. « Prochains sprints envisagés » +2). **Near-miss s21 (friction méthode réelle, corrigée)** : une **VRAIE régression déterministe** — `FrontWasmConfigSupprimerActeurTempsReelTests` rouge **3/3 en isolation** (`RechargerRoles()` posé au s21 sur le handler SignalR `MiseAJour` faisait courir la diffusion contre l'accusé de suppression) — a été **étiquetée « flake *TempsReel* »** par dev-team (triage s18 **sur-appliqué faute de discriminateur**) et a **failli passer le gate** ; rattrapée au **G3** par le thread principal (re-run isolé + baseline s20 288/288 verte), corrigée à la cause (accusé optimiste avant POST, commit `37bced4`). **Fix rétro s21** : le garde-fou triage `dev-team` exige désormais un **re-run EN ISOLATION x2-3** AVANT tout étiquetage — **N/N rouge déterministe = régression** (STOP, jamais « flake », jamais continuer) ; seul un rouge **intermittent** reste flake catalogué (cf. `JOURNAL-METHODE.md`). Le **vrai** flake P1 intermittent multi-clients (~1/3 runs sous charge parallèle) reste une dette distincte, candidat rétrofit fort (+2).
- ~~Dernière saisie hors-contexte restante~~ — **éteinte au s12 (palier 7)** : page/route/lien `/planning/definir-transfert` supprimés, 3e dialog transfert livrée, épic É12 refermé. Constaté s11, résolu s12.
- **Asymétrie seed runtime/tests (s15)** — en mode Mongo, **aucun seed** (app vide au 1er lancement, durable ensuite) ; en InMemory, seed de base **conservé** pour la non-régression. Décision PO assumée (hors process). Effet de bord : 3 tests Mongo s09 ont été re-câblés sur ajout explicite d'acteurs (durabilité toujours prouvée sur store réel).
- **Vulnérabilités transitives du driver Mongo** (`SharpCompress` 0.30.1 NU1902 modéré, `Snappier` 1.0.0 NU1903 élevé) — warnings au build depuis le pivot Mongo généralisé (s15). À traiter par une montée de version du driver MongoDB.Driver. Non bloquant (warnings, pas erreurs).
- **Variantes de plage reportées tranche 2 (s15)** — drag riche, plage vide, chevauchement, plage à cheval sur vue/mois : seul le geste clic-début+clic-fin sur cases contiguës est livré (É4/É7).
- **Cohérence config foyer → planning (retours s21)** — le PO demande que ce qui est configuré dans le foyer soit **effectif pour le planning** de bout en bout. Une part est déjà tenue (acteurs / cycle de fond résolus depuis le store vivant), mais le besoin est signalé comme **écart perçu** : à cadrer (quels réglages ne se propagent pas encore ?) au make-gherkin d'un sujet dédié. Origine retours s21.
- **Rôle livré comme caractéristique sans droits attachés (s21)** — le modèle de rôles (référentiel + affectation) n'a **pas encore** de comportements/droits (nounou/grand-parent/second parent) : le couplage rôle → droits par acteur vit dans É10 (auth, palier 13), après la prise en main de compte. Invariant tenu : le rôle **n'intervient pas** dans la résolution grille/légende.

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