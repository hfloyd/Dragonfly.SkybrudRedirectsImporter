namespace Dragonfly.SkybrudRedirectsImporter.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class ImportData
    {
        public IEnumerable<ImportDataItem> Items { get; set; }

        public ImportData()
        {
            Items = new List<ImportDataItem>();
        }
    }

    public class ImportDataItem
    {
        public string OldUrl { get; set; }
        public string NewUrl { get; set; }
    }

  
}
