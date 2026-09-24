namespace GM_Desktop_Application.Models
{
    public static class CampaignText
    {
        // UTF-16 code units, matching WPF TextBox.MaxLength. Validate before trimming.
        public const int NameMaxLength = 200;
        public const int SystemNameMaxLength = 100;

        public static string? Validate(string? value, int maxLength, string label)
        {
            if (value is null || value.Length == 0)
            {
                return $"Enter a {label}.";
            }
            if (value.Length > maxLength)
            {
                return $"The {label} must be {maxLength} characters or fewer.";
            }
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsControl(character) || character is '\u2028' or '\u2029' or '\u061C' or '\u200E' or '\u200F' ||
                    character is >= '\u202A' and <= '\u202E' or >= '\u2066' and <= '\u206F')
                {
                    return $"The {label} cannot contain control characters, line breaks, or text-direction controls.";
                }
                if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    index++;
                }
                else if (char.IsSurrogate(character))
                {
                    return $"The {label} contains invalid Unicode text.";
                }
            }
            return string.IsNullOrWhiteSpace(value) ? $"Enter a {label}." : null;
        }
    }
}
