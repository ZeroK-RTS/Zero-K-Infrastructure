using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace EntityFramework.Extensions
{
    /// <summary>
    /// EF6's batch operations, over EF Core's.
    ///
    /// `EntityFramework.Extensions` gave EF6 `DbSet&lt;T&gt;.Delete()` and
    /// `IQueryable&lt;T&gt;.Update(x =&gt; new T { ... })`: one SQL statement against every row
    /// the query matches, without loading anything. EF Core 7 grew the same capability under
    /// different names - `ExecuteDelete` and `ExecuteUpdate` - so this is a translation
    /// rather than a reimplementation, and the database does the same work either way.
    ///
    /// **This is an implementation, not a shim, and the difference matters here.** An earlier
    /// attempt supplied `EntityFramework.Extensions` as an empty namespace because its using
    /// looked dead; the controller compiled and would have thrown on an admin page that
    /// deletes planets and treaties. Nothing about "it compiles" distinguishes the two, so
    /// this one is exercised against a real database by ZkData.Core's write harness.
    ///
    /// `Update` is the part with actual work in it. EF6's form is a member initialiser -
    /// `x =&gt; new Account { PwMetalUsed = 0, FactionID = null }` - and EF Core wants a chain
    /// of `SetProperty` calls. The initialiser is taken apart and rebuilt as that chain, which
    /// is why arbitrary expressions work and not only constants: whatever EF6 allowed on the
    /// right-hand side, EF Core receives unchanged and translates itself.
    /// </summary>
    public static class BatchOperations
    {
        public static int Delete<T>(this IQueryable<T> source) where T : class
            => source.ExecuteDelete();

        public static int Update<T>(this IQueryable<T> source, Expression<Func<T, T>> updateFactory)
            where T : class
        {
            if (!(updateFactory.Body is MemberInitExpression init))
                throw new NotSupportedException(
                    "Update expects a member initialiser, as EF6's version did: x => new T { A = 1 }. Got "
                    + updateFactory.Body.NodeType + ".");

            if (init.NewExpression.Arguments.Count != 0)
                throw new NotSupportedException(
                    "Update expects a parameterless constructor in the initialiser; EF Core has nowhere "
                    + "to put constructor arguments.");

            var entity = updateFactory.Parameters[0];
            var calls = Expression.Parameter(typeof(SetPropertyCalls<T>), "setters");
            Expression chain = calls;

            foreach (var binding in init.Bindings)
            {
                if (!(binding is MemberAssignment assignment))
                    throw new NotSupportedException(
                        "Update supports property assignments only; " + binding.Member.Name
                        + " is a " + binding.BindingType + " binding.");

                var property = Expression.Lambda(
                    Expression.MakeMemberAccess(entity, assignment.Member), entity);
                var value = Expression.Lambda(assignment.Expression, entity);

                // SetProperty<TProperty>(Func<T,TProperty> property, Func<T,TProperty> value).
                //
                // Func, not Expression<Func> - and that is not a detail. These lambdas live
                // INSIDE the expression tree EF Core parses, so the parameters are declared as
                // delegates and the arguments are LambdaExpressions passed straight through,
                // exactly as the C# compiler emits for setters.SetProperty(x => x.A, x => 1).
                // Matching Expression<> here finds no overload at all.
                //
                // The sibling overload takes a bare TProperty value; this one is chosen because
                // it accepts any expression, which is what EF6's member initialiser allowed.
                var setProperty = typeof(SetPropertyCalls<T>).GetMethods()
                    .Single(m => m.Name == nameof(SetPropertyCalls<T>.SetProperty)
                                 && m.GetParameters().Length == 2
                                 && m.GetParameters()[1].ParameterType.IsGenericType
                                 && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                    .MakeGenericMethod(property.ReturnType);

                chain = Expression.Call(chain, setProperty, property, value);
            }

            if (chain == calls) return 0;   // nothing assigned; EF Core rejects an empty chain

            var setters = Expression.Lambda<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>>(chain, calls);
            return source.ExecuteUpdate(setters);
        }
    }
}
