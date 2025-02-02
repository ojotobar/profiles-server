namespace ProfessionalProfiles.Graph.Validations
{
    internal class ValidationExtensions
    {
        internal static bool BeAValidDateRange(DateTime startDate, DateTime? endDate)
        {
            return !endDate.HasValue || (endDate.HasValue && startDate.Date < endDate.Value.Date);
        }

        internal static bool BeAValidEndDate(DateTime? date)
        {
            return !date.HasValue || (date.HasValue && !date.Equals(default));
        }

        internal static bool BeAValidDate(DateTime date)
        {
            return !date.Equals(default);
        }
    }
}
