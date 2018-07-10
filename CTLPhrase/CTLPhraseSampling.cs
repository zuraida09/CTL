using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;

namespace CTLPhrase
{
    public class CTLPhraseSampling
    {
        ACTPhraseSampling sourceModel;
        ACTPhraseSampling targetModel;

        protected int savestep;
        protected int niters;

        protected string outputfile;
        protected int twords;                   //topwords
        protected Random rnd;

        Corpora cor;

        protected int M;                           //#Documents
        protected int Voc;                         //#Words in Vocabulary
        protected int K;                           //#Topics
        protected int VV;                          //#Author Pairs
        protected int sourceAuthorNum;
        protected int targetAuthorNum;

        protected double alpha;                    //Dirichlet Prior Parameter for Document->Topic
        protected double beta;                     //Dirichlet Prior Parameter for Topic->Word
        protected double gamma;
        protected double gammaT;

        public double[][] phi;                  //Topic->Word Distributions
        protected double[][] lambda;               //Document -> Coin Distribution ???????????????
        public double[][][] vartheta;             //Document -> Collaboration Topic Distribution

        protected double[][] thetaSource;          //Document -> Topic Distributions for Source Domain
        protected double[][] thetaTarget;          //Document -> Topic Distributions for Target Domain

        //public int M;                           //#Documents
        //public int Voc;                         //#Words in Vocabulary
        //public int K;                           //#Topics
        //public int VV;                          //#Author Pairs
        //public int sourceAuthorNum;
        //public int targetAuthorNum;

        //public double alpha;                    //Dirichlet Prior Parameter for Document->Topic
        //public double beta;                     //Dirichlet Prior Parameter for Topic->Word
        //public double gamma;
        //public double gammaT;

        //public double[][] phi;                  //Topic->Word Distributions
        //public double[][] lambda;               //Document -> Coin Distribution ???????????????
        //public double[][][] vartheta;             //Document -> Collaboration Topic Distribution

        //public double[][] thetaSource;          //Document -> Topic Distributions for Source Domain
        //public double[][] thetaTarget;          //Document -> Topic Distributions for Target Domain

        protected int wn;                       //jumlah token
        protected int[] words;
        protected int[] coins;
        //protected int[] authors;
        protected int[] isSourceDomain;
        protected int[] sourceAuthors;
        protected int[] targetAuthors;
        protected int[] doc;
        protected int[] z;

        protected int[][] zx;
        protected int[] zxsum;
        protected int[][][] zvv;
        protected int[][] zvvsum;

        public int[][] zvSource;
        public int[] zvsumSource;
        protected int[][] zxSource;
        public int[] zxsumSource;

        public int[][] zvTarget;
        public int[] zvsumTarget;
        protected int[][] zxTarget;
        public int[] zxsumTarget;

        protected int[][] dc;                   //document -> coint distribution
        protected int[][] cz;                   //coint -> topic distribution

        protected double[] p;                   //probabilitas topik 
        protected double[] ps;                  //probabilitas koin
        protected double[] pSource;
        protected double[] pTarget;

        public string GetJSONString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string GetJSONString(object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented);
        }

        public CTLPhraseSampling(ACTPhraseSampling sourceModel, ACTPhraseSampling targetModel)
        {
            M = 0;
            Voc = 0;
            K = sourceModel.K + targetModel.K;
            //alpha = 0.1;
            //beta = 0.1;
            gamma = 3.0;
            gammaT = 0.1;

            rnd = new Random();

            this.sourceModel = sourceModel;
            this.targetModel = targetModel;
        }

        public void InitOption(CommandLineOption opt)
        {
            try
            {
                //K = opt.topics;
                alpha = opt.alpha;
                beta = opt.beta;
                savestep = 10;                             //opt.savestep;
                niters = 100;                              //opt.niters; JANGAN LUPAAAAAAAAAAAAAAAAAAAAAAAAAAA
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

            M = cor.totalDocuments;                                         //jumlah dokumen
            Voc = cor.VocabSize;                                          //jumlah kata di vocab

            p = new double[K];
            ps = new double[2];
            pSource = new double[K];
            pTarget = new double[K];

            sourceAuthorNum = sourceModel.cor.TotalAuthor;
            targetAuthorNum = targetModel.cor.TotalAuthor;

            zx = new int[Voc][];                                            //kata-topik
            zxSource = new int[Voc][];
            zxTarget = new int[Voc][];

            for (int w = 0; w < Voc; w++)
            {
                zx[w] = new int[K];
                zxSource[w] = new int[K];
                zxTarget[w] = new int[K];

                sourceModel.zx[w].CopyTo(zxSource[w], 0);
                targetModel.zx[w].CopyTo(zxTarget[w], sourceModel.K);
            }

            zxsum = new int[K];
            zxsumSource = new int[K];
            zxsumTarget = new int[K];

            sourceModel.zxsum.CopyTo(zxsumSource, 0);
            targetModel.zxsum.CopyTo(zxsumTarget, sourceModel.K);

            zvSource = new int[sourceModel.V][];
            zvTarget = new int[targetModel.V][];

            for (int v = 0; v < sourceModel.V; v++)
            {
                zvSource[v] = new int[K];
                sourceModel.zv[v].CopyTo(zvSource[v], 0);
            }

            for (int v = 0; v < targetModel.V; v++)
            {
                zvTarget[v] = new int[K];
                targetModel.zv[v].CopyTo(zvTarget[v], sourceModel.K);
            }

            zvsumSource = sourceModel.zvsum;
            zvsumTarget = targetModel.zvsum;

            zvv = new int[sourceAuthorNum][][];                          //author pair-topik
            zvvsum = new int[sourceAuthorNum][];                         //jumlah topik untuk setiap author pair
            vartheta = new double[sourceAuthorNum][][];                  //author pair-topik

            for (int a = 0; a < sourceAuthorNum; a++)
            {
                zvv[a] = new int[targetAuthorNum][];
                vartheta[a] = new double[targetAuthorNum][];
                zvvsum[a] = new int[targetAuthorNum];

                for (int b = 0; b < targetAuthorNum; b++)
                {
                    zvv[a][b] = new int[K];
                    vartheta[a][b] = new double[K];
                }
            }

            phi = new double[K][];                                          //phi untuk topik kata 

            for (int k = 0; k < K; k++)
            {
                phi[k] = new double[Voc];
            }

            dc = new int[M][];
            cz = new int[2][];

            for (int m = 0; m < M; m++)
            {
                dc[m] = new int[2];
            }

            for (int i = 0; i < 2; i++)
            {
                cz[i] = new int[K];
            }

            words = new int[cor.totalWords];                        //sejumlah token pada korpus, isinya wordID sesuai vocab
            doc = new int[cor.totalWords];                          //docID untuk setiap kata
            //authors = new int[cor.totalWords];                       //authorID untuk setiap kata
            sourceAuthors = new int[cor.totalWords];
            targetAuthors = new int[cor.totalWords];
            coins = new int[cor.totalWords];
            isSourceDomain = new int[cor.totalWords];
            z = new int[cor.totalWords];                            //topik untuk setiap kata
            wn = 0;

            for (int i = 0; i < M; i++)                             //iterate dokumen
            {
                Document currDoc = cor.Docs[i];
                int docLength = currDoc.Length;

                for (int j = 0; j < docLength; j++)                 //iterate token pada dokumen
                {
                    words[wn] = currDoc.Words[j];                   //menyimpan wordID dari vocab untuk setiap kata

                    int coin = (rnd.NextDouble() < 0.5) ? 0 : 1;
                    coins[wn] = coin;
                    dc[i][coin] += 1;

                    int sourceAuthor = currDoc.RandomSourceAuthorID;
                    int targetAuthor = currDoc.RandomTargetAuthorID;

                    sourceAuthors[wn] = sourceAuthor;
                    targetAuthors[wn] = targetAuthor;

                    doc[wn] = i;
                    wn++;
                }
            }

            for (int i = 0; i < wn; i++)
            {
                int topic = rnd.Next(K);
                z[i] = topic;
                cz[coins[i]][topic] += 1;

                if (coins[i] == 0)
                {
                    zx[words[i]][topic] += 1;       //coba ini ntar di luar
                    zxsum[topic] += 1;

                    zvv[sourceAuthors[i]][targetAuthors[i]][topic] += 1;
                    zvvsum[sourceAuthors[i]][targetAuthors[i]] += 1;
                }
                else
                {
                    if (topic < sourceModel.K)
                    {
                        isSourceDomain[i] = 1;
                        zxSource[words[i]][topic] += 1;
                        zxsumSource[topic] += 1;

                        zvSource[sourceAuthors[i]][topic] += 1;
                        zvsumSource[sourceAuthors[i]] += 1;
                    }
                    else
                    {
                        isSourceDomain[i] = 0;
                        zxTarget[words[i]][topic] += 1;
                        zxsumTarget[topic] += 1;

                        zvTarget[targetAuthors[i]][topic] += 1;
                        zvsumTarget[targetAuthors[i]] += 1;
                    }
                }
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
            Console.WriteLine(Environment.NewLine);
            Console.WriteLine("=====================================");
            Console.WriteLine("Aplha: " + alpha.ToString());
            Console.WriteLine("Beta: " + beta.ToString());
            Console.WriteLine("Gamma: " + gamma.ToString());
            Console.WriteLine("GammaT: " + gammaT.ToString());
            Console.WriteLine("M: " + M);
            Console.WriteLine("K: " + K);
            Console.WriteLine("V: " + VV);
            Console.WriteLine("Words in Vocab: " + Voc);
            Console.WriteLine("Total iterations:" + niters);
            Console.WriteLine("Save at: " + savestep);
            Console.WriteLine("=====================================");
            Console.WriteLine();
        }

        private void GibbsSampling(int totalIter)
        {
            for (int iter = 1; iter <= totalIter; iter++)
            {
                Console.Write("Iteration " + iter + ":");
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                for (int i = 0; i < wn; i++)
                {
                    int topic = DoSampling(i);
                    z[i] = topic;
                }

                stopWatch.Stop();
                //Console.WriteLine(stopWatch.ElapsedMilliseconds / 1000.0 + " seconds");

                if (iter == 1 || iter == totalIter || iter % savestep == 0)
                {
                    SaveModel(outputfile + "." + iter.ToString() + ".json");
                    SaveTopWords(outputfile + "." + iter.ToString() + ".topwords");
                    Console.WriteLine("LogLikelihood= " + LogLikelihood);
                }

                Console.WriteLine();
            }
        }

        private int DoSampling(int i)
        {
            int oldZ = z[i];
            int c = coins[i];

            int d = doc[i];
            int w = words[i];                           //wordID
            int vs = sourceAuthors[i];                  //sourceAuthorID
            int vt = targetAuthors[i];                  //targetAuthorID

            dc[d][c] -= 1;

            if (c == 0)
            {
                zx[w][oldZ] -= 1;
                zxsum[oldZ] -= 1;

                zvv[vs][vt][oldZ] -= 1;
                zvvsum[vs][vt] -= 1;
            }
            else
            {
                //if (isSourceDomain[i] == 1)            //coba cek lagi perlu gak ini dikurangi?
                //{
                //    zvSource[vs][oldZ] -= 1;
                //    zvsumSource[vs] -= 1;
                //}
                //else
                //{
                //    zvTarget[vt][oldZ] -= 1;
                //    zvsumTarget[vt] -= 1;
                //}
            }

            isSourceDomain[i] = (rnd.NextDouble() < 0.5) ? 0 : 1;

            vs = sourceAuthors[i] = cor.Docs[d].RandomSourceAuthorID;
            vt = targetAuthors[i] = cor.Docs[d].RandomTargetAuthorID;

            double Kalpha = K * alpha;
            double Vocbeta = Voc * beta;

            ps[0] = (dc[d][0] + gammaT) / (dc[d][0] + dc[d][1] + gammaT + gamma) *
                     (zvv[vs][vt][oldZ] + zvSource[vs][oldZ] + zvTarget[vt][oldZ] + alpha) / (zvvsum[vs][vt] + zvsumSource[vs] + zvsumTarget[vt] + Kalpha);

            if (isSourceDomain[i] == 1)
            {
                ps[1] = (dc[d][1] + gamma) / (dc[d][0] + dc[d][1] + gammaT + gamma) *
                    (zvv[vs][vt][oldZ] + zvSource[vs][oldZ] + alpha) / (zvvsum[vs][vt] + zvsumSource[vs] + Kalpha);
            }
            else
            {
                ps[1] = (dc[d][1] + gamma) / (dc[d][0] + dc[d][1] + gammaT + gamma) *
                    (zvv[vs][vt][oldZ] + zvTarget[vs][oldZ] + alpha) / (zvvsum[vs][vt] + zvsumTarget[vt] + Kalpha);
            }

            ps[1] += ps[0];
            var temp = rnd.NextDouble();
            double cp = temp * ps[1];

            if (cp < ps[0])                                 //coin nya 0
            {
                dc[d][0] += 1;

                for (int k = 0; k < K; k++)
                {
                    p[k] = (zx[w][k] + zxSource[w][k] + zxTarget[w][k] + beta) / (zxsum[k] + zxsumSource[k] + zxsumTarget[k] + Vocbeta)
                            * (zvv[vs][vt][k] + zvSource[vs][k] + zvTarget[vt][k] + alpha) / (zvvsum[vs][vt] + zvsumSource[vs] + zvsumTarget[vt] + Kalpha);
                }

                for (int k = 1; k < K; k++)
                {
                    p[k] += p[k - 1];
                }

                cp = rnd.NextDouble() * p[K - 1];

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
                zvv[vs][vt][newZ] += 1;
                zxsum[newZ] += 1;
                zvvsum[vs][vt] += 1;

                return newZ;
            }
            else
            {
                dc[d][1] += 1;

                if (isSourceDomain[i] == 1)
                {
                    for (int k = 0; k < sourceModel.K; k++)     //coba pake K aja 
                    {
                        pSource[k] = (zxSource[w][k] + beta) / (zxsumSource[k] + Vocbeta) * (zvSource[vs][k] + alpha) / (zvsumSource[vs] + Kalpha);
                    }

                    for (int k = 1; k < sourceModel.K; k++)
                    {
                        pSource[k] += pSource[k - 1];
                    }

                    cp = rnd.NextDouble() * pSource[K - 1];

                    int newZ;

                    for (newZ = 0; newZ < K; newZ++)
                    {
                        if (pSource[newZ] > cp)
                        {
                            break;
                        }
                    }

                    if (newZ == sourceModel.K) newZ--;

                    zxSource[w][newZ] += 1;
                    zvSource[vs][newZ] += 1;
                    zxsumSource[newZ] += 1;
                    zvsumSource[vs] += 1;

                    return newZ;
                }
                else
                {
                    for (int k = sourceModel.K; k < K; k++)
                    {
                        pTarget[k] = (zxTarget[w][k] + beta) / (zxsumTarget[k] + Vocbeta) * (zvTarget[vt][k] + alpha) / (zvsumTarget[vt] + Kalpha);
                    }

                    for (int k = sourceModel.K; k < K; k++)
                    {
                        pTarget[k] += pTarget[k - 1];
                    }

                    cp = rnd.NextDouble() * pTarget[K - 1];

                    int newZ;

                    for (newZ = sourceModel.K; newZ < targetModel.K; newZ++)
                    {
                        if (pTarget[newZ] > cp)
                        {
                            break;
                        }
                    }

                    if (newZ == targetModel.K) newZ--;

                    zxTarget[w][newZ] += 1;
                    zvTarget[vt][newZ] += 1;
                    zxsumTarget[newZ] += 1;
                    zvsumTarget[vt] += 1;

                    return newZ;
                }
            }
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
                    var wordsProbsListOrdered = wordsProbsList.OrderBy(e => -e.Value).ToList();

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
            for (int vs = 0; vs < sourceAuthorNum; vs++)
            {
                for (int vt = 0; vt < targetAuthorNum; vt++)
                {
                    for (int k = 0; k < K; k++)
                    {
                        vartheta[vs][vt][k] = (zvv[vs][vt][k] + zvSource[vs][k] + zvTarget[vt][k] + alpha) / (zvvsum[vs][vt] + zvsumSource[vs] + zvsumTarget[vt] + K * alpha);
                    }
                }
            }

            for (int k = 0; k < K; k++)
            {
                for (int w = 0; w < Voc; w++)
                {
                    phi[k][w] = (zx[w][k] + zxSource[w][k] + zxTarget[w][k] + beta) / (zxsum[k] + zxsumSource[k] + zxsumTarget[k] + Voc * beta);
                }
            }
        }

        public double LogLikelihood
        {
            get
            {
                double ans = 0;

                for (int i = 0; i < wn; i++)
                {
                    int w = words[i];
                    int vs = sourceAuthors[i];
                    int vt = targetAuthors[i];
                    double tmp = 0;

                    for (int k = 0; k < K; k++)
                    {
                        tmp += phi[k][w] * vartheta[vs][vt][k];
                    }

                    ans += Math.Log(tmp);
                }

                return ans;
            }
        }
    }
}
