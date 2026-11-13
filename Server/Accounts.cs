using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MMORPG.Server;

public sealed record LoginSession(string Token,DateTimeOffset Expires,bool Persistent);
public sealed class AccountService(IGameStore store,GameCatalog catalog)
{
    private readonly ConcurrentDictionary<string,(string Account,DateTimeOffset Expires)> sessions=new();
    private readonly ConcurrentDictionary<string,(string Account,string Character,DateTimeOffset Expires)> tickets=new();
    private static string Key(string token)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string Token()=>Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static byte[] Hash(string password,byte[] salt)=>Rfc2898DeriveBytes.Pbkdf2(password,salt,600000,HashAlgorithmName.SHA256,32);
    public LoginSession Register(string username,string password){username=(username??"").ToLowerInvariant();password??="";if(!Regex.IsMatch(username,"^[a-z0-9_]{3,24}$")||password.Length is <12 or >128)throw new GameRuleException("Account: 3–24 Buchstaben/Ziffern/_; Passwort: 12–128 Zeichen.");var a=new AccountRecord(Guid.NewGuid().ToString("N"),username,RandomNumberGenerator.GetBytes(16),[]);a=a with{PasswordHash=Hash(password,a.Salt)};store.Register(a);return NewSession(a.Id);}
    public LoginSession? Login(string username,string password){if(password is null||password.Length>128)return null;var a=store.Account((username??"").ToLowerInvariant());var salt=a?.Salt??new byte[16];var hash=Hash(password,salt);return a is not null&&CryptographicOperations.FixedTimeEquals(hash,a.PasswordHash)?NewSession(a.Id):null;}
    private LoginSession NewSession(string id){Prune();if(sessions.Count>=10000)throw new GameRuleException("Zu viele Sitzungen.");var t=Token();var expires=DateTimeOffset.UtcNow.AddHours(12);sessions[Key(t)]=(id,expires);return new(t,expires,store.Durable);}
    private void Prune(){var now=DateTimeOffset.UtcNow;foreach(var x in sessions.Where(x=>x.Value.Expires<now))sessions.TryRemove(x.Key,out _);foreach(var x in tickets.Where(x=>x.Value.Expires<now))tickets.TryRemove(x.Key,out _);}
    public string? Resolve(string token)=>token.Length==64&&sessions.TryGetValue(Key(token),out var s)&&s.Expires>DateTimeOffset.UtcNow?s.Account:null;
    public List<CharacterRecord> List(string account)=>store.List(account);
    public CharacterRecord CreateCharacter(string account,int slot,string name,string classId,string appearance="default"){name??="";classId??="";if(slot is <0 or >2||!catalog.Classes.ContainsKey(classId)||!Regex.IsMatch(name,@"^[\p{L}\p{N}_ -]{3,20}$"))throw new GameRuleException("Ungültiger Name, Platz oder Klasse.");appearance??="default";if(appearance=="default")appearance="neutral|0|0|0|0|0";if(!AppearanceRules.Valid(appearance))throw new GameRuleException("Ungültige Erscheinung.");var c=new CharacterRecord{AccountId=account,Slot=slot,Name=name,ClassId=classId,Appearance=appearance};c.State.Hp=catalog.BaseHp;c.State.Mana=catalog.BaseMana;c.State.Attributes=catalog.Attributes.ToDictionary(x=>x,_=>0);c.State.Professions=catalog.Professions.ToDictionary(x=>x,_=>0);store.Create(c);return c;}
    public string Ticket(string session,string id){Prune();if(id is null||id.Length!=32)throw new GameRuleException("Charakter nicht verfügbar.");var account=Resolve(session);if(account is null||store.Get(id)?.AccountId!=account)throw new GameRuleException("Charakter nicht verfügbar.");if(tickets.Count>=1000)throw new GameRuleException("Zu viele Anfragen.");var t=Token();tickets[Key(t)]=(account,id,DateTimeOffset.UtcNow.AddSeconds(30));return t;}
    public CharacterRecord? ConsumeTicket(string token){if(token.Length!=64||!tickets.TryRemove(Key(token),out var t)||t.Expires<DateTimeOffset.UtcNow)return null;var c=store.Get(t.Character);return c?.AccountId==t.Account?c:null;}
}
