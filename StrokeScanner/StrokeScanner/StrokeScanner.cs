/// Jason Fennell
/// March 8, 2008
/// 
/// This application is supposed to scan a box along the points in a sketch in the order they were
/// drawn in time.  This box jumps by increments and at every increment detects the strokes around it
/// (currently by looking for overlapping bounding boxes) and groups that set of strokes.  Each
/// of these groups have post-processing run on them after this scan to look for features that define
/// transitions between strokes.

using Sketch;
using ConverterXML;
using System.Drawing;
using Set;

using System;
using System.Collections.Generic;
using System.Text;

using libsvm;
using Svm;

using System.Threading;

namespace StrokeScanner
{
    internal class SSSVM : ClassifySVM
    {
        private ScaleParameters tp;
        internal SSSVM(string s) : base(s+".model") 
        {
            tp = ScaleParameters.LoadDesignation(s + ".param");
        }

        public new int predict(svm_node[] x)
        {
            if (x.Length != tp.scale_factors.Length)
                throw new Exception("given vector has wrong dimension.");

            for (int i = 0; i < x.Length; i++)
            {
                x[i].value_Renamed /= tp.scale_factors[i];
            }
            return (int)base.predict(x);
        }

        public override string predictToString(int predict)
        {
            return intToString(predict);
        }

        public static string intToString(int predict)
        {
            if (predict == 1) return "transition found";
            return "no transition";
        }
    }

    public class StrokeScanner
    {
        private const int INCREMENT = 2;
        private const double scannerWidth = 100;
        private const double scannerHeight = 100;

        #region Color List
        /// <summary>
        /// List of colors
        /// </summary>
        private static readonly string[] colorList = new string[]
			{	"Blue", "Crimson", "Green", "Purple", "Orange", "Pink", 
                "PowderBlue", "MistyRose", "Salmon", "MediumAquamarine", 
				"Yellow", "Wheat", "Aqua", "Aquamarine", 
                "BlueViolet", "BurlyWood", 
                "CadetBlue", "Chartreuse", "Chocolate", "Coral", 
                "Cornflower", "Brown", "Cyan", "DarkBlue", 
                "DarkCyan", "DarkGoldenrod", "DarkGray", "DarkGreen",
                "DarkKhaki", "DarkMagenta", "DarkOliveGreen", "DarkOrange", 
                "DarkOrchid", "DarkRed", "DarkSalmon", "DarkSeaGreen", 
                "DarkSlateBlue", "DarkSlateGray", "DarkTurquoise", 
                "DarkViolet", "DeepPink", "DeepSkyBlue", "DimGray",
		        "DodgerBlue", "Firebrick", "FloralWhite", "ForestGreen",
			    "Fuchsia", "Gainsboro",	"GhostWhite", "Gold", "Goldenrod",
			    "Gray", "GreenYellow", "Honeydew",	"HotPink",
			    "IndianRed", "Indigo", "Ivory", "Khaki", "Lavender",
			    "LavenderBlush", "LawnGreen", "LemonChiffon", "LightBlue",
			    "LightCoral", "LightCyan", "LightGoldenrodYellow",
			    "LightGray", "LightGreen", "LightPink", "LightSalmon",
			    "LightSeaGreen", "LightSkyBlue", "LightSlateGray", 
                "LightSteelBlue", "LightYellow", "Lime", "LimeGreen", 
                "Linen", "Magenta", "Maroon", 
                "MediumBlue", "MediumOrchid", "MediumPurple", 
                "MediumSeaGreen", "MediumSlateBlue", "MediumSpringGreen", 
                "MediumTurquoise", "MediumVioletRed", "MidnightBlue", 
                "MintCream", "Moccasin", "NavajoWhite", "Navy", 
                "OldLace", "Olive", "OliveDrab", "OrangeRed", 
                "Orchid", "PaleGoldenrod", "PaleGreen", "PaleTurquoise", 
                "PaleVioletRed", "PapayaWhip", "PeachPuff", "Peru", 
                "Plum", "Red", "RosyBrown", 
                "RoyalBlue", "SaddleBrown", "SandyBrown", 
                "SeaGreen", "SeaShell", "Sienna", "Silver", "SkyBlue", 
                "SlateBlue", "SlateGray", "Snow", "SpringGreen", "SteelBlue", 
                "Tan", "Teal", "Thistle", "Tomato", "Transparent", 
                "Turquoise", "Violet", "WhiteSmoke", "YellowGreen"};
        #endregion

        #region INTERNALS
        List<Scanner> scanners;
        SSSVM csvm;
        #endregion

        /// <summary>
        /// Constructor to create a StrokeScanner that cannot be used for classification.
        /// However, it might be useful for training.
        /// </summary>
        public StrokeScanner()
        {
            scanners = new List<Scanner>();
            csvm = null;
        }

        /// <summary>
        /// Constructor creates an SVM instance out of the given model file.
        /// </summary>
        /// <param name="modelfile"></param>
        public StrokeScanner(string modelfile)
        {
            scanners = new List<Scanner>();
            csvm = new SSSVM(modelfile);
        }

        static void Main(string[] args)
        {
            TestMain(args);
            Console.Read();
        }

        static void TrainMain(string[] args)
        {
            DateTime x = DateTime.Now;
            string[] s = new string[] {/*"1106_3.8.1.labeled.xml", */"0268_3.3.1.labeled.xml", 
                "1357_2.7.1.labeled.xml", "4242_1.2.1.labeled.xml", "1585_2.6.1.labeled.xml"/*, 
                "3141_3.5.1.labeled.xml", "1106_3.8.1.labeled.xml"*/ };
            new StrokeScanner().train(s, "scanbox_model");

            Console.WriteLine("Done.  Total Time: {0}", DateTime.Now-x);
        }

        static void TestMain(string[] args)
        {
            //ConverterXML.ReadXML xmlHolder = new ConverterXML.ReadXML("1106_3.8.1.labeled.xml");
            ConverterXML.ReadXML xmlHolder = new ConverterXML.ReadXML("1585_2.6.1.labeled.xml");
            Sketch.Sketch sketchHolder = xmlHolder.Sketch;

            RemoveLabels(ref sketchHolder);

            StrokeScanner ss = new StrokeScanner("scanbox_model");
            ss.group(sketchHolder);

            foreach (Scanner s in ss.scanners)
            {
                s.addToSketch(ref sketchHolder);
            }

            ConverterXML.MakeXML xmlWriter = new MakeXML(sketchHolder);
            xmlWriter.WriteXML("testOutput.xml");

            writeDomainFile("testDomain.txt", 2);
            ss.writeScanboxOnly("scanboxes.xml");
        }

        public void group(Sketch.Sketch sketch)
        {
            if (csvm == null) 
                throw new Exception("No Model File Loaded.  Cannot proceed with SVM based grouping.");

            scanners = new List<Scanner>();
            findScanboxes(sketch);

            for (int i = 0; i < scanners.Count; i++)
            {
                scanners[i].ClusterLabel = csvm.predict(scanners[i].SvmNodes());
            }
        }

        /// <summary>
        /// Train the stroke scanner.  Writes SVM model to the specified file.
        /// Takes in training data in the form of labeled sketches.
        /// First removes all unnecessary substrokes, like labels and unlabeled strokes.
        /// Then find scanboxes.
        /// Finally make feature vectors and use those for SVM stuff.
        /// </summary>
        /// <param name="files">Files from which to pull training data.</param>
        /// <param name="outfile">File to which the trained SVM model should be output.</param>
        public void train(string[] files, string outfile)
        {
            scanners = new List<Scanner>();

            foreach (string filename in files)
            {
                #region Load Sketch
                // Read in the sketch to be scanned.
                ConverterXML.ReadXML xmlHolder = new ConverterXML.ReadXML(filename);
                Sketch.Sketch sketchHolder = xmlHolder.Sketch;
                #endregion

                RemoveLabels(ref sketchHolder);

                findScanboxes(sketchHolder);
            }

            svm_problem prob = get_problem();
            double[] scales = scale(ref prob);
            ScaleParameters tp = new ScaleParameters(scales.Length);
            tp.scale_factors = scales;
            tp.writeToFile(outfile + ".param");

            TrainSVM tsvm = new TrainSVM(prob, outfile+".model");
            tsvm.start();

        }

        /// <summary>
        /// Scale all dimensions to [-1,1] or [0,1] or [-1,0] and return the scale factors.
        /// Does not scale any dimensions that already have maxima inside [-1,1].
        /// </summary>
        /// <param name="problem">svm_problem to scale</param>
        /// <returns></returns>
        private double[] scale(ref svm_problem problem)
        {
            double[] res = new double[problem.x[0].Length];

            for (int i = 0; i < res.Length; i++) res[i] = 0d;

            // find scale factors
            for (int i = 0; i < problem.x.Length; i++)
            {
                for (int j = 0; j < problem.x[i].Length; j++)
                {
                    double test = Math.Abs(problem.x[i][j].value_Renamed);
                    if (test > res[j]) res[j] = test;
                }
            }

            // do scaling, ignore dimensions that don't need to be scaled
            for (int i = 0; i < problem.x.Length; i++)
            {
                for (int j = 0; j < problem.x[i].Length; j++)
                {
                    if (res[j] < 1)
                    {
                        res[j] = 1;
                    }
                    else
                    {
                        problem.x[i][j].value_Renamed /= res[j];
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Find scanboxes in the given sketch.
        /// </summary>
        /// <param name="sketch">a sketch</param>
        private void findScanboxes(Sketch.Sketch sketch)
        {
            #if DEBUG
            Console.WriteLine("Beginning scan");
            #endif

            List<Substroke> substrokes = sketch.SubstrokesL;
            List<Sketch.Point> points = new List<Sketch.Point>(sketch.Points);
            points.Sort();

            for (int p = 0; p < (points.Count - INCREMENT); p += INCREMENT)
            {
                Scanner scanner = new Scanner(scannerBox(points[p]));

                for (int s = 0; s < substrokes.Count; ++s)
                {
                    scanner.testAndAdd(substrokes[s]);
                }

                if (scanner.PotentialTransition)
                {
                    scanners.Add(scanner);
                }
            }

            splitAndCenterScanners(ref scanners);
            reduceScanners(ref scanners);
            
            #if DEBUG
            Console.WriteLine("Scan complete.");
            #endif
        }

        /// <summary>
        /// Create an svm_problem out of this instace.
        /// Assumes we already have a list of scanners in 'scanners'
        /// </summary>
        /// <returns>properly filled in svm_problem object</returns>
        public svm_problem get_problem()
        {
            svm_problem res = new svm_problem();
            res.l = scanners.Count;
            res.y = new double[res.l];
            res.x = new svm_node[res.l][];

            for (int i = 0; i < res.l; i++)
            {
                res.x[i] = scanners[i].SvmNodes();
                res.y[i] = scanners[i].Label;
            }
            return res;
        }

        /// <summary>
        /// Split scanboxes such that they contain only two substrokes and ensure all 
        /// scanboxes are centered on the closest point between the two substrokes
        /// </summary>
        /// <param name="scanners">list of scanboxes to simplify</param>
        private void splitAndCenterScanners(ref List<Scanner> scanners)
        {
            int i;

            for (i = 0; i < scanners.Count; i++)
            {
                List<Substroke> subs = scanners[i].ScanGroup.AsList();
                if (scanners[i].ScanGroup.Count > 2)
                {
                    for (int x = 0; x < scanners[i].ScanGroup.Count - 1; x++)
                    {
                        for (int y = x + 1; y < scanners[i].ScanGroup.Count; y++)
                        {
                            Scanner center = new Scanner(minRectBetweenStrokes(subs[x], subs[y]));
                            center.testAndAdd(subs[x]);
                            center.testAndAdd(subs[y]);
                            scanners.Add(center);
                        }
                    }
                    scanners.RemoveAt(i--);
                }
                else if (scanners[i].ScanGroup.Count == 2)
                {
                    Scanner center = new Scanner(minRectBetweenStrokes(subs[0], subs[1]));
                    center.testAndAdd(subs[0]);
                    center.testAndAdd(subs[1]);
                    scanners[i] = center;
                }
                else
                {
                    scanners.RemoveAt(i--);
                }
            }
        }

        /// <summary>
        /// get rid of duplicate scanboxes
        /// </summary>
        /// <param name="scanners"></param>
        private void reduceScanners(ref List<Scanner> scanners)
        {
            for (int i = 0; i < scanners.Count; i++)
            {
                for (int j = i + 1; j < scanners.Count; j++)
                {
                    if (scanners[i].ScanGroup.Equals(scanners[j].ScanGroup) &&
                        Rect.overlap(scanners[i].ScanBox, scanners[j].ScanBox))
                    {
                        scanners.RemoveAt(j--);
                    }
                }
            }
        }

        #region Utilities
        /// <summary>
        /// Writes out a special domain file specific to this sketch. Also modifies the sketch so that
        /// all shape names are unique (this will break other code modules, but will allow you to view
        /// groupings in the new labeler).        
        /// </summary>
        /// <param name="filename"></param>
        private static void writeDomainFile(string filename, int numGroups)
        {
            filename += ".domain.txt";

            System.IO.StreamWriter domain_writer = new System.IO.StreamWriter(filename, false);
            domain_writer.WriteLine("Clustering Research");
            domain_writer.WriteLine("Special debug domain file for " + filename);

            domain_writer.WriteLine(String.Format("Background {0} {1}", 0, colorList[0]));

            for (int i = 0; i < numGroups; ++i)
            {

                domain_writer.WriteLine(String.Format("Group{0} {1} {2}", i, i + 1, 
                    colorList[(i + 1) % colorList.Length]));
            }
            domain_writer.Close();
        }

        /// <summary>
        /// write out a sketch with just the scanboxes and their contents
        /// </summary>
        /// <param name="filename"></param>
        private void writeScanboxOnly(string filename)
        {

            Sketch.Sketch s = new Sketch.Sketch();
            s.XmlAttrs.Id = Guid.NewGuid();

            foreach (Scanner scan in scanners)
            {
                foreach (Substroke sub in scan.ScanWindow)
                {
                    Stroke stroke = new Stroke(sub);
                    stroke.XmlAttrs.Id = Guid.NewGuid();
                    stroke.XmlAttrs.Time = (ulong)DateTime.Now.Ticks;
                    stroke.XmlAttrs.Type = "stroke";
                    stroke.XmlAttrs.Name = "stroke";
                    s.AddStroke(stroke);
                }

                Shape shape = new Shape(scan.ScanWindow, new XmlStructs.XmlShapeAttrs(true));
                shape.XmlAttrs.Time = (ulong)DateTime.Now.Ticks;
                shape.XmlAttrs.Type = "Background";
                shape.XmlAttrs.Name = "shape";

                s.AddShape(shape);
                //scan.addToSketch(ref s);
            }

            MakeXML mxml = new MakeXML(s);
            mxml.WriteXML(filename);
        }

        /// <summary>
        /// Return the rectangle that defines the scanner box around a given point.
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public static Rect scannerBox(Sketch.Point pt)
        {
            double xDelta = scannerWidth / 2;
            double yDelta = scannerHeight / 2;

            return new Rect(pt.X - xDelta, pt.Y - yDelta, pt.X + xDelta, pt.Y + yDelta);
        }

        /// <summary>
        /// Find the pair of points on s and t that are closest to each other and return the 
        /// scanbox centered between them
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns></returns>
        public static Rect minRectBetweenStrokes(Substroke s, Substroke t)
        {
            double minDistance = double.PositiveInfinity;
            double currDistance;
            Sketch.Point minPoint1 = null;
            Sketch.Point minPoint2 = null;

            foreach (Sketch.Point a in s.Points)
            {
                foreach (Sketch.Point b in t.Points)
                {
                    currDistance = euclideanDistance(a, b);
                    if (currDistance < minDistance)
                    {
                        minDistance = currDistance;
                        minPoint1 = a;
                        minPoint2 = b;
                    }
                }
            }

            double centerX = (minPoint1.X + minPoint2.X) / 2d;
            double centerY = (minPoint1.Y + minPoint2.Y) / 2d;
            double wid = scannerWidth / 2d;
            double hei = scannerHeight / 2d;

            return new Rect(centerX - wid, centerY - hei, centerX + wid, centerY + hei);
        }

        /// <summary>
        /// calculates distance between two points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double euclideanDistance(Sketch.Point a, Sketch.Point b)
        {
            double diffX = a.X - b.X;
            double diffY = a.Y - b.Y;
            return Math.Sqrt((diffX * diffX) + (diffY * diffY));
        }

        /// <summary>
        /// Forcefully remove all label shapes and their strokes from the sketch.
        /// Also removes any strokes which are unlabeled and hence won't help us here.
        /// </summary>
        /// <param name="sketch"></param>
        public static void RemoveLabels(ref Sketch.Sketch sketch)
        {
            for (int i = 0; i < sketch.ShapesL.Count; i++)
            {
                if (sketch.ShapesL[i].LabelL == "label")
                {
                    sketch.RemoveShapeAndSubstrokes(sketch.ShapesL[i--]);
                }
            }

            List<Substroke> sses = sketch.SubstrokesL;
            for (int i = 0; i < sses.Count; i++)
            {
                if (sses[i].FirstLabel == "unlabeled")
                {
                    sketch.RemoveSubstroke(sses[i]);
                }
            }

        }
        #endregion
    }
}
