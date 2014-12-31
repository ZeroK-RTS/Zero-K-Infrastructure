using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using ServiceStack.Text;

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

        public List<string> GetOrderedTables()
        { 
            var tables = new List<string>();
            // get all tables

            using (var con = new SqlConnection(targetConnectionString)) {
                con.Open();
                using (var com = new SqlCommand(string.Format("SELECT * FROM information_schema.tables where table_catalog = '{0}'", targetDbName), con))
                {
                    using (var reader = com.ExecuteReader()) {
                        while (reader.Read()) {
                            var name = reader.GetString(2);
                            if (!name.StartsWith("__")) tables.Add(name);
                        }
                    }
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine(@"select    s1.name as from_schema
                        ,        o1.Name as from_table
                        ,        s2.name as to_schema
                        ,        o2.Name as to_table
                        from    sys.foreign_keys fk
                        inner    join sys.objects o1
                        on        fk.parent_object_id = o1.object_id
                        inner    join sys.schemas s1
                        on        o1.schema_id = s1.schema_id
                        inner    join sys.objects o2
                        on        fk.referenced_object_id = o2.object_id
                        inner    join sys.schemas s2
                        on        o2.schema_id = s2.schema_id
                        /*For the purposes of finding dependency hierarchy
                        we're not worried about self-referencing tables*/
                        where    not    (    s1.name = s2.name
                                        and    o1.name = o2.name)");

            var deps = new List<Dependency>();
            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                con.ChangeDatabase(targetDbName);
                using (var com = new SqlCommand(sb.ToString(), con))
                {
                    using (var reader = com.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            deps.Add(new Dependency() {From = reader.GetString(1), To = reader.GetString(3)});
                        }
                    }
                }
            }


            var final = new List<string>();
            while (tables.Count > 0) {
                foreach (var t in tables) {
                    if (!deps.Any(x => x.From == t)) {
                        deps.RemoveAll(x => x.To == t);
                        final.Add(t);
                        tables.Remove(t);
                        break;
                    }
                }
            }

            return final;
        }

        public class Dependency
        {
            public string From;
            public string To;
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
            sb.AppendFormat("IF OBJECTPROPERTY(OBJECT_ID('[{1}].dbo.{0}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT [{1}].[dbo].[{0}] ON;\n", tableName, targetDbName);
            sb.AppendFormat("INSERT INTO [{3}].dbo.{0} ({1}) select {1} from [{2}].dbo.{0};\n", tableName, colNames, sourceDbName, targetDbName);
            sb.AppendFormat("IF OBJECTPROPERTY(OBJECT_ID('[{1}].dbo.{0}'), 'TableHasIdentity') = 1 SET IDENTITY_INSERT [{1}].[dbo].[{0}] ON;\n", tableName, targetDbName);
            
            using (var con = new SqlConnection(targetConnectionString))
            {
                con.Open();
                using (var com = new SqlCommand(sb.ToString(), con)) {
                    com.ExecuteNonQuery();
                }
            }

        }

    }
}
