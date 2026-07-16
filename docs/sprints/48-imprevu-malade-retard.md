# Sprint 48 — Signalement d'imprévu dédié (malade / retard)

> **Palier 15** (spec [`notifications-et-echange.md`](../specs/notifications-et-echange.md), section « Imprévu &
> échange ») — le **signalement d'imprévu DÉDIÉ**, distinct de l'échange consenti s47. L'échange s47 couvre le cas
> **négocié / actionnable** (« peux-tu récupérer à ma place ? » → accepter/refuser). Ce sprint couvre le cas
> **NON négocié, purement INFORMATIF** : « l'enfant EST malade », « je serai en retard ce soir » — une **notification
> poussée dans la cloche s47**, **SANS aucun effet sur la résolution** (aucune surcharge, aucun transfert, aucune
> bascule de responsable). Réutilise intégralement les briques s47 (journal de changements, cloche, diffusion porteuse
> de payload) et le pattern d'entrée de menu clic-case (s44 délégation / s47 proposer-échange).

## Avancement — 6/7

| # | Scénario | Type | Statut |
|---|----------|------|--------|
| 1 | Signaler malade/retard consigne un événement au JOURNAL s47, SANS toucher la résolution (0 surcharge / 0 transfert / 0 bascule) | @back | ✅ |
| 2 | L'événement d'imprévu apparaît dans le flux notifications du/des acteur(s) concerné(s), trié par récence, lu/non-lu par utilisateur | @back | ✅ |
| 3 | Cas limite : motif optionnel vide accepté ; jour hors fenêtre chargée enregistré sans crash ; deux adaptateurs InMemory + Mongo durable | @back | ✅ |
| 4 | Cas erreur / gating back : type d'imprévu inconnu refusé sans écriture ; le journal reste trace non-autorité (résolution jamais impactée) | @back | ✅ |
| 5 | Entrée « signaler un imprévu » du menu clic-case (Parent-gated), choix malade/retard + motif optionnel, Échap = Annuler (port s33) | @ihm | ✅ |
| 6 | La notification d'imprévu apparaît dans la CLOCHE s47 (libellé informatif « X est malade le 12 »), lu/non-lu, PAS d'action de suivi | @ihm | ✅ |
| 7 | Temps réel : diffusion porteuse de payload (INotificateurChangement s47) → la cloche d'un 2ᵉ écran converge, 0 GET sur push | @ihm | 🔴 |

## Goal & cadrage

**Objectif** : donner une **voix au cas non-consenti** de l'imprévu. Aujourd'hui un parent ne peut que **négocier**
un échange (s47) ou **déléguer** unilatéralement (s44) — il n'a **aucun moyen de simplement SIGNALER** un fait subi
(« l'enfant est malade », « je serai en retard »). Ce signalement **informe** les autres acteurs via la cloche, **sans
rien décider** sur le planning.

**POINT DE CONCEPTION (mineur, tranché au cadrage) — signalement INFORMATIF, pas actionnable.**
Le signalement d'imprévu est **purement INFORMATIF** ce sprint : il produit une **notification** (« X est malade le 12 »,
« Y sera en retard le 12 »), **sans action de suivi** attachée (pas d'accepter/refuser, pas de bascule proposée).
**Justification** : le cas **actionnable / négocié existe déjà** (échange proposition→accord s47). L'imprévu dédié est
le cas **subi, non-négocié** — il n'y a rien à accepter, juste à **prévenir**. Une éventuelle action de suivi (« proposer
un échange en réaction à un imprévu ») est **hors scope** (backlog, dépend de l'échange s47 déjà livré).

**SURFACE — AUCUNE surface neuve.** Réutilise :
- l'**entrée de menu clic-case** (comme « déléguer ce jour » s44 et « proposer un échange » s47) : une nouvelle entrée
  **« signaler un imprévu »**, Parent-gated ;
- la **cloche s47** (barre du haut) comme surface de restitution — la notification d'imprévu est un événement du
  **journal de changements existant**, rendu dans le panneau cloche au même titre que délégations / plages / reprises /
  échanges.

**HORS SCOPE (bornes)** :
- **Action de suivi / réaction** à un imprévu (proposer un échange déclenché depuis la notif) — backlog.
- **Notifications push / e-mail externes** — la cloche reste **in-app** (SignalR), comme s47.
- **Multi-enfants / plage / récurrence** du signalement — un imprévu porte **un jour, un enfant**.
- **AUCUNE persistance neuve hors le journal s47** : réutilise `IJournalChangements` + `IEtatLectureNotifications`
  existants (le type d'événement d'imprévu s'y ajoute), aucun store neuf.

**POINTS DE VIGILANCE (anti-régression) :**
- **Journal = TRACE DE LECTURE non-autorité (invariant s47)** : le signalement d'imprévu **consigne** un événement mais
  **la résolution ne lit JAMAIS le journal** — signaler « malade » **ne change pas** qui est responsable, **n'écrit
  aucune surcharge, ne dérive aucun transfert, ne bascule aucun fond**. À prouver explicitement (Sc.1, Sc.4) : store des
  surcharges **intact**, case **inchangée**.
- **0-GET sur push maintenu (s44–s47)** : la cloche reprojette depuis la **diffusion porteuse de payload**
  (`INotificateurChangement`), jamais un GET dédié sur push (garde anti-flake [[flake-signalr-blast-radius]]).
- **Gating** : entrée visible **connecté && Parent** (Invité ne voit ni l'entrée ni la cloche), cohérent s44/s47.

## Scénarios

```gherkin
@back @vert
Scénario Sc.1 — Signaler un imprévu consigne au journal SANS toucher la résolution
  Étant donné un jour dont le responsable est résolu (surcharge > fond > neutre) et un enfant du foyer
  Quand un parent signale un imprévu « malade » (ou « retard ») sur ce jour pour cet enfant
  Alors un événement d'imprévu {type: malade|retard, jour, enfant, acteur signalant, horodatage via IDateTimeProvider}
      est consigné au JOURNAL DE CHANGEMENTS existant (IJournalChangements)
  Et le store des surcharges reste INTACT (aucune surcharge écrite)
  Et aucun transfert n'est dérivé, aucune bascule de responsable n'a lieu
  Et la résolution de la case reste STRICTEMENT inchangée (le journal n'est jamais lu par la résolution)

@back @vert
Scénario Sc.2 — L'imprévu apparaît dans le flux notifications, trié par récence, lu/non-lu par utilisateur
  Étant donné un imprévu signalé et des acteurs concernés par le jour/enfant
  Quand le flux de notifications d'un acteur concerné est restitué
  Alors l'événement d'imprévu y figure, trié par RÉCENCE d'écriture (le plus récent en tête)
  Et il porte l'état lu/non-lu PAR utilisateur (IEtatLectureNotifications) + entre dans le compteur de non-lus
  Et marquer-lu est idempotent (aucun doublon, compteur stable), sans affecter l'état non-lu d'un autre utilisateur

@back @vert
Scénario Sc.3 — Cas limite : motif vide, jour hors fenêtre, deux adaptateurs durables
  Étant donné un signalement d'imprévu avec un motif optionnel LAISSÉ VIDE
  Quand l'imprévu est consigné
  Alors l'enregistrement est valide (motif vide accepté, aucune écriture partielle)
  Et un imprévu signalé sur un jour HORS de la fenêtre de grille chargée s'enregistre sans crash (une date)
  Et le comportement est prouvé identique sur les deux adaptateurs (InMemory ET Mongo durable)

@back @vert
Scénario Sc.4 — Cas erreur / invariant : type inconnu refusé, journal reste non-autorité
  Étant donné une demande de signalement portant un type d'imprévu INCONNU (ni malade ni retard)
  Quand la commande est traitée
  Alors elle est REFUSÉE AVANT écriture (aucun événement consigné, aucune écriture partielle)
  Et pour un signalement valide, aucune lecture ultérieure de la résolution ne consulte le journal
  Et écrire/supprimer une surcharge par ailleurs n'altère jamais la vérité via le journal (séparation tenue)

@ihm @vert
Scénario Sc.5 — Entrée « signaler un imprévu » du menu clic-case, Parent-gated, Échap = Annuler
  Étant donné le hub /planning et le menu clic-case d'un jour (à côté de « déléguer ce jour » s44 / « proposer un échange » s47)
  Quand un Parent ouvre le menu et choisit « signaler un imprévu »
  Alors un mini-dialog s'ouvre avec le choix « malade » / « retard » + un champ motif OPTIONNEL
  Et la commande est émise par le CANAL D'ÉCRITURE (jamais la diffusion)
  Et Échap annule et ferme le mini-dialog (port IEcouteurEchapModal s33), sans commande
  Et un Invité ne voit NI le menu clic-case NI l'entrée (Parent-gated, cohérent s44/s47)

@ihm @vert
Scénario Sc.6 — La notification d'imprévu apparaît dans la cloche, informative, sans action de suivi
  Étant donné un imprévu « malade » signalé sur le 12 pour un enfant
  Quand j'ouvre le panneau de la cloche s47
  Alors la notification « X est malade le 12 » (ou « Y sera en retard le 12 ») y figure, INFORMATIVE
  Et elle porte l'état lu/non-lu et l'action marquer-lu, comme les autres événements
  Et elle N'EXPOSE AUCUNE action de suivi (pas d'accepter/refuser — l'imprévu informatif n'est pas négociable)

@ihm @pending
Scénario Sc.7 — Temps réel : la cloche d'un 2ᵉ écran converge par diffusion porteuse de payload, 0 GET
  Étant donné deux écrans connectés en tant qu'acteurs concernés par le jour/enfant
  Quand un imprévu est signalé depuis le premier écran
  Alors la cloche du second écran reçoit l'événement via la diffusion porteuse de payload (INotificateurChangement s47)
  Et le badge de non-lus et le panneau convergent PAR REPROJECTION CLIENT, 0 GET dédié sur push
  Et la diffusion ne déclenche aucune écriture (elle porte une donnée de LECTURE — séparation des canaux tenue)
```

# Retours produit (PO)

<!-- Rempli au gate G3. -->
