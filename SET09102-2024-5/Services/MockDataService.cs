using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using SET09102_2024_5.Interfaces;
using SET09102_2024_5.Models;

namespace SET09102_2024_5.Services
{
    /// <summary>
    /// Mock implementation of the IDataService interface that provides historical environmental data
    /// from local Excel files. This service reads structured data from Excel workbooks stored in the
    /// application's Resources/Raw directory. Supports different data categories (Air, Water, Weather)
    /// with appropriate handling for each format's timestamp representation.
    /// </summary>
    public class MockDataService : IDataService
    {
        public async Task<List<EnvironmentalDataModel>> GetHistoricalData(string category, string site)
        {
            // Determine file path based on category
            var fileName = category switch
            {
                "Air" => "Air_quality.xlsx",
                "Water" => "Water_quality.xlsx",
                "Weather" => "Weather.xlsx",
                _ => throw new ArgumentException("Unknown category")
            };

            // Build path to Data folder
            var dataDir = Path.Combine(AppContext.BaseDirectory, "Resources", "Raw");
            var filePath = Path.Combine(dataDir, fileName);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Data file not found: {filePath}");

            // Read the file and parse data
            return await Task.Run(() =>
            {
                var list = new List<EnvironmentalDataModel>();
                using var workbook = new XLWorkbook(filePath);
                var ws = workbook.Worksheet(1);

                // Find header row (where first cell is "Date" or "time")
                var headerRow = ws.RowsUsed()
                    .First(r =>
                        r.Cell(1).GetString().Trim().Equals("Date", StringComparison.OrdinalIgnoreCase)
                     || r.Cell(1).GetString().Trim().Equals("time", StringComparison.OrdinalIgnoreCase)
                    );
                int headerIndex = headerRow.RowNumber();

                // Determine if weather style (single time column)
                bool isWeather = headerRow.Cell(1).GetString().Trim().Equals("time", StringComparison.OrdinalIgnoreCase);

                // Identify column indexes
                int dateCol = headerRow.CellsUsed()
                    .First(c => c.GetString().Trim().Equals(isWeather ? "time" : "Date", StringComparison.OrdinalIgnoreCase))
                    .Address.ColumnNumber;
                int timeCol = isWeather ? -1 : headerRow.CellsUsed()
                    .First(c => c.GetString().Trim().Equals("Time", StringComparison.OrdinalIgnoreCase))
                    .Address.ColumnNumber;

                // Find the very last column in the sheet
                int lastCol = ws.LastColumnUsed().ColumnNumber();

                // Build measureCols = every column between 1..lastCol except dateCol & timeCol
                var measureCols = Enumerable
                    .Range(1, lastCol)
                    .Where(ci => ci != dateCol && ci != timeCol)
                    .ToList();

                // Read header-names for each measure column
                var headerNames = measureCols.Select(ci => ws.Row(headerIndex).Cell(ci).GetString().Trim()).ToList();

                // Process data rows after header
                foreach (var row in ws.Rows(headerIndex + 1, ws.LastRowUsed().RowNumber()))
                {
                    // 1) read raw strings
                    var dateRaw = row.Cell(dateCol).GetString().Trim();
                    var timeRaw = isWeather ? "" : row.Cell(timeCol).GetString().Trim();

                    // 2) skip any row missing date (or time for Air/Water)
                    if (string.IsNullOrWhiteSpace(dateRaw) || (!isWeather && string.IsNullOrWhiteSpace(timeRaw)))
                        continue;

                    DateTime timestamp;
                    if (isWeather)
                    {
                        // parse ISO‐style full timestamp
                        var raw = row.Cell(dateCol).GetString().Trim();
                        if (!DateTime.TryParse(raw, out timestamp)) continue;
                    }
                    else
                    {
                        // for Air/Water: dateCol may hold midnight always, so split parts explicitly
                        // get date part
                        var dateCell = row.Cell(dateCol);
                        DateTime datePart = dateCell.DataType == XLDataType.DateTime
                            ? dateCell.GetDateTime().Date
                            : DateTime.Parse(dateCell.GetString().Trim()).Date;
                        // get time part
                        var timeText = row.Cell(timeCol).GetString().Trim();
                        if (!TimeSpan.TryParse(timeText, out var timePart)) continue;
                        timestamp = datePart + timePart;
                    }

                    // build the dictionary
                    var model = new EnvironmentalDataModel
                    {
                        Timestamp = timestamp,
                        SensorSite = site,
                        DataCategory = category
                    };

                    for (int i = 0; i < measureCols.Count; i++)
                    {
                        var colIndex = measureCols[i];
                        var param = headerNames[i];
                        var cell = row.Cell(colIndex);
                        double dval = cell.DataType == XLDataType.Number
                            ? cell.GetDouble()
                            : double.TryParse(cell.GetString().Trim(), out var dd) ? dd : 0;
                        model.Values[param] = dval;
                    }
                    list.Add(model);
                }
                return list;
            });
        }
    }
}
