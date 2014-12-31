using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PlasmaShared
{
    public class DbCloner
    {
        string sourceConnectionString;
        string targetConnectionString;

        public DbCloner(string sourceConnectionString, string targetConnectionString)
        {
            this.sourceConnectionString = sourceConnectionString;
            this.targetConnectionString = targetConnectionString;
        }

    }
}
