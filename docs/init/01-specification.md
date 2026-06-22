# Planning de garde — Organisation des semaines de garde

## Contexte

Une app où parents et intervenants (nounou, grands-parents…) organisent à
l'avance et partagent les semaines de garde des enfants d'un foyer.

## Mécaniques de base

- Un créneau de garde a un horaire (début → fin), un responsable unique, un lieu et une activité
- Une journée est une suite de créneaux enchaînés
- Le planning suit un cycle de plusieurs semaines qui se répète automatiquement
- Deux rôles d'accès : Parent et Invité

## Règles de gestion

### Foyer & enfants

1. **Multi-enfants** — Un foyer peut compter plusieurs enfants, chacun avec sa propre organisation de garde

2. **Familles recomposées** — Un foyer peut mélanger des enfants de parents différents, tous visibles dans le même planning

### Rôles & accès

3. **Deux rôles** — Un Parent gère tout (créneaux, lieux, invitations) ; un Invité est en consultation seule

4. **Modification réservée aux Parents** — Seul un Parent peut créer ou éditer un créneau

### Planning & créneaux

5. **Responsable unique** — Un créneau a toujours un seul responsable (pas de « X ou Y »)

6. **Transferts explicites** — Un créneau précise qui dépose, qui récupère, et à quelle heure

7. **Cycle récurrent** — Le planning se répète selon un cycle de plusieurs semaines (ex : semaine paire / impaire)

8. **Exception ponctuelle** — Un jour précis peut être surchargé sans casser le cycle de fond ; le cycle reprend ensuite

9. **Lieux éditables** — La liste des lieux (domicile A/B, école, nounou, activité…) est librement modifiable

### Modifications & notifications

10. **Modification directe** — Un Parent applique son changement immédiatement ; les autres sont notifiés (pas de workflow de validation en v1)

11. **Notifications in-app** — Les notifications couvrent les changements de planning et les rappels de transfert
