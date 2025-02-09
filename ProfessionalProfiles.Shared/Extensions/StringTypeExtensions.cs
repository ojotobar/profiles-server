using CSharpTypes.Extensions.Guid;
using System.Globalization;
using System.Text;

namespace ProfessionalProfiles.Shared.Extensions
{
    public static class StringTypeExtensions
    {
        public static string CapitalizeText(this string title)
        {
            return new CultureInfo("en-US", false).TextInfo.ToTitleCase(title);
        }

        public static string EncodeGuidAsBase64(this Guid guid, long ticks)
        {
            var text = $"{guid.ToString().ReplaceDash(ticks.ToString())}n4{ticks}";
            var plainTextBytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static Guid DecodeBase64StringAsGuid(this string base64String)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64String);
            //Take back to original
            var originalString = Encoding.UTF8.GetString(base64EncodedBytes);

            //Extract the id
            return ExtractIdFromString(originalString);
        }

        public static bool IsValidApiKey(this string base64String, Guid id, long ticks)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64String);
            //Take back to original
            var originalString = Encoding.UTF8.GetString(base64EncodedBytes);

            //Extract the id
            var extractedTicks = ExtractTicksFromString(originalString);
            //Extract the ticks
            var extractedId = ExtractIdFromString(originalString);

            return extractedId.IsNotEmpty() && id.Equals(extractedId) && 
                extractedTicks != default && ticks == extractedTicks;
        }

        public static string GenerateOtp()
        {
            Random rnd = new Random();
            var randomNumber = (rnd.Next(100000, 999999)).ToString();
            return randomNumber;
        }

        public static string ReplaceDash(this string str, string replaceWith)
        {
            var result = string.Empty;
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == '-')
                {
                    result += replaceWith;
                }
                else
                {
                    result += str[i];
                }
            }
            return result;
        }

        private static Guid ExtractIdFromString(string str)
        {
            var parsedId = string.Empty;
            var split = str.Split("n4");
            if (split.Length < 2)
            {
                return Guid.Empty;
            }

            var idString = split[0];
            var ticksString = split[1];
            var idArray = idString.Split(ticksString);
            idString = string.Join('-', idArray);

            Guid.TryParse(idString, out var userId);
            return userId;
        }

        private static long ExtractTicksFromString(string str)
        {
            var parsedId = string.Empty;
            var split = str.Split("n4");
            if (split.Length < 2)
            {
                return default;
            }

            var ticksString = split[1];
            long.TryParse(ticksString, out long ticks);
            return ticks;
        }
    }
}
