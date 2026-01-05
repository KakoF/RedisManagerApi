using System.Text.Json;

namespace Domain.Utils
{
    public static class JsonElementConverter
    {
        public static T ConvertTo<T>(object value)
        {
            if (value is JsonElement element)
            {
                return typeof(T) switch
                {
                    var t when t == typeof(int) => (T)(object)element.GetInt32(),
                    var t when t == typeof(double) => (T)(object)element.GetDouble(),
                    var t when t == typeof(bool) => (T)(object)element.GetBoolean(),
                    var t when t == typeof(string) => (T)(object)element.GetString()!,
                    _ => throw new InvalidCastException($"Type {typeof(T)} not suported")
                };
            }
            return (T)System.Convert.ChangeType(value, typeof(T));
        }
    }

}
