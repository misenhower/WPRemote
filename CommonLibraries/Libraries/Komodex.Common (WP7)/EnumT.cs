using System;

namespace Komodex.Common
{
    #region Enum Parsing

    public static class Enum<T>
    {
        public static T Parse(string value)
        {
            return Enum<T>.Parse(value, true);
        }

        public static T Parse(string value, bool ignoreCase)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        public static bool TryParse(string value, bool ignoreCase, out T result)
        {
            try
            {
                result = (T)Enum.Parse(typeof(T), value, ignoreCase);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        public static T ParseOrDefault(string value, T defaultValue)
        {
            return ParseOrDefault(value, defaultValue, true);
        }

        public static T ParseOrDefault(string value, T defaultValue, bool ignoreCase)
        {
            T result;

            if (TryParse(value, ignoreCase, out result))
                return result;
            else
                return defaultValue;
        }
    }

    #endregion
}
