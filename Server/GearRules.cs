namespace MMORPG.Server;
public sealed class GearState
{
 public string Item {get;set;}="";public int Condition {get;set;}=100;
}
public static class AppearanceRules
{
 public static bool Valid(string value){var p=value.Split('|');return p.Length==6&&p[0] is "male" or "female" or "neutral"&&p.Skip(1).All(x=>int.TryParse(x,out var v)&&v>=0&&v<=3);}
}
public static class GearRules
{
 public static bool Unpack(CharacterState c,string item,GameCatalog g){if(!g.Items.TryGetValue(item,out var d)||d.Slot==""||c.Inventory.GetValueOrDefault(item)<1||c.Gear.Count>=g.InventorySlots||c.Equipment.Values.Contains(item))return false;c.Inventory[item]--;if(c.Inventory[item]==0)c.Inventory.Remove(item);c.Gear[Guid.NewGuid().ToString("N")]=new GearState{Item=item};return true;}
 public static bool Equip(CharacterRecord c,string id,GameCatalog g){if(!c.State.Gear.TryGetValue(id,out var gear)||!g.Items.TryGetValue(gear.Item,out var d)||c.State.Level<d.MinLevel||d.Classes.Count>0&&!d.Classes.Contains(c.ClassId))return false;c.State.Equipment.Remove(d.Slot);c.State.EquippedGear[d.Slot]=id;return true;}
 public static bool Repair(CharacterState c,string id,GameCatalog g){if(!c.Gear.TryGetValue(id,out var gear)||gear.Condition>=100)return false;var price=Math.Max(1,(100-gear.Condition)*g.RepairPerPoint);if(c.Coins<price)return false;c.Coins-=price;gear.Condition=100;return true;}
 public static int Bonus(CharacterState c,GameCatalog g,bool attack)=>c.EquippedGear.Values.Where(c.Gear.ContainsKey).Sum(id=>{var gear=c.Gear[id];var d=g.Items[gear.Item];return gear.Condition>0?(attack?d.Attack:d.Defense):0;});
 public static void Use(CharacterState c){c.WearCounter++;if(c.WearCounter>=10){c.WearCounter=0;Wear(c);}}
 public static void Wear(CharacterState c){foreach(var id in c.EquippedGear.Values)if(c.Gear.TryGetValue(id,out var gear))gear.Condition=Math.Max(0,gear.Condition-1);}
}
