using System.Linq.Expressions;

namespace AppComunicazioni.Helpers
{
    public static class DynamicQueryHelper
    {
        /// <summary>
        /// Applica un filtro dinamico alla query su una proprietà specifica.
        /// </summary>
        /// <typeparam name="T">Tipo dell'entità.</typeparam>
        /// <param name="source">Origine della query.</param>
        /// <param name="propertyName">Nome della proprietà da filtrare.</param>
        /// <param name="value">Valore da confrontare.</param>
        /// <returns>Una query filtrata.</returns>
        public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> source, string propertyName, object value)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var constant = Expression.Constant(value);
            var comparison = Expression.Equal(property, constant);
            var lambda = Expression.Lambda<Func<T, bool>>(comparison, parameter);

            return source.Where(lambda);
        }

        /// <summary>
        /// Applica un ordinamento dinamico alla query.
        /// </summary>
        /// <typeparam name="T">Tipo dell'entità.</typeparam>
        /// <param name="source">Origine della query.</param>
        /// <param name="propertyName">Nome della proprietà per l'ordinamento.</param>
        /// <param name="ascending">True per ordinamento crescente, false per decrescente.</param>
        /// <returns>Una query ordinata.</returns>
        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> source, string propertyName, bool ascending)
        {
            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyName);
            var lambda = Expression.Lambda(property, parameter);

            var methodName = ascending ? "OrderBy" : "OrderByDescending";
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.Type);

            return (IQueryable<T>)method.Invoke(null, new object[] { source, lambda });
        }

        /// <summary>
        /// Applica filtri multipli dinamici alla query.
        /// </summary>
        /// <typeparam name="T">Tipo dell'entità.</typeparam>
        /// <param name="source">Origine della query.</param>
        /// <param name="filters">Un dizionario di filtri con nome proprietà e valore.</param>
        /// <returns>Una query filtrata.</returns>
        public static IQueryable<T> ApplyMultipleFilters<T>(this IQueryable<T> source, Dictionary<string, object> filters)
        {
            if (filters == null || !filters.Any())
                return source;

            foreach (var filter in filters)
            {
                source = source.ApplyFilter(filter.Key, filter.Value);
            }

            return source;
        }

        /// <summary>
        /// Applica un intervallo di date dinamico alla query.
        /// </summary>
        /// <typeparam name="T">Tipo dell'entità.</typeparam>
        /// <param name="source">Origine della query.</param>
        /// <param name="startDateProperty">Nome della proprietà per la data di inizio.</param>
        /// <param name="endDateProperty">Nome della proprietà per la data di fine.</param>
        /// <param name="startDate">Data di inizio.</param>
        /// <param name="endDate">Data di fine.</param>
        /// <returns>Una query filtrata per intervallo di date.</returns>
        public static IQueryable<T> ApplyDateRangeFilter<T>(this IQueryable<T> source, string startDateProperty, string endDateProperty, DateTime? startDate, DateTime? endDate)
        {
            var parameter = Expression.Parameter(typeof(T), "x");

            if (startDate.HasValue)
            {
                var startProperty = Expression.Property(parameter, startDateProperty);
                var startConstant = Expression.Constant(startDate.Value);
                var startComparison = Expression.GreaterThanOrEqual(startProperty, startConstant);
                var startLambda = Expression.Lambda<Func<T, bool>>(startComparison, parameter);

                source = source.Where(startLambda);
            }

            if (endDate.HasValue)
            {
                var endProperty = Expression.Property(parameter, endDateProperty);
                var endConstant = Expression.Constant(endDate.Value);
                var endComparison = Expression.LessThanOrEqual(endProperty, endConstant);
                var endLambda = Expression.Lambda<Func<T, bool>>(endComparison, parameter);

                source = source.Where(endLambda);
            }

            return source;
        }

        /// <summary>
        /// Combina filtri, ordinamenti e paginazione in una singola query dinamica.
        /// </summary>
        /// <typeparam name="T">Tipo dell'entità.</typeparam>
        /// <param name="source">Origine della query.</param>
        /// <param name="filters">Un dizionario di filtri.</param>
        /// <param name="sortField">Campo per l'ordinamento.</param>
        /// <param name="sortOrder">Ordinamento: "asc" o "desc".</param>
        /// <param name="pageNumber">Numero di pagina.</param>
        /// <param name="pageSize">Dimensione della pagina.</param>
        /// <returns>Una query filtrata, ordinata e paginata.</returns>
        public static IQueryable<T> BuildDynamicQuery<T>(this IQueryable<T> source, Dictionary<string, object>? filters, string sortField, string sortOrder, int pageNumber, int pageSize)
        {
            if (filters != null && filters.Any())
            {
                source = source.ApplyMultipleFilters(filters);
            }

            bool ascending = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase);
            source = source.ApplySorting(sortField, ascending);

            if (pageSize > 0)
            {
                source = source.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            }

            return source;
        }
    }
}
