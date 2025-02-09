using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.String;
using System.Text.RegularExpressions;

namespace ProfessionalProfiles.Graph.Validations
{
    internal class ValidationExtensions
    {
        public static bool IsAphaWithSomeChars(string str)
        {
            return !string.IsNullOrWhiteSpace(str) && new Regex("^[a-zA-Z .+#/]*$").IsMatch(str.Trim());
        }

        public static bool IsAphaNumeric(string str)
        {
            return !string.IsNullOrWhiteSpace(str) && new Regex("^[a-zA-Z0-9]*$").IsMatch(str.Trim());
        }

        public static bool IsApha(string str)
        {
            return !string.IsNullOrWhiteSpace(str) && new Regex("^[a-zA-Z]*$").IsMatch(str.Trim());
        }

        internal static bool BeAValidListOfString(List<string>? strings)
        {
            return !strings!.IsNotNullOrEmpty() || (strings!.IsNotNullOrEmpty() && strings!.All(s => s.IsNotNullOrEmpty()));
        }

        internal static bool BeAValidDateRange(DateTime startDate, DateTime? endDate)
        {
            return !endDate.HasValue || (endDate.HasValue && startDate.Date < endDate.Value.Date);
        }

        internal static bool BeAValidDate(DateTime? date)
        {
            return !date.HasValue || (date.HasValue && !date.Equals(default));
        }

        internal static bool BeAValidDate(DateTime date)
        {
            return !date.Equals(default);
        }

        internal static bool BeAValidNumber(int? number)
        {
            return !number.HasValue || (number.HasValue && !number.Equals(default));
        }
    }
}
