# Product backlog — RESTE À FAIRE (planning-de-garde)

> **Backlog produit vivant** (artefact SCRUM) : ce qui **reste** à livrer. Miroir de
> [`BACKLOG-Done.md`](BACKLOG-Done.md) qui archive le **déjà fait** (26 sprints, paliers ✅,
> besoins ✅, dettes refermées). Source de vérité du *quoi/quand* qui reste ; le *pourquoi* vit
> dans la spec vivante éclatée [`docs/specs/`](specs/index.md).
>
> **Tenue à jour par le pipeline** : `/cloture` ajoute les besoins issus des retours PO et
> **déplace vers `BACKLOG-Done.md`** ce qui est livré (gate G3 passé). Statuts : 🟡 en cours ·
> ⬜ à faire. Origine tracée : `spec` (règle/palier), `retours sNN`, `dette`.

## En cours

*(Aucun sprint en cours.)* Dernier livré = **s29 `slots-recurrents-et-transfert-bicolore`**
(**slot récurrent hebdo simple** — jour de semaine + plage début→fin + lieu, **enfant implicite**,
posé via la **dialog « Poser un slot » unifiée** (case « Répéter chaque semaine »), persistance
**Mongo durable** + InMemory, **projection des occurrences** dans `GrilleAgendaQuery`, suppression
idempotente par id stable ; slot = **localisation orthogonale à la responsabilité**, invariant tenu ;
**transfert saisi rendu en diagonale bicolore sur la pastille de date** — couleurs cédant/recevant
résolues sur le référentiel acteurs, orphelin → neutre, légende motif « Transfert », jour sans
transfert = pastille unicolore inchangée ; gate G3 validé PO ; 14/14, suite **515/515**).
Prochain = `/planning`.

> **Retours PO au gate s29 → cadrés en candidat SPRINT 30 dédié** (ligne +0 ci-dessous) : slot
> récurrent conditionné à la garde (D1, **révision d'invariant**), config des slots récurrents dans
> la Config du foyer + récurrence **multi-jours** (D2), transfert **auto-dérivé** de la succession de
> périodes (D3, **porte métier**). + dette P1 **référentiel d'enfants** (ci-dessous).

## Prochains sprints envisagés

| Rang | Sujet envisagé | Épics | Pourquoi maintenant |
|-----:|----------------|-------|---------------------|
| **+0 (candidat SPRINT 30 dédié — retours PO gate s29, extension directe du livré)** | **Slots récurrents & transferts — 2ᵉ incrément.** Regroupe les 3 demandes reportées au gate s29. **D1 — slot récurrent conditionné à la garde** : toggle « seulement les jours où l'enfant est chez moi » = **RÉVISION D'INVARIANT** (couple l'occurrence du slot à la résolution de responsabilité surcharge > fond > neutre, alors que le slot est aujourd'hui une **localisation orthogonale**) → à cadrer comme **révision de règle hors boucle**, pas un simple feature. **D2 — configurer les slots récurrents dans la Config du foyer** + récurrence **MULTI-JOURS** (ex. École lun/mar/jeu/ven) = nouvelle **surface IHM** (onglet config) + extension du modèle de récurrence (hebdo simple → set de jours). **D3 — transfert AUTO-dérivé de la succession de périodes** (fin période A + début période B le lendemain ⇒ transfert le jour de bascule) = nouvelle sémantique **« transfert implicite »** → **PORTE MÉTIER À TRANCHER** au /planning : priorité **saisi vs dérivé** ; cas limites : retombée **neutre** (fin de garde sans successeur = pas de transfert), **collision** avec un transfert saisi le même jour, **bord de fenêtre** (J+1 non chargé), transition **fond↔période**, acteur **orphelin**. | É6, É8, É7, É2 | Extension directe du livré s29, valeur produit continue ; D1 & D3 touchent des invariants → cadrer au /planning **avant** de coder (révision de règle) |
| **+0 (P1 — dette structurelle actée gate s29 : enfant implicite/masqué)** | **Référentiel d'enfants** — hisser l'enfant en agrégat de 1er rang : **agrégat + port d'énumération** (miroir du référentiel de lieux s27) + **onglet config-foyer** + **sélecteur d'enfant** dans la dialog de pose. Aujourd'hui l'`EnfantId` (« Léa ») reste **implicite/masqué** dans la dialog (`Session.EnfantId` transmis au back, jamais choisi) : bloquant dès qu'un foyer a **≥2 enfants**. Inclut la **rétro-affectation** des slots existants attachés au fantôme. | É1, É6, É2 | Prérequis d'un vrai multi-enfants (spec règle 1) ; la dialog de pose masque un choix qui doit devenir explicite ; cohérent avec le hissage lieux s27 |
| **+1 (P0 — reliquat de la DETTE de câblage auth, s28 en a soldé la moitié)** | **Câblage auth réel — RELIQUAT après s28.** ✅ **Soldé s28** : `IEnvoiMail` (SMTP dev Smtp4dev), `IReferentielJetonsReset` (store Mongo durable), expiration 60 min prouvée, DI des handlers récup/reset + endpoints, **écrans IHM** mot-de-passe-oublié + redéfinir-par-jeton, **login email+mot de passe** (back+IHM), rapprochement Google **logique** + endpoint `demarrer`/callback + DI. **RESTE (P0)** : (1) **provider Google OAuth réel** — le placeholder `FournisseurOAuthGoogleNonCable` renvoie `null` (échange client secret / redirect_uri / callback en env. déployé non câblé) ; (2) **écran consommateur de `definir-mot-de-passe`** (endpoint livré, sans IHM). **RESTE (hors P0)** : (3) **relais SMTP externe réel** — choix PO = **rester Smtp4dev** (dette assumée) ; (4) **boutons MS / Apple OAuth** → **404** (providers non câblés) ; (5) **écran d'inscription libre-service** (handler DI, écran non construit). | É10, É5, É2 | s28 a rendu le reset + le login mot de passe **opérationnels en runtime réel** ; **Google réel** reste le seul volet OAuth non branché (P0), le reste est de la surface (MS/Apple, inscription) ou une dette assumée (SMTP externe) |
| **+1 (P0 — retour PO s28, candidat prioritaire prochain sprint)** | **Édition d'un acteur dans la Configuration du foyer via icône crayon + dialog** — faire évoluer l'onglet Acteurs pour **modifier un acteur** depuis une **icône crayon** ouvrant une **dialog de modification** (au lieu de l'édition inline actuelle). ⚠️ **Réserve** : le PO fournira un **retour structuré complet au prochain `/planning`** (périmètre exact des champs éditables, comportement de la dialog) — cadrer alors le scope précis. | É2, É1 | Retour PO exprimé à la clôture s28 ; améliore l'ergonomie de configuration du foyer (cohérent avec les dialogs d'écriture en contexte s11-s12) |
| **+1 (P1 — flake, 5ᵉ montée de sévérité, non pris s25/s26)** | **Rétrofit complet du garde *TempsReel* SignalR** — cibler la **convergence SignalR multi-clients** (distincte de la course d'énumération gardée s13). Chaque feature ajoutant un client SignalR (auth, config) a aggravé un flake **intermittent** (`FrontWasm*TempsReel*`, vert isolé) : la suite exige **couramment un 2ᵉ run**. Triage durci (rétro s21) tient. Helper bUnit partagé + audit + **sérialiser les assemblies à I/O réel** (le flake déborde de `*TempsReel*` vers les tests SMTP/Mongo sous charge parallèle, s29). | É3 | Le gate exige déjà souvent 2 runs ; chaque client SignalR neuf aggrave **et** le blast-radius monte (s29). À traiter **avant** tout nouveau feature ajoutant des clients SignalR |
| +3 | **Convergence `EditerPeriodeHandler` / `ModifierPeriodeHandler`** — deux handlers de mutation de période coexistent (le second legacy s02, même port + même modèle de concurrence) ; converger vers un seul chemin d'écriture — **dette de code** (DDD : un seul modèle de concurrence par agrégat) | É7 | Évite la dérive de deux chemins d'édition divergents ; ménage hygiénique post-s17 |
| +4 | **Édition concurrente du même jour sous dialog ouverte** (last-write-wins règle 11, à démontrer sous dialog) — DIFFÉRÉE jusqu'à stabilisation SignalR | É7 | Cas limite runtime ; dépend du +1 flake |
| +5 | **Cycle de fond riche** : choisir le début/ancre + config fine (frontière de jour, plage début/fin, sur-cycle vacances, WE-only). Sujet plein — rouvre la décision « ancrage ISO sans ancre » | É7, É1 | Retour PO /configuration s10 |

---

## Épics — besoins ouverts (⬜/🟡)

> Seuls les besoins **restants** sont listés. Les besoins livrés (✅) sont dans
> [`BACKLOG-Done.md`](BACKLOG-Done.md) par épic. Statuts : 🟡 en cours · ⬜ à faire.

### Épic 1 — Fondation données & modèle foyer

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Extraire la config foyer de `Foyer.cs` vers persistance (base) | ⬜ | Palier 10 | retours s03 (#11, dette) · spec p4 |
| Déclaration des enfants du foyer (N enfants, ≥1) | 🟡 | Palier 4/10 | spec règle 1 |
| ↳ **Référentiel d'enfants** (agrégat + port d'énumération + onglet config-foyer + **sélecteur d'enfant** dans la dialog de pose) — **dette P1 actée gate s29** : l'`EnfantId` (« Léa ») reste **implicite/masqué** (`Session.EnfantId` transmis, jamais choisi) ; inclut la **rétro-affectation** des slots existants attachés au fantôme. Bloquant dès ≥2 enfants. | 🟡 | candidat s30 | dette s29 · spec règle 1 |
| Familles recomposées (enfants de parents différents, même planning) | ⬜ | Palier 5-6 | spec règle 2 · retours s07 |
| Parents liés entre eux via leur(s) enfant(s) (graphe foyer) | ⬜ | Palier 5-6 | retours s07 · spec règles 2-3 |
| Deux parents (toujours exactement 2 ; le 1er saisit l'autre) | ⬜ | Palier 5 | retours s01 · spec règle 3 |
| ~~Lieux éditables et persistés (référentiel des sélecteurs)~~ **livré s27** | ✅ | Palier 10 | spec règle 11 · retours s21 |
| Set de couleurs par défaut persisté (acteur → couleur) | 🟡 | Palier 10 | spec règle 15 |

### Épic 2 — Modèle & configuration d'acteurs

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Trois types d'acteurs avec rôles distincts (Admin / Parent / Autre) | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Écran de configuration du foyer complet (acteurs + cycle + couleurs, persisté) | ⬜ | Palier 10 | retours s01 (#7) · spec p5 |
| Affichage/actions adaptés au type d'acteur | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Création d'acteurs par le parent configurateur (email obligatoire → compte inactif) | ⬜ | Palier 5-6 | retours s08 · spec règles 4/6-7 |
| **Cohérence config foyer → planning** : ce qui est configuré doit être **effectif** pour le planning (de bout en bout) | 🟡 | à séquencer | retours s21 |
| ↳ *Volets tenus* : **couleurs** (config→grille/légende, s20 + non-régression s27), **acteurs/rôles/cycle** (store vivant), **lieux** (référentiel éditable+persisté pilotant validation ET sélecteurs, s27). Reste à cadrer : autres réglages non propagés (ex. set couleurs par défaut). | 🟡 | à séquencer | retours s21 |

### Épic 3 — Fondations techniques (architecture & API)

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Convention code-behind systématique (`.razor.cs`, pas de `@code` inline) | 🟡 | s04+ | retours s03 (#7, dette) |

### Épic 5 — Lisibilité & identité visuelle

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Personnalisation des couleurs par utilisateur authentifié | ⬜ | Palier 13 | spec règle 16 |

### Épic 6 — Créneaux & slots de localisation

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| ~~**Slot récurrent hebdomadaire simple** (jour de semaine + plage début→fin + lieu, enfant implicite, projeté en occurrences)~~ **livré s29** (posé via dialog « Poser un slot » unifiée, persistance Mongo durable, projection dans `GrilleAgendaQuery`, suppression idempotente par id stable ; slot = **localisation orthogonale à la responsabilité**) | ✅ | s29 | goal G2 s29 |
| **Slot récurrent conditionné à la garde** (toggle « seulement les jours où l'enfant est chez moi ») — **révision d'invariant** (couple l'occurrence à la responsabilité) | ⬜ | candidat s30 (D1) | retours s29 |
| **Slot récurrent MULTI-JOURS + configuration en Config du foyer** (ex. École lun/mar/jeu/ven) — extension récurrence + nouvelle surface IHM | ⬜ | candidat s30 (D2) | retours s29 |
| **Slot imbriqué** — un slot peut en contenir un autre (ex. chez mamie **et** cours de natation) | ⬜ | à séquencer | retours s07 (idée) |

### Épic 7 — Périodes de garde & responsabilité récurrente

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Cycle de fond **riche** (ancre/début explicite, frontière de jour, plage début/fin, sur-cycle vacances, WE-only) | ⬜ | à séquencer | retours s10 (R3/R4) |

### Épic 8 — Transferts & bascule de responsabilité

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Transfert dérivé automatiquement par défaut (saisie réservée au ponctuel) | ⬜ | Palier 5-6 | spec règle 17 · retours s02 (#14) |
| Transfert ponctuel & modifiable | 🟡 | Palier 5+ | spec règle 18 |
| ~~**Transfert matérialisé sur le planning** : case **bicolore** + séparation en diagonale (départ → arrivée)~~ **livré s29** (diagonale bicolore sur la **pastille de date**, couleurs cédant/recevant résolues sur le référentiel acteurs, orphelin → neutre, légende motif « Transfert », jour sans transfert = unicolore inchangé ; **transfert saisi inchangé**, présentation seule) | ✅ | s29 | retours s17 (#7) |
| **Transfert AUTO-dérivé de la succession de périodes** (fin période A + début période B ⇒ transfert le jour de bascule) — **porte métier** : priorité saisi vs dérivé, cas limites (neutre, collision, bord de fenêtre, fond↔période, orphelin) | ⬜ | candidat s30 (D3) | retours s29 · spec règle 17 |
| Transferts exposés dans le panneau cloche | ⬜ | Palier 11 | spec règle 20 · retours s02 (#8)/s03 |

### Épic 9 — Notifications & événements à venir

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Panneau cloche listant les événements à venir | ⬜ | Palier 11 | spec règles 20/120 · retours s02/s03 |
| Transferts listés comme événements (date, acteurs, lieu, heure) | ⬜ | Palier 11 | spec règle 20 |
| Changements de planning exposés comme événements | ⬜ | Palier 11 | spec règle 20 |
| « Qui récupère ce soir » — immédiat (qui-quand-où du jour) | ⬜ | Palier 11 | spec p4 · spec v03 incrément 2 |

### Épic 10 — Authentification & accès utilisateurs

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| ~~⚠️ **DETTE — câbler les adaptateurs concrets auth (s25, entorse G2)**~~ **partiellement soldée s28** : ✅ `IEnvoiMail` (SMTP dev), `IReferentielJetonsReset` (Mongo durable), expiration 60 min, DI handlers récup/reset + endpoints, écrans mot-de-passe-oublié + redéfinir-par-jeton, login email+MDP. **Reste (P0)** : **provider Google OAuth réel** (`FournisseurOAuthGoogleNonCable` renvoie `null`) + **écran consommateur de `definir-mot-de-passe`**. **Reste (surface/dette assumée)** : MS/Apple OAuth (404), SMTP externe réel (choix PO = Smtp4dev), écran inscription libre-service | 🟡 | Palier 13 (P0 reliquat) | dette assumée G2 s25, part soldée s28 |
| **Protéger la page `/configuration`** pour les non connectés — ⚠️ **vérifier d'abord** : le guard global s25 est censé déjà couvrir cette route ; si accessible sans session, c'est un **trou résiduel du guard s25** à combler, pas un besoin neuf | ⬜ | à séquencer (P1) | demande PO 2026-07-03 |
| Droits d'accès par utilisateur identifié (selon rôle) | ⬜ | Palier 5 + 13 | spec règles 6-7 |
| Personnalisation des couleurs par utilisateur authentifié | ⬜ | Palier 13 | spec règle 16 |
| **Compte créé inactif — volet droits/impersonation** (statut Inactif posé s22 ; le créateur a tous droits + impersonation tant que le compte est inactif — non livré) | 🟡 | Palier 13 | retours s08 · s22 |
| **Prise en main de son compte** par l'utilisateur réel (via une demande) ; puis édition de ses caractéristiques selon son rôle | ⬜ | Palier 13 | retours s08 (idée) |
| **Droits par rôle après prise en main** : Nounou/Grand-parent = éditer profil + demandes ; Second parent = éditer profil + administrer le planning **sur sa période** + demandes d'adaptation | ⬜ | Palier 13 | retours s08 · spec règles 6-7 |

> **Note câblage auth** : la **logique** OAuth 2b, mot de passe, inscription libre-service et
> récupération par jeton est **livrée s25** (prouvée par doublure de port) → voir `BACKLOG-Done.md`.
> Le **câblage réel** est **soldé s28** pour le **reset E2E** (SMTP dev + jetons Mongo + 60 min +
> 2 écrans) et le **login email+mot de passe** ; il **reste** (P0) le **provider Google OAuth réel**
> et l'**écran consommateur de `definir-mot-de-passe`**, plus la surface MS/Apple + inscription +
> le choix assumé Smtp4dev (dette P0 ci-dessus).

### Épic 11 — Imprévu & échange

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Signalement d'imprévu (malade, retard…) + notification immédiate | ⬜ | Palier 12 | spec p7 |
| Échange de dernière minute (proposition + accord requis) | ⬜ | Palier 12 | spec p7 |
| Transferts temporaires (exception, non récurrents) | ⬜ | Palier 12 | spec règles 17-18 |

---

## À faire — paliers de séquencement (⬜)

> Vue de séquencement (ordre de livraison). Paliers 1-9 + 14 **livrés** (voir `BACKLOG-Done.md`).
> Les sujets techniques sont séquencés **derrière l'usage**.

| Palier | Besoin | Épics | Origine |
|-------:|--------|-------|---------|
| 9bis | **Survol → résumé de la journée** (enrichissement après ~1s ; périmètre à cadrer) | É5, É9 | spec v09 · besoins s07 |
| 10 | **Config foyer durable restante** (~~lieux~~ **livré s27** · set couleurs par défaut) + Admin/Parent/Autre + écran de config complet | É1, É2, É7 | spec v05 p5-6 · retours s01/s03 |
| 11 | **Immédiat & événements à venir** — panneau cloche (transferts + changements + « qui récupère ce soir ») | É8, É9 | spec v05 p7 · retours s02/s03 |
| 12 | **Imprévu & échange** — malade/retard/échange + transferts dérivés automatiquement | É8, É11 | spec v05 p8 · spec règles 19-20 |
| 13 | **Ouverture de l'accès (reste)** — câblage adaptateurs auth réels + comptes inactifs (droits) + prise en main par rôle + personnalisation des couleurs *(auth logique + landing + thème sombre déjà livrés s22-s26)* | É10, É2, É5 | spec v05 p9 · retours s01/s07/s08 |
| 15 | **PWA — saisie hors-ligne** (cache + file d'écritures rejouée au retour de connexion) | É12, É3 | spec v06 · besoins s05 |

> **Piste technique (PWA)** — *outbox pattern* comme socle d'une file d'écritures rejouable
> (garantit qu'une commande acceptée hors-ligne est rejouée puis diffusée exactement une fois) ;
> *event sourcing* seulement si le besoin offline/rejeu/audit le justifie, sinon **outbox + file
> client (IndexedDB)** suffit pour l'amorce. À trancher au palier PWA.

## Dépendances entre épics (pour la découpe des sprints)

- **É10 (Auth) → personnalisation couleurs d'É5** : requiert l'identification.
- **É7 (Périodes) + É8 (Transferts) + É9 (Cloche)** forment un bloc « responsabilité + événements ».
- **É12 (Écriture) → É9 (Cloche)** : les événements apparaissent après que les écritures soient observables.
- **É1 (Config foyer) → É2 (Modèle d'acteurs)** : déclarer les acteurs requiert la persistance.
- **É11 (Imprévu)** vient en dernier, paliers 1-6 stabilisés.

## Garde-fous structurels ouverts

- Convention code-behind systématique (`.razor` + `.razor.cs`, pas de `@code` inline) — encore partiel.
- Séparation des canaux : écriture = requête/réponse ; diffusion temps réel = lecture seule (jamais d'écriture par la diffusion) — invariant à tenir.

## Dettes ouvertes

- **Données en dur restantes dans `Foyer.cs`** (É1) — à persister : **set couleurs par défaut** (reste). *(config foyer acteurs persistée s09/s15 ; **lieux hissés en référentiel éditable + persisté s27**, `Foyer.Lieux` static + `FoyerLieuRepository` retirés.)* — retours s03 (#11).
- **Flakes temps-réel SignalR** (É3, `FrontWasm*TempsReel*`) — verts en isolation, **intermittents sous charge parallèle** (timing SignalR/Docker), **dette de test** (pas un bug `src/`). Chaque sprint ajoutant un client SignalR (config, auth) a **aggravé** le flake : au **s24** jusqu'à **6 flakes simultanés** sous charge `Web.Tests`, la suite exige **souvent un 2ᵉ run**. ⚠️ **Blast-radius en HAUSSE de sévérité (s29)** : le flake **déborde de `*TempsReel*`** et touche désormais des **tests runtime hors SignalR** (I/O SMTP/Mongo) **sous charge parallèle** — le remède le plus large n'est plus le seul helper bUnit partagé mais **sérialiser les assemblies I/O** (collections xUnit non parallèles pour les tests à I/O réel). **Triage durci (rétro s21) tient** : re-run EN ISOLATION x2-3 AVANT tout étiquetage — **N/N rouge déterministe = régression** (STOP, jamais « flake »), seul un rouge **intermittent** reste flake catalogué (cf. `JOURNAL-METHODE.md`). **Rétrofit complet = candidat de TÊTE** (helper bUnit partagé + audit, +1 ci-dessus), prérequis de l'édition concurrente (+4).
- **Risque d'adoption du second parent** (É10) — **réduit s28** : le login est **opérationnel en runtime réel** (reset E2E + email/mot de passe, seed compte démo). Reliquat P0 : **Google OAuth réel** + écran `definir-mot-de-passe` ; surface : MS/Apple + inscription libre-service (dette P0 ci-dessus).
- **Enfant implicite/masqué dans la dialog de pose (dette P1, actée gate s29)** — la dialog « Poser un
  slot » (récurrent comme ponctuel) transmet l'`EnfantId` via `Session.EnfantId` **sans le faire choisir**
  (fantôme « Léa »). Solder par un **référentiel d'enfants** (agrégat + port d'énumération + onglet
  config-foyer + sélecteur d'enfant), miroir du hissage lieux s27, avec **rétro-affectation** des slots
  existants. Bloquant dès qu'un foyer a ≥2 enfants. Candidat s30. — É1/É6.
- **Cycle de fond riche réclamé** (É7) — au-delà du plus petit incrément livré s10 : ancre/début, frontière de jour, plage début/fin, sur-cycles vacances, WE-only. Sujet plein (+5).
- **Vulnérabilités transitives du driver Mongo** (`SharpCompress` 0.30.1 NU1902 modéré, `Snappier` 1.0.0 NU1903 élevé) — warnings depuis le pivot Mongo généralisé (s15). À traiter par une montée de `MongoDB.Driver`. Non bloquant.
- **Variantes de plage reportées tranche 2 (s15)** — drag riche, plage vide, chevauchement, plage à cheval sur vue/mois : seul le geste clic-début+clic-fin sur cases contiguës est livré.
- **Cohérence config foyer → planning (retours s21)** — le PO demande que ce qui est configuré soit **effectif** pour le planning. Tenu : acteurs / rôles / cycle (store vivant), **couleurs** (config→grille/légende, filet non-régression s27), **lieux** (référentiel éditable + persisté pilotant validation de pose ET sélecteurs des dialogs, **s27**). À cadrer : réglages restants non propagés (set couleurs par défaut, cycle de fond riche).
- **Rôle livré comme caractéristique sans droits attachés (s21)** — le modèle de rôles (référentiel + affectation) n'a pas encore de comportements/droits ; le couplage rôle → droits vit dans É10 (palier 13), après la prise en main de compte. Invariant tenu : le rôle **n'intervient pas** dans la résolution grille/légende.
- **Asymétrie seed runtime/tests (s15)** — mode Mongo : **aucun seed** (app vide au 1er lancement, durable ensuite) ; InMemory : seed conservé pour la non-régression. Décision PO assumée. **Étendue aux lieux (s27)** : en mode Mongo le foyer **part sans lieux** (aucun seed), donc **aucun slot posable tant qu'un lieu n'est pas configuré** — parité stricte avec l'asymétrie seed acteurs.
