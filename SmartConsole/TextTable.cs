using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bessett.SmartConsole.Text
{
    public enum AlignmentType
    {
        Left,
        Right,
        Center
    }

    public class TextColumn
    {
        public string Header { get; set; }
        public int Width { get; set; }
        public AlignmentType Alignment { get; set; }
        public string PropertyName { get; set; }
        public bool UseWordwrap { get; set; }
        public bool AutoSize { get; set; }
    }

    public class TextTable
    {

        List<TextColumn> _columns = new List<TextColumn>();

        public TextTable AddColumn(TextColumn columnDef)
        {
            _columns.Add(columnDef);
            return this;
        }
        public TextTable AddColumn(string headerName, string propertyName = null)
        {
            return AddColumn(new TextColumn() {Header = headerName, PropertyName=propertyName, Width = headerName.Length });
        }

        public TextTable AddColumn(string headerName, AlignmentType alignment = AlignmentType.Left, string propertyName = null)
        {
            return AddColumn(new TextColumn() { Header = headerName, Width = headerName.Length, Alignment = alignment, PropertyName = propertyName});
        }

        public TextTable AddColumn(string headerName, int width, AlignmentType alignment = AlignmentType.Left, string propertyName = null)
        {
            return AddColumn(new TextColumn() { Header = headerName, Width = width, Alignment = alignment, PropertyName = propertyName ?? headerName });
          
        }

        public string Render(IEnumerable<string[]> rows, bool includeHeaders = true, int maxColumnWidth = 15, params string[] headers)
        {
            var sb = new StringBuilder();

            if (_columns.Count == 0 || headers.Length > 0)
            {
                GenColumns(rows, maxColumnWidth, headers);
            }  
            
            foreach (var row in RenderRows(rows, includeHeaders))
            {
                sb.AppendLine(row);
            }
            return sb.ToString();
        }

        public string Render(IEnumerable<object> rows, bool includeHeaders = true)
        {
            var sb = new StringBuilder();

            foreach (var row in RenderRows(rows, includeHeaders))
            {
                sb.AppendLine(row);
            }
            return sb.ToString();
        }

        public IEnumerable<string> RenderRowsOrig(IEnumerable<string[]> rows, bool includeHeaders = true)
        {
            if (includeHeaders)
                yield return HeaderText(includeHeaders);

            foreach (var row in rows)
            {
                yield return RowText(row);
            }
        }
        public IEnumerable<string> RenderRows(IEnumerable<string[]> rows, bool includeHeaders = true)
        {
            if (includeHeaders)
                yield return HeaderText(includeHeaders);

            foreach (var row in rows)
            {
                List<List<string>> allColumnLines = new List<List<string>>();
                int maxLinesPerRow = 0;

                // Process each column's text for overflow
                for (int colIndex = 0; colIndex < row.Length; colIndex++)
                {
                    int columnWidth = _columns[colIndex].Width;  // Retrieve the width from the column definition
                    var lines = SplitIntoLines(row[colIndex], columnWidth);
                    allColumnLines.Add(lines);
                    maxLinesPerRow = Math.Max(maxLinesPerRow, lines.Count);
                }

                // Normalize the number of lines across all columns
                foreach (var columnLines in allColumnLines)
                {
                    while (columnLines.Count < maxLinesPerRow)
                        columnLines.Add("");  // Add empty string to ensure all columns have the same number of lines
                }

                // Combine and yield each line across all columns
                for (int lineIndex = 0; lineIndex < maxLinesPerRow; lineIndex++)
                {
                    var lineToRender = new StringBuilder();
                    for (int colIndex = 0; colIndex < allColumnLines.Count; colIndex++)
                    {
                        lineToRender.Append(FormatCell(allColumnLines[colIndex][lineIndex], _columns[colIndex].Width, _columns[colIndex].Alignment));
                    }
                    yield return lineToRender.ToString();
                }
            }
        }

        private List<string> SplitIntoLines(string text, int maxWidth)
        {
            List<string> lines = new List<string>();
            int start = 0;
            while (start < text.Length)
            {
                int length = Math.Min(maxWidth, text.Length - start);
                lines.Add(text.Substring(start, length));
                start += length;
            }
            return lines;
        }

        private string FormatCell(string text, int width, AlignmentType alignment)
        {
            // Format text based on alignment: left, right, or center.
            switch (alignment)
            {
                case AlignmentType.Left:
                    return text.PadRight(width);
                case AlignmentType.Right:
                    return text.PadLeft(width);
                case AlignmentType.Center:
                    int padding = (width - text.Length) / 2;
                    return text.PadLeft(text.Length + padding).PadRight(width);
                default:
                    return text.PadRight(width);
            }
        }

        private string TextFieldNEW2(string value, int size, AlignmentType alignment)
        {
            // Assuming the TextField method handles text fitting and alignment properly
            // Placeholder implementation for TextField
            return value.PadRight(size).Substring(0, size);
        }

        int buildColumns(IEnumerable<object> target, Type objType, PropertyInfo info)
        {
            // for each propery, find the longest length & add 1
            int result = 0;

            foreach (var obj in target)
            {
                if (info.GetValue(obj) != null)
                {
                    //var l = ((string)Convert.ChangeType(info.GetValue(obj), typeof(string))).Length;
                    var l= info.GetValue(obj).ToString().Length;
                    result = (l > result) ? l : result;
                }
            }
            return result + 1;
        }

        public IEnumerable<string> RenderRows(IEnumerable<object> rows, bool includeHeaders = true)
        {
            if (includeHeaders)
                yield return HeaderText(includeHeaders);

            foreach (var row in rows)
            {
                List<List<string>> allColumnLines = new List<List<string>>();
                int maxLinesPerRow = 0;

                // Process each property in the row object for overflow
                for (int colIndex = 0; colIndex < _columns.Count; colIndex++)
                {
                    var column = _columns[colIndex];
                    var propertyValue = row.GetType().GetProperty(column.PropertyName)?.GetValue(row, null)?.ToString() ?? "";

                    int columnWidth = column.Width;
                    var lines = SplitIntoLines(propertyValue, columnWidth);
                    allColumnLines.Add(lines);
                    maxLinesPerRow = Math.Max(maxLinesPerRow, lines.Count);
                }

                // Normalize the number of lines across all columns
                foreach (var columnLines in allColumnLines)
                {
                    while (columnLines.Count < maxLinesPerRow)
                        columnLines.Add("");  // Add empty string to ensure all columns have the same number of lines
                }

                // Combine and yield each line across all columns
                for (int lineIndex = 0; lineIndex < maxLinesPerRow; lineIndex++)
                {
                    var lineToRender = new StringBuilder();
                    for (int colIndex = 0; colIndex < allColumnLines.Count; colIndex++)
                    {
                        lineToRender.Append(FormatCell(allColumnLines[colIndex][lineIndex], _columns[colIndex].Width, _columns[colIndex].Alignment));
                    }
                    yield return lineToRender.ToString();
                }
            }
        }

        public IEnumerable<string> RenderRowsOrig(IEnumerable<object> rows, bool includeHeaders = true)
        {
            Type rowType = null;
            PropertyInfo[] rowTypeProperties = null;

            foreach (var row in rows)
            {
                if (rowType == null)
                {
                    rowType = row.GetType();
                    rowTypeProperties = rowType.GetProperties();

                    if (_columns.Count == 0)
                    {
                        // autogenerate some headers
                        foreach (var propertyInfo in rowTypeProperties)
                        {
                            var colSize = buildColumns(rows, rowType, propertyInfo);
                            AddColumn(propertyInfo.Name,  colSize> propertyInfo.Name.Length ? colSize : propertyInfo.Name.Length);
                        }
                    }

                    if (includeHeaders)
                        yield return HeaderText(includeHeaders);
                }

                // get the values for the object
                var values = rowTypeProperties.Select(p => p.GetValue(row)?.ToString()).ToArray();

                yield return RowText(values);
            }
        }

        public string HeaderText(bool appendMarkers = true)
        {
            var result = new StringBuilder()
                .AppendLine(RowText(_columns.Select(c => c.Header).ToArray()));

            if (appendMarkers)
                result.AppendLine(FieldMarkers());

            return result.ToString();
        }

        public string RowText(params string[] values)
        {
            var sb = new StringBuilder();

            int maxCol = (_columns.Count>0 && values.Length >= _columns.Count) ? _columns.Count : values.Length;

            for (int i = 0; i < maxCol; i++)
            {
                if (i > _columns.Count - 1)
                {
                    AddColumn("Column " + i, values[i].Length);
                }
                sb.Append(TextField(values[i], _columns[i].Width, _columns[i].Alignment));
            }

            return sb.ToString();
        }

        string FieldMarkers(char marker = '-')
        {
            var sb = new StringBuilder();
            var columnMarkers = _columns.Select(c => new StringBuilder().Append(marker, c.Width).ToString()).ToArray();

            return RowText(columnMarkers);
        }

        string TextField(string value, int size, AlignmentType alignment = AlignmentType.Left, string delimiter = " ")
        {
            var result = new StringBuilder();

            if (value?.Length > size)
                value = value.Substring(0, size);

            switch (alignment)
            {
                case AlignmentType.Center:
                    int f = size;
                    int v = value.Length;
                    int a = (f >= v) ? (f - v) / 2 : 0;

                    return result.Append(' ', a).Append(value).Append(' ', f - (a + v)).ToString() + delimiter;
                case AlignmentType.Right:
                    return result.Append(' ', size).Append(value).ToString().Substring(value.Length, size) + delimiter;
                default:
                    return result.Append(value??"").Append(' ', size).ToString().Substring(0, size) + delimiter;
            }
        }

        string TextFieldOrig1(string value, int size, AlignmentType alignment = AlignmentType.Left, string delimiter = " ")
        {
            if (string.IsNullOrEmpty(value))
                value = ""; // Handle null or empty strings

            List<string> wrappedLines = WordWrap(value, size); // Wrap text into lines

            StringBuilder formattedResult = new StringBuilder();

            foreach (var line in wrappedLines)
            {
                // Apply alignment to each line
                switch (alignment)
                {
                    case AlignmentType.Right:
                        formattedResult.AppendLine(line.PadLeft(size));
                        break;
                    case AlignmentType.Center:
                        int padding = (size - line.Length) / 2;
                        formattedResult.AppendLine(line.PadLeft(line.Length + padding).PadRight(size));
                        break;
                    case AlignmentType.Left:
                    default:
                        formattedResult.AppendLine(line.PadRight(size));
                        break;
                }
            }

            return formattedResult.ToString().TrimEnd();
        }

        string TextFieldNew(string value, int size, AlignmentType alignment = AlignmentType.Left, string delimiter = " ")
        {
            var result = new StringBuilder();

            // Ensure handling null or empty values
            if (string.IsNullOrEmpty(value))
            {
                return result.Append(' ', size).ToString().Substring(0, size) + delimiter;
            }

            // Word wrapping logic
            var words = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > size) // Check if adding this word would exceed the width
                {
                    result.AppendLine(currentLine.ToString().PadRight(size)); // Finish the current line
                    currentLine.Clear();
                }

                if (currentLine.Length > 0)
                    currentLine.Append(" ");

                currentLine.Append(word);
            }

            if (currentLine.Length > 0)
                result.AppendLine(currentLine.ToString().PadRight(size)); // Add the last line

            // Aligning the text as per the specified alignment
            var lines = result.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            result.Clear();
            foreach (var line in lines)
            {
                switch (alignment)
                {
                    case AlignmentType.Center:
                        int padding = (size - line.Trim().Length) / 2;
                        result.AppendLine(line.Trim().PadLeft(padding + line.Trim().Length).PadRight(size));
                        break;
                    case AlignmentType.Right:
                        result.AppendLine(line.Trim().PadLeft(size));
                        break;
                    default:
                        result.AppendLine(line.PadRight(size));
                        break;
                }
            }

            return result.ToString().TrimEnd(); // Ensure we remove the last new line for the last row
        }

        List<string> WordWrap(string text, int maxLineLength)
        {
            List<string> lines = new List<string>();

            if (string.IsNullOrEmpty(text))
            {
                lines.Add(""); // return an empty line for empty text
                return lines;
            }

            int start = 0, end;
            while ((end = start + maxLineLength) < text.Length)
            {
                while (text[end] != ' ' && end > start) end--;
                if (end == start) end = start + maxLineLength; // If no spaces, force wrap

                lines.Add(text.Substring(start, end - start).Trim());
                start = end + 1;
            }

            if (start < text.Length)
                lines.Add(text.Substring(start).Trim());

            return lines;
        }

        /// <summary>
        /// Enumerate through the data and find the longest string in each column
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="maxColumnWIdth"></param>
        internal void GenColumns(IEnumerable<string[]> dataList, int maxColumnWIdth = 15, params string[] rowHeaders)
        {
            if (_columns.Count > 0) return;

            var headers = (rowHeaders.Length > 0)
                ? rowHeaders
                : dataList.Select((C, i) => $"Col {i}").ToArray();

            _columns.AddRange(headers.Select(c => new TextColumn() { Header = c, Width = 1 }));

            ProfileColumns(dataList, maxColumnWIdth).ToArray();
        }

        /// <summary>
        /// Enumerate through the data and caharacterize each column
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="maxColumnWIdth"></param>
        /// <param name="headerRow"></param>
        /// <returns></returns>
        internal IEnumerable<string[]> ProfileColumns(IEnumerable<string[]> dataList, int maxColumnWIdth = 15, int headerRow = -1)
        {
            var rowNumber = 0;
            var column = 0;
            // for each propery, find the longest length & add columns            
            foreach (var row in dataList)
            {
                column = 0;
                foreach (var col in row)
                {
                    if (col.Length > _columns[column].Width)
                        _columns[column].Width = col.Length > maxColumnWIdth ? maxColumnWIdth : col.Length;
                    column++;
                }

                // return all rows except the header row
                if (rowNumber != headerRow)
                    yield return row;

                rowNumber++;

            }
        }

        /// <summary>
        /// Enumerate through the data and find the longest string in each column
        /// Specify the row to use as the header row (defalut is no header row)
        /// The header row will be used to name the columns, and will be skipped in the data returned   
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="maxColumnWIdth"></param>
        internal IEnumerable<string[]> AutoColumns(IEnumerable<string[]> dataList, int maxColumnWIdth = 15, int headerRow = -1)
        {
            if (_columns.Count > 0) _columns.Clear();

            var headers = (headerRow >= 0)  
                ? dataList.ElementAt(headerRow)
                : dataList.Select((C, i) => $"Col {i}").ToArray();

            _columns.AddRange(headers.Select(c => new TextColumn() { Header = c, Width = 1 }));

            return ProfileColumns(dataList, maxColumnWIdth);

        }

        public static class Extenions
        {
            public static string ToWrapped( string s, int width, string sep = "\n\t")
            {
                return string.Join(sep, WordWrap(s, width));
            }

            public static List<string> WordWrap(string text, int maxLineLength)
            {
                if (string.IsNullOrEmpty(text))
                {
                    return new List<string>() { { "(No text)" } };
                }
                if (text.Contains("\r"))
                {
                    text = text.Replace("\r\n", "\n").Replace("\r", "\n");
                }

                if (text.Contains("\n"))
                {
                    var list = new List<string>();

                    var paragraphs = text.Split('\n');
                    foreach (var paragraph in paragraphs)
                    {
                        list.AddRange(WordWrap(paragraph, maxLineLength));
                    }
                    return list;
                }
                else
                {
                    var list = new List<string>();
                    int currentIndex;
                    var lastWrap = 0;
                    var whitespace = new[] { ' ', '\r', '\n', '\t' };
                    do
                    {
                        currentIndex = lastWrap + maxLineLength > text.Length
                            ? text.Length
                            : (text.LastIndexOfAny(new[] { ' ', ',', '.', '?', '!', ':', ';', '-', '\n', '\r', '\t' },
                                   Math.Min(text.Length - 1, lastWrap + maxLineLength)) + 1);
                        if (currentIndex <= lastWrap)
                            currentIndex = Math.Min(lastWrap + maxLineLength, text.Length);
                        list.Add(text.Substring(lastWrap, currentIndex - lastWrap).Trim(whitespace));
                        lastWrap = currentIndex;
                    } while (currentIndex < text.Length);

                    return list;
                }
            }
        }
    }

    public static class TextTableExtensions
    {
        /// <summary>
        /// Render a list of objects directly as a text table
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string RenderAsTextTable<T>(this IEnumerable<T> list)
        where T : class, new()
        {
            var table = new TextTable();
            return table.Render(list);
        }

        /// <summary>
        /// Render a list of objects directly as a text table
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string RenderAsTextTable(this IEnumerable<string[]> list, int MaxColWidth = 15, params string[] headers)
        //where T : class, IConvertible
        {
            var table = new TextTable();
            table.GenColumns(list, MaxColWidth, headers);
            return table.Render(list);
        }
    }
}
