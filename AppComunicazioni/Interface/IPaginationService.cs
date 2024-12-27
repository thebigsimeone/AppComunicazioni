namespace AppComunicazioni.Interface
{
    public interface IPaginationService
    {
        Task<(IQueryable<T> PaginatedData, int TotalPages)> PaginateAsync<T>(
                                                            IQueryable<T> query,
                                                            int pageNumber,
                                                            int pageSize
                                                        ) where T : class;
    }
}
