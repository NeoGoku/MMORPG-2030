using MMORPG.Server;

public static class WorldContentTests
{
    public static void Run()
    {
        var district=District.Load("Client/data/district.json");
        var catalog=GameCatalog.Load("Client/data/game.json");
        void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS world content: "+label);}
        foreach(var passage in district.Passages){
            Check(district.Walkable(passage.From.X,passage.From.Z)&&district.Walkable(passage.To.X,passage.To.Z),passage.Id+" endpoints are walkable");
            var low=new World(district,catalog);var id=low.JoinCharacter(new CharacterRecord{State=new(){X=passage.From.X,Z=passage.From.Z}})!;
            low.Act(id,1,"world_transit",passage.Id);Check(low.View(id).Players.Single().X==passage.From.X,passage.Id+" rejects level bypass");
            var high=new World(district,catalog);id=high.JoinCharacter(new CharacterRecord{State=new(){Level=30,X=passage.From.X,Z=passage.From.Z}})!;
            high.Input(id,1,1,0);high.Act(id,2,"world_transit",passage.Id);high.Step(.05);
            Check(high.View(id).Players.Single().X==passage.To.X&&high.View(id).Players.Single().Z==passage.To.Z,passage.Id+" transit stops input drift");
            high.Act(id,3,"world_transit",passage.Id);Check(high.View(id).Players.Single().X==passage.From.X,passage.Id+" has same-world return");
            high.Act(id,4,"world_transit","forged");Check(high.View(id).Players.Single().X==passage.From.X,"unknown passage rejected");
        }
        Check(district.Encounters.All(m=>district.Walkable(m.X,m.Z)),"all encounter spawns avoid structural walls");
        Check(district.Encounters.Where(m=>m.LootItem!="").All(m=>catalog.Items.ContainsKey(m.LootItem)),"new encounter loot resolves real items");
        var store=new MemoryGameStore();var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));
        var character=new CharacterRecord{AccountId=account,ClassId="Tank",State=new(){Level=20,X=83,Z=-52,Inventory=new(){{"helix_shard",5},{"storm_core",1},{"ancient_ore",3}},Professions=new(){{"smithing",300}},ClaimedQuests=["helix_lab"]}};
        store.Create(character);var world=new World(district,catalog,store);world.JoinCharacter(character);
        world.Act(character.Id,1,"craft","helix_plate");world.Act(character.Id,2,"claim","helix_master");
        var saved=store.Get(character.Id)!;
        Check(saved.State.Inventory.GetValueOrDefault("helix_plate")==1&&saved.State.Inventory.GetValueOrDefault("reset_stone")==1,"outpost crafting and chained quest reward persist");
        Check(CharacterRules.Navigation(saved.State,catalog.Quests.Single(q=>q.Id=="helix_signal"),catalog)?.X==86,"quest objective points into playable region");
        var ready=new CharacterState{Level=20,X=80,Z=-52,QuestProgress=new(){{"helix_signal",3}}};
        Check(CharacterRules.Navigation(ready,catalog.Quests.Single(q=>q.Id=="helix_signal"),catalog)?.X==80,"completed quest routes to nearest field trader");
        world.Leave(character.Id);
        saved=store.Get(character.Id)!;saved.State.X=86;saved.State.Z=-65;saved.State.Attributes=catalog.Attributes.ToDictionary(a=>a,_=>1000);store.Save(saved);world=new World(district,catalog,store);world.JoinCharacter(saved);
        world.Act(saved.Id,1,"target","helix-sentry");world.Act(saved.Id,2,"attack","");world.Step(.05);
        var drop=world.View(saved.Id).Loot.Single(l=>l.MonsterId=="helix-sentry");world.Act(saved.Id,3,"loot",drop.Id);world.Act(saved.Id,4,"loot",drop.Id);
        Check(store.Get(saved.Id)!.State.Inventory.GetValueOrDefault("helix_shard")==1,"new monster grants configured personal loot once");
    }
}
