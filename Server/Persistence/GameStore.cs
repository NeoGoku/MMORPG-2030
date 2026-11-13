using System.Text.Json;
using Npgsql;

namespace MMORPG.Server;

public sealed record AccountRecord(string Id,string Username,byte[] Salt,byte[] PasswordHash);
public interface IGameStore
{
    bool CommitExists(string account,string id)=>false;
    SharedWorld LoadWorld()=>new();
    void SaveWorld(SharedWorld world,IReadOnlyList<CharacterRecord> characters)=>throw new GameRuleException("Shared storage unavailable.");
    bool Durable {get;}
    AccountRecord? Account(string username);
    void Register(AccountRecord account);
    List<CharacterRecord> List(string account);
    CharacterRecord? Get(string id);
    void Create(CharacterRecord character);
    void Save(CharacterRecord character);
    void SaveBatch(IReadOnlyList<CharacterRecord> characters);
}
public sealed class MemoryGameStore : IGameStore
{
    private readonly object gate=new();private readonly Dictionary<string,AccountRecord> accounts=[];private readonly Dictionary<string,CharacterRecord> characters=[];
    private SharedWorld shared=new();
    private readonly Dictionary<string,HashSet<string>> receipts=[];
    public bool CommitExists(string account,string id){lock(gate)return receipts.TryGetValue(id,out var owners)&&owners.Contains(account);}
    public SharedWorld LoadWorld(){lock(gate)return SharedWorld.Copy(shared);}
    public void SaveWorld(SharedWorld world,IReadOnlyList<CharacterRecord> batch)=>SaveTransaction(batch,world);
    public bool Durable=>false;
    public AccountRecord? Account(string username){lock(gate)return accounts.GetValueOrDefault(username);}
    public void Register(AccountRecord a){lock(gate){if(!accounts.TryAdd(a.Username,a))throw new GameRuleException("Account bereits vorhanden.");}}
    public List<CharacterRecord> List(string account){lock(gate)return characters.Values.Where(c=>c.AccountId==account).Select(CharacterRules.Clone).OrderBy(c=>c.Slot).ToList();}
    public CharacterRecord? Get(string id){lock(gate)return characters.TryGetValue(id,out var c)?CharacterRules.Clone(c):null;}
    public void Create(CharacterRecord c){lock(gate){if(characters.Values.Any(x=>x.AccountId==c.AccountId&&x.Slot==c.Slot))throw new GameRuleException("Charakterplatz belegt.");characters.Add(c.Id,CharacterRules.Clone(c));}}
    public void Save(CharacterRecord c)=>SaveBatch([c]);
    public void SaveBatch(IReadOnlyList<CharacterRecord> batch)=>SaveTransaction(batch,null);
    private void SaveTransaction(IReadOnlyList<CharacterRecord> batch,SharedWorld? world){lock(gate){
        if(batch.Count>10||batch.Select(c=>c.Id).Distinct().Count()!=batch.Count)throw new GameRuleException("Ungültige Transaktion.");
        var id=CommitIdentity.For(batch,world);
        if(receipts.ContainsKey(id)){
            if(batch.Any(c=>!characters.TryGetValue(c.Id,out var old)||old.Version!=c.Version+1||old.LastCommit!=id)||world is not null&&(shared.Version!=world.Version+1||shared.LastCommit!=id))throw new GameRuleException("Bestätigter Commit wurde bereits durch einen neueren Stand ersetzt.");
            CommitIdentity.Confirm(batch,world,id);return;
        }
        if(world is not null&&world.Version!=shared.Version)throw new GameRuleException("Weltversion veraltet.");
        foreach(var c in batch)if(c.State.Coins<0||!characters.TryGetValue(c.Id,out var old)||old.AccountId!=c.AccountId||old.Version!=c.Version)throw new GameRuleException("Speicherversion veraltet.");
        CommitIdentity.Confirm(batch,world,id);
        foreach(var c in batch)characters[c.Id]=CharacterRules.Clone(c);
        if(world is not null)shared=SharedWorld.Copy(world);
        receipts[id]=batch.Select(c=>c.AccountId).ToHashSet();
    }}

}
public sealed class PostgresGameStore : IGameStore,IDisposable
{
    private readonly NpgsqlDataSource source;
    public bool Durable=>true;
    public PostgresGameStore(string connection)
    {
        var cs=new NpgsqlConnectionStringBuilder(connection){Timeout=5,CommandTimeout=5};source=NpgsqlDataSource.Create(cs.ConnectionString);
        using var db=source.OpenConnection();using var tx=db.BeginTransaction();
        using(var cmd=new NpgsqlCommand("SELECT pg_advisory_xact_lock(2030003); CREATE TABLE IF NOT EXISTS schema_migrations(version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());",db,tx))cmd.ExecuteNonQuery();
        foreach(var path in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory,"Migrations"),"*.sql").Order()){
            var version=int.Parse(Path.GetFileName(path).Split('_')[0]);using var exists=new NpgsqlCommand("SELECT count(*) FROM schema_migrations WHERE version=$1",db,tx);exists.Parameters.AddWithValue(version);if((long)exists.ExecuteScalar()!>0)continue;
            using var migrate=new NpgsqlCommand(File.ReadAllText(path),db,tx);migrate.ExecuteNonQuery();using var record=new NpgsqlCommand("INSERT INTO schema_migrations(version) VALUES($1)",db,tx);record.Parameters.AddWithValue(version);record.ExecuteNonQuery();
        }tx.Commit();
    }
    public AccountRecord? Account(string username){using var db=source.OpenConnection();using var cmd=new NpgsqlCommand("SELECT id,username,salt,password_hash FROM accounts WHERE username=$1",db);cmd.Parameters.AddWithValue(username);using var r=cmd.ExecuteReader();return r.Read()?new(r.GetString(0),r.GetString(1),r.GetFieldValue<byte[]>(2),r.GetFieldValue<byte[]>(3)):null;}
    public void Register(AccountRecord a){using var db=source.OpenConnection();using var cmd=new NpgsqlCommand("INSERT INTO accounts(id,username,salt,password_hash) VALUES($1,$2,$3,$4)",db);cmd.Parameters.AddWithValue(a.Id);cmd.Parameters.AddWithValue(a.Username);cmd.Parameters.AddWithValue(a.Salt);cmd.Parameters.AddWithValue(a.PasswordHash);try{cmd.ExecuteNonQuery();}catch(PostgresException e)when(e.SqlState=="23505"){throw new GameRuleException("Account bereits vorhanden.");}}
    public List<CharacterRecord> List(string account){using var db=source.OpenConnection();using var cmd=new NpgsqlCommand("SELECT document::text,version FROM characters WHERE account_id=$1 ORDER BY slot",db);cmd.Parameters.AddWithValue(account);using var r=cmd.ExecuteReader();var list=new List<CharacterRecord>();while(r.Read()){var c=JsonSerializer.Deserialize<CharacterRecord>(r.GetString(0))!;c.Version=r.GetInt64(1);list.Add(c);}return list;}
    public CharacterRecord? Get(string id){using var db=source.OpenConnection();using var cmd=new NpgsqlCommand("SELECT document::text,version FROM characters WHERE id=$1",db);cmd.Parameters.AddWithValue(id);using var r=cmd.ExecuteReader();if(!r.Read())return null;var c=JsonSerializer.Deserialize<CharacterRecord>(r.GetString(0))!;c.Version=r.GetInt64(1);return c;}
    public void Create(CharacterRecord c){using var db=source.OpenConnection();using var cmd=new NpgsqlCommand("INSERT INTO characters(id,account_id,slot,document,level,coins) VALUES($1,$2,$3,$4::jsonb,$5,$6)",db);cmd.Parameters.AddWithValue(c.Id);cmd.Parameters.AddWithValue(c.AccountId);cmd.Parameters.AddWithValue(c.Slot);cmd.Parameters.AddWithValue(JsonSerializer.Serialize(c));cmd.Parameters.AddWithValue(c.State.Level);cmd.Parameters.AddWithValue(c.State.Coins);try{cmd.ExecuteNonQuery();}catch(PostgresException e)when(e.SqlState=="23505"){throw new GameRuleException("Charakterplatz belegt.");}}
    public void Save(CharacterRecord c)=>SaveBatch([c]);
    public SharedWorld LoadWorld(){using var db=source.OpenConnection();using var cmd=new NpgsqlCommand("SELECT document::text,version FROM shared_world WHERE id=1",db);using var r=cmd.ExecuteReader();if(!r.Read())throw new GameRuleException("Welt fehlt.");var w=JsonSerializer.Deserialize<SharedWorld>(r.GetString(0))!;w.Version=r.GetInt64(1);return w;}
    public void SaveWorld(SharedWorld world,IReadOnlyList<CharacterRecord> batch)=>SaveTransaction(batch,world);
    public void SaveBatch(IReadOnlyList<CharacterRecord> batch)=>SaveTransaction(batch,null);
    public bool CommitExists(string account,string id){using var db=source.OpenConnection();using var cmd=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM commit_receipts WHERE operation_id=$1 AND $2=ANY(account_ids))",db);cmd.Parameters.AddWithValue(id);cmd.Parameters.AddWithValue(account);return (bool)cmd.ExecuteScalar()!;}
    private bool ResolveReceipt(string id,IReadOnlyList<CharacterRecord> batch,SharedWorld? world)
    {using var db=source.OpenConnection();return ResolveReceipt(db,null,id,batch,world);}
    private static bool ResolveReceipt(NpgsqlConnection db,NpgsqlTransaction? tx,string id,IReadOnlyList<CharacterRecord> batch,SharedWorld? world)
    {
        using var exists=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM commit_receipts WHERE operation_id=$1)",db,tx);exists.Parameters.AddWithValue(id);if(!(bool)exists.ExecuteScalar()!)return false;
        foreach(var c in batch){using var cmd=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM characters WHERE id=$1 AND account_id=$2 AND version=$3 AND document->>'LastCommit'=$4)",db,tx);cmd.Parameters.AddWithValue(c.Id);cmd.Parameters.AddWithValue(c.AccountId);cmd.Parameters.AddWithValue(c.Version+1);cmd.Parameters.AddWithValue(id);if(!(bool)cmd.ExecuteScalar()!)throw new GameRuleException("Bestätigter Commit wurde bereits durch einen neueren Stand ersetzt.");}
        if(world is not null){using var cmd=new NpgsqlCommand("SELECT EXISTS(SELECT 1 FROM shared_world WHERE id=1 AND version=$1 AND document->>'LastCommit'=$2)",db,tx);cmd.Parameters.AddWithValue(world.Version+1);cmd.Parameters.AddWithValue(id);if(!(bool)cmd.ExecuteScalar()!)throw new GameRuleException("Bestätigte Welttransaktion wurde bereits durch einen neueren Stand ersetzt.");}
        return true;
    }
    private void SaveTransaction(IReadOnlyList<CharacterRecord> batch,SharedWorld? shared)
    {
        if(batch.Count>10||batch.Select(c=>c.Id).Distinct().Count()!=batch.Count)throw new GameRuleException("Ungültige Transaktion.");
        if(batch.Any(c=>c.State.Coins<0))throw new GameRuleException("Ungültige Währung.");
        var operation=CommitIdentity.For(batch,shared);
        using var db=source.OpenConnection();using var tx=db.BeginTransaction();
        using(var receipt=new NpgsqlCommand("INSERT INTO commit_receipts(operation_id,account_ids) VALUES($1,$2) ON CONFLICT DO NOTHING RETURNING operation_id",db,tx)){
            receipt.Parameters.AddWithValue(operation);receipt.Parameters.AddWithValue(batch.Select(c=>c.AccountId).Distinct().ToArray());
            if(receipt.ExecuteScalar() is null){if(!ResolveReceipt(db,tx,operation,batch,shared))throw new GameRuleException("Commit nicht auflösbar.");CommitIdentity.Confirm(batch,shared,operation);return;}
        }
        if(shared is not null){
            using var worldCmd=new NpgsqlCommand("UPDATE shared_world SET document=$1::jsonb,version=version+1,updated_at=now() WHERE id=1 AND version=$2",db,tx);
            worldCmd.Parameters.AddWithValue(CommitIdentity.Document(shared,operation));worldCmd.Parameters.AddWithValue(shared.Version);
            if(worldCmd.ExecuteNonQuery()!=1)throw new GameRuleException("Weltversion veraltet.");
        }
        foreach(var c in batch.OrderBy(c=>c.Id)){
            using var cmd=new NpgsqlCommand("UPDATE characters SET document=$1::jsonb,level=$2,coins=$3,version=version+1,updated_at=now() WHERE id=$4 AND account_id=$5 AND version=$6",db,tx);
            cmd.Parameters.AddWithValue(CommitIdentity.Document(c,operation));cmd.Parameters.AddWithValue(c.State.Level);cmd.Parameters.AddWithValue(c.State.Coins);cmd.Parameters.AddWithValue(c.Id);cmd.Parameters.AddWithValue(c.AccountId);cmd.Parameters.AddWithValue(c.Version);
            if(cmd.ExecuteNonQuery()!=1)throw new GameRuleException("Speicherversion veraltet.");
        }
        try{tx.Commit();}
        catch(NpgsqlException){
            // A lost response is not proof of rollback. Resolve only the exact payload;
            // never rerun a purchase, transfer or generated-identity action here.
            if(!ResolveReceipt(operation,batch,shared))throw;
        }
        CommitIdentity.Confirm(batch,shared,operation);
    }
    public void Dispose()=>source.Dispose();
}
