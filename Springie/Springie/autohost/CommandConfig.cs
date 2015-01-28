#region using

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using LobbyClient;

#endregion

namespace Springie.autohost
{
    public class CommandConfig
    {
        SayPlace[] listenTo = new[] { SayPlace.Battle, SayPlace.User, SayPlace.Game };

        [Category("Texts")]
        [Description("Help text to be displayed in !help listings.")]
        public string HelpText { get; set; }

        [XmlIgnore]
        public DateTime lastCall = DateTime.Now;

        [Category("Command")]
        [Description("Rights level. If user's rights level is higher or equal to rights level of command - user has rights to use this command.")]
        public int Level { get; set; }

        [Category("Command")]
        [Description("From which places can you use this command. Normal = PM to server, Battle = battle lobby, Game = from running game.")]
        public SayPlace[] ListenTo { get { return listenTo; } set { listenTo = value; } }

        public bool AllowSpecs { get; set; }


        [ReadOnly(true)]
        [Category("Command")]
        public string Name { get; set; }

        [Category("Command")]
        [Description("How often can this command be executed (in seconds). 0 = no throttling, can execute at any time.")]
        public int Throttling { get; set; }

        public CommandConfig() { 
       
        }


        public CommandConfig(string name, int level, string helpText, int throttling, SayPlace[] listenTo)
            : this(name, level, helpText, throttling)
        {
            this.listenTo = listenTo;
        }

        public CommandConfig(string name, int level, string helpText, int throttling)
            : this(name, level, helpText)
        {
            Throttling = throttling;
        }

        public CommandConfig(string name, int level, string helpText)
        {
            Name = name;
            Level = level;
            HelpText = helpText;
            AllowSpecs = true;
        }
    } ;
}