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

> **⚠️ DÉCISION PO gate G3 s44 — RETRAIT des surfaces de LECTURE s42 + s43 (rien ne se perd) — ACTÉ & LIVRÉ s44.**
> Au gate visuel de s44, le PO a tranché (2 retours successifs) : **il ne veut QUE la grille agenda + l'action
> sur la case**, plus aucune surface de lecture « carte du jour » / « à venir ». En conséquence, **s42
> (carte « Aujourd'hui ») ET s43 (panneau « À venir ») sont RETIRÉS ENTIÈREMENT** — composants IHM **et**
> read models backend `CarteDuJourQuery` (s42) / `AVenirQuery` (s43) + leurs tests (**pas de code mort**) —
> **fait au Sc.7 ✅**. **Raison PO** : le noyau produit « qui récupère » se lit **directement sur la grille agenda**
> (socle `GrilleAgendaQuery`, conservé) ; les deux briques de lecture dédiées faisaient doublon avec la grille et
> n'étaient plus voulues. **Conséquence épic NOYAU PRODUIT** : les incréments « carte du jour » (s42) et « à venir »
> (s43) précédemment marqués LIVRÉS **ne sont plus dans le produit** ; l'unique surface de lecture est la
> grille agenda ; la 1ʳᵉ ÉCRITURE (délégation) est portée sur la grille. Les candidats « carte/liste
> persistante hors semaine » et « à-venir au-delà de la fenêtre » (limitations s42/s43) **tombent** avec
> le retrait de ces surfaces.

> **✅ AMENDEMENT s47 — la décision « grille = SEULE surface » (s44) est AMENDÉE : la CLOCHE (barre du haut) est une
> surface hors-grille ASSUMÉE, alignée à la vision v1.** Le PO a **rouvert explicitement** en G2 s47 la surface
> cloche / notifications qu'il avait fait retirer en s44. La décision s44 **tenait pour les surfaces de LECTURE du
> planning redondantes avec la grille** (carte du jour s42, panneau « À venir » s43 — toujours retirées, ne pas
> réintroduire sans arbitrage). Elle **NE couvre PAS** la cloche : la cloche **n'est pas une re-lecture du planning**,
> c'est une **surface de NOTIFICATION de CHANGEMENT** (paliers cloche/imprévu de la vision), **assumée hors-grille**,
> désormais **DANS LA BARRE D'APPLICATION du haut** (MainLayout, ordre : déconnexion — cloche — sombre), gatée
> connecté && Parent (rien sur `/connexion` ni Invité). **Le noyau de LECTURE du planning reste la grille agenda ;
> la cloche s'y ajoute comme surface transverse.** Backlog et spec (`saisie-et-grille.md`, `notifications-et-echange.md`)
> alignés sur cet amendement — plus de contradiction « seule surface ».

**s48 `imprevu-malade-retard` MERGÉ — SIGNALEMENT D'IMPRÉVU DÉDIÉ, cas NON-négocié / purement INFORMATIF (palier 15).**
Complète l'échange consenti s47 (négocié, actionnable) par le cas **subi** : « l'enfant EST malade », « je serai en retard
ce soir » — un **fait qu'on PRÉVIENT**, pas qu'on négocie. **AUCUNE surface neuve, AUCUN store neuf** (garde surface arbitrée
AU CADRAGE : réutilise entrée menu clic-case + cloche s47 + journal `IJournalChangements` + diffusion porteuse de payload
`INotificateurChangement`, 0 rework G3). **@back** : signaler un imprévu `{type: malade|retard, jour, enfant, acteur signalant,
horodatage IDateTimeProvider}` **consigne au JOURNAL s47 SANS TOUCHER LA RÉSOLUTION** — invariant s47 tenu et **explicitement
prouvé** (store des surcharges INTACT, aucune surcharge écrite, aucun transfert dérivé, aucune bascule de responsable, case
STRICTEMENT inchangée, journal jamais lu par la résolution). Flux notifications trié par récence, **lu/non-lu PAR utilisateur**
+ compteur, **marquer-lu idempotent**. Cas limite : **motif optionnel vide accepté**, jour hors fenêtre chargée sans crash,
prouvé sur **deux adaptateurs InMemory + Mongo durable**. Cas erreur : **type d'imprévu inconnu REFUSÉ AVANT écriture** (règle
dans l'agrégat `Imprevu`, aucune écriture partielle). **@ihm** : entrée **« signaler un imprévu » du menu clic-case**
(Parent-gated, à côté de « déléguer ce jour » s44 / « proposer un échange » s47), mini-dialog **malade/retard + motif optionnel**,
**Échap = Annuler** (port `IEcouteurEchapModal` s33), émission par le **canal d'écriture** ; la notif apparaît dans la **cloche s47
INFORMATIVE** (« X est malade le 12 » / « Y sera en retard le 12 »), lu/non-lu + marquer-lu, **SANS action de suivi** (pas
d'accepter/refuser — non négociable, non-négligeable : c'est ce qui la distingue de l'échange s47) ; **temps réel** — la cloche
d'un 2ᵉ écran converge **par reprojection client depuis la diffusion porteuse de payload, 0 GET sur push** (garde anti-flake
[[flake-signalr-blast-radius]] respectée). **7/7 ✅**, gate G3 validé PO, **aucun retour produit** au gate. **Le candidat de
tête « signalement d'imprévu (malade/retard) » est LIVRÉ.** **Hors scope s48** (backlog) : **action de suivi / réaction** à un
imprévu (proposer un échange déclenché depuis la notif — dépend de l'échange s47 déjà livré), **notifications push / e-mail
externes** (cloche in-app SignalR), **multi-enfants / plage / récurrence** du signalement (un imprévu = un jour, un enfant).
**Candidats de tête au prochain `/planning`** : **action de suivi sur imprévu** (proposer un échange en réaction), **palier 9
sélection de plage** (tranche 2, s49), **délégation récurrente/série** (D2), échange sur une **plage** / **multi-enfants**, reste
Config foyer (édition depuis le graphe, graphe étendu, arbitrage inline vs modal, liste de slots par activité, lien adresse
acteur↔lieu, suppression slot récurrent IHM, suppression d'un enfant) ; **P0 auth** (Google OAuth réel + écran
définir-mot-de-passe).

Précédent = **s47 `echange-proposition-accord` MERGÉ — attaque les paliers CLOCHE (11/14) + IMPRÉVU & ÉCHANGE (12/15) de la vision.**
Sprint le plus lourd de la série (2 modèles neufs + surface + port de transport). **Brique A — CLOCHE GÉNÉRALE (palier
11/14 « Immédiat & événements à venir »)** : read model d'événements de changement = **JOURNAL DE CHANGEMENTS append-only**
derrière un port neuf **`IJournalChangements`**, **alimenté par CHAQUE handler d'écriture existant** (délégation s44, plage
s45, reprise s46, transfert s31) qui y consigne `{type, jour/enfant, cédant/recevant, horodatage via IDateTimeProvider}` —
**TRACE DE LECTURE horodatée, JAMAIS lue par la résolution** (non-autorité, pas de double vérité : la vérité de résolution
reste périodes/transferts). Capte les **reprises s46 malgré la suppression** (indérivable de l'état courant → journal
persisté nécessaire). **État LU / NON-LU PAR utilisateur** persisté (port **`IEtatLectureNotifications`**) + **compteur**,
**marquer-lu idempotent**. **2 adaptateurs InMemory + Mongo durable** pour chaque port. **IHM** : cloche + **badge compteur**
+ **panneau déroulant** (liste chrono, lu/non-lu, marquer-lu, Échap ferme), **DANS LA BARRE DU HAUT** (MainLayout), composant
autonome, **gating connecté && Parent** (rien sur `/connexion` ni Invité). **Brique B — ÉCHANGE PROPOSITION → ACCORD (palier
12/15 « Imprévu & échange », flux consenti)** : `ProposerEchange(jour, enfant, versActeur)` crée une **Proposition `pending`**
(notif chez le recevant) **SANS écrire de surcharge ni changer la résolution** (anti vert-qui-ment prouvé : store des
surcharges intact, case inchangée tant que non acceptée) ; **`AccepterProposition` COMPOSE la délégation s44** (surcharge du
jour + transfert bicolore auto-dérivé s31, R24) → `accepté` ; **`RefuserProposition` retire SANS écriture** → `refusé`. **Cas
limite / erreur** : soi-même refusé sans écriture · délégataire inconnu/orphelin refusé **AVANT** écriture · ré-proposition
**last-write-wins R11** sans doublon · jour hors fenêtre sans crash. **IHM** : notification d'échange **ACTIONNABLE dans la
cloche** (Accepter / Refuser via mini-dialog) + entrée **« proposer un échange » du menu clic-case** (Parent-gated) ; **PLUS
de badge sur la case NI d'entrée conditionnelle** pour répondre (réponse dans la cloche). **Transport temps réel (décision
archi tranchée en cours de sprint, SM)** : diffusion SignalR **PORTEUSE DE PAYLOAD** via port neuf **`INotificateurChangement`**
(journal décoré `JournalChangementsDiffusant` portant `EvenementChangementSnapshot`), branchée sur **chaque endpoint
d'écriture** ; la cloche **reprojette depuis la diffusion → 0 GET sur push** (prouvé Sc.4 & Sc.9, garde-fou anti-flake
[[flake-signalr-blast-radius]] respecté). **NE viole PAS « diffusion = lecture seule »** : la diffusion porte une donnée de
LECTURE (snapshot d'un changement déjà écrit), l'écriture reste exclusivement sur le canal requête/réponse. **9/9 ✅**, suite
complète **809/812** (3 skip baseline), **gate G3 validé PO** (y compris le **repositionnement de la cloche en barre du haut**).
**Aucun retour produit nouveau** au gate. **La décision « grille = seule surface » (s44) est AMENDÉE** (voir ⚠️ ci-dessus).
**2 escalades de conception BIEN gérées** (journal dérivé-vs-persisté Sc.1 ; transport 0-GET Sc.4/9 : la dev-team a escaladé
AVANT de coder, le SM a tranché — comportement voulu). **Hors scope s47** (backlog) : échange sur une **PLAGE `[J1..J2]`**
(s45, sprint borné à UN jour), échange **récurrent/série** (D2) & **multi-enfants**, **notifications push / e-mail externes**
(la cloche est **in-app** temps réel SignalR). **Candidats de tête au prochain `/planning`** : échange sur une plage /
multi-enfants, **signalement d'imprévu** (malade/retard, entrée dédiée), notifications push externes ; reste Config foyer
(édition depuis le graphe, graphe étendu, arbitrage inline vs modal, liste de slots par activité, lien adresse acteur↔lieu,
suppression slot récurrent IHM, suppression d'un enfant) ; **P0 auth** (Google OAuth réel + écran définir-mot-de-passe).

**s46 `annuler-reprendre-delegation` MERGÉ — FERME la boucle *undo* laissée ouverte en s44/s45** : un parent qui a délégué la
récupération d'un jour peut **reprendre ce jour** (« finalement je peux récupérer »). **AUCUN modèle / store / commande neuf,
AUCUNE surface neuve.** **@back** : use case `AnnulerDelegation(jour[, enfant])` de **COMPOSITION** — **compose la SUPPRESSION de
surcharge EXISTANTE (s16)** → la case retombe sur le **FOND (cycle)**, le **transfert bicolore dérivé s31 DISPARAÎT** (re-dérivé
de la résolution, jamais réécrit) ; deux adaptateurs InMemory + Mongo durable, prouvé store réel. **Cas limite** : jour **sans
délégation active** → **no-op idempotent** (store intact), **ré-annulation idempotente**, **last-write-wins R11** sans doublon ni
jour tiers touché. **GRANULARITÉ = UNE OCCURRENCE** : reprendre **le seul jour cliqué** **même s'il fait partie d'une plage s45** —
les segments restants `[J1..J-1]`/`[J+1..J3]` réécrits par le **chemin période s06**, transferts dérivés **recalculés** (le trou
produit ses propres bascules) ; **PAS** toute la série d'un coup (reprendre une plage = jour par jour). **@ihm** : **entrée
conditionnelle « reprendre ce jour » du menu clic-case EXISTANT** (à côté de « déléguer ce jour » s44), **visible seulement si la
case porte une délégation active** (`JourCase.PorteSurcharge`, surface en lecture), **absente** sinon ; **Parent-gated** (Invité ne
voit ni menu ni entrée), **mini-dialog de confirmation Échap = Annuler** (port s33), émission par le **canal d'écriture**
`annuler-delegation` ; **convergence temps réel** de la case sur 2ᵉ écran par **reprojection client SignalR, 0 GET**. **PORTE DE
CONCEPTION SURFACE arbitrée AU CADRAGE** (garde s44) : aucune surface neuve, **0 rework G3, 6/6 du 1ᵉʳ coup**, gate G3 validé PO,
**aucun retour produit**. Suite **784/787** (3 skip baseline). **La boucle undo (hors scope s44/s45) est désormais FERMÉE.** **Hors
scope s46** (backlog) : action « reprendre toute la plage » d'un coup, **notifications** « X a repris » (Palier 11). **Candidats de
tête au prochain `/planning`** : **panneau cloche notifications/alertes push** (Palier 11), **délégation récurrente/série** (D2),
reste Config foyer (édition depuis le graphe, graphe étendu, arbitrage inline vs modal, liste de slots par activité, lien adresse
acteur↔lieu, suppression slot récurrent IHM, suppression d'un enfant) ; **P0 auth** (Google OAuth réel + écran
définir-mot-de-passe), **R3 « exactement 2 » à l'écriture** (non imposée, choix produit).

Précédent = **s45 `deleguer-plage-de-jours` MERGÉ — EXTENSION de la délégation s44 du JOUR UNIQUE à une PLAGE `[J1..J2]`** : l'imprévu
qui DURE (voyage, hospitalisation, « je pars du 20 au 25, X récupère les enfants »). **AUCUNE surface neuve, AUCUN modèle/
commande neuf** : le **mini-dialog « déléguer ce jour » EXISTANT (s44)** est **enrichi d'un champ « jusqu'au »** (date de
fin, **défaut = jour cliqué = parité s44 stricte** → délégation d'UN jour inchangée). **@back** : `DeleguerRecuperation(début,
fin, enfant, versActeur)` **COMPOSE l'écriture surcharge MULTI-JOURS `[début..fin]` via s06** (`s06` gère déjà une période) —
B prime (surcharge > fond) sur **chaque** jour ; **transferts bicolores AUTO-DÉRIVÉS s31** (R24) aux **DEUX frontières**
(entrée J1, sortie J2+1), jamais réécrits ; deux adaptateurs InMemory + Mongo durable. **Cas limite** : chevauchement →
**last-write-wins R11** sans doublon ; **`fin < début` (plage vide)** → refus AVANT écriture, store intact ; **soi-même** →
refus sans écriture ; **`fin` hors fenêtre chargée** → écriture valide sans crash. **Cas erreur** : **délégataire inconnu /
orphelin** → refus AVANT écriture, **aucune écriture partielle** (aucun jour de la plage écrit). **@ihm** : champ « jusqu'au »
Parent-gated, **Échap = Annuler** (port s33), **refus domaine → dialog reste ouverte + motif + saisie conservée (acteur ET
plage)** ; convergence temps réel de **TOUTES les cases de la plage** (nouveau responsable + transferts dérivés aux frontières)
sur 2ᵉ écran par **reprojection client SignalR, 0 GET**. **PORTE DE CONCEPTION SURFACE arbitrée AU CADRAGE** (garde s44) :
surface tenue (mini-dialog existant), **0 rework G3, 6/6 du 1ᵉʳ coup**, gate G3 validé PO. Suite **770/773** (3 skip). **Hors
scope (backlog)** : délégation **récurrente/série « tous les mardis »** (D2, distincte d'une plage contiguë), **sélection de
plage par DRAG sur la grille** (dépend du **palier 9 calendrier-navigable non livré**), ~~**annulation/undo dédié**~~ **LIVRÉ s46**
(`AnnulerDelegation` compose la suppression s16, retour au fond, granularité une occurrence), **notifications** « X a délégué » (Palier 11). **Candidats de tête au prochain
`/planning`** : **panneau cloche notifications/alertes push** (Palier 11), **délégation récurrente/série** (D2), reste Config
foyer (édition depuis le graphe, graphe étendu, arbitrage inline vs modal, liste de slots par activité, lien adresse
acteur↔lieu, suppression slot récurrent IHM, suppression d'un enfant) ; **P0 auth** (Google OAuth réel + écran
définir-mot-de-passe), **R3 « exactement 2 » à l'écriture** (non imposée, choix produit).

Précédent = **s44 `deleguer-recuperation-jour` MERGÉ — 1ʳᵉ ÉCRITURE du NOYAU PRODUIT « qui récupère »** : un parent qui ne peut
pas récupérer un jour en **délègue la récupération à un autre acteur pour CE jour-là** (imprévu / échange de dernière
minute, **UN jour ponctuel**). **@back** : use case `DeleguerRecuperation(jour, enfant, versActeur)` de **COMPOSITION**
— expose l'**écriture surcharge ponctuelle EXISTANTE** (période d'UN jour, s06) avec le délégataire responsable, **aucun
modèle/commande/store de transfert neuf** ; le **transfert cédant→recevant** est **AUTO-DÉRIVÉ s31** (R24, bascule
fond→surcharge→fond, rendu bicolore réutilisé) ; deux adaptateurs InMemory + Mongo durable. Cas limite (**last-write-wins
R11** sans doublon, **refus soi-même** sans écriture, jour hors fenêtre écrivable sans crash) ; cas erreur
(**délégataire inconnu/orphelin → refus AVANT écriture**, store intact). **@ihm** : entrée **« déléguer ce jour » du
menu clic-case** de la grille (à côté d'« Affecter une période » / « Définir un transfert ») → mini-dialog
**Parent-gated** (Invité ne voit ni menu ni entrée), **Échap = Annuler** (port s33), refus domaine → dialog reste
ouverte + motif + saisie conservée, émission par le **canal d'écriture** ; **convergence temps réel de la CASE** (nouveau
responsable + transfert bicolore dérivé) sur 2ᵉ écran par **reprojection client SignalR, 0 GET**. **RETRAIT s42/s43
acté au Sc.7** (voir ⚠️ ci-dessus). Sprint fortement pivoté au gate G3 (surface carte→menu clic-case, puis retrait
panneau, puis retrait carte). **7/7 ✅**, gate G3 validé PO. **Hors scope (backlog)** : délégation **récurrente/série**
(D2), **notifications** « X a délégué » (Palier 11). **Candidats de tête au prochain `/planning`** : **panneau cloche
notifications/alertes push** (Palier 11), **délégation récurrente/série** (D2), reste Config foyer (édition depuis le
graphe, graphe étendu, arbitrage inline vs modal, liste de slots par activité, lien adresse acteur↔lieu, suppression
slot récurrent IHM, suppression d'un enfant) ; **P0 auth** (Google OAuth réel + écran définir-mot-de-passe), **R3
« exactement 2 » à l'écriture** (non imposée, choix produit).

Précédent = **s43 `liste-a-venir`** *(RETIRÉ du produit s44 — voir ⚠️ ci-dessus)* — **2ᵉ incrément du NOYAU PRODUIT** (prolonge la
carte du jour s42) : un **panneau « À venir »** sous la carte « Aujourd'hui », **STRICTEMENT LECTURE**, qui restitue
pour les **N prochains jours de la fenêtre de grille chargée** + l'enfant sélectionné : **QUI** récupère (résolu
surcharge>fond>neutre), **OÙ** (slots s29), **transfert** éventuel (saisi OU dérivé s31). **@back** : query PURE
`AVenirQuery` **COMPOSANT `GrilleAgendaQuery`** — **miroir strict de `CarteDuJourQuery` s42** itérée sur les jours à
venir, **SANS réimplémenter la résolution/dérivation/projection, AUCUN store neuf, AUCUNE mutation**, deux adaptateurs
(InMemory + Mongo durable) ; repli fidèle (aucun responsable = « personne assignée » sans fantôme ; orphelin = repli
neutre sans nom, `Resolvable` s13 ; jour sans transfert = unicolore ; sans slot = sans lieu ; **fenêtre sans à-venir =
liste vide / message neutre**). **@ihm** : panneau sous la carte du jour (date croissante, qui/où/transfert par jour),
strictement lecture ; **Invité VOIT** ; **reprojection client** depuis la grille chargée ; convergence **SignalR par
reprojection client** (0 GET sur push, anti-amplification flake s42). **LIMITATION assumée (même que s42, routée au
backlog)** : au-delà de la fenêtre de grille chargée, les à-venir ne sont pas affichés (arbitrage liste persistante GET
vs flake non tranché). 5/5 ✅, suite **768/768**, gate G3 validé PO (validation directe, aucun rework ni retour produit
nouveau). **État épic NOYAU PRODUIT « qui récupère »** : carte du jour (s42) + à-venir prochains jours (s43) **LIVRÉS
en LECTURE** ; **reste** = **notifications/alertes push** (cloche « changement », Palier 11), **carte/liste PERSISTANTE
hors semaine chargée** (limitation s42/s43), **à-venir au-delà de la fenêtre chargée**. **Candidats de tête au prochain
`/planning`** : **imprévu & échange de dernière minute / transferts temporaires** (Palier 12, noyau produit à forte
valeur — **ÉCRITURE**), **panneau cloche notifications/alertes push** (Palier 11, incrément suivant annoncé hors scope
s42/s43), **carte/liste du jour persistante hors semaine courante** (limitation s42/s43, persistance vs coût GET/flake) ;
reste Config foyer : **édition depuis le graphe**, **graphe étendu**, **arbitrage inline vs modal** (tension s32), **liste
de slots par activité**, **lien adresse acteur↔lieu**, **suppression slot récurrent IHM**, **suppression d'un enfant** ;
**P0 auth** (Google OAuth réel + écran définir-mot-de-passe), **R3 « exactement 2 » à l'écriture** (non imposée, choix
produit). Précédent = **s42 `qui-recupere-ce-soir-carte-du-jour`** *(RETIRÉ du produit s44 — voir ⚠️ ci-dessus)* — **PIVOT assumé hors
Config foyer vers le NOYAU PRODUIT** (après 9 incréments consécutifs Config foyer s32→s41) : **1ᵉʳ incrément du
noyau produit** = la carte **« Qui récupère ce soir »** (jour courant, **STRICTEMENT LECTURE**), payoff de tout
le foyer configuré. **@back** : query `CarteDuJourQuery` **PURE composant** `GrilleAgendaQuery` (responsable
résolu **surcharge>fond>neutre**, slots de localisation du jour s29, transfert cédant→recevant **saisi OU dérivé
s31**) — **SANS réimplémenter la résolution, AUCUN store neuf, AUCUNE mutation**, deux adaptateurs, durable Mongo ;
repli fidèle (aucun responsable = « personne assignée » sans fantôme ; orphelin = repli neutre sans nom, `Resolvable`
s13 ; jour sans transfert = unicolore ; sans slot = sans lieu). **@ihm** : carte « Aujourd'hui » en tête du planning
(**reprojection client** depuis la grille chargée), strictement lecture ; **Invité VOIT** la carte ; convergence
**SignalR par reprojection client** (0 GET sur push, anti-amplification flake). **LIMITATION assumée routée au
backlog** : la carte se reprojette depuis la **fenêtre de grille chargée** — si l'utilisateur navigue vers une
semaine ne contenant PAS le jour courant, la carte **disparaît** (choix guidé par l'anti-amplification flake, aucun
GET dédié sur push ; à arbitrer si le PO veut la carte PERSISTANTE hors de la vue du jour courant). 5/5 ✅, suite
**752/752**, gate G3 validé PO (validation directe, aucun rework ni retour produit nouveau). **Candidats de tête au
prochain `/planning`** (suite du NOYAU PRODUIT) : **panneau cloche multi-événements / à-venir** (Palier 11, incrément
suivant annoncé hors scope s42), **carte du jour persistante hors semaine courante** (limitation s42, persistance vs
coût GET/flake), **imprévu & échange de dernière minute** (Palier 12) ; reste Config foyer : **édition depuis le
graphe**, **graphe étendu**, **arbitrage inline vs modal** (tension s32), **liste de slots par activité**, **lien
adresse acteur↔lieu**, **suppression slot récurrent IHM**, **suppression d'un enfant** ; **P0 auth** (Google OAuth
réel + écran définir-mot-de-passe), **R3 « exactement 2 » à l'écriture** (non imposée, choix produit). Précédent =
**s41 `commandes-inverses-actif-admin`** : **débloque le sens OFF du
toggle actif/admin** (dette s33 SOLDÉE). **@back** : commande `DeDesignerAdmin` (agrégat `AdministrationFoyer`,
idempotent, acteur inconnu refusé sans mutation) + **borne « dernier admin »** (dé-désigner le seul admin refusé
AVANT écriture, foyer jamais sans admin) + commande `DesactiverCompte` (`Actif→Inactif` via
`CompteUtilisateur.Desactiver`/`IEditeurComptes`, idempotent, compte inconnu refusé, compte désactivé refuse la
connexion — garde s23 ; deux adaptateurs InMemory + Mongo durable). **@ihm** : **toggle actif/admin
BI-DIRECTIONNEL** (le OFF émet la vraie commande inverse, fin du verrou ON s33, plus de no-op silencieux) ; refus
« dernier admin » → modal reste ouverte + motif + toggles conservés, store intact ; Échap=Annuler ; Parent-gated
(R8/R9 préservé) + convergence SignalR (dé-désignation converge sur 2ᵉ écran, 0 GET). Un test s33 figeant le verrou
ON a été mis à jour (comportement RETIRÉ par le goal, évolution légitime). 6/6 ✅, suite **739/739**, gate G3 validé
PO (validation directe, aucun rework ni retour produit nouveau). **Candidats de tête au prochain `/planning`** :
**édition depuis le graphe**, **graphe étendu**, **arbitrage inline vs modal** (tension s32), **liste de slots par
activité**, **lien adresse acteur↔lieu**, **suppression slot récurrent IHM**, **suppression d'un enfant**, **édition
concurrente sous dialog**, **R3 « exactement 2 » imposée à l'écriture** (non imposée, choix produit). Précédent =
**s40 `completude-couple-badge`** (**7ᵉ incrément épic Refonte Config foyer**) : **STATUT de complétude du couple R3 par enfant, en LECTURE seule** — enum `StatutCoupleR3` composé **PUR**
depuis les liens s34 + rôle-du-lien s37, **enrichit `GrapheFoyerQuery` s38** (aucun store neuf, aucune mutation, aucun
blocage d'écriture). Règle : **complet** = père ET mère résolus ; **incomplet** = 0/1 parent, OU 2 sans le couple
père+mère (ex. deux « parent-libre ») ; **vide** = racine sans parent ; **orphelin exclu du décompte** (miroir
`Resolvable()` s13). **Badge lecture seule** sur le graphe (complet / incomplet / aucun parent), **R3 SIGNALÉE jamais
IMPOSÉE** (Sc.3 verrouille : aucun blocage d'écriture ajouté). Parent-gated lecture (Invité voit le badge) +
convergence **SignalR par reprojection client** (0 GET). **Rework gate (retour PO placement, absorbé en in-goal)** :
la vue graphe s38 + badges est **relocalisée dans un onglet « Foyer » placé EN PREMIER** (actif par défaut) de la
Config foyer — le graphe ne s'étale plus sur l'écran d'arrivée ; comportement strictement préservé. 5/5 ✅, suite
**715/715**, gate G3 validé PO (après rework de placement). **R3 STATUT signalé ; contrainte « exactement 2 » à
l'écriture toujours NON imposée** (choix produit tenu). Précédent = **s39 `retrofit-flake-signalr-tempsreel`** (**sprint de DETTE / INFRA
DE TEST**, pas de feature produit) : la dette flake SignalR *TempsReel* (candidat de tête depuis s36) est **SOLDÉE
À LA CAUSE** — baseline **36 % rouge** full-suite parallèle → **0 %** (12 runs), via collection xUnit
`SignalRTempsReelCollection` (`DisableParallelization=true`) **ciblée** sur les 55 `FrontWasm*TempsReel*` (**0
`src/` produit touché**, 2 courses de TEST démasquées neutralisées). **La dette flake ne domine PLUS la tête des
candidats.** Décision clôture : `-Serial` reste le défaut au gate, parallèle exercé au cycle TDD (concurrence
réelle fiable). **Candidats de tête au prochain `/planning`** (features produit ; **complétude du couple R3 SIGNALÉE en
lecture livrée s40** — le STATUT est affiché, la CONTRAINTE « exactement 2 » à l'écriture reste non imposée, choix
produit) : **édition depuis le graphe**, **graphe étendu**, **arbitrage inline vs modal**, **commandes
inverses actif/admin**, **liste de slots par activité**, **lien adresse acteur↔lieu**, **suppression slot récurrent
IHM**, **suppression d'un enfant**, **édition concurrente sous dialog**. Précédent = **s38 `graphe-foyer-enfant-racine`** (**6ᵉ incrément épic Refonte
Config foyer — RESTITUTION visuelle du foyer câblé s34/s36/s37**) : une **vue « graphe enfant-racine » en LECTURE
SEULE** à l'arrivée sur la Config foyer. **@back** : query agrégée **PURE** `GrapheFoyerQuery` restituant, PAR
enfant, ses **parents liés + rôle-du-lien** {père/mère/parent-libre} (s37) — **aucune mutation, aucun store neuf**,
**deux adaptateurs** (InMemory + Mongo), reflet **fidèle** des liens réels (parent non lié absent, acteur orphelin
sans nom fantôme, enfant sans parent = racine isolée, store vide = graphe vide). **@ihm** : chaque **enfant en
RACINE**, branches « **nom (rôle-du-lien)** », **familles recomposées R2 VISIBLES par construction** (deux racines,
parent partagé sous chaque enfant), store vide → message neutre, **Parent-gated lecture** (Invité voit la vue),
**convergence SignalR par REPROJECTION CLIENT** (un lien modifié dans la modal Enfants converge le graphe **sans
rechargement, 0 GET**). 5/5 ✅, suite **695/695** (`test.ps1 -Serial`), gate G3 **validé PO (validation directe,
aucun rework, aucun retour produit nouveau)**. **R1 (multi-enfants/graphe) désormais exercé en LECTURE ; R2
familles recomposées VISIBLE. Reste ouvert** : **R3 « exactement 2 parents »/complétude du couple**, **édition
depuis le graphe**, **graphe étendu** (grands-parents / parents liés entre eux / lien enfant↔activité), dette flake
*TempsReel* (candidat de tête). Précédent = **s37 `pere-mere-lien-enfant`** (**5ᵉ incrément épic Refonte
Config foyer, suite du lien s34/s36**) : le lien enfant↔parent porte un **attribut « rôle-du-lien » ∈
{père, mère, parent-libre}** (record `ParentLie`, commande `LierEnfantParent` enrichie, champ additif
`RolesDesLiens`, **persistance Mongo durable**, id enfant inchangé) distinguant les 2 parents jusque-là
séparés par le **seul NOM** ; **invariant d'exclusivité** (pas deux « père » ni deux « mère » sur un enfant,
refus AVANT écriture, store intact ; « parent-libre » **répétable** ; borne 0..2 s34 + éligibilité role-flag
s36 inchangées) ; **compat non destructive** (lien s34 sans attribut relu à « parent-libre », pas de migration
destructive). **IHM** : **sélecteur père/mère/parent** par parent lié dans la modal Enfants (refus → modal
ouverte + motif + sélection conservée, Échap = Annuler), colonne « Parents liés » = **« nom (rôle) »**,
Parent-gated + SignalR. 5/5 ✅, suite **682/682** (via `test.ps1 -Serial`, mode gate anti-flake s36 validé
en un run), gate G3 validé PO. **Reste (hors scope s37)** : familles recomposées **R2/R3** (« exactement 2
parents ») + graphe enfant-racine, dette flake *TempsReel* (candidat de tête). Précédent = **s36
`eligibilite-parent-type-acteur`** (**révision
d'invariant, retour PO gate s35 — goal 0 PRIORITAIRE**) : **qu'est-ce qui rend un acteur liable comme
parent** est redéfini. **PIVOT en cours de sprint** : l'**option A** (éligibilité = `TypeActeur.Parent`)
a été **livrée puis REJETÉE au gate** (bug structurel : `TypeActeur` **jamais saisi via l'IHM** →
`TypeParDefaut = Parent` → **tout acteur créé devenait liable à tort**, Valérie/nounou & Mamie/grand-parent),
**ré-arbitrée en G2 vers B1+B2 (role-flag)** et refaite. **LIVRÉ (role-flag)** : chaque **`RoleFoyer` porte
un flag booléen « est rôle parent »** (`EstRoleParent`, ports `IEnumerationRoles` / `IEditeurReferentielRoles.
MarquerParent`, persistance **InMemory + Mongo durable**, défaut neutre false sur donnée antérieure) ;
commande/handler **`MarquerRoleParent`** (bascule idempotente, rôle inexistant refusé sans écriture) ; **seed
B2** pré-cochant **Papa/Maman/Parent** au seed initial (un rôle créé ensuite démarre non-parent — anti-piège
libellé s35) + seed démo affectant un rôle-parent aux acteurs-parents (Alice→Papa, Bruno→Maman) ;
**éligibilité du lien enfant↔parent = l'acteur porte un rôle marqué parent** (REMPLACE l'option A ; rôle non
marqué ou sans rôle = refusé sans écriture partielle ; borne 0..2 s34 inchangée) ; **`TypeActeur` reste
cantonné au seul gating impersonation R8/R9** (aucun droit d'écriture gagné/perdu). **IHM** : sélecteur modal
Enfants (`ActeursParents()`) filtré sur rôle marqué parent, **case « rôle parent »** dans la modal Rôles
(crayon→modal s33, Échap = Annuler, Parent-gated, SignalR — décocher retire l'acteur du sélecteur en temps
réel). 7/7 ✅, suite **665/665**, gate G3 validé PO. **Reste (hors scope s36)** : **champ père/mère distinct**
(distinction par le NOM), **familles recomposées R2/R3** + graphe enfant-racine, **saisie/édition du
`TypeActeur`** lui-même. Précédent = **s35 `lieux-vers-activites`** (**4ᵉ incrément vertical
de l'épic Refonte Config foyer**) : le référentiel **« Lieux » (s27) est renommé « Activités »**
(refactor sémantique iso-comportement Domaine + Application ; ports / query / handlers / deux
adaptateurs ; **validation de pose préservée** — slot sur activité inconnue refusé sans écriture ;
l'**axe LOCALISATION du slot** `LieuId` reste le « où » de la garde, **distinct, non renommé**) ;
l'activité porte désormais un **champ « adresse »** (Mongo durable, vide accepté, sans écriture
partielle, miroir adresse acteur s33) ; **lien enfant↔activité N-M** (commandes lier/délier
idempotentes, plusieurs enfants partagent une activité, rejets enfant/activité inconnu sans écriture
partielle, Mongo durable, id stables inchangés) ; l'**onglet Activités** est harmonisé au patron
**tableau lecture seule + crayon → modal** (**lot atomique de surface** : swap de l'ancien onglet
Lieux **inline** → tableau + modal, **même commit** incluant le renommage HTTP `lieux→activite` +
DTOs + record Web `LieuFoyer→ActiviteFoyer`), colonnes **libellé + adresse + « Enfants liés »**,
modal avec champ adresse + **sélecteur des enfants** à lier/délier, Échap = Annuler (port
`IEcouteurEchapModal` s33), **Parent-gated**, convergence **SignalR** lecture. 6/6 ✅, suite
**639/639**, gate G3 validé PO. **L'épic Refonte Config foyer a désormais Acteurs / Rôles / Cycle /
Enfants / Activités TOUS harmonisés** au patron tableau + crayon → modal. **Retour PO gate s35 (goal 0
PRIORITAIRE s36)** : révision de l'**éligibilité « parent »** du lien enfant↔parent (options A/B/C,
cf. bloc ⚠️ ci-dessous). **Reste (hors scope tenu s35)** : **liste de slots par activité** (récurrents/
non), **lien adresse acteur↔lieu/domicile** de l'enfant en garde, **révision de la validation de
pose**, familles recomposées **R2/R3** + graphe enfant-racine. Précédent = **s34 `enfants-lien-parent`**
(**3ᵉ incrément vertical de l'épic Refonte Config foyer**) : **lien enfant↔parent** — l'enfant, jusque-là agrégat **nu**
`EnfantFoyer(Id, Prénom)` sans aucun lien parent, porte désormais un **lien vers 1..2 parents-acteurs**
(commande **lier / délier** idempotente, règle **« 2 parents max »**, rejets **acteur inexistant /
non-parent / déjà lié SANS écriture partielle**, persistance **Mongo durable**, id enfant inchangé =
enrichissement pas recréation) ; l'**onglet Enfants** est harmonisé au patron **tableau lecture seule +
crayon → modal** (lot atomique de surface, **swap inline→modal**) avec **colonne « Parents liés »** en
lecture et un **sélecteur de parents** dans la modal ; invariants tenus (refus → **modal ouverte** +
motif + saisie conservée, **Parent-gated** Invité lecture seule, convergence **SignalR** lecture). 6/6 ✅,
suite **618/618**, gate G3 validé PO. **Traite la question PO du gate s33** (« lier un enfant à 2 parents »).
**Reste (hors scope tenu ce sprint)** : familles recomposées **R2/R3** (contrainte « exactement 2 parents »,
graphe foyer recomposé), **renommage Lieux → Activités** + lien enfant↔activité, **vue d'accueil = graph
enfant en racine**, suppression d'un enfant. Précédent = **s33 `refonte-config-acteurs-roles-cycle`** (**2ᵉ
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

> **✅ RÉSOLU s36 — éligibilité « parent » redéfinie en flag « rôle parent » (option B1+B2).** Le retour PO
> gate s35 (« Papa/Maman sont OBLIGATOIREMENT des rôles Parent, donc liables ») est **traité**. L'ancienne
> règle s34 (match du **libellé littéral** « Parent ») est **abandonnée** ; l'**option A** (éligibilité =
> `TypeActeur.Parent`) a été **livrée puis REJETÉE au gate** — **écartée par construction** car le
> `TypeActeur` n'est **jamais saisi via l'IHM** (`TypeParDefaut = Parent` → tout acteur créé était liable à
> tort). **Retenue = B1+B2** : flag booléen **« est rôle parent »** sur `RoleFoyer` (source de vérité unique,
> pilotable, persisté InMemory+Mongo), commande `MarquerRoleParent`, **pré-cochage Papa/Maman/Parent au seed**
> (un rôle créé ensuite = non-parent, anti-piège libellé), **éligibilité = l'acteur porte un rôle marqué
> parent**, `TypeActeur` **cantonné au gating R8/R9**. 7/7, 665/665, gate validé. Détail : « En cours »
> ci-dessus + spec `acteurs-et-config-foyer.md` (bloc s36) + Épic 1 ligne dédiée.
>
> **Candidats goal prochain `/planning`** (**dette flake *TempsReel* SOLDÉE À LA CAUSE s39** — voir ci-dessous,
> ne domine PLUS la tête ; **graphe enfant-racine + R2 familles recomposées lecture résolus
> s38** ; **rôle-du-lien père/mère résolu s37** ; Éligibilité parent **résolue s36** ; Activités harmonisées +
> renommage + lien enfant↔activité **livrés s35** ; Enfants + lien enfant↔parent **livrés s34** ; Acteurs 2ᵉ
> incr. + Rôles/Cycle **livrés s33**) :
> (0) ~~**[P1, dette flake — CANDIDAT DE TÊTE]** rétrofit du garde *TempsReel* SignalR~~ **SOLDÉE s39** :
> collection xUnit `SignalRTempsReelCollection` (`DisableParallelization=true`) **ciblée** sur les 55 classes
> `FrontWasm*TempsReel*` (pas de rideau : ~213 autres Web.Tests restent parallèles, `Tests`/`Api.Tests`
> inchangés) ; la sérialisation a démasqué **2 courses de convergence de TEST** (vertes en isolation, PAS des
> régressions produit) neutralisées par gardes déterministes (**0 assertion, 0 `src/` produit touchés**).
> Résultat **36 % → 0 %** de rouge full-suite parallèle (12 runs). **Décision clôture : `-Serial` reste le
> défaut au gate** (ceinture+bretelles, coût quasi nul), **`test.ps1` nu parallèle** exercé par la dev-team
> au cycle TDD (concurrence réelle, désormais fiable). Aucune dette résiduelle produit ;
> (1) **[partiellement livré s38]** graphe enfant-racine + **R2 familles recomposées VISIBLES en LECTURE**
> livrés s38 ; **reste** : **R3 « exactement 2 parents » / complétude du couple**, **graphe ÉTENDU**
> (grands-parents, parents liés entre eux via leurs enfants, lien enfant↔activité), **édition depuis le graphe**,
> **vue planning centrée couple** (recomposé) ;
> (2) **prolonger les Activités (post-s35)** — **liste de slots par activité** (récurrents/non ; une activité
> « avec une liste de slots », hors scope s35) + **lien adresse acteur↔lieu/domicile** de l'enfant en garde
> (retours PO gate s33) + **révision de la validation de pose** (préservée iso s35, à repenser) ;
> (3) **arbitrage inline vs modal** (tension s32, toujours ouvert, à trancher en G2) ;
> (4) **suppression d'un slot récurrent depuis l'IHM** (retour PO gate s31, affordance manquante +
> nuance occurrence unique vs série — goal 4 reporté s33) ;
> (5) ~~**commandes inverses actif/admin**~~ **SOLDÉ s41** (dé-désigner admin + désactiver compte + borne
> « dernier admin » → toggle bi-directionnel ; verrou ON s33 levé). **D2** (slots récurrents en Config foyer +
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
| **+1 (P0 — ÉPIC : Refonte de la Configuration du foyer ; 1er incrément LIVRÉ s32)** | **Refonte de la Configuration du foyer** — brief PO complet : [`docs/briefs/refonte-configuration-foyer.md`](briefs/refonte-configuration-foyer.md). Harmoniser toutes les sections config sur un même patron **tableau lecture seule + crayon → modal**. **(A) Acteurs — ✅ 1er INCRÉMENT LIVRÉ s32** : tableau lecture seule (nom, email, rôle, **état en pastille** actif/admin) + colonne **crayon → modal** éditant les **champs existants** (nom, couleur, rôle via CRUD existant) + « Ajouter » = modal vide ; refus→modal ouverte ; Parent-gated + SignalR. **(A) 2ᵉ INCRÉMENT ✅ LIVRÉ s33** : **toggle actif/admin** *dans* la modal (**sens ON**, toggle déjà ON verrouillé faute de commande inverse ; actif conditionné à un compte), **adresse de résidence** [champ neuf back+modal+tableau], **palette couleur** en picker minimal (solde la dette set couleurs). **⚠️ Reste (A) : arbitrage inline vs modal** (retour PO gate s32 : édition inline au clic de la valeur, EN PLUS de la modal — tension directe avec s32, à trancher en G2). **(B) Rôles ✅ HARMONISÉ s33** (tableau lecture + crayon→modal). **(C) Cycle ✅ HARMONISÉ s33** (tableau + crayon→modal hébergeant l'éditeur `definir-cycle` ; **cycles déclarés désormais TOUS visibles** — corrige le trou gate s32). **(D) Lieux → « Activités » ✅ LIVRÉ s35** : référentiel « Lieux » (s27) **renommé « Activités »** (refactor iso-comportement Domaine + Application, validation de pose préservée, axe LOCALISATION `LieuId` distinct non renommé), **champ adresse** sur l'activité (Mongo durable, vide accepté), **lien enfant↔activité N-M** (lier/délier idempotents, rejets sans écriture partielle), **onglet Activités** au patron tableau + crayon→modal (swap inline→modal, renommage HTTP au SWAP, colonne « Enfants liés » + sélecteur d'enfants). **Reste (D)** : **liste de slots par activité** (récurrents/non, hors scope s35), **lien adresse acteur↔lieu/domicile**, **révision de la validation de pose**. **(E) Enfants ✅ HARMONISÉ + lien parent LIVRÉ s34** : onglet Enfants au patron tableau lecture + crayon→modal (swap inline→modal), **lien enfant↔parent 1..2 parents** (lier/délier, 2 max, rejets sans écriture partielle, Mongo durable) + sélecteur de parents + colonne « Parents liés ». **(E) Vue d'accueil = graphe enfant-racine ✅ LIVRÉ s38 EN LECTURE** (query PURE `GrapheFoyerQuery` + vue lecture seule, familles recomposées **R2 visibles**, reprojection SignalR client) ; **badge de complétude du couple R3 ✅ SIGNALÉ en lecture + vue graphe relocalisée dans un onglet « Foyer » (1ᵉʳ) ✅ LIVRÉS s40**. **Reste (E)** : **contrainte R3 « exactement 2 » imposée à l'écriture** (choix produit, non traité), graphe étendu (grands-parents/parents liés entre eux), édition depuis le graphe. **Bonus (2ᵉ temps)** : multi-enfants configurables, vue planning centrée sur la garde des enfants d'un couple, **vue foyer recomposé** + proposition de config dédiée. | É1, É2, É7, É6 | Retour PO structuré (annoncé /planning s29, capté clôture s30) ; **1er incrément acteurs crayon/modal livré s32** ; reste à **découper au /planning** en incréments verticaux (arbitrage inline vs modal + toggle/adresse/palette acteurs ; harmonisation rôles/cycle/enfants ; activités/graph) ; introduit des champs neufs (adresse, palette couleur) et un lien enfant↔parent |
| ~~**+0 (P1 — flake, 5ᵉ montée CHIFFRÉE au gate s36 — CANDIDAT DE TÊTE)** — Rétrofit du garde *TempsReel* SignalR~~ **SOLDÉ s39** (4/4 critères, mesuré des deux côtés) : baseline **4/11 ≈ 36 % rouge** full-suite parallèle (victime UNIQUE `FrontWasmConfigEnfantsTempsReelTests`, isolé 3/3 vert = **course de charge, pas régression**) ; fix = collection xUnit `SignalRTempsReelCollection` (`DisableParallelization=true`) **ciblée** sur les 55 classes `FrontWasm*TempsReel*` (**périmètre minimal explicite, pas de rideau** : ~213 autres Web.Tests parallèles, `Tests`/`Api.Tests` inchangés — le blast-radius SMTP/Mongo n'a jamais rougi au baseline). La sérialisation a **démasqué 2 courses de convergence de TEST** (`ConfigEnfants` + `Impersonation`, vertes en isolation, **PAS des régressions produit**), neutralisées par 2 gardes déterministes (**0 assertion, 0 `src/` produit modifiés** ; course d'énumération s13 ni re-gardée ni cassée). **Résultat 36 % → 0 %** sur **12 runs** full-suite parallèle + `-Serial` 695/695. **Aucune dette résiduelle produit.** | É3 | ✅ soldé s39 |
| +3 | **Convergence `EditerPeriodeHandler` / `ModifierPeriodeHandler`** — deux handlers de mutation de période coexistent (le second legacy s02, même port + même modèle de concurrence) ; converger vers un seul chemin d'écriture — **dette de code** (DDD : un seul modèle de concurrence par agrégat) | É7 | Évite la dérive de deux chemins d'édition divergents ; ménage hygiénique post-s17 |
| +4 | **Édition concurrente du même jour sous dialog ouverte** (last-write-wins règle 11, à démontrer sous dialog) — ~~DIFFÉRÉE jusqu'à stabilisation SignalR~~ **débloquée** : la dette flake *TempsReel* est **soldée s39** (parallèle 0 % rouge), le prérequis de stabilité est levé | É7 | Cas limite runtime ; **plus de dépendance flake** (soldé s39) |
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
| ~~**Lien enfant↔parent** (dont **lier un enfant à 2 parents**)~~ **livré s34** : l'enfant (agrégat nu `EnfantFoyer(Id, Prénom)`) porte désormais un **lien vers 1..2 parents-acteurs** — commande **lier / délier** idempotente, règle **« 2 parents max »**, rejets **acteur inexistant / non-parent / déjà lié SANS écriture partielle**, persistance **Mongo durable**, id enfant inchangé (enrichissement). **Sélecteur de parents** dans la modal Enfants + **colonne « Parents liés »** en lecture. **Traite la question PO du gate s33.** *(Reste ouvert : contrainte « exactement 2 parents » R2/R3 + graphe enfant-racine — cf. lignes familles recomposées ci-dessous.)* | ✅ | s34 | retours s29 (PO) · re-signalé gate s33 · brief config foyer |
| ~~**Harmoniser l'onglet Enfants**~~ **livré s34** · ~~**harmoniser « Lieux » → « Activités »**~~ **livré s35** : onglet Enfants (s34) puis onglet **Activités** (s35) au patron **tableau lecture seule + crayon → modal** (lot atomique de surface, **swap inline→modal**, comme Acteurs s32 / Rôles & Cycle s33). **Épic Refonte Config foyer : Acteurs / Rôles / Cycle / Enfants / Activités TOUS harmonisés.** | ✅ | s34 (Enfants) + s35 (Activités) | retours s29 (PO) · re-signalé gate s33 · brief config foyer |
| ~~**[PRIORITAIRE, retour PO gate s35] Révision de l'éligibilité « parent » du lien enfant↔parent**~~ **livré s36 (option B1+B2, role-flag)** : le critère « libellé littéral « Parent » » (s34) est **abandonné** ; l'**option A** (`TypeActeur.Parent`) a été **livrée puis REJETÉE au gate** (écartée par construction : `TypeActeur` jamais saisi via l'IHM → tout acteur créé liable à tort). **Retenu** : flag booléen **« est rôle parent » sur `RoleFoyer`** (source de vérité unique, pilotable, persisté InMemory+Mongo durable ; défaut neutre false), commande **`MarquerRoleParent`** (idempotente, rôle inexistant refusé sans écriture), **pré-cochage Papa/Maman/Parent au seed initial** (rôle créé ensuite = non-parent, anti-piège libellé s35) + seed démo affectant un rôle-parent aux acteurs-parents ; **éligibilité du lien = l'acteur porte un rôle marqué parent** (rôle non marqué ou sans rôle = refusé sans écriture partielle ; borne 0..2 s34 inchangée) ; **`TypeActeur` cantonné au seul gating impersonation R8/R9** (aucun droit gagné/perdu). IHM : sélecteur modal Enfants filtré + **case « rôle parent »** dans la modal Rôles (Échap, Parent-gated, SignalR décoche→retire en direct). 7/7, 665/665, gate validé. | ✅ | s36 | retours gate s35 (PO) · lien s34 |
| ~~**Champ père/mère distinct sur le lien enfant↔parent**~~ **livré s37** : le lien enfant↔parent (s34, éligibilité role-flag s36) porte désormais un **attribut « rôle-du-lien » ∈ {père, mère, parent-libre}** (record `ParentLie`, commande `LierEnfantParent` enrichie, champ additif `RolesDesLiens`, **persistance Mongo durable**, id enfant inchangé) ; **invariant d'exclusivité** : pas deux « père » ni deux « mère » sur le même enfant (refus AVANT écriture, store intact) ; **« parent-libre » répétable** ; **compat non destructive** (lien s34 sans attribut relu à « parent-libre », pas de migration destructive) ; borne 0..2 (s34) + éligibilité role-flag (s36) **inchangées**. IHM : **sélecteur père/mère/parent** par parent lié dans la modal Enfants (refus → modal ouverte + motif + sélection conservée, Échap = Annuler), colonne « Parents liés » = **« nom (rôle) »**, Parent-gated + SignalR. 5/5, suite **682/682**, gate G3 validé PO. *(Reste ouvert : familles recomposées R2/R3 « exactement 2 parents » + graphe enfant-racine.)* | ✅ | s37 | hors scope s36 · lien s34/s36 |
| **Familles recomposées** (enfants de parents différents, même planning) — **VISIBLES en LECTURE s38** (vue graphe enfant-racine : deux racines, parent partagé sous chaque enfant, reflet fidèle) ; **complétude du couple R3 SIGNALÉE par enfant s40** (badge complet/incomplet/vide, lecture seule) ; reste ouvert : **vue planning centrée couple** (recomposé) + contrainte **R3 « exactement 2 parents » imposée à l'écriture** (non traitée, choix produit) | 🟡 | Palier 5-6 | spec règle 2 · retours s07 · **partiel s38 · statut s40** |
| ~~**Graphe foyer enfant-racine** (vue d'accueil Config foyer)~~ **livré s38 EN LECTURE** : query PURE `GrapheFoyerQuery` (par enfant → parents liés + rôle-du-lien, deux adaptateurs InMemory+Mongo) + **vue lecture seule** (enfant en racine « nom (rôle) », familles recomposées visibles, store vide = message neutre, Parent-gated, convergence SignalR par **reprojection client**). **Enrichi s40** : **badge de complétude du couple** (StatutCoupleR3 signalé en lecture) + **relocalisation dans un onglet « Foyer » (1ᵉʳ, actif par défaut)** de la Config foyer. **Reste ouvert (graphe ÉTENDU)** : grands-parents, parents liés entre eux via leurs enfants, lien enfant↔activité dans le graphe, **édition depuis le graphe** | 🟡 | Palier 5-6 | retours s07 · spec règles 2-3 · **partiel s38 · badge+onglet s40** |
| **Deux parents (toujours exactement 2 ; le 1er saisit l'autre)** — **STATUT de complétude SIGNALÉ en LECTURE s40** (enum `StatutCoupleR3` composé PUR depuis `GrapheFoyerQuery`, badge complet/incomplet/vide par enfant, orphelin exclu du décompte, **R3 signalée JAMAIS imposée**) ; **la CONTRAINTE « exactement 2 parents » à l'écriture reste NON imposée** (choix produit tenu — probablement volontairement non bloquant ; ⬜ à rouvrir uniquement si on veut l'imposer) | 🟡 | Palier 5 | retours s01 · spec règle 3 · **statut signalé s40** |
| ~~Lieux éditables et persistés (référentiel des sélecteurs)~~ **livré s27** | ✅ | Palier 10 | spec règle 11 · retours s21 |
| ~~**Repenser « Lieux » → « Activités » liées à l'enfant**~~ **livré s35** : référentiel « Lieux » (s27) **renommé « Activités »** (refactor sémantique iso-comportement Domaine + Application, validation de pose préservée, axe LOCALISATION `LieuId` du slot distinct et non renommé) ; l'activité porte un **champ adresse** (Mongo durable, vide accepté, sans écriture partielle) ; **lien enfant↔activité N-M** (lier/délier idempotents, rejets enfant/activité inconnu sans écriture partielle, plusieurs enfants partagent une activité) ; **onglet Activités** au patron tableau + crayon→modal (swap inline→modal, renommage HTTP `lieux→activite` au SWAP). **Reste ouvert** : **liste de slots par activité** (récurrents/non) — non traité s35 (ligne Épic 6) ; **lien adresse acteur↔lieu/domicile** (ligne dédiée ci-dessous) ; **révision de la validation de pose** (préservée iso s35). | ✅ | s35 | retours s29 (PO) · brief config foyer |
| **Lien adresse acteur-parent ↔ « lieu/domicile » de l'enfant en garde** (retour PO à chaud s33) : l'**adresse de résidence de l'acteur** (champ ajouté s33 Sc.1) doit **alimenter/être reliée** à un **lieu** — le lieu de résidence de l'enfant **quand il est chez un parent**. NOUVELLE **relation acteur↔lieu** (domicile-parent comme lieu implicite/dérivé) à cadrer AVEC le volet « Lieux → Activités » (impact validation de pose + référentiel lieux s27). **HORS scope s33** (goal = acteur enrichi + harmonisation rôles/cycle ; Activités/lieux explicitement non traités) → à découper au `/planning`. | ⬜ | épic Refonte Config foyer | retours s33 (PO, à chaud) · brief config foyer |
| ~~Set de couleurs par défaut persisté (acteur → couleur)~~ **soldé s33** : **palette couleur en picker minimal** dans la modal acteur (choix dans le set de couleurs, couleur courante pré-sélectionnée, persistée via la commande existante, grille/légende suit sans reload). *(Hors scope tenu : pas de palette custom — créer/renommer/supprimer des couleurs.)* | ✅ | s33 | spec règle 15 |

### Épic 2 — Modèle & configuration d'acteurs

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| Trois types d'acteurs avec rôles distincts (Admin / Parent / Autre) | ⬜ | Palier 5 | retours s01 (#3) · spec règles 6-7 |
| Écran de configuration du foyer complet (acteurs + cycle + couleurs, persisté) | ⬜ | Palier 10 | retours s01 (#7) · spec p5 |
| **Refonte Config foyer — patron tableau lecture seule + crayon → modal** — **Acteurs 1er incr. livré s32**, **Acteurs 2ᵉ incr. + Rôles & Cycle livrés s33** (toggle actif/admin sens ON + verrou, adresse, palette picker ; Rôles & Cycle au patron ; **tous les cycles déclarés visibles** ; fermeture Échap des modals), **Enfants harmonisés + lien enfant↔parent livrés s34** (patron tableau+crayon→modal, swap inline→modal, colonne « Parents liés » + sélecteur, lien 1..2 parents), **Activités harmonisées + renommage Lieux→Activités + lien enfant↔activité livrés s35** (4ᵉ incr. ; swap inline→modal, renommage HTTP au SWAP, champ adresse, colonne « Enfants liés » + sélecteur d'enfants), **éligibilité « parent » du lien enfant↔parent en flag « rôle parent » livrée s36** (case « rôle parent » dans la modal Rôles, sélecteur Enfants filtré), **rôle-du-lien père/mère/parent-libre sur le lien enfant↔parent livré s37** (record `ParentLie`, invariant d'exclusivité, compat non destructive ; sélecteur père/mère/parent dans la modal Enfants + colonne « nom (rôle) »), **vue graphe foyer enfant-racine lecture seule livrée s38** (query PURE `GrapheFoyerQuery`, enfant en racine « nom (rôle) », familles recomposées R2 visibles, reprojection SignalR client), **badge de complétude du couple R3 en LECTURE + relocalisation de la vue graphe dans un onglet « Foyer » (1ᵉʳ, actif par défaut) livrés s40** (StatutCoupleR3 composé PUR, signalé jamais imposé). **Épic : Acteurs / Rôles / Cycle / Enfants / Activités TOUS harmonisés + vue graphe foyer + badge complétude + onglet Foyer.** **Commandes inverses actif/admin livrées s41** (dé-désigner admin + désactiver compte + borne « dernier admin » → toggle bi-directionnel, dette verrou ON s33 soldée). **Reste** : **contrainte R3 « exactement 2 » imposée à l'écriture** (choix produit, non traité), **graphe étendu/édition depuis le graphe**, **arbitrage inline vs modal**. Brief : [`docs/briefs/refonte-configuration-foyer.md`](briefs/refonte-configuration-foyer.md). | 🟡 | épic Refonte Config foyer (Acteurs+Rôles+Cycle+Enfants+Activités+graphe+badge+cmd inverses faits) | retours s28/s29 (PO) · brief · **s32+s33+s34+s35+s36+s37+s38+s40+s41** |
| **⚠️ Édition INLINE au clic (valeur seule) — À ARBITRER (retour PO gate s32)** : le PO veut, **EN PLUS** de la modal s32, cliquer un champ du tableau pour l'éditer **en place** (clic → champ ouvert, **Entrée** valide, **clic dehors** referme **sans update**). **TENSION directe avec s32** (qui a retiré l'inline au profit de la modal) → **choix de direction** (inline seul / modal seule / cohabitation) à **trancher en G2 au prochain /planning** avant tout code. | ⬜ | **à arbitrer G2** | retours gate s32 (PO) |
| ~~**Adresse de résidence de l'acteur** (champ de modèle neuf, exposé dans la modal d'édition acteur)~~ **livré s33** : champ **adresse de résidence** porté par le modèle d'acteur, **persisté Mongo durable**, relu par la query de config, éditable dans la modal + **rendu dans le tableau lecture**, **adresse vide acceptée** (optionnel, sans écriture partielle). *(Reste ouvert : relier cette adresse à un lieu/domicile de l'enfant en garde — cf. Épic 1 « lien adresse acteur↔lieu ».)* | ✅ | s33 | brief config foyer (PO) · re-signalé s32 |
| ~~**Commandes inverses actif/admin** (dé-désignation admin + désactivation de compte)~~ **livré s41 — dette « toggle verrouillé ON s33 » SOLDÉE** : commande/handler **`DeDesignerAdmin`** (agrégat `AdministrationFoyer`, idempotent, acteur inconnu refusé sans mutation, **borne « dernier admin »** = dé-désigner le seul admin refusé AVANT écriture, foyer jamais sans admin) + commande **`DesactiverCompte`** (`Actif→Inactif` via `CompteUtilisateur.Desactiver`/`IEditeurComptes`, idempotent, compte inconnu refusé, compte désactivé refuse la connexion — garde s23 tenue, deux adaptateurs). **Toggle actif/admin désormais BI-DIRECTIONNEL** : le OFF émet la vraie commande inverse (fin du verrou ON s33, plus de no-op silencieux) ; refus → modal ouverte + motif + toggles conservés, Échap=Annuler, Parent-gated (R8/R9 préservé) + convergence SignalR. 6/6, suite **739/739**, gate G3 validé PO. | ✅ | s41 | routé au gate s33 (PO, Sc.4) |
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
| **Liste de slots par activité** (une activité « avec une liste de slots » récurrents/non) — le référentiel Activités s35 porte libellé + adresse + lien enfant, **pas** de slots ; brancher une liste de slots portée par l'activité = extension du modèle de slots + surface modal Activités | ⬜ | épic Refonte Config foyer (hors scope s35) | retours s29 (PO) · brief config foyer · hors scope s35 |
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
| ~~Transferts exposés dans le panneau cloche~~ **livré s47** : chaque écriture de transfert/délégation (s31/s44/s45/s46) est consignée au **journal de changements** (`IJournalChangements`) et surface comme **notification horodatée dans la cloche** (barre du haut), lu/non-lu par utilisateur, temps réel SignalR porteur de payload (0 GET). | ✅ | s47 | spec règle 20 · retours s02 (#8)/s03 |

### Épic 9 — Notifications & événements à venir

| Besoin | Statut | Palier | Origine |
|--------|:------:|--------|---------|
| ~~**Panneau cloche = NOTIFICATION de CHANGEMENT** (badge « non-lu », diff, signal qu'une chose a changé)~~ **livré s47** : **CLOCHE GÉNÉRALE** dans la **barre du haut** (MainLayout, gating connecté && Parent) — icône + **badge compteur de non-lus** + **panneau déroulant** (liste chrono, lu/non-lu, marquer-lu idempotent, Échap ferme). Servie par un **JOURNAL DE CHANGEMENTS append-only** (`IJournalChangements`, 2 adaptateurs InMemory + Mongo) alimenté par chaque handler d'écriture, **TRACE DE LECTURE horodatée non-autorité** (jamais lue par la résolution) ; **état lu/non-lu PAR utilisateur** persisté (`IEtatLectureNotifications`) ; temps réel SignalR **porteur de payload** (`INotificateurChangement`), reprojection client **0 GET**. **Surface hors-grille assumée** (amende « grille = seule surface » s44). | ✅ | s47 | spec règles 20/120 · retours s02/s03 |
| ~~Transferts listés comme événements (date, acteurs, lieu, heure)~~ **livré s47** (journal de changements : type, jour/enfant, cédant/recevant, horodatage). | ✅ | s47 | spec règle 20 |
| ~~Changements de planning exposés comme événements~~ **livré s47** (délégation s44 / plage s45 / reprise s46 / transfert s31 / proposition d'échange → événements horodatés dans la cloche). | ✅ | s47 | spec règle 20 |
| ~~« Qui récupère ce soir » — immédiat (qui-quand-où du jour)~~ **livré s42 EN LECTURE** : carte « Aujourd'hui » (jour courant + enfant sélectionné) restituant **qui / où / transfert du jour**, via query `CarteDuJourQuery` **PURE composant** `GrilleAgendaQuery` (résolution surcharge>fond>neutre + slots s29 + transfert saisi/dérivé s31, **aucun store neuf, aucune mutation**, deux adaptateurs, Mongo durable) ; carte en tête du planning par **reprojection client**, **STRICTEMENT lecture**, **Invité VOIT**, convergence **SignalR par reprojection client** (0 GET sur push). **1ᵉʳ incrément du noyau produit** après l'épic Config foyer. | ✅ | s42 | spec p4 · spec v03 incrément 2 |
| ~~**Liste « À venir » — prochains jours (qui/où/transfert)**~~ **livré s43 EN LECTURE** (**2ᵉ incrément du noyau produit**, prolonge la carte du jour s42) : **panneau « À venir »** sous la carte « Aujourd'hui » listant les **N prochains jours de la fenêtre de grille chargée** (date croissante) + l'enfant sélectionné avec **qui / où / transfert** par jour, via query `AVenirQuery` **PURE composant** `GrilleAgendaQuery` (**miroir strict de `CarteDuJourQuery` s42** itérée sur les jours à venir, **aucun store neuf, aucune mutation**, deux adaptateurs, Mongo durable) ; repli fidèle (personne assignée / orphelin neutre `Resolvable` s13 / sans slot sans lieu / **fenêtre sans à-venir = liste vide message neutre**) ; **STRICTEMENT lecture**, **Invité VOIT**, **reprojection client**, convergence **SignalR** (0 GET sur push). **Notifications/alertes push explicitement HORS scope** (cf. ligne cloche ci-dessus). | ✅ | s43 | spec p4/p7 · spec règle 20 · **suite carte du jour s42** |
| **Carte du jour ET liste « À venir » PERSISTANTES hors de la semaine courante** (limitation s42 **+ s43**) : carte (s42) et panneau « À venir » (s43) se reprojettent depuis la **fenêtre de grille chargée**, donc **disparaissent / ne s'affichent pas au-delà** si l'utilisateur navigue hors du jour courant / de la fenêtre. Choix guidé par l'**anti-amplification flake** (aucun GET dédié sur push). **À arbitrer** : persistance hors vue **vs** coût d'un GET sur push (risque flake). | ⬜ | Palier 11 (arbitrage) | **limitation s42/s43** |

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
| **Signalement d'imprévu (malade, retard…) + notification immédiate** — le **mécanisme de notification** (cloche + journal + temps réel) est **livré s47** ; reste une **entrée dédiée « signaler un imprévu »** (malade/retard) distincte de l'échange (candidat goal prochain). | 🟡 | Palier 12 | spec p7 · **mécanisme s47** |
| ~~Échange de dernière minute *(proposition + accord requis)*~~ **livré s47 (flux PROPOSITION → ACCORD consenti)** : `ProposerEchange` crée une **Proposition `pending`** (notif chez le recevant) **SANS écrire de surcharge ni changer la résolution** (anti vert-qui-ment prouvé) ; **`AccepterProposition` COMPOSE la délégation s44** (surcharge + transfert dérivé s31) → `accepté` ; **`RefuserProposition` retire SANS écriture** → `refusé`. Cas limite/erreur (soi-même, inconnu/orphelin refusé avant écriture, last-write-wins R11, jour hors fenêtre sans crash), 2 adaptateurs InMemory + Mongo. **IHM** : notif **ACTIONNABLE dans la cloche** (Accepter/Refuser) + entrée « proposer un échange » du menu clic-case, temps réel 0 GET. Complète la **délégation directe** s44 par le **workflow de consentement**. | ✅ | s47 | spec p7 · **s47** (partiel s44) |
| **Échange sur une PLAGE `[J1..J2]`** (s45) & échange **récurrent/série** (D2) & **multi-enfants** — s47 borné à **UN jour ponctuel** ; étendre le flux proposition→accord à une plage / série / plusieurs enfants. | ⬜ | Palier 12 | hors scope s47 |
| **Notifications push / e-mail externes** — la cloche s47 est **in-app** (temps réel SignalR) ; notifier hors de l'app (push mobile, e-mail) reste ouvert. | ⬜ | Palier 12/13 | hors scope s47 · spec p7 |
| ~~**Transferts temporaires** (exception, non récurrents)~~ **livré s44** : **délégation de la récupération d'UN jour** — use case `DeleguerRecuperation` composant l'écriture surcharge ponctuelle (s06), transfert **auto-dérivé s31**, entrée du menu clic-case Parent-gated, refus (soi-même / délégataire inconnu) sans écriture, convergence temps réel de la case (0 GET). **Reste ⬜** : délégation **récurrente/série** (D2). | ✅ | s44 | spec règles 17-18 · **s44** |

---

## À faire — paliers de séquencement (⬜)

> Vue de séquencement (ordre de livraison). Paliers 1-9 + 14 **livrés** (voir `BACKLOG-Done.md`).
> Les sujets techniques sont séquencés **derrière l'usage**.

| Palier | Besoin | Épics | Origine |
|-------:|--------|-------|---------|
| 9bis | **Survol → résumé de la journée** (enrichissement après ~1s ; périmètre à cadrer) | É5, É9 | spec v09 · besoins s07 |
| 10 | **Config foyer durable restante** (~~lieux~~ **livré s27** · set couleurs par défaut) + Admin/Parent/Autre + écran de config complet | É1, É2, É7 | spec v05 p5-6 · retours s01/s03 |
| ~~11~~ | ~~**Immédiat & événements à venir** — panneau cloche~~ **livré s47** (cloche générale : journal de changements append-only non-autorité, lu/non-lu par utilisateur, badge, panneau, barre du haut, temps réel porteur de payload 0 GET) ; « qui récupère ce soir » livré s42 en lecture | É8, É9 | spec v05 p7 · **s47** |
| ~~12~~ | ~~**Imprévu & échange**~~ **échange proposition→accord livré s47** (flux consenti : proposer=pending sans écriture, accepter compose s44, refuser sans écriture ; actionnable dans la cloche) ; **reste** : signalement d'imprévu dédié (malade/retard), échange plage/série/multi-enfants (⬜, Épic 11) | É8, É11 | spec v05 p8 · **s47** |
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
- ~~**Flakes temps-réel SignalR** (É3, `FrontWasm*TempsReel*`)~~ — **SOLDÉE À LA CAUSE s39.** Baseline mesuré **4/11 ≈ 36 % rouge** full-suite parallèle (victime UNIQUE `FrontWasmConfigEnfantsTempsReelTests`, isolé 3/3 vert). Remède = collection xUnit `SignalRTempsReelCollection` (`DisableParallelization=true`) **ciblée** sur les 55 classes `FrontWasm*TempsReel*` (**pas un rideau** : ~213 autres Web.Tests restent parallèles, `Tests`/`Api.Tests` inchangés — le blast-radius SMTP/Mongo n'a **jamais** rougi au baseline, contrairement à l'hypothèse s29). La sérialisation a **démasqué 2 courses de convergence de TEST** (`ConfigEnfants` fermeture modal async + `Impersonation` catalogue incarnable async), **vertes en isolation = PAS des régressions produit**, neutralisées par 2 gardes déterministes (**0 assertion, 0 `src/` produit modifiés** ; course d'énumération s13 ni re-gardée ni cassée). **Résultat 36 % → 0 %** sur 12 runs parallèle + `-Serial` 695/695. **Aucune dette résiduelle produit** ; le triage durci (rétro s21) a **discriminé** flake (course de charge) vs régression à chaque étape. Décision clôture : `-Serial` **reste** le défaut au gate (ceinture+bretelles), parallèle exercé par la dev-team au cycle TDD (concurrence réelle désormais fiable). L'édition concurrente sous dialog (+4) n'est plus bloquée par cette dette.
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
