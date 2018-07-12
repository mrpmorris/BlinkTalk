public static class NullableIntExtensions
{
    public static string ToStringOrDefault(this int? value)
    {
        if (value == null)
            return "-1";
        return value.ToString();
    }
}
