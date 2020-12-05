using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace FormFlow
{
    [DebuggerDisplay("{SerializableId}")]
    public readonly struct JourneyInstanceId : IEquatable<JourneyInstanceId>
    {
        public JourneyInstanceId(string journeyName, RouteValueDictionary routeValues)
        {
            JourneyName = journeyName ?? throw new ArgumentNullException(nameof(journeyName));
            RouteValues = routeValues ?? throw new ArgumentNullException(nameof(routeValues));
        }

        public string JourneyName { get; }

        public string? RandomExtension => RouteValues[Constants.RandomExtensionQueryParameterName] as string;

        public IReadOnlyDictionary<string, object> RouteValues { get; }

        public string SerializableId
        {
            get
            {
                var urlEncoder = UrlEncoder.Default;

                var url = urlEncoder.Encode(JourneyName);

                foreach (var kvp in RouteValues)
                {
                    var routeValue = kvp.Value;

                    if (!(routeValue is string) && routeValue is IEnumerable enumerable)
                    {
                        foreach (var v in enumerable)
                        {
                            if (v != null)
                            {
                                url = QueryHelpers.AddQueryString(url, kvp.Key, v.ToString());
                            }
                        }
                    }
                    else
                    {
                        url = QueryHelpers.AddQueryString(url, kvp.Key, routeValue.ToString());
                    }
                }

                return url;
            }
        }

        public static JourneyInstanceId Create(JourneyDescriptor journeyDescriptor, HttpRequest httpRequest)
        {
            if (journeyDescriptor == null)
            {
                throw new ArgumentNullException(nameof(journeyDescriptor));
            }

            var routeValues = GetNormalizedRouteValues(httpRequest);

            var instanceRouteValues = new RouteValueDictionary();

            foreach (var routeParam in journeyDescriptor.DependentRouteDataKeys)
            {
                if (!routeValues.TryGetValue(routeParam, out var routeValue))
                {
                    throw new InvalidOperationException(
                        $"Request is missing dependent route data entry: '{routeParam}'.");
                }

                instanceRouteValues.Add(routeParam, routeValue);
            }

            if (journeyDescriptor.UseRandomExtension)
            {
                var randExt = Guid.NewGuid().ToString();

                // It's important that this overwrite any existing random extension
                instanceRouteValues[Constants.RandomExtensionQueryParameterName] = randExt;
            }

            return new JourneyInstanceId(journeyDescriptor.JourneyName, instanceRouteValues);
        }

        public static bool TryResolve(
            JourneyDescriptor journeyDescriptor,
            HttpRequest httpRequest,
            out JourneyInstanceId instanceId)
        {
            if (journeyDescriptor == null)
            {
                throw new ArgumentNullException(nameof(journeyDescriptor));
            }

            var routeValues = GetNormalizedRouteValues(httpRequest);

            var instanceRouteValues = new RouteValueDictionary();

            foreach (var routeParam in journeyDescriptor.DependentRouteDataKeys)
            {
                if (!routeValues.TryGetValue(routeParam, out var routeValue))
                {
                    instanceId = default;
                    return false;
                }

                instanceRouteValues.Add(routeParam, routeValue);
            }

            if (journeyDescriptor.UseRandomExtension)
            {
                if (!routeValues.TryGetValue(Constants.RandomExtensionQueryParameterName, out var randomExt) ||
                    randomExt == null)
                {
                    instanceId = default;
                    return false;
                }

                instanceRouteValues.Add(Constants.RandomExtensionQueryParameterName, randomExt);
            }

            instanceId = new JourneyInstanceId(journeyDescriptor.JourneyName, instanceRouteValues);
            return true;
        }

        public bool Equals([AllowNull] JourneyInstanceId other) =>
            JourneyName == other.JourneyName && RouteValues.SequenceEqual(other.RouteValues);

        public override bool Equals(object? obj) => obj is JourneyInstanceId x && x.Equals(this);

        public override int GetHashCode() => HashCode.Combine(JourneyName, RouteValues);

        public override string ToString() => SerializableId;

        public static bool operator ==(JourneyInstanceId left, JourneyInstanceId right) => left.Equals(right);

        public static bool operator !=(JourneyInstanceId left, JourneyInstanceId right) => !(left == right);

        public static implicit operator string(JourneyInstanceId instanceId) => instanceId.ToString();

        private static RouteValueDictionary GetNormalizedRouteValues(HttpRequest request) =>
            new RouteValueDictionary(
                request.HttpContext.GetRouteData().Values
                    .Concat(request.Query.ToDictionary(
                        q => q.Key,
                        q =>
                        {
                            var stringValues = q.Value;
                            return stringValues.Count > 1 ? (object)stringValues.ToArray() : stringValues[0];
                        })));
    }
}
