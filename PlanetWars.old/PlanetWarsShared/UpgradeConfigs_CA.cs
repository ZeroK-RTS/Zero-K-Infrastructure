using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetWarsShared
{
#if !BA_UPGRADES
    public class Upgrades
    {
        public List<UpgradeDef> UpgradeDefs { get; set; }
        public Upgrades()
        {
            UpgradeDef.GlobalID = 0;
            UpgradeDefs = new List<UpgradeDef> {
              new UpgradeDef("Buildings", "Defense", 1, "Arm", "LLT, Defender, Radar Tower, Sonar Station",
                 new List<UnitDef>{
                    new UnitDef("armllt", "Ray"),
                    new UnitDef("armrl", "Defender"),
                    new UnitDef("armrad", "Radar Tower"),
                    new UnitDef("armsonar", "Sonar Station"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 1, "Core", "LLT, Pulverizer, Radar Tower, Sonar Station",
                 new List<UnitDef>{
                    new UnitDef("corllt", "Lotus"),
                    new UnitDef("corrl", "Pulverizer"),
                    new UnitDef("corrad", "Radar Tower"),
                    new UnitDef("corsonar", "Sonar Station"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 2, "Arm", "Stardust, Faraday, Packo, Torpedo Launcher",
                 new List<UnitDef>{
                    new UnitDef("armdeva", "Stardust"),
                    new UnitDef("armartic", "Faraday"),
                    new UnitDef("armarch", "Packo"),
                    new UnitDef("armtl", "Harpoon"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 2, "Core", "Scorcher, Newton, Razors Kiss, Torpedo Launcher",
                 new List<UnitDef>{
                    new UnitDef("corpre", "Scorcher"),
                    new UnitDef("corjamt", "Newton"),
                    new UnitDef("corrazor", "Razor's Kiss"),
                    new UnitDef("cortl", "Urchin"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 3, "Arm", "HLT, Pitbull, Sneaky Pete",
                 new List<UnitDef>{
                    new UnitDef("armhlt", "Sentinel"),
                    new UnitDef("armpb", "Pit Bull"),
                    new UnitDef("armjamt", "Sneaky Pete"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 3, "Core", "HLT, Pitbull, Aegis",
                 new List<UnitDef>{
                    new UnitDef("corhlt", "Stinger"),
                    new UnitDef("corvipe", "Viper"),
                    new UnitDef("corjamt", "Aegis"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 4, "Arm", "Chainsaw, Adv Radar",
                 new List<UnitDef>{
                    new UnitDef("armcir", "Chainsaw"),
                    new UnitDef("armarad", "Advanced Radar Tower"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 4, "Core", "Flak, Adv Radar",
                 new List<UnitDef>{
                    new UnitDef("corflak", "Cobra"),
                    new UnitDef("corarad", "Advanced Radar Tower"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 5, "Arm", "Advanced Defenses",
                 new List<UnitDef>{
                    new UnitDef("armanni", "Annihilator"),
                    new UnitDef("armamd", "Protector"),
                    new UnitDef("mercury", "Mercury"),
                    new UnitDef("armemp", "Detonator"),
                 } ),

              new UpgradeDef("Buildings", "Defense", 5, "Core", "Advanced Defense",
                 new List<UnitDef>{
                    new UnitDef("cordoom", "Doomsday Machine"),
                    new UnitDef("corfmd", "Fortitude"),
                    new UnitDef("corfmd", "Fortitude"),
                    new UnitDef("screamer", "Screamer"),
                    new UnitDef("cortron", "Catalyst"),
                 } ),

              new UpgradeDef("Buildings", "Apocalyptic", 7, "Arm", "Nuclear Silo or Big Bertha",
                 new List<UnitDef>{
                    new UnitDef("armsilo", "Retaliator"),
                    new UnitDef("armbrtha", "Big Bertha"),
                 } ),

              new UpgradeDef("Buildings", "Apocalyptic", 7, "Core", "Nuclear Silo or Intimidator",
                 new List<UnitDef>{
                    new UnitDef("corsilo", "Silencer"),
                    new UnitDef("corint", "Intimidator"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 1, "Arm", "Basic mex/energy",
                 new List<UnitDef>{
                    new UnitDef("armmex", "Metal Extractor"),
                    new UnitDef("armsolar", "Solar Collector"),
                    new UnitDef("armwin", "Wind Generator"),
                    new UnitDef("armtide", "Tidal Generator"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 1, "Core", "Basic mex/energy",
                 new List<UnitDef>{
                    new UnitDef("cormex", "Metal Extractor"),
                    new UnitDef("corsolar", "Solar Collector"),
                    new UnitDef("corwin", "Wind Generator"),
                    new UnitDef("armtide", "Tidal Generator"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 2, "Arm", "Nanotower, Air Pad",
                 new List<UnitDef>{
                    new UnitDef("armnanotc", "Caretaker"),
                    new UnitDef("armasp", "Air Repair Pad"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 2, "Core", "Nanotower, Air Pad",
                 new List<UnitDef>{
                    new UnitDef("cornanotc", "Custodian"),
                    new UnitDef("corasp", "Air Repair Pad"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 3, "Arm", "Light Lab",
                 new List<UnitDef>{
                    new UnitDef("armlab", "Infantry Bot Factory"),
                    new UnitDef("armvp", "Light Vehicle Factory"),
                    new UnitDef("armsy", "Shipyard"),
                    new UnitDef("armfhp", "Amphibious Operations Platform"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 3, "Core", "Light Lab",
                 new List<UnitDef>{
                    new UnitDef("corlab", "Infantry Bot Factory"),
                    new UnitDef("corvp", "Light Vehicle Factory"),
                    new UnitDef("corsy", "Shipyard"),
                    new UnitDef("corfhp", "Amphibious Operations Platform"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 4, "Arm", "Fusion, Heavy, Special and Air Labs",
                 new List<UnitDef>{
                    new UnitDef("armfus", "Fusion Reactor"),
                    new UnitDef("armalab", "Tactical Walker Factory"),
                    new UnitDef("armavp", "Heavy Tank Factory"),
                    new UnitDef("armap", "Airplane Plant"),
                    new UnitDef("armaap", "Gunship Plant"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 4, "Core", "Fusion, Heavy Lab",
                 new List<UnitDef>{
                    new UnitDef("corfus", "Graviton Power Generator"),
                    new UnitDef("coralab", "Tactical Walker Factory"),
                    new UnitDef("coravp", "Heavy Tank Factory"),
                    new UnitDef("corap", "Airplane Plant"),
                    new UnitDef("coraap", "Gunship Plant"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 5, "Arm", "Advanced Fusion",
                 new List<UnitDef>{
                    new UnitDef("aafus", "Tachyon Collider"),
                 } ),

              new UpgradeDef("Buildings", "Economy", 5, "Core", "Advanced Fusion",
                 new List<UnitDef>{
                    new UnitDef("cafus", "Singularity Reactor"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 2, "Arm", "A ship/hover under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("armch", "Hovercon"),
                    new UnitDef("armbeaver", "Beaver"),
                    new UnitDef("armpt", "Skeeter"),
                    new UnitDef("armsh", "Skimmer"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 2, "Core", "A ship/hover under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("corch", "Hovercon"),
                    new UnitDef("pinchy", "Pinchy"),
                    new UnitDef("corsh", "Scrubber"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 3, "Arm", "A ship/hover under 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("armah", "Swatter"),
                    new UnitDef("armtboat", "Surfboard"),
                    new UnitDef("armcs", "Coral"),
                    new UnitDef("decade", "Decade"),
                    new UnitDef("armamph", "Pelican"),
                    new UnitDef("armanac", "Anaconda"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 3, "Core", "A ship/hover under 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("corcs", "Mariner"),
                    new UnitDef("corah", "Slinger"),
                    new UnitDef("coresupp", "Supporter"),
                    new UnitDef("corpt", "Searcher"),
                    new UnitDef("armtboat", "Surfboard"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 4, "Arm", "A ship/hover under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("dclship", "Hunter"),
                    new UnitDef("armsub", "Lurker"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 4, "Core", "A ship/hover under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("corarch", "Shredder"),
                    new UnitDef("corsub", "Snake"),
                    new UnitDef("coramph", "Gimp"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 5, "Arm", "A ship/hover under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("armaas", "Archer"),
                    new UnitDef("armthovr", "Bear"),
                    new UnitDef("armroy", "Crusader"),
                 } ),

              new UpgradeDef("Units", "Ships/Hovers", 5, "Core", "A ship/hover under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("nsaclash", "Halberd"),
                    new UnitDef("corthovr", "Turtle"),
                    new UnitDef("corroy", "Enforcer"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 1, "Arm", "A vehicle under 75 in cost",
                 new List<UnitDef>{
                    new UnitDef("armfav", "Jeffy"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 1, "Core", "A vehicle under 75 in cost",
                 new List<UnitDef>{
                    new UnitDef("corfav", "Weasel"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 2, "Arm", "A vehicle under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("armflash", "Flash"),
                    new UnitDef("armsam", "Samson"),
                    new UnitDef("arm_conveh", "Pioneer"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 2, "Core", "A vehicle under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("corned", "Mason"),
                    new UnitDef("corgator", "Instigator"),
                    new UnitDef("cormist", "Slasher"),
                    new UnitDef("corvrad", "Informant"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 3, "Arm", "A vehicle under 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("armst", "Gremlin"),
                    new UnitDef("consul", "Consul"),
                    new UnitDef("tawf013", "Shellshocker"),
                    new UnitDef("armstump", "Stumpy"),
                    new UnitDef("armjanus", "Janus"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 3, "Core", "A vehicle under 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("corlevlr", "Leveler"),
                    new UnitDef("coracv", "Welder"),
                    new UnitDef("logkoda", "Kodachi"),
                    new UnitDef("corraid", "Ravager"),
                    new UnitDef("corgarp", "Wolverine"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 4, "Arm", "A vehicle under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("armyork", "Phalanx"),
                    new UnitDef("tawf003", "Mumbo"),
                    new UnitDef("armlatnk", "Panther"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 4, "Core", "A vehicle under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("corsent", "Copperhead"),
                    new UnitDef("core_egg_shell", "Dragon's Egg"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 5, "Arm", "A vehicle under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("armmanni", "Penetrator"),
                    new UnitDef("armmerl", "Merl"),
                    new UnitDef("armbull", "Bulldog"),
                 } ),

              new UpgradeDef("Units", "Vehicles", 5, "Core", "A vehicle under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("cormart", "Pillager"),
                    new UnitDef("correap", "Reaper"),
                    new UnitDef("trem", "Tremor"),
                    new UnitDef("tawf114", "Banisher"),
                 } ),

              new UpgradeDef("Units", "Bots", 1, "Arm", "Glaive, Fleas, Constructor",
                 new List<UnitDef>{
                    new UnitDef("armflea", "Flea"),
                    new UnitDef("armpw", "Glaive"),
                    new UnitDef("armrectr", "Rector"),
                 } ),

              new UpgradeDef("Units", "Bots", 1, "Core", "Bandit, Cloggers, Constructor",
                 new List<UnitDef>{
                    new UnitDef("corclog", "Clogger"),
                    new UnitDef("corak", "Bandit"),
                    new UnitDef("cornecro", "Necro"),
                 } ),

              new UpgradeDef("Units", "Bots", 2, "Arm", "A bot under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("arm_spider", "Weaver"),
                    new UnitDef("armfast", "Zipper"),
                    new UnitDef("armham", "Hammer"),
                    new UnitDef("arm_marky", "Marky"),
                    new UnitDef("armjeth", "Jethro"),
                    new UnitDef("armtick", "Tick"),
                    new UnitDef("armrock", "Rocko"),
                 } ),

              new UpgradeDef("Units", "Bots", 2, "Core", "A bot under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("corthud", "Thug"),
                    new UnitDef("corstorm", "Rogue"),
                    new UnitDef("corcrash", "Crasher"),
                 } ),

              new UpgradeDef("Units", "Bots", 3, "Arm", "A bot under 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("armwar", "Warrior"),
                    new UnitDef("armspy", "Infiltrator"),
                    new UnitDef("armsptk", "Recluse"),
                    new UnitDef("arm_venom", "Venom"),
                    new UnitDef("armzeus", "Zeus"),
                 } ),

              new UpgradeDef("Units", "Bots", 3, "Core", "A bot under 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("corfast", "Freaker"),
                    new UnitDef("cormort", "Morty"),
                    new UnitDef("corroach", "Roach"),
                    new UnitDef("corpyro", "Pyro"),
                    new UnitDef("cormak", "Outlaw"),
                 } ),

              new UpgradeDef("Units", "Bots", 4, "Arm", "A bot under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("armaser", "Eraser"),
                    new UnitDef("armaak", "Archangel"),
                 } ),

              new UpgradeDef("Units", "Bots", 4, "Core", "A bot under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("core_spectre", "Aspis"),
                    new UnitDef("coraak", "Manticore"),
                    new UnitDef("corhrk", "Dominator"),
                    new UnitDef("corsktl", "Skuttle"),
                 } ),

              new UpgradeDef("Units", "Bots", 5, "Arm", "A bot under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("armsnipe", "Sharpshooter"),
                 } ),

              new UpgradeDef("Units", "Bots", 5, "Core", "A bot under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("corcan", "Jack"),
                 } ),

              new UpgradeDef("Units", "Bots", 6, "Arm", "A bot under 2500 in cost",
                 new List<UnitDef>{
                    new UnitDef("armcrabe", "Crabe"),
                 } ),

              new UpgradeDef("Units", "Bots", 6, "Core", "A bot under 2500 in cost",
                 new List<UnitDef>{
                    new UnitDef("corsumo", "Sumo"),
                 } ),

              new UpgradeDef("Units", "Bots", 7, "Arm", "Assault Strider",
                 new List<UnitDef>{
                    new UnitDef("armraz", "Razorback"),
                 } ),

              new UpgradeDef("Units", "Bots", 7, "Core", "Assault Strider",
                 new List<UnitDef>{
                    new UnitDef("corkarg", "Karganeth"),
                 } ),

              new UpgradeDef("Units", "Air", 1, "Arm", "A plane under 75 in cost",
                 new List<UnitDef>{
                    new UnitDef("bladew", "Gnat"),
                 } ),

              new UpgradeDef("Units", "Air", 1, "Core", "A plane under 75 in cost",
                 new List<UnitDef>{
                    new UnitDef("blastwing", "Blastwing"),
                 } ),

              new UpgradeDef("Units", "Air", 2, "Arm", "A plane under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("armfig", "Swiftspear"),
                 } ),

              new UpgradeDef("Units", "Air", 2, "Core", "A plane under 150 in cost",
                 new List<UnitDef>{
                    new UnitDef("fighter", "Avenger"),
                 } ),

              new UpgradeDef("Units", "Air", 3, "Arm", "A plane under 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("armhawk", "Hawk"),
                    new UnitDef("armca", "Crane"),
                    new UnitDef("armawac", "Cirrus"),
                    new UnitDef("armthund", "Thunder"),
                    new UnitDef("armkam", "Banshee"),
                 } ),

              new UpgradeDef("Units", "Air", 3, "Core", "A plane aunder 350 in cost",
                 new List<UnitDef>{
                    new UnitDef("corshad", "Shadow"),
                    new UnitDef("corca", "Bumblebee"),
                    new UnitDef("corvamp", "Vamp"),
                    new UnitDef("corawac", "Vulture"),
                    new UnitDef("corape", "Rapier"),
                    new UnitDef("owl", "Owl"),
                 } ),

              new UpgradeDef("Units", "Air", 4, "Arm", "A plane under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("corgripn", "Stiletto"),
                    new UnitDef("armpnix", "Tempest"),
                 } ),

              new UpgradeDef("Units", "Air", 4, "Core", "A plane under 600 in cost",
                 new List<UnitDef>{
                    new UnitDef("corhurc", "Condor"),
                    new UnitDef("corhurc2", "Firestorm"),
                 } ),

              new UpgradeDef("Units", "Air", 5, "Arm", "A plane under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("armbrawl", "Brawler"),
                 } ),

              new UpgradeDef("Units", "Air", 5, "Core", "A plane under 1200 in cost",
                 new List<UnitDef>{
                    new UnitDef("blackdawn", "Black Dawn"),
                 } ),

              new UpgradeDef("Spacefleets", "Blockade", 4, "Arm", "Blocks enemy upgrades and can invade far away planets",
                 new List<UnitDef>{
                    new UnitDef("fleet_blockade", "Blockade fleet"),
                 } ),

              new UpgradeDef("Spacefleets", "Blockade", 4, "Core", "Blocks enemy upgrades and can invade far away planets",
                 new List<UnitDef>{
                    new UnitDef("fleet_blockade", "Blockade fleet"),
                 } ),

            };
        }

    }
#endif
}
