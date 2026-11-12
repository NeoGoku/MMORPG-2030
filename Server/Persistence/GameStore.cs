using System.Text.Json;
using Npgsql;

namespace MMORPG.Server;

public sealed record AccountRecord(string Id,string Username,byte[] Salt,byte[] PasswordHash);
public interface IGameStore
{
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
    public bool Durable=>false;
    public AccountRecord? Account(string username){lock(gate)return accounts.GetValueOrDefault(username);}
    public void Register(AccountRecord a){lock(gate){if(!accounts.TryAdd(a.Username,a))throw new GameRuleException("Account bereits vorhanden.");}}
    public List<CharacterRecord> List(string account){lock(gate)return characters.Values.Where(c=>c.AccountId==account).Select(CharacterRules.Clone).OrderBy(c=>c.Slot).ToList();}
    public CharacterRecord? Get(string id){lock(gate)return characters.TryGetValue(id,out var c)?CharacterRules.Clone(c):null;}
    public void Create(CharacterRecord c){lock(gate){if(characters.Values.Any(x=>x.AccountId==c.AccountId&&x.Slot==c.Slot))throw new GameRuleException("Charakterplatz belegt.");characters.Add(c.Id,CharacterRules.Clone(c));}}
    public void Save(CharacterRecord c)=>SaveBatch([c]);
    public void SaveBatch(IReadOnlyList<CharacterRecord> batch){lock(gate){
        if(batch.Count>10||batch.Select(c=>c.Id).Distinct().Count()!=batch.Count)throw new GameRuleException("Ungültige Transaktion.");
        foreach(var c in batch)if(c.State.Coins<0||!characters.TryGetValue(c.Id,out var old)||old.AccountId!=c.AccountId||old.Version!=c.Version)throw new GameRuleException("Speicherversion veraltet.");
        foreach(var c in batch){c.Version++;characters[c.Id]=CharacterRules.Clone(c);}
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
    public void SaveBatch(IReadOnlyList<CharacterRecord> batch)
    {
        if(batch.Count>10||batch.Select(c=>c.Id).Distinct().Count()!=batch.Count)throw new GameRuleException("Ungültige Transaktion.");
        using var db=source.OpenConnection();using var tx=db.BeginTransaction();
        foreach(var c in batch.OrderBy(c=>c.Id)){
            using var cmd=new NpgsqlCommand("UPDATE characters SET document=$1::jsonb,level=$2,coins=$3,version=version+1,updated_at=now() WHERE id=$4 AND account_id=$5 AND version=$6",db,tx);
            cmd.Parameters.AddWithValue(JsonSerializer.Serialize(c));cmd.Parameters.AddWithValue(c.State.Level);cmd.Parameters.AddWithValue(c.State.Coins);cmd.Parameters.AddWithValue(c.Id);cmd.Parameters.AddWithValue(c.AccountId);cmd.Parameters.AddWithValue(c.Version);
            if(cmd.ExecuteNonQuery()!=1)throw new GameRuleException("Speicherversion veraltet.");
        }
        tx.Commit();foreach(var c in batch)c.Version++;
    }
    public void Dispose()=>source.Dispose();
}
