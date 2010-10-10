using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlanetWarsShared
{
#if BA_UPGRADES
    public class Upgrades
    {
        public List<UpgradeDef> UpgradeDefs { get; set; }
        public Upgrades()
        {
            UpgradeDef.GlobalID = 0;
            UpgradeDefs = new List<UpgradeDef> {
                  new UpgradeDef("Buildings", "Defense", 1, "Arm", "LLT or Defenders",
                     new List<UnitDef>{
                              new UnitDef("armllt", "LLT"),
                              new UnitDef("armrl", "Defender"),
                              new UnitDef("armrad", "Radar Tower"),
                              new UnitDef("armsonar", "Sonar Station"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 1, "Core", "LLT or Pulverizer",
                     new List<UnitDef>{
                              new UnitDef("corllt", "LLT"),
                              new UnitDef("corrl", "Pulverizer"),
                              new UnitDef("corrad", "Radar Tower"),
                              new UnitDef("corsonar", "Sonar Station"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 2, "Arm", "Beamer, Dragon's Claw, Packo, Sneaky Pete, Torpedo Launcher",
                     new List<UnitDef>{
                              new UnitDef("tawf001", "Beamer"),
                              new UnitDef("armclaw", "Dragon's Claw"),
                              new UnitDef("packo", "Pack0"),
                              new UnitDef("armjamt", "Sneaky Pete"),
                              new UnitDef("armtl", "Harpoon"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 2, "Core", "HLLT, SAM, Castro, Torpedo Launcher",
                     new List<UnitDef>{
                              new UnitDef("hllt", "HLLT"),
                              new UnitDef("madsam", "SAM"),
                              new UnitDef("corjamt", "Castro"),
                              new UnitDef("cortl", "Urchin"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 3, "Arm", "HLT Pitbull",
                     new List<UnitDef>{
                              new UnitDef("armhlt", "Sentinel"),
                              new UnitDef("armpb", "Pit Bull"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 3, "Core", "HLT Pitbull",
                     new List<UnitDef>{
                              new UnitDef("corhlt", "Gaat Gun"),
                              new UnitDef("corvipe", "Viper"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 4, "Arm", "Chainsaw, Flak, Adv Radar, Adv Jammer",
                     new List<UnitDef>{
                              new UnitDef("armcir", "Chainsaw"),
                              new UnitDef("armflak", "Flakker"),
                              new UnitDef("armarad", "Advanced Radar Tower"),
                              new UnitDef("armveil", "Veil"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 4, "Core", "Flak, Eradicator, Adv Radar, Adv Jammer",
                     new List<UnitDef>{
                              new UnitDef("corflak", "Cobra"),
                              new UnitDef("corerad", "Eradicator"),
                              new UnitDef("corarad", "Advanced Radar Tower"),
                              new UnitDef("corshroud", "Shroud"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 5, "Arm", "Advanced Defenses",
                     new List<UnitDef>{
                              new UnitDef("armanni", "Annihilator"),
                              new UnitDef("armamd", "Protector"),
                              new UnitDef("mercury", "Mercury"),
                              new UnitDef("armgate", "Keeper"),
                              new UnitDef("armemp", "Detonator"),
                     } ),

                  new UpgradeDef("Buildings", "Defense", 5, "Core", "Advanced Defense",
                     new List<UnitDef>{
                              new UnitDef("cordoom", "Doomsday Machine"),
                              new UnitDef("corfmd", "Fortitude"),
                              new UnitDef("screamer", "Screamer"),
                              new UnitDef("corgate", "Overseer"),
                              new UnitDef("cortron", "Catalyst"),
                     } ),

                  new UpgradeDef("Buildings", "Apocalyptic", 6, "Arm", "Nuclear Silo or Big Bertha",
                     new List<UnitDef>{
                              new UnitDef("armsilo", "Retaliator"),
                              new UnitDef("armbrtha", "Big Bertha"),
                     } ),

                  new UpgradeDef("Buildings", "Apocalyptic", 6, "Core", "Nuclear Silo or Intimidator",
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
                              new UnitDef("armmakr", "Metal Maker"),
                              new UnitDef("armuwmex", "Underwater Metal Extractor"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 1, "Core", "Basic mex/energy",
                     new List<UnitDef>{
                              new UnitDef("cormex", "Metal Extractor"),
                              new UnitDef("corsolar", "Solar Collector"),
                              new UnitDef("corwin", "Wind Generator"),
                              new UnitDef("armtide", "Tidal Generator"),
                              new UnitDef("armmakr", "Metal Maker"),
                              new UnitDef("coruwmex", "Underwater Metal Extractor"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 2, "Arm", "Twilight Nanotower Air Pad",
                     new List<UnitDef>{
                              new UnitDef("armamex", "Twilight"),
                              new UnitDef("armnanotc", "Nano Turret"),
                              new UnitDef("armasp", "Air Repair Pad"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 2, "Core", "Exploiter Nanotower Air Pad",
                     new List<UnitDef>{
                              new UnitDef("corexp", "Exploiter"),
                              new UnitDef("cornanotc", "Nano Turret"),
                              new UnitDef("corasp", "Air Repair Pad"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 3, "Arm", "T1 Lab or Adv. Solar",
                     new List<UnitDef>{
                              new UnitDef("armlab", "Kbot Lab"),
                              new UnitDef("armvp", "Vehicle Plant"),
                              new UnitDef("armsy", "Shipyard"),
                              new UnitDef("armap", "Aircraft Plant"),
                              new UnitDef("coradvsol", "Advanced Solar Collector"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 3, "Core", "T1 Lab or Adv. Solar",
                     new List<UnitDef>{
                              new UnitDef("corlab", "Kbot Lab"),
                              new UnitDef("corvp", "Vehicle Plant"),
                              new UnitDef("corsy", "Shipyard"),
                              new UnitDef("corap", "Aircraft Plant"),
                              new UnitDef("coradvsol", "Advanced Solar Collector"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 4, "Arm", "T2 Labs",
                     new List<UnitDef>{
                              new UnitDef("armalab", "Advanced Kbot Lab"),
                              new UnitDef("armavp", "Advanced Vehicle Plant"),
                              new UnitDef("armfhp", "Floating Hovercraft Platform"),
                              new UnitDef("armaap", "Advanced Aircraft Plant"),
                              new UnitDef("armhp", "Hovercraft Platform"),
                              new UnitDef("armasy", "Advanced Shipyard"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 4, "Core", "T2 Lab",
                     new List<UnitDef>{
                              new UnitDef("coralab", "Advanced Kbot Lab"),
                              new UnitDef("coravp", "Advanced Vehicle Plant"),
                              new UnitDef("corfhp", "Floating Hovercraft Platform"),
                              new UnitDef("coraap", "Advanced Aircraft Plant"),
                              new UnitDef("corhp", "Hovercraft Platform"),
                              new UnitDef("corasy", "Advanced Shipyard"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 5, "Arm", "Mohos, Fusion",
                     new List<UnitDef>{
                              new UnitDef("armfus", "Fusion Reactor"),
                              new UnitDef("armmoho", "Moho Mine"),
                              new UnitDef("armuwmme", "Underwater Moho Mine"),
                              new UnitDef("amgeo", "Moho Geothermal Powerplant"),
                              new UnitDef("armmmkr", "Moho Metal Maker"),
                              new UnitDef("armuwmmm", "Underwater Moho Metal Maker"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 5, "Arm", "Mohos, Fusion",
                     new List<UnitDef>{
                              new UnitDef("corfus", "Fusion Reactor"),
                              new UnitDef("cormoho", "Moho Mine"),
                              new UnitDef("coruwmme", "Underwater Moho Mine"),
                              new UnitDef("cormexp", "Moho Exploiter"),
                              new UnitDef("cmgeo", "Moho Geothermal Powerplant"),
                              new UnitDef("cormmkr", "Moho Metal Maker"),
                              new UnitDef("coruwmme", "Underwater Moho Mine"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 6, "Arm", "Advanced Fusion",
                     new List<UnitDef>{
                              new UnitDef("aafus", "Advanced Fusion Reactor"),
                     } ),

                  new UpgradeDef("Buildings", "Economy", 6, "Core", "Advanced Fusion",
                     new List<UnitDef>{
                              new UnitDef("cafus", "Advanced Fusion Reactor"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 2, "Arm", "A ship/hover under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("armsh", "Skimmer"),
                              new UnitDef("armpt", "Skeeter"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 2, "Core", "A ship/hover under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("corsh", "Scrubber"),
                              new UnitDef("corpt", "Searcher"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 3, "Arm", "A ship/hover under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("armch", "Construction Hovercraft"),
                              new UnitDef("armanac", "Anaconda"),
                              new UnitDef("armah", "Swatter"),
                              new UnitDef("armmh", "Wombat"),
                              new UnitDef("armcs", "Construction Ship"),
                              new UnitDef("armmls", "Valiant"),
                              new UnitDef("armsjam", "Escort"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 3, "Core", "A ship/hover under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("corch", "Construction Hovercraft"),
                              new UnitDef("corsnap", "Snapper"),
                              new UnitDef("corah", "Slinger"),
                              new UnitDef("cormh", "Nixer"),
                              new UnitDef("corcs", "Construction Ship"),
                              new UnitDef("cormls", "Pathfinder"),
                              new UnitDef("corsjam", "Phantom"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 4, "Arm", "A ship/hover under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("decade", "Decade"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 4, "Core", "A ship/hover under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("coresupp", "Supporter"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 5, "Arm", "A ship/hover under 1200 in cost",
                     new List<UnitDef>{
                              new UnitDef("armthovr", "Bear"),
                              new UnitDef("armsub", "Lurker"),
                              new UnitDef("armroy", "Crusader"),
                              new UnitDef("armtship", "Hulk"),
                              new UnitDef("armacsub", "Advanced Construction Sub"),
                              new UnitDef("armrecl", "Grim Reaper"),
                              new UnitDef("armaas", "Archer"),
                     } ),

                  new UpgradeDef("Units", "Ships/Hovers", 5, "Core", "A ship/hover under 1200 in cost",
                     new List<UnitDef>{
                              new UnitDef("corthovr", "Turtle"),
                              new UnitDef("nsaclash", "Halberd"),
                              new UnitDef("corsub", "Snake"),
                              new UnitDef("corroy", "Enforcer"),
                              new UnitDef("cortship", "Envoy"),
                              new UnitDef("coracsub", "Advanced Construction Sub"),
                              new UnitDef("correcl", "Death Cavalry"),
                              new UnitDef("corshark", "Shark"),
                              new UnitDef("corarch", "Shredder"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 1, "Arm", "A vehicle under 75 in cost",
                     new List<UnitDef>{
                              new UnitDef("armmlv", "Podger"),
                              new UnitDef("armfav", "Jeffy"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 1, "Core", "A vehicle under 75 in cost",
                     new List<UnitDef>{
                              new UnitDef("corfav", "Weasel"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 2, "Arm", "A vehicle under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("armflash", "Flash"),
                              new UnitDef("armseer", "Seer"),
                              new UnitDef("armjam", "Jammer"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 2, "Core", "A vehicle under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("cormlv", "Spoiler"),
                              new UnitDef("corgator", "Instigator"),
                              new UnitDef("coreter", "Deleter"),
                              new UnitDef("corvrad", "Informer"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 3, "Arm", "A vehicle under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("armcv", "Construction Vehicle"),
                              new UnitDef("armbeaver", "Beaver"),
                              new UnitDef("armpincer", "Pincer"),
                              new UnitDef("armstump", "Stumpy"),
                              new UnitDef("tawf013", "Shellshocker"),
                              new UnitDef("armjanus", "Janus"),
                              new UnitDef("armsam", "Samson"),
                              new UnitDef("consul", "Consul"),
                              new UnitDef("armst", "Gremlin"),
                              new UnitDef("armmart", "Luger"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 3, "Core", "A vehicle under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("corcv", "Construction Vehicle"),
                              new UnitDef("cormuskrat", "Muskrat"),
                              new UnitDef("corgarp", "Garpike"),
                              new UnitDef("corraid", "Raider"),
                              new UnitDef("corlevlr", "Leveler"),
                              new UnitDef("corwolv", "Wolverine"),
                              new UnitDef("cormist", "Slasher"),
                              new UnitDef("cormart", "Pillager"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 4, "Arm", "A vehicle under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("armacv", "Advanced Construction Vehicle"),
                              new UnitDef("armlatnk", "Panther"),
                              new UnitDef("armyork", "Phalanx"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 4, "Core", "A vehicle under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("coracv", "Advanced Construction Vehicle"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 5, "Arm", "A vehicle under 1200 in cost",
                     new List<UnitDef>{
                              new UnitDef("armcroc", "Triton"),
                              new UnitDef("armbull", "Bulldog"),
                              new UnitDef("armmerl", "Merl"),
                     } ),

                  new UpgradeDef("Units", "Vehicles", 5, "Core", "A vehicle under 1200 in cost",
                     new List<UnitDef>{
                              new UnitDef("corseal", "Croc"),
                              new UnitDef("correap", "Reaper"),
                              new UnitDef("corvroc", "Diplomat"),
                              new UnitDef("corsent", "Copperhead"),
                     } ),

                  new UpgradeDef("Units", "Bots", 1, "Arm", "Peewee Fleas",
                     new List<UnitDef>{
                              new UnitDef("armpw", "Peewee"),
                              new UnitDef("armflea", "Flea"),
                     } ),

                  new UpgradeDef("Units", "Bots", 1, "Core", "A.K.",
                     new List<UnitDef>{
                              new UnitDef("corak", "A.K."),
                     } ),

                  new UpgradeDef("Units", "Bots", 2, "Arm", "A bot under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("armck", "Construction Kbot"),
                              new UnitDef("armpw", "Peewee"),
                              new UnitDef("armrectr", "Rector"),
                              new UnitDef("armrock", "Rocko"),
                              new UnitDef("armham", "Hammer"),
                              new UnitDef("armjeth", "Jethro"),
                              new UnitDef("armflea", "Flea"),
                              new UnitDef("armaser", "Eraser"),
                              new UnitDef("armmark", "Marky"),
                     } ),

                  new UpgradeDef("Units", "Bots", 2, "Core", "A bot under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("corck", "Construction Kbot"),
                              new UnitDef("corak", "A.K."),
                              new UnitDef("cornecro", "Necro"),
                              new UnitDef("corstorm", "Storm"),
                              new UnitDef("corthud", "Thud"),
                              new UnitDef("corcrash", "Crasher"),
                              new UnitDef("corvoyr", "Voyeur"),
                              new UnitDef("corspec", "Spectre"),
                     } ),

                  new UpgradeDef("Units", "Bots", 3, "Arm", "A bot under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("armwar", "Warrior"),
                              new UnitDef("armfark", "Fark"),
                              new UnitDef("armfast", "Zipper"),
                              new UnitDef("armamph", "Pelican"),
                              new UnitDef("armfido", "Fido"),
                              new UnitDef("armspid", "Spider"),
                              new UnitDef("armvader", "Invader"),
                              new UnitDef("armspy", "Infiltrator"),
                     } ),

                  new UpgradeDef("Units", "Bots", 3, "Core", "A bot under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("corfast", "Freaker"),
                              new UnitDef("corpyro", "Pyro"),
                              new UnitDef("corroach", "Roach"),
                              new UnitDef("corspy", "Parasite"),
                     } ),

                  new UpgradeDef("Units", "Bots", 4, "Arm", "A bot under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("armack", "Advanced Construction Kbot"),
                              new UnitDef("armzeus", "Zeus"),
                              new UnitDef("armsptk", "Recluse"),
                              new UnitDef("armaak", "Archangel"),
                     } ),

                  new UpgradeDef("Units", "Bots", 4, "Core", "A bot under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("corack", "Advanced Construction Kbot"),
                              new UnitDef("coramph", "Gimp"),
                              new UnitDef("cormort", "Morty"),
                     } ),

                  new UpgradeDef("Units", "Bots", 5, "Arm", "A bot under 1200 in cost",
                     new List<UnitDef>{
                              new UnitDef("armmav", "Maverick"),
                              new UnitDef("armsnipe", "Sharpshooter"),
                              new UnitDef("armdecom", "Commander"),
                     } ),

                  new UpgradeDef("Units", "Bots", 5, "Core", "A bot under 1200 in cost",
                     new List<UnitDef>{
                              new UnitDef("corcan", "Can"),
                              new UnitDef("cortermite", "Termite"),
                              new UnitDef("corhrk", "Dominator"),
                              new UnitDef("coraak", "Manticore"),
                              new UnitDef("corsktl", "Skuttle"),
                              new UnitDef("cordecom", "Commander"),
                     } ),

                  new UpgradeDef("Units", "Air", 1, "Arm", "A plane under 75 in cost",
                     new List<UnitDef>{
                              new UnitDef("armpeep", "Peeper"),
                     } ),

                  new UpgradeDef("Units", "Air", 1, "Core", "A plane under 75 in cost",
                     new List<UnitDef>{
                              new UnitDef("corfink", "Fink"),
                              new UnitDef("bladew", "Bladewing"),
                     } ),

                  new UpgradeDef("Units", "Air", 2, "Arm", "A plane under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("armfig", "Freedom Fighter"),
                              new UnitDef("armatlas", "Atlas"),
                     } ),

                  new UpgradeDef("Units", "Air", 2, "Core", "A plane under 150 in cost",
                     new List<UnitDef>{
                              new UnitDef("corveng", "Avenger"),
                              new UnitDef("corvalk", "Valkyrie"),
                     } ),

                  new UpgradeDef("Units", "Air", 3, "Arm", "A plane under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("armca", "Construction Aircraft"),
                              new UnitDef("armthund", "Thunder"),
                              new UnitDef("armkam", "Banshee"),
                              new UnitDef("armhawk", "Hawk"),
                              new UnitDef("armawac", "Eagle"),
                     } ),

                  new UpgradeDef("Units", "Air", 3, "Core", "A plane under 350 in cost",
                     new List<UnitDef>{
                              new UnitDef("corca", "Construction Aircraft"),
                              new UnitDef("corshad", "Shadow"),
                              new UnitDef("corvamp", "Vamp"),
                              new UnitDef("corawac", "Vulture"),
                     } ),

                  new UpgradeDef("Units", "Air", 4, "Arm", "A plane under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("armaca", "Advanced Construction Aircraft"),
                              new UnitDef("armbrawl", "Brawler"),
                              new UnitDef("armpnix", "Phoenix"),
                              new UnitDef("armlance", "Lancet"),
                              new UnitDef("armdfly", "Dragonfly"),
                              new UnitDef("corgripn", "Stiletto"),
                     } ),

                  new UpgradeDef("Units", "Air", 4, "Core", "A plane under 600 in cost",
                     new List<UnitDef>{
                              new UnitDef("coraca", "Advanced Construction Aircraft"),
                              new UnitDef("corape", "Rapier"),
                              new UnitDef("corhurc", "Hurricane"),
                              new UnitDef("cortitan", "Titan"),
                              new UnitDef("armsl", "Seahook"),
                     } ),

                  new UpgradeDef("Units", "Air", 5, "Arm", "A plane under 2000 in cost",
                     new List<UnitDef>{
                              new UnitDef("blade", "Blade"),
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
