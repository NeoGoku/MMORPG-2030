namespace MMORPG.Server;
public static class ClassRules
{
 public static ClassDef Kit(CharacterRecord c,GameCatalog g)=>g.Classes[c.ClassId];
 public static int Attack(CharacterRecord c,GameCatalog g)=>c.State.Level>=Kit(c,g).PassiveLevel?Kit(c,g).PassiveAttack:0;
 public static int Defense(CharacterRecord c,GameCatalog g)=>c.State.Level>=Kit(c,g).PassiveLevel?Kit(c,g).PassiveDefense:0;
 public static int Healing(CharacterRecord c,GameCatalog g)=>c.State.Level>=Kit(c,g).PassiveLevel?Kit(c,g).PassiveHeal:0;
}
