<!-- Généré via le skill make-gherkin (test sous pression autonome). Exemple, non lié au produit planning-de-garde. -->

# RDV médicaux — Analyse & scénarios

## Analyse technique

- **Composants impactés** — `AgendaService` (.NET, gestion des plages + génération créneaux), `ReservationService` (.NET, vérification optimiste + écriture atomique), composant Blazor `AgendaView` (affichage temps réel des créneaux), hub SignalR `AgendaHub` (push d'état créneau aux clients connectés à un agenda).
- **Contrats de données** — `DisponibilitéDto { praticienId, debut, fin }`, `CreneauDto { id, praticienId, debut, fin, etat: libre|réservé|indisponible|passé }`, `ReservationDto { creauId, patientId, typeConsultationId }` → retourne `RdvDto { id, creauId, patientId, typeConsultation, debut, fin }` ou `409 Conflict { code: "CRENEAU_DEJA_RESERVE", message }`.
- **Points d'attention TDD** — tester `ReservationService` avec un double accès concurrent en mémoire avant de brancher la BDD (race condition contrôlée) ; tester la bascule "passé" avec une horloge injectable ; valider le rejet de chevauchement de plages en isolation pure, avant tout test d'intégration SignalR.

## Scénarios

Feature: Gestion de l'agenda médical
  En tant que praticien ou patient,
  Je veux configurer des plages de disponibilité et réserver des créneaux
  Afin de garantir un agenda fiable sans double-booking

@nominal
Scenario 1: Le praticien crée une plage de disponibilité
  Given le praticien "Dr Martin" n'a aucune plage configurée
  When il crée une plage du lundi 09h00 au lundi 12h00
  Then l'agenda de "Dr Martin" contient une plage "lundi 09h00-12h00" à l'état actif
  And les créneaux de 30 min sont générés : 09h00, 09h30, 10h00, 10h30, 11h00, 11h30

@nominal
Scenario 2: Le patient réserve un créneau libre
  Given le créneau "Dr Martin / lundi 10h00" est à l'état "libre"
  And le type de consultation "Consultation générale" a une durée de 30 min
  When le patient "Alice Dupont" réserve ce créneau avec le type "Consultation générale"
  Then le créneau "Dr Martin / lundi 10h00" passe à l'état "réservé"
  And le patient "Alice Dupont" voit un RDV listé : "Dr Martin — lundi 10h00, Consultation générale"
  And l'agenda de "Dr Martin" affiche ce créneau comme "réservé" avec le nom "Alice Dupont"

@nominal
Scenario 3: L'agenda du praticien est mis à jour en temps réel via SignalR
  Given deux navigateurs sont ouverts sur l'agenda de "Dr Martin"
  And le créneau "lundi 10h00" est à l'état "libre" dans les deux vues
  When le patient "Alice Dupont" réserve le créneau "lundi 10h00" depuis le navigateur A
  Then le navigateur B affiche automatiquement le créneau "lundi 10h00" à l'état "réservé"
  And aucune action manuelle de rechargement n'est requise dans le navigateur B

@nominal
Scenario 4: Le praticien crée un type de consultation
  Given le praticien "Dr Martin" n'a aucun type de consultation
  When il crée le type "Téléconsultation" avec une durée de 20 min
  Then la liste des types de "Dr Martin" contient "Téléconsultation — 20 min"
  And ce type est proposable lors d'une réservation patient

@limite
Scenario 5: Le praticien tente de créer deux plages qui se chevauchent
  Given le praticien "Dr Martin" a une plage "lundi 09h00-12h00"
  When il tente de créer une plage "lundi 11h00-13h00"
  Then la création est rejetée avec le message "Cette plage chevauche une disponibilité existante"
  And la plage "lundi 09h00-12h00" reste inchangée

@limite
Scenario 6: Un créneau bascule automatiquement à l'état "passé" quand son horaire est dépassé
  Given le créneau "Dr Martin / lundi 10h00" est à l'état "libre"
  And l'horloge système indique lundi 10h01
  When l'agenda est consulté
  Then le créneau "Dr Martin / lundi 10h00" est affiché à l'état "passé"
  And il n'apparaît plus dans la liste des créneaux réservables

@limite
Scenario 7: Un patient tente de réserver un créneau déjà à l'état "passé"
  Given le créneau "Dr Martin / lundi 10h00" est à l'état "passé"
  When le patient "Bob Lemaire" tente de le réserver
  Then la réservation est rejetée avec le message "Ce créneau n'est plus disponible"
  And aucun RDV n'est créé pour "Bob Lemaire"

@erreur
Scenario 8: Deux patients tentent simultanément de réserver le même créneau libre
  Given le créneau "Dr Martin / lundi 14h00" est à l'état "libre"
  And le patient "Alice Dupont" et le patient "Bob Lemaire" soumettent simultanément une réservation sur ce créneau
  When les deux requêtes arrivent concurremment au service de réservation
  Then exactement un RDV est créé (premier arrivé côté serveur)
  And le second patient reçoit une erreur HTTP 409 avec le code "CRENEAU_DEJA_RESERVE"
  And l'UI du second patient affiche "Ce créneau vient d'être réservé par quelqu'un d'autre"
  And le créneau est à l'état "réservé" dans l'agenda de "Dr Martin"

@erreur
Scenario 9: Un patient tente de réserver un créneau déjà réservé
  Given le créneau "Dr Martin / lundi 14h00" est à l'état "réservé" (par "Alice Dupont")
  When le patient "Bob Lemaire" tente de réserver ce créneau
  Then la réservation est rejetée avec le code "CRENEAU_DEJA_RESERVE"
  And le message affiché est "Ce créneau n'est plus disponible à la réservation"
  And le RDV d'"Alice Dupont" reste inchangé

@erreur
Scenario 10: Le praticien supprime une plage qui contient un créneau réservé
  Given la plage "Dr Martin / lundi 09h00-12h00" contient le créneau "10h00" à l'état "réservé"
  When le praticien tente de supprimer cette plage
  Then la suppression est rejetée avec le message "Cette plage contient des rendez-vous confirmés"
  And la plage et le créneau réservé restent inchangés
