using System.Collections.Concurrent;

namespace HotelReservations.Infrastructure;

/// <summary>
/// Lightweight service locator so any class can get its dependencies without constructor plumbing.
/// </summary>
public static class ServiceRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<object>> Factories = new();

    public static void Register<T>(Func<T> factory) where T : class => Factories[typeof(T)] = factory;

    public static T Resolve<T>() where T : class => (T)Factories[typeof(T)]();
}
