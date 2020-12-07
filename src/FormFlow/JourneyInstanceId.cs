using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace FormFlow
{
    [DebuggerDisplay("{SerializableId}")]
    public readonly struct JourneyInstanceId : IEquatable<JourneyInstanceId>
    {
        public JourneyInstanceId(string journeyName, IReadOnlyDictionary<string, StringValues> keys)
        {
            JourneyName = journeyName ?? throw new ArgumentNullException(nameof(journeyName));
            Keys = keys ?? throw new ArgumentNullException(nameof(keys));
        }

        public string JourneyName { get; }

        public IReadOnlyDictionary<string, StringValues> Keys { get; }

        public string? UniqueKey => Keys[Constants.UniqueKeyQueryParameterName];

        public string SerializableId
        {
            get
            {
                var urlEncoder = UrlEncoder.Default;

                var url = urlEncoder.Encode(JourneyName);

                foreach (var kvp in Keys)
                {
                    var value = kvp.Value;

                    foreach (var sv in value)
                    {
                        url = QueryHelpers.AddQueryString(url, kvp.Key, sv);
                    }
                }

                return url;
            }
        }

        public static JourneyInstanceId Create(JourneyDescriptor journeyDescriptor, IValueProvider valueProvider)
        {
            if (journeyDescriptor == null)
            {
                throw new ArgumentNullException(nameof(journeyDescriptor));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            var instanceKeys = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in journeyDescriptor.RequestDataKeys)
            {
                var keyIsOptional = IsKeyOptional(key, out var normalizedKey);

                var keyValueProviderResult = valueProvider.GetValue(normalizedKey);

                if (keyValueProviderResult.Length == 0)
                {
                    if (!keyIsOptional)
                    {
                        throw new InvalidOperationException($"Cannot resolve '{key}' from request.");
                    }
                    else
                    {
                        continue;
                    }
                }

                instanceKeys.Add(normalizedKey, keyValueProviderResult.Values);
            }

            if (journeyDescriptor.AppendUniqueKey)
            {
                var uniqueKey = Guid.NewGuid().ToString();

                // It's important that this overwrite any existing random extension
                instanceKeys[Constants.UniqueKeyQueryParameterName] = uniqueKey;
            }

            return new JourneyInstanceId(journeyDescriptor.JourneyName, instanceKeys);
        }

        public static bool TryResolve(
            JourneyDescriptor journeyDescriptor,
            IValueProvider valueProvider,
            out JourneyInstanceId instanceId)
        {
            if (journeyDescriptor == null)
            {
                throw new ArgumentNullException(nameof(journeyDescriptor));
            }

            if (valueProvider == null)
            {
                throw new ArgumentNullException(nameof(valueProvider));
            }

            var instanceKeys = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

            foreach (var key in journeyDescriptor.RequestDataKeys)
            {
                var keyIsOptional = IsKeyOptional(key, out var normalizedKey);

                var keyValueProviderResult = valueProvider.GetValue(normalizedKey);

                if (keyValueProviderResult.Length == 0)
                {
                    if (!keyIsOptional)
                    {
                        instanceId = default;
                        return false;
                    }
                    else
                    {
                        continue;
                    }
                }

                instanceKeys.Add(normalizedKey, keyValueProviderResult.Values);
            }

            if (journeyDescriptor.AppendUniqueKey)
            {
                var uniqueKeyValueProviderResult = valueProvider.GetValue(Constants.UniqueKeyQueryParameterName);

                if (uniqueKeyValueProviderResult.Length == 0)
                {
                    instanceId = default;
                    return false;
                }

                instanceKeys.Add(
                    Constants.UniqueKeyQueryParameterName,
                    uniqueKeyValueProviderResult.FirstValue);
            }

            instanceId = new JourneyInstanceId(journeyDescriptor.JourneyName, instanceKeys);
            return true;
        }

        public bool Equals([AllowNull] JourneyInstanceId other) =>
            JourneyName == other.JourneyName && Keys.SequenceEqual(other.Keys);

        public override bool Equals(object? obj) => obj is JourneyInstanceId x && x.Equals(this);

        public override int GetHashCode() => HashCode.Combine(JourneyName, Keys);

        public override string ToString() => SerializableId;

        public static bool operator ==(JourneyInstanceId left, JourneyInstanceId right) => left.Equals(right);

        public static bool operator !=(JourneyInstanceId left, JourneyInstanceId right) => !(left == right);

        public static implicit operator string(JourneyInstanceId instanceId) => instanceId.ToString();

        private static bool IsKeyOptional(string key, out string normalizedKey)
        {
            if (key.EndsWith("?"))
            {
                normalizedKey = key[0..^1];
                return true;
            }
            else
            {
                normalizedKey = key;
                return false;
            }
        }
    }
}
