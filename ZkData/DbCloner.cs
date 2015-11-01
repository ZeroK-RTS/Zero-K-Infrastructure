using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ZkData
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
            List<string> tables = new List<string>();
            using (var con = new SqlConnection(targetConnectionString)) {
                con.Open();
                using (var com = new SqlCommand("EXEC sp_msdependencies @intrans = 1 ,@objtype=3", con)) {
                    using (var reader = com.ExecuteReader()) {
                        while (reader.Read()) {
                            var name = reader.GetString(1);
                            tables.Add(name);
                            //if (!name.StartsWith("__")) yield return name;
                        }
                    }
                }
            }

            List<Tuple<string,string>> fromTo = new List<Tuple<string, string>>();

            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                using (
                    var com =
                        new SqlCommand(
                            "select o1.Name as from_table,  o2.Name as to_table from sys.foreign_keys fk inner join sys.objects o1 on fk.parent_object_id = o1.object_id inner join sys.schemas s1 on o1.schema_id = s1.schema_id inner join sys.objects o2 on fk.referenced_object_id = o2.object_id inner join sys.schemas s2 on o2.schema_id = s2.schema_id where not(s1.name = s2.name and o1.name = o2.name)",
                            con))
                {
                    using (var reader = com.ExecuteReader())
                    {
                        while (reader.Read()) fromTo.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                    }

                }
            }

            tables.Reverse();
            List<string> final = new List<string>();
            bool del;
            do
            {
                del = false;
                foreach (var t in tables.ToList())
                {
                    if (!fromTo.Any(y => y.Item2 == t))
                    {
                        del = true;
                        final.Add(t);
                        fromTo.RemoveAll(x => x.Item1 == t);
                        tables.Remove(t);
                    }
                }
            } while (del);
            final.AddRange(tables);


            return final;
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
                            ret.Add("["+reader.GetName(i)+"]");
                        }
                    }
                }
            }
            return ret;
        }

        public event Action<string> LogEvent = s => {};

        void Log(string format, params object[] args) {
            LogEvent?.Invoke(string.Format(format +"\n", args));
        }

        public void CloneAllTables()
        {
            var tables = GetOrderedTables().ToList();

            foreach (var t in tables)
            {
                Log("Disabling checks: " + t);
                DisableChecks(t);
            }
               
            foreach (var table in tables) {
                
                try
                {
                    Log("Deleting: " + table);
                    DeleteFromTable(table);
                }
                catch (Exception ex)
                {
                    Log("Error cloning {0}: {1}", table, ex);
                }
            }

            tables.Reverse();

            foreach (var table in tables) {
                try {
                    Log("Cloning: " + table);
                    CloneTable(table);
                } catch (Exception ex) {
                    Log("Error cloning {0}: {1}", table, ex);
                }
            }

            foreach (var t in tables)
            {
                Log("Enabling checks: " + t);
                EnableChecks(t);
            }
        }

        public void DeleteFromTable(string tableName)
        {
            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                var sql = string.Format("DELETE FROM [{1}].dbo.{0};", tableName, targetDbName);
                using (var com = new SqlCommand(sql, con) { CommandTimeout = 3600 })
                {
                    com.ExecuteNonQuery();
                }
            }
        }

        void DisableChecks(string tableName) {
            var sb = new StringBuilder();
            sb.AppendFormat("ALTER TABLE [{0}].dbo.{1} NOCHECK CONSTRAINT ALL;\n", targetDbName, tableName);
            sb.AppendFormat("IF OBJECTPROPERTY(OBJECT_ID('[{1}].dbo.{0}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT [{1}].[dbo].[{0}] ON;\n", tableName, targetDbName);

            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                using (var com = new SqlCommand(sb.ToString(), con) { CommandTimeout = 3600 })
                {
                    com.ExecuteNonQuery();
                }
            }
        }

        void EnableChecks(string tableName) {
            var sb = new StringBuilder();
            sb.AppendFormat("IF OBJECTPROPERTY(OBJECT_ID('[{1}].dbo.{0}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT [{1}].[dbo].[{0}] OFF;\n", tableName, targetDbName);
            sb.AppendFormat("ALTER TABLE [{0}].dbo.{1} CHECK CONSTRAINT ALL;\n", targetDbName, tableName);
            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                using (var com = new SqlCommand(sb.ToString(), con) { CommandTimeout = 3600 })
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
            sb.AppendFormat("IF OBJECTPROPERTY(OBJECT_ID('[{1}].dbo.{0}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT [{1}].[dbo].[{0}] OFF;\n", tableName, targetDbName);
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
