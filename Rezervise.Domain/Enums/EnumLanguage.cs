namespace Rezervise.Domain.Enums
{
    public enum EnumLanguage
    {
        Turkish = 0,
        English = 1,
        German = 2,
        French = 3,
        Spanish = 4,
        Arabic = 5,
        Russian = 6
    }

    public static class EnumLanguageExtensions
    {
        public static string ToCode(this EnumLanguage language)
        {
            return language switch
            {
                EnumLanguage.Turkish => "tr",
                EnumLanguage.English => "en",
                EnumLanguage.German => "de",
                EnumLanguage.French => "fr",
                EnumLanguage.Spanish => "es",
                EnumLanguage.Arabic => "ar",
                EnumLanguage.Russian => "ru",
                _ => "tr"
            };
        }

        public static EnumLanguage FromCode(string code)
        {
            return code?.ToLowerInvariant() switch
            {
                "tr" => EnumLanguage.Turkish,
                "en" => EnumLanguage.English,
                "de" => EnumLanguage.German,
                "fr" => EnumLanguage.French,
                "es" => EnumLanguage.Spanish,
                "ar" => EnumLanguage.Arabic,
                "ru" => EnumLanguage.Russian,
                _ => EnumLanguage.Turkish
            };
        }
    }
}