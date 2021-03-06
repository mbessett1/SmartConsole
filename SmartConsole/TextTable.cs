﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            return AddColumn(new TextColumn() {Header = headerName, Width = headerName.Length });
        }

        public TextTable AddColumn(string headerName, AlignmentType alignment = AlignmentType.Left, string propertyName = null)
        {
            return AddColumn(new TextColumn() { Header = headerName, Width = headerName.Length, Alignment = alignment, PropertyName = propertyName});
        }

        public TextTable AddColumn(string headerName, int width, AlignmentType alignment = AlignmentType.Left, string propertyName = null)
        {
            return AddColumn(new TextColumn() { Header = headerName, Width = width, Alignment = alignment, PropertyName = propertyName });
          
        }

        public string Render(IEnumerable<string[]> rows, bool includeHeaders = true)
        {
            var sb = new StringBuilder();

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
                var l = ((string) Convert.ChangeType(info.GetValue(obj), typeof(string))).Length;
                result = (l > result) ? l : result;
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
                var values = rowTypeProperties.Select(p => p.GetValue(row).ToString()).ToArray();

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

            int maxCol = (values.Length >= _columns.Count) ? _columns.Count : values.Length;

            for (int i = 0; i < maxCol; i++)
            {
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

            if (value.Length > size)
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
                    return result.Append(value).Append(' ', size).ToString().Substring(0, size) + delimiter;
            }
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









}
