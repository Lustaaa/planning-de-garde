using System;
using System.Collections.Generic;

namespace RdvMedicaux.Domain;

public class AgendaService
{
    private static readonly TimeSpan DureeCreneauDefaut = TimeSpan.FromMinutes(30);

    private readonly Dictionary<string, List<Plage>>   _plages   = new();
    private readonly Dictionary<string, List<Creneau>> _creneaux = new();

    public void CreerPlage(string praticienId, DateTime debut, DateTime fin)
    {
        var plage = new Plage { PraticienId = praticienId, Debut = debut, Fin = fin };

        if (!_plages.ContainsKey(praticienId))
            _plages[praticienId] = new List<Plage>();

        _plages[praticienId].Add(plage);

        // Génération des créneaux de 30 min
        if (!_creneaux.ContainsKey(praticienId))
            _creneaux[praticienId] = new List<Creneau>();

        var curseur = debut;
        while (curseur + DureeCreneauDefaut <= fin)
        {
            _creneaux[praticienId].Add(new Creneau
            {
                PraticienId = praticienId,
                Debut = curseur,
                Fin   = curseur + DureeCreneauDefaut
            });
            curseur += DureeCreneauDefaut;
        }
    }

    public List<Plage> GetPlages(string praticienId) =>
        _plages.TryGetValue(praticienId, out var list) ? list : new List<Plage>();

    public List<Creneau> GetCreneaux(string praticienId) =>
        _creneaux.TryGetValue(praticienId, out var list) ? list : new List<Creneau>();
}
