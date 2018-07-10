using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Evaluation
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                GetGroundTruth();

                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
        }

        private static void GetGroundTruth()
        {
            string[] groundTruth = File.ReadAllLines("testData.txt");
            string[] trainingResult = File.ReadAllLines("rank.txt");
            Collaboration coll;
            string[] temp;
            int sourceAuthorID;
            List<string> collaborators;

            for (int i = 0; i < groundTruth.Length; i++)
            {
                temp = groundTruth[i].Split(' ');
                sourceAuthorID = Convert.ToInt32(temp[0]);
                collaborators = temp[1].Split(',').ToList();

                coll = new Collaboration(sourceAuthorID);

                coll.Collaborators.AddRange(collaborators);
                coll.Recommendations.AddRange(trainingResult[i].Split(',').ToList());
            }
        }
    }

    public class Collaboration
    {
        public int SourceAuthorID { get; set; }
        public List<string> Collaborators { get; set; }
        public List<string> Recommendations { get; set; }

        public Collaboration(int sourceAuthorID)
        {
            this.SourceAuthorID = sourceAuthorID;
            this.Collaborators = new List<string>();
            this.Recommendations = new List<string>();
        }
    }
}
