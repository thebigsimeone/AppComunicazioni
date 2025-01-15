using AppComunicazioni.Interface;
using AppComunicazioni.Models;
using Microsoft.EntityFrameworkCore;

public class PaginationService : IPaginationService
{
    public async Task<(IQueryable<T> PaginatedData, int TotalPages)> PaginateAsync<T>(
    IQueryable<T> query, int pageNumber, int pageSize) where T : class
    {
        if (pageNumber < 1) pageNumber = 1;

        var totalItems = await query.CountAsync();
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 1;

        var paginatedData = query
            .OrderBy(e => EF.Property<int>(e, "Id"))  // Correzione qui
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return (paginatedData, totalPages);
    }
}
