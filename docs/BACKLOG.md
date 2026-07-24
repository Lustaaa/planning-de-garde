# Product backlog — RESTE À FAIRE (planning-de-garde)

> **Backlog produit vivant** (artefact SCRUM) : ce qui **reste** à livrer. Miroir de
> [`BACKLOG-Done.md`](BACKLOG-Done.md) qui archive le **déjà fait** (53 sprints, paliers ✅, besoins ✅,
> dettes refermées). Source de vérité du *quoi/quand* qui reste ; le *pourquoi* vit dans la spec
> vivante éclatée [`docs/specs/`](specs/index.md).
>
> **Tenue à jour par le pipeline** : `/cloture` ajoute les besoins issus des retours PO et **retire**
> ce qui est livré (le fait vit dans `BACKLOG-Done.md`). Statuts : 🟡 en cours · ⬜ à faire. Origine
> tracée : `spec` (règle/palier), `retours sNN`, `dette`.

## État courant

**54 sprints livrés/mergés** (suite complète verte à chaque merge, `main` à jour). L'épic **Refonte
Config foyer** a Acteurs / Rôles / Cycle / Enfants / Activités harmonisés + vue graphe foyer + badge
complétude + commandes inverses actif/admin. Le **noyau produit « qui récupère »** est livré : grille
agenda (seule surface de lecture du planning), **délégation** d'un jour (s44) / d'une plage (s45) /
reprise (s46), **cloche** de notifications avec journal de changements + digest immédiat (s47/s50),
**échange** consenti proposition→accord d'un jour puis d'une plage (s47/s52), **signalement d'imprévu**
malade/retard + action de suivi (s48/s51). **R1 multi-enfants dé-risqué de bout en bout** (s53) :
isolation stricte par enfant sur tous les chemins d'écriture et de lecture ; un enfant sans cycle
propre → NEUTRE (le PO doit configurer le cycle de chaque enfant). **Volet « activités » terminé**
(s54) : vocabulaire IHM « slot »→« activité » + référentiel « Lieux », **routes REST nested sous
l'enfant** (`/api/enfants/{id}/activites*`), **récurrence multi-jours**, **édition de série**,
**exclusion vacances scolaires**, **exceptions d'occurrence « cette occurrence / série »**, **config
foyer par enfant** (créer/éditer/**supprimer** — D2 + trou s31 soldés).

## Candidats de tête au prochain `/planning`

- **Passe de retours PO sur les activités (s54) — PASSE ARCHITECTE FAITE (hors pipeline), branche
  `ia-fix/retours-s54-activites`, PR au PO.** Retours recueillis dans `docs/briefs/sprint 55 - revue.md`.
  **Faits** : ✅ n°6 adresse d'un lieu persistée **à la création** (trou backend — la commande d'ajout ne
  portait pas l'adresse) ; ✅ n°2 & n°8 dropdown de sélection d'enfant → **onglets** (Activités récurrentes
  + Cycle de fond) ; ✅ n°4 bouton **Annuler** sur la dialog d'activité récurrente ; ✅ n°5 sortie explicite
  sur **toutes** les dialogs (dialog Vacances : bouton **Fermer** ajouté — les autres en avaient déjà une).
  **Arbitrages PO reçus et implémentés** (2ᵉ lot de la même passe) : ✅ n°1 grille /planning — **clic sur une
  activité récurrente → dialog d'édition de la SÉRIE** (composant **partagé** `EditerSerieRecurrenteDialog`,
  réutilisé hors `/configuration`) ; la dialog **conserve la suppression** avec portée « cette occurrence »
  (exception S9, date du clic) ET « toute la série » (S5) — **corbeilles retirées** de la grille, invite-scope
  autonome supprimée, **aucune capacité s54 perdue** ; ✅ n°3 **vacances fusionnées** dans la dialog d'édition
  (mode **édition seule**) — la dialog Vacances autonome + le bouton 🏖️ disparaissent. ✅ n°7 largeur déjà
  uniforme (`.dialog-panneau`, `min(520px, 94vw)`) — clos, rien à faire. **Tout le lot de retours s54 est
  soldé** (949/949 vert) ; PR au PO sur `ia-fix/retours-s54-activites`. *(gate s54)*
- **VUE multi-enfants SIMULTANÉE** (lanes / colonnes sur la grille) — surface de LECTURE **neuve**
  (décision PO au coût gate). s53 a livré la vue **MONO-enfant** (sélecteur s30) ; voir plusieurs
  enfants d'un coup est un incrément séparable. *(porte de conception P1 s53, retours s53)*
- **Imprévu / échange MULTI-ENFANT** — R1 exercé de bout en bout (s53) **débloque** l'échange /
  délégation multi-enfants (bornés mono-enfant jusqu'à s52). `SignalerImprevu` : journal / cloche
  **transverses par design (P3)** ; à **border** si un besoin d'imprévu ciblant plusieurs enfants émerge.
- **Délégation / échange récurrent / série** (D2, « tous les mardis ») — distinct d'une plage contiguë.
- **Digest PERSISTANT hors fenêtre chargée** (limitation s42/s43/s50) — le digest se reprojette depuis
  la fenêtre de grille chargée ; naviguer hors du jour courant fait disparaître la section
  « aujourd'hui ». **À arbitrer** : persistance hors vue **vs** coût d'un GET sur push (risque flake).
- **Reste Config foyer** : arbitrage **inline vs modal** (à trancher en G2), **graphe étendu** /
  édition depuis le graphe, **liste de slots par activité**, **lien adresse acteur↔lieu**,
  **suppression d'un enfant** (+ borne R1 au Delete), **R3 « exactement 2 » imposée à l'écriture**
  (choix produit, non imposée). *(suppression slot récurrent IHM + récurrence multi-jours + config
  foyer par enfant = **livrés s54**.)*
- **P0 auth** : **Google OAuth réel** + écran consommateur `definir-mot-de-passe`.

## Petits ajustements / retours ponctuels

- ⬜ **Libellé « Parent responsable » plus explicite dans la dialog** (tweak wording des dialogs
  d'écriture scopées enfant). *(retours s53)*
- ⬜ **Nettoyage optionnel des données legacy cycle** `EnfantId=''`/`undefined` — docs **inertes**
  (jamais lus pour un enfant précis) mais présents dans le store ; un
  `db.cycle_de_fond.deleteMany({EnfantId: {$in: ['', null]}})` les purgerait. **Donnée PO — NON
  exécuté sans validation.** Optionnel, sans impact fonctionnel. *(conséquence UX s53)*

## Arbitrages ouverts (à trancher en G2)

> **⚠️ Direction inline vs modal (retour PO gate s32).** Le PO veut, **EN PLUS** de la modal, pouvoir
> **cliquer un champ du tableau pour l'éditer EN PLACE** (valeur seule) : clic → champ ouvert,
> **Entrée valide**, **clic dehors referme sans update**. **TENSION directe avec s32** (qui a retiré
> l'édition inline au profit de la modal) : c'est un **choix de direction** (inline seul / modal seule /
> cohabitation inline pour la valeur + modal pour le reste) à **trancher en G2** avant tout code — ne
> pas re-livrer l'inline sans arbitrage explicite, sous peine d'annuler la valeur de s32.

## Défauts (bugs) à corriger

> Défauts constatés en usage réel (retours PO). Un défaut n'attend pas un sprint « feature » pour être
> corrigé. **Aucun défaut ouvert actuellement.**

## Prochains sprints envisagés

| Rang | Sujet envisagé | Épics | Pourquoi maintenant |
|-----:|----------------|-------|---------------------|
| +1 (P0 — reliquat câblage auth) | **Câblage auth réel — RESTE (P0)** : (1) **provider Google OAuth réel** (le placeholder `FournisseurOAuthGoogleNonCable` renvoie `null` : échange client secret / redirect_uri / callback en env. déployé non câblé) ; (2) **écran consommateur de `definir-mot-de-passe`** (endpoint livré, sans IHM). **Reste (hors P0)** : relais SMTP externe réel (choix PO = **rester Smtp4dev**, dette assumée), boutons MS / Apple OAuth → **404**, écran d'inscription libre-service. | É10, É5, É2 | **Google réel** = seul volet OAuth non branché ; le reste est de la surface ou une dette assumée |
| +1 (P0 — ÉPIC) | **Refonte de la Configuration du foyer — RESTE** (brief : [`docs/briefs/refonte-configuration-foyer.md`](briefs/refonte-configuration-foyer.md)). Acteurs / Rôles / Cycle / Enfants / Activités **harmonisés** (tableau lecture + crayon→modal) + vue graphe foyer + badge complétude + commandes inverses actif/admin **livrés**. **Reste** : arbitrage **inline vs modal** (G2), **graphe étendu** (grands-parents, parents liés entre eux, lien enfant↔activité dans le graphe) + **édition depuis le graphe**, **liste de slots par activité**, **lien adresse acteur↔lieu/domicile**, **suppression slot récurrent IHM**, **suppression d'un enfant**, contrainte **R3 « exactement 2 » imposée à l'écriture** (choix produit). | É1, É2, É7, É6 | Épic structuré (brief PO) ; reste à découper au `/planning` en incréments verticaux |
| +3 | **Convergence `EditerPeriodeHandler` / `ModifierPeriodeHandler`** — deux handlers de mutation de période coexistent (le second legacy s02, même port + même modèle de concurrence) ; converger vers un seul chemin d'écriture — **dette de code** (DDD : un seul modèle de concurrence par agrégat). | É7 | Évite la dérive de deux chemins d'édition divergents ; ménage hygiénique post-s17 |
| +4 | **Édition concurrente du même jour sous dialog ouverte** (last-write-wins règle 11, à démontrer sous dialog) — **débloquée** : dette flake *TempsReel* soldée s39 (parallèle 0 % rouge), prérequis de stabilité levé. | É7 | Cas limite runtime ; plus de dépendance flake |
| +5 | **Cycle de fond riche** : choisir le début/ancre + config fine (frontière de jour, plage début/fin, sur-cycle vacances, WE-only). Sujet plein — rouvre la décision « ancrage ISO sans ancre ». | É7, É1 | Retour PO /configuration s10 |

---

## Épics — besoins ouverts (⬜/🟡)

> Seuls les besoins **restants** sont listés. Les besoins livrés (✅) sont dans
> [`BACKLOG-Done.md`](BACKLOG-Done.md) par épic. Statuts : 🟡 en cours · ⬜ à faire.

### Épic 1 — Fondation données & modèle foyer

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Extraire la config foyer de `Foyer.cs` vers persistance (base) | ⬜ | Palier 10 | retours s03 (#11, dette) · spec p4 |
| **Reliquats du référentiel d'enfants (s30)** : (1) **migration = utilitaire ops non auto-câblé** au runtime (aucun red ne la force) ; (2) **enfant par défaut du sélecteur = seed « Léa »** *(le vrai multi-enfants R1 est désormais exercé de bout en bout s53)* | ⬜ | à séquencer | reliquats s30 |
| **Suppression d'un enfant** (Delete) + borne défensive « ≥1 enfant » (R1) au Delete | ⬜ | Palier 10 | spec R1 · hors scope s30 |
| **Familles recomposées** (enfants de parents différents, même planning) — VISIBLES en lecture (graphe enfant-racine s38) + complétude du couple R3 signalée par enfant (s40) ; **reste** : **vue planning centrée couple** (recomposé) + contrainte **R3 « exactement 2 » imposée à l'écriture** (non traitée, choix produit) | 🟡 | Palier 5-6 | spec règle 2 · retours s07 |
| **Graphe foyer enfant-racine ÉTENDU** — reste ouvert : grands-parents, parents liés entre eux via leurs enfants, lien enfant↔activité dans le graphe, **édition depuis le graphe** *(vue lecture seule livrée s38, badge + onglet « Foyer » s40)* | 🟡 | Palier 5-6 | retours s07 · spec règles 2-3 |
| **Contrainte R3 « exactement 2 parents » imposée à l'écriture** — le STATUT est signalé en lecture (s40) ; la CONTRAINTE reste **NON imposée** (choix produit, à rouvrir seulement si on veut la bloquer) | 🟡 | Palier 5 | retours s01 · spec règle 3 |
| **Lien adresse acteur-parent ↔ « lieu/domicile » de l'enfant en garde** — l'adresse de résidence de l'acteur (champ s33) doit alimenter/être reliée à un **lieu** (domicile-parent comme lieu implicite/dérivé) ; NOUVELLE relation acteur↔lieu à cadrer avec le volet Activités (impact validation de pose). | ⬜ | épic Refonte Config foyer | retours s33 (PO) · brief config foyer |

### Épic 2 — Modèle & configuration d'acteurs

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Trois types d'acteurs avec rôles distincts (Admin / Parent / Autre) | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Écran de configuration du foyer complet (acteurs + cycle + couleurs, persisté) | ⬜ | Palier 10 | retours s01 (#7) · spec p5 |
| **Refonte Config foyer — reste** : arbitrage **inline vs modal** (à trancher G2), **graphe étendu** / édition depuis le graphe, **liste de slots par activité**, **lien adresse acteur↔lieu**, **suppression slot récurrent IHM**, **suppression d'un enfant**, **R3 « exactement 2 » imposée à l'écriture**. Brief : [`docs/briefs/refonte-configuration-foyer.md`](briefs/refonte-configuration-foyer.md). | 🟡 | épic Refonte Config foyer | retours s28/s29 (PO) · brief |
| **⚠️ Édition INLINE au clic (valeur seule) — À ARBITRER (retour PO gate s32)** : cliquer un champ du tableau pour l'éditer **en place** (clic → champ ouvert, **Entrée** valide, **clic dehors** referme **sans update**), **EN PLUS** de la modal s32. **TENSION directe avec s32** → choix de direction à **trancher en G2** avant tout code. | ⬜ | **à arbitrer G2** | retours gate s32 (PO) |
| Affichage/actions adaptés au type d'acteur | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Création d'acteurs par le parent configurateur (email obligatoire → compte inactif) | ⬜ | Palier 5-6 | retours s08 · spec règles 4/6-7 |
| **Cohérence config foyer → planning** : ce qui est configuré doit être **effectif** pour le planning (de bout en bout). *Tenu* : couleurs, acteurs/rôles/cycle, lieux (s27). *Reste à cadrer* : réglages non propagés (set couleurs par défaut). | 🟡 | à séquencer | retours s21 |

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
| **Liste de slots par activité** (une activité/lieu « avec une liste de slots » récurrents/non) — le référentiel « Lieux » (ex-« Activités » s35, re-renommé s54) porte libellé + adresse + lien enfant, **pas** de slots ; brancher une liste de slots portée par le lieu = extension du modèle de slots + surface modal | ⬜ | épic Refonte Config foyer | retours s29 (PO) · brief · hors scope s35 |
| **Slot imbriqué** — un slot peut en contenir un autre (ex. chez mamie **et** cours de natation) | ⬜ | à séquencer | retours s07 (idée) |

> **Livrés s54 (« terminer tout ce qui est lié aux activités »)** : slot récurrent **MULTI-JOURS** (set
> de jours) + **configuration en Config du foyer PAR ENFANT** (D2) ; **suppression d'un récurrent depuis
> l'IHM** + **nuance « cette occurrence » vs « toute la série »** (trou re-signalé s31) ; + édition de
> série, exclusion vacances, vocabulaire « activité »/« Lieux », routes REST nested sous l'enfant.

### Épic 7 — Périodes de garde & responsabilité récurrente

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Cycle de fond **riche** (ancre/début explicite, frontière de jour, plage début/fin, sur-cycle vacances, WE-only) | ⬜ | à séquencer | retours s10 (R3/R4) |

### Épic 8 — Transferts & bascule de responsabilité

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Transfert ponctuel & modifiable | 🟡 | Palier 5+ | spec règle 18 |

### Épic 9 — Notifications & événements à venir

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| **Digest / carte du jour & « à venir » PERSISTANTS hors de la fenêtre chargée** (limitation s42/s43/s50) : la carte, la liste « à venir » et le digest cloche se reprojettent depuis la **fenêtre de grille chargée**, donc disparaissent au-delà. Choix guidé par l'**anti-amplification flake** (aucun GET dédié sur push). **À arbitrer** : persistance hors vue **vs** coût d'un GET sur push (risque flake). | ⬜ | Palier 11 (arbitrage) | limitation s42/s43/s50 |

### Épic 10 — Authentification & accès utilisateurs

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| **Câblage auth réel — RESTE (P0)** : **provider Google OAuth réel** (`FournisseurOAuthGoogleNonCable` renvoie `null`) + **écran consommateur de `definir-mot-de-passe`**. **Reste (surface/dette assumée)** : MS/Apple OAuth (404), SMTP externe réel (choix PO = Smtp4dev), écran inscription libre-service. | 🟡 | Palier 13 (P0 reliquat) | dette assumée G2 s25, part soldée s28 |
| **Protéger la page `/configuration`** pour les non connectés — ⚠️ **vérifier d'abord** : le guard global s25 est censé déjà couvrir cette route ; si accessible sans session, c'est un **trou résiduel du guard s25** à combler, pas un besoin neuf | ⬜ | à séquencer (P1) | demande PO 2026-07-03 |
| Droits d'accès par utilisateur identifié (selon rôle) | ⬜ | Palier 5 + 13 | spec règles 6-7 |
| **Compte créé inactif — volet droits/impersonation** (statut Inactif posé s22 ; le créateur a tous droits + impersonation tant que le compte est inactif — non livré) | 🟡 | Palier 13 | retours s08 · s22 |
| **Prise en main de son compte** par l'utilisateur réel (via une demande) ; puis édition de ses caractéristiques selon son rôle | ⬜ | Palier 13 | retours s08 (idée) |
| **Droits par rôle après prise en main** : Nounou/Grand-parent = éditer profil + demandes ; Second parent = éditer profil + administrer le planning **sur sa période** + demandes d'adaptation | ⬜ | Palier 13 | retours s08 · spec règles 6-7 |

> **Note câblage auth** : la **logique** OAuth 2b, mot de passe, inscription libre-service et récupération
> par jeton est **livrée s25** (voir `BACKLOG-Done.md`). Le **câblage réel** est **soldé s28** pour le
> reset E2E (SMTP dev + jetons Mongo + 60 min + 2 écrans) et le login email+mot de passe ; il **reste**
> (P0) le **provider Google OAuth réel** et l'**écran consommateur de `definir-mot-de-passe`**, plus la
> surface MS/Apple + inscription + le choix assumé Smtp4dev.

### Épic 11 — Imprévu & échange

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| **Échange MULTI-ENFANTS & échange récurrent/série** (D2) — s52 borné à un **échange plage MONO-ENFANT** ; R1 désormais exercé de bout en bout (s53) **débloque** le multi-enfants, à cadrer ; la série « tous les mardis » reste ouverte. | ⬜ | Palier 12 | hors scope s47/s52 |
| **Notifications push / e-mail externes** — la cloche s47 est **in-app** (temps réel SignalR) ; notifier hors de l'app (push mobile, e-mail) reste ouvert. | ⬜ | Palier 12/13 | hors scope s47 · spec p7 |

---

## À faire — paliers de séquencement (⬜)

> Vue de séquencement (ordre de livraison). Paliers 1-9 + 11 + 12 + 14 **livrés** (voir `BACKLOG-Done.md`).
> Les sujets techniques sont séquencés **derrière l'usage**.

| Palier | Besoin | Épics | Origine |
|-------:|--------|-------|---------|
| 9bis | **Survol → résumé de la journée** (enrichissement après ~1s ; périmètre à cadrer) | É5, É9 | spec v09 · besoins s07 |
| 10 | **Config foyer durable restante** (set couleurs par défaut) + Admin/Parent/Autre + écran de config complet | É1, É2, É7 | spec v05 p5-6 · retours s01/s03 |
| 13 | **Ouverture de l'accès (reste)** — câblage adaptateurs auth réels + comptes inactifs (droits) + prise en main par rôle + personnalisation des couleurs *(auth logique + landing + thème sombre déjà livrés s22-s26)* | É10, É2, É5 | spec v05 p9 · retours s01/s07/s08 |
| 15 | **PWA — saisie hors-ligne** (cache + file d'écritures rejouée au retour de connexion) | É12, É3 | spec v06 · besoins s05 |

> **Piste technique (PWA)** — *outbox pattern* comme socle d'une file d'écritures rejouable (garantit
> qu'une commande acceptée hors-ligne est rejouée puis diffusée exactement une fois) ; *event sourcing*
> seulement si le besoin offline/rejeu/audit le justifie, sinon **outbox + file client (IndexedDB)**
> suffit pour l'amorce. À trancher au palier PWA.

## Dépendances entre épics (pour la découpe des sprints)

- **É10 (Auth) → personnalisation couleurs d'É5** : requiert l'identification.
- **É7 (Périodes) + É8 (Transferts) + É9 (Cloche)** forment un bloc « responsabilité + événements ».
- **É12 (Écriture) → É9 (Cloche)** : les événements apparaissent après que les écritures soient observables.
- **É1 (Config foyer) → É2 (Modèle d'acteurs)** : déclarer les acteurs requiert la persistance.
- **É11 (Imprévu)** vient en dernier, paliers 1-6 stabilisés.

## Garde-fous structurels ouverts

- Convention code-behind systématique (`.razor` + `.razor.cs`, pas de `@code` inline) — encore partiel.
- Séparation des canaux : écriture = requête/réponse ; diffusion temps réel = lecture seule (jamais
  d'écriture par la diffusion) — invariant à tenir.

## Dettes ouvertes

- **⚠️ Flake P1 SignalR *TempsReel* — MONTÉE DE SÉVÉRITÉ RÉ-OUVERTE (s54, à prioriser).** La dette
  soldée s39 (collection `SignalRTempsReelCollection` non parallèle) **remonte** : full-suite **rouge
  intermittent** (0→6 échecs selon les runs) par **concurrence SignalR intra-`Web.Tests`**, mais
  **TOUJOURS vert en isolation** (`Web.Tests` 354/354) et **vert déterministe en série** (945/945,
  `-Serial`). **Non imputable au sprint** (triage x3 discriminé, pas une régression produit). Rétrofit =
  **sérialisation des assemblies I/O intra-`Web.Tests`** (étendre la non-parallélisation aux
  assemblies I/O sous charge, cf. blast-radius s29/s36). *(s54)*
- **Réserve de gate NON exercée par bUnit (s54)** — la **saisie manuelle** des `input[type=time]` (heures
  create/edit d'un récurrent) et `input[type=date]` (plages de vacances) n'est **pas** prouvée par bUnit :
  le **câblage POST/PUT est prouvé côté store réel**, mais la **frappe des champs** reste à confirmer.
  **Candidat SMOKE Playwright** (projet E2E `tests/PlanningDeGarde.Web.E2E`, hors `.slnx`, cf. leçon s49).
  *(s54)*
- **Données en dur restantes dans `Foyer.cs`** (É1) — à persister : **set couleurs par défaut** (reste).
  *(config foyer acteurs persistée s09/s15 ; lieux hissés en référentiel éditable + persisté s27.)* — retours s03 (#11).
- **Cycle de fond riche réclamé** (É7) — au-delà du plus petit incrément livré s10 : ancre/début,
  frontière de jour, plage début/fin, sur-cycles vacances, WE-only. Sujet plein (+5).
- **Vulnérabilités transitives du driver Mongo** (`SharpCompress` 0.30.1 NU1902 modéré, `Snappier` 1.0.0
  NU1903 élevé) — warnings depuis le pivot Mongo généralisé (s15). À traiter par une montée de
  `MongoDB.Driver`. Non bloquant.
- **Cohérence config foyer → planning (retours s21)** — le PO demande que ce qui est configuré soit
  **effectif** pour le planning. Tenu : acteurs / rôles / cycle (store vivant), couleurs (s27), lieux
  (s27). À cadrer : réglages restants non propagés (set couleurs par défaut, cycle de fond riche).
- **Rôle livré comme caractéristique sans droits attachés (s21)** — le modèle de rôles (référentiel +
  affectation) n'a pas encore de comportements/droits ; le couplage rôle → droits vit dans É10
  (palier 13), après la prise en main de compte. Invariant tenu : le rôle **n'intervient pas** dans la
  résolution grille/légende.
- **Variantes de plage reportées** — la sélection par DRAG sur la grille est **livrée s49** (palier 9
  complet). **Restent ouverts** : plage **vide / chevauchement** riches, plage **à cheval sur plusieurs
  vues / mois** (navigation pendant le drag), **sélection persistée** (volontairement volatile s49).
- **Asymétrie seed runtime/tests (s15)** — mode Mongo : **aucun seed** (app vide au 1er lancement,
  durable ensuite) ; InMemory : seed conservé pour la non-régression. Décision PO assumée. **Étendue
  aux lieux (s27)** : en mode Mongo le foyer **part sans lieux**, donc **aucun slot posable tant qu'un
  lieu n'est pas configuré**. **Étendue au cycle par enfant (s53)** : un enfant sans cycle configuré →
  NEUTRE (le PO doit configurer le cycle de chaque enfant).
