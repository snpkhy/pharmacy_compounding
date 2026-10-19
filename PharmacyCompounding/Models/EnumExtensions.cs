using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace PharmacyCompounding.Models
{
    public static class EnumExtensions
    {
        public static string GetDisplayName(this Enum value) =>
            value.GetType().GetMember(value.ToString()).First()
                 .GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
    }
}
