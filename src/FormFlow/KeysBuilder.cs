using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Primitives;

namespace FormFlow;

public class KeysBuilder
{
    private readonly Dictionary<string, StringValues> _values;

    public KeysBuilder()
    {
        _values = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
    }

    public KeysBuilder With(string key, object value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        _values[key] = value switch
        {
            StringValues sv => sv,
            string str => str,
            _ => value.ToString()
        };

        return this;
    }

    public IReadOnlyDictionary<string, StringValues> Build() =>
        new ReadOnlyDictionary<string, StringValues>(_values);

    public static IReadOnlyDictionary<string, StringValues> CreateEmpty() =>
        new KeysBuilder().Build();
}
