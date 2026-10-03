using ClosedXML.Excel;

namespace WebBanHang.Helpers
{
    public static class ExcelWorkbook
    {
        public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public static byte[] Build(string sheetName, IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            for (var column = 0; column < headers.Count; column++)
            {
                var cell = worksheet.Cell(1, column + 1);
                cell.Value = headers[column];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#0b6b4d");
            }

            var rowNumber = 2;
            foreach (var row in rows)
            {
                for (var column = 0; column < headers.Count && column < row.Length; column++)
                {
                    worksheet.Cell(rowNumber, column + 1).Value = XLCellValue.FromObject(row[column]);
                }
                rowNumber++;
            }

            var lastRow = Math.Max(1, rowNumber - 1);
            var table = worksheet.Range(1, 1, lastRow, headers.Count).CreateTable();
            table.Theme = XLTableTheme.TableStyleMedium4;
            worksheet.SheetView.FreezeRows(1);
            worksheet.Columns().AdjustToContents();
            worksheet.Columns(1, headers.Count).AdjustToContents(8, 40);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
