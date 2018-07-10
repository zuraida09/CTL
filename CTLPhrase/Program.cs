using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using CommandLine;

// code https://github.com/Aixile/LDA_CGS

namespace CTLPhrase
{
    public class Program
    {
        static private CommandLineOption GetDefaultOption()
        {
            CommandLineOption option = new CommandLineOption();
            option.alpha = 0.1;
            option.beta = 0.01;
            option.topics = 2;
            option.savestep = 1000;
            option.niters = 1000;                //1000; GANTIIIIIIIIIII PLUS VOCAB SIZE
            option.twords = 3;
            option.info = "info.txt";
            option.papers = "papers.txt";
            option.authors = "authors.txt";
            return option;
        }

        static void Main(string[] args)
        {
            CommandLineOption opt = GetDefaultOption();
            Parser parser = new Parser();
            var stopwatch = new Stopwatch();

            try
            {
                parser.ParseArguments(args, opt);

                string[] info = File.ReadAllLines(opt.info);

                int sourceAuthorNum = Convert.ToInt32(info[0]);
                int targetAuthorNum = Convert.ToInt32(info[1]);
                int sourcePaperNum = Convert.ToInt32(info[2]);
                int targetPaperNum = Convert.ToInt32(info[3]);
                int collaborationPaperNum = Convert.ToInt32(info[4]);

                List<string> papers = File.ReadAllLines(opt.papers).ToList();
                List<string> authors = File.ReadAllLines(opt.authors).ToList();

                var sourcePapers = papers.Take(sourcePaperNum).ToArray();
                var targetPapers = papers.Skip(sourcePaperNum).Take(targetPaperNum).ToArray();
                var collaborationPapers = papers.Skip(sourcePaperNum + targetPaperNum).ToArray();

                var sourceAuthors = authors.Take(sourcePaperNum).ToArray();
                var targetAuthors = authors.Skip(sourcePaperNum).Take(targetPaperNum).ToArray();
                var collaborationAuthors = authors.Skip(sourcePaperNum + targetPaperNum).ToArray();

                int vocabSize = 12;

                Corpora sourceCor = new Corpora(vocabSize, sourceAuthorNum);
                sourceCor.LoadDataFile(sourcePapers, sourceAuthors);
                ACTPhraseSampling sourceModel = new ACTPhraseSampling();
                sourceModel.TrainNewModel(sourceCor, opt, "outSource.txt");

                Corpora targetCor = new Corpora(vocabSize, targetAuthorNum);
                targetCor.LoadDataFile(targetPapers, targetAuthors);
                ACTPhraseSampling targetModel = new ACTPhraseSampling();
                targetModel.TrainNewModel(targetCor, opt, "outTarget.txt");

                Corpora collaborationCor = new Corpora(vocabSize, 0);
                collaborationCor.LoadDataFile(collaborationPapers, collaborationAuthors);
                CTLPhraseSampling collaborationModel = new CTLPhraseSampling(sourceModel, targetModel);
                collaborationModel.TrainNewModel(collaborationCor, opt, "outColl.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
