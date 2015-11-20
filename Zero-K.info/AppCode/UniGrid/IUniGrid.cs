using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;

namespace ZeroKWeb
{
    public interface IUniGrid
    {
        IEnumerable<IUniGridCol> BaseCols { get; }
        string ID { get; }
        string OrderColumn { get; }
        bool OrderIsDescending { get; }
        int PageCount { get; }
        int PageNumber { get; set; }
        int PageSize { get; }
        int RecordCount { get; }
        string Title { get; }
        bool CsvRequested { get; }
        void PrepareRenderData();
        bool RenderHeaders { get; }
        bool AllowCsvExport { get; }
        MvcHtmlString RenderTableRows();
        void RenderCsv(Encoding encoding = null, string delimiter = ";");
        List<string> SelectedKeys { get; } 
    }
}