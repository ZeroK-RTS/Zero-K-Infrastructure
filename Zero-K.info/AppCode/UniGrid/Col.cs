using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web.Mvc;
using System.Web.WebPages;

namespace ZeroKWeb
{
    public class Col
    {
        /// <summary>
        /// Text description of the Grid column
        /// </summary>
        public Func<object,HelperResult> Description;
        /// <summary>
        /// Expression describing value grid has to be ordered by
        /// </summary>
        public LambdaExpression OrderByExpression;
        /// <summary>
        /// Expression for filtering
        /// </summary>
        public LambdaExpression FilterByExpression;
        /// <summary>
        /// SelectListItem enumeration for building select-filter
        /// </summary>
        public IEnumerable<SelectListItem> All;
        /// <summary>
        /// Index of the column
        /// </summary>
        public int Index;

        public ColType Type;

        public static Col Create<T,TOrderKey,TFilterKey>(
            int index,
            Func<object, HelperResult> description, 
            Expression<Func<T,TOrderKey>> orderbyexpression, 
            Expression<Func<T,TFilterKey>> filterbyexpression, 
            IEnumerable<SelectListItem> all = null,
            ColType type = ColType.TypeDriven)
        {
            var col = new Col(index, description, all, type);
            col.OrderByExpression = orderbyexpression;
            col.FilterByExpression = filterbyexpression;
            return col;
        }

        public Col(int index, Func<object, HelperResult> description, IEnumerable<SelectListItem> all, ColType type)
        {
            Index = index;
            Description = description;
            All = all;
            Type = type;
        }
    }

    public enum ColType
    {
        TypeDriven,
        Select
    }

    /// <summary>
    /// Filtering description if you filter only by string description of the db column
    /// </summary>
    [Obsolete("Not used with expressions filtering", true)]
    public struct Filtering
    {
        public string parameter;
        public string value;
    }

    public struct FilteringValue
    {
        public LambdaExpression expression;
        public string value;
    }
}