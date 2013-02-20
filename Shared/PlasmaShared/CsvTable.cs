using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PlasmaShared
{
    public class CsvRow: IEnumerable<KeyValuePair<string, string>>
    {
        readonly List<string> cells = new List<string>();
        readonly Dictionary<string, int> headers = new Dictionary<string, int>();

        internal CsvRow(List<string> row, Dictionary<string, int> headers) {
            cells = row;
            this.headers = headers;
        }


        public string this[string key] {
            get {
                if (!headers.ContainsKey(key)) throw new ApplicationException(string.Format("Table does not contain column {0}", key));
                return cells[headers[key]];
            }
        }

        public string this[int key] { get { return cells[key]; } }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
            foreach (var kvp in headers) yield return new KeyValuePair<string, string>(kvp.Key, cells.Count > kvp.Value ? cells[kvp.Value] : null);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }

    public class CsvTable: IEnumerable<CsvRow>
    {
        readonly Dictionary<string, int> headers;
        readonly List<List<string>> rows = new List<List<string>>();

        public Dictionary<string, int> Headers { get { return headers; } }

        protected CsvTable() {}


        public CsvTable(Stream csvText, bool hasHeaders, bool trim = true, char delimiter = ';', string encoding = "windows-1250") {
            var lines = new StreamReader(csvText, Encoding.GetEncoding(encoding)).ReadToEnd()
                                                                                 .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (hasHeaders) {
                headers = new Dictionary<string, int>();
                var key = 0;
                foreach (var c in ParseCsvLine(lines.First(), delimiter)) headers[c.Trim()] = key++;
            }
            foreach (var r in lines.Skip(hasHeaders ? 1 : 0)) rows.Add(ParseCsvLine(r, delimiter));
        }

        public static List<string> ParseCsvLine(string line, char delimiter = ';') {
            var ret = new List<string>();
            var pos = 0;
            while (pos < line.Length) {
                string value;

                // Special handling for quoted field
                if (line[pos] == '"') {
                    // Skip initial quote
                    pos++;

                    // Parse quoted value
                    var start = pos;
                    while (pos < line.Length) {
                        // Test for quote character
                        if (line[pos] == '"') {
                            // Found one
                            pos++;

                            // If two quotes together, keep one
                            // Otherwise, indicates end of value
                            if (pos >= line.Length || line[pos] != '"') {
                                pos--;
                                break;
                            }
                        }
                        pos++;
                    }
                    value = line.Substring(start, pos - start);
                    value = value.Replace("\"\"", "\"");
                }
                else {
                    // Parse unquoted value
                    var start = pos;
                    while (pos < line.Length && line[pos] != delimiter) pos++;
                    value = line.Substring(start, pos - start);
                }

                // Add field to list
                ret.Add(value);

                // Eat up to and including next comma
                while (pos < line.Length && line[pos] != delimiter) pos++;
                if (pos < line.Length) pos++;
            }
            return ret;
        }


        public IEnumerator<CsvRow> GetEnumerator() {
            foreach (var row in rows) yield return new CsvRow(row, headers);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}