using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PharmacyCompounding.Data
{
    // Captures the SQL commands EF Core sends to PostgreSQL while handling a request,
    // so a report page can show what SQL its LINQ query turned into
    public class SqlCaptureInterceptor : DbCommandInterceptor
    {
        private const string Key = "CapturedSql";
        private readonly IHttpContextAccessor _http;

        public SqlCaptureInterceptor(IHttpContextAccessor http) => _http = http;

        public static List<string> For(HttpContext context) =>
            context.Items[Key] as List<string> ?? new List<string>();

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            Capture(command);
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            Capture(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Capture(command);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<object> result, CancellationToken cancellationToken = default)
        {
            Capture(command);
            return ValueTask.FromResult(result);
        }

        private void Capture(DbCommand command)
        {
            var context = _http.HttpContext;
            if (context == null) return;
            if (context.Items[Key] is not List<string> list)
                context.Items[Key] = list = new List<string>();

            // Parameter values are added as comments above the command text
            var parameters = command.Parameters.Cast<DbParameter>()
                .Select(p => $"-- {p.ParameterName} = {Format(p.Value)}");
            list.Add(string.Join("\n", parameters.Append(command.CommandText.Trim())));
        }

        private static string Format(object? value) => value switch
        {
            null or DBNull => "NULL",
            string s => $"'{s}'",
            DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'",
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
    }
}
