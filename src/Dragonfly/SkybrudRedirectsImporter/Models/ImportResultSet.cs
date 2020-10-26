namespace Dragonfly.SkybrudRedirectsImporter.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Mvc;
    using Dragonfly.SkybrudRedirectsImporter.Utilities;
    using Skybrud.Umbraco.Redirects.Models;


    public class ImportResultSet
    {
        public FormInputsImport FormInputs { get; set; }
        public List<RedirectItem> SuccessItems { get; set; }
        public List<ImportErrorItem> ErrorItems { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }

        public ImportResultSet()
        {
            SuccessItems = new List<RedirectItem>();
            ErrorItems = new List<ImportErrorItem>();
        }
    }

    //public class ImportSuccessItem
    //{
    //    public string OldUrl { get; set; }
    //    public int NewNodeId { get; set; }

    //    public Constants.NodeType UmbracoNodeType { get; set; }
    //}

    public class ImportErrorItem
    {
        public ImportErrorItem(ImportDataItem Item, string ErrorText)
        {
            this.ImportItem = Item;
            this.ErrorMessage = ErrorText;
        }

        public ImportDataItem ImportItem { get; set; }
        public string ErrorMessage { get; set; }
    }
}
