using AppComunicazioni.Interface;
using Microsoft.EntityFrameworkCore;

namespace AppComunicazioni.Service
{
    public class PaginationService : IPaginationService
    {
        public async Task<(IQueryable<T> PaginatedData, int TotalPages)> PaginateAsync<T>(
            IQueryable<T> query,
            int pageNumber,
            int pageSize
        ) where T : class
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var paginatedData = query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return (paginatedData, totalPages);
        }
    }
}
