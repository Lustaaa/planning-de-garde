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

*(Aucun sprint en cours.)* Dernier livré = **s26 `refonte-graphique`** (« Studio », thème
clair/sombre persisté, 14/14, suite **458/458**). Prochain = `/planning`.

## Prochains sprints envisagés

| Rang | Sujet envisagé | Épics | Pourquoi maintenant |
|-----:|----------------|-------|---------------------|
| **+1 (P0 — DETTE DE CÂBLAGE ASSUMÉE s25, TÊTE)** | **Câbler les adaptateurs concrets des volets auth prouvés par doublure (s25)** : (1) `IEnvoiMail` SMTP réel ; (2) `IReferentielJetonsReset` store durable ; (3) `IFournisseurOAuth` par provider (Google/MS/Apple, secrets/callbacks) + endpoint `api/oauth/{provider}/demarrer` ; (4) **enregistrement DI** des handlers `@preuve-doublure` (récup mot de passe, OAuth callback) ; (5) **écrans IHM** mot-de-passe-oublié + inscription libre-service ; (6) **confirmer l'expiration du jeton reset** (défaut 60 min). Preuve bout-à-bout à consigner. **Scope architecte** ou sprint dédié. | É10, É5, É2 | L'entorse G2 s25 a laissé la logique verte contre des doublures : **rien n'est utilisable en runtime réel** tant que ces adaptateurs/écrans ne sont pas branchés. Priorité de tête pour rendre le login opérationnel |
| **+1 (P1 — flake, 5ᵉ montée de sévérité, non pris s25/s26)** | **Rétrofit complet du garde *TempsReel* SignalR** — cibler la **convergence SignalR multi-clients** (distincte de la course d'énumération gardée s13). Chaque feature ajoutant un client SignalR (auth, config) a aggravé un flake **intermittent** (`FrontWasm*TempsReel*`, vert isolé) : la suite exige **couramment un 2ᵉ run**. Triage durci (rétro s21) tient. Helper bUnit partagé + audit. | É3 | Le gate 458/458 exige déjà souvent 2 runs ; chaque client SignalR neuf aggrave. À traiter **avant** tout nouveau feature ajoutant des clients SignalR |
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
| Familles recomposées (enfants de parents différents, même planning) | ⬜ | Palier 5-6 | spec règle 2 · retours s07 |
| Parents liés entre eux via leur(s) enfant(s) (graphe foyer) | ⬜ | Palier 5-6 | retours s07 · spec règles 2-3 |
| Deux parents (toujours exactement 2 ; le 1er saisit l'autre) | ⬜ | Palier 5 | retours s01 · spec règle 3 |
| Lieux éditables et persistés (référentiel des sélecteurs) | 🟡 | Palier 10 | spec règle 11 |
| Set de couleurs par défaut persisté (acteur → couleur) | 🟡 | Palier 10 | spec règle 15 |

### Épic 2 — Modèle & configuration d'acteurs

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Trois types d'acteurs avec rôles distincts (Admin / Parent / Autre) | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Écran de configuration du foyer complet (acteurs + cycle + couleurs, persisté) | ⬜ | Palier 10 | retours s01 (#7) · spec p5 |
| Affichage/actions adaptés au type d'acteur | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Création d'acteurs par le parent configurateur (email obligatoire → compte inactif) | ⬜ | Palier 5-6 | retours s08 · spec règles 4/6-7 |
| **Cohérence config foyer → planning** : ce qui est configuré doit être **effectif** pour le planning (de bout en bout) | ⬜ | à séquencer | retours s21 |

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
| **Transfert matérialisé sur le planning** : case **bicolore** + séparation en diagonale (départ → arrivée) | ⬜ | à séquencer | retours s17 (#7) |
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
| ⚠️ **DETTE — câbler les adaptateurs concrets auth (s25, entorse G2)** : `IEnvoiMail` SMTP + `IReferentielJetonsReset` durable + `IFournisseurOAuth` réels + endpoint `api/oauth/{provider}/demarrer` + **DI** des handlers `@preuve-doublure` + **écrans IHM** mot-de-passe-oublié & inscription + confirmer expiration jeton (60 min) | ⬜ | Palier 13 (P0, TÊTE) | dette assumée G2 s25 |
| **Protéger la page `/configuration`** pour les non connectés — ⚠️ **vérifier d'abord** : le guard global s25 est censé déjà couvrir cette route ; si accessible sans session, c'est un **trou résiduel du guard s25** à combler, pas un besoin neuf | ⬜ | à séquencer (P1) | demande PO 2026-07-03 |
| Droits d'accès par utilisateur identifié (selon rôle) | ⬜ | Palier 5 + 13 | spec règles 6-7 |
| Personnalisation des couleurs par utilisateur authentifié | ⬜ | Palier 13 | spec règle 16 |
| **Compte créé inactif — volet droits/impersonation** (statut Inactif posé s22 ; le créateur a tous droits + impersonation tant que le compte est inactif — non livré) | 🟡 | Palier 13 | retours s08 · s22 |
| **Prise en main de son compte** par l'utilisateur réel (via une demande) ; puis édition de ses caractéristiques selon son rôle | ⬜ | Palier 13 | retours s08 (idée) |
| **Droits par rôle après prise en main** : Nounou/Grand-parent = éditer profil + demandes ; Second parent = éditer profil + administrer le planning **sur sa période** + demandes d'adaptation | ⬜ | Palier 13 | retours s08 · spec règles 6-7 |

> **Note câblage auth** : la **logique** OAuth 2b, mot de passe, inscription libre-service et
> récupération par jeton est **livrée s25** (prouvée par doublure de port) → voir `BACKLOG-Done.md`.
> Seul le **câblage des adaptateurs réels + les écrans IHM** reste (dette P0 ci-dessus).

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
| 10 | **Config foyer durable restante** (lieux, set couleurs) + Admin/Parent/Autre + écran de config complet | É1, É2, É7 | spec v05 p5-6 · retours s01/s03 |
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

- **Données en dur restantes dans `Foyer.cs`** (É1) — à persister (lieux, set couleurs) — retours s03 (#11). *(config foyer acteurs déjà persistée s09/s15.)*
- **Flakes temps-réel SignalR** (É3, `FrontWasm*TempsReel*`) — verts en isolation, **intermittents sous charge parallèle** (timing SignalR/Docker), **dette de test** (pas un bug `src/`). Chaque sprint ajoutant un client SignalR (config, auth) a **aggravé** le flake : au **s24** jusqu'à **6 flakes simultanés** sous charge `Web.Tests`, la suite exige **souvent un 2ᵉ run**. **Triage durci (rétro s21) tient** : re-run EN ISOLATION x2-3 AVANT tout étiquetage — **N/N rouge déterministe = régression** (STOP, jamais « flake »), seul un rouge **intermittent** reste flake catalogué (cf. `JOURNAL-METHODE.md`). **Rétrofit complet = candidat de TÊTE** (helper bUnit partagé + audit, +1 ci-dessus), prérequis de l'édition concurrente (+4).
- **Risque d'adoption du second parent** (É10) — le login est livré mais le câblage réel (SMTP/OAuth) et les écrans manquent → à ne pas laisser glisser (dette P0 ci-dessus).
- **Cycle de fond riche réclamé** (É7) — au-delà du plus petit incrément livré s10 : ancre/début, frontière de jour, plage début/fin, sur-cycles vacances, WE-only. Sujet plein (+5).
- **Vulnérabilités transitives du driver Mongo** (`SharpCompress` 0.30.1 NU1902 modéré, `Snappier` 1.0.0 NU1903 élevé) — warnings depuis le pivot Mongo généralisé (s15). À traiter par une montée de `MongoDB.Driver`. Non bloquant.
- **Variantes de plage reportées tranche 2 (s15)** — drag riche, plage vide, chevauchement, plage à cheval sur vue/mois : seul le geste clic-début+clic-fin sur cases contiguës est livré.
- **Cohérence config foyer → planning (retours s21)** — le PO demande que ce qui est configuré soit **effectif** pour le planning. Une part est tenue (acteurs / cycle résolus depuis le store vivant) ; à cadrer : quels réglages ne se propagent pas encore ?
- **Rôle livré comme caractéristique sans droits attachés (s21)** — le modèle de rôles (référentiel + affectation) n'a pas encore de comportements/droits ; le couplage rôle → droits vit dans É10 (palier 13), après la prise en main de compte. Invariant tenu : le rôle **n'intervient pas** dans la résolution grille/légende.
- **Asymétrie seed runtime/tests (s15)** — mode Mongo : **aucun seed** (app vide au 1er lancement, durable ensuite) ; InMemory : seed conservé pour la non-régression. Décision PO assumée.
