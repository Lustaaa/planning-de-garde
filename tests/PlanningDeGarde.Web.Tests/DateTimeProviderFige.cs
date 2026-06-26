using System;
using PlanningDeGarde.Application;

namespace PlanningDeGarde.Web.Tests;

/// <summary>
/// Double de test du port <see cref="IDateTimeProvider"/> : fige « aujourd'hui » pour le
/// déterminisme. Les formulaires pré-remplissent leur date depuis ce port (jamais
/// <c>DateTime.Today</c> en dur), si bien qu'une pose validée « sans toucher aux dates » tombe à la
/// date injectée — symétrie avec <c>Projeter(dateReference)</c> côté lecture.
/// </summary>
public sealed class DateTimeProviderFige(DateTime aujourdhui) : IDateTimeProvider
{
    public DateTime Maintenant => aujourdhui;
    public DateOnly Aujourdhui => DateOnly.FromDateTime(aujourdhui);
}
