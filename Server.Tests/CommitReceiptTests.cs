using MMORPG.Server;

public static class CommitReceiptTests
{
    public static void Run()
    {
        void Scenario(IGameStore store,string label){
            void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS receipts "+label+": "+message);}
            var account=Guid.NewGuid().ToString("N");store.Register(new(account,account,new byte[32],new byte[32]));
            var c=new CharacterRecord{AccountId=account};PetRules.Normalize(c);store.Create(c);c.State.Coins=9;
            var unacknowledged=CharacterRules.Clone(c);store.Save(c);var receipt=c.LastCommit;
            Check(receipt.Length==64&&store.CommitExists(account,receipt)&&!store.CommitExists("foreign",receipt),"receipt belongs only to transaction account");
            var superseded=CharacterRules.Clone(unacknowledged);
            store.Save(unacknowledged);
            Check(unacknowledged.Version==c.Version&&store.Get(c.Id)!.Version==1&&store.Get(c.Id)!.State.Coins==9,"retransmitted exact write resolves without second revision or transfer");
            var world=store.LoadWorld();world.Proceeds[c.Id]=7;c.State.Coins=3;
            var oldWorld=SharedWorld.Copy(world);var oldCharacter=CharacterRules.Clone(c);
            store.SaveWorld(world,[c]);store.SaveWorld(oldWorld,[oldCharacter]);
            Check(store.LoadWorld().Version==world.Version&&oldWorld.LastCommit==world.LastCommit&&store.Get(c.Id)!.State.Coins==3,"world and character receipt resolve one atomic economic commit");
            c.State.Coins=4;store.Save(c);var rejected=false;
            try{store.Save(superseded);}catch(GameRuleException){rejected=true;}
            Check(rejected&&store.Get(c.Id)!.State.Coins==4,"old confirmed payload cannot overwrite newer progress");
            var ordered=CharacterRules.Clone(c);ordered.State.Attributes=new(){{"b",2},{"a",1}};var identity=CommitIdentity.For([ordered],null);ordered.State.Attributes=new(){{"a",1},{"b",2}};
            Check(identity==CommitIdentity.For([ordered],null),"payload identity is independent of map order");
            if(store is PostgresGameStore&&Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE") is {} cs){using var reopened=new PostgresGameStore(cs);Check(reopened.CommitExists(account,receipt),"receipt survives independent store reopen");}
        }
        Scenario(new MemoryGameStore(),"memory");
        if(Environment.GetEnvironmentVariable("MMORPG_TEST_DATABASE") is {} connection){using var db=new PostgresGameStore(connection);Scenario(db,"PostgreSQL");}
    }
}
