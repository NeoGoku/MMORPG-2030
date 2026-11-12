using MMORPG.Server;
public static class M4PetTests
{
    public static void Run()
    {
        void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS M4 pet: "+label);}
        var g=GameCatalog.Load("Client/data/game.json");var c=new CharacterRecord();PetRules.Normalize(c);
        c.State.Inventory["pet_egg"]=1;Check(PetRules.Hatch(c.State,g)&&c.State.OwnedPets.Count==2,"hatch owned second pet consumes egg");
        c.State.Pet.Level=2;c.State.OwnedPets[1].Level=2;c.State.Inventory["pet_feed"]=2;
        var parent=c.State.OwnedPets[1];Check(PetRules.Breed(c.State,parent.Id,g)&&c.State.OwnedPets.Count==3,"breed two owned adults");
        var child=c.State.OwnedPets[^1];Check(child.Parents.Count==2&&child.Traits.All(t=>t.Value==c.State.Pet.Traits[t.Key]||t.Value==parent.Traits[t.Key]),"offspring inherits actual parental traits");
        c.State.Pet.Level=5;c.State.Inventory["evolution_shard"]=1;Check(PetRules.Evolve(c.State,g)&&c.State.Pet.Evolution==1,"evolution consumes catalyst and changes stage");
        c.State.Pet.Active=false;c.State.Inventory["pet_feed"]=1;Check(PetRules.Train(c.State,1000,g),"offline training begins once");
        Check(!PetRules.Train(c.State,1001,g),"training cannot be duplicated");var before=c.State.Pet.Exp;
        Check(PetRules.Complete(c.State.Pet,1000+g.PetTrainingSeconds,g)&&c.State.Pet.Exp!=before,"completed offline interval grants bounded progression");
        var exp=c.State.Pet.Exp;Check(!PetRules.Complete(c.State.Pet,100000,g)&&c.State.Pet.Exp==exp,"offline reward cannot be claimed repeatedly");
    }
}
