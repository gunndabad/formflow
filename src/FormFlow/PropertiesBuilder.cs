using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FormFlow
{
    public class PropertiesBuilder
    {
        private readonly Dictionary<object, object> _values;

        public PropertiesBuilder()
        {
            _values = new Dictionary<object, object>();
        }

        public PropertiesBuilder Add(object key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            _values.Add(key, value);
            return this;
        }

        public IReadOnlyDictionary<object, object> Build() =>
            new ReadOnlyDictionary<object, object>(_values);

        public static IReadOnlyDictionary<object, object> CreateEmpty() =>
            new PropertiesBuilder().Build();
    }
}
