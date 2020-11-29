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
    public readonly struct FormFlowInstanceId : IEquatable<FormFlowInstanceId>
    {
        public FormFlowInstanceId(string key, RouteValueDictionary routeValues)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            RouteValues = routeValues ?? throw new ArgumentNullException(nameof(routeValues));
        }

        public string Key { get; }

        public string? RandomExtension => RouteValues[Constants.RandomExtensionQueryParameterName] as string;

        public IReadOnlyDictionary<string, object> RouteValues { get; }

        public string SerializableId
        {
            get
            {
                var urlEncoder = UrlEncoder.Default;

                var url = urlEncoder.Encode(Key);

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

        public static FormFlowInstanceId Create(FlowDescriptor flowDescriptor, HttpRequest httpRequest)
        {
            if (flowDescriptor == null)
            {
                throw new ArgumentNullException(nameof(flowDescriptor));
            }

            var routeValues = GetNormalizedRouteValues(httpRequest);

            var instanceRouteValues = new RouteValueDictionary();

            foreach (var routeParam in flowDescriptor.DependentRouteDataKeys)
            {
                if (!routeValues.TryGetValue(routeParam, out var routeValue))
                {
                    throw new InvalidOperationException(
                        $"Request is missing dependent route data entry: '{routeParam}'.");
                }

                instanceRouteValues.Add(routeParam, routeValue);
            }

            if (flowDescriptor.UseRandomExtension)
            {
                var randExt = Guid.NewGuid().ToString();

                // It's important that this overwrite any existing random extension
                instanceRouteValues[Constants.RandomExtensionQueryParameterName] = randExt;
            }

            return new FormFlowInstanceId(flowDescriptor.Key, instanceRouteValues);
        }

        public static bool TryResolve(
            FlowDescriptor flowDescriptor,
            HttpRequest httpRequest,
            out FormFlowInstanceId instanceId)
        {
            if (flowDescriptor == null)
            {
                throw new ArgumentNullException(nameof(flowDescriptor));
            }

            var routeValues = GetNormalizedRouteValues(httpRequest);

            var instanceRouteValues = new RouteValueDictionary();

            foreach (var routeParam in flowDescriptor.DependentRouteDataKeys)
            {
                if (!routeValues.TryGetValue(routeParam, out var routeValue))
                {
                    instanceId = default;
                    return false;
                }

                instanceRouteValues.Add(routeParam, routeValue);
            }

            if (flowDescriptor.UseRandomExtension)
            {
                if (!routeValues.TryGetValue(Constants.RandomExtensionQueryParameterName, out var randomExt) ||
                    randomExt == null)
                {
                    instanceId = default;
                    return false;
                }

                instanceRouteValues.Add(Constants.RandomExtensionQueryParameterName, randomExt);
            }

            instanceId = new FormFlowInstanceId(flowDescriptor.Key, instanceRouteValues);
            return true;
        }

        public bool Equals([AllowNull] FormFlowInstanceId other) =>
            Key == other.Key && RouteValues.SequenceEqual(other.RouteValues);

        public override bool Equals(object? obj) => obj is FormFlowInstanceId x && x.Equals(this);

        public override int GetHashCode() => HashCode.Combine(Key, RouteValues);

        public override string ToString() => SerializableId;

        public static bool operator ==(FormFlowInstanceId left, FormFlowInstanceId right) => left.Equals(right);

        public static bool operator !=(FormFlowInstanceId left, FormFlowInstanceId right) => !(left == right);

        public static implicit operator string(FormFlowInstanceId instanceId) => instanceId.ToString();

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
