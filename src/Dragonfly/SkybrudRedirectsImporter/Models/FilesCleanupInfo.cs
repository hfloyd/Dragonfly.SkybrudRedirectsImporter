namespace Dragonfly.SkybrudRedirectsImporter.Models
{
    using System;
    using System.Collections.Generic;
    using Dragonfly.NetModels;

    class FilesCleanupInfo
    {
        public StatusMessage Status { get; set; }
        public string FolderToClean { get; set; }
        public int OriginalFilesCount { get; set; }
        public int DaysToKeep { get; set; }
        public DateTime RangeStartDate { get; set; }
        public DateTime RangeEndDate { get; set; }
        public IEnumerable<DateTime> AllDates { get; set; }

    }

}
