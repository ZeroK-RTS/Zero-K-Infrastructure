using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace ZeroKWeb
{
    public class UniGrid<T> : IUniGrid
    {
        public List<UniGridCol<T>> Cols = new List<UniGridCol<T>>();

        public Func<T, object> ItemFormat;
        private int _pageNumber;

        public List<string> SelectedKeys { get; set; }


        public UniGrid(IQueryable<T> data, string title = null, string id = null) : this(id)
        {
            InputData = data;
            if (title != null) Title = title;
        }


        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="id">id of the grid (optional, "gr" used otherwise)</param>
        public UniGrid(string id = null)
        {
            SelectedKeys = new List<string>();
            ID = string.IsNullOrEmpty(id) ? "gr" : id;
            PageSize = 30;

            HttpRequest r = HttpContext.Current.Request;
            int.TryParse(r[ID + "page"], out _pageNumber);
            OrderColumn = r[ID + "order"];
            OrderIsDescending = r[ID + "desc"] == "True";
            CsvRequested = r[ID + "csv"] == "True";
            var selections = r.Form.GetValues(ID + "sel");
            if (selections != null)
            {
                foreach (var val in selections)
                {
                    SelectedKeys.Add(val);
                }
            }
        }

        public IQueryable<T> InputData { get; set; }
        public IQueryable<T> RenderData { get; private set; }

        #region IUniGridModel Members

        public int PageSize { get; set; }

        public string Title { get; set; }

        public string CsvFileName { get; set; }

        public UniGridCol<T> AddCol(string description = null, Func<T, object> commonFormat = null) {
            var c = new UniGridCol<T>(description, commonFormat);
            Cols.Add(c);
            return c;
        }

        public string GenerateCsv(string delimiter = ";")
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(delimiter, Cols.Where(x => x.AllowCsv).Select(x => EscapeCsvField(x.Description)))); // header

            // row data
            foreach (T row in RenderData)
            {
                sb.AppendLine(string.Join(delimiter,
                                          Cols.Where(x => x.AllowCsv).Select(x =>
                                          {
                                              string str = String.Empty;
                                              object rendered = x.CsvFormat(row);
                                              if (rendered != null) str = rendered.ToString();
                                              return EscapeCsvField(str);
                                          })));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generate CSV and send to browser
        /// </summary>
        /// <param name="encoding">warning default encoding windows-1250</param>
        /// <param name="delimiter"></param>
        public void RenderCsv(Encoding encoding = null, string delimiter = ";")
        {
            if (encoding == null) encoding = Encoding.GetEncoding("windows-1250");
            var csv = GenerateCsv(delimiter);

            HttpResponse response = HttpContext.Current.Response;
            response.Clear();
            response.ClearContent();
            response.ClearHeaders();
            response.ContentType = "text/csv";
            var name = CsvFileName;
            if (string.IsNullOrEmpty(name)) name = Title;
            if (string.IsNullOrEmpty(name)) name = "export";
            response.AddHeader("Content-Disposition", string.Format("attachment;filename={0}.csv", name));
            response.BinaryWrite(encoding.GetBytes(csv));
            response.End();
        }

        /// <summary>
        /// Call this if you are rendering table manually - prepares RenderData 
        /// </summary>
        public void PrepareRenderData()
        {
            // generate unique "id" for columns
            foreach (var c in Cols.Where(x => x.Description != null))
            {
                c.ID = new string(c.Description.Where(x => (x >= 'a' && x <= 'z') || (x >= 'A' && x <= 'Z')).Take(3).ToArray());
            }
            for (int i = 0; i < Cols.Count; i++)
            {
                UniGridCol<T> col = Cols[i];
                if (string.IsNullOrEmpty(col.ID) || Cols.Any(x => x != col && x.ID == col.ID)) col.ID += i + 1;
            }

            RenderData = InputData;
            RecordCount = RenderData.Count();

            if (!string.IsNullOrEmpty(OrderColumn))
            {
                UniGridCol<T> col = Cols.FirstOrDefault(x => x.ID == OrderColumn);
                if (col != null) RenderData = OrderBy(RenderData, col.SortExpression, !OrderIsDescending);
            }
        }

        public IEnumerable<IUniGridCol> BaseCols
        {
            get { return Cols; }
        }

        /// <summary>
        /// Render table rows with even and odd classes and selection column (if enabled)
        /// </summary>
        /// <returns></returns>
        public MvcHtmlString RenderTableRows()
        {
            var sb = new StringBuilder();
            int cnt = 0;
            foreach (T row in GetCurrentPageData())
            {
                cnt++;
                sb.AppendFormat("<tr class=\"{0}\">", cnt % 2 == 0 ? "even" : "odd");
                foreach (var columnDef in Cols.Where(x => x.AllowWeb))
                {
                    if (columnDef.KeySelector != null)
                    {
                        var selVal = columnDef.KeySelector(row).ToString();
                        bool isSelected = SelectedKeys.Contains(selVal);
                        sb.AppendFormat("<td><input type='checkbox' value='{2}' {1} class='js-grid-selector' onclick=\"gridSelect('{0}',$(this))\" />{3}</td>", ID, isSelected ? "checked='checked'" : "", selVal, columnDef.WebFormat(row));
                    }
                    else
                    {
                        sb.AppendFormat("<td>{0}</td>", columnDef.WebFormat(row));
                    }
                }
                sb.AppendLine("</tr>");
            }
            return new MvcHtmlString(sb.ToString());
        }

        public bool CsvRequested { get; set; }

        public string ID { get; set; }

        public int PageCount
        {
            get { return (int)Math.Ceiling(RecordCount / (double)PageSize); }
        }

        public int PageNumber
        {
            get
            {
                if (_pageNumber < 1) return 1;
                if (_pageNumber > PageCount) return Math.Max(1, PageCount);
                return _pageNumber;
            }
            set { _pageNumber = value; }
        }

        public string OrderColumn { get; set; }
        public bool OrderIsDescending { get; set; }


        public int RecordCount { get; private set; }

        #endregion

        private static string EscapeCsvField(string input)
        {
            return string.Format("\"{0}\"", input.Replace("\"", "\"\""));
        }

        public IQueryable<T> GetCurrentPageData()
        {
            return RenderData.Skip((PageNumber - 1) * PageSize).Take(PageSize);
        }

        public static IQueryable<TOrder> OrderBy<TOrder>(IQueryable<TOrder> source, LambdaExpression orderingExpression, bool isAscending = false)
        {
            if (orderingExpression == null) return source;

            MethodCallExpression resultExp = Expression.Call(typeof(Queryable),
                                                             isAscending ? "OrderBy" : "OrderByDescending",
                                                             new[] { orderingExpression.Parameters[0].Type, orderingExpression.ReturnType },
                                                             source.Expression,
                                                             Expression.Quote(orderingExpression));
            return source.Provider.CreateQuery<TOrder>(resultExp);
        }

        public MvcHtmlString RenderItems(Func<T, object> itemFormat = null)
        {
            if (itemFormat != null) ItemFormat = itemFormat;
            var sb = new StringBuilder();
            foreach (T r in GetCurrentPageData())
            {
                sb.AppendLine(ItemFormat(r).ToString());
            }
            return new MvcHtmlString(sb.ToString());
        }
    }
}