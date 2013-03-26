using System;

namespace Komodex.Common
{
    #region Enum Parsing

    public static class Enum<T>
    {
        public static T Parse(string value, bool ignoreCase = false)
        {
            return (T)Enum.Parse(typeof(T), value, ignoreCase);
        }

        public static bool TryParse(string value, out T result)
        {
            return TryParse(value, false, out result);
        }

        public static bool TryParse(string value, bool ignoreCase, out T result)
        {
            try
            {
                result = Parse(value, ignoreCase);
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }

        public static T ParseOrDefault(string value, T defaultValue = default(T))
        {
            return ParseOrDefault(value, false, defaultValue);
        }

        public static T ParseOrDefault(string value, bool ignoreCase, T defaultValue = default(T))
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
