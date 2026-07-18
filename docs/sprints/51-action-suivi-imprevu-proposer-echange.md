# Sprint 51 — Action de suivi sur un imprévu : réagir (malade/retard) en proposant un échange

> **Palier 15** (spec [`notifications-et-echange.md`](../specs/notifications-et-echange.md), section « Imprévu &
> échange ») — **ferme la boucle** ouverte s48. Aujourd'hui un imprévu (malade/retard s48) est **purement
> informatif** dans la cloche : aucun moyen d'y **réagir**. Ce sprint greffe une **action de suivi** : depuis la
> notification d'imprévu, un parent **propose un échange** (réemploi INTÉGRAL de `ProposerEchange` s47 — proposition
> `pending`, actionnable Accepter/Refuser chez le recevant). Compose deux briques **déjà livrées** : l'imprévu s48
> (journal `IJournalChangements`, cloche) + l'échange s47 (`Proposition`, `Accepter/RefuserProposition`,
> diffusion porteuse de payload `INotificateurChangement`). **AUCUN modèle/store neuf.**

## Avancement — 4/7

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| 1 | Proposer un échange EN RÉACTION à un imprévu journalisé → `Proposition` pending pré-remplie jour+enfant de l'imprévu, SANS écriture (store surcharges intact, imprévu reste au journal) | @back | ✅ |
| 2 | ACCEPTER la proposition issue de l'imprévu compose la délégation s44 (surcharge + transfert dérivé s31) ; REFUSER sans écriture ; deux adaptateurs InMemory + Mongo durable | @back | ✅ |
| 3 | Cas limite : ré-proposition last-write-wins R11 sans doublon ; jour hors fenêtre chargée sans crash ; identique InMemory + Mongo durable | @back | ✅ |
| 4 | Cas erreur / invariant : soi-même / délégataire inconnu / orphelin refusés AVANT écriture (aucune écriture partielle) ; imprévu (fait) et proposition (échange) restent des modèles SÉPARÉS | @back | ✅ |
| 5 | Entrée d'action « proposer un échange » DANS la notif d'imprévu de la cloche (contextualisée jour/enfant), Parent-gated (Invité inerte) → mini-dialog s47 pré-rempli, Échap = Annuler (port s33) | @ihm | ⏳ |
| 6 | La proposition issue de l'imprévu apparaît ACTIONNABLE (Accepter/Refuser) chez le recevant dans SA cloche ; accepter compose la délégation, la case converge | @ihm | ⏳ |
| 7 | Temps réel : diffusion porteuse de payload (INotificateurChangement s47) → cloche d'un 2ᵉ écran converge PAR REPROJECTION CLIENT, 0 GET sur push | @ihm | ⏳ |

## Goal & cadrage

**Objectif** : transformer l'imprévu **subi** (s48) en **point de départ d'une action**. « Léa est malade le 29/06 »
apparaît dans ma cloche → je **propose direct un échange** à un autre parent, sans quitter la notification. La valeur =
raccorder deux flux qui existent déjà mais restaient **cloisonnés** : signaler un fait (s48) et négocier une réaffectation
(s47).

**GARDE DE DISTINCTION (non négociable).** L'imprévu **LUI-MÊME reste un FAIT informatif non-négocié** (modèle `Imprevu`
s48 : consigné au journal, jamais lu par la résolution, sans état pending/accepté/refusé). L'action de suivi est une
**PROPOSITION D'ÉCHANGE DISTINCTE** (modèle `Proposition` s47) **greffée en réaction** — elle ne mute pas l'imprévu, ne le
« résout » pas, ne lui ajoute pas de statut. Les **deux modèles restent séparés** : l'imprévu informe, la proposition
négocie. Signaler « malade » **puis** proposer un échange = **deux événements distincts** au journal.

### PORTE DE CONCEPTION SURFACE — TRANCHÉE AU CADRAGE (les @ihm ne se mènent qu'après cet arbitrage écrit)

**Emplacement retenu : l'action « proposer un échange » vit DANS la notification d'imprévu de la cloche** (une entrée
d'action **contextuelle** attachée à la notif, portant déjà le jour + l'enfant de l'imprévu, qui **pré-remplit** la
mini-dialog « proposer un échange » s47). Le parent n'a plus qu'à choisir le **versActeur**.

**Cet ajout AMENDE la décision s48 « imprévu = informatif, SANS action de suivi (non négociable) ».** Justification de
l'amendement : en s48, « sans action de suivi » était **volontaire et borné** — le cas actionnable existait déjà (échange
s47) et on refusait de **fusionner** les deux (l'imprévu n'est pas négociable *en tant que fait*). Ce sprint **ne
contredit pas** cette décision : l'imprévu **reste informatif et non-négociable** ; on **ne rend pas l'imprévu
actionnable**, on **greffe À CÔTÉ** une proposition d'échange distincte, déclenchée *par réaction*. L'action ne mute pas la
notif d'imprévu (toujours lu/non-lu informatif) ; elle **crée un second événement** (la proposition). La borne s48 tenait
tant qu'aucun besoin de réaction n'était exprimé ; il l'est désormais (candidat de tête depuis s48).

**Alternatives ÉCARTÉES** (arbitrées ici, PAS au gate G3 — anti-rework en cascade, récit s44) :
- **Bouton sur la case de la grille** (« réagir à l'imprévu du jour ») — écarté : l'imprévu se **lit dans la cloche**, pas
  sur la grille (anti-cliquet s44 : la grille reste la seule surface de LECTURE du planning, pas une surface d'alertes) ;
  l'action doit être **au plus près de l'information** qui la motive.
- **Nouvelle entrée du menu clic-case** (« proposer un échange suite à imprévu ») — écarté : redondant avec l'entrée
  « proposer un échange » s47 **déjà** dans le menu clic-case, et **décorrélé** de l'imprévu (perd le contexte jour/enfant,
  oblige à re-saisir). L'intérêt du suivi est précisément d'être **contextuel à la notif**.
- **Transformer la notif d'imprévu en notif actionnable type échange** (accepter/refuser sur l'imprévu) — écarté :
  **violerait la garde de distinction** (fusionnerait les deux modèles, rendrait l'imprévu négociable — exactement ce que
  s48 a proscrit). L'action produit une **proposition séparée**, elle ne « répond » pas à l'imprévu.

**SURFACE — AUCUNE surface neuve.** Réutilise : la **cloche s47** (barre du haut) qui gagne une **action contextuelle** sur
les notifs d'imprévu ; la **mini-dialog « proposer un échange » s47** (pré-remplie jour+enfant, le proposant choisit le
versActeur) ; la notif de proposition **actionnable** s47 chez le recevant (Accepter/Refuser).

**HORS SCOPE (bornes)** :
- **Réaction autre qu'un échange** (déléguer unilatéralement depuis la notif, « annuler ma garde ») — ce sprint = **un**
  suivi, la **proposition d'échange** consentie.
- **Imprévu sur une plage / série / multi-enfants** (borne s48 : un imprévu = un jour, un enfant) → la proposition greffée
  hérite du **jour+enfant unique** de l'imprévu ; l'échange sur plage/multi-enfants reste un candidat backlog distinct.
- **Notifications push / e-mail externes** — la cloche reste **in-app** (SignalR), comme s47/s48.
- **AUCUNE persistance neuve** : réutilise `Proposition` s47 (+ ses ports/store) et le journal s48. Le type d'événement
  existe déjà ; aucun store neuf.

**POINTS DE VIGILANCE (anti-régression) :**
- **Invariant s47 « 0 écriture avant accord »** : proposer (même depuis un imprévu) crée un `pending` **SANS** surcharge —
  le store des surcharges reste **INTACT**, la case **inchangée** tant que non acceptée. À prouver explicitement (Sc.1).
- **Invariant s48 « journal = trace non-autorité »** : l'imprévu reste consigné au journal, **jamais lu par la
  résolution** ; la proposition greffée ne le mute pas. Modèles séparés (Sc.4).
- **Accepter = composition s44** : `AccepterProposition` compose la délégation (surcharge du jour + transfert bicolore
  auto-dérivé s31, R24), aucun chemin d'écriture neuf (Sc.2, Sc.6).
- **0-GET sur push maintenu (s44–s48)** : la cloche reprojette depuis la **diffusion porteuse de payload**
  (`INotificateurChangement`), jamais un GET dédié sur push (garde anti-flake [[flake-signalr-blast-radius]]).
- **Gating** : action visible **connecté && Parent** (Invité ne voit ni la cloche ni l'action), cohérent s44/s47/s48.
- **Gate visuel (leçon s49)** : interaction = clics / mini-dialog (couverte bUnit), **pas de geste souris natif** → **pas
  de smoke Playwright requis** ; MAIS **rebuild du build WASM SERVI** (conteneur `web` docker) à jour **avant le gate PO**
  (un artefact `--no-build` périmé masquerait le câblage au navigateur du PO).

## Scénarios

```gherkin
@back @vert
Scénario Sc.1 — Proposer un échange en réaction à un imprévu crée une proposition pending SANS écriture
  Étant donné un imprévu « malade » (ou « retard ») consigné au journal s48 sur un jour J pour un enfant E
  Quand un parent propose un échange EN RÉACTION à cet imprévu vers un délégataire éligible (versActeur)
  Alors une Proposition s47 « pending » est créée, PRÉ-REMPLIE avec le jour J et l'enfant E de l'imprévu
  Et le store des surcharges reste INTACT (aucune surcharge écrite, aucun transfert dérivé, case J inchangée)
  Et l'imprévu d'origine reste consigné au journal, INCHANGÉ (fait informatif non muté par la proposition)
  Et un événement de proposition distinct entre dans le flux notifications (proposition ≠ imprévu, deux événements)

@back @vert
Scénario Sc.2 — Accepter compose la délégation s44 ; refuser sans écriture ; deux adaptateurs
  Étant donné une Proposition pending issue d'un imprévu (jour J, enfant E, versActeur B)
  Quand le recevant B ACCEPTE la proposition
  Alors AccepterProposition COMPOSE la délégation s44 : surcharge du jour J vers B + transfert bicolore auto-dérivé s31 (R24)
  Et la proposition passe à « accepté », la case J converge sur le nouveau responsable
  Et si le recevant REFUSE à la place, la proposition passe à « refusé » SANS aucune écriture (store intact)
  Et le comportement est prouvé identique sur les deux adaptateurs (InMemory ET Mongo durable)

@back @vert
Scénario Sc.3 — Cas limite : ré-proposition last-write-wins, jour hors fenêtre, deux adaptateurs
  Étant donné un imprévu sur un jour J pour un enfant E
  Quand un échange est proposé en réaction PUIS re-proposé (autre versActeur) sur le même jour/enfant
  Alors la dernière proposition GAGNE (last-write-wins R11), sans doublon de proposition pending
  Et un imprévu situé sur un jour J HORS de la fenêtre de grille chargée accepte une proposition greffée sans crash
  Et le comportement est prouvé identique sur les deux adaptateurs (InMemory ET Mongo durable)

@back @vert
Scénario Sc.4 — Cas erreur / invariant : refus avant écriture, modèles imprévu/proposition séparés
  Étant donné un imprévu consigné et une demande de proposition greffée en réaction
  Quand le versActeur est SOI-MÊME, ou un délégataire INCONNU, ou un acteur ORPHELIN
  Alors la proposition est REFUSÉE AVANT écriture (aucune Proposition créée, aucune écriture partielle, store intact)
  Et dans tous les cas l'imprévu d'origine reste un FAIT informatif au journal, non muté, non « résolu » par la tentative
  Et la résolution ne consulte jamais le journal (imprévu s48 et proposition s47 restent des modèles SÉPARÉS)

@ihm @pending
Scénario Sc.5 — Action « proposer un échange » dans la notif d'imprévu de la cloche, Parent-gated, Échap = Annuler
  Étant donné le panneau de la cloche s47 affichant une notif d'imprévu « X est malade le 12 » (jour J, enfant E)
  Quand un Parent active l'action « proposer un échange » attachée à cette notif d'imprévu
  Alors la mini-dialog « proposer un échange » s47 s'ouvre PRÉ-REMPLIE avec le jour J et l'enfant E de l'imprévu
  Et le Parent n'a plus qu'à choisir le versActeur ; la commande est émise par le CANAL D'ÉCRITURE (jamais la diffusion)
  Et Échap annule et ferme la mini-dialog (port IEcouteurEchapModal s33), sans commande
  Et un Invité ne voit NI la cloche NI l'action (Parent-gated, cohérent s44/s47/s48) ; la notif d'imprévu reste informative

@ihm @pending
Scénario Sc.6 — La proposition issue de l'imprévu est actionnable chez le recevant et la case converge
  Étant donné une proposition greffée sur un imprévu, émise vers le recevant B
  Quand B ouvre SA cloche
  Alors la notification de proposition y figure ACTIONNABLE (Accepter / Refuser via mini-dialog s47)
  Et Accepter compose la délégation s44 → la case du jour J bascule sur B (transfert bicolore dérivé s31)
  Et Refuser clôt la proposition sans aucune écriture ; l'imprévu d'origine reste, lui, une notif informative inchangée

@ihm @pending
Scénario Sc.7 — Temps réel : la cloche d'un 2ᵉ écran converge par diffusion porteuse de payload, 0 GET
  Étant donné deux écrans connectés (le proposant et le recevant concernés par le jour/enfant)
  Quand une proposition est greffée sur un imprévu depuis le premier écran, puis acceptée depuis le second
  Alors la cloche du second écran reçoit la proposition via la diffusion porteuse de payload (INotificateurChangement s47)
  Et badge de non-lus, panneau et case convergent PAR REPROJECTION CLIENT, 0 GET dédié sur push
  Et la diffusion ne déclenche aucune écriture (elle porte une donnée de LECTURE — séparation des canaux tenue)
```

# Retours produit (PO)

<!-- Rempli au gate G3. -->
