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

*(Aucun sprint en cours.)* Dernier livré = **s33 `refonte-config-acteurs-roles-cycle`** (**2ᵉ
incrément vertical de l'épic Refonte Config foyer**) : **(A) Acteurs enrichis** — état actif/admin
passé de pastille lecture à **TOGGLE dans la modal** (**sens ON uniquement** : désignation admin /
activation de compte via les commandes **existantes** ; toggle déjà ON **verrouillé** faute de
commande inverse — dé-désignation / désactivation portées au backlog ; toggle actif actionnable
seulement si l'acteur porte un **compte**) ; **champ neuf « adresse de résidence »** (modèle +
persistance Mongo + modal + rendu tableau, vide accepté) ; **palette couleur en picker minimal**
(solde la dette « set couleurs par défaut »). **(B) Rôles & Cycle harmonisés** au patron **tableau
lecture seule + crayon → modal** (lot atomique de surface s32) ; l'onglet **Cycle rend visibles
tous les cycles déclarés** (corrige le trou gate s32) en hébergeant l'éditeur `definir-cycle`
existant tel quel dans la modal. **Finitions PO gate** : alignements table Rôles/Cycle, libellés
« Semaine paire/impaire », **fermeture Échap des 3 modals** (capture au niveau **document** via port
`IEcouteurEchapModal` — corrige un 1er `@onkeydown` bUnit vert-qui-ment). Sc.1/3 @back réels (adresse +
lecture cycles), Sc.2 early-green s21 (filet non-régression). 11/11 ✅, suite **600/600**, gate G3
validé PO. Prochain = `/planning`.

> **⚠️ À ARBITRER au prochain `/planning` — direction inline vs modal (retour PO gate s32).** Le PO veut,
> **EN PLUS** de la modal, pouvoir **cliquer un champ du tableau pour l'éditer EN PLACE** (valeur seule,
> pas la modal) : clic → champ ouvert, **Entrée valide**, **clic dehors referme sans update**. **NOUVEAU
> VOLET en TENSION directe avec la refonte s32** (qui a justement RETIRÉ l'édition inline au profit de la
> modal). **Non absorbable** : c'est un **choix de direction** (inline seul / modal seule / cohabitation
> inline pour la valeur + modal pour le reste) à **trancher en G2** avant tout code — ne pas re-livrer
> l'inline sans arbitrage explicite, sous peine d'annuler la valeur de s32.

> **Candidats goal prochain `/planning`** (Acteurs 2ᵉ incr. + harmonisation Rôles/Cycle **livrés s33**) :
> (1) **3ᵉ incrément épic Refonte Config foyer — harmoniser Lieux/Activités & Enfants** au même patron
> tableau lecture + crayon→modal (nouveau volet, retour PO gate s33), combiné au **lien enfant↔parent**
> (« lier un enfant à 2 parents », re-signalé gate s33) et au **renommage Lieux → Activités** ;
> (2) **arbitrage inline vs modal** (tension s32, toujours ouvert, à trancher en G2) ;
> (3) **suppression d'un slot récurrent depuis l'IHM** (retour PO gate s31, affordance manquante +
> nuance occurrence unique vs série — goal 4 reporté s33) ;
> (4) **commandes inverses actif/admin** (dé-désignation admin + désactivation de compte — le toggle
> s33 est verrouillé en sens ON faute de ces commandes). **D2** (slots récurrents en Config foyer +
> récurrence **multi-jours**) reste dette ouverte séparée. **Google OAuth réel** (P0) reste en tête.

## Défauts (bugs) à corriger

> Défauts constatés en usage réel (retours PO). Candidats fix prioritaires — un défaut n'attend pas
> un sprint « feature » pour être corrigé.

| Prio | Défaut | Détail | Origine |
|:----:|--------|--------|---------|
| ~~**P0** — **FAIT s31**~~ | ~~**F5 sur `/planning` → renvoie sur la page de login**~~ **CORRIGÉ s31 (V1)** : session **persistée/restaurée côté client** (port `IPersistanceSession` + adaptateur JS localStorage) au démarrage, **purgée au logout** (borne anti-cliquet R30 + logout s23 **tenus**) → F5 connecté reste connecté ; F5 après logout redirige `/connexion`. | retours s29 (PO) · fait s31 |
| ~~**P2** — **FAIT s31**~~ | ~~**Champ mot de passe sans bouton « œil »**~~ **LIVRÉ s31 (V1)** : bouton œil afficher/masquer le mot de passe sur `/connexion` (toggle). | retours s29 (PO) · fait s31 |

## Prochains sprints envisagés

| Rang | Sujet envisagé | Épics | Pourquoi maintenant |
|-----:|----------------|-------|---------------------|
| ~~**+0 (SPRINT 31 — D1 + D3 combinés)** — **LIVRÉ s31**~~ | ~~Slot conditionné à la garde (D1) + transfert auto-dérivé (D3)~~ **LIVRÉ s31** (15/15, 561/561, gate G3 validé) : **D3** transfert AUTO-dérivé sur **deux chemins séparés** — (1) succession de **périodes saisies** (fin A jour J + début B jour J+1) ; (2) **bascule du CYCLE DE FOND** (`ResoudreResponsable(J-1) ≠ ResoudreResponsable(J)`, ajouté au **rework G3 option A**, prouvé Mongo réel) ; priorité **SAISI > DÉRIVÉ**, cas limites neutre/bord de fenêtre/orphelin R6, rendu bicolore réutilisé. **D1** slot récurrent conditionné à la garde (toggle « seulement les jours où l'enfant est chez moi », occurrence projetée seulement les jours où le poseur est résolu responsable, défaut s29 inchangé, toggle en dialog). Les 2 changements de cœur ont été **séquencés strictement** (V1 dérisque → V2 D3 vert → V3 D1) sans jamais se croiser. | É6, É8, É7 | livré s31 |
| **+0 (D2 — dette ouverte séparée, retour PO gate s29)** | **Configurer les slots récurrents dans la Config du foyer + récurrence MULTI-JOURS** (ex. École lun/mar/jeu/ven) = nouvelle **surface IHM** (onglet config) + extension du modèle de récurrence (hebdo simple → set de jours). Distinct de D1/D3 (aucun changement de cœur de résolution) — peut être séquencé indépendamment. | É6, É2 | Retour PO gate s29 ; extension de récurrence + surface config, sans toucher la résolution |
| ~~**+0 (P1 — dette structurelle actée gate s29 : enfant implicite/masqué)** — Référentiel d'enfants~~ **livré s30** : enfant hissé en **agrégat de 1er rang** (id opaque + prénom), ports énumération/édition, rejets prénom vide/doublon sans écriture, **store Mongo durable sans seed**, **validation d'existence à la pose** (ponctuel + récurrent), **migration rétro-affectation idempotente** prouvée store réel, **onglet « Enfants »** config foyer, **sélecteur d'enfant explicite** (`Session.EnfantId` fantôme retiré). **Reliquats explicites** : (1) **migration = utilitaire ops non auto-câblé** (aucun red runtime ne la force) ; (2) **enfant par défaut du sélecteur = seed « Léa »** (pas de choix persisté par contexte) ; (3) **vrai multi-enfants au sens spec R1 pas encore exercé** au-delà de l'agrégat (familles recomposées, graphe parents, multi-enfants dans le cycle de fond restent ouverts). | ✅ (reliquats notés Épic 1) | livré s30 | dette s29 · spec R1 |
| **+1 (P0 — reliquat de la DETTE de câblage auth, s28 en a soldé la moitié)** | **Câblage auth réel — RELIQUAT après s28.** ✅ **Soldé s28** : `IEnvoiMail` (SMTP dev Smtp4dev), `IReferentielJetonsReset` (store Mongo durable), expiration 60 min prouvée, DI des handlers récup/reset + endpoints, **écrans IHM** mot-de-passe-oublié + redéfinir-par-jeton, **login email+mot de passe** (back+IHM), rapprochement Google **logique** + endpoint `demarrer`/callback + DI. **RESTE (P0)** : (1) **provider Google OAuth réel** — le placeholder `FournisseurOAuthGoogleNonCable` renvoie `null` (échange client secret / redirect_uri / callback en env. déployé non câblé) ; (2) **écran consommateur de `definir-mot-de-passe`** (endpoint livré, sans IHM). **RESTE (hors P0)** : (3) **relais SMTP externe réel** — choix PO = **rester Smtp4dev** (dette assumée) ; (4) **boutons MS / Apple OAuth** → **404** (providers non câblés) ; (5) **écran d'inscription libre-service** (handler DI, écran non construit). | É10, É5, É2 | s28 a rendu le reset + le login mot de passe **opérationnels en runtime réel** ; **Google réel** reste le seul volet OAuth non branché (P0), le reste est de la surface (MS/Apple, inscription) ou une dette assumée (SMTP externe) |
| **+1 (P0 — ÉPIC : Refonte de la Configuration du foyer ; 1er incrément LIVRÉ s32)** | **Refonte de la Configuration du foyer** — brief PO complet : [`docs/briefs/refonte-configuration-foyer.md`](briefs/refonte-configuration-foyer.md). Harmoniser toutes les sections config sur un même patron **tableau lecture seule + crayon → modal**. **(A) Acteurs — ✅ 1er INCRÉMENT LIVRÉ s32** : tableau lecture seule (nom, email, rôle, **état en pastille** actif/admin) + colonne **crayon → modal** éditant les **champs existants** (nom, couleur, rôle via CRUD existant) + « Ajouter » = modal vide ; refus→modal ouverte ; Parent-gated + SignalR. **(A) 2ᵉ INCRÉMENT ✅ LIVRÉ s33** : **toggle actif/admin** *dans* la modal (**sens ON**, toggle déjà ON verrouillé faute de commande inverse ; actif conditionné à un compte), **adresse de résidence** [champ neuf back+modal+tableau], **palette couleur** en picker minimal (solde la dette set couleurs). **⚠️ Reste (A) : arbitrage inline vs modal** (retour PO gate s32 : édition inline au clic de la valeur, EN PLUS de la modal — tension directe avec s32, à trancher en G2). **(B) Rôles ✅ HARMONISÉ s33** (tableau lecture + crayon→modal). **(C) Cycle ✅ HARMONISÉ s33** (tableau + crayon→modal hébergeant l'éditeur `definir-cycle` ; **cycles déclarés désormais TOUS visibles** — corrige le trou gate s32). **(D) Lieux → « Activités »** : le PO repense le lieu comme une **activité liée à l'enfant** (nom, **adresse**, **liste de slots** avec flag récurrent/non ; plusieurs enfants peuvent partager la même activité) — **renommage sémantique + lien enfant↔activité** (à cadrer : impact sur le référentiel de lieux s27 + validation de pose). **(E) Enfants** : **harmoniser** l'onglet Enfants (livré s30) avec le reste ; **vue d'accueil lecture seule = graph avec l'enfant en racine** ; **lien enfant↔parent** (« comment lier les enfants au parent ? À faire pendant la refonte des acteurs »). **Bonus (2ᵉ temps)** : multi-enfants configurables, vue planning centrée sur la garde des enfants d'un couple, **vue foyer recomposé** + proposition de config dédiée. | É1, É2, É7, É6 | Retour PO structuré (annoncé /planning s29, capté clôture s30) ; **1er incrément acteurs crayon/modal livré s32** ; reste à **découper au /planning** en incréments verticaux (arbitrage inline vs modal + toggle/adresse/palette acteurs ; harmonisation rôles/cycle/enfants ; activités/graph) ; introduit des champs neufs (adresse, palette couleur) et un lien enfant↔parent |
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
| ↳ ~~**Référentiel d'enfants** (agrégat + port d'énumération + onglet config-foyer + **sélecteur d'enfant** dans la dialog de pose)~~ **livré s30** : agrégat `Enfant` (id opaque + prénom), ports énumération/édition, rejets vide/doublon sans écriture, **Mongo durable sans seed**, **validation d'existence à la pose** (ponctuel + récurrent), **migration rétro-affectation idempotente** prouvée store réel, onglet « Enfants » + sélecteur explicite (`Session.EnfantId` retiré). | ✅ | s30 | dette s29 · spec R1 |
| ↳ **Reliquats du référentiel d'enfants (s30)** : (1) **migration = utilitaire ops non auto-câblé** au runtime (aucun red ne la force) ; (2) **enfant par défaut du sélecteur = seed « Léa »** ; (3) **vrai multi-enfants au sens spec R1 pas encore exercé** au-delà de l'agrégat (usage réel ≥2 enfants de bout en bout). | ⬜ | à séquencer | reliquats s30 · spec R1 |
| Suppression d'un enfant (Delete) + borne défensive « ≥1 enfant » (R1) au Delete | ⬜ | Palier 10 | spec R1 · hors scope s30 |
| **Lien enfant↔parent** (« comment lier les enfants au parent ? » — dont **lier un enfant à 2 parents**, **re-signalé au gate s33**) — à traiter **dans la refonte des acteurs / le volet Enfants** (épic Refonte Config foyer, +1 P0) ; vue d'accueil config = **graph enfant en racine** ; à combiner avec R2/R3 (familles recomposées, toujours 2 parents) | ⬜ | épic Refonte Config foyer | retours s29 (PO) · **re-signalé gate s33** · brief config foyer |
| **Harmoniser l'onglet Enfants ET « Lieux »** (livrés s30/s27) au patron **tableau lecture + crayon→modal** (comme Acteurs s32 / Rôles & Cycle s33) — **prochain volet de l'épic**, retour PO gate s33 (« faire le même patron sur les Lieux et les Enfants ») ; à combiner avec le lien enfant↔parent et le renommage Lieux→Activités | ⬜ | épic Refonte Config foyer (3ᵉ incr.) | retours s29 (PO) · **re-signalé gate s33** · brief config foyer |
| Familles recomposées (enfants de parents différents, même planning) | ⬜ | Palier 5-6 | spec règle 2 · retours s07 |
| Parents liés entre eux via leur(s) enfant(s) (graphe foyer) | ⬜ | Palier 5-6 | retours s07 · spec règles 2-3 |
| Deux parents (toujours exactement 2 ; le 1er saisit l'autre) | ⬜ | Palier 5 | retours s01 · spec règle 3 |
| ~~Lieux éditables et persistés (référentiel des sélecteurs)~~ **livré s27** | ✅ | Palier 10 | spec règle 11 · retours s21 |
| **Repenser « Lieux » → « Activités » liées à l'enfant** (PO : « lieux n'est pas le bon terme ») : propriétés nom + **adresse** + **liste de slots** (flag récurrent/non) ; plusieurs enfants peuvent partager la même activité — **renommage sémantique + lien enfant↔activité**, impact à cadrer sur le référentiel lieux s27 + validation de pose | ⬜ | épic Refonte Config foyer | retours s29 (PO) · brief config foyer |
| **Lien adresse acteur-parent ↔ « lieu/domicile » de l'enfant en garde** (retour PO à chaud s33) : l'**adresse de résidence de l'acteur** (champ ajouté s33 Sc.1) doit **alimenter/être reliée** à un **lieu** — le lieu de résidence de l'enfant **quand il est chez un parent**. NOUVELLE **relation acteur↔lieu** (domicile-parent comme lieu implicite/dérivé) à cadrer AVEC le volet « Lieux → Activités » (impact validation de pose + référentiel lieux s27). **HORS scope s33** (goal = acteur enrichi + harmonisation rôles/cycle ; Activités/lieux explicitement non traités) → à découper au `/planning`. | ⬜ | épic Refonte Config foyer | retours s33 (PO, à chaud) · brief config foyer |
| ~~Set de couleurs par défaut persisté (acteur → couleur)~~ **soldé s33** : **palette couleur en picker minimal** dans la modal acteur (choix dans le set de couleurs, couleur courante pré-sélectionnée, persistée via la commande existante, grille/légende suit sans reload). *(Hors scope tenu : pas de palette custom — créer/renommer/supprimer des couleurs.)* | ✅ | s33 | spec règle 15 |

### Épic 2 — Modèle & configuration d'acteurs

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Trois types d'acteurs avec rôles distincts (Admin / Parent / Autre) | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Écran de configuration du foyer complet (acteurs + cycle + couleurs, persisté) | ⬜ | Palier 10 | retours s01 (#7) · spec p5 |
| **Refonte Config foyer — patron tableau lecture seule + crayon → modal** — **Acteurs 1er incr. livré s32**, **Acteurs 2ᵉ incr. + Rôles & Cycle livrés s33** (toggle actif/admin sens ON + verrou, adresse, palette picker ; Rôles & Cycle au patron tableau+crayon→modal ; **tous les cycles déclarés visibles** ; fermeture Échap des modals). **Reste** : harmoniser **Lieux/Activités & Enfants** au même patron (3ᵉ incr.), **arbitrage inline vs modal**, **commandes inverses actif/admin**. Brief : [`docs/briefs/refonte-configuration-foyer.md`](briefs/refonte-configuration-foyer.md). | 🟡 | épic Refonte Config foyer (Acteurs+Rôles+Cycle faits) | retours s28/s29 (PO) · brief · **s32+s33** |
| **⚠️ Édition INLINE au clic (valeur seule) — À ARBITRER (retour PO gate s32)** : le PO veut, **EN PLUS** de la modal s32, cliquer un champ du tableau pour l'éditer **en place** (clic → champ ouvert, **Entrée** valide, **clic dehors** referme **sans update**). **TENSION directe avec s32** (qui a retiré l'inline au profit de la modal) → **choix de direction** (inline seul / modal seule / cohabitation) à **trancher en G2 au prochain /planning** avant tout code. | ⬜ | **à arbitrer G2** | retours gate s32 (PO) |
| ~~**Adresse de résidence de l'acteur** (champ de modèle neuf, exposé dans la modal d'édition acteur)~~ **livré s33** : champ **adresse de résidence** porté par le modèle d'acteur, **persisté Mongo durable**, relu par la query de config, éditable dans la modal + **rendu dans le tableau lecture**, **adresse vide acceptée** (optionnel, sans écriture partielle). *(Reste ouvert : relier cette adresse à un lieu/domicile de l'enfant en garde — cf. Épic 1 « lien adresse acteur↔lieu ».)* | ✅ | s33 | brief config foyer (PO) · re-signalé s32 |
| **Commandes inverses actif/admin** (dé-désignation admin + désactivation de compte) — le toggle actif/admin s33 est **verrouillé en sens ON** faute de ces commandes montantes-seules ; un OFF « no-op silencieux » serait un vert-qui-ment (proscrit s33). Ajouter les commandes/handlers **domaine** (dé-désigner un admin, désactiver un compte) puis débloquer le sens OFF du toggle. **CANDIDAT GOAL prochain `/planning`**. | ⬜ | épic Refonte Config foyer | routé au gate s33 (PO, Sc.4) |
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
| ~~**Slot récurrent conditionné à la garde**~~ **livré s31 (D1)** : toggle « seulement les jours où l'enfant est chez moi » → occurrence projetée **uniquement les jours où la résolution (surcharge > fond) désigne le parent poseur responsable** ; lit la responsabilité sans la modifier ; slot **non conditionné** (défaut) = comportement s29 strictement inchangé ; toggle dans la dialog « Poser un slot ». **Révision d'invariant assumée** (le slot lit désormais la responsabilité). | ✅ | s31 | retours s29 |
| **Slot récurrent MULTI-JOURS + configuration en Config du foyer** (ex. École lun/mar/jeu/ven) — extension récurrence + nouvelle surface IHM | ⬜ | dette ouverte (D2, séparée) | retours s29 |
| **Supprimer un slot récurrent depuis l'IHM** (affordance manquante) + **nuance occurrence unique vs série** : le back sait déjà supprimer par id stable (idempotent, s29), mais le PO **ne trouve toujours pas comment** le faire dans l'IHM (**re-signalé au gate s31**) ; en plus, clarifier **supprimer une seule occurrence** (instance) **vs toute la série** (nouvelle sémantique à trancher). **CANDIDAT GOAL PROCHAIN `/planning`** (nouvelle surface IHM + commande/handler, hors goal s31). | ⬜ | **candidat goal /planning** | retours s29 · **re-signalé s31 (PO)** |
| **Slot imbriqué** — un slot peut en contenir un autre (ex. chez mamie **et** cours de natation) | ⬜ | à séquencer | retours s07 (idée) |

### Épic 7 — Périodes de garde & responsabilité récurrente

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Cycle de fond **riche** (ancre/début explicite, frontière de jour, plage début/fin, sur-cycle vacances, WE-only) | ⬜ | à séquencer | retours s10 (R3/R4) |

### Épic 8 — Transferts & bascule de responsabilité

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| ~~Transfert dérivé automatiquement par défaut (saisie réservée au ponctuel)~~ **livré s31 (D3)** : transfert dérivé automatiquement, priorité **SAISI > DÉRIVÉ** (le saisi prime, pas de doublon). Deux chemins de dérivation (succession de périodes **et** bascule du cycle de fond). | ✅ | s31 | spec règle 24 · retours s02 (#14) |
| Transfert ponctuel & modifiable | 🟡 | Palier 5+ | spec règle 18 |
| ~~**Transfert matérialisé sur le planning** : case **bicolore** + séparation en diagonale (départ → arrivée)~~ **livré s29** (diagonale bicolore sur la **pastille de date**, couleurs cédant/recevant résolues sur le référentiel acteurs, orphelin → neutre, légende motif « Transfert », jour sans transfert = unicolore inchangé ; **transfert saisi inchangé**, présentation seule) | ✅ | s29 | retours s17 (#7) |
| ~~**Transfert AUTO-dérivé de la succession de périodes**~~ **livré s31 (D3)** : **deux chemins de dérivation séparés** — (1) succession de **périodes saisies** (fin A jour J + début B jour J+1, même enfant) ; (2) **bascule du cycle de fond** (le responsable résolu change d'un jour à l'autre, ajouté au rework G3 option A). Priorité **SAISI > DÉRIVÉ** (pas de doublon), cas limites tenus : **neutre** (fin sans successeur), **bord de fenêtre** (J+1 non chargé), **orphelin R6** (acteur supprimé → repli neutre côté orphelin) ; rendu bicolore réutilisé (présentation s29). Prouvé runtime sur Mongo réel (06/07, 10/08). | ✅ | s31 | retours s29 · spec règle 24 |
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
- ~~**Enfant implicite/masqué dans la dialog de pose (dette P1, actée gate s29)**~~ — **SOLDÉE s30** :
  enfant hissé en **agrégat de 1er rang** (id opaque + prénom), ports énumération/édition, rejets vide/
  doublon sans écriture, **Mongo durable sans seed**, **validation d'existence à la pose** (ponctuel +
  récurrent), **migration rétro-affectation idempotente** prouvée store réel, **onglet « Enfants »** +
  **sélecteur d'enfant explicite** (`Session.EnfantId` fantôme retiré). **Reliquats ouverts (Épic 1)** :
  (1) migration = **utilitaire ops non auto-câblé** au runtime ; (2) enfant par défaut du sélecteur =
  **seed « Léa »** ; (3) **vrai multi-enfants au sens spec R1 pas encore exercé** de bout en bout. — É1/É6.
- **Cycle de fond riche réclamé** (É7) — au-delà du plus petit incrément livré s10 : ancre/début, frontière de jour, plage début/fin, sur-cycles vacances, WE-only. Sujet plein (+5).
- **Vulnérabilités transitives du driver Mongo** (`SharpCompress` 0.30.1 NU1902 modéré, `Snappier` 1.0.0 NU1903 élevé) — warnings depuis le pivot Mongo généralisé (s15). À traiter par une montée de `MongoDB.Driver`. Non bloquant.
- **Variantes de plage reportées tranche 2 (s15)** — drag riche, plage vide, chevauchement, plage à cheval sur vue/mois : seul le geste clic-début+clic-fin sur cases contiguës est livré.
- **Cohérence config foyer → planning (retours s21)** — le PO demande que ce qui est configuré soit **effectif** pour le planning. Tenu : acteurs / rôles / cycle (store vivant), **couleurs** (config→grille/légende, filet non-régression s27), **lieux** (référentiel éditable + persisté pilotant validation de pose ET sélecteurs des dialogs, **s27**). À cadrer : réglages restants non propagés (set couleurs par défaut, cycle de fond riche).
- **Rôle livré comme caractéristique sans droits attachés (s21)** — le modèle de rôles (référentiel + affectation) n'a pas encore de comportements/droits ; le couplage rôle → droits vit dans É10 (palier 13), après la prise en main de compte. Invariant tenu : le rôle **n'intervient pas** dans la résolution grille/légende.
- **Asymétrie seed runtime/tests (s15)** — mode Mongo : **aucun seed** (app vide au 1er lancement, durable ensuite) ; InMemory : seed conservé pour la non-régression. Décision PO assumée. **Étendue aux lieux (s27)** : en mode Mongo le foyer **part sans lieux** (aucun seed), donc **aucun slot posable tant qu'un lieu n'est pas configuré** — parité stricte avec l'asymétrie seed acteurs.
