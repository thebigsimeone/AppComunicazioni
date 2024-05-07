namespace AppComunicazioni.Models;
using System;
using System.Collections.Generic;

public class ComunicazioniViewModel
{
    public IEnumerable<Comunicazioni>? Comunicazioni { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}