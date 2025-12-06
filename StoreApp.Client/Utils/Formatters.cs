namespace StoreApp.Client.Utils
{
    public static class Formatters
    {
        public static string FormatCurrency(decimal value)
        {
            return value.ToString("N0") + " đ";
        }

        public static string FormatCurrency(double value)
        {
            return FormatCurrency((decimal)value);
        }
    }
}
