using Infrastructure.Abstracts;
using System.Text.Json;
namespace Infrastructure.Strategy
{
    public static class RedisSaveStrategyFactory
    {
        public static RedisSaveStrategy GetStrategy(object value)
        {
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Number => new IntSaveStrategy(),   // ou DoubleSaveStrategy
                    JsonValueKind.String => new StringSaveStrategy(),
                    JsonValueKind.True or JsonValueKind.False => new BoolSaveStrategy(),
                    JsonValueKind.Array => new ListSaveStrategy(),
                    JsonValueKind.Object => new JsonSaveStrategy(),
                    _ => new JsonSaveStrategy()
                };
            }

            // fallback se não for JsonElement
            return new JsonSaveStrategy();

        }
    }

}
