# Sprint 47 — Cloche générale + échange de dernière minute (proposition → accord)

> Palier 11 (cloche / notifications) **+** palier 12 / épic 11 (échange consenti). Deux briques
> greffées : **(A) une CLOCHE GÉNÉRALE** de changements (fondation) ; **(B) l'échange
> proposition→accord** dont les propositions arrivent comme **notifications ACTIONNABLES** dans la
> cloche. La cloche est la **1ʳᵉ surface hors-grille rouverte** depuis s44.

## Avancement — 3/9

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| **BRIQUE A — Cloche générale (fondation : lecture + lu/non-lu)** | | | |
| Sc.1 | **Journal de changements append-only** alimenté par les handlers d'écriture existants (délégations s44 / plages s45 / **reprises s46** / transferts) — **trace de LECTURE horodatée, NON autorité de résolution** ; liste chrono (récence) par utilisateur, 2 adaptateurs InMemory + Mongo | @back | 🔴 |
| Sc.2 | État **lu / non-lu PAR utilisateur** (vrai état persisté), marquer-lu idempotent, **compteur de non-lus** — 2 adaptateurs InMemory + Mongo durable | @back | 🔴 |
| Sc.3 | Cloche + **badge compteur** en en-tête du planning + **panneau déroulant** (liste chrono, lu/non-lu, marquer lu), Parent-gated, Échap ferme | @ihm | 🔴 |
| Sc.4 | Temps réel : un changement → nouvelle notif + compteur incrémenté chez les destinataires par reprojection client SignalR, **0 GET** | @ihm | 🔴 |
| **BRIQUE B — Échange proposition → accord (greffé sur la cloche)** | | | |
| Sc.5 | PROPOSER = notification `pending` chez le recevant **SANS aucune écriture de surcharge** (résolution de la case inchangée) | @back | ✅ |
| Sc.6 | ACCEPTER **compose la délégation s44** (surcharge + transfert dérivé s31) ; REFUSER retire sans écriture — Mongo durable | @back | ✅ |
| Sc.7 | Cas limite / erreur : soi-même refusé · délégataire inconnu/orphelin refusé AVANT écriture · ré-proposition last-write-wins R11 · jour hors fenêtre sans crash | @back | ✅ |
| Sc.8 | Notification d'échange **ACTIONNABLE dans la cloche** (Accepter / Refuser depuis la notif) + entrée « proposer un échange » du menu clic-case (Parent-gated), Échap | @ihm | 🔴 |
| Sc.9 | Temps réel : accord → notif + case résolue (surcharge + transfert) sur 2ᵉ écran par reprojection SignalR, 0 GET ; refus → notif close sans écriture | @ihm | 🔴 |

> **Ligne de coupe (si découpage 2 sprints retenu par le PO)** : Sc.1–4 = **sprint 47 (cloche générale
> lecture + lu/non-lu)**, brique livrable et cohérente seule ; Sc.5–9 = **sprint 48 (échange
> actionnable dans la cloche)**. Voir `resume` / décision PO.

## Goal (G2 tranché PO)

Un parent qui ne peut pas récupérer un jour **PROPOSE l'échange** à un autre acteur ; le recevant est
**NOTIFIÉ via la cloche** et **ACCEPTE / REFUSE depuis la notification**. Contrairement à la
délégation s44 (unilatérale, effet immédiat), **la proposition n'a AUCUN effet sur la résolution**
tant qu'elle n'est pas acceptée : c'est le **consentement** du recevant qui déclenche l'écriture.

### Surface — RÉOUVERTURE PO de la cloche hors-grille (porte de conception s44, NOUVELLE décision)

**Le PO ROUVRE EXPLICITEMENT la surface cloche / notifications hors-grille qu'il avait fait retirer
en s44.** Cette décision **LÈVE le « grille = seule surface »** pour la seule brique notifications
(le noyau de lecture du planning reste la grille ; la cloche s'y ajoute en en-tête). Elle **remplace**
la surface « badge sur la case » du cadrage précédent.

- **CLOCHE GÉNÉRALE** : icône cloche + **badge compteur de non-lus** en **en-tête du planning** ; clic
  → **panneau déroulant** listant **TOUS les changements récents** (délégations s44, plages s45,
  reprises s46, transferts, propositions d'échange) avec état **LU / NON-LU par utilisateur**.
- **Propositions d'échange en attente = notifications ACTIONNABLES** : **Accepter / Refuser DEPUIS la
  notification** dans la cloche. **PLUS de badge sur la case**, **PLUS d'entrée conditionnelle du menu
  clic-case** pour répondre.
- **PROPOSER** reste une entrée **« proposer un échange » du menu clic-case** (Parent-gated) sur la
  case du jour visé.

### Modèle & règles

- **Échange** : `ProposerEchange(jour, enfant, versActeur)` crée une **Proposition `pending`** (notif
  chez le recevant) **sans aucune écriture** ; `AccepterProposition` **compose la délégation
  EXISTANTE s44** (surcharge du jour + transfert bicolore auto-dérivé s31, R24) → `accepté` ;
  `RefuserProposition` → `refusé`, aucune écriture. Deux adaptateurs InMemory + Mongo durable.
- **Cloche** : les notifications sont servies par un **JOURNAL DE CHANGEMENTS append-only** (port neuf
  `IJournalChangements`, 2 adaptateurs InMemory + Mongo), **alimenté par CHAQUE handler d'écriture
  existant** (délégation s44 / plage s45 / **reprise s46** / transfert s31 + propositions) qui y
  enregistre `{type, jour, enfant, cédant/recevant, horodatage via IDateTimeProvider}`. **Décision
  d'archi (SM, arbitrage dev-team) : la dérivation PURE de l'état courant est INFAISABLE** — une
  **reprise s46 SUPPRIME la surcharge** (aucune trace dérivable) et ni `PeriodeSnapshot` ni
  `TransfertSnapshot` ne portent d'**horodatage de création** (ils ne connaissent que le jour couvert,
  pas l'instant d'écriture, donc pas de tri par récence). Le journal est donc **persisté** mais reste
  une **TRACE DE LECTURE : il n'est PAS autorité de résolution** — la **vérité de résolution reste les
  périodes/transferts** (surcharge > fond, transferts dérivés). **Pas de « store d'événements vérité
  divergent »** au sens interdit : aucune résolution ne lit le journal. Le **second état persisté** est
  le **lu / non-lu par utilisateur** (+ compteur), séparé du journal. Deux adaptateurs chacun.

### POINT DE VIGILANCE — journal = trace de lecture, PAS autorité de résolution (Sc.1)

**Le journal de changements est une TRACE DE LECTURE horodatée, jamais une source de vérité.** La
**résolution d'une case reste exclusivement les périodes/transferts** (surcharge > fond, transferts
dérivés s31) — **aucun code de résolution ne lit le journal**. Prouver qu'écrire/supprimer une
surcharge n'altère pas la vérité via le journal, et réciproquement que le journal capte bien les
**reprises s46** (suppressions) que l'état courant ne permet PAS de dériver. Donnée derrière un
**port** (`IJournalChangements`, 2 adaptateurs) — jamais figée dans le code.

### ORDRE D'ATTAQUE AUTORISÉ (arbitrage SM)

**Réordonnancement AUTORISÉ.** La **brique B backend (Sc.5–7)** — entité `Proposition`
pending/accepté/refusé, `ProposerEchange` (n'écrit rien), `AccepterProposition` (compose
`DeleguerRecuperationHandler` s44), `RefuserProposition` — **ne dépend QUE de l'existant s44/s31**,
**pas** de la structure du journal (brique A). La dev-team **mène Sc.5→7 immédiatement**, en
parallèle de la mise en place du journal (Sc.1–2). Contrainte inchangée : **suite complète verte** à
chaque commit, un scénario individuellement asserté par commit. Les scénarios @ihm (Sc.3, 4, 8, 9)
restent **derrière** leurs backends respectifs.

### POINT DE VIGILANCE — anti vert-qui-ment (Sc.5)

**PROPOSER ne doit RIEN écrire.** Prouver que **le store des surcharges reste intact** et que **la
résolution de la case est inchangée** tant que la proposition n'est pas acceptée — un pending qui
teinterait déjà la case serait une délégation déguisée (s44), pas un échange consenti.

### Hors-scope (backlog)

- **PLAGE `[J1..J2]`** (s45) — sprint borné à **UN jour ponctuel** pour l'échange.
- **Récurrence / série** (D2), échange **multi-enfants**.
- **Notifications push / e-mail externes** — la cloche est **in-app**, temps réel SignalR.

## Scénarios

### Sc.1 — Journal de changements append-only, trace de lecture horodatée @back @pending
```gherkin
Étant donné des handlers d'écriture existants (délégation s44, plage s45, reprise s46, transfert s31)
Quand chacun réalise son écriture, il consigne un événement au JOURNAL DE CHANGEMENTS (type, jour/enfant, acteurs, horodatage via IDateTimeProvider)
Et en particulier une REPRISE s46 (qui SUPPRIME la surcharge) consigne bien son événement, car le journal ne dérive pas de l'état courant
Quand on interroge le flux de notifications d'un utilisateur destinataire
Alors chaque changement le concernant apparaît comme un événement horodaté, trié par RÉCENCE de l'écriture (le plus récent en tête)
Et le journal est une TRACE DE LECTURE : il n'est JAMAIS lu par la résolution (la vérité reste périodes/transferts, surcharge > fond)
Et le comportement est prouvé sur les DEUX adaptateurs (InMemory + Mongo durable)
```

### Sc.2 — État lu / non-lu par utilisateur + compteur @back @pending
```gherkin
Étant donné un flux de notifications pour un utilisateur avec des événements non lus
Quand on lit le compteur de non-lus
Alors il reflète le nombre d'événements non encore marqués lus PAR CET utilisateur
Quand l'utilisateur marque une notification (ou toutes) comme lue(s)
Alors l'état "lu" est persisté PAR utilisateur (un autre utilisateur garde son propre état non-lu)
Et re-marquer lu est idempotent (aucun doublon, compteur stable)
Et l'état lu/non-lu est durable (prouvé sur store Mongo réel), deux adaptateurs
```

### Sc.3 — Cloche + badge compteur + panneau déroulant @ihm @pending
```gherkin
Étant donné un Parent connecté avec des notifications non lues
Alors une icône cloche avec un badge compteur de non-lus est visible en en-tête du planning
Quand il clique la cloche
Alors un panneau déroulant liste les changements récents (chrono), chacun marqué lu ou non-lu
Et il peut marquer une notification (ou tout) comme lue, le compteur se met à jour
Et Échap ferme le panneau (port IEcouteurEchapModal s33)
Et un Invité ne voit pas la cloche (Parent-gated)
```

### Sc.4 — Temps réel : nouvelle notif + compteur convergent @ihm @pending
```gherkin
Étant donné deux écrans, celui d'un utilisateur destinataire affichant sa cloche (compteur = N)
Quand un changement le concernant est écrit (délégation / plage / reprise / transfert) depuis un autre écran
Alors sa cloche CONVERGE par reprojection client (SignalR lecture seule, 0 GET) :
  une nouvelle notification apparaît en tête du panneau et le compteur passe à N+1
Et aucun GET dédié n'est déclenché sur le push
```

### Sc.5 — PROPOSER crée une notif pending SANS écrire de surcharge @back @vert
```gherkin
Étant donné un foyer avec un responsable de fond résolu pour un jour et un acteur tiers connu
Quand un parent PROPOSE l'échange de ce jour vers l'acteur tiers (ProposerEchange)
Alors une Proposition "proposé" (pending) est enregistrée et surface comme notification chez le recevant
Et AUCUNE surcharge n'est écrite : le store des surcharges est INCHANGÉ (identique à avant la proposition)
Et la résolution de la case reste "surcharge > fond" inchangée (aucun basculement, aucun transfert dérivé)
Et le comportement est prouvé sur les DEUX adaptateurs (InMemory + Mongo durable)
```

### Sc.6 — ACCEPTER compose la délégation s44 ; REFUSER retire sans écriture @back @vert
```gherkin
Étant donné une Proposition "proposé" (pending) sur un jour, adressée à un recevant connu
Quand le recevant ACCEPTE (AccepterProposition)
Alors la délégation EXISTANTE s44 est composée : une surcharge du jour est écrite, le recevant prime (surcharge > fond)
Et le transfert cédant → recevant est AUTO-DÉRIVÉ (s31, R24), jamais réécrit
Et la Proposition passe à "accepté" (écriture durable, prouvée Mongo réel)
Quand une autre Proposition pending est REFUSÉE (RefuserProposition)
Alors elle passe à "refusé", AUCUNE surcharge n'est écrite, le store reste intact
```

### Sc.7 — Cas limite & erreur : refus AVANT écriture, idempotence, robustesse @back @vert
```gherkin
Quand un parent PROPOSE l'échange à SOI-MÊME (recevant = responsable déjà résolu)
Alors la proposition est REFUSÉE explicitement, sans aucune écriture (aucune Proposition, aucune surcharge)

Quand un parent PROPOSE vers un délégataire INCONNU / orphelin (id stable absent du store)
Alors la proposition est refusée AVANT écriture, store intact, aucune écriture partielle

Quand une SECONDE proposition est émise sur le même jour/enfant déjà porteur d'un pending
Alors last-write-wins (R11) : une seule Proposition pending subsiste, sans doublon

Quand la proposition porte sur un jour HORS de la fenêtre chargée
Alors l'enregistrement est valide (une date), sans crash, affichage suivant la fenêtre
```

### Sc.8 — Notification d'échange ACTIONNABLE dans la cloche @ihm @pending
```gherkin
Étant donné une Proposition pending adressée à l'utilisateur courant, visible dans sa cloche
Alors la notification d'échange porte deux actions "Accepter" et "Refuser" (notification actionnable)
Quand il choisit "Accepter" (resp. "Refuser") — via mini-dialog de confirmation
Alors AccepterProposition (resp. RefuserProposition) est émis par le canal d'écriture (jamais la diffusion)
Et il n'existe NI badge sur la case NI entrée conditionnelle du menu clic-case pour répondre (réponse dans la cloche)
Et l'émetteur PROPOSE via l'entrée "proposer un échange" du menu clic-case de la case du jour (Parent-gated)
Et Échap ferme le mini-dialog / le panneau sans commande ; un Invité ne voit ni cloche ni entrée (Parent-gated)
```

### Sc.9 — Temps réel : accord / refus convergent @ihm @pending
```gherkin
Étant donné deux écrans, une Proposition pending notifiée dans la cloche du recevant
Quand le recevant ACCEPTE depuis sa cloche
Alors sur le 2ᵉ écran (émetteur) la case du jour CONVERGE par reprojection client (SignalR, 0 GET) :
  le recevant devient responsable (surcharge), le transfert bicolore dérivé apparaît, la notif passe "accepté"
Quand, dans une autre passe, le recevant REFUSE
Alors la notification se clôt (refusé) par reprojection client (0 GET), sans écriture ni changement de responsable
```

# Retours produit (PO)

_(à remplir au gate G3)_
