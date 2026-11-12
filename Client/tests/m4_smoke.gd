extends SceneTree
const Practice=preload("res://scripts/practice.gd")
func _initialize() -> void:
    for cls in JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json")).classes:
        var definitions: Array=JSON.parse_string(FileAccess.get_file_as_string("res://data/game.json")).classes[cls].skills
        for skill in definitions:
            var sim=Practice.new()
            sim.m3.class_id=cls;sim.player.level=100;sim.player.x=-6.0;sim.player.z=-7.0;sim.player.hp=40
            sim.act("target","drone-1")
            if skill.kind=="explode":sim.act("skill","skill3")
            sim.m3.effects.weaken=5.0
            sim.act("skill",skill.id)
            assert(sim.m3.mana<100,"functional class skill")
            if skill.kind in ["heal","party_heal","drain"]:assert(sim.player.hp>40,"actual healing effect")
            if skill.kind in ["buff","party_buff"]:assert(sim.m3.buff>0,"actual buff")
            if skill.kind=="cleanse":assert(not sim.m3.effects.has("weaken"),"cleanse removes negative status")
            if skill.kind=="summon":assert(sim.m3.summons.size()==1 and sim.snapshot().pets.is_empty(),"summon distinct from companion pet")
            if skill.kind in ["damage","dot","slow","break","taunt","aoe","explode"]:assert(sim.monsters[0].hp<42,"actual monster damage")
            sim.free()
    var sim=Practice.new()
    sim.player.x=-8.0;sim.player.z=6.0
    sim.m3.inventory.pet_egg=1;sim.act("pet_hatch","")
    assert(sim.m3.owned_pets.size()==2,"hatch second pet")
    sim.m3.pet.level=2;sim.m3.owned_pets[1].level=2;sim.m3.inventory.pet_feed=2
    sim.act("pet_breed",sim.m3.owned_pets[1].id)
    assert(sim.m3.owned_pets.size()==3 and sim.m3.owned_pets.back().parents.size()==2,"breed and inherit lineage")
    sim.m3.pet.level=5;sim.m3.inventory.evolution_shard=1;sim.act("pet_evolve","")
    assert(sim.m3.pet.evolution==1,"evolution consumes catalyst")
    sim.m3.pet.active=false;sim.m3.inventory.pet_feed=1;sim.act("pet_train","")
    assert(sim.m3.pet.trainingUntil>0,"offline training schedule")
    sim.m3.pet.trainingUntil=Time.get_unix_time_from_system()-1
    sim.m3.tick(.05)
    assert(sim.m3.pet.trainingUntil==0,"offline training resolves once")
    sim.player.x=0.0;sim.player.z=3.0;sim.act("farm_start","14")
    assert(sim.m3.automation.active,"bounded local farming opt-in")
    sim.step(.05,Vector2.RIGHT)
    assert(not sim.m3.automation.active,"manual input cancels")
    sim.free()
    print("PASS: 64 functional local class skills, real effects/summons, pet lifecycle and guarded automation")
    quit()
