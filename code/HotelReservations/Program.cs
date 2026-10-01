using HotelReservations.Contracts;
using HotelReservations.Controllers;
using HotelReservations.Cqrs;
using HotelReservations.Data;
using HotelReservations.Endpoints;
using HotelReservations.Factories;
using HotelReservations.Infrastructure;
using HotelReservations.Infrastructure.Caching;
using HotelReservations.Infrastructure.Resilience;
using HotelReservations.Infrastructure.Sharding;
using HotelReservations.Managers;
using HotelReservations.Queries;
using HotelReservations.Services;

var builder = WebApplication.CreateBuilder(args);

ServiceRegistry.Register<IClock>(() => new SystemClock());

builder.Services.AddSingleton<IShardRouter, ConsistentHashShardRouter>();
builder.Services.AddSingleton<IReservationRepository, InMemoryReservationRepository>();
builder.Services.AddSingleton<ReservationCache>();
builder.Services.AddSingleton<RetryExecutor>();
builder.Services.AddSingleton<ReservationDtoFactory>();
builder.Services.AddSingleton<IReservationQueryFactory, ReservationQueryFactory>();
builder.Services.AddScoped<IQueryHandler<FindReservationQuery, ReservationDto?>, FindReservationQueryHandler>();
builder.Services.AddScoped<IMediator, Mediator>();
builder.Services.AddScoped<IReservationManager, ReservationManager>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IReservationController, ReservationController>();

var app = builder.Build();

app.MapReservationEndpoints();

app.Run();
