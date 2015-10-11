using System;
using System.Linq.Expressions;

namespace ZeroKWeb
{
    public class UniGridCol<T>: IUniGridCol
    {
        public Func<T, object> CsvFormat;
        public Func<T, object> WebFormat;
        public bool IsSelector { get { return KeySelector != null; }
        }
        public string Width { get; set; }

        public UniGridCol(string description = null, Func<T, object> commonFormat = null) {
            Description = description;
            WebFormat = commonFormat;
            CsvFormat = commonFormat;
        }

        /// <summary>
        /// Expression describing value grid has to be ordered by
        /// </summary>
        public LambdaExpression SortExpression { get; private set; }
        public Func<T, object> KeySelector { get; private set; }

        #region IUniGridCol Members

        /// <summary>
        /// Text description of the Grid column
        /// </summary>
        public string Description { get; set; }

        public string ID { get; set; }
        public bool AllowsSort {
            get { return SortExpression != null; }
        }
        public bool AllowWeb {
            get { return WebFormat != null; }
        }
        public bool AllowCsv {
            get { return CsvFormat != null; }
        }

        #endregion

        public UniGridCol<T> SetWebFormat(Func<T, object> webformat) {
            WebFormat = webformat;
            return this;
        }


        public UniGridCol<T> SetCsvFormat(Func<T, object> csvformat) {
            CsvFormat = csvformat;
            return this;
        }

        public UniGridCol<T> SetWidth(string width) {
            Width = width;
            return this;
        }

        public UniGridCol<T> SetSort<TOrder>(Expression<Func<T, TOrder>> orderby) {
            SortExpression = orderby;
            return this;
        }

        /// <summary>
        /// Marks column as "row selector", selected values are sent in "IDsel" where grid ID is gr by default
        /// </summary>
        /// <param name="keySelector"></param>
        /// <returns></returns>
        public UniGridCol<T> SetRowSelection(Func<T,object> keySelector) {
            KeySelector = keySelector;
            return this;
        }


    }
}