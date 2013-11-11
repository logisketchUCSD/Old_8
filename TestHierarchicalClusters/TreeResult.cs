using System;
using System.Collections.Generic;
using System.Text;

namespace TestHierarchicalClusters
{
    public class TreeResult
    {
        double radius;
        double scoreThreshold;
        int depth;
        string linkingMethod;

        int numCorrect;
        int numTests;
        int numTraining;

        double timeTakenMS;

        public TreeResult(double r, double sc, int dep, int correct, int total, int train, double time, string method)
        {
            radius = r;
            scoreThreshold = sc;
            depth = dep;
            numCorrect = correct;
            numTests = total;
            numTraining = train;
            timeTakenMS = time;
            linkingMethod = method;
        }

        internal void Print(System.IO.StreamWriter writer)
        {
            double accuracy = (double)numCorrect / (double)numTests;
            double avgTime = timeTakenMS / numTests;
            writer.WriteLine("Tree\t" + numTests + "\t" + numTraining + "\t" + numCorrect + "\t" +
                accuracy.ToString("#0.00000") + "\t" + timeTakenMS.ToString("#0") + "\t" + 
                avgTime.ToString("#0.00") + "\t" + radius.ToString("#0.00") + "\t" + 
                scoreThreshold.ToString("#0.00") + "\t" + depth);
        }



        internal void PrintConsole()
        {
            Console.WriteLine("Tree " + linkingMethod + ":\t" + numCorrect + " / " + numTests + 
                " in " + timeTakenMS.ToString("#0") + " ms");
        }
    }
}
