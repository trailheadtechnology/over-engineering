using System.Security.Cryptography;
using System.Text;

namespace HotelReservations.Infrastructure.Sharding;

/// <summary>
/// Consistent hashing across database shards, with virtual nodes for even distribution,
/// so we can add shards as reservation volume grows without rehashing everything.
/// </summary>
public sealed class ConsistentHashShardRouter : IShardRouter
{
    private const int VirtualNodesPerShard = 100;
    private readonly SortedDictionary<uint, int> _ring = [];

    public int ShardCount => 64;

    public ConsistentHashShardRouter()
    {
        for (var shard = 0; shard < ShardCount; shard++)
        {
            for (var node = 0; node < VirtualNodesPerShard; node++)
                _ring[Hash($"shard-{shard}-vnode-{node}")] = shard;
        }
    }

    public int GetShard(string key)
    {
        var hash = Hash(key.ToUpperInvariant());
        foreach (var (point, shard) in _ring)
        {
            if (point >= hash)
                return shard;
        }
        return _ring.First().Value;
    }

    private static uint Hash(string value) =>
        BitConverter.ToUInt32(SHA256.HashData(Encoding.UTF8.GetBytes(value)), 0);
}
