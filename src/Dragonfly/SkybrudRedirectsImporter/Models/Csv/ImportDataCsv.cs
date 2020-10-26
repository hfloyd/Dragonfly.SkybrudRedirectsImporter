namespace Dragonfly.SkybrudRedirectsImporter.Models.Csv
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using LINQtoCSV;

    internal class ImportDataCsvLine
    {
        [CsvColumn(Name = "Old", FieldIndex = 1)]
        public string Old { get; set; }

        [CsvColumn(Name = "New", FieldIndex = 2)]
        public string New { get; set; }
    }

    internal class ImportDataCsv
    {
        public IEnumerable<ImportDataCsvLine> Lines { get; set; }
    }
}
