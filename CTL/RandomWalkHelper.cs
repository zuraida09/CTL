using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CTL
{
    public static class RandomWalkHelper
    {
        //thetaSource = new double[sourceModel.V][K];
        //thetaTarget = new double[targetModel.V][K];

        public static void ExportDataForMatlab(double threshold, double[][] thetaSource, double[][] thetaTarget, int topicNum)
        {
            Console.WriteLine();

            int sourceAuthorNum = thetaSource.Length;
            int targetAuthorNum = thetaTarget.Length;

            //jangan lupa threshold

            int startIndexSource = 1;
            int startIndexTarget = startIndexSource + sourceAuthorNum;
            int startIndexTopic = startIndexTarget + targetAuthorNum;

            Console.WriteLine();


            using (StreamWriter file = new StreamWriter("dataForMatlab.txt"))
            {
                for (int vs = 0; vs < sourceAuthorNum; vs++)
                {
                    for (int k = 0; k < topicNum; k++)
                    {
                        if (thetaSource[vs][k] > threshold)
                        {
                            file.WriteLine((vs + startIndexSource) + " " + (k + startIndexTopic) + " " + thetaSource[vs][k]);
                        }
                    }
                }

                for (int vt = 0; vt < targetAuthorNum; vt++)
                {
                    for (int k = 0; k < topicNum; k++)
                    {
                        if (thetaTarget[vt][k] > threshold)
                        {
                            file.WriteLine((vt + startIndexTarget) + " " + (k + startIndexTopic) + " " + thetaTarget[vt][k]);
                        }
                    }
                }
            }
        }

        public static string NewSourceIndex(this int sourceAuthorIndex)
        {
            return (1 + sourceAuthorIndex).ToString();
        }
    }
}
