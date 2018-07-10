using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CTL
{
    public class Document
    { 
        public int[] Words;
        public int Length;
        public int[] Authors;
        public int[] SourceAuthors;
        public int[] TargetAuthors;
        public Random rnd;

        public void Init(string paper, string authors)
        {
            try
            {
                //string sp = @"\s+";
                //this.Words = Regex.Split(paper, sp).Select(x => Convert.ToInt32(x)).ToArray();

                rnd = new Random();

                this.Words = paper.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Convert.ToInt32(x)).ToArray();
                this.Length = Words.Length;

                var tempAuthor = authors.Split('|');

                if (!string.IsNullOrEmpty(tempAuthor[0]))
                {
                    this.SourceAuthors = Regex.Split(tempAuthor[0], ",").Select(x => Convert.ToInt32(x)).ToArray();
                }
                else
                {
                    this.SourceAuthors = new int[0];
                }

                if (!string.IsNullOrEmpty(tempAuthor[1]))
                {
                    this.TargetAuthors = Regex.Split(tempAuthor[1], ",").Select(x => Convert.ToInt32(x)).ToArray();
                }
                else
                {
                    this.TargetAuthors = new int[0];
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Init(string str, WordDictionary WD)
        {
            try
            {
                string sp = @"\s+";
                string[] doc = Regex.Split(str, sp);
                Words = new int[doc.Length];

                for (int i = 0; i < Words.Length; i++)
                {
                    Words[i] = WD.GetWords(doc[i]);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public int RandomAuthorID
        {
            get
            {
                if (this.SourceAuthors.Length != 0)
                {
                    return this.RandomSourceAuthorID;
                }
                else
                {
                    return this.RandomTargetAuthorID;
                }
            }
        }

        public int RandomSourceAuthorID
        {
            get
            {
                int temp = rnd.Next(this.SourceAuthors.Length);
                return this.SourceAuthors[temp];
            }
        }

        public int RandomTargetAuthorID
        {
            get
            {
                int temp = rnd.Next(this.TargetAuthors.Length);
                return this.TargetAuthors[temp];
            }
        }
    }
}
