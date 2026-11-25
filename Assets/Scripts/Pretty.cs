using System.Globalization;

// Text helpers: "mr-mime" -> "Mr Mime", "special-attack" -> "Sp. Atk", etc.
public static class Pretty
{
    public static string Name(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        switch (raw)
        {
            case "nidoran-f": return "Nidoran (F)";
            case "nidoran-m": return "Nidoran (M)";
            case "farfetchd": return "Farfetch'd";
            case "mr-mime": return "Mr. Mime";
        }
        string spaced = raw.Replace('-', ' ');
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced);
    }

    public static string Stat(string raw)
    {
        switch (raw)
        {
            case "hp": return "HP";
            case "attack": return "Attack";
            case "defense": return "Defense";
            case "special-attack": return "Sp. Atk";
            case "special-defense": return "Sp. Def";
            case "speed": return "Speed";
            default: return Name(raw);
        }
    }

    // Pokédex texts come with hard line breaks and form feeds.
    public static string Flavor(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return raw.Replace('\n', ' ').Replace('\f', ' ').Replace('­', '-').Replace("  ", " ").Trim();
    }
}
