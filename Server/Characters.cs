using System.Numerics;
using System.Text.Json;

namespace MMORPG.Server;

public sealed class GameRuleException(string message) : Exception(message);
public sealed class SkillDef {public int PvpPower {get;set;}=18;public int FocusCost {get;set;} public string Id {get;set;}="";public string Name {get;set;}=""; public string Kind {get;set;}="";public double Power {get;set;} public double Range {get;set;} public int Cost {get;set;} public double Cooldown {get;set;} public bool Enabled {get;set;} public int MinLevel {get;set;}=1;public double Radius {get;set;}public double Duration {get;set;}=8;public string Status {get;set;}=""; }
public sealed class ClassDef {public string Resource {get;set;}="Focus";public string Passive {get;set;}="";public int PassiveLevel {get;set;}=5;public int PassiveAttack {get;set;}public int PassiveDefense {get;set;}public int PassiveHeal {get;set;}public int ResourceRegen {get;set;}=2; public string Attribute {get;set;}=""; public List<SkillDef> Skills {get;set;}=[]; }
public sealed class ItemDef {public int MinLevel {get;set;}=1;public List<string> Classes {get;set;}=[];public string Name {get;set;}="";public int Stack {get;set;}public int Price {get;set;}public string Rarity {get;set;}="Normal"; public string Slot {get;set;}="";public int Attack {get;set;}public int Defense {get;set;}public int Heal {get;set;} }
public sealed class Recipe {public int MinProfessionLevel {get;set;}=1;public string Tool {get;set;}="";public string Name {get;set;}="";public string Profession {get;set;}="";public Dictionary<string,int> Inputs {get;set;}=[];public string Output {get;set;}="";public int Count {get;set;} }
public sealed class QuestDef {public Point? Navigation {get;set;}public int MinLevel {get;set;}=1;public string ClassId {get;set;}="";public bool Daily {get;set;}public int Points {get;set;}public Dictionary<string,int> Rewards {get;set;}=[];public string Id {get;set;}="";public string Name {get;set;}="";public string Type {get;set;}="";public string Event {get;set;}="";public string Item {get;set;}="";public string Prerequisite {get;set;}="";public int Required {get;set;}public int Coins {get;set;}public int Exp {get;set;} }
public sealed class ResourceNode : Point {public int MinProfessionLevel {get;set;}=1;public string Id {get;set;}="";public string Name {get;set;}="";public string Profession {get;set;}="";public string Item {get;set;}="";}
public sealed class GameCatalog
{
    public List<string> Attributes {get;set;}=[];public Dictionary<string,ClassDef> Classes {get;set;}=[]; public Dictionary<string,ItemDef> Items {get;set;}=[];
    public Dictionary<string,Recipe> Recipes {get;set;}=[];public List<string> Professions {get;set;}=[];public List<ResourceNode> Nodes {get;set;}=[];public Dictionary<string,Point> Services {get;set;}=[];public List<QuestDef> Quests {get;set;}=[];
    public int MaxStalls {get;set;}=100;public int StallSeconds {get;set;}=86400;
    public int RepairPerPoint {get;set;}=1;
    public int ResetLevel {get;set;}=10;public int PrestigeLevel {get;set;}=100;public int DeathExpPercent {get;set;}=5;
    public int AuctionDays {get;set;}=7;public int MarketPageSize {get;set;}=20;
    public int BaseAttack {get;set;}=14;public int BaseHp {get;set;}=100;public int VitalityHp {get;set;}=8;public int BaseMana {get;set;}=100;public int WisdomMana {get;set;}=3;public int ManaRegen {get;set;}=3;public int BuffAttack {get;set;}=5;public int BuffDefense {get;set;}=3;public int BuffSeconds {get;set;}=8;public int PetDamage {get;set;}=4;public int PetHeal {get;set;}=4;public int PetInterval {get;set;}=3;public int PetRange {get;set;}=10;public int PetExpPerLevel {get;set;}=50;public int LevelExpBase {get;set;}=100;
    public int FarmRadius {get;set;}=14;public int FarmSeconds {get;set;}=60;public int DaySeconds {get;set;}=600;public int WeatherSeconds {get;set;}=120;
    public int MaxOwnedPets {get;set;}=10;public int PetBreedLevel {get;set;}=2;public int PetEvolutionLevel {get;set;}=5;public int PetTrainingSeconds {get;set;}=300;public int PetTrainingExp {get;set;}=50;
    public double InterestRadius {get;set;}=52;public int ProfessionGatherXp {get;set;}=10;public int ProfessionCraftXp {get;set;}=25;public int ProfessionLevelXp {get;set;}=100;public int HotHeal {get;set;}=4;public int StatusPenalty {get;set;}=3;public int BlinkDistance {get;set;}=4;
    public int PartyLimit {get;set;}=10;public double PartyExpRadius {get;set;}=40;public double LocalChatRadius {get;set;}=30;
    public int InventorySlots {get;set;}=24;public int BaseFarmCost {get;set;}=100;public int FarmGrowth {get;set;}=2;
    public static GameCatalog Load(string path)=>JsonSerializer.Deserialize<GameCatalog>(File.ReadAllText(path),new JsonSerializerOptions{PropertyNameCaseInsensitive=true})!;
}
public sealed class PetState {public string Id {get;set;}="";public int Hp {get;set;}=100;public double RecoveryUntil {get;set;}public double TrainingUntil {get;set;}public Dictionary<string,int> Traits {get;set;}=new(){{"strength",1},{"affinity",1},{"vitality",1}};public Dictionary<string,int> Inventory {get;set;}=[];public bool Active {get;set;} public string Mode {get;set;}="combat";public int Level {get;set;}=1;public int Exp {get;set;}public int Evolution {get;set;}public List<string> Parents {get;set;}=[];}
public sealed class CharacterState
{
    public Dictionary<string,GearState> Gear {get;set;}=[];public Dictionary<string,string> EquippedGear {get;set;}=[];
    public int WearCounter {get;set;}
    public int Focus {get;set;}=100;
    public int Prestige {get;set;}
    public Dictionary<string,long> DailyClaims {get;set;}=[];
    public bool HousingIntro {get;set;}
    public int Level {get;set;}=1;public int Exp {get;set;}public int StatPoints {get;set;}public int Coins {get;set;}=20;public int Scrap {get;set;}public int Kills {get;set;}public string Quest {get;set;}="available";
    public int Hp {get;set;}=100;public Dictionary<string,double> Timers {get;set;}=[];public double BuffExpires {get;set;}
    public double X {get;set;}=0;public double Z {get;set;}=10;public int Mana {get;set;}=100;public Dictionary<string,int> Attributes {get;set;}=[];
    public Dictionary<string,int> Inventory {get;set;}=new(){{"potion",2}};public Dictionary<string,string> Equipment {get;set;}=[];
    public Dictionary<string,int> Professions {get;set;}=[];public Dictionary<string,int> QuestProgress {get;set;}=[];public HashSet<string> ClaimedQuests {get;set;}=[];
    public List<string> SkillBar {get;set;}=["skill1","skill2","skill3","skill4","skill5","skill6"];
    public string FarmXp {get;set;}="0";public int FarmedPoints {get;set;}public PetState Pet {get;set;}=new();public List<PetState> OwnedPets {get;set;}=[];
}
public sealed class CharacterRecord
{
    public string Id {get;set;}=Guid.NewGuid().ToString("N");public string AccountId {get;set;}="";public int Slot {get;set;}public string Name {get;set;}="Reisender";public string ClassId {get;set;}="Shield Sword";
    public string Appearance {get;set;}="default";public long Version {get;set;} public CharacterState State {get;set;}=new();
}
public static class CharacterRules
{
    public static CharacterRecord Clone(CharacterRecord c){var copy=JsonSerializer.Deserialize<CharacterRecord>(JsonSerializer.Serialize(c))!;PetRules.Normalize(copy);return copy;}
    public static bool Allocate(CharacterState c,string attribute,GameCatalog g){if(c.StatPoints<1||!g.Attributes.Contains(attribute))return false;c.StatPoints--;c.Attributes[attribute]=checked(c.Attributes.GetValueOrDefault(attribute)+1);return true;}
    public static BigInteger FarmCost(CharacterState c,GameCatalog g)=>g.BaseFarmCost*BigInteger.Pow(g.FarmGrowth,c.FarmedPoints);
    public static bool FarmPoint(CharacterState c,GameCatalog g){var xp=BigInteger.Parse(c.FarmXp);var cost=FarmCost(c,g);if(xp<cost)return false;c.FarmXp=(xp-cost).ToString();c.FarmedPoints++;c.StatPoints++;return true;}
    public static int Slots(CharacterState c,GameCatalog g)=>c.Gear.Count+c.Inventory.Sum(i=>(i.Value+g.Items[i.Key].Stack-1)/g.Items[i.Key].Stack);
    public static bool Add(CharacterState c,string item,int amount,GameCatalog g){if(!g.Items.ContainsKey(item)||amount<1)return false;var previous=c.Inventory.GetValueOrDefault(item);c.Inventory[item]=checked(previous+amount);if(Slots(c,g)>g.InventorySlots){if(previous==0)c.Inventory.Remove(item);else c.Inventory[item]=previous;return false;}return true;}
    public static bool Buy(CharacterState c,string item,GameCatalog g){if(!g.Items.TryGetValue(item,out var d)||c.Coins<d.Price||!Add(c,item,1,g))return false;c.Coins-=d.Price;return true;}
    public static bool Sell(CharacterState c,string item,GameCatalog g){if(!g.Items.TryGetValue(item,out var d)||c.Inventory.GetValueOrDefault(item)<1||c.Equipment.Values.Contains(item))return false;c.Inventory[item]--;if(c.Inventory[item]==0)c.Inventory.Remove(item);c.Coins=checked(c.Coins+Math.Max(1,d.Price/2));return true;}
    public static bool Equip(CharacterState c,string item,GameCatalog g,string classId=""){if(c.Inventory.GetValueOrDefault(item)<1||!g.Items.TryGetValue(item,out var d)||d.Slot==""||c.Level<d.MinLevel||d.Classes.Count>0&&!d.Classes.Contains(classId))return false;c.EquippedGear.Remove(d.Slot);c.Equipment[d.Slot]=item;return true;}
    public static bool Craft(CharacterState c,string recipe,GameCatalog g){if(!g.Recipes.TryGetValue(recipe,out var r)||1+c.Professions.GetValueOrDefault(r.Profession)/g.ProfessionLevelXp<r.MinProfessionLevel||r.Tool!=""&&c.Inventory.GetValueOrDefault(r.Tool)<1||r.Inputs.Any(i=>c.Inventory.GetValueOrDefault(i.Key)<i.Value))return false;var copy=new Dictionary<string,int>(c.Inventory);foreach(var i in r.Inputs){c.Inventory[i.Key]-=i.Value;if(c.Inventory[i.Key]==0)c.Inventory.Remove(i.Key);}if(!Add(c,r.Output,r.Count,g)){c.Inventory=copy;return false;}c.Professions[r.Profession]=c.Professions.GetValueOrDefault(r.Profession)+g.ProfessionCraftXp;c.FarmXp=(BigInteger.Parse(c.FarmXp)+25).ToString();return true;}
    public static int Attack(CharacterRecord c,GameCatalog g)=>g.BaseAttack+ClassRules.Attack(c,g)+Math.Min(c.State.Prestige,100)+c.State.Attributes.GetValueOrDefault(g.Classes[c.ClassId].Attribute)+c.State.Equipment.Values.Sum(i=>g.Items[i].Attack)+GearRules.Bonus(c.State,g,true);
    public static int Defense(CharacterRecord c,GameCatalog g)=>ClassRules.Defense(c,g)+c.State.Attributes.GetValueOrDefault("armor")+c.State.Equipment.Values.Sum(i=>g.Items[i].Defense)+GearRules.Bonus(c.State,g,false);
    public static int MaxHp(CharacterRecord c,GameCatalog g)=>g.BaseHp+c.State.Attributes.GetValueOrDefault("vitality")*g.VitalityHp;
    public static Point? Navigation(CharacterState c,QuestDef q,GameCatalog g)=>!QuestOpen(c,q)?null:c.QuestProgress.GetValueOrDefault(q.Id)>=q.Required?g.Services.GetValueOrDefault("merchant"):q.Navigation;
    public static long Day=>DateTimeOffset.UtcNow.ToUnixTimeSeconds()/86400;
    public static bool QuestOpen(CharacterState c,QuestDef q)=>c.Level>=q.MinLevel&&(q.Prerequisite==""||c.ClaimedQuests.Contains(q.Prerequisite))&&(!c.ClaimedQuests.Contains(q.Id)||q.Daily&&c.DailyClaims.GetValueOrDefault(q.Id)!=Day);
    public static void Progress(CharacterState c,string kind,string item,GameCatalog g){foreach(var q in g.Quests.Where(q=>QuestOpen(c,q)&&q.Event==kind&&(q.Item==""||q.Item==item))){if(q.Daily&&c.DailyClaims.GetValueOrDefault(q.Id,-1)!=Day){c.QuestProgress[q.Id]=0;c.DailyClaims[q.Id]=Day;c.ClaimedQuests.Remove(q.Id);}c.QuestProgress[q.Id]=Math.Min(q.Required,c.QuestProgress.GetValueOrDefault(q.Id)+1);}}
    public static bool Claim(CharacterState c,string id,GameCatalog g,string classId=""){
        var q=g.Quests.FirstOrDefault(q=>q.Id==id);if(q is null||!QuestOpen(c,q)||q.ClassId!=""&&q.ClassId!=classId||c.QuestProgress.GetValueOrDefault(id)<q.Required||q.Daily&&c.DailyClaims.GetValueOrDefault(id,-1)!=Day)return false;
        if((long)c.Coins+q.Coins>int.MaxValue||(long)c.StatPoints+q.Points>int.MaxValue)return false;
        var inventory=new Dictionary<string,int>(c.Inventory);foreach(var item in q.Rewards)if(!Add(c,item.Key,item.Value,g)){c.Inventory=inventory;return false;}
        c.ClaimedQuests.Add(id);if(q.Daily)c.DailyClaims[id]=Day;c.Coins+=q.Coins;c.StatPoints+=q.Points;c.Exp+=q.Exp;
        while(c.Level<100&&c.Exp>=c.Level*g.LevelExpBase){c.Exp-=c.Level*g.LevelExpBase;c.Level++;c.StatPoints++;}return true;
    }
    public static bool Reset(CharacterState c,GameCatalog g){if(c.Level<g.ResetLevel||c.Inventory.GetValueOrDefault("reset_stone")<1)return false;var points=c.Attributes.Values.Sum(v=>(long)v);if(points+c.StatPoints>int.MaxValue)return false;c.Inventory["reset_stone"]--;if(c.Inventory["reset_stone"]==0)c.Inventory.Remove("reset_stone");c.StatPoints+=(int)points;foreach(var key in c.Attributes.Keys.ToArray())c.Attributes[key]=0;return true;}
    public static bool PrestigeUp(CharacterState c,GameCatalog g){if(c.Level<g.PrestigeLevel||c.Inventory.GetValueOrDefault("boss_core")<1||c.Prestige==int.MaxValue)return false;c.Inventory["boss_core"]--;if(c.Inventory["boss_core"]==0)c.Inventory.Remove("boss_core");c.Prestige++;return true;}
}
