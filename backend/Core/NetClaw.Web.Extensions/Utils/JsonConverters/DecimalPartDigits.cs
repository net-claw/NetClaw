namespace NetClaw.AspNetCore.Extensions.Utils.JsonConverters
{
    public static class DecimalPartDigits
    {
        public const int MoneyDecimalDigit = 6;
        public const int RateDecimalDigit = 12;

        public static decimal StandardizeDecimalDigit(this decimal asDecimal)
        {
            return Math.Round(asDecimal, MoneyDecimalDigit);
        }

        public static int Compare(decimal left, decimal right)
        {
            return decimal.Compare(Math.Round(left, MoneyDecimalDigit), Math.Round(right, MoneyDecimalDigit));
        }
    }
}
