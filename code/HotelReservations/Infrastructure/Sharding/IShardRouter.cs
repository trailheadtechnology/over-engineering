namespace HotelReservations.Infrastructure.Sharding;

public interface IShardRouter
{
    int ShardCount { get; }

    int GetShard(string key);
}
