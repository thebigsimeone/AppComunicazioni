using AppComunicazioni.Interface;
using Microsoft.EntityFrameworkCore;

public class PaginationService : IPaginationService
{
    public async Task<(IQueryable<T> PaginatedData, int TotalPages)> PaginateAsync<T>(
        IQueryable<T> query, int pageNumber, int pageSize) where T : class
    {
        if (pageNumber < 1) pageNumber = 1;

        // Conta totale degli elementi
        var totalItems = await query.CountAsync();
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 1;

        var paginatedData = query
            .OrderBy(e => EF.Property<object>(e, "Id")) // Ordinamento per 'Id'
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return (paginatedData, totalPages);
    }
}
