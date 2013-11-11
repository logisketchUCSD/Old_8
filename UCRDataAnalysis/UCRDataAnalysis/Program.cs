using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ConverterXML;
using Sketch;
using Recognizers;
using ImageRecognizer;

using ImgRecognizer = System.Collections.Generic.List<ImageRecognizer.BitmapSymbol>;

namespace UCRDataAnalysis
{
    /// <summary>
    /// Run a test for UCRDataAnalysis
    /// </summary>
    class Program
    {
        const double testing_percent = 0.67;
        const int small_set = 5;
        const int user_set = 2;
        static List<string> classes = new List<string>(new string[] { "line", "ellipse", "rectangle" });
        static string[] activities = new string[] { "basic", "er", "process" };
        static List<MySketch> sketches;

        static Dictionary<string, Dictionary<string, int>> counts = new Dictionary<string, Dictionary<string, int>>();
        static Dictionary<string, Dictionary<string, float>> scounts = new Dictionary<string, Dictionary<string, float>>();

        static TimeSpan total = new TimeSpan();
        static int nclassified = 0;

        static void Main(string[] args)
        {
            MainAutomaticData(args);
        }
        
        // terrible copying and pasting ensues

        // this main is made for optimizing multi$ performance
        static void MainParameterTest(string[] args)
        {
            DateTime start = DateTime.Now;
            int run_number = 0;
            while (Directory.Exists(string.Format("Run{0}", ++run_number))) ;
            Directory.CreateDirectory(string.Format("Run{0}", run_number));

            Console.WriteLine("Beginning Run #{0}", run_number);

            string[] hmc = Directory.GetFiles("e:\\sketch\\Data\\Gate Study Data\\LabeledSketches\\TabletPC\\HMC", "*.xml", SearchOption.AllDirectories);
            string[] ucr = Directory.GetFiles("e:\\sketch\\Data\\Gate Study Data\\LabeledSketches\\TabletPC\\UCR", "*.xml", SearchOption.AllDirectories);

            sketches = new List<MySketch>();

            // read HMC
            for (int i = 1; i <= 12; i++)
            {
                foreach (string file in hmc)
                {
                    // only care about and/or/not
                    if (file.Contains("NAND") || file.Contains("NOR") || file.Contains("XOR")) continue;
                    if (file.Contains(string.Format("\\{0}_", i))) // iso,copy
                    {
                        string act = file.Contains("COPY") ? "copy" : "iso";
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i, act));
                    }
                    else if (file.Contains(string.Format("_{0}_", i))) // synth
                    {
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i, "synth"));
                    }
                }
            }

            Console.WriteLine("Done reading HMC sketches");

            // read UCR
            for (int i = 1; i <= 12; i++)
            {
                foreach (string file in ucr)
                {
                    // only care about and/or/not
                    if (file.Contains("NAND") || file.Contains("NOR") || file.Contains("XOR")) continue;
                    if (file.Contains(string.Format("\\{0}_", i))) // iso,copy
                    {
                        string act = file.Contains("COPY") ? "copy" : "iso";
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i + 12, act));
                    }
                    else if (file.Contains(string.Format("_{0}_", i))) // synth
                    {
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i + 12, "synth"));
                    }
                }
            }

            foreach (MySketch ms in sketches)
            {
                if (!counts.ContainsKey(ms.Activity))
                {
                    counts.Add(ms.Activity, new Dictionary<string, int>());
                    scounts.Add(ms.Activity, new Dictionary<string, float>());
                }
                foreach (Shape s in ms.Sketch.ShapesL)
                {
                    if (classes.Contains(s.LabelL))
                    {
                        if (!counts[ms.Activity].ContainsKey(s.LabelL))
                        {
                            counts[ms.Activity].Add(s.LabelL, 0);
                            scounts[ms.Activity].Add(s.LabelL, 0);
                        }
                        counts[ms.Activity][s.LabelL] += 1;
                        scounts[ms.Activity][s.LabelL] += s.SubstrokesL.Count;
                    }
                }
            }

            Console.WriteLine("{0} sketches loaded", sketches.Count);
            foreach (string a in activities)
            {
                foreach (string c in classes)
                {
                    Console.WriteLine("{0}-{1}: {2} shapes, {3} strokes/shape", a, c, counts[a][c], scounts[a][c] / counts[a][c]);
                }
            }



            Console.WriteLine("Done reading UCR sketches");

            List<int> training_users = new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 });
            List<int> testing_users = new List<int>();
            int ntesters = (int)((double)(training_users.Count) * testing_percent);

            Random r = new Random();

            for (int i = 0; i < ntesters; i++)
            {
                int next = r.Next(training_users.Count);
                // next line adds testing user taken out of training users
                testing_users.Add(training_users[next]);
                training_users.RemoveAt(next);
            }

            training_users.Sort();
            testing_users.Sort();

            DollarRecognizer copyFrom = new DollarRecognizer(64, 48, Math.PI/20, new DollarRecognizer.distance_function(DollarRecognizer.HADistance));

            Console.WriteLine("Entering $1 tester -- HADist");
            DollarRecognizer[] smallSets = makeSmallSets(training_users, copyFrom);
            TestDollar(smallSets, testing_users, "DollarHA", run_number);

            copyFrom = new DollarRecognizer(64, 48, Math.PI / 20, new DollarRecognizer.distance_function(DollarRecognizer.SAdistance));
            Console.WriteLine("Entering $1 tester -- SADist");
            DollarRecognizer[] smallSets2 = makeSmallSets(training_users, copyFrom);
            TestDollar(smallSets2, testing_users, "DollarSA", run_number);

            DateTime end = DateTime.Now;
            TimeSpan timer = end - start;
            Console.WriteLine("Total Time: {0}", timer);
            Console.Read();
        }

        // this main is tailored to Australian data
        static void MainAutomaticData(string[] args)
        {
            DateTime start = DateTime.Now;
            int run_number = 0;
            while (Directory.Exists(string.Format("Run{0}", ++run_number))) ;
            Directory.CreateDirectory(string.Format("Run{0}", run_number));

            Console.WriteLine("Beginning Run #{0}", run_number);

            //string dir = Directory.GetCurrentDirectory();
            //dir = dir.Substring(0, dir.IndexOf("Code\\Recognition"));
            //string aeroot = dir + "Data\\Gate Study Data\\LabeledSketches\\automaticEvalData\\";
            string aeroot = "..\\..\\..\\..\\..\\..\\Data\\Gate Study Data\\LabeledSketches\\automaticEvalData\\";

            //string[] e85 = Directory.GetFiles("e:\\sketch\\Data\\E85\\New Labeled Documents\\Labeled Summer 2007", "*.xml");

            sketches = new List<MySketch>();

            // read AutoEval
            for (int i = 0; i <= 32; i++)
            {
                string[] ae = Directory.GetFiles(aeroot+i+"\\", "*.xml", SearchOption.AllDirectories);
                foreach (string file in ae)
                {

                    if (file.Contains("Entity"))
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i, activities[1]));
                    else if (file.Contains("Process"))
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i, activities[2]));
                    else
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i, activities[0]));
                }
            }

            Console.WriteLine("Done reading files -- organizing sketches");

            foreach (MySketch ms in sketches)
            {
                if (!counts.ContainsKey(ms.Activity))
                {
                    counts.Add(ms.Activity, new Dictionary<string, int>());
                    scounts.Add(ms.Activity, new Dictionary<string, float>());
                }
                foreach (Shape s in ms.Sketch.ShapesL)
                {
                    if (classes.Contains(s.LabelL))
                    {
                        if (!counts[ms.Activity].ContainsKey(s.LabelL))
                        {
                            counts[ms.Activity].Add(s.LabelL, 0);
                            scounts[ms.Activity].Add(s.LabelL, 0);
                        }
                        counts[ms.Activity][s.LabelL] += 1;
                        scounts[ms.Activity][s.LabelL] += s.SubstrokesL.Count;
                    }
                }
            }

            Console.WriteLine("{0} sketches loaded", sketches.Count);
            foreach (string a in activities)
            {
                foreach (string c in classes)
                {
                    Console.WriteLine("{0}-{1}: {2} shapes, {3} strokes/shape", a, c, counts[a][c], scounts[a][c] / counts[a][c]);
                }
            }

            List<int> training_users = new List<int>(new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32 });
            List<int> testing_users = new List<int>();
            int ntesters = (int)((double)(training_users.Count) * testing_percent);

            Random r = new Random();

            for (int i = 0; i < ntesters; i++)
            {
                int next = r.Next(training_users.Count);
                // next line adds testing user taken out of training users
                testing_users.Add(training_users[next]);
                training_users.RemoveAt(next);
            }

            training_users.Sort();
            testing_users.Sort();

            DollarRecognizer copyFrom = new DollarRecognizer(64, 48, Math.PI/20, new DollarRecognizer.distance_function(DollarRecognizer.HADistance));

            Console.WriteLine("Entering $1 tester");
            DollarRecognizer[] smallSets = makeSmallSets(training_users, copyFrom);
            TestDollar(smallSets, testing_users, "DollarHA", run_number);

            Console.WriteLine("Entering $N tester");
            NDollar[] smallSetsN = makeSmallSetsN(training_users);
            TestNDollar(smallSetsN, testing_users, run_number);

            Console.WriteLine("Entering Img tester");
            ImgRecognizer[] smallImgs = makeSmallSetsImg(training_users);
            TestImageBased(smallImgs, testing_users, run_number);

            copyFrom = new DollarRecognizer(64, 48, Math.PI / 20, new DollarRecognizer.distance_function(DollarRecognizer.SAdistance));
            Console.WriteLine("Entering $1 tester -- SADist");
            smallSets = makeSmallSets(training_users, copyFrom);
            TestDollar(smallSets, testing_users, "DollarSA", run_number);

            DateTime end = DateTime.Now;
            TimeSpan timer = end - start;
            Console.WriteLine("Total Time: {0}", timer);
            Console.Read();
        }

        // this was the original main, perfect for testing all 3 recognizers
        // on the gate study data and has commented out code for e85, too
        static void MainOriginal(string[] args)
        {
            DateTime start = DateTime.Now;
            int run_number = 0;
            while (Directory.Exists(string.Format("Run{0}", ++run_number))) ;
            Directory.CreateDirectory(string.Format("Run{0}", run_number));

            Console.WriteLine("Beginning Run #{0}", run_number);

            string[] hmc = Directory.GetFiles("e:\\sketch\\Data\\Gate Study Data\\LabeledSketches\\TabletPC\\HMC", "*.xml", SearchOption.AllDirectories);
            string[] ucr = Directory.GetFiles("e:\\sketch\\Data\\Gate Study Data\\LabeledSketches\\TabletPC\\UCR", "*.xml", SearchOption.AllDirectories);

            //string[] e85 = Directory.GetFiles("e:\\sketch\\Data\\E85\\New Labeled Documents\\Labeled Summer 2007", "*.xml");

            sketches = new List<MySketch>();

            // read HMC
            for (int i = 1; i <= 12; i++)
            {
                foreach (string file in hmc)
                {
                    // only care about and/or/not
                    if (file.Contains("NAND") || file.Contains("NOR") || file.Contains("XOR")) continue;
                    if (file.Contains(string.Format("\\{0}_", i))) // iso,copy
                    {
                        string act = file.Contains("COPY") ? "copy" : "iso";
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i, act));
                    }
                    else if (file.Contains(string.Format("_{0}_", i))) // synth
                    {
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i, "synth"));
                    }
                }
            }

            Console.WriteLine("Done reading HMC sketches");

            // read UCR
            for (int i = 1; i <= 12; i++)
            {
                foreach (string file in ucr)
                {
                    // only care about and/or/not
                    if (file.Contains("NAND") || file.Contains("NOR") || file.Contains("XOR")) continue;
                    if (file.Contains(string.Format("\\{0}_", i))) // iso,copy
                    {
                        string act = file.Contains("COPY") ? "copy" : "iso";
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i + 12, act));
                    }
                    else if (file.Contains(string.Format("_{0}_", i))) // synth
                    {
                        sketches.Add(new MySketch(new ReadXML(file).Sketch, i + 12, "synth"));
                    }
                }
            }

            foreach (MySketch ms in sketches)
            {
                if (!counts.ContainsKey(ms.Activity))
                {
                    counts.Add(ms.Activity, new Dictionary<string, int>());
                    scounts.Add(ms.Activity, new Dictionary<string, float>());
                }
                foreach (Shape s in ms.Sketch.ShapesL)
                {
                    if (classes.Contains(s.LabelL))
                    {
                        if (!counts[ms.Activity].ContainsKey(s.LabelL))
                        {
                            counts[ms.Activity].Add(s.LabelL, 0);
                            scounts[ms.Activity].Add(s.LabelL, 0);
                        }
                        counts[ms.Activity][s.LabelL] += 1;
                        scounts[ms.Activity][s.LabelL] += s.SubstrokesL.Count;
                    }
                }
            }

            Console.WriteLine("{0} sketches loaded", sketches.Count);
            foreach (string a in activities)
            {
                foreach (string c in classes)
                {
                    Console.WriteLine("{0}-{1}: {2} shapes, {3} strokes/shape", a, c, counts[a][c], scounts[a][c] / counts[a][c]);
                }
            }



            Console.WriteLine("Done reading UCR sketches");

            List<int> training_users = new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 });
            List<int> testing_users = new List<int>();
            int ntesters = (int)((double)(training_users.Count) * testing_percent);

            Random r = new Random();

            for (int i = 0; i < ntesters; i++)
            {
                int next = r.Next(training_users.Count);
                // next line adds testing user taken out of triaining users
                // commented out because all testing users come from E85 for this configuration
                testing_users.Add(training_users[next]);
                training_users.RemoveAt(next);
            }

            training_users.Sort();
            testing_users.Sort();

            //read E85 -- comment out if you're just doing UCR study stuff
            //foreach (string file in e85)
            //{
            //    testing_users.Add(int.Parse(file.Substring(file.LastIndexOf("\\")+1, 4)));
            //    sketches.Add(new MySketch(new ReadXML(file).Sketch, int.Parse(file.Substring(file.LastIndexOf("\\")+1, 4)), "synth"));
            //}


           // Console.WriteLine("Entering Rubine tester");
            // { iso, copy, synth }
          //  Rubine[] rubines = TrainRubine(training_users);
           // TestRubine(rubines, testing_users, run_number);

            DollarRecognizer copyFrom = new DollarRecognizer();

            Console.WriteLine("Entering $1 tester");
            DollarRecognizer[] smallSets = makeSmallSets(training_users, copyFrom);
            TestDollar(smallSets, testing_users, "Dollar", run_number);

            Console.WriteLine("Entering $N tester");
            NDollar[] smallSetsN = makeSmallSetsN(training_users);
            TestNDollar(smallSetsN, testing_users, run_number);

            Console.WriteLine("Entering Img tester");
            ImgRecognizer[] smallImgs = makeSmallSetsImg(training_users);
            TestImageBased(smallImgs, testing_users, run_number);

            //DollarRecognizer.distance_function hd = 
            //    new DollarRecognizer.distance_function(DollarRecognizer.HDdistance);
            //DollarRecognizer.distance_function mhd = 
            //    new DollarRecognizer.distance_function(DollarRecognizer.MHDdistance);
            //DollarRecognizer.distance_function phd = 
            //    new DollarRecognizer.distance_function(DollarRecognizer.PHDdistance);
            // Hausdorff stuff takes  a long time to run and didn't output anything conclusive
            //Hausdorff(run_number, hd, "Regular");
            //Hausdorff(run_number, mhd, "Modified");
            //Hausdorff(run_number, phd, "Partial");

            DateTime end = DateTime.Now;
            TimeSpan timer = end - start;
            Console.WriteLine("Total Time: {0}", timer);
            Console.Read();
        }

        #region Rubine
        #region Training
        /// <summary>
        /// Function to train the rubine recognizers needed for the study
        /// </summary>
        /// <param name="users"></param>
        /// <returns></returns>
        static Rubine[] TrainRubine(List<int> users)
        {
            Rubine[] res = new Rubine[activities.Length];
            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = new Rubine();
                res[i].train(makeData(users, activities[i]));
            }
            return res;
        }
        #endregion
        #region Testing
        /// <summary>
        /// Test the rubine classifier
        /// </summary>
        /// <param name="rubines"></param>
        /// <param name="users">users excluded from training set for cross validation</param>
        /// <param name="run"></param>
        static void TestRubine(Rubine[] rubines, List<int> users, int run)
        {
            int[][,] confusions = new int[9][,];
            for (int i = 0; i < activities.Length; i++) // i = training activity
            {
                Rubine current = rubines[i];
                for (int j = 0; j < activities.Length; j++) // j = testing activity
                {
                    confusions[i * activities.Length + j] = new int[classes.Count, classes.Count];
                    foreach (MySketch m in sketches)
                    {
                        if (m.Activity == activities[j]) // sketch is in current testing activity
                        {
                            if (i != j || users.Contains(m.Author)) // if we're doing cross-class, we can test on everyone's sketches
                                                                    // otherwise, we can only test on training users' sketches
                            {
                                List<Shape> ls = m.Sketch.ShapesL;
                                foreach (Shape s in ls)
                                {
                                    if (classes.Contains(s.LabelL))
                                    {
                                        string recognized = current.classify(s).BestLabel;
                                        confusions[i * activities.Length + j]
                                            [classes.IndexOf(s.LabelL), classes.IndexOf(recognized)]++;

                                    }
                                }
                            }
                        }
                    }
                }
            }


            if (!Directory.Exists(string.Format("Run{0}\\Rubine", run)))
                Directory.CreateDirectory(string.Format("Run{0}\\Rubine", run));
            WriteSummary(users, run, string.Format("Run{0}\\Rubine\\summary.txt", run));
            for (int i = 0; i < activities.Length; i++)
            {
                for (int j = 0; j < activities.Length; j++)
                {
                    string fname = string.Format("Run{0}\\Rubine\\{1}-{2}.csv", run, activities[i], activities[j]);
                    int ind = i * activities.Length + j;
                    writeCSV(run, confusions[ind], fname);
                }
            }
        }
        #endregion
        #endregion

        #region $1
        #region Training
        static Dictionary<string, List<Shape>> makeDollarSet(List<int> users, string activity, int n)
        {
            Dictionary<string, List<Shape>> unused;
            return makeDollarSet(users, activity, n, out unused);
        }

        /// <summary>
        /// Make a set of training data
        /// </summary>
        /// <param name="users">users to select examples from</param>
        /// <param name="activity">activity to select examples from</param>
        /// <param name="n">number of examples per class</param>
        /// <param name="unused">dictionary mapping class name to unused symbols of that class for these users, this activity</param>
        /// <returns>dictionary mapping class name to list of example symbols</returns>
        static Dictionary<string, List<Shape>> makeDollarSet
            (List<int> users, string activity, int n, out Dictionary<string, List<Shape>> unused)
        {
            unused = makeData(users, activity);
            Dictionary<string, List<Shape>> res = new Dictionary<string, List<Shape>>();

            Random r = new Random();

            for (int i = 0; i < n; i++)
            {
                foreach (string type in unused.Keys)
                {
                    if (!res.ContainsKey(type)) res.Add(type, new List<Shape>());
                    if (unused[type].Count == 0) continue;
                    int next = r.Next(unused[type].Count);
                    res[type].Add(unused[type][next]);
                    unused[type].RemoveAt(next);
                }
            }

            return res;
        }

        static DollarRecognizer[] makeSmallSets(List<int> users, DollarRecognizer copyFrom)
        {
            DollarRecognizer[] res = new DollarRecognizer[activities.Length];
            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = new DollarRecognizer(copyFrom);
                foreach (KeyValuePair<string, List<Shape>> kvp in makeDollarSet(users, activities[i], small_set))
                {
                    if (classes.Contains(kvp.Key))
                        res[i].addExamples(kvp.Key, kvp.Value);
                }
            }
            return res;
        }

        static NDollar[] makeSmallSetsN(List<int> users)
        {
            NDollar[] res = new NDollar[activities.Length];
            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = new NDollar();
                foreach (KeyValuePair<string, List<Shape>> kvp in makeDollarSet(users, activities[i], small_set))
                {
                    if (classes.Contains(kvp.Key))
                        res[i].addExamples(kvp.Key, kvp.Value);
                }
            }
            return res;
        }

        static DollarRecognizer[] makeUserSets(int user, out Dictionary<string, List<Shape>>[] unused, DollarRecognizer copyFrom)
        {
            List<int> users = new List<int>(new int[] { user });
            DollarRecognizer[] res = new DollarRecognizer[activities.Length];
            unused = new Dictionary<string, List<Shape>>[activities.Length];
            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = new DollarRecognizer(copyFrom);
                foreach (KeyValuePair<string, List<Shape>> kvp in makeDollarSet(users, activities[i], user_set, out unused[i]))
                {
                    if (classes.Contains(kvp.Key))
                        res[i].addExamples(kvp.Key, kvp.Value);
                }
            }
            return res;
        }

        static NDollar[] makeUserSetsN(int user, out Dictionary<string, List<Shape>>[] unused)
        {
            List<int> users = new List<int>(new int[] { user });
            NDollar[] res = new NDollar[activities.Length];
            unused = new Dictionary<string, List<Shape>>[activities.Length];
            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = new NDollar();
                foreach (KeyValuePair<string, List<Shape>> kvp in makeDollarSet(users, activities[i], user_set, out unused[i]))
                {
                    if (classes.Contains(kvp.Key))
                        res[i].addExamples(kvp.Key, kvp.Value);
                }
            }
            return res;
        }

        static Dictionary<string, List<Shape>>[] userShapes(int user)
        {
            Dictionary<string, List<Shape>>[] res = new Dictionary<string, List<Shape>>[activities.Length];

            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = makeData(new List<int>(new int[] { user }), activities[i]);
            }
            return res;
        }

        static DollarRecognizer[] combine(DollarRecognizer[] a, DollarRecognizer[] b, DollarRecognizer copyFrom)
        {
            DollarRecognizer[] res = new DollarRecognizer[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                res[i] = new DollarRecognizer(copyFrom);
                for (int j = 0; j < classes.Count; j++)
                {
                    res[i].addProcessedExamples(classes[j], a[i].getProcessedExamples(classes[j]));
                    res[i].addProcessedExamples(classes[j], b[i].getProcessedExamples(classes[j]));
                }
            }

            return res;
        }

        static NDollar[] combineN(NDollar[] a, NDollar[] b)
        {
            NDollar[] res = new NDollar[a.Length];

            for (int i = 0; i < a.Length; i++)
            {
                res[i] = new NDollar();
                for (int j = 0; j < classes.Count; j++)
                {
                    res[i].addProcessedExamples(classes[j], a[i].getProcessedExamples(classes[j]));
                    res[i].addProcessedExamples(classes[j], b[i].getProcessedExamples(classes[j]));
                }
            }

            return res;
        }
        #endregion

        #region Testing
        static void TestDollar(DollarRecognizer[] smalls, List<int> userList, String name, int run)
        {
            if (!Directory.Exists(string.Format("Run{0}\\{1}", run, name)))
                Directory.CreateDirectory(string.Format("Run{0}\\{1}", run, name));

            foreach (int u in userList)
            {
                // username formatting for UCR data
                string userName = string.Format("USER{0}", u);
                // username formatting for E85 data
                //string userName = "E85-" + u;
                if (!Directory.Exists(string.Format("Run{0}\\{2}\\{1}", run, userName, name)))
                    Directory.CreateDirectory(string.Format("Run{0}\\{2}\\{1}", run, userName, name));
                if (!Directory.Exists(string.Format("Run{0}\\{2}\\{1}\\GlobalSet", run, userName, name)))
                    Directory.CreateDirectory(string.Format("Run{0}\\{2}\\{1}\\GlobalSet", run, userName, name));
                if (!Directory.Exists(string.Format("Run{0}\\{2}\\{1}\\UserSet", run, userName, name)))
                    Directory.CreateDirectory(string.Format("Run{0}\\{2}\\{1}\\UserSet", run, userName, name));
                if (!Directory.Exists(string.Format("Run{0}\\{2}\\{1}\\ComboSet", run, userName, name)))
                    Directory.CreateDirectory(string.Format("Run{0}\\{2}\\{1}\\ComboSet", run, userName, name));

                Dictionary<string, List<Shape>>[] testingShapes;
                Dictionary<string, List<Shape>>[] allShapes = userShapes(u);
                DollarRecognizer[] users = makeUserSets(u, out testingShapes, smalls[0]);
                DollarRecognizer[] combs = combine(smalls, users, smalls[0]);
                DateTime start, end;


                for (int i = 0; i < activities.Length; i++)
                {

                    DollarRecognizer small = smalls[i];
                    DollarRecognizer user = users[i];
                    DollarRecognizer comb = combs[i];

                    for (int j = 0; j < activities.Length; j++)
                    {

                        int[][,] confusion = new int[3][,];
                        confusion[0] = new int[classes.Count, classes.Count];
                        confusion[1] = new int[classes.Count, classes.Count];
                        confusion[2] = new int[classes.Count, classes.Count];

                        for (int correctNumber = 0; correctNumber < classes.Count; correctNumber++)
                        {
                            foreach (Shape s in allShapes[j][classes[correctNumber]])
                            {
                                start = DateTime.Now;
                                confusion[0][correctNumber, classes.IndexOf(small.classify(s))]++;
                                end = DateTime.Now;
                                total += (end - start);
                                nclassified += 1;
                                if (i != j)
                                {
                                    start = DateTime.Now;
                                    string x = user.classify(s);
                                    if (x != null)
                                        confusion[1][correctNumber, classes.IndexOf(x)]++;
                                    x = comb.classify(s);
                                    if (x != null)
                                        confusion[2][correctNumber, classes.IndexOf(x)]++;
                                    end = DateTime.Now;
                                    total += (end - start);
                                    nclassified += 2;
                                }
                            }
                            foreach (Shape s in testingShapes[j][classes[correctNumber]])
                            {
                                if (i == j)
                                {
                                    start = DateTime.Now;
                                    confusion[1][correctNumber, classes.IndexOf(user.classify(s))]++;
                                    confusion[2][correctNumber, classes.IndexOf(comb.classify(s))]++;
                                    end = DateTime.Now;
                                    total += (end - start);
                                    nclassified += 2;
                                }
                            }
                        }

                        writeCSV(run, confusion[0], string.Format("Run{0}\\{4}\\{1}\\GlobalSet\\{2}-{3}.csv", run, userName, activities[i], activities[j], name));
                        writeCSV(run, confusion[1], string.Format("Run{0}\\{4}\\{1}\\UserSet\\{2}-{3}.csv", run, userName, activities[i], activities[j], name));
                        writeCSV(run, confusion[2], string.Format("Run{0}\\{4}\\{1}\\ComboSet\\{2}-{3}.csv", run, userName, activities[i], activities[j], name));
                    }

                }
            }
            Console.WriteLine("total classification time: {0}", total);
            Console.WriteLine("total symbols classified: {0}", nclassified);
            Console.WriteLine("average ms/symbol: {0}", total.TotalMilliseconds / nclassified);
        }
        #endregion
        #endregion

        #region NDollar
        #region Testing
        static void TestNDollar(NDollar[] smalls, List<int> userList, int run)
        {
            nclassified = 0;
            if (!Directory.Exists(string.Format("Run{0}\\NDollar", run)))
                Directory.CreateDirectory(string.Format("Run{0}\\NDollar", run));

            foreach (int u in userList)
            {
                Console.WriteLine("User {0}", u);
                // username formatting for UCR data
                string userName = string.Format("USER{0}", u);
                // username formatting for E85 data
                //string userName = "E85-" + u;
                if (!Directory.Exists(string.Format("Run{0}\\NDollar\\{1}", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\NDollar\\{1}", run, userName));
                if (!Directory.Exists(string.Format("Run{0}\\NDollar\\{1}\\GlobalSet", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\NDollar\\{1}\\GlobalSet", run, userName));
                if (!Directory.Exists(string.Format("Run{0}\\NDollar\\{1}\\UserSet", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\NDollar\\{1}\\UserSet", run, userName));
                if (!Directory.Exists(string.Format("Run{0}\\NDollar\\{1}\\ComboSet", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\NDollar\\{1}\\ComboSet", run, userName));

                Dictionary<string, List<Shape>>[] testingShapes;
                Dictionary<string, List<Shape>>[] allShapes = userShapes(u);
                NDollar[] users = makeUserSetsN(u, out testingShapes);
                NDollar[] combs = combineN(smalls, users);
                DateTime start, end;


                for (int i = 0; i < activities.Length; i++)
                {

                    NDollar small = smalls[i];
                    NDollar user = users[i];
                    NDollar comb = combs[i];

                    for (int j = 0; j < activities.Length; j++)
                    {
                       
                        int[][,] confusion = new int[3][,];
                        confusion[0] = new int[classes.Count, classes.Count];
                        confusion[1] = new int[classes.Count, classes.Count];
                        confusion[2] = new int[classes.Count, classes.Count];

                        for (int correctNumber = 0; correctNumber < classes.Count; correctNumber++)
                        {
                            foreach (Shape s in allShapes[j][classes[correctNumber]])
                            {
                                start = DateTime.Now;
                                confusion[0][correctNumber, classes.IndexOf(small.classify(s))]++;
                                end = DateTime.Now;
                                total += (end - start);
                                nclassified += 1;
                                if (i != j)
                                {
                                    start = DateTime.Now;
                                    string x = user.classify(s);
                                    if (x != null)
                                        confusion[1][correctNumber, classes.IndexOf(x)]++;
                                    x = comb.classify(s);
                                    if (x != null)
                                        confusion[2][correctNumber, classes.IndexOf(x)]++;
                                    end = DateTime.Now;
                                    total += (end - start);
                                    nclassified += 2;
                                }
                            }
                            foreach (Shape s in testingShapes[j][classes[correctNumber]])
                            {
                                if (i == j)
                                {
                                    start = DateTime.Now;
                                    confusion[1][correctNumber, classes.IndexOf(user.classify(s))]++;
                                    confusion[2][correctNumber, classes.IndexOf(comb.classify(s))]++;
                                    end = DateTime.Now;
                                    total += (end - start);
                                    nclassified += 2;
                                }
                            }
                        }

                        writeCSV(run, confusion[0], string.Format("Run{0}\\NDollar\\{1}\\GlobalSet\\{2}-{3}.csv", run, userName, activities[i], activities[j]));
                        writeCSV(run, confusion[1], string.Format("Run{0}\\NDollar\\{1}\\UserSet\\{2}-{3}.csv", run, userName, activities[i], activities[j]));
                        writeCSV(run, confusion[2], string.Format("Run{0}\\NDollar\\{1}\\ComboSet\\{2}-{3}.csv", run, userName, activities[i], activities[j]));
                    }

                }
            }
            Console.WriteLine("total classification time: {0}", total);
            Console.WriteLine("total symbols classified: {0}", nclassified);
            Console.WriteLine("average ms/symbol: {0}", total.TotalMilliseconds / nclassified);
        }
        #endregion
        #endregion

        #region Image-Based

        #region Training
        /// <summary>
        /// Make a set of user-independent training data for each activity
        /// </summary>
        /// <param name="users"></param>
        /// <returns></returns>
        static ImgRecognizer[] makeSmallSetsImg(List<int> users)
        {
            ImgRecognizer[] res = new ImgRecognizer[activities.Length];
            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = new ImgRecognizer();
                foreach (KeyValuePair<string, List<Shape>> kvp in makeDollarSet(users, activities[i], small_set))
                {
                    if (classes.Contains(kvp.Key))
                        foreach (Shape s in kvp.Value)
                        {
                            res[i].Add(new BitmapSymbol(s));
                        }
                }
            }
            return res;
        }

        /// <summary>
        /// Make a set of user-specific training data for each activity
        /// </summary>
        /// <param name="user"></param>
        /// <param name="unused"></param>
        /// <returns></returns>
        static ImgRecognizer[] makeUserSetsImg(int user, out Dictionary<string, List<Shape>>[] unused)
        {
            List<int> users = new List<int>(new int[] { user });
            ImgRecognizer[] res = new ImgRecognizer[activities.Length];
            unused = new Dictionary<string, List<Shape>>[activities.Length];
            for (int i = 0; i < activities.Length; i++)
            {
                res[i] = new ImgRecognizer();
                foreach (KeyValuePair<string, List<Shape>> kvp in makeDollarSet(users, activities[i], user_set, out unused[i]))
                {
                    if (classes.Contains(kvp.Key))
                        foreach (Shape s in kvp.Value)
                            res[i].Add(new BitmapSymbol(s));
                }
            }
            return res;
        }
        #endregion

        #region Testing

        /// <summary>
        /// Combine two sets of ImgRecognizers
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        static ImgRecognizer[] combineImg(ImgRecognizer[] a, ImgRecognizer[] b)
        {
            ImgRecognizer[] res = new ImgRecognizer[a.Length];

            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = new ImgRecognizer();
                res[i].AddRange(a[i]);
                res[i].AddRange(b[i]);
            }

            return res;
        }

        static void TestImageBased(ImgRecognizer[] smalls, List<int> userList, int run)
        {
            if (!Directory.Exists(string.Format("Run{0}\\Img", run)))
                Directory.CreateDirectory(string.Format("Run{0}\\Img", run));

            foreach (int u in userList)
            {
                // username formatting for UCR data
                string userName =  string.Format("USER{0}", u);
                // username formatting for E85 data
                //string userName = "E85-" + u;
                if (!Directory.Exists(string.Format("Run{0}\\Img\\{1}", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\Img\\{1}", run, userName));
                if (!Directory.Exists(string.Format("Run{0}\\Img\\{1}\\GlobalSet", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\Img\\{1}\\GlobalSet", run, userName));
                if (!Directory.Exists(string.Format("Run{0}\\Img\\{1}\\UserSet", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\Img\\{1}\\UserSet", run, userName));
                if (!Directory.Exists(string.Format("Run{0}\\Img\\{1}\\ComboSet", run, userName)))
                    Directory.CreateDirectory(string.Format("Run{0}\\Img\\{1}\\ComboSet", run, userName));

                Dictionary<string, List<Shape>>[] testingShapes;
                Dictionary<string, List<Shape>>[] allShapes = userShapes(u);
                ImgRecognizer[] users = makeUserSetsImg(u, out testingShapes);
                ImgRecognizer[] combs = combineImg(smalls, users);
                Dictionary<string, List<SymbolRank>> blah;


                for (int i = 0; i < activities.Length; i++)
                {

                    ImgRecognizer small = smalls[i];
                    ImgRecognizer user = users[i];
                    ImgRecognizer comb = combs[i];

                    for (int j = 0; j < activities.Length; j++)
                    {

                        int[][,] confusion = new int[3][,];
                        confusion[0] = new int[classes.Count, classes.Count];
                        confusion[1] = new int[classes.Count, classes.Count];
                        confusion[2] = new int[classes.Count, classes.Count];

                        for (int correctNumber = 0; correctNumber < classes.Count; correctNumber++)
                        {
                            foreach (Shape s in allShapes[j][classes[correctNumber]])
                            {
                                BitmapSymbol globalS = new BitmapSymbol(s);
                                BitmapSymbol userS = new BitmapSymbol(s);
                                BitmapSymbol comboS = new BitmapSymbol(s);
                                confusion[0][correctNumber, classes.IndexOf(globalS.FindSimilarity_and_Rank(small, out blah)[0])]++;
                                if (i != j)
                                {
                                    string x = userS.FindSimilarity_and_Rank(user, out blah)[0];
                                    if (x != null)
                                        confusion[1][correctNumber, classes.IndexOf(x)]++;
                                    x = comboS.FindSimilarity_and_Rank(comb, out blah)[0];
                                    if (x != null)
                                        confusion[2][correctNumber, classes.IndexOf(x)]++;
                                }
                            }
                            foreach (Shape s in testingShapes[j][classes[correctNumber]])
                            {
                                if (i == j)
                                {
                                    BitmapSymbol userS = new BitmapSymbol(s);
                                    BitmapSymbol comboS = new BitmapSymbol(s);
                                    confusion[1][correctNumber, classes.IndexOf(userS.FindSimilarity_and_Rank(user, out blah)[0])]++;
                                    confusion[2][correctNumber, classes.IndexOf(comboS.FindSimilarity_and_Rank(comb, out blah)[0])]++;
                                }
                            }
                        }

                        writeCSV(run, confusion[0], string.Format("Run{0}\\Img\\{1}\\GlobalSet\\{2}-{3}.csv", run, userName, activities[i], activities[j]));
                        writeCSV(run, confusion[1], string.Format("Run{0}\\Img\\{1}\\UserSet\\{2}-{3}.csv", run, userName, activities[i], activities[j]));
                        writeCSV(run, confusion[2], string.Format("Run{0}\\Img\\{1}\\ComboSet\\{2}-{3}.csv", run, userName, activities[i], activities[j]));
                    }

                }
            }
        }
        #endregion

        #endregion

        #region Utility
        /// <summary>
        /// Write out a CSV for a confusion matrix
        /// </summary>
        /// <param name="run"></param>
        /// <param name="confusions"></param>
        /// <param name="fname"></param>
        private static void writeCSV(int run, int[,] confusions, string fname)
        {
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(fname);
                // Write headers: observed gate labels across top
                // actual gate labels 1st column
                sw.Write("observed/actual");
                for (int x = 0; x < classes.Count; x++)
                    sw.Write("," + classes[x]);
                sw.Write(",Precision,Recall\n");
                // Write results
                for (int x = 0; x < classes.Count; x++)
                {
                    sw.Write(classes[x] + ",");
                    for (int y = 0; y <= classes.Count+1; y++)
                    {
                        if (y < classes.Count) sw.Write(confusions[x, y] + ",");
                        else if (y == classes.Count) sw.Write(string.Format("={0}{1}*100/SUM(B{1}:{2}{1}),",
                            (char)('B' + x), x + 2, (char)('A' + classes.Count)));
                        else sw.Write(string.Format("={0}{1}*100/SUM({0}2:{0}{2})\n",
                            (char)('B' + x), x + 2, 1 + classes.Count));
                    }
                }
            }
            finally
            {
                if (sw != null)
                    sw.Close();
            }
        }

        /// <summary>
        /// collect training data for a set of users and a given activity
        /// </summary>
        /// <param name="users"></param>
        /// <param name="activity"></param>
        /// <returns></returns>
        static Dictionary<string, List<Shape>> makeData(List<int> users, string activity)
        {
            Dictionary<string, List<Shape>> data = new Dictionary<string, List<Shape>>();
            foreach (MySketch m in sketches)
            {
                if (users.Contains(m.Author) && m.Activity == activity)
                {
                    List<Shape> ls = m.Sketch.ShapesL;
                    foreach (Shape s in ls)
                    {
                        if (classes.Contains(s.LabelL))
                        {
                            if (!data.ContainsKey(s.LabelL))
                                data.Add(s.LabelL, new List<Shape>());
                            data[s.LabelL].Add(s);
                        }

                    }
                }
            }

            foreach (string c in classes)
            {
                if (!data.ContainsKey(c)) data.Add(c, new List<Shape>());
            }

            return data;
        }

        /// <summary>
        /// Write out a list of testing users
        /// </summary>
        /// <param name="users"></param>
        /// <param name="run"></param>
        /// <param name="filename"></param>
        private static void WriteSummary(List<int> users, int run, string filename)
        {
            StreamWriter stw = null;
            try
            {
                stw = new StreamWriter(filename);
                stw.Write("Testing Users:\n");
                foreach (int user in users)
                {
                    string school = (user > 12) ? "UCR" : "HMC";
                    int number = (user > 12) ? user - 12 : user;
                    stw.Write(string.Format("  {0} #{1}\n", school, number));
                }
            }
            finally
            {
                if (stw != null) stw.Close();
            }
        }
        #endregion

    }
}
