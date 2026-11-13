using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MMORPG.Server;

// Identity binds payload, ownership and expected revisions. Reordering dictionaries
// cannot turn a retransmitted write into a different economic operation.
public static class CommitIdentity
{
    public static string For(IReadOnlyList<CharacterRecord> batch, SharedWorld? world)
    {
        JsonNode? Clean(object value){var node=JsonSerializer.SerializeToNode(value)!;node.AsObject().Remove("LastCommit");return node;}
        var envelope=new JsonObject { ["characters"]=new JsonArray(batch.OrderBy(c=>c.Id).Select(c=>Clean(c)).ToArray()), ["world"]=world is null?null:Clean(world) };
        JsonNode? Sort(JsonNode? value)=>value switch {
            JsonObject obj=>new JsonObject(obj.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new KeyValuePair<string,JsonNode?>(p.Key,Sort(p.Value)))),
            JsonArray array=>new JsonArray(array.Select(Sort).ToArray()),
            _=>value?.DeepClone()
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Sort(envelope)!.ToJsonString())));
    }
    public static void Confirm(IReadOnlyList<CharacterRecord> batch,SharedWorld? world,string id)
    {foreach(var c in batch){c.Version++;c.LastCommit=id;}if(world is not null){world.Version++;world.LastCommit=id;}}
    public static string Document<T>(T value,string id)
    {var node=JsonSerializer.SerializeToNode(value)!;node["LastCommit"]=id;return node.ToJsonString();}
}
