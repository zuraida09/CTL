using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.IO;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("test");

            PrepareDataType2("Medical Informatics", "Database", 2001);
            PrepareDataType2("Medical Informatics", "Data Mining", 2001);
            PrepareDataType2("Data Mining", "Theory", 2001);
            PrepareDataType2("Visualization", "Data Mining", 2001);

            //PrepareData("Medical Informatics", "Database");
            //PrepareData("Medical Informatics", "Data Mining");
            //PrepareData("Data Mining", "Theory");
            //PrepareData("Visualization", "Data Mining");

            Console.WriteLine("Finished");
            Console.ReadLine();
        }

        private static void PrepareDataType2(string sourceDomain, string targetDomain, int trainingLimit)
        {
            MySqlConnection con = new MySqlConnection("host=localhost;user=root;password=password0!;database=aminer_big;");
            con.Open();

            #region Author Source
            //string sql = "SELECT distinct(authorNamePlain) FROM aminer_big.temp_paperauthors where domainName = '" + sourceDomain + "' and year < " + trainingLimit;
            string sql = "SELECT distinct(authorNamePlain) FROM aminer_big.temp_paperauthors where domainName = '" + sourceDomain + "'";
            MySqlCommand cmd = new MySqlCommand(sql, con);
            MySqlDataReader reader = cmd.ExecuteReader();

            AuthorDictionary sourceAD = new AuthorDictionary();
            List<string> sourceAuthors = new List<string>();
            string tempAuthor;
            int authorID;

            while (reader.Read())
            {
                tempAuthor = reader.GetString("authorNamePlain");
                sourceAuthors.Add(tempAuthor);
                authorID = sourceAD.GetAuthor(tempAuthor);
            }

            con.Close();
            con.Open();

            Console.WriteLine(sourceDomain + " authors: " + sourceAuthors.Count);
            #endregion

            #region Author Target
            //sql = "SELECT distinct(authorNamePlain) FROM aminer_big.temp_paperauthors where domainName = '" + targetDomain + "' and year < " + trainingLimit;
            sql = "SELECT distinct(authorNamePlain) FROM aminer_big.temp_paperauthors where domainName = '" + targetDomain + "'";
            cmd = new MySqlCommand(sql, con);
            reader = cmd.ExecuteReader();

            AuthorDictionary targetAD = new AuthorDictionary();
            List<string> targetAuthors = new List<string>();

            while (reader.Read())
            {
                tempAuthor = reader.GetString("authorNamePlain");
                targetAuthors.Add(tempAuthor);
                authorID = targetAD.GetAuthor(tempAuthor);
            }

            con.Close();
            con.Open();

            Console.WriteLine(targetDomain + " authors: " + targetAuthors.Count);
            #endregion

            #region Paper
            sql = "SELECT paperID, category, temp_paperauthors.year, authorNamePlain, title, abstract FROM aminer_big.temp_paperauthors inner join m_topicpaperauthor on paperID = id where (domainName = '" + sourceDomain + "' or domainName = '" + targetDomain + "')";
            cmd = new MySqlCommand(sql, con);
            reader = cmd.ExecuteReader();

            List<Paper> papers = new List<Paper>();
            int tempPaperID = 0;
            int sourceAuthorTemp;
            int targetAuthorTemp;
            Paper paper = new Paper(tempPaperID, 0, string.Empty, string.Empty);

            while (reader.Read())
            {
                int paperID = Convert.ToInt32(reader.GetString("paperID"));

                if (paperID != tempPaperID)
                {
                    string titleAndAbstract = reader.GetString("title") + " " + reader.GetString("abstract");
                    string paperDomain = reader.GetString("category");
                    int year = Convert.ToInt32(reader.GetString("year"));

                    paper = new Paper(paperID, year, paperDomain, titleAndAbstract);
                    papers.Add(paper);
                    tempPaperID = paperID;
                }

                string author = reader.GetString("authorNamePlain");
                paper.Authors.Add(author);

                sourceAuthorTemp = sourceAD.GetAuthorID(author);
                targetAuthorTemp = targetAD.GetAuthorID(author);

                if (sourceAuthorTemp != -1)
                {
                    paper.SourceAuthorIDs.Add(sourceAuthorTemp);
                }

                if (targetAuthorTemp != -1)
                {
                    paper.TargetAuthorIDs.Add(targetAuthorTemp);
                }

                if (sourceAuthorTemp == -1 && targetAuthorTemp == -1)
                {
                    Console.WriteLine("==========>>>" + author);
                }
            }

            con.Close();
            #endregion

            var sourcePapers = papers.Where(x => x.PaperDomain == sourceDomain);
            var targetPapers = papers.Where(x => x.PaperDomain == targetDomain);
            var collaborationPapers = papers.Where(x => x.SourceAuthorIDs.Count >= 1 && x.TargetAuthorIDs.Count >= 1);
            var collaborationPapersTraining = collaborationPapers.Where(x => x.Year < trainingLimit);
            var collaborationPapersTesting = collaborationPapers.Where(x => x.Year >= trainingLimit);
            var undetected = papers.Where(x => x.SourceAuthorIDs.Count == 0 && x.TargetAuthorIDs.Count == 0);

            Console.WriteLine("all: " + papers.Count);
            Console.WriteLine(sourceDomain + ": " + sourcePapers.Count());
            Console.WriteLine(targetDomain + ": " + targetPapers.Count());
            Console.WriteLine("coll: " + collaborationPapers.Count() + " - " + collaborationPapers.Where(x => x.Authors.Count > 1).Count());
            Console.WriteLine("========= UNIDENTIFIED DOMAIN: " + undetected.Count());

            Console.WriteLine("Collaboration training: " + collaborationPapersTraining.Count());
            Console.WriteLine("Collaboration testing: " + collaborationPapersTesting.Count());

            string folderName = sourceDomain + " - " + targetDomain;
            System.IO.Directory.CreateDirectory(folderName);

            #region Data Training
            using (StreamWriter file = new StreamWriter(folderName + "/info.txt"))
            {
                file.WriteLine(sourceAuthors.Count);
                file.WriteLine(targetAuthors.Count);
                file.WriteLine(sourcePapers.Count());
                file.WriteLine(targetPapers.Count());
                file.WriteLine(collaborationPapersTraining.Count());
                file.WriteLine(collaborationPapersTesting.Count());
            }

            using (StreamWriter file = new StreamWriter(folderName + "/sourceAuthorIDs.txt"))
            {
                foreach (var item in sourceAD.Author2Id)
                {
                    file.WriteLine(item.Value + " " + item.Key);
                }
            }

            using (StreamWriter file = new StreamWriter(folderName + "/targetAuthorIDs.txt"))
            {
                foreach (var item in targetAD.Author2Id)
                {
                    file.WriteLine(item.Value + " " + item.Key);
                }
            }

            using (StreamWriter file = new StreamWriter(folderName + "/papers.txt"))
            {
                foreach (var item in sourcePapers)
                {
                    file.WriteLine(item.TitleAndAbstract);
                }

                foreach (var item in targetPapers)
                {
                    file.WriteLine(item.TitleAndAbstract);
                }

                foreach (var item in collaborationPapersTraining)
                {
                    file.WriteLine(item.TitleAndAbstract);
                }
            }

            using (StreamWriter file = new StreamWriter(folderName + "/authors.txt"))
            {
                foreach (var item in sourcePapers)
                {
                    file.WriteLine(item.AllAuthors);
                }

                foreach (var item in targetPapers)
                {
                    file.WriteLine(item.AllAuthors);
                }

                foreach (var item in collaborationPapersTraining)
                {
                    file.WriteLine(item.AllAuthors + " - " + item.Year);
                }
            }
            #endregion

            #region Data Testing
            List<AuthorPair> authorPairs = new List<AuthorPair>();

            foreach (var item in collaborationPapersTesting)
            {
                foreach (var sourceAuth in item.SourceAuthorIDs)
                {
                    foreach (var targetAuth in item.TargetAuthorIDs)
                    {
                        authorPairs.Add(new AuthorPair(sourceAuth, targetAuth));
                    }
                }
            }

            var authorMapping = authorPairs.GroupBy(x => x.SourceAuthorID).Select(g => new Collaborations(g.Key, g.Select(x => x.TargetAuthorID).Distinct().OrderBy(x => x).ToList())).OrderBy(x => x.SourceAuthorID);

            using (StreamWriter file = new StreamWriter(folderName + "/testData.txt"))
            {
                foreach (var item in authorMapping)
                {
                    file.WriteLine(item.SourceAuthorID + " " + string.Join(",", item.Collaborators));
                }
            }
            #endregion

            #region check intersection data
            var collAuthorTraining = new List<int>();

            foreach (var item in collaborationPapersTraining)
            {
                collAuthorTraining.AddRange(item.SourceAuthorIDs);
            }

            collAuthorTraining = collAuthorTraining.Distinct().ToList();
            var collAuthorTesting = authorMapping.Where(x => x.Collaborators.Count() >= 5).Select(x => x.SourceAuthorID).ToList();
            var both = collAuthorTesting.Intersect(collAuthorTraining);

            Console.WriteLine("coll author training: " + collAuthorTraining.Count);
            Console.WriteLine("coll author testing dengan kolaborator minimal 5: " + collAuthorTesting.Count);
            Console.WriteLine("intersection: " + both.Count());
            #endregion

            Console.WriteLine(Environment.NewLine);

            #region check author with no single domain data
            //List<string> singleDomain = new List<string>();
            //List<string> collaboration = new List<string>();

            //foreach (var item in sourcePapers)
            //{
            //    singleDomain.AddRange(item.Authors);
            //}

            //foreach (var item in targetPapers)
            //{
            //    singleDomain.AddRange(item.Authors);
            //}

            //foreach (var item in collaborationPapers)
            //{
            //    collaboration.AddRange(item.Authors);
            //}

            //IEnumerable<string> differenceQuery = collaboration.Except(singleDomain);
            //Console.WriteLine("Authors in collaboration but not singleDomain " + differenceQuery.Count());

            //foreach (string s in differenceQuery)
            //{
            //    Console.WriteLine(s);
            //}
            #endregion
        }

        private static void PrepareData(string sourceDomain, string targetDomain)
        {
            MySqlConnection con = new MySqlConnection("host=localhost;user=root;password=password0!;database=aminer_big;");
            con.Open();

            string sql = "SELECT distinct(authorNamePlain) FROM aminer_big.temp_paperauthors where domainName = '" + sourceDomain + "'";
            MySqlCommand cmd = new MySqlCommand(sql, con);
            MySqlDataReader reader = cmd.ExecuteReader();

            AuthorDictionary sourceAD = new AuthorDictionary();
            List<string> sourceAuthors = new List<string>();
            string tempAuthor;
            int authorID;

            while (reader.Read())
            {
                tempAuthor = reader.GetString("authorNamePlain");
                sourceAuthors.Add(tempAuthor);
                authorID = sourceAD.GetAuthor(tempAuthor);
            }

            con.Close();
            con.Open();

            Console.WriteLine(sourceDomain + " authors: " + sourceAuthors.Count);

            sql = "SELECT distinct(authorNamePlain) FROM aminer_big.temp_paperauthors where domainName = '" + targetDomain + "'";
            cmd = new MySqlCommand(sql, con);
            reader = cmd.ExecuteReader();

            AuthorDictionary targetAD = new AuthorDictionary();
            List<string> targetAuthors = new List<string>();

            while (reader.Read())
            {
                tempAuthor = reader.GetString("authorNamePlain");
                targetAuthors.Add(tempAuthor);
                authorID = targetAD.GetAuthor(tempAuthor);
            }

            con.Close();
            con.Open();

            Console.WriteLine(targetDomain + " authors: " + targetAuthors.Count);

            sql = "SELECT paperID, category, authorNamePlain, title, abstract FROM aminer_big.temp_paperauthors inner join m_topicpaperauthor on paperID = id where domainName = '" + sourceDomain + "' or domainName = '" + targetDomain + "'";
            cmd = new MySqlCommand(sql, con);
            reader = cmd.ExecuteReader();

            List<Paper> papers = new List<Paper>();
            int tempPaperID = 0;
            int sourceAuthorTemp;
            int targetAuthorTemp;
            Paper paper = new Paper(tempPaperID, 0, string.Empty, string.Empty);

            while (reader.Read())
            {
                int paperID = Convert.ToInt32(reader.GetString("paperID"));

                if (paperID != tempPaperID)
                {
                    string titleAndAbstract = reader.GetString("title") + " " + reader.GetString("abstract");
                    string paperDomain = reader.GetString("category");
                    int year = Convert.ToInt32(reader.GetString("year"));

                    paper = new Paper(paperID, year, paperDomain, titleAndAbstract);
                    papers.Add(paper);
                    tempPaperID = paperID;
                }

                string author = reader.GetString("authorNamePlain");
                paper.Authors.Add(author);

                sourceAuthorTemp = sourceAD.GetAuthorID(author);
                targetAuthorTemp = targetAD.GetAuthorID(author);

                if (sourceAuthorTemp != -1)
                {
                    paper.SourceAuthorIDs.Add(sourceAuthorTemp);
                }

                if (targetAuthorTemp != -1)
                {
                    paper.TargetAuthorIDs.Add(targetAuthorTemp);
                }

                if (sourceAuthorTemp == -1 && targetAuthorTemp == -1)
                {
                    Console.WriteLine("==========>>>" + author);
                }
            }

            con.Close();

            var paper1Author = papers.Where(x => x.Authors.Count == 1);
            var sourcePapers = papers.Where(x => x.SourceAuthorIDs.Count >= 1 && x.TargetAuthorIDs.Count == 0);
            var targetPapers = papers.Where(x => x.SourceAuthorIDs.Count == 0 && x.TargetAuthorIDs.Count >= 1);
            var collaborationPapers = papers.Where(x => x.SourceAuthorIDs.Count >= 1 && x.TargetAuthorIDs.Count >= 1);
            var undetected = papers.Where(x => x.SourceAuthorIDs.Count == 0 && x.TargetAuthorIDs.Count == 0);

            Console.WriteLine("all: " + papers.Count);
            Console.WriteLine(sourceDomain + ": " + sourcePapers.Count());
            Console.WriteLine(targetDomain + ": " + targetPapers.Count());
            Console.WriteLine("coll: " + collaborationPapers.Count() + " - " + collaborationPapers.Where(x => x.Authors.Count > 1).Count());
            Console.WriteLine("========= UNIDENTIFIED DOMAIN: " + undetected.Count());

            using (StreamWriter file = new StreamWriter("info.txt"))
            {
                file.WriteLine(sourceAuthors.Count);
                file.WriteLine(targetAuthors.Count);
                file.WriteLine(sourcePapers.Count());
                file.WriteLine(targetPapers.Count());
                file.WriteLine(collaborationPapers.Count());
            }

            using (StreamWriter file = new StreamWriter("papers.txt"))
            {
                foreach (var item in sourcePapers)
                {
                    file.WriteLine(item.TitleAndAbstract);
                }

                foreach (var item in targetPapers)
                {
                    file.WriteLine(item.TitleAndAbstract);
                }

                foreach (var item in collaborationPapers)
                {
                    file.WriteLine(item.TitleAndAbstract);
                }
            }

            using (StreamWriter file = new StreamWriter("authors.txt"))
            {
                foreach (var item in sourcePapers)
                {
                    file.WriteLine(item.AllAuthors);
                }

                foreach (var item in targetPapers)
                {
                    file.WriteLine(item.AllAuthors);
                }

                foreach (var item in collaborationPapers)
                {
                    file.WriteLine(item.AllAuthors);
                }
            }

            #region check author with no single domain data
            //List<string> singleDomain = new List<string>();
            //List<string> collaboration = new List<string>();

            //foreach (var item in sourcePapers)
            //{
            //    singleDomain.AddRange(item.Authors);
            //}

            //foreach (var item in targetPapers)
            //{
            //    singleDomain.AddRange(item.Authors);
            //}

            //foreach (var item in collaborationPapers)
            //{
            //    collaboration.AddRange(item.Authors);
            //}

            //IEnumerable<string> differenceQuery = collaboration.Except(singleDomain);
            //Console.WriteLine("Authors in collaboration but not singleDomain " + differenceQuery.Count());

            //foreach (string s in differenceQuery)
            //{
            //    Console.WriteLine(s);
            //}
            #endregion
        }
    }
}

public class Paper
{
    public int ID { get; set; }
    public int Year { get; set; }
    public string PaperDomain { get; set; }
    public string TitleAndAbstract { get; set; }
    public List<string> Authors { get; set; }
    public List<string> Domains { get; set; }
    public List<int> SourceAuthorIDs { get; set; }
    public List<int> TargetAuthorIDs { get; set; }

    public Paper(int ID, int year, string paperDomain, string titleAndAbstract)
    {
        this.ID = ID;
        this.PaperDomain = paperDomain;
        this.TitleAndAbstract = titleAndAbstract;
        this.Year = year;
        this.Authors = new List<string>();
        this.SourceAuthorIDs = new List<int>();
        this.TargetAuthorIDs = new List<int>();
        this.Domains = new List<string>();
    }

    public int JumlahDomain
    {
        get
        {
            return this.Domains.Distinct().Count();
        }
    }

    public string AllAuthors
    {
        get
        {
            return string.Join(",", this.SourceAuthorIDs) + "|" + string.Join(",", this.TargetAuthorIDs);
        }
    }
}

public class AuthorPair
{
    public int SourceAuthorID { get; set; }
    public int TargetAuthorID { get; set; }

    public AuthorPair(int sourceAuthorID, int targetAuthorID)
    {
        this.SourceAuthorID = sourceAuthorID;
        this.TargetAuthorID = targetAuthorID;
    }
}

public class Collaborations
{
    public int SourceAuthorID { get; set; }
    public List<int> Collaborators { get; set; }

    public Collaborations(int sourceAuthorID, List<int> collaborators)
    {
        this.SourceAuthorID = sourceAuthorID;
        this.Collaborators = collaborators;
    }
}

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

    public int GetAuthorID(string str)
    {
        if (Author2Id.ContainsKey(str))
        {
            return Author2Id[str];
        }
        else
        {
            return -1;
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

