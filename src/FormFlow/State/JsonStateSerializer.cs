using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FormFlow.State;

public class JsonStateSerializer : IStateSerializer
{
    private static readonly Encoding _encoding = Encoding.UTF8;

    private readonly IOptions<JsonOptions> _jsonOptionsAccessor;

    public JsonStateSerializer(IOptions<JsonOptions> jsonOptionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(jsonOptionsAccessor);
        _jsonOptionsAccessor = jsonOptionsAccessor;
    }

    public object Deserialize(Type type, byte[] bytes) =>
        JsonSerializer.Deserialize(_encoding.GetString(bytes), type, _jsonOptionsAccessor.Value.JsonSerializerOptions) ??
            throw new InvalidOperationException("Data is empty.");

    public byte[] Serialize(Type type, object state) =>
        _encoding.GetBytes(JsonSerializer.Serialize(state, type, _jsonOptionsAccessor.Value.JsonSerializerOptions));
}
