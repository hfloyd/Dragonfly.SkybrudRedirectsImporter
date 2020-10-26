namespace Dragonfly.SkybrudRedirectsImporter.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Dragonfly.NetHelpers;
    using Dragonfly.NetModels;
    using Dragonfly.SkybrudRedirectsImporter.Models;
    using Newtonsoft.Json;

    public enum FilesOption
    {
        All,
        ExceptLastOne,
        None
    }

    public static class FilesIO
    {
        internal static string DataPath()
        {
            return Constants.FileUploadPath;
        }

        internal static string TimestringFormat()
        {
            return "yyyy-MM-dd-HH-mm-ss-UTC";
        }

        public static Dictionary<string, string> GetFilesOptions()
        {
            var list = new Dictionary<string, string>();

            list.Add(FilesOption.All.ToString(), "All");
            list.Add(FilesOption.ExceptLastOne.ToString(), "All Except the Most Recent One");

            return list;
        }

        public static StatusMessage DeleteFiles(FilesOption Option)
        {
            var msg = new StatusMessage();

            IEnumerable<FileInfo> filesList;
            var allFilesMsg = GetListOfFiles(DataPath(), out filesList);

            if (!allFilesMsg.Success)
            {
                msg = allFilesMsg;
            }
            else
            {
                var cli = new FilesCleanupInfo();
                switch (Option)
                {
                    case FilesOption.All:
                        cli = DeleteFilesAll();
                        break;

                    case FilesOption.ExceptLastOne:
                        cli = DeleteFilesAllExceptLatest();
                        break;

                    case FilesOption.None:
                        break;
                    default:
                        //DeleteFilesByDate(allFiles)
                        throw new ArgumentOutOfRangeException(nameof(Option), Option, null);
                }

                msg.Success = cli.Status.Success;
                msg.Message = "DeleteFiles completed. See inner status for details.";
                msg.InnerStatuses.Add(cli.Status);
                msg.RelatedObject = cli;
            }

            msg.TimestampEnd = DateTime.Now;
            return msg;
        }

        private static FilesCleanupInfo DeleteFilesAll()
        {
            var sm = new StatusMessage();

            var folderPath = DataPath();
            var mappedPath = Dragonfly.NetHelpers.Files.GetMappedPath(folderPath);
            var allFiles = Directory.GetFiles(mappedPath, "*.json");
            var qtyFiles = allFiles.Count();

            var cleanupInfo = new FilesCleanupInfo();
            cleanupInfo.FolderToClean = folderPath;
            cleanupInfo.OriginalFilesCount = qtyFiles;

            //Delete all files 
            var saveDeleteList = new List<string>();
            var countDeletes = 0;
            var countSaves = 0;
            try
            {
                foreach (var file in allFiles)
                {
                    saveDeleteList.Add($"DELETE '{file}'");
                    countDeletes++;
                    File.Delete(file);
                }
            }
            catch (Exception e)
            {
                sm.Success = false;
                sm.RelatedObject = saveDeleteList;
                sm.TimestampEnd = DateTime.Now;
                sm.Message = $"{countDeletes} file(s) were deleted and {countSaves} file(s) were preserved, but there was an error.";
                sm.RelatedException = e;
            }

            sm.Success = true;
            sm.RelatedObject = saveDeleteList;
            sm.TimestampEnd = DateTime.Now;
            sm.Message = $"{countDeletes} file(s) were deleted and {countSaves} file(s) were preserved.";

            cleanupInfo.Status = sm;

            return cleanupInfo;
        }

        private static FilesCleanupInfo DeleteFilesAllExceptLatest()
        {
            var sm = new StatusMessage();

            var folderPath = DataPath();
            var mappedPath = Dragonfly.NetHelpers.Files.GetMappedPath(folderPath);
            var allFiles = Directory.GetFiles(mappedPath, "*.json");
            var qtyFiles = allFiles.Count();

            var cleanupInfo = new FilesCleanupInfo();
            cleanupInfo.FolderToClean = folderPath;
            cleanupInfo.OriginalFilesCount = qtyFiles;

            //Delete all files except last
            var saveDeleteList = new List<string>();
            var countDeletes = 0;
            var countSaves = 0;

            IEnumerable<FileInfo> resultFilesList;
            var resultFilesMsg = GetListOfFiles(DataPath(), out resultFilesList);

            if (resultFilesMsg.Success)
            {
                try
                {
                    var sortedList = resultFilesList.OrderByDescending(n => n.CreationTimeUtc).ToList();

                    var keepFile = sortedList.First();
                    saveDeleteList.Add($"SAVE '{keepFile.Name}'");
                    countSaves++;

                    foreach (var file in sortedList.Skip(1))
                    {
                        saveDeleteList.Add($"DELETE '{file.Name}'");
                        countDeletes++;
                        File.Delete($"{mappedPath}{file.Name}");
                    }

                    sm.Success = true;
                    sm.RelatedObject = saveDeleteList;
                    sm.TimestampEnd = DateTime.Now;
                    sm.Message = $"{countDeletes} file(s) were deleted and {countSaves} file(s) were preserved.";
                }
                catch (Exception e)
                {
                    sm.Success = false;
                    sm.RelatedObject = saveDeleteList;
                    sm.TimestampEnd = DateTime.Now;
                    sm.Message = $"{countDeletes} file(s) were deleted and {countSaves} file(s) were preserved, but there was an error.";
                    sm.RelatedException = e;
                }
            }
            else
            {
                sm = resultFilesMsg;
            }

            cleanupInfo.Status = sm;

            return cleanupInfo;
        }

        private static FilesCleanupInfo DeleteFilesByDate(int DaysToSave)
        {
            var cleanupInfo = new FilesCleanupInfo();
            var sm = new StatusMessage();

            var folderPath = DataPath();
            cleanupInfo.FolderToClean = folderPath;

            //Dates to Save
            var days = new TimeSpan(DaysToSave - 1, 0, 0, 0);
            cleanupInfo.RangeEndDate = DateTime.Today;
            cleanupInfo.RangeStartDate = cleanupInfo.RangeEndDate.Subtract(days);

            var dateRange = cleanupInfo.RangeStartDate.To(cleanupInfo.RangeEndDate).IncludeStart().IncludeEnd();
            var allDates = dateRange.Step(x => x.AddDays(1)).ToList();
            cleanupInfo.AllDates = allDates;

            //Get files
            var mappedPath = "";
            var mappablePath = Dragonfly.NetHelpers.Files.TryGetMappedPath(folderPath, out mappedPath);
            if (!mappablePath)
            {
                sm.Success = false;
                sm.Message = $"Unable to Map Path for {folderPath}";

                cleanupInfo.Status = sm;
                return cleanupInfo;
            }

            var allFiles = Directory.GetFiles(mappedPath, "*.json");
            var qtyFiles = allFiles.Count();
            cleanupInfo.OriginalFilesCount = qtyFiles;
            cleanupInfo.DaysToKeep = DaysToSave;

            //Delete files outside of date range
            var saveDeleteList = new List<string>();
            var countDeletes = 0;
            var countSaves = 0;

            IEnumerable<FileInfo> filesList;
            var allFilesMsg = GetListOfFiles(DataPath(), out filesList);
            foreach (var file in filesList)
            {
                if (file.CreationTimeUtc != DateTime.MinValue)
                {
                    if (!allDates.Contains((DateTime)file.CreationTimeUtc))
                    {
                        saveDeleteList.Add($"DELETE '{file.Name}'");
                        countDeletes++;
                        File.Delete(file.Name);
                    }
                    else
                    {
                        saveDeleteList.Add($"SAVE '{file.Name}'");
                        countSaves++;
                    }
                }
            }


            //sm.Success = true;
            //sm.RelatedObject = saveDeleteList;
            //sm.TimestampEnd = DateTime.Now;
            //sm.Message = $"{countDeletes} file(s) were deleted and {countSaves} file(s) were preserved.";

            cleanupInfo.Status = sm;

            return cleanupInfo;
        }

        //public static StatusMessage ReadResultSet(string SiteDomain, DateTime TimestampToFetch, int StartNode, out TestResultSet ResultSetData)
        //{
        //    var msg = new StatusMessage();

        //    var fullFilename = BuildFilePath(SiteDomain, TimestampToFetch, StartNode);

        //    try
        //    {
        //        //Get saved data
        //        var json = Dragonfly.NetHelpers.Files.GetTextFileContents(fullFilename);
        //        ResultSetData = JsonConvert.DeserializeObject<TestResultSet>(json);

        //        msg.Success = true;
        //        msg.Message = $"Data read from '{fullFilename}'.";
        //    }
        //    catch (Exception e)
        //    {
        //        msg.Success = false;
        //        msg.Message = $"Unable to read data from '{fullFilename}'.";
        //        msg.RelatedException = e;
        //        ResultSetData = null;
        //    }

        //    msg.TimestampEnd = DateTime.Now;
        //    return msg;
        //}

        //public static StatusMessage ReadResultSet(string Filename, out TestResultSet ResultSetData)
        //{
        //    var msg = new StatusMessage();

        //    var fullFilename = BuildFilePath(Filename);

        //    try
        //    {
        //        //Get saved data
        //        var json = Dragonfly.NetHelpers.Files.GetTextFileContents(fullFilename);
        //        ResultSetData = JsonConvert.DeserializeObject<TestResultSet>(json);

        //        msg.Success = true;
        //        msg.Message = $"Data read from '{fullFilename}'.";
        //    }
        //    catch (Exception e)
        //    {
        //        msg.Success = false;
        //        msg.Message = $"Unable to read data from '{fullFilename}'.";
        //        msg.RelatedException = e;
        //        ResultSetData = null;
        //    }

        //    msg.TimestampEnd = DateTime.Now;
        //    return msg;
        //}

        //public static StatusMessage SaveResultSet(TestResultSet ResultSet)
        //{
        //    var msg = new StatusMessage();

        //    var fullFilename = BuildFilePath(ResultSet.SiteDomain, ResultSet.TestStartTimestamp, ResultSet.StartNode);

        //    try
        //    {
        //        var json = JsonConvert.SerializeObject(ResultSet);
        //        var savedSuccessfully = Dragonfly.NetHelpers.Files.CreateTextFile(fullFilename, json, true, false);

        //        if (savedSuccessfully)
        //        {
        //            msg.Success = true;
        //            msg.Message = $"Data saved successfully to '{fullFilename}'.";
        //            msg.ObjectName = fullFilename;
        //        }
        //        else
        //        {
        //            msg.Success = false;
        //            msg.Message = $"Unable to save data to '{fullFilename}'.";
        //            msg.MessageDetails = "Unknown issue";
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        msg.Success = false;
        //        msg.Message = $"Unable to save data to '{fullFilename}'.";
        //        msg.RelatedException = e;
        //    }

        //    msg.TimestampEnd = DateTime.Now;
        //    return msg;
        //}

        public static StatusMessage GetListOfFiles(string FolderPath, out IEnumerable<FileInfo> FilesList)
        {
            var returnStatusMsg = new StatusMessage();
            var filesList = new List<FileInfo>();

            var folder = string.IsNullOrEmpty(FolderPath) ? DataPath() : FolderPath;
            var isMappable = Dragonfly.NetHelpers.Files.TryGetMappedPath(folder, out var dirMapped);

            if (!isMappable)
            {
                returnStatusMsg.Success = false;
                returnStatusMsg.Message = $"Unable to Map '{folder}'";
            }
            else
            {
                try
                {
                    var files = Directory.GetFiles(dirMapped).ToList();
                    foreach (var filepath in files)
                    {
                        var filename = "";
                        try
                        {
                            //filename = filepath.Replace(dirMapped, "");
                            var fileInfo = new FileInfo(filepath);
                            filesList.Add(fileInfo);
                            //filesList.Add(filename, GetTimestampFromFileName(filename));
                        }
                        catch (Exception e)
                        {
                            var fileMsg = new StatusMessage(false);
                            returnStatusMsg.Message = $"Error processing file '{filename}'.";
                            fileMsg.RelatedException = e;

                            returnStatusMsg.InnerStatuses.Add(fileMsg);
                        }
                    }
                }
                //catch (System.IO.DirectoryNotFoundException missingDirEx)
                //{
                //    //continue
                //}
                catch (Exception e)
                {
                    returnStatusMsg.Success = false;
                    returnStatusMsg.Message = $"Error accessing files in '{dirMapped}'.";
                    returnStatusMsg.RelatedException = e;
                }

                if (filesList.Any())
                {
                    returnStatusMsg.Success = true;
                    returnStatusMsg.Message = $"{filesList.Count} files found.";
                }
                else
                {
                    if (returnStatusMsg.Message == "")
                    {
                        returnStatusMsg.Success = false;
                        returnStatusMsg.Message = $"No files found.";
                    }
                }
            }

            FilesList = filesList;
            return returnStatusMsg;

        }

        private static string BuildFilePath(string Filename)
        {
            var ext = Filename.EndsWith(".json") ? "" : ".json";
            var fullFilename = DataPath() + Filename + ext;

            return fullFilename;
        }

        private static string BuildFilePath(string SiteDomain, DateTime Timestamp, int StartNode)
        {
            var nodePart = StartNode == 0 ? "ALL" : StartNode.ToString();
            var fullFilename = DataPath() + SiteDomain + "_" + TimestampToString(Timestamp) + "_" + nodePart + ".json";

            return fullFilename;
        }

        //public enum PathPart
        //{
        //    Folder,
        //    Filename,
        //    Domain,
        //    Timestamp,
        //    StartNode,
        //    Extension
        //}

        //public static FileInfo ParseFilePath(string FilePath)
        //{
        //    var pathData = new FileInfo();

        //    //Filename & Folder
        //    var pathDelim = FilePath.Contains("/") ? '/' : '\\';
        //    var splitFilePath = FilePath.Split(pathDelim).ToList();

        //    var filename = splitFilePath.Last();
        //    pathData.Filename = filename;

        //    var folderPath = FilePath.Replace(filename, "").Trim();
        //    if (folderPath != "") { pathData.Folder = folderPath; }

        //    if (filename.Contains("_"))
        //    {
        //        //Extension
        //        var splitExt = filename.Split('.').ToList();
        //        var ext = splitExt.Last();
        //        pathData.Extension = ext;

        //        var filenameStripped = filename.Replace("." + ext, "");
        //        var splitFileName = filenameStripped.Split('_');

        //        //Domain
        //        pathData.Domain = splitFileName[0];

        //        //Timestamp
        //        var filenameDate = splitFileName[1];
        //        var timestampVal = StringToTimestamp(filenameDate);
        //        pathData.Timestamp = timestampVal;

        //        //StartNode
        //        var startNode = splitFileName[2] == "ALL" ? 0 : Convert.ToInt32(splitFileName[2]);
        //        pathData.StartNode = startNode;

        //    }
        //    else
        //    {
        //        pathData = ParseArchiveFilename(FilePath);
        //    }

        //    return pathData;
        //}

        //private static FileInfo ParseArchiveFilename(string FilePath)
        //{
        //    var pathData = new FileInfo();

        //    //Filename & Folder
        //    var pathDelim = FilePath.Contains("/") ? '/' : '\\';
        //    var splitFilePath = FilePath.Split(pathDelim).ToList();

        //    var filename = splitFilePath.Last();
        //    pathData.Filename = filename;

        //    var folderPath = FilePath.Replace(filename, "").Trim();
        //    if (folderPath != "") { pathData.Folder = folderPath; }

        //    //Extension
        //    var splitExt = filename.Split('.').ToList();
        //    var ext = splitExt.Last();
        //    pathData.Extension = ext;

        //    var filenameStripped = filename.Replace("." + ext, "");

        //    var format = TimestringFormat();
        //    var dateStartChar = filenameStripped.Length - format.Length;
        //    var filenameDate = filenameStripped.Substring(dateStartChar, format.Length);

        //    var timestampVal = StringToTimestamp(filenameDate);
        //    pathData.Timestamp = timestampVal;

        //    var domainText = filenameStripped.Replace("-" + filenameDate, "");
        //    pathData.Domain = domainText;

        //    pathData.StartNode = 0;

        //    return pathData;
        //}
        //private static DateTime GetTimestampFromFileName(string Filename)
        //{
        //    var filenameStripped = Filename.Replace(".json", "");
        //    var format = TimestringFormat();
        //    var dateStartChar = filenameStripped.Length - format.Length;
        //    var filenameDate = filenameStripped.Substring(dateStartChar, format.Length);

        //    return StringToTimestamp(filenameDate);
        //}

        public static string TimestampToString(DateTime Timestamp)
        {
            var utcDate = Timestamp.ToUniversalTime();
            var format = TimestringFormat();
            var timeString = utcDate.ToString(format);

            ////Get rid of "+" in timezone
            //timeString = timeString.Replace("+", "");

            return timeString;
        }

        public static DateTime StringToTimestamp(string TimestampString)
        {
            var dateString = TimestampString;
            var format = TimestringFormat();
            var formatNoUtc = format.Replace("-UTC", "");
            try
            {
                if (dateString.EndsWith("-UTC"))
                {
                    dateString = dateString.Replace("-UTC", "");
                    DateTime dateUtc;
                    var dateParsed = DateTime.TryParseExact(dateString, formatNoUtc,
                        CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateUtc);
                    if (dateParsed)
                    {
                        return dateUtc;
                    }
                    else
                    {
                        try
                        {
                            var date = DateTime.Parse(dateString);
                            return date;
                        }
                        catch (Exception exception)
                        {
                            var date = DateTime.MinValue;
                            return date;
                        }
                    }
                }
                else
                {
                    var dateParts = dateString.Split('-');
                    var year = Int32.Parse(dateParts[0]);
                    var month = Int32.Parse(dateParts[1]);
                    var day = Int32.Parse(dateParts[2]);
                    var hour = Int32.Parse(dateParts[3]);
                    var minute = Int32.Parse(dateParts[4]);
                    var second = Int32.Parse(dateParts[5]);
                    var ap = dateParts[6];
                    var timezone = dateParts[7];

                    if (ap.ToLower() == "pm")
                    {
                        hour = hour + 12;
                    }

                    //var tzId = TimeZoneInfo.GetSystemTimeZones();
                    //TimeZoneInfo tzone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                    DateTime date = new DateTime(year, month, day, hour, minute, second, 0, DateTimeKind.Unspecified);
                    //DateTimeOffset dateTimeOffset = new DateTimeOffset(date, tzone.BaseUtcOffset);

                    return date;
                    //return dateTimeOffset.ToLocalTime();

                }
            }
            catch (Exception e)
            {
                try
                {
                    var date = DateTime.Parse(dateString);
                    return date;
                }
                catch (Exception exception)
                {
                    var date = DateTime.MinValue;
                    return date;
                }
            }
        }

    }
}
