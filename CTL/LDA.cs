using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace CTL
{
    [Serializable]
    public class LDA
    {
        public int M;               //#Documents
        public int Voc;             //#Words in Vocabulary
        public int K;               //#Topics
        public int V;               //#Authors

        public double alpha;        //Dirichlet Prior Parameter for Document->Topic
        public double beta;         //Dirichlet Prior Parameter for Topic->Word

        public double[][] theta;    //Document -> Topic Distributions
        public double[][] phi;      //Topic->Word Distributions

        protected int wn;           //jumlah token
        protected int[] words;
        protected int[] authors;
        protected int[] docs;
        protected int[] z;

        public double LogLikelihood
        {
            get
            {
                double ans = 0;

                for (int i = 0; i < wn; i++) 
                {
                    int w = words[i];
                    int v = authors[i];
                    double tmp = 0;

                    for (int k = 0; k < K; k++)
                    {
                        tmp += phi[k][w] * theta[v][k];
                    }

                    ans += Math.Log(tmp);
                }

                return ans;
            }
        }
        
        public string GetJSONString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public string GetJSONString(object o)
        {
            return JsonConvert.SerializeObject(o, Formatting.Indented);
        }
    }
}
