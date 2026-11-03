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
            // Parameter values are added as comments above the command text
            var parameters = command.Parameters.Cast<DbParameter>()
                .Select(p => $"-- {p.ParameterName} = {Format(p.Value)}");
            Add(_http.HttpContext, string.Join("\n", parameters.Append(command.CommandText.Trim())));
        }

        internal static void Add(HttpContext? context, string sql)
        {
            if (context == null) return;
            // Several contexts of one request may run queries in parallel (the parallel processing page)
            lock (context.Items)
            {
                if (context.Items[Key] is not List<string> list)
                    context.Items[Key] = list = new List<string>();
                list.Add(sql);
            }
        }

        private static string Format(object? value) => value switch
        {
            null or DBNull => "NULL",
            string s => $"'{s}'",
            DateTime d => $"'{d:yyyy-MM-dd HH:mm:ss}'",
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            System.Collections.IEnumerable list => "{" + string.Join(", ", list.Cast<object?>().Select(Format)) + "}",
            _ => value.ToString() ?? ""
        };
    }

    // Adds the transaction boundaries (BEGIN / COMMIT / ROLLBACK) to the same list of captured SQL
    public class TransactionCaptureInterceptor : DbTransactionInterceptor
    {
        private readonly IHttpContextAccessor _http;

        public TransactionCaptureInterceptor(IHttpContextAccessor http) => _http = http;

        public override ValueTask<DbTransaction> TransactionStartedAsync(DbConnection connection, TransactionEndEventData eventData, DbTransaction result, CancellationToken cancellationToken = default)
        {
            SqlCaptureInterceptor.Add(_http.HttpContext, $"BEGIN TRANSACTION ISOLATION LEVEL {IsolationName(result.IsolationLevel)}");
            return ValueTask.FromResult(result);
        }

        // ReadCommitted → READ COMMITTED, as in SQL
        private static string IsolationName(System.Data.IsolationLevel level) =>
            System.Text.RegularExpressions.Regex.Replace(level.ToString(), "(?<=[a-z])(?=[A-Z])", " ").ToUpperInvariant();

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            SqlCaptureInterceptor.Add(_http.HttpContext, "COMMIT");
            return Task.CompletedTask;
        }

        public override Task TransactionRolledBackAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            SqlCaptureInterceptor.Add(_http.HttpContext, "ROLLBACK");
            return Task.CompletedTask;
        }
    }
}
