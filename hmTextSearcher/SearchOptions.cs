using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace hmTextSearcher
{
    public class SearchOptions
    {
        public enum SearchType { HDD, DataBase}

        public SearchType Type { set; get; } = SearchType.HDD;

        public string ConnString { set; get; }

        public string[] Patterns { set; get; } = { "*.*" };

        public string Path { set; get; }

        public bool isRecursive { set; get; }

        public bool isMatchCase { set; get; }

        public string Text { set; get; }

        public long MaxSize { set; get; }

        public override bool Equals(object obj)
        {
            var item = obj as SearchOptions;

            if (item == null)
                return false;

            return
                // compare patterns without considering the order
                new HashSet<string>(this.Patterns).SetEquals(item.Patterns) &&

                this.Path.ToLower().Equals(item.Path.ToLower()) &&
                this.isRecursive.Equals(item.isRecursive) &&
                this.isMatchCase.Equals(item.isMatchCase) &&
                this.Text.ToLower().Equals(item.Text.ToLower()) &&
                this.MaxSize.Equals(item.MaxSize);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return
                    this.Path.GetHashCode() ^
                    this.isRecursive.GetHashCode() ^
                    this.isMatchCase.GetHashCode() ^
                    this.Text.GetHashCode() ^
                    this.MaxSize.GetHashCode();
            }
        }

        public string GetDbName()
        {
            const string s = "Initial Catalog=";

            int i = this.ConnString.IndexOf(s);

            int endIndex = this.ConnString.IndexOf(';', i);
            int startIndex = i + s.Length;

            if (i != -1)
                return this.ConnString.Substring(startIndex, endIndex - startIndex);

            return null;
        }
    }
}
