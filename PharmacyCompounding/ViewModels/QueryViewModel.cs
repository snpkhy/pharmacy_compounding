using Microsoft.AspNetCore.Mvc.Rendering;
using PharmacyCompounding.Models;

namespace PharmacyCompounding.ViewModels
{
    // Description and result of one query on the Queries page: form parameters, table, totals, executed SQL
    public class QueryViewModel
    {
        public string Action { get; set; } = string.Empty;
        public int Number { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // Queries that change data are sent with POST
        public bool IsPost { get; set; }
        public string SubmitText { get; set; } = "Виконати";
        public List<QueryParam> Params { get; } = new();

        public string? Message { get; set; }
        public string? Error { get; set; }
        public string? ResultTitle { get; set; }
        public List<(string Label, string Value)> Summary { get; } = new();

        public string[] Columns { get; private set; } = Array.Empty<string>();
        public bool[] RightAligned { get; private set; } = Array.Empty<bool>();
        public List<string[]> Rows { get; } = new();

        public List<string> Sql { get; set; } = new();

        public QueryViewModel Param(string name, string label, string type, object? value, IEnumerable<SelectListItem>? options = null)
        {
            Params.Add(new QueryParam { Name = name, Label = label, Type = type, Value = Format(value, forInput: true), Options = options?.ToList() });
            return this;
        }

        public void Table(params string[] columns)
        {
            Columns = columns;
            RightAligned = new bool[columns.Length];
        }

        // Numeric columns are right-aligned
        public void Row(params object?[] values)
        {
            for (var i = 0; i < values.Length; i++)
                if (values[i] is int or long or decimal or double) RightAligned[i] = true;
            Rows.Add(values.Select(v => Format(v)).ToArray());
        }

        public void Total(string label, object? value) => Summary.Add((label, Format(value)));

        public static string Format(object? value, bool forInput = false) => value switch
        {
            null => forInput ? "" : "—",
            DateTime d when forInput => d.ToString("yyyy-MM-dd"),
            DateTime d => d.TimeOfDay == TimeSpan.Zero ? d.ToString("dd.MM.yyyy") : d.ToString("dd.MM.yyyy HH:mm"),
            decimal m when forInput => m.ToString("0.##"),
            decimal m => m.ToString("N2"),
            double x => x.ToString("0.##"),
            bool b => b ? "так" : "ні",
            Enum e => e.GetDisplayName(),
            _ => value.ToString() ?? ""
        };
    }

    public class QueryParam
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = "text";   // text, number, date, select
        public string Value { get; set; } = string.Empty;
        public List<SelectListItem>? Options { get; set; }
    }
}
