using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CTL
{
    public class Corpora
    {
        public int totalWords;
        public int totalDocuments;
        public int VocabSize;
        public int TotalAuthor;
        public Document[] Docs;
        //public WordDictionary WD;
        //public AuthorDictionary AD;

        public Corpora(int vocabSize, int totalAuthor)
        {
            //WD = new WordDictionary();
            //AD = new AuthorDictionary();
            totalDocuments = 0;
            totalWords = 0;
            this.VocabSize = vocabSize;
            this.TotalAuthor = totalAuthor;
        }

        public void LoadDataFile(string[] papers, string[] authors)
        {
            try
            {
                totalDocuments = papers.Length;
                Docs = new Document[totalDocuments];

                for (int i = 0; i < totalDocuments; i++)
                {
                    Docs[i] = new Document();
                    Docs[i].Init(papers[i], authors[i]);

                    totalWords += Docs[i].Length;
                }                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        public void LoadDataFileCollaboration(string fileAuthor, string fileDocuments, AuthorDictionary sourceAuthor, AuthorDictionary targetAuthor)
        {
            try
            {
                string[] f = File.ReadAllLines(fileDocuments);
                totalDocuments = f.Length;
                Docs = new Document[totalDocuments];

                for (int i = 0; i < totalDocuments; i++)
                {
                    Docs[i] = new Document();
                    //Docs[i].Init(f[i], WD);   ???????????????????????
                    totalWords += Docs[i].Length;
                }

                string[] a = File.ReadAllLines(fileAuthor);
                int totalRowAuthors = a.Length;

                if (totalRowAuthors != totalDocuments)
                {
                    Console.WriteLine("Unmatched number of documents and authors!!!");
                }
                
                for (int i = 0; i < totalRowAuthors; i++)
                {
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw ex;
            }
        }

        public string GetStringByID(int id)
        {
            //return WD.GetString(id);
            return string.Empty;
        }
    }
}
