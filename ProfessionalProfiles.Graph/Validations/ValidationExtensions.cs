using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.String;

namespace ProfessionalProfiles.Graph.Validations
{
    internal class ValidationExtensions
    {
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
