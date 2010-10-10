#region using

using System.ComponentModel;

#endregion

namespace Springie.autohost
{
    public class PrivilegedUser
    {
        [Category("User")]
        [Description("Rights level. If rights level is higher or equal to rights level of command - user has rights to use that command.")]
        public int Level { get; set; }

        [Category("User")]
        [Description("Nickname used in spring lobby")]
        public string Name { get; set; }

        public PrivilegedUser() {}

        public PrivilegedUser(string name, int level)
        {
            Name = name;
            Level = level;
        }
    } ;
}