using System;
using Microsoft.EntityFrameworkCore;

namespace System.Data.Entity.SqlServer
{
    /// <summary>
    /// EF6's <c>SqlFunctions</c>, for the one member the website actually calls.
    ///
    /// This is deliberately NOT the dead-using stub that the same namespace already carries
    /// in ZeroKWeb.Core - that one exists because <c>UsersController</c> names the namespace
    /// and then never uses anything from it. <c>ClansController</c> is the opposite case: it
    /// calls <c>PatIndex</c> four times, to find a clan whose name or shortcut collides with
    /// the one being created. An empty stub would compile and then throw, or worse, return
    /// null and let every clan name through - which is exactly the trap recorded for
    /// EntityFramework.Extensions in DeadControllerUsings2.cs.
    ///
    /// So this maps to the same T-SQL EF6 emitted. EF6 translated SqlFunctions.PatIndex to
    /// the built-in PATINDEX; <see cref="Register"/> tells EF Core to do the same, and
    /// <c>IsBuiltIn</c> is what keeps it from being emitted as <c>[dbo].[PATINDEX]</c>.
    /// Identical SQL is the whole claim here: any collation, wildcard or NULL behaviour is
    /// then the database's, and therefore unchanged.
    ///
    /// Calling it outside a query throws, as EF6's did - the body never runs on either stack.
    /// </summary>
    public static class SqlFunctions
    {
        public static int? PatIndex(string stringPattern, string target)
            => throw new NotSupportedException(
                "SqlFunctions.PatIndex can only be used inside a LINQ to Entities query.");

        /// <summary>Called from ZkDataContext.OnModelCreating.</summary>
        public static void Register(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDbFunction(typeof(SqlFunctions).GetMethod(nameof(PatIndex), new[] { typeof(string), typeof(string) }))
                .HasName("PATINDEX")
                .IsBuiltIn();
        }
    }
}
