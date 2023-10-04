using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FormFlow.State;

public class JsonStateSerializer : IStateSerializer
{
    private readonly IOptions<JsonOptions> _jsonOptionsAccessor;

    public JsonStateSerializer(IOptions<JsonOptions> jsonOptionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(jsonOptionsAccessor);
        _jsonOptionsAccessor = jsonOptionsAccessor;
    }

    public object Deserialize(Type type, string serialized) =>
        JsonSerializer.Deserialize(serialized, type, _jsonOptionsAccessor.Value.JsonSerializerOptions) ??
            throw new InvalidOperationException("Data is empty.");

    public string Serialize(Type type, object state) =>
        JsonSerializer.Serialize(state, type, _jsonOptionsAccessor.Value.JsonSerializerOptions);
}
