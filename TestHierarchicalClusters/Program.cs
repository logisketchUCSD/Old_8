using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using ConverterXML;
using RecognitionTemplates;
using ImageAligner;
using Utilities;
using HierarchicalCluster;

namespace TestHierarchicalClusters
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("(0) Directory of Sketches, (1) Search Pattern, (2) % files for testing, (3) OutFile");
                return;
            }
            string dir = args[0];
            string searchPattern = args[1];
            string outFile = args[3];

            double[] radii = new double[] { 1.3 };
            double[] scrThrsh = new double[] { 0.90 };
            int[] depths = new int[] { 1000 }; //1, 3, 5, 10, 20, 100, 

            /*double maxR;
            bool goodR = double.TryParse(args[4], out maxR);
            if (!goodR)
            {
                Console.WriteLine("Not a valid number for the max radius");
                return;
            }

            double scoreThreshold;
            bool goodT = double.TryParse(args[5], out scoreThreshold);
            if (!goodT)
            {
                Console.WriteLine("Not a valid number for the score threshold");
                return;
            }

            int maxD;
            bool goodD = int.TryParse(args[6], out maxD);
            if (!goodD)
            {
                Console.WriteLine("Not a valid number for the max depth");
                return;
            }*/

            

            List<string> files = new List<string>(System.IO.Directory.GetFiles(dir, searchPattern));

            double percent;
            bool good = double.TryParse(args[2], out percent);
            if (!good)
            {
                Console.WriteLine("Not a valid number for the % of files to be used for testing");
                return;
            }

            int numTestFiles = (int)(files.Count * percent / 100.0);
            List<string> testFiles = new List<string>(numTestFiles);
            Random r = new Random();
            for (int i = 0; i < numTestFiles; i++)
            {
                double rand = r.NextDouble();
                int ind = (int)(rand * (files.Count - 1));
                string testFile = files[ind];
                if (!testFiles.Contains(testFile))
                {
                    testFiles.Add(testFile);
                    files.Remove(testFile);
                }
                else
                    i--;
            }

            DateTime startShapes = DateTime.Now;

            Console.WriteLine("Starting to find Shapes: " + startShapes.ToLongTimeString());

            Dictionary<string, List<RecognitionTemplate>> symbols = new Dictionary<string, List<RecognitionTemplate>>();
            List<RecognitionTemplate> templates = new List<RecognitionTemplate>();
            Dictionary<string, List<RecognitionTemplate>> ssSymbols = new Dictionary<string, List<RecognitionTemplate>>();
            List<RecognitionTemplate> ssTemplates = new List<RecognitionTemplate>();

            List<double> timesSymbol = new List<double>();
            List<double> timesSketch = new List<double>();

            foreach (string sketchFile in files)
            {
                DateTime dtt1 = DateTime.Now;
                Sketch.Sketch sketch = new ReadXML(sketchFile).Sketch;
                DateTime dtt2 = DateTime.Now;
                TimeSpan st1 = dtt2 - dtt1;
                timesSketch.Add(st1.TotalMilliseconds);
                sketch = General.ReOrderParentShapes(sketch);
                sketch = General.LinkShapes(sketch);

                foreach (Shape shape in sketch.ShapesL)
                {
                    if (shape.ParentShape.SubstrokesL.Count > 0) continue;

                    foreach (Substroke stroke in shape.SubstrokesL)
                    {
                        string label = stroke.Labels[stroke.Labels.Length - 1];
                        if (!ssSymbols.ContainsKey(label))
                            ssSymbols.Add(label, new List<RecognitionTemplate>());
                        ImageSymbol ssImg = new ImageSymbol(stroke.PointsAsSysPoints, new Utilities.SymbolInfo(label));
                        ssSymbols[label].Add(ssImg);
                        ssTemplates.Add(ssImg);
                    }

                    if (!General.IsGate(shape)) continue;

                    if (!symbols.ContainsKey(shape.Label))
                        symbols.Add(shape.Label, new List<RecognitionTemplate>());
                    DateTime dt1 = DateTime.Now;
                    ImageSymbol img = new ImageSymbol(ShapePoints(shape), new Utilities.SymbolInfo(shape.Label));
                    DateTime dt2 = DateTime.Now;
                    TimeSpan s1 = dt2 - dt1;
                    timesSymbol.Add(s1.TotalMilliseconds);
                    symbols[shape.Label].Add(img);
                    templates.Add(img);
                }

                double avgSketch = Utilities.Compute.Mean(timesSketch.ToArray());
                double avgSymbol = Utilities.Compute.Mean(timesSymbol.ToArray());
                //Console.WriteLine(avgSketch.ToString("#0.0") + "\t" + avgSymbol.ToString("#0.0"));
            }

            List<RecognitionTemplate> tests = new List<RecognitionTemplate>();
            List<RecognitionTemplate> ssTests = new List<RecognitionTemplate>();

            foreach (string testFile in testFiles)
            {
                Sketch.Sketch sketchTest = new ReadXML(testFile).Sketch;
                sketchTest = General.ReOrderParentShapes(sketchTest);
                sketchTest = General.LinkShapes(sketchTest);

                foreach (Shape shape in sketchTest.ShapesL)
                {
                    if (shape.ParentShape.SubstrokesL.Count > 0) continue;

                    foreach (Substroke stroke in shape.SubstrokesL)
                        ssTests.Add(new ImageSymbol(stroke.PointsAsSysPoints, new Utilities.SymbolInfo(stroke.Labels[stroke.Labels.Length - 1]))); 
                    
                    if (!General.IsGate(shape)) continue;

                    tests.Add(new ImageSymbol(ShapePoints(shape), new Utilities.SymbolInfo(shape.Label)));
                }

                
            }

            DateTime startClusters = DateTime.Now;
            Console.WriteLine("Shapes found, starting to build clusters: " + startClusters.ToLongTimeString());

            List<Cluster> clustersComplete = new List<Cluster>();
            //List<Cluster> clustersSingle = new List<Cluster>();
            //List<Cluster> clustersAverage = new List<Cluster>();
            foreach (KeyValuePair<string, List<RecognitionTemplate>> kvp in symbols)
            {
                List<RecognitionTemplate> bits = kvp.Value;
                
                Cluster cComplete = Cluster.BuildTree(bits, ClusterLinking.Complete);
                clustersComplete.Add(cComplete);

                /*Cluster cSingle = Cluster.BuildTree(bits, ClusterLinking.Single);
                clustersSingle.Add(cSingle);

                Cluster cAverage = Cluster.BuildTree(bits, ClusterLinking.Average);
                clustersAverage.Add(cAverage);*/
            }

            List<Cluster> clustersSSComplete = new List<Cluster>();
            foreach (KeyValuePair<string, List<RecognitionTemplate>> kvp in ssSymbols)
            {
                List<RecognitionTemplate> bits = kvp.Value;

                Cluster ssComplete = Cluster.BuildTree(bits, ClusterLinking.Complete);
                clustersSSComplete.Add(ssComplete);
            }

            /*foreach (Cluster c in clusters)
            {
                foreach (Cluster c2 in clusters)
                {
                    if (c == c2) continue;

                    double score = c2.Recognize(c.Center);
                    Console.WriteLine(c.Center.Name + " - " + c2.Center.Name + ": " + score.ToString("#0.000") + " (r = " + c.Radius.ToString("#0.000") + ")");
                }
                Console.WriteLine();
                Console.WriteLine();
            }*/
            DateTime startTreeComplete1 = DateTime.Now;
            Console.WriteLine("Clusters built, starting recognition Complete with New: " + startTreeComplete1.ToLongTimeString());

            List<TreeResult> resultsCompleteNew = new List<TreeResult>();

            for (int i = 0; i < radii.Length; i++)
            {
                ClusteringParameters.RADIUS_RATIO = radii[i];
                for (int j = 0; j < scrThrsh.Length; j++)
                {
                    ClusteringParameters.SCORE_THRESHOLD = scrThrsh[j];
                    for (int k = 0; k < depths.Length; k++)
                    {
                        ClusteringParameters.MAX_DEPTH = depths[k];
                        Console.WriteLine("Tree BestFirst: " + ClusteringParameters.RADIUS_RATIO + ", " + ClusteringParameters.SCORE_THRESHOLD + ", " + ClusteringParameters.MAX_DEPTH);

                        DateTime s1 = DateTime.Now;
                        int correct1 = 0;
                        foreach (RecognitionTemplate t in tests)
                        {
                            double bestScore;
                            string bestName = Cluster.RecognizeBestFirst(clustersComplete, t, out bestScore);
                            //Console.WriteLine(t.Name + " --> " + bestName + ": " + bestScore.ToString("#0.000"));
                            if (t.Name == bestName)
                                correct1++;
                        }
                        DateTime e1 = DateTime.Now;

                        TimeSpan time = e1 - s1;
                        resultsCompleteNew.Add(new TreeResult(ClusteringParameters.RADIUS_RATIO,
                            ClusteringParameters.SCORE_THRESHOLD,
                            ClusteringParameters.MAX_DEPTH,
                            correct1,
                            tests.Count,
                            templates.Count,
                            time.TotalMilliseconds,
                            "Complete"));
                    }
                }
            }

            

            /*DateTime startTreeComplete = DateTime.Now;
            Console.WriteLine("Clusters built, starting recognition Complete: " + startTreeComplete.ToLongTimeString());

            List<TreeResult> resultsComplete = new List<TreeResult>();

            for (int i = 0; i < radii.Length; i++)
            {
                ClusteringParameters.RADIUS_RATIO = radii[i];
                for (int j = 0; j < scrThrsh.Length; j++)
                {
                    ClusteringParameters.SCORE_THRESHOLD = scrThrsh[j];
                    for (int k = 0; k < depths.Length; k++)
                    {
                        ClusteringParameters.MAX_DEPTH = depths[k];

                        DateTime s1 = DateTime.Now;
                        int correct1 = 0;
                        foreach (RecognitionTemplate t in tests)
                        {
                            double bestScore = -1.0;
                            string bestName = "None";
                            foreach (Cluster c in clustersComplete)
                            {
                                double score = c.Recognize(t);
                                //Dictionary<double, RecognitionTemplate> best = c.RecognizeNBest(t, 5);
                                
                                string name = c.Center.Name;
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestName = name;
                                }
                                //Console.WriteLine(name + ": " + score.ToString("#0.000"));
                            }
                            //Console.WriteLine(t.Name + " --> " + bestName + ": " + bestScore.ToString("#0.000"));
                            if (t.Name == bestName)
                                correct1++;
                        }
                        DateTime e1 = DateTime.Now;

                        TimeSpan time = e1 - s1;
                        resultsComplete.Add(new TreeResult(ClusteringParameters.RADIUS_RATIO,
                            ClusteringParameters.SCORE_THRESHOLD,
                            ClusteringParameters.MAX_DEPTH,
                            correct1,
                            tests.Count,
                            templates.Count,
                            time.TotalMilliseconds,
                            "Complete"));
                    }
                }
            }

            DateTime startTreeSingle = DateTime.Now;
            Console.WriteLine("Clusters built, starting recognition Single: " + startTreeSingle.ToLongTimeString());

            List<TreeResult> resultsSingle = new List<TreeResult>();

            for (int i = 0; i < radii.Length; i++)
            {
                ClusteringParameters.RADIUS_RATIO = radii[i];
                for (int j = 0; j < scrThrsh.Length; j++)
                {
                    ClusteringParameters.SCORE_THRESHOLD = scrThrsh[j];
                    for (int k = 0; k < depths.Length; k++)
                    {
                        ClusteringParameters.MAX_DEPTH = depths[k];

                        DateTime s1 = DateTime.Now;
                        int correct1 = 0;
                        foreach (RecognitionTemplate t in tests)
                        {
                            double bestScore = -1.0;
                            string bestName = "None";
                            foreach (Cluster c in clustersSingle)
                            {
                                double score = c.Recognize(t);
                                //Dictionary<double, RecognitionTemplate> best = c.RecognizeNBest(t, 5);

                                string name = c.Center.Name;
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestName = name;
                                }
                                //Console.WriteLine(name + ": " + score.ToString("#0.000"));
                            }
                            //Console.WriteLine(t.Name + " --> " + bestName + ": " + bestScore.ToString("#0.000"));
                            if (t.Name == bestName)
                                correct1++;
                        }
                        DateTime e1 = DateTime.Now;

                        TimeSpan time = e1 - s1;
                        resultsSingle.Add(new TreeResult(ClusteringParameters.RADIUS_RATIO,
                            ClusteringParameters.SCORE_THRESHOLD,
                            ClusteringParameters.MAX_DEPTH,
                            correct1,
                            tests.Count,
                            templates.Count,
                            time.TotalMilliseconds, 
                            "Single"));
                    }
                }
            }

            DateTime startTreeAverage = DateTime.Now;
            Console.WriteLine("Clusters built, starting recognition Average: " + startTreeAverage.ToLongTimeString());

            List<TreeResult> resultsAverage = new List<TreeResult>();

            for (int i = 0; i < radii.Length; i++)
            {
                ClusteringParameters.RADIUS_RATIO = radii[i];
                for (int j = 0; j < scrThrsh.Length; j++)
                {
                    ClusteringParameters.SCORE_THRESHOLD = scrThrsh[j];
                    for (int k = 0; k < depths.Length; k++)
                    {
                        ClusteringParameters.MAX_DEPTH = depths[k];

                        DateTime s1 = DateTime.Now;
                        int correct1 = 0;
                        foreach (RecognitionTemplate t in tests)
                        {
                            double bestScore = -1.0;
                            string bestName = "None";
                            foreach (Cluster c in clustersAverage)
                            {
                                double score = c.Recognize(t);
                                //Dictionary<double, RecognitionTemplate> best = c.RecognizeNBest(t, 5);

                                string name = c.Center.Name;
                                if (score > bestScore)
                                {
                                    bestScore = score;
                                    bestName = name;
                                }
                                //Console.WriteLine(name + ": " + score.ToString("#0.000"));
                            }
                            //Console.WriteLine(t.Name + " --> " + bestName + ": " + bestScore.ToString("#0.000"));
                            if (t.Name == bestName)
                                correct1++;
                        }
                        DateTime e1 = DateTime.Now;

                        TimeSpan time = e1 - s1;
                        resultsAverage.Add(new TreeResult(ClusteringParameters.RADIUS_RATIO,
                            ClusteringParameters.SCORE_THRESHOLD,
                            ClusteringParameters.MAX_DEPTH,
                            correct1,
                            tests.Count,
                            templates.Count,
                            time.TotalMilliseconds,
                            "Average"));
                    }
                }
            }*/

            DateTime startRegular = DateTime.Now;
            Console.WriteLine("Finished Recognition for Tree, starting regular recognition: " + startRegular.ToLongTimeString());

            DateTime s2 = DateTime.Now;
            int correct2 = 0;
            foreach (RecognitionTemplate t in tests)
            {
                //System.Drawing.Bitmap mapTest = ((ImageSymbol)t).MakeBitmap();
                double bestScore = -1.0;
                string bestName = "None";
                foreach (RecognitionTemplate template in templates)
                {
                    //System.Drawing.Bitmap mapTemplate = ((ImageSymbol)template).MakeBitmap();
                    double score = template.Recognize(t);
                    string name = template.Name;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestName = name;

                        //BitmapsShower shower = new BitmapsShower(mapTest, mapTemplate);
                        //shower.Show();
                    }
                    //Console.WriteLine(name + ": " + score.ToString("#0.000"));
                    

                    //shower.Close();
                }
                //Console.WriteLine(t.Name + " --> " + bestName + ": " + bestScore.ToString("#0.000"));
                if (t.Name == bestName)
                    correct2++;
            }
            DateTime e2 = DateTime.Now;

            TimeSpan d2 = e2 - s2;

            DateTime startTreeCompleteSS = DateTime.Now;
            Console.WriteLine("Clusters built, starting SS recognition Complete: " + startTreeCompleteSS.ToLongTimeString());

            List<TreeResult> resultsCompleteSS = new List<TreeResult>();

            for (int i = 0; i < radii.Length; i++)
            {
                ClusteringParameters.RADIUS_RATIO = radii[i];
                for (int j = 0; j < scrThrsh.Length; j++)
                {
                    ClusteringParameters.SCORE_THRESHOLD = scrThrsh[j];
                    for (int k = 0; k < depths.Length; k++)
                    {
                        ClusteringParameters.MAX_DEPTH = depths[k];
                        //Console.WriteLine("Tree SS BestFirst: " + ClusteringParameters.RADIUS_RATIO + ", " + ClusteringParameters.SCORE_THRESHOLD + ", " + ClusteringParameters.MAX_DEPTH);

                        DateTime s1 = DateTime.Now;
                        int correct1 = 0;
                        foreach (RecognitionTemplate t in ssTests)
                        {
                            double bestScore;
                            string bestName = Cluster.RecognizeBestFirst(clustersSSComplete, t, out bestScore);
                            //Console.WriteLine(t.Name + " --> " + bestName + ": " + bestScore.ToString("#0.000"));
                            if (t.Name == bestName)
                                correct1++;
                        }
                        DateTime e1 = DateTime.Now;

                        TimeSpan time = e1 - s1;
                        resultsCompleteSS.Add(new TreeResult(ClusteringParameters.RADIUS_RATIO,
                            ClusteringParameters.SCORE_THRESHOLD,
                            ClusteringParameters.MAX_DEPTH,
                            correct1,
                            ssTests.Count,
                            ssTemplates.Count,
                            time.TotalMilliseconds,
                            "Complete"));
                    }
                }
            }

            DateTime startRegularSS = DateTime.Now;
            Console.WriteLine("Finished Recognition for Tree, starting regular recognition: " + startRegularSS.ToLongTimeString());

            DateTime s2ss = DateTime.Now;
            int correct2ss = 0;
            foreach (RecognitionTemplate t in ssTests)
            {
                double bestScore = -1.0;
                string bestName = "None";
                foreach (RecognitionTemplate template in ssTemplates)
                {
                    double score = template.Recognize(t);
                    string name = template.Name;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestName = name;
                    }
                }
                if (t.Name == bestName)
                    correct2ss++;
            }
            DateTime e2ss = DateTime.Now;

            TimeSpan d2ss = e2ss - s2ss;

            Console.WriteLine("Finished regular recognition SS: " + e2ss.ToLongTimeString());

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine(templates.Count + " Training Examples:");
            
            

            /*foreach (TreeResult res in resultsComplete)
                res.PrintConsole();

            foreach (TreeResult res in resultsSingle)
                res.PrintConsole();

            foreach (TreeResult res in resultsAverage)
                res.PrintConsole();*/

            foreach (TreeResult res in resultsCompleteNew)
                res.PrintConsole();

            Console.WriteLine("Regular:\t" + correct2 + " / " + tests.Count + " in " + d2.TotalMilliseconds.ToString("#0") + " ms");

            Console.WriteLine();
            Console.WriteLine();

            foreach (TreeResult res in resultsCompleteSS)
                res.PrintConsole();

            Console.WriteLine("Regular:\t" + correct2ss + " / " + ssTests.Count + " in " + d2ss.TotalMilliseconds.ToString("#0") + " ms");
            
            System.IO.StreamWriter writer = new System.IO.StreamWriter(outFile, true);

            writer.WriteLine("Search Pattern:\t" + searchPattern);
            writer.WriteLine("Percent Testing:\t" + percent);

            writer.WriteLine("Using the following sketches for testing:");
            foreach (string file in testFiles)
                writer.WriteLine("  " + System.IO.Path.GetFileName(file));
            writer.WriteLine("Starting to find Shapes: " + startShapes.ToLongTimeString());
            writer.WriteLine("Shapes found, starting to build clusters: " + startClusters.ToLongTimeString());
            writer.WriteLine("Clusters built, starting recognition Complete with New: " + startTreeComplete1.ToLongTimeString());
            //writer.WriteLine("Clusters built, starting recognition Complete: " + startTreeComplete.ToLongTimeString());
            //writer.WriteLine("Clusters built, starting recognition Single: " + startTreeSingle.ToLongTimeString());
            //writer.WriteLine("Clusters built, starting recognition Average: " + startTreeAverage.ToLongTimeString());
            writer.WriteLine("Finished Recognition for Tree, starting regular recognition: " + startRegular.ToLongTimeString());
            writer.WriteLine("Finished regular recognition: " + e2ss.ToLongTimeString());

            writer.WriteLine(templates.Count + " Training Examples:");

            double acc = (double)correct2 / (double)tests.Count;
            double timeTaken = d2.TotalMilliseconds;
            double avgTime = timeTaken / tests.Count;
            writer.WriteLine("Regular\t" + tests.Count + "\t" + templates.Count + "\t" + correct2 + "\t" +
                acc.ToString("#0.00000") + "\t" + timeTaken.ToString("#0") + "\t" +
                avgTime.ToString("#0.00") + "\t" + "-1.0" + "\t" +
                "-1.0" + "\t" + "-1");

            Console.WriteLine("Regular:\t" + correct2ss + " / " + ssTests.Count + " in " + d2ss.TotalMilliseconds.ToString("#0") + " ms");
            


            

            /*foreach (TreeResult res in resultsComplete)
                res.Print(writer);

            foreach (TreeResult res in resultsSingle)
                res.Print(writer);

            foreach (TreeResult res in resultsAverage)
                res.Print(writer);
            */
            foreach (TreeResult res in resultsCompleteNew)
                res.Print(writer);

            writer.WriteLine();

            double accSS = (double)correct2ss / (double)ssTests.Count;
            double timeTakenSS = d2ss.TotalMilliseconds;
            double avgTimeSS = timeTakenSS / ssTests.Count;
            writer.WriteLine("RegularSS\t" + ssTests.Count + "\t" + ssTemplates.Count + "\t" + correct2ss + "\t" +
                accSS.ToString("#0.00000") + "\t" + timeTakenSS.ToString("#0") + "\t" +
                avgTimeSS.ToString("#0.00") + "\t" + "-1.0" + "\t" +
                "-1.0" + "\t" + "-1");

            foreach (TreeResult res in resultsCompleteSS)
                res.Print(writer);

            writer.WriteLine();
            writer.WriteLine();

            writer.Close();
        }

        static System.Drawing.Point[] ShapePoints(Shape shape)
        {
            List<Point> points = shape.Points;
            System.Drawing.Point[] array = new System.Drawing.Point[points.Count];

            for (int i = 0; i < points.Count; i++)
                array[i] = points[i].SysDrawPoint;

            return array;
        }
    }
}
