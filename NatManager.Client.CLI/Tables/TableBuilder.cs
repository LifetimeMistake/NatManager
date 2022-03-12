﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NatManager.Client.CLI.Tables
{
    // Credit: https://stackoverflow.com/a/14698822
    public class TableBuilder : IEnumerable<ITextRow>
    {
        protected class TextRow : List<string>, ITextRow
        {
            protected TableBuilder? owner;
            public TextRow(TableBuilder Owner)
            {
                owner = Owner;
                if (owner == null) throw new ArgumentException("Owner");
            }

            public string Output()
            {
                StringBuilder sb = new StringBuilder();
                Output(sb);
                return sb.ToString();
            }

            public void Output(StringBuilder sb)
            {
                if (owner != null)
                    sb.AppendFormat(owner.FormatString, this.ToArray());
            }
            public object Tag { get; set; }
        }

        public string Separator { get; set; }

        protected List<ITextRow> rows = new List<ITextRow>();
        protected List<int> colLength = new List<int>();

        public TableBuilder()
        {
            Separator = "  ";
        }

        public TableBuilder(string separator) : this()
        {
            Separator = separator;
        }

        public void AddHeader(params object[] cols)
        {
            string[] hypenRow = new string[cols.Length];
            for (int i = 0; i < cols.Length; i++)
            {
                object o = cols[i];
                int length = o.ToString().Length;
                hypenRow[i] = new string(Enumerable.Repeat('-', length).ToArray());
            }

            AddRow(cols);
            AddRow(hypenRow);
        }

        public ITextRow AddRow(params object[] cols)
        {
            TextRow row = new TextRow(this);
            foreach (object o in cols)
            {
                string str = o.ToString().Trim();
                row.Add(str);
                if (colLength.Count >= row.Count)
                {
                    int curLength = colLength[row.Count - 1];
                    if (str.Length > curLength) colLength[row.Count - 1] = str.Length;
                }
                else
                {
                    colLength.Add(str.Length);
                }
            }
            rows.Add(row);
            return row;
        }

        protected string? _fmtString = null;
        public string FormatString
        {
            get
            {
                if (_fmtString == null)
                {
                    string format = "";
                    int i = 0;
                    foreach (int len in colLength)
                    {
                        format += string.Format("{{{0},-{1}}}{2}", i++, len, Separator);
                    }
                    format += "\r\n";
                    _fmtString = format;
                }
                return _fmtString;
            }
        }

        public string Output()
        {
            StringBuilder sb = new StringBuilder();
            foreach (TextRow row in rows)
            {
                row.Output(sb);
            }
            return sb.ToString();
        }

        #region IEnumerable Members

        public IEnumerator<ITextRow> GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return rows.GetEnumerator();
        }

        #endregion
    }
}
