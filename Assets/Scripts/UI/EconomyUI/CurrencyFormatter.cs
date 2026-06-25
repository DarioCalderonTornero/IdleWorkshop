public static class CurrencyFormatter
{
    public static string Format(double amount)
    {
        if (amount < 0) return "0";

        if (amount < 1_000)
            return ((long)amount).ToString();

        if (amount < 1_000_000)
            return FormatTier(amount, 1_000, "K");

        if (amount < 1_000_000_000)
            return FormatTier(amount, 1_000_000, "M");

        if (amount < 1_000_000_000_000)
            return FormatTier(amount, 1_000_000_000, "B");

        if (amount < 1_000_000_000_000_000)
            return FormatTier(amount, 1_000_000_000_000, "T");

        // Notación científica para números mayores a T
        return amount.ToString("0.##e+0");
    }

    private static string FormatTier(double amount, double divisor, string suffix)
    {
        double value = amount / divisor;

        string formatted;

        if (value < 10)
            formatted = value.ToString("0.##");       // 1.25K
        else if (value < 100)
            formatted = value.ToString("0.#");        // 10.3K
        else
            formatted = value.ToString("0");          // 125K

        return formatted + suffix;
    }
}
