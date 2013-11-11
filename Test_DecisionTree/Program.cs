using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Utilities;
using DecisionTreeClassifier;

namespace Test_DecisionTree
{
    class Program
    {
        static void Main(string[] args)
        {
            List<string> featureNames = new List<string>();
            /*featureNames.Add("s1");
            featureNames.Add("c1");
            featureNames.Add("s2");
            featureNames.Add("c2");
            featureNames.Add("s3");
            featureNames.Add("c3");
            featureNames.Add("s4");
            featureNames.Add("c4");
            featureNames.Add("s5");
            featureNames.Add("c5");*/

            // Read in an artificial data file of discrete values
            //string file = "C:\\Documents and Settings\\eric\\Desktop\\poker-hand-training-true.data";
            string file = "C:\\Documents and Settings\\eric\\My Documents\\Research\\CURRENT\\DC Feature Files\\Weka v5\\SingleStrokeTraining_01.arff";
            StreamReader reader = new StreamReader(file);

            List<Instance> instances = new List<Instance>();
            string line;
            reader.ReadLine(); // Ignore the @RELATION line

            while ((line = reader.ReadLine()) != null && line != "")
            {
                string[] split = line.Split("'".ToCharArray());
                if (split.Length > 1)
                {
                    string feature = split[1];
                    featureNames.Add(feature);
                }
            }

            reader.ReadLine(); // Ignore the @DATA line

            while ((line = reader.ReadLine()) != null && line != "")
            {
                string[] split = line.Split(",".ToCharArray());
                string cls = split[split.Length - 1];
                List<object> values = new List<object>();
                for (int i = 0; i < split.Length - 1; i++)
                    values.Add(GetValueAsObject(split[i]));

                instances.Add(new Instance(cls, featureNames, values, 1.0));
            }

            reader.Close();


            // Read in testing data
            //string testFile = "C:\\Documents and Settings\\eric\\Desktop\\poker-hand-testing.data";
            string testFile = "C:\\Documents and Settings\\eric\\My Documents\\Research\\CURRENT\\DC Feature Files\\Weka v5\\01_features.txt";

            StreamReader testReader = new StreamReader(testFile);

            testReader.ReadLine(); testReader.ReadLine(); // Ignore first two lines

            List<Instance> testInstances = new List<Instance>();
            string testLine;
            while ((testLine = testReader.ReadLine()) != null && testLine != "")
            {
                string[] split = testLine.Split(",".ToCharArray());
                string cls = "None";
                if (split[split.Length - 1].Trim() == "1")
                    cls = "Label";
                else if (split[split.Length - 2].Trim() == "1")
                    cls = "Wire";
                else if (split[split.Length - 3].Trim() == "1")
                    cls = "Gate";
                List<object> values = new List<object>();
                for (int i = 0; i < split.Length - 3; i++)
                    values.Add(GetValueAsObject(split[i]));

                testInstances.Add(new Instance(cls, featureNames, values, 1.0));
            }

            testReader.Close();

            // Create Tree
            TreeNode root = new TreeNode(instances, true, 3);

            // Test tree
            List<string> allClasses = new List<string>();
            /*allClasses.Add("0");
            allClasses.Add("1");
            allClasses.Add("2");
            allClasses.Add("3");
            allClasses.Add("4");
            allClasses.Add("5");
            allClasses.Add("6");
            allClasses.Add("7");
            allClasses.Add("8");
            allClasses.Add("9");*/
            allClasses.Add("Gate");
            allClasses.Add("Wire");
            allClasses.Add("Label");

            ConfusionMatrix matrix = new ConfusionMatrix(allClasses);
            foreach (Instance I in testInstances)
            {
                string cls = root.Classify(I);
                matrix.Add(I.Classification, cls);
            }

            StreamWriter writer = new StreamWriter("C:\\Documents and Settings\\eric\\Desktop\\ssClassificationDC_01.txt");
            matrix.Print(writer);
            writer.Close();
        }

        private static object GetValueAsObject(string p)
        {
            int i;
            bool gi = int.TryParse(p, out i);
            if (gi)
                return (object)i;

            double d;
            bool gd = double.TryParse(p, out d);
            if (gd)
                return (object)d;

            decimal n;
            bool gn = decimal.TryParse(p, out n);
            if (gn)
                return (object)n;

            return (object)p;
        }
    }
}
