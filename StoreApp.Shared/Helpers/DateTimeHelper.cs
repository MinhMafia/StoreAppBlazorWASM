namespace StoreApp.Shared.Helpers
{
    /// <summary>
    /// Helper class for consistent timezone handling across the application.
    /// All dates are stored in UTC. Use these methods to convert for display/comparison.
    /// Vietnam timezone is UTC+7.
    /// </summary>
    public static class DateTimeHelper
    {
        private const int VietnamUtcOffset = 7;

        /// <summary>
        /// Get current UTC time
        /// </summary>
        public static DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Get current Vietnam time (for display purposes)
        /// </summary>
        public static DateTime VietnamNow => DateTime.UtcNow.AddHours(VietnamUtcOffset);

        /// <summary>
        /// Get current Vietnam date (date only, for date comparisons)
        /// </summary>
        public static DateTime VietnamToday => VietnamNow.Date;

        /// <summary>
        /// Convert UTC datetime to Vietnam time
        /// </summary>
        public static DateTime ToVietnamTime(DateTime utcDateTime)
        {
            return utcDateTime.AddHours(VietnamUtcOffset);
        }

        /// <summary>
        /// Convert nullable UTC datetime to Vietnam time
        /// </summary>
        public static DateTime? ToVietnamTime(DateTime? utcDateTime)
        {
            return utcDateTime?.AddHours(VietnamUtcOffset);
        }

        /// <summary>
        /// Convert Vietnam time to UTC for storage
        /// </summary>
        public static DateTime ToUtc(DateTime vietnamDateTime)
        {
            return vietnamDateTime.AddHours(-VietnamUtcOffset);
        }

        /// <summary>
        /// Convert nullable Vietnam time to UTC for storage
        /// </summary>
        public static DateTime? ToUtc(DateTime? vietnamDateTime)
        {
            return vietnamDateTime?.AddHours(-VietnamUtcOffset);
        }

        /// <summary>
        /// Format UTC datetime for display in Vietnam timezone
        /// </summary>
        public static string FormatVietnam(DateTime? utcDateTime, string format = "dd/MM/yyyy")
        {
            if (!utcDateTime.HasValue) return "N/A";
            return ToVietnamTime(utcDateTime.Value).ToString(format);
        }

        /// <summary>
        /// Format UTC datetime for display with time in Vietnam timezone
        /// </summary>
        public static string FormatVietnamDateTime(DateTime? utcDateTime)
        {
            return FormatVietnam(utcDateTime, "dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Check if a UTC date is in the past (compared to Vietnam today)
        /// </summary>
        public static bool IsExpired(DateTime? utcEndDate)
        {
            if (!utcEndDate.HasValue) return false;
            return ToVietnamTime(utcEndDate.Value).Date < VietnamToday;
        }

        /// <summary>
        /// Check if a UTC date is in the future (compared to Vietnam today)
        /// </summary>
        public static bool IsScheduled(DateTime? utcStartDate)
        {
            if (!utcStartDate.HasValue) return false;
            return ToVietnamTime(utcStartDate.Value).Date > VietnamToday;
        }

        /// <summary>
        /// Check if current Vietnam date is within the date range (inclusive)
        /// </summary>
        public static bool IsWithinRange(DateTime? utcStartDate, DateTime? utcEndDate)
        {
            var today = VietnamToday;
            var startOk = !utcStartDate.HasValue || ToVietnamTime(utcStartDate.Value).Date <= today;
            var endOk = !utcEndDate.HasValue || ToVietnamTime(utcEndDate.Value).Date >= today;
            return startOk && endOk;
        }
    }
}
