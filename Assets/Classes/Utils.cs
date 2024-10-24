using System;
using System.Globalization;

public class StringUtils
{
    public static string ToPascalCase(string fullName)
    {
        // Divide el nombre completo en palabras
        string[] words = fullName.Split(new char[] { ' ', '\t', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        // Convierte cada palabra a Pascal Case
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(words[i].ToLower());
        }

        // Unir las palabras de nuevo en un solo string
        return string.Join(" ", words);
    }

    public static string ConvertMinutesToTimeString(int minutes)
    {
        int hours = minutes / 60;
        int remainingMinutes = minutes % 60;
        return string.Format("{0:D2}H:{1:D2}M", hours, remainingMinutes);
    }
}