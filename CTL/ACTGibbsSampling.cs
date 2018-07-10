using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;

namespace CTL
{
    public class ACTGibbsSampling : LDA
    {
        protected int savestep;
        protected int niters;
        protected string outputfile;
        protected int twords;

        [JsonIgnore] public Corpora cor;

        public int[][] zx;
        public int[][] zv;
        public int[] zxsum;
        public int[] zvsum;

        protected double[] p;
        protected Random rnd;

        public ACTGibbsSampling()
        {
            M = 0;
            Voc = 0;
            K = 10;
            alpha = 0.1;
            beta = 0.1;

            rnd = new Random();
        }

        public void InitOption(CommandLineOption opt)
        {
            try
            {
                K = opt.topics;
                alpha = opt.alpha;
                beta = opt.beta;
                savestep = opt.savestep;
                niters = opt.niters;
                twords = opt.twords;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void InitModel(Corpora cor)
        {
            this.cor = cor;

            M = cor.totalDocuments;                         //jumlah dokumen
            Voc = cor.VocabSize;                            //jumlah kata di vocab
            V = cor.TotalAuthor;                            //jumlah author

            Console.WriteLine(V);

            p = new double[K];

            zx = new int[Voc][];                            //kata-topik
            zv = new int[V][];                              //author-topik

            for (int w = 0; w < Voc; w++)
            {
                zx[w] = new int[K];
            }

            for (int v = 0; v < V; v++)
            {
                zv[v] = new int[K];
            }

            zxsum = new int[K];                             //jumlah kata untuk setiap topik
            zvsum = new int[V];                             //jumlah topik untuk setiap author

            words = new int[cor.totalWords];                //sejumlah token pada korpus, isinya wordID sesuai vocab
            docs = new int[cor.totalWords];                  //docID untuk setiap kata
            authors = new int[cor.totalWords];              //authorID untuk setiap kata
            z = new int[cor.totalWords];                    //topik untuk setiap kata
            wn = 0;

            for (int i = 0; i < M; i++)                     //iterate dokumen
            {
                int l = cor.Docs[i].Length;

                for (int j = 0; j < l; j++)                 //iterate token pada dokumen
                {
                    words[wn] = cor.Docs[i].Words[j];       //menyimpan wordID dari vocab untuk setiap kata

                    int author = cor.Docs[i].RandomAuthorID;
                    authors[wn] = author;
                    zvsum[author] += 1;                         //jumlah topik pada author diinisialisasi sama dengan jumlah kata
                    
                    docs[wn] = i;
                    wn++;
                }
            }

            for (int i = 0; i < wn; i++)
            {
                int topic = rnd.Next(K);                    //select random topik untuk tiap kata lalu update nilai statistik 
                zx[words[i]][topic] += 1;
                zv[authors[i]][topic] += 1;
                zxsum[topic] += 1;
                z[i] = topic;
            }

            theta = new double[V][];                        //theta untuk author topik --> dimensi nya kebalik sama nd

            for (int v = 0; v < V; v++)
            {
                theta[v] = new double[K];
            }

            phi = new double[K][];                          //phi untuk topik kata 

            for (int k = 0; k < K; k++)
            {
                phi[k] = new double[Voc];
            }
        }

        public void TrainNewModel(Corpora cor, CommandLineOption opt, string outputFile)
        {
            this.outputfile = outputFile;

            InitOption(opt);
            InitModel(cor);
            PrintModelInfo();
            GibbsSampling(niters);
        }

        public void PrintModelInfo()
        {
            Console.WriteLine("Aplha: " + alpha.ToString());
            Console.WriteLine("Beta: " + beta.ToString());
            Console.WriteLine("M: " + M);
            Console.WriteLine("K: " + K);
            Console.WriteLine("V: " + V);
            Console.WriteLine("Words in Vocab: " + Voc);
            Console.WriteLine("Total iterations:" + niters);
            Console.WriteLine("Save at: " + savestep);
            Console.WriteLine();
        }

        private void GibbsSampling(int totalIter)
        {
            for (int iter = 1; iter <= totalIter; iter++)
            {
                //Console.Write("Iteration " + iter + ":");
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                for (int i = 0; i < wn; i++)
                {
                    int topic = DoSampling(i);
                    z[i] = topic;
                }

                stopWatch.Stop();
                //Console.WriteLine(stopWatch.ElapsedMilliseconds / 1000.0 + " seconds");

                if (iter == 1 || iter % savestep == 0)
                {
                    SaveModel(outputfile + "." + iter.ToString() + ".json");
                    SaveTopWords(outputfile + "." + iter.ToString() + ".topwords");
                    Console.WriteLine("LogLikelihood= " + LogLikelihood);
                }
            }
        }

        private int DoSampling(int i)
        {
            int oldZ = z[i];                    //topik lama
            int w = words[i];                   //wordID
            int v = authors[i];                 //authorID
            int docIndex = docs[i];             //docID
            
            zx[w][oldZ] -= 1;                   //kurangi dari statistik
            zv[v][oldZ] -= 1;
            zxsum[oldZ] -= 1;
            zvsum[v] -= 1;
            
            v = cor.Docs[docIndex].RandomAuthorID;
            authors[i] = v;

            double Vocbeta = Voc * beta;
            double Kalpha = K * alpha;

            for (int k = 0; k < K; k++)         //multinomial sampling via cummulative method
            {
                p[k] = (zx[w][k] + beta) / (zxsum[k] + Vocbeta) * (zv[v][k] + alpha) / (zvsum[v] + Kalpha);
            }
            
            for (int k = 1; k < K; k++)         //cummulate multinomial parameters
            {
                p[k] += p[k - 1];
            }

            double cp = rnd.NextDouble() * p[K - 1];    //scaled sample because of unnormalized p[]

            int newZ;

            for (newZ = 0; newZ < K; newZ++)
            {
                if (p[newZ] > cp)
                {
                    break;
                }
            }

            if (newZ == K) newZ--;

            zx[w][newZ] += 1;
            zv[v][newZ] += 1;
            zxsum[newZ] += 1;
            zvsum[v] += 1;

            return newZ;
        }

        public void SaveModel(string modelpath)
        {
            CalcParameter();
            string jstr = GetJSONString();

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(modelpath))
            {
                sw.WriteLine(jstr);
            }
        }

        public void SaveTopWords(string modelpath)
        {
            int tw = twords > Voc ? Voc : twords;

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(modelpath))
            {
                for (int k = 0; k < K; k++)
                {
                    var wordsProbsList = new Dictionary<int, double>();

                    for (int w = 0; w < Voc; w++)
                    {
                        wordsProbsList.Add(w, phi[k][w]);
                    }

                    double ans = 0;
                    for (int w = 0; w < Voc; w++)
                    {
                        ans += phi[k][w];
                    }

                    if (Math.Abs(ans - 1.00) > 0.1)
                    {
                        throw (new Exception("Phi Calculation Error"));
                    }

                    sw.Write("Topic " + k + "th:\n");
                    var wordsProbsListOrdered = wordsProbsList.OrderBy(e => -e.Value).ToList();     //JANGAN LUPA DIKEMBALIKAN WORD NYA DI BAWAH

                    for (int i = 0; i < tw; i++)
                    {
                        string word = wordsProbsListOrdered[i].Key.ToString(); // cor.GetStringByID(wordsProbsListOrdered[i].Key);
                        sw.WriteLine("\t" + word + " " + wordsProbsListOrdered[i].Value);
                    }
                }
            }
        }

        protected void CalcParameter()
        {
            for (int v = 0; v < V; v++)
            {
                for (int k = 0; k < K; k++)
                {
                    theta[v][k] = (zv[v][k] + alpha) / (zvsum[v] + K * alpha);
                }
            }

            for (int k = 0; k < K; k++)
            {
                for (int w = 0; w < Voc; w++)
                {
                    phi[k][w] = (zx[w][k] + beta) / (zxsum[k] + Voc * beta);
                }
            }
        }
    }
}
