using System;

namespace FormFlow
{
    /// <summary>
    /// Marker attribute for annotating FormFlow journey state types.
    /// </summary>
    /// <remarks>
    /// Used by <see cref="ServiceCollectionExtensions.AddJourneyStateTypes(Microsoft.Extensions.DependencyInjection.IServiceCollection, System.Reflection.Assembly)"/>
    /// for identifying the state types to register strongly-typed instances for in the DI container.
    /// This enables resolving <see cref="JourneyInstance{TState}"/> when <c>TState</c> has been decorated with this attribute.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class JourneyStateAttribute : Attribute
    {
    }
}
