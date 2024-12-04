using AppComunicazioni.Models;

public class ComunicazioniViewModel
{
    public List<Comunicazioni>? Comunicazioni { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public string? CodCor { get; set; }
    public string? Servizio { get; set; }
    public DateTime? MonthYear { get; set; }
    public string? SortField { get; set; }
    public string? SortOrder { get; set; }
    public bool SoloRigheNonRestituite { get; set; } // Nuovo campo
    public int PageSize { get; set; } = 10;
}
