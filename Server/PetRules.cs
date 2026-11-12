using System.Security.Cryptography;
using System.Text.Json;
using System.Numerics;
namespace MMORPG.Server;
public static class PetRules
{
    public static PetState Copy(PetState p)=>JsonSerializer.Deserialize<PetState>(JsonSerializer.Serialize(p))!;
    public static int MaxHp(PetState p)=>100+p.Level*5+p.Evolution*20+p.Traits.GetValueOrDefault("vitality",1)*5;
    public static void Normalize(CharacterRecord c)
    {
        if(c.State.Pet.Id=="")c.State.Pet.Id=c.Id+"-pet";
        var current=Copy(c.State.Pet);var index=c.State.OwnedPets.FindIndex(p=>p.Id==current.Id);
        if(index<0)c.State.OwnedPets.Add(current);else c.State.OwnedPets[index]=current;
        foreach(var pet in c.State.OwnedPets)if(pet.Id!=current.Id)pet.Active=false;
    }
    private static bool Consume(CharacterState c,string item,int count){if(c.Inventory.GetValueOrDefault(item)<count)return false;c.Inventory[item]-=count;if(c.Inventory[item]==0)c.Inventory.Remove(item);return true;}
    public static bool Hatch(CharacterState c,GameCatalog g)
    {
        if(c.OwnedPets.Count>=g.MaxOwnedPets||!Consume(c,"pet_egg",1))return false;
        c.OwnedPets.Add(new PetState{Id=Guid.NewGuid().ToString("N"),Traits=new(){{"strength",RandomNumberGenerator.GetInt32(1,5)},{"affinity",RandomNumberGenerator.GetInt32(1,5)},{"vitality",RandomNumberGenerator.GetInt32(1,5)}}});return true;
    }
    public static bool Breed(CharacterState c,string second,GameCatalog g)
    {
        var a=c.Pet;var b=c.OwnedPets.FirstOrDefault(p=>p.Id==second);
        if(b is null||b.Id==a.Id||a.Level<g.PetBreedLevel||b.Level<g.PetBreedLevel||a.TrainingUntil>0||b.TrainingUntil>0||c.OwnedPets.Count>=g.MaxOwnedPets||!Consume(c,"pet_feed",2))return false;
        var child=new PetState{Id=Guid.NewGuid().ToString("N"),Parents=[a.Id,b.Id]};
        foreach(var key in child.Traits.Keys.ToArray())child.Traits[key]=(RandomNumberGenerator.GetInt32(2)==0?a:b).Traits.GetValueOrDefault(key,1);
        c.OwnedPets.Add(child);c.Professions["animal_breeding"]=c.Professions.GetValueOrDefault("animal_breeding")+g.ProfessionCraftXp;c.FarmXp=(BigInteger.Parse(c.FarmXp)+25).ToString();return true;
    }
    public static bool Evolve(CharacterState c,GameCatalog g)
    {
        if(c.Pet.Level<g.PetEvolutionLevel||c.Pet.Evolution>=3||c.Pet.TrainingUntil>0||!Consume(c,"evolution_shard",1))return false;
        c.Pet.Evolution++;c.Pet.Hp=MaxHp(c.Pet);return true;
    }
    public static bool Train(CharacterState c,double now,GameCatalog g)
    {
        if(c.Pet.Active||c.Pet.TrainingUntil>0||!Consume(c,"pet_feed",1))return false;
        c.Pet.TrainingUntil=now+g.PetTrainingSeconds;return true;
    }
    public static bool Complete(PetState p,double now,GameCatalog g)
    {
        if(p.TrainingUntil<=0||p.TrainingUntil>now)return false;
        p.TrainingUntil=0;p.Exp+=g.PetTrainingExp;while(p.Exp>=p.Level*g.PetExpPerLevel){p.Exp-=p.Level*g.PetExpPerLevel;p.Level++;}return true;
    }
    public static bool CompleteAll(CharacterRecord c,double now,GameCatalog g)
    {
        Normalize(c);var changed=Complete(c.State.Pet,now,g);
        foreach(var pet in c.State.OwnedPets.Where(p=>p.Id!=c.State.Pet.Id))changed|=Complete(pet,now,g);
        Normalize(c);return changed;
    }
    public static bool Select(CharacterState c,string id)
    {
        var chosen=c.OwnedPets.FirstOrDefault(p=>p.Id==id);if(chosen is null||id==c.Pet.Id)return false;
        var current=c.OwnedPets.FindIndex(p=>p.Id==c.Pet.Id);var wasActive=c.Pet.Active;c.Pet.Active=false;
        if(current>=0)c.OwnedPets[current]=Copy(c.Pet);
        c.Pet=Copy(chosen);c.Pet.Active=wasActive&&c.Pet.TrainingUntil<=0;return true;
    }
    public static bool AddBag(PetState p,string item,GameCatalog g)
    {
        var old=p.Inventory.GetValueOrDefault(item);p.Inventory[item]=old+1;
        if(p.Inventory.Sum(i=>(i.Value+g.Items[i.Key].Stack-1)/g.Items[i.Key].Stack)>8){if(old==0)p.Inventory.Remove(item);else p.Inventory[item]=old;return false;}return true;
    }
    public static bool TakeBag(CharacterState c,GameCatalog g)
    {
        var copy=new Dictionary<string,int>(c.Inventory);
        foreach(var item in c.Pet.Inventory)if(!CharacterRules.Add(c,item.Key,item.Value,g)){c.Inventory=copy;return false;}
        c.Pet.Inventory.Clear();return true;
    }
}
