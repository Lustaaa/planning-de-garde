// Test d'acceptation — Scénario 1 (BDD)
// @nominal : Le praticien crée une plage de disponibilité

using System;
using System.Collections.Generic;
using System.Linq;
using RdvMedicaux.Domain;
using Xunit;

namespace RdvMedicaux.Tests;

public class Scenario1_PraticienCreePlageDisponibilite
{
    [Fact]
    public void Scenario1_Nominal_PraticienCreePlage_CreneauxGeneres()
    {
        // Given le praticien "Dr Martin" n'a aucune plage configurée
        var service = new AgendaService();
        var praticienId = "dr-martin";

        // When il crée une plage du lundi 09h00 au lundi 12h00
        var lundi = new DateTime(2026, 6, 22); // lundi de référence (ISO)
        var debut = lundi.Date.AddHours(9);
        var fin   = lundi.Date.AddHours(12);

        service.CreerPlage(praticienId, debut, fin);

        // Then l'agenda contient une plage "lundi 09h00-12h00" à l'état actif
        var plages = service.GetPlages(praticienId);
        Assert.Single(plages);
        var plage = plages[0];
        Assert.Equal(debut, plage.Debut);
        Assert.Equal(fin,   plage.Fin);
        Assert.Equal(EtatPlage.Active, plage.Etat);

        // And les créneaux de 30 min sont générés : 09h00, 09h30, 10h00, 10h30, 11h00, 11h30
        var creneaux = service.GetCreneaux(praticienId);
        var heuresAttendues = new List<TimeSpan>
        {
            new(9, 0, 0), new(9, 30, 0), new(10, 0, 0),
            new(10, 30, 0), new(11, 0, 0), new(11, 30, 0)
        };

        Assert.Equal(heuresAttendues.Count, creneaux.Count);
        foreach (var h in heuresAttendues)
        {
            Assert.Contains(creneaux, c => c.Debut == lundi.Date.Add(h));
        }
    }
}
