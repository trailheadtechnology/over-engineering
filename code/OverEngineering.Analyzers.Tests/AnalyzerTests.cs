using static OverEngineering.Analyzers.Tests.AnalyzerRunner;

namespace OverEngineering.Analyzers.Tests;

public class SingleTypeArgumentTests
{
    [Fact]
    public async Task Flags_generic_base_class_always_closed_over_the_same_type()
    {
        var ids = await IdsAsync(new SingleTypeArgumentAnalyzer(), """
            using System;
            abstract class Entity<TId> { public TId Id { get; init; } = default!; }
            class Guest : Entity<Guid> { }
            class Room : Entity<Guid> { }
            """);
        Assert.Equal(["OE0002"], ids);
    }

    [Fact]
    public async Task Ignores_generic_type_used_with_different_arguments()
    {
        var ids = await IdsAsync(new SingleTypeArgumentAnalyzer(), """
            using System;
            abstract class Entity<TId> { public TId Id { get; init; } = default!; }
            class Guest : Entity<Guid> { }
            class Room : Entity<int> { }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Ignores_uses_inside_other_generic_code()
    {
        var ids = await IdsAsync(new SingleTypeArgumentAnalyzer(), """
            interface IRepository<T> { }
            class Repository<T> : IRepository<T> { }
            class Guest { }
            class Room { }
            class Uses { IRepository<Guest>? g; IRepository<Room>? r; Repository<Guest>? a; Repository<Room>? b; }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Unbound_typeof_does_not_count_as_a_second_use()
    {
        var ids = await IdsAsync(new SingleTypeArgumentAnalyzer(), """
            interface IHandler<T> { }
            class Handler : IHandler<int> { }
            class Mediator { System.Type T = typeof(IHandler<>); }
            """);
        Assert.Equal(["OE0002"], ids);
    }

    [Fact]
    public async Task Ignores_framework_generics()
    {
        var ids = await IdsAsync(new SingleTypeArgumentAnalyzer(), """
            using System.Collections.Generic;
            class C { List<int> Items = new List<int>(); }
            """);
        Assert.Empty(ids);
    }
}

public class SingleImplementationInterfaceTests
{
    [Fact]
    public async Task Flags_interface_with_one_implementation()
    {
        var ids = await IdsAsync(new SingleImplementationInterfaceAnalyzer(), """
            interface IReservationService { void Find(); }
            class ReservationService : IReservationService { public void Find() { } }
            """);
        Assert.Equal(["OE0003"], ids);
    }

    [Fact]
    public async Task Ignores_interface_with_two_implementations()
    {
        var ids = await IdsAsync(new SingleImplementationInterfaceAnalyzer(), """
            interface IPaymentGateway { void Charge(); }
            class CardGateway : IPaymentGateway { public void Charge() { } }
            class InvoiceGateway : IPaymentGateway { public void Charge() { } }
            """);
        Assert.Empty(ids);
    }
}

public class TrivialFactoryTests
{
    [Fact]
    public async Task Flags_factory_that_only_calls_new()
    {
        var ids = await IdsAsync(new TrivialFactoryAnalyzer(), """
            record Query(string Code, int Mode);
            class QueryFactory { public Query Create(string code, int mode) => new(code, mode); }
            """);
        Assert.Equal(["OE0006"], ids);
    }

    [Fact]
    public async Task Flags_block_body_with_explicit_type()
    {
        var ids = await IdsAsync(new TrivialFactoryAnalyzer(), """
            class Widget { }
            class WidgetFactory { public Widget Create() { return new Widget(); } }
            """);
        Assert.Equal(["OE0006"], ids);
    }

    [Fact]
    public async Task Ignores_factory_that_maps_or_decides()
    {
        var ids = await IdsAsync(new TrivialFactoryAnalyzer(), """
            record Dto(string Name);
            class Guest { public string First = ""; public string Last = ""; }
            class DtoFactory { public Dto Create(Guest g) => new($"{g.First} {g.Last}"); }
            """);
        Assert.Empty(ids);
    }

    [Fact]
    public async Task Ignores_classes_not_named_factory()
    {
        var ids = await IdsAsync(new TrivialFactoryAnalyzer(), """
            record Query(string Code);
            class Queries { public Query Create(string code) => new(code); }
            """);
        Assert.Empty(ids);
    }
}

public class PassThroughMethodTests
{
    [Fact]
    public async Task Flags_method_that_only_forwards()
    {
        var ids = await IdsAsync(new PassThroughMethodAnalyzer(), """
            using System.Threading.Tasks;
            class Repo { public Task<string> GetAsync(int id) => Task.FromResult(""); }
            class Service
            {
                private readonly Repo _repo = new();
                public Task<string> GetAsync(int id) => _repo.GetAsync(id);
            }
            """);
        Assert.Equal(["OE0007"], ids);
    }

    [Fact]
    public async Task Ignores_method_that_adds_behavior()
    {
        var ids = await IdsAsync(new PassThroughMethodAnalyzer(), """
            using System;
            using System.Threading.Tasks;
            class Repo { public Task SaveAsync(string name) => Task.CompletedTask; }
            class Service
            {
                private readonly Repo _repo = new();
                public Task SaveAsync(string name)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(name);
                    return _repo.SaveAsync(name);
                }
            }
            """);
        Assert.Empty(ids);
    }
}

public class HandRolledRetryTests
{
    [Fact]
    public async Task Flags_loop_that_catches_and_delays()
    {
        var ids = await IdsAsync(new HandRolledRetryAnalyzer(), """
            using System;
            using System.Threading.Tasks;
            class C
            {
                async Task<int> M(Func<Task<int>> action)
                {
                    for (var attempt = 1; attempt <= 3; attempt++)
                    {
                        try { return await action(); }
                        catch (Exception) when (attempt < 3) { await Task.Delay(100); }
                    }
                    throw new InvalidOperationException();
                }
            }
            """);
        Assert.Equal(["OE0009"], ids);
    }

    [Fact]
    public async Task Ignores_loop_that_catches_without_waiting()
    {
        var ids = await IdsAsync(new HandRolledRetryAnalyzer(), """
            using System;
            class C
            {
                int M(string[] values)
                {
                    var total = 0;
                    foreach (var v in values)
                    {
                        try { total += int.Parse(v); }
                        catch (FormatException) { }
                    }
                    return total;
                }
            }
            """);
        Assert.Empty(ids);
    }
}
