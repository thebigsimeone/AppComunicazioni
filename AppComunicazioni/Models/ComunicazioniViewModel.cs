namespace AppComunicazioni.Models;
using System;
using System.Collections.Generic;

public class ComunicazioniViewModel
{
    public List<Comunicazioni> Comunicazioni { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string SearchTerm { get; set; }
    public string CodCor { get; set; } // CodCor per il filtro specifico
    public DateTime? MonthYear { get; set; } // MonthYear per il filtro del mese e anno
    public string SortField { get; set; }
    public string SortOrder { get; set; }
    public int PageSize { get; set; } = 10;

}