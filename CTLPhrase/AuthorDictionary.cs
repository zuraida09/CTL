using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTLPhrase
{
    public class AuthorDictionary
    {
        public Dictionary<string, int> Author2Id;
        public List<string> Authors;
        public int Count;

        public AuthorDictionary()
        {
            Author2Id = new Dictionary<string, int>();
            Authors = new List<string>();
            Count = 0;
        }

        public string GetString(int id)
        {
            if (id > Count) return null;
            return Authors[id];
        }

        public int GetAuthor(string str)
        {
            if (Author2Id.ContainsKey(str))
            {
                return Author2Id[str];
            }
            else
            {
                return AddAuthor(str);
            }
        }

        public int AddAuthor(string str)
        {
            if (!Author2Id.ContainsKey(str))
            {
                Authors.Add(str);
                Author2Id[str] = Count;
                Count++;
                return Count - 1;
            }

            return -1;
        }
    }
}
