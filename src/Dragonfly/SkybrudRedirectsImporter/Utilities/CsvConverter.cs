namespace Dragonfly.SkybrudRedirectsImporter.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Dragonfly.NetHelpers;
    using Dragonfly.NetModels;
    using Dragonfly.SkybrudRedirectsImporter.Models;
    using Dragonfly.SkybrudRedirectsImporter.Models.Csv;
    using LINQtoCSV;


    public class CsvConverter
    {
        internal static StatusMessage ConvertImportDataCsvToModel(string ImportFilePath, out ImportData DataModel)
        {
            var returnMsg = new StatusMessage();
            returnMsg.Success = true;
            var csvModel = new ImportDataCsv();
            DataModel = new ImportData();
            var dataItems = new List<ImportDataItem>();

            try
            {
                CsvFileDescription inputFileDescription = new CsvFileDescription
                {
                    SeparatorChar = ',',
                    FirstLineHasColumnNames = true
                };

                CsvContext cc = new CsvContext();
                string localDiskPath = Files.GetMappedPath(ImportFilePath);

                csvModel.Lines = cc.Read<ImportDataCsvLine>(localDiskPath, inputFileDescription);
                returnMsg.Message = $"CSV File read into CSV Model - {csvModel.Lines.Count()} lines converted.";

                foreach (var line in csvModel.Lines.ToList())
                {
                    var item = new ImportDataItem();
                    item.OldUrl = line.Old;
                    item.NewUrl = line.New;
                    dataItems.Add(item);
                }

                DataModel.Items = dataItems;

            }
            catch (AggregatedException ae)
            {
                returnMsg.Message = "Errors encountered while reading CSV file.";
                returnMsg.Success = false;
                returnMsg.RelatedException = ae;

                // Process all exceptions generated while processing the file
                List<Exception> innerExceptionsList = (List<Exception>)ae.Data["InnerExceptionsList"];
                foreach (Exception e in innerExceptionsList)
                {
                    returnMsg.MessageDetails += $"{e.GetType()}: {e.Message}\n";
                }
            }
            catch (Exception e)
            {
                var msg = $"Errors encountered while converting CSV file '{ImportFilePath}'\n";
                returnMsg.Message += msg;
                if (!e.Message.StartsWith("Could not find file"))
                {
                    returnMsg.MessageDetails += $"{msg} - [{e.Message}].\n";
                }
                else
                {
                    returnMsg.MessageDetails += $"{msg}.\n";
                }

                returnMsg.Success = false;
            }

            return returnMsg;
        }
    }
}