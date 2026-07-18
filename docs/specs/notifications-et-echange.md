# Notifications (cloche) & échange consenti

> Sujet **créé s47** (paliers « Immédiat & événements à venir » / cloche **et** « Imprévu &
> échange » / flux consenti). Source de vérité pour la **cloche générale de changements**
> (journal, lu/non-lu, surface barre du haut, diffusion temps réel porteuse de payload), l'**échange
> proposition → accord**, le **signalement d'imprévu informatif** (brique C, s48), le **digest
> « immédiat » dans la cloche** (brique D, s50 — qui récupère ce soir + transferts à venir) et l'**action
> de suivi sur un imprévu** (brique E, s51 — proposer un échange en réaction). Édité en diff, jamais
> réécrit en bloc.

## Contexte

Deux briques greffées l'une sur l'autre, livrées ensemble s47 :

- **(A) une CLOCHE GÉNÉRALE** de changements — la 1ʳᵉ **surface hors-grille** rouverte depuis s44 —
  qui signale à chaque utilisateur **ce qui a changé le concernant** (délégations, plages, reprises,
  transferts, propositions d'échange), avec un **compteur de non-lus** et un état **lu / non-lu par
  utilisateur** ;
- **(B) l'échange PROPOSITION → ACCORD** — l'imprévu / échange de dernière minute **consenti** : un
  parent **propose** un jour à un autre acteur, qui est **notifié via la cloche** et **accepte /
  refuse depuis la notification**. Contrairement à la **délégation directe** s44 (unilatérale, effet
  immédiat), la **proposition n'a AUCUN effet sur la résolution** tant qu'elle n'est pas acceptée :
  c'est le **consentement** du recevant qui déclenche l'écriture.

> **Amendement de « grille = seule surface » (s44).** Le PO avait fait retirer en s44 les surfaces de
> lecture **redondantes avec la grille** (carte du jour s42, panneau « À venir » s43). La cloche
> **n'est pas une re-lecture du planning** : c'est une surface de **notification de changement**,
> **assumée hors-grille**, posée **dans la barre d'application du haut**. Le noyau de **lecture** du
> planning reste la grille agenda (cf. [`saisie-et-grille.md`](saisie-et-grille.md)).

## Brique A — Cloche générale de changements

### Journal de changements = TRACE DE LECTURE, jamais autorité de résolution

- Les notifications sont servies par un **JOURNAL DE CHANGEMENTS append-only** (port neuf
  **`IJournalChangements`**, deux adaptateurs InMemory + Mongo durable), **alimenté par CHAQUE handler
  d'écriture existant** — délégation (s44), plage (s45), **reprise (s46)**, transfert (s31), plus les
  propositions d'échange (brique B) — qui y consigne un événement `{type, jour, enfant,
  cédant / recevant, horodatage via `IDateTimeProvider`}`.
- **Le journal est une TRACE DE LECTURE horodatée, JAMAIS une source de vérité.** La **résolution
  d'une case reste EXCLUSIVEMENT les périodes / transferts** (surcharge > fond > neutre, transferts
  dérivés s31) : **aucun code de résolution ne lit le journal**. Écrire / supprimer une surcharge
  n'altère pas la vérité via le journal, et réciproquement. **Pas de « store d'événements vérité
  divergent »** au sens interdit.
- **Le journal est PERSISTÉ, pas dérivé de l'état courant** *(décision d'archi SM, arbitrage
  dev-team, Sc.1)* : une **reprise s46 SUPPRIME la surcharge** (aucune trace dérivable de l'état
  courant), et ni `PeriodeSnapshot` ni `TransfertSnapshot` ne portent d'**horodatage de création**
  (ils ne connaissent que le jour couvert, pas l'instant d'écriture, donc pas de tri par récence).
  Dériver le flux de notifications de l'état courant est donc **infaisable** — d'où un journal
  persisté, distinct de la résolution.
- Le flux d'un utilisateur restitue **les événements le concernant, triés par RÉCENCE de l'écriture**
  (le plus récent en tête).

### État lu / non-lu par utilisateur

- Un **second état persisté**, séparé du journal : le **lu / non-lu PAR utilisateur** (+ **compteur
  de non-lus**), derrière le port **`IEtatLectureNotifications`** (deux adaptateurs InMemory + Mongo
  durable). Un utilisateur qui marque lu **n'affecte pas** l'état non-lu d'un autre.
- **Marquer-lu (une notif ou toutes) est idempotent** : re-marquer lu ne crée aucun doublon, le
  compteur reste stable.

### Surface — cloche en barre du haut

- **Icône cloche + badge compteur de non-lus + panneau déroulant** (liste chrono, chaque événement
  marqué lu / non-lu, action marquer-lu) — **DANS LA BARRE D'APPLICATION DU HAUT** (`MainLayout`,
  ordre : déconnexion — **cloche** — thème sombre), **composant autonome**.
- **Gating** : visible **connecté && Parent** — **rien** sur `/connexion` ni pour un **Invité**.
- **Échap ferme** le panneau (port `IEcouteurEchapModal` s33).

### Temps réel — diffusion PORTEUSE DE PAYLOAD

*(décision d'archi SM, arbitrage dev-team, Sc.4 & Sc.9)*

- Les surfaces temps réel s42–s46 reprojetaient depuis la donnée **déjà chargée par l'unique GET
  grille**, car leur changement vivait **dans le read model grille**. La **cloche est hors
  read-model-grille** : reprojeter depuis la grille ne suffit pas.
- Nouveau **port de diffusion `INotificateurChangement`** portant l'**`EvenementChangementSnapshot`**
  (journal décoré `JournalChangementsDiffusant`), branché sur **CHAQUE endpoint d'écriture**
  (délégation s44, plage s45, reprise s46, transfert s31, proposition / accept / refus s47). Le
  client **reçoit l'événement dans la diffusion et reprojette → 0 GET sur push**, conforme au
  garde-fou anti-flake ([[flake-signalr-blast-radius]] : « nouveau client SignalR = reprojection
  depuis la diffusion, jamais un GET sur push »).
- **Ne viole PAS « diffusion = lecture seule »** : la diffusion **porte une donnée de LECTURE**
  (snapshot d'un changement déjà écrit) ; **l'écriture reste exclusivement sur le canal
  requête / réponse**. La diffusion ne déclenche jamais d'écriture. **Donnée derrière un port**
  (jamais figée dans le code) ; **pas** de couplage au read model grille, **pas** de GET dédié sur
  push. **Identité du flux** = `IdentiteEffective.Id` de la session courante (cohérente avec le
  Parent-gating et le lu/non-lu par utilisateur).

## Brique B — Échange proposition → accord (consenti)

- **`ProposerEchange(jour, enfant, versActeur)`** crée une **Proposition `pending`** (notification
  chez le recevant) **SANS AUCUNE écriture de surcharge** : le store des surcharges reste **intact**
  et la **résolution de la case est inchangée** (surcharge > fond, aucun basculement, aucun transfert
  dérivé) tant que la proposition n'est pas acceptée. *(Anti vert-qui-ment, Sc.5 : un pending qui
  teinterait déjà la case serait une **délégation déguisée** s44, pas un échange consenti.)*
- **`AccepterProposition`** → `accepté` : **COMPOSE la délégation EXISTANTE s44** — une **surcharge
  du jour** est écrite (le recevant prime, surcharge > fond) et le **transfert cédant → recevant est
  AUTO-DÉRIVÉ** (s31, R24), jamais réécrit. Écriture durable (prouvée Mongo réel).
- **`RefuserProposition`** → `refusé` : la proposition se clôt, **AUCUNE surcharge n'est écrite**, le
  store reste intact.
- **Cas limite & erreur** *(Sc.7)* : **proposer à SOI-MÊME** (recevant = responsable déjà résolu) →
  **refusé sans écriture** ; **délégataire INCONNU / orphelin** (id stable absent du store) → **refus
  AVANT écriture**, store intact, aucune écriture partielle ; **seconde proposition** sur un jour /
  enfant déjà porteur d'un pending → **last-write-wins (R11)**, une seule Proposition pending subsiste
  sans doublon ; **jour hors fenêtre chargée** → enregistrement valide (une date), sans crash. Deux
  adaptateurs InMemory + Mongo durable.

### Surface de l'échange

- **Proposer** = entrée **« proposer un échange » du menu clic-case** (Parent-gated) sur la case du
  jour visé (cf. [`ecriture-en-contexte.md`](ecriture-en-contexte.md), menu clic-case mutualisé).
- **Répondre** = la Proposition pending est une **notification ACTIONNABLE dans la cloche** :
  **Accepter / Refuser DEPUIS la notification** (via mini-dialog de confirmation), émis par le
  **canal d'écriture** (jamais la diffusion). **PLUS de badge sur la case**, **PLUS d'entrée
  conditionnelle du menu clic-case** pour répondre. Échap ferme le mini-dialog / le panneau sans
  commande ; un **Invité** ne voit ni cloche ni entrée (Parent-gated).
- **Temps réel** *(Sc.9)* : à l'**accord**, la case du jour d'un 2ᵉ écran **converge** (recevant
  responsable par surcharge + transfert bicolore dérivé, notif → « accepté ») par reprojection client
  (SignalR, 0 GET) ; au **refus**, la notification se clôt sans écriture ni changement de responsable.

## Brique C — Signalement d'imprévu dédié (malade / retard) *(livré s48)*

- **Cas NON-négocié, purement INFORMATIF**, distinct de l'échange consenti (brique B). Là où l'échange
  **négocie** un jour (proposer → accepter / refuser), le signalement d'imprévu **PRÉVIENT d'un fait
  subi** : « l'enfant EST malade », « je serai en retard ce soir ». Il n'y a **rien à accepter**, juste
  à **informer** les autres acteurs.
- **`SignalerImprevu(type, jour, enfant[, motif])`** (agrégat `Imprevu`, `type ∈ {malade, retard}`,
  **motif optionnel**) **consigne un événement au JOURNAL DE CHANGEMENTS existant** (`IJournalChangements`,
  brique A) `{type, jour, enfant, acteur signalant, horodatage `IDateTimeProvider`}` — **AUCUN store neuf**.
- **Invariant s47 tenu et explicitement prouvé** *(Sc.1, Sc.4)* : signaler un imprévu **ne touche JAMAIS
  la résolution** — **aucune surcharge écrite** (store des surcharges **intact**), **aucun transfert
  dérivé**, **aucune bascule de responsable**, **case STRICTEMENT inchangée**. Le journal reste **trace
  de lecture non-autorité**, jamais lu par la résolution.
- **Cas limite** *(Sc.3)* : **motif vide accepté** (aucune écriture partielle) ; jour hors fenêtre de
  grille chargée enregistré sans crash ; comportement identique **InMemory + Mongo durable**.
- **Cas erreur** *(Sc.4)* : **type d'imprévu INCONNU refusé AVANT écriture** (règle dans l'agrégat
  `Imprevu`, aucun événement consigné).
- **Surface — AUCUNE surface neuve** : entrée **« signaler un imprévu » du menu clic-case** (Parent-gated,
  à côté de « déléguer ce jour » s44 / « proposer un échange » s47) → **mini-dialog malade / retard +
  motif optionnel**, **Échap = Annuler** (port `IEcouteurEchapModal` s33), émission par le **canal
  d'écriture**. Restitution dans la **cloche s47** : notification **INFORMATIVE** (« X est malade le 12 »,
  « Y sera en retard le 12 »), lu/non-lu + marquer-lu. En s48 la notif était **SANS aucune action de
  suivi** *(Sc.6)* — pas d'accepter / refuser : l'imprévu informatif n'est **pas négociable** (c'est ce
  qui le distingue de l'échange brique B).
  > **Amendement s51 (brique E ci-dessous).** La notif d'imprévu **porte désormais une action de suivi
  > « proposer un échange »**. Cet ajout **NE contredit PAS** la borne s48 : l'imprévu **reste un FAIT
  > informatif non-négocié** (le modèle `Imprevu` n'est ni muté, ni « résolu », ni rendu actionnable).
  > On ne rend **pas l'imprévu négociable** ; on **greffe À CÔTÉ**, en réaction, une **proposition
  > d'échange DISTINCTE** (brique B, modèle `Proposition` séparé). L'imprévu informe, la proposition
  > négocie — deux modèles, deux événements.
- **Temps réel** *(Sc.7)* : la cloche d'un 2ᵉ écran **converge par reprojection client depuis la diffusion
  porteuse de payload** (`INotificateurChangement`), **0 GET sur push** ; la diffusion porte une **donnée
  de lecture** (ne déclenche aucune écriture — séparation des canaux tenue).
- **Hors scope s48** (backlog) : **action de suivi / réaction** à un imprévu (proposer un échange déclenché
  depuis la notif — dépend de l'échange brique B) ; **multi-enfants / plage / récurrence** (un imprévu =
  **un jour, un enfant**) ; notifications **push / e-mail externes**.

## Brique D — Digest « immédiat » dans la cloche (qui récupère ce soir + à venir) *(livré s50)*

- **Réintégration DANS la cloche** du contenu de LECTURE retiré en s44 — la carte « qui récupère ce
  soir » (s42) et le panneau « À venir » (s43) — **sans le re-poser sur la grille**. Le digest est une
  **SECTION de LECTURE permanente EN TÊTE du panneau déroulant de la cloche**, **au-dessus du flux
  chrono lu/non-lu** (brique A), qui reste rendu **inchangé en dessous**. **Complète le palier
  « Immédiat & événements à venir »** ([`sequence-de-livraison.md`](sequence-de-livraison.md)) : la
  cloche porte désormais **et** les changements horodatés **et** l'état « immédiat + à-venir ».
- **`DigestImmediatQuery` = query PURE de COMPOSITION** réemployant `GrilleAgendaQuery` (records
  `ResponsableDuJour` / `TransfertDuJour` / `JourDigest` / `DigestImmediat`, **miroir** des
  ex-`CarteDuJourQuery` s42 / `AVenirQuery` s43) : **(a)** « qui récupère aujourd'hui / ce soir » =
  responsable résolu **surcharge > fond > neutre** + **où** (slot s29) + **transfert** éventuel (saisi
  OU dérivé s31) ; **(b)** « transferts à venir » des **N prochains jours de la fenêtre chargée**, en
  ordre **chronologique croissant**. **AUCUN store neuf, AUCUNE mutation** — de LECTURE pure, identique
  **InMemory + Mongo durable**.
- **Replis fidèles** *(Sc.3)* : jour sans responsable résolu = « personne assignée » (aucun nom
  fantôme) ; responsable **orphelin** (id absent du référentiel) = repli **neutre** sans nom (R6 /
  `Resolvable` s13) ; jour sans transfert **absent** de la liste (ni ligne vide) ; jour sans slot
  restitué **sans lieu**. **Fenêtre vide / jour courant hors-fenêtre** *(Sc.4)* = section vide neutre,
  **sans crash** ; **store des surcharges STRICTEMENT intact** (invariant 0-mutation prouvé sur les
  deux adaptateurs) — la query est de LECTURE, aucune case altérée.
- **Surface — section en tête du panneau cloche, lecture STRICTE** *(Sc.5, Sc.8)* : **aucun bouton,
  aucune action, aucune entrée cliquable** ; **Parent-gated** (Invité ne voit pas le digest ; rien sur
  `/connexion`). **Aucune carte / aucun panneau réintroduit sur la page `/planning`** —
  **anti-cliquet s44 tenu** : la grille reste la **seule surface de lecture SUR la page**, le digest
  vit **uniquement dans la cloche** (surface hors-grille assumée depuis s47).
- **Reprojection client, 0 GET dédié** *(Sc.6)* : un **état client partagé** (`EtatDigestPartage` — la
  grille publie la fenêtre chargée, la cloche s'y abonne) fait **reprojeter le digest depuis la donnée
  déjà chargée par l'unique GET grille** ; **aucun GET « digest »**. **Convergence temps réel**
  *(Sc.7)* d'un 2ᵉ écran par **reprojection client depuis la diffusion porteuse de payload**
  `INotificateurChangement` (brique A), **0 GET sur push** (garde-fou anti-flake
  [[flake-signalr-blast-radius]]).
- **Limitation assumée (héritage s42/s43, routée backlog)** : le digest se reprojette depuis la
  **fenêtre de grille chargée** — naviguer vers une semaine ne contenant PAS le jour courant fait
  **disparaître la section « aujourd'hui »** et borne les « à-venir » à la fenêtre. L'arbitrage
  **persistance hors-fenêtre vs coût GET/flake n'est PAS rouvert** (aucun GET dédié sur navigation).

## Brique E — Action de suivi sur un imprévu : proposer un échange en réaction *(livré s51)*

- **Ferme la boucle ouverte s48** : un imprévu (malade / retard, brique C) était **purement informatif** dans
  la cloche, sans moyen d'y **réagir**. La brique E greffe une **action de suivi** sur la notif d'imprévu :
  depuis « Léa est malade le 29/06 », un parent **propose direct un échange** à un autre acteur, sans quitter
  la notification. Elle **raccorde deux flux déjà livrés mais cloisonnés** : signaler un fait (brique C, s48)
  et négocier une réaffectation (brique B, s47).
- **GARDE DE DISTINCTION (non négociable).** L'imprévu **LUI-MÊME reste un FAIT informatif non-négocié**
  (modèle `Imprevu` s48 : consigné au journal, jamais lu par la résolution, **sans** état pending /
  accepté / refusé). L'action de suivi est une **PROPOSITION D'ÉCHANGE DISTINCTE** (modèle `Proposition`
  s47, brique B) **greffée en réaction** — elle **ne mute pas** l'imprévu, ne le « résout » pas, ne lui
  ajoute aucun statut. **Les deux modèles restent séparés** : l'imprévu **informe**, la proposition
  **négocie**. Signaler « malade » **puis** proposer un échange = **deux événements distincts** au journal.
- **`ProposerEchangeSuiteImprevu` = use case de COMPOSITION** (`ProposerEchangeSuiteImprevuHandler`, endpoint
  `/api/canal/proposer-echange-suite-imprevu`) : **lit l'imprévu au journal** (jour + enfant **hérités**),
  puis **délègue à `ProposerEchange` s47** (brique B) avec le `versActeur` choisi. **AUCUN modèle / store
  neuf** — réemploi INTÉGRAL de `Proposition` s47 (+ ses ports / store) et du journal s48.
- **Invariants prouvés** *(Sc.1–Sc.4)* : proposer (même depuis un imprévu) crée un `pending` **SANS écriture**
  (store des surcharges **INTACT**, case inchangée tant que non acceptée — invariant s47) ; l'imprévu
  d'origine reste **consigné au journal, inchangé** ; **ACCEPTER compose la délégation s44** (surcharge +
  transfert bicolore auto-dérivé s31, R24), **REFUSER sans écriture** ; **soi-même / délégataire inconnu /
  orphelin refusés AVANT écriture** (aucune écriture partielle) ; **ré-proposition last-write-wins R11** sans
  doublon ; **jour hors fenêtre chargée sans crash**. Identique **InMemory + Mongo durable**.
- **Surface — AUCUNE surface neuve** *(PORTE DE CONCEPTION arbitrée AU CADRAGE)* : l'action **« proposer un
  échange » vit DANS la notif d'imprévu de la cloche** (entrée **contextuelle** portant déjà le jour +
  l'enfant), qui **pré-remplit** la mini-dialog « proposer un échange » s47 (`ProposerEchangeDialog`) — le
  parent n'a plus qu'à choisir le `versActeur`. **Parent-gated** (Invité inerte, ne voit ni cloche ni
  action) ; commande émise par le **canal d'écriture** ; **Échap = Annuler** (port `IEcouteurEchapModal`
  s33). *(Alternatives écartées au cadrage : bouton sur la case de la grille — anti-cliquet s44 ; nouvelle
  entrée du menu clic-case — redondante avec « proposer un échange » s47 et décorrélée du contexte imprévu ;
  transformer la notif d'imprévu en notif actionnable — violerait la garde de distinction.)*
- La **proposition greffée** est **ACTIONNABLE** (Accepter / Refuser) chez le recevant dans **sa** cloche
  (brique B) ; Accepter compose la délégation → la case du jour converge. **Temps réel** *(Sc.7)* : la cloche
  d'un 2ᵉ écran **converge par reprojection client** depuis la **diffusion porteuse de payload**
  (`INotificateurChangement` s47), **0 GET sur push** (garde anti-flake [[flake-signalr-blast-radius]]).
- **Hors scope s51** (backlog) : **réaction autre qu'un échange** (déléguer unilatéralement / « annuler ma
  garde » depuis la notif) ; **imprévu / échange greffé sur PLAGE / série / multi-enfants** (borne s48 :
  un imprévu = **un jour, un enfant** — la proposition hérite du jour+enfant unique) ; notifications **push /
  e-mail externes**.

## Règles de gestion (catalogue : `regles-de-gestion.md`)

- **Journal = trace de lecture non-autorité** : le journal de changements n'est **jamais** lu par la
  résolution ; la vérité de résolution reste **périodes / transferts** (surcharge > fond > neutre,
  transferts dérivés s31).
- **Proposer n'écrit rien** : une Proposition `pending` **n'altère ni le store des surcharges ni la
  résolution** ; seul le **consentement** (accepter) déclenche l'écriture (composition s44).
- **Diffusion porteuse de payload ≠ écriture par la diffusion** : la diffusion porte une **donnée de
  lecture** ; l'écriture reste exclusivement sur le canal requête / réponse (invariant de séparation
  des canaux tenu).
- **R11 (last-write-wins)** appliquée à la ré-proposition ; **R24 / R25** (transfert dérivé /
  ponctuel) réutilisées par l'accord — textes canoniques dans
  [`periodes-et-cycle-de-fond.md`](periodes-et-cycle-de-fond.md).
- **Digest « immédiat » = LECTURE pure** *(s50)* : `DigestImmediatQuery` **compose** `GrilleAgendaQuery`
  **sans store neuf ni mutation** ; réintégré **dans la cloche**, jamais re-posé sur la grille
  (**anti-cliquet s44** tenu). La reprojection client (fenêtre grille + diffusion porteuse de payload)
  n'émet **aucun GET dédié**.

## Risques / hors-scope (backlog)

- **Échange borné à UN jour ponctuel** : plage `[J1..J2]` (s45), récurrence / série (D2) et
  **multi-enfants** restent ouverts.
- **Cloche in-app uniquement** : notifications **push / e-mail externes** hors scope (la diffusion est
  temps réel SignalR in-app).
- **Signalement d'imprévu dédié** (malade / retard) distinct de l'échange : **livré s48** (brique C
  ci-dessus, informatif). L'**action de suivi** (proposer un échange en réaction à un imprévu) est
  **livrée s51** (brique E ci-dessus, composition de la brique B ; l'imprévu reste informatif, la
  proposition greffée est un modèle distinct). Reste ouverte la **réaction autre qu'un échange**
  (déléguer / annuler depuis la notif) et l'imprévu / échange greffé sur **plage / série / multi-enfants**.
- **Digest « immédiat » dans la cloche** (qui récupère ce soir + à venir) : **livré s50** (brique D
  ci-dessus). **Reste** : le digest **PERSISTANT hors de la fenêtre de grille chargée** (arbitrage
  persistance vs coût GET/flake non rouvert — limitation héritée s42/s43).

Cf. [`risques-et-questions-ouvertes.md`](risques-et-questions-ouvertes.md) et le backlog
[`../BACKLOG.md`](../BACKLOG.md) (Épics 9 & 11).
