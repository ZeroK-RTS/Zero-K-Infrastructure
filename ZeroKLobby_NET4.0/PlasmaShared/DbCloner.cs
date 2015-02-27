using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PlasmaShared
{
    public class DbCloner
    {
        string sourceDbName;
        string targetDbName;
        string targetConnectionString;

        public DbCloner(string sourceDbName, string targetDbName, string targetConnectionString)
        {
            this.targetDbName = targetDbName;
            this.sourceDbName = sourceDbName;
            this.targetConnectionString = targetConnectionString;
        }

        public IEnumerable<string> GetOrderedTables()
        {
            using (var con = new SqlConnection(targetConnectionString)) {
                con.Open();
                using (var com = new SqlCommand("EXEC sp_msdependencies @intrans = 1 ,@objtype=3", con)) {
                    using (var reader = com.ExecuteReader()) {
                        while (reader.Read()) {
                            var name = reader.GetString(1);
                            if (!name.StartsWith("__")) yield return name;
                        }
                    }
                }
            }
        }

        public List<string> GetTableColumns(string name)
        {
            var ret = new List<string>();
            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                using (var com = new SqlCommand(string.Format("SELECT TOP 1 * FROM {0}", name), con))
                {
                    using (var reader = com.ExecuteReader())
                    {
                        for (int i = 0; i < reader.FieldCount; i++) {
                            ret.Add(reader.GetName(i));
                        }
                    }
                }
            }
            return ret;
        }


        public void CloneAllTables()
        {
            var tables = GetOrderedTables().ToList();

            for (int i = tables.Count - 1; i >= 0; i--) {
                
                try
                {
                    DeleteFromTable(tables[i]);
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error cloning {0}: {1}", tables[i], ex);
                }
            }
            
            foreach (var table in tables) {
                try {
                    CloneTable(table);
                } catch (Exception ex) {
                    Trace.TraceError("Error cloning {0}: {1}", table, ex);
                }
            }
        }

        public void DeleteFromTable(string tableName)
        {
            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                using (var com = new SqlCommand(string.Format("DELETE FROM [{1}].dbo.{0};", tableName,targetDbName), con))
                {
                    com.ExecuteNonQuery();
                }
            }
        }

        public void CloneTable(string tableName)
        {
            var colNames = string.Join(",", GetTableColumns(tableName));

            var sb = new StringBuilder();
            sb.AppendFormat("ALTER TABLE [{0}].dbo.{1} NOCHECK CONSTRAINT ALL;\n", targetDbName, tableName);
            sb.AppendFormat("IF OBJECTPROPERTY(OBJECT_ID('[{1}].dbo.{0}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT [{1}].[dbo].[{0}] ON;\n", tableName, targetDbName);
            sb.AppendFormat("INSERT INTO [{3}].dbo.{0} ({1}) select {1} from [{2}].dbo.{0};\n", tableName, colNames, sourceDbName, targetDbName);
            sb.AppendFormat("IF OBJECTPROPERTY(OBJECT_ID('[{1}].dbo.{0}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT [{1}].[dbo].[{0}] ON;\n", tableName, targetDbName);
            sb.AppendFormat("ALTER TABLE [{0}].dbo.{1} CHECK CONSTRAINT ALL;\n", targetDbName, tableName);
            
            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                using (var com = new SqlCommand(sb.ToString(), con) {CommandTimeout = 3600}) {
                    com.ExecuteNonQuery();
                }
            }

        }

    }
}
