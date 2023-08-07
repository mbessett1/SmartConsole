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
            return AddColumn(new TextColumn() { Header = headerName, Width = width, Alignment = alignment, PropertyName = propertyName });
          
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

        public IEnumerable<string> RenderRows(IEnumerable<string[]> rows, bool includeHeaders = true)
        {
            if (includeHeaders)
                yield return HeaderText(includeHeaders);

            foreach (var row in rows)
            {
                yield return RowText(row);
            }
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
