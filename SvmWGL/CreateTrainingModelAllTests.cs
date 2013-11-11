using System;
using System.Collections.Generic;
using System.IO;
using Sketch;
using System.Collections;
using System.Diagnostics;

namespace SeparateText
{
    public class CreateTrainingModel
    {
        private static double[] boundBox;
        public enum TypeRecog { WGL, WG, GL, WL, GNG, WNW, LNL }
        static TypeRecog typerecog;

        public CreateTrainingModel(List<Sketch.Sketch> sketches, string outfile)
        {
            write(sketches, outfile);
        }

        static private string substrokeToLine(Sketch.Substroke sub)
        {
            int category = -1;
            switch (typerecog)
            { 
                case TypeRecog.WGL:
                case TypeRecog.WG:
                case TypeRecog.GL:
                case TypeRecog.WL:
                    if (sub.GetFirstLabel().Equals("Wire")) category = 1;
                    if (sub.GetFirstLabel().Equals("Gate")) category = 2;
                    if (sub.GetFirstLabel().Equals("Label")) category = 3;
                    break;
                case TypeRecog.GNG:
                    if (sub.GetFirstLabel().Equals("Gate"))
                        category = 2;
                    else
                        category = 5;
                    break;

                case TypeRecog.WNW:
                    if (sub.GetFirstLabel().Equals("Wire"))
                        category = 1;
                    else
                        category = 4;
                    break;

                case TypeRecog.LNL:
                    if (sub.GetFirstLabel().Equals("Label"))
                        category = 3;
                    else
                        category = 6;
                    break;
                default:
                    Console.WriteLine("Something went wrong in the first switch.");
                    break;
                
            }


            Featurefy.FeatureStroke fragFeat = new Featurefy.FeatureStroke(sub);

            string line = category.ToString();

            line += " 1:" + arcLengthShort(fragFeat).ToString();
            line += " 2:" + arcLengthLong(fragFeat).ToString();
            line += " 3:" + distBetweenEndsLarge(fragFeat).ToString();
            line += " 4:" + distBetweenEndsSmall(fragFeat).ToString();
            line += " 5:" + turning360(fragFeat).ToString();
            line += " 6:" + turningLarge(fragFeat).ToString();
            line += " 7:" + turningSmall(fragFeat).ToString();
            line += " 8:" + turningZero(fragFeat).ToString();
            line += " 9:" + squareInkDensityHigh(fragFeat).ToString();
            line += " 10:" + squareInkDensityLow(fragFeat).ToString();
            line += " 11:" + distFromLR(fragFeat).ToString();
            line += " 12:" + distFromTB(fragFeat).ToString();

            return line;
        }

        private StreamWriter write(string line, StreamWriter sw)
        {
            sw.WriteLine(line);
            return sw;
        }

        private void write(List<Sketch.Sketch> sketches, string outfile)
        {
            StreamWriter sw = new StreamWriter(outfile);
            foreach (Sketch.Sketch sketch in sketches)
            {
                boundBox = bbox(sketch); 
                Sketch.Substroke[] subs = sketch.Substrokes;
                int i, len = subs.Length;

                for (i = 0; i < len; ++i)
                {
                    switch (typerecog) 
                    { 
                        case TypeRecog.WGL:
                        case TypeRecog.GNG:
                        case TypeRecog.WNW:
                        case TypeRecog.LNL:
                            if (subs[i].GetFirstLabel().Equals("Gate") || subs[i].GetFirstLabel().Equals("Wire") ||
                                subs[i].GetFirstLabel().Equals("Label"))
                                sw = write(substrokeToLine(subs[i]), sw);
                            break;

                        case TypeRecog.WG:
                            if (subs[i].GetFirstLabel().Equals("Gate") || subs[i].GetFirstLabel().Equals("Wire"))
                                sw = write(substrokeToLine(subs[i]), sw);
                            break;

                        case TypeRecog.GL:
                            if (subs[i].GetFirstLabel().Equals("Gate") || subs[i].GetFirstLabel().Equals("Label"))
                                sw = write(substrokeToLine(subs[i]), sw);
                            break;

                        case TypeRecog.WL:
                            if (subs[i].GetFirstLabel().Equals("Wire") || subs[i].GetFirstLabel().Equals("Label"))
                                sw = write(substrokeToLine(subs[i]), sw);
                            break;
                        default:
                            Console.WriteLine("Something went wrong in the second switch.");
                            break;
                    }
                }
            }
            sw.Close();
        }

        public static void Main(string[] args)
        {
            bool createFile = true;   // args[0] == 1 ? true : false;
            bool train      = true;   // args[1] == 1 ? true : false;
            bool inference  = true;   // args[2] == 1 ? true : false;
            string trainFile = "";
            StreamWriter sWriter = new StreamWriter("RESULTS.txt");

            Console.WriteLine("Program started: {0}", DateTime.Now);

            for (int svmType = 0; svmType < 5; ++svmType)
            {
                if (svmType == 2) continue;

                for (int kernelType = 0; kernelType < 4; ++kernelType)
                {
                    for (int typeRec = 0; typeRec < 7; ++typeRec)
                    {
                        switch (typeRec)
                        {
                            case 0:
                                typerecog = TypeRecog.WGL;
                                break;
                            case 1:
                                typerecog = TypeRecog.WG;
                                break;
                            case 2:
                                typerecog = TypeRecog.GL;
                                break;
                            case 3:
                                typerecog = TypeRecog.WL;
                                break;
                            case 4:
                                typerecog = TypeRecog.GNG;
                                break;
                            case 5:
                                typerecog = TypeRecog.WNW;
                                break;
                            case 6:
                                typerecog = TypeRecog.LNL;
                                break;
                            default:
                                Console.WriteLine("Something went wrong in the third switch.");
                                break;
                        }
                        trainFile = "sType" + svmType + "kType" + kernelType + "rType" + typeRec + ".train";
                        Console.WriteLine("Progress: {0}", trainFile);

                        if (createFile)
                        {

                            List<Sketch.Sketch> sketches = new List<Sketch.Sketch>();
                            List<string> files = new List<string>();

                            files.Add("training1.xml"); files.Add("training2.xml"); files.Add("training3.xml");
                            files.Add("training4.xml"); files.Add("training5.xml");

                            foreach (string file in files)
                                sketches.Add((new ConverterXML.ReadXML(file)).Sketch);

                            CreateTrainingModel ct = new CreateTrainingModel(sketches, trainFile);
                        }

                        if (train)
                        {
                            //Svm.TrainSVM tsvm = new Svm.TrainSVM("3types.train", "t1.model");
                            //tsvm.setParam(true);
                            //tsvm.setParam(20, 19.675, 20.377, 20, -13.904, -13.202, 5, 5);
                            //setParam(20, -6.0, 20.0, 20, 7.0, -19.0, 5, 5);
                            //tsvm.start();
                            //tsvm.save(20, 20, "out.model");
                            //tsvm.save(19.7179, -12.9524, "out.model");
                            //tsvm.save(20.1664, -13.553, "textGNL.model");
                            //tsvm.save(20.17693, -13.553, "textGNL.model");
                            //tsvm.save(2.3239, -5.59947, "textExc.model");

                            Process runLatest = new Process();
                            runLatest.StartInfo.FileName = @"C:\Documents and Settings\Student\My Documents\Trunk2\Code\Recognition\SeparateText\bin\Debug\svmtrain.exe";
                            runLatest.StartInfo.Arguments = " -s " + svmType + " -t " + kernelType + " -b 1 " + trainFile;
                            runLatest.Start();
                            runLatest.WaitForExit();

                        }

                        if (inference)
                        {
                            //Svm.ClassifySVM csvm = new Svm.ClassifySVM("textGL2.model");
                            double totalWires = 0.0; double correctWires = 0.0;
                            double totalGates = 0.0; double correctGates = 0.0;
                            double totalLabels = 0.0; double correctLabels = 0.0;
                            double totalNonwires = 0.0; double correctNonwires = 0.0;
                            double totalNongates = 0.0; double correctNongates = 0.0;
                            double totalNonlabels = 0.0; double correctNonlabels = 0.0;
                            int numUnlab = 0;

                            Recognizers.WGLRecognizer wglr = new Recognizers.WGLRecognizer(trainFile + ".model");

                            string dir_path = @"C:\Documents and Settings\Student\My Documents\Trunk2\Code\Recognition\RunCRF\INPUT-MULTIPASS-EASY-2";
                            foreach (string file in Directory.GetFiles(dir_path, "*.xml"))
                            {
                                Sketch.Sketch sketch = (new ConverterXML.ReadXML(file)).Sketch;
                                boundBox = bbox(sketch);

                                foreach (Sketch.Substroke sub in sketch.Substrokes)
                                {
                                    switch (typerecog)
                                    {
                                        case TypeRecog.WGL:
                                        case TypeRecog.GNG:
                                        case TypeRecog.WNW:
                                        case TypeRecog.LNL:
                                            if (!(sub.GetFirstLabel().Equals("Gate") || sub.GetFirstLabel().Equals("Wire") ||
                                                sub.GetFirstLabel().Equals("Label")))
                                                continue;
                                            break;
                                        case TypeRecog.WG:
                                            if (!(sub.GetFirstLabel().Equals("Gate") || sub.GetFirstLabel().Equals("Wire")))
                                                continue;
                                            break;
                                        case TypeRecog.GL:
                                            if (!(sub.GetFirstLabel().Equals("Gate") || sub.GetFirstLabel().Equals("Label")))
                                                continue;
                                            break;
                                        case TypeRecog.WL:
                                            if (!(sub.GetFirstLabel().Equals("Wire") || sub.GetFirstLabel().Equals("Label")))
                                                continue;
                                            break;
                                        default:
                                            Console.WriteLine("Something went wrong in the fourth switch.");
                                            break;
                                    }

                                    string correctLabel = sub.GetFirstLabel();
                                    string line = substrokeToLine(sub).Insert(0, "0").Remove(1, 1);
                                    Recognizers.Recognizer.Results res = wglr.Recognize(sub, line);
                                    string checkLabel = res.bestLabel();

                                    switch (typerecog)
                                    {
                                        case TypeRecog.WGL:
                                            break;
                                        case TypeRecog.GNG:
                                            if (!correctLabel.Equals("Gate"))
                                                correctLabel = "Nongate";
                                            break;
                                        case TypeRecog.WNW:
                                            if (!correctLabel.Equals("Wire"))
                                                correctLabel = "Nonwire";
                                            break;
                                        case TypeRecog.LNL:
                                            if (!correctLabel.Equals("Label"))
                                                correctLabel = "Nonlabel";
                                            break;
                                        case TypeRecog.WG:
                                            break;
                                        case TypeRecog.GL:
                                            break;
                                        case TypeRecog.WL:
                                            break;
                                        default:
                                            Console.WriteLine("Something went wrong in the fourth switch.");
                                            break;
                                    }

                                    //sketch.AddLabel(sub, checkLabel);
                                    //ConverterXML.MakeXML xmlOut = new ConverterXML.MakeXML(sketch);
                                    //xmlOut.WriteXML(file.Remove(file.Length - 4, 4) + ".LABELED.xml");

                                    if (typerecog == TypeRecog.WGL)
                                    {
                                        switch (correctLabel)
                                        {
                                            case "Wire":
                                                ++totalWires;
                                                if (checkLabel.Equals("Wire")) ++correctWires;
                                                break;
                                            case "Gate":
                                                ++totalGates;
                                                if (checkLabel.Equals("Gate")) ++correctGates;
                                                break;
                                            case "Label":
                                                ++totalLabels;
                                                if (checkLabel.Equals("Label")) ++correctLabels;
                                                break;
                                            default:
                                                Console.WriteLine("Something went wrong in the fifth switch.");
                                                break;
                                        }
                                    }


                                    if (typerecog == TypeRecog.WG)
                                    {
                                        switch (correctLabel)
                                        {
                                            case "Wire":
                                                ++totalWires;
                                                if (checkLabel.Equals("Wire")) ++correctWires;
                                                break;
                                            case "Gate":
                                                ++totalGates;
                                                if (checkLabel.Equals("Gate")) ++correctGates;
                                                break;
                                            default:
                                                Console.WriteLine("Something went wrong in the sixth switch.");
                                                break;
                                        }
                                    }
                                    if (typerecog == TypeRecog.GL)
                                    {
                                        switch (correctLabel)
                                        {
                                            case "Gate":
                                                ++totalGates;
                                                if (checkLabel.Equals("Gate")) ++correctGates;
                                                break;
                                            case "Label":
                                                ++totalLabels;
                                                if (checkLabel.Equals("Label")) ++correctLabels;
                                                break;
                                            default:
                                                Console.WriteLine("Something went wrong in the seventh switch.");
                                                break;
                                        }
                                    }

                                    if (typerecog == TypeRecog.WL)
                                    {
                                        switch (correctLabel)
                                        {
                                            case "Wire":
                                                ++totalWires;
                                                if (checkLabel.Equals("Wire")) ++correctWires;
                                                break;
                                            case "Label":
                                                ++totalLabels;
                                                if (checkLabel.Equals("Label")) ++correctLabels;
                                                break;
                                            default:
                                                Console.WriteLine("Something went wrong in the eigth switch.");
                                                break;
                                        }
                                    }

                                    if (typerecog == TypeRecog.GNG)
                                    {
                                        switch (correctLabel)
                                        {
                                            case "Gate":
                                                ++totalGates;
                                                if (checkLabel.Equals("Gate")) ++correctGates;
                                                break;
                                            case "Nongate":
                                                ++totalNongates;
                                                if (checkLabel.Equals("Nongate")) ++correctNongates;
                                                break;
                                            default:
                                                Console.WriteLine("Something went wrong in the ningth switch.");
                                                break;
                                        }
                                    }

                                    if (typerecog == TypeRecog.WNW)
                                    {
                                        switch (correctLabel)
                                        {
                                            case "Wire":
                                                ++totalWires;
                                                if (checkLabel.Equals("Wire")) ++correctWires;
                                                break;
                                            case "Nonwire":
                                                ++totalNonwires;
                                                if (checkLabel.Equals("Nonwire")) ++correctNonwires;
                                                break;
                                            default:
                                                Console.WriteLine("Something went wrong in the tenth switch");
                                                break;
                                        }
                                    }

                                    if (typerecog == TypeRecog.LNL)
                                    {
                                        switch (correctLabel)
                                        {
                                            case "Nonlabel":
                                                ++totalNonlabels;
                                                if (checkLabel.Equals("Nonlabel")) ++correctNonlabels;
                                                break;
                                            case "Label":
                                                ++totalLabels;
                                                if (checkLabel.Equals("Label")) ++correctLabels;
                                                break;
                                            default:
                                                Console.WriteLine("Something went wrong in the eleventh switch.");
                                                break;
                                        }
                                    }


                                }
                            }

                            //sWriter.WriteLine("Number of unlabeled strokes is: {0}", numUnlab);
                            sWriter.WriteLine("Case: {0}", trainFile);
                            sWriter.WriteLine("Percentage of correct Wires: {0:##.000%}",
                                correctWires / totalWires);
                            sWriter.WriteLine("Percentage of correct Gates: {0:##.000%}",
                                correctGates / totalGates);
                            sWriter.WriteLine("Percentage of correct Labels: {0:##.000%}",
                                correctLabels / totalLabels);
                            sWriter.WriteLine("Percentage of correct Nonwires: {0:##.000%}",
                                correctNonwires / totalNonwires);
                            sWriter.WriteLine("Percentage of correct Nongates: {0:##.000%}",
                                correctNongates / totalNongates);
                            sWriter.WriteLine("Percentage of correct Nonlabels: {0:##.000%}",
                                correctNonlabels / totalNonlabels);
                            sWriter.WriteLine("");
                        }

                    }
        }
        }
            sWriter.Close();
            Console.WriteLine("");
            Console.WriteLine("Program ended: {0}", DateTime.Now);
            Console.ReadLine();
        }

        #region Helper functions
        #endregion

        #region Feature functions
        /// <summary>
        /// Removes shapes with given types from Sketch.Sketch.
        /// </summary>
        /// <param name="sketchHolder">Sketch to be changed.</param>
        /// <param name="typesArray">Types which we want to remove.</param>
        static void cleanUpSketch(ref Sketch.Sketch sketchHolder, params string[] typesArray)
        {
            ArrayList typesAList = ArrayList.Adapter(typesArray);

            foreach (Sketch.Shape shape in sketchHolder.Shapes)
            {
                String type = shape.XmlAttrs.Type.ToString();

                if (typesAList.Contains(type))
                {
                    sketchHolder.RemoveShape(shape);
                }
            }
        }
        /// <summary>
        /// Feature function to get the minium distance from the left or right of the sketch
        /// </summary>
        /// <param name="callingNode"></param>
        /// <param name="input"></param>
        /// <returns>1 if close to edge, -1 if far away</returns>
        static private double distFromLR(Featurefy.FeatureStroke fragFeat)
        {
            double fromL = fragFeat.Spatial.UpperLeft.X - boundBox[0];
            double fromR = boundBox[2] - fragFeat.Spatial.LowerRight.X;
            double dist = fromL;
            if (fromR < dist)
            {
                dist = fromR;
            }
            double scale = 30;
            return tfLow(dist, (boundBox[2] - boundBox[0]) / 4, scale);
        }

        /// <summary>
        /// Feature function to get the minium distance from the left or right of the sketch
        /// </summary>
        /// <param name="callingNode"></param>
        /// <param name="input"></param>
        /// <returns>1 if close to edge, -1 if far away</returns>
        static private double distFromTB(Featurefy.FeatureStroke fragFeat)
        {
            double fromTop = fragFeat.Spatial.UpperLeft.Y - boundBox[1];
            double fromBot = boundBox[3] - fragFeat.Spatial.LowerRight.Y;
            double dist = fromTop;
            if (fromBot < dist)
            {
                dist = fromBot;
            }
            double scale = 30;
            return tfLow(dist, (boundBox[3] - boundBox[1]) / 4, scale);
        }


        /// <summary>
        /// Returns the bounding box of the whole sketch [leftx, topy, rightx, bottomy]
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns>A list [leftx, topy, rightx, bottomy] specifying the bounding box of the sketch</returns>
        public static double[] bbox(Sketch.Sketch sketch)
        {
            Sketch.Stroke[] strokes = sketch.Strokes;

            double topy = Double.PositiveInfinity;
            double leftx = Double.PositiveInfinity;
            double boty = 0;
            double rightx = 0;

            for (int i = 0; i < strokes.Length; i++)
            {
                Featurefy.FeatureStroke fragFeat = new Featurefy.FeatureStroke(strokes[i]);

                if (fragFeat.Spatial.UpperLeft.X < leftx)
                {
                    leftx = fragFeat.Spatial.UpperLeft.X;
                }
                if (fragFeat.Spatial.UpperLeft.Y < topy)
                {
                    topy = fragFeat.Spatial.UpperLeft.Y;
                }
                if (fragFeat.Spatial.LowerRight.X > rightx)
                {
                    rightx = fragFeat.Spatial.LowerRight.X;
                }
                if (fragFeat.Spatial.UpperLeft.Y > boty)
                {
                    boty = fragFeat.Spatial.UpperLeft.Y;
                }
            }
            return new double[4] { leftx, topy, rightx, boty };
        }

        static public double squareInkDensityHigh(Featurefy.FeatureStroke fragFeat)
        {
            double density = fragFeat.ArcLength.InkDensity;
            double scale = 100;
            return tfHigh(density, 24, scale);
            //return CreateGraph.tfHigh(density, parameter, scale);
        }


        static public double squareInkDensityLow(Featurefy.FeatureStroke fragFeat)
        {
            double density = fragFeat.ArcLength.InkDensity;
            double scale = 90;
            return tfLow(density, 5, scale);
        }


        /// <summary>
        /// Determines whether this Node falls into the category of short
        /// </summary>
        /// <param name="callingNode">Node that the feature function is being evaluated around</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1 if this Node is short, -1 otherwise</returns>
        static public double arcLengthShort(Featurefy.FeatureStroke fragFeat)
        {
            //ARBITRARY VALUE!!!!
            // Data analysis should be performed to determine what a good threshold is for this
            // NOTE: Aaron changed this value from 1000 to 300
            double scale = 30;
            return tfLow(fragFeat.ArcLength.TotalLength, 300, scale);
        }

        /// <summary>
        /// Determines whether this Node falls into the category of long
        /// </summary>
        /// <param name="callingNode">Node that the feature function is being evaluated around</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1 if this Node is long, -1 otherwise</returns>
        static public double arcLengthLong(Featurefy.FeatureStroke fragFeat)
        {
            //ARBITRARY VALUE!!!!
            // Data analysis should be performed to determine what a good threshold is for this
            // NOTE: Aaron changed this value from 2000 to 2000
            double scale = 30;
            return tfHigh(fragFeat.ArcLength.TotalLength, 2000, scale);
        }

        /// <summary>
        /// Determines if the two ends of this stroke are far apart
        /// </summary>
        /// <param name="callingNode">Node to evaluate on</param>
        /// <param name="input">The set of all stroke data in the graph</param>
        /// <returns>1 if far apart, -1 otherwise</returns>
        static public double distBetweenEndsLarge(Featurefy.FeatureStroke fragFeat)
        {
            Point p1 = fragFeat.Spatial.FirstPoint;
            Point p2 = fragFeat.Spatial.LastPoint;
            
            double u = p1.X;
            double v = p1.Y;
            double p = p2.X;
            double q = p2.Y;

            double dist = Math.Sqrt((u - p) * (u - p) + (v - q) * (v - q));

            // transfer at dist > 70% of arclength
            if (fragFeat.ArcLength.TotalLength == 0.0)
            {
                //This stroke has a length of zero, so this feature is meaningless
                return 0.0;
            }
            double scale = 30;
            return tfHigh(dist, (fragFeat.ArcLength.TotalLength * 0.7), scale);
        }

        /// <summary>
        /// Determines if the two endso of this stroke are close together
        /// </summary>
        /// <param name="callingNode">Node to evaluate on</param>
        /// <param name="input">The set of all stroke data in the graph</param>
        /// <returns></returns>
        static public double distBetweenEndsSmall(Featurefy.FeatureStroke fragFeat)
        {
            Point p1 = fragFeat.Spatial.FirstPoint;
            Point p2 = fragFeat.Spatial.LastPoint;

            double u = p1.X;
            double v = p1.Y;
            double p = p2.X;
            double q = p2.Y;

            double dist = Math.Sqrt((u - p) * (u - p) + (v - q) * (v - q));

            // transfer at dist < 20% of arclength
            if (fragFeat.ArcLength.TotalLength == 0.0)
            {
                //This stroke has a length of zero, so this feature is meaningless
                return 0.0;
            }

            double scale = 30;
            return tfLow(dist, (fragFeat.ArcLength.TotalLength * 0.2), scale);
        }

        /// <summary>
        /// This function find the total angle (in radians) that a stroke turns over its length.
        /// </summary>
        /// <param name="callingNode">Node that the feature function is being evaluated on.</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>Angle turned by stroke in radians.</returns>
        static private double turning(Featurefy.FeatureStroke fragFeat)
        {
            // End point window so we don't count initial hooks
            const int WINDOW = 5;

            double sumDeltaAngle = 0.0;

            // Make sure we have something relevant to compute
            if (fragFeat.Points.Length < (WINDOW * 2) + 1)
                return sumDeltaAngle;

            // Make room for the data, and initialize to our first case
            double prevX = (fragFeat.Points[WINDOW]).X;
            double prevY = (fragFeat.Points[WINDOW]).Y;
            double X = (fragFeat.Points[WINDOW + 1]).X;
            double Y = (fragFeat.Points[WINDOW + 1]).Y;

            // The change in X and Y
            double delX = X - prevX;
            double delY = Y - prevY;

            // ah-Ha, the angle
            double prevDirection = Math.Atan2(delY, delX);

            // Make some space we will need
            double newDirection;
            double deltaAngle;

            int length = fragFeat.Points.Length - WINDOW;
            for (int i = WINDOW + 2; i < length; i++)
            {
                // Update the previous values
                prevX = X;
                prevY = Y;

                // Grab the new values
                X = (fragFeat.Points[i]).X;
                Y = (fragFeat.Points[i]).Y;

                // Find the new deltas
                delX = X - prevX;
                delY = Y - prevY;

                // Find the new direction
                newDirection = Math.Atan2(delY, delX);

                // Find the change from the previous dirction
                deltaAngle = newDirection - prevDirection;

                // Not so fast, we're not done yet
                // deltaAngle has to be in the range +pi to -pi
                deltaAngle = (deltaAngle % (2 * Math.PI));
                if (deltaAngle > Math.PI)
                {
                    deltaAngle -= (2 * Math.PI);
                }

                // And finally add it to the sum
                sumDeltaAngle += deltaAngle;

                // Some bookkeeping
                prevDirection = newDirection;
            }

            return Math.Abs(sumDeltaAngle);
        }

        /// <summary>
        /// Determines if the stroke underwent no net angle change
        /// </summary>
        /// <param name="callingNode">Node that the feature function is being evaluated on</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1 if turned ~0 deg, -1 otherwise</returns>
        static public double turningZero(Featurefy.FeatureStroke fragFeat)
        {
            //angle turned is near zero?
            double deltaAngle = turning(fragFeat);

            //ARBITRARY VALUE!!!
            // NOTE: Aaron changed value from 17.5 (0.305) to 20
            double scale = 30;
            return tfLow(deltaAngle, 0.349, scale); //approx 17.5 degrees
        }

        /// <summary>
        /// Determines if the stroke underwent a small angle change
        /// </summary>
        /// <param name="callingNode">Node that the feature function is being evaluated on</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1 if turned small angle, -1 otherwise</returns>
        static public double turningSmall(Featurefy.FeatureStroke fragFeat)
        {
            //angle turned is small
            double deltaAngle = turning(fragFeat);

            //ARBITRARY VALUE!!!
            // band is 17.5 (0.305) to 217.5 degrees (3.80)
            // NOTE: Aaron changed this value to 20 - 180
            double scale = 30;
            double upperLimit = tfLow(deltaAngle, 3.14, scale); // <217.5 degrees
            double lowerLimit = tfHigh(deltaAngle, 0.349, scale); // >17.5 degrees
            return upperLimit * lowerLimit; //multiply them to create a band of approx 1
        }

        /// <summary>
        /// Determines if the stroke underwent approx 1 full rotation (217.5 to 450 degrees)
        /// </summary>
        /// <param name="callingNode">Node that the feature function is being evaluated on</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1 if turned one rotation, -1 otherwise</returns>
        static public double turning360(Featurefy.FeatureStroke fragFeat)
        {
            //angle turned is near one revolution
            double deltaAngle = turning(fragFeat);

            //ARBITRARY VALUE!!!
            // band is 217.5 (3.80) to 450 (7.85) degrees
            // NOTE: Aaron changed this value to 290 - 430
            double scale = 30;
            double upperLimit = tfLow(deltaAngle, 7.50, scale); // <450 degrees
            double lowerLimit = tfHigh(deltaAngle, 5.06, scale); // >217.5 degrees
            return upperLimit * lowerLimit; //multiply them to create a band of approx 1
        }

        /// <summary>
        /// Determines if the stroke underwent a large amount of turning (>450 degrees)
        /// </summary>
        /// <param name="callingNode">Node that the feature function is being evaluated on</param>
        /// <param name="input">The set of all stroke data for the graph</param>
        /// <returns>1 if turned large amount, -1 otherwise</returns>
        static public double turningLarge(Featurefy.FeatureStroke fragFeat)
        {
            //angle turned is large
            double deltaAngle = turning(fragFeat);

            //ARBITRARY VALUE!!!
            // NOTE: Aaron changed this value to > 430
            double scale = 30;
            return tfHigh(deltaAngle, 7.50, scale); //approx 450 degrees
        }

        /// <summary>
        /// Creates a smooth transfer of the output from 1 to -1 as the input crosses the threshold
        /// </summary>
        /// <param name="input">The value to create the transfer on</param>
        /// <param name="threshold">The point around which a transfer is made</param>
        /// <param name="scale">The value by which we scale the function. The higher the value, 
        ///                     the higher the slope</param>>
        /// <returns>asymptotic from 1 to -1</returns>
        public static double tfLow(double input, double threshold, double scale)
        {
            //arctan provides the smooth transfer function I desire
            //arctan goes from -1.2 to +1.2 as the input goes from -3 to +3
            //the input will be scaled to map a change of 10% threshold to 0-3
            //Note: the default value of slope is 30.0
            return (-1.0 * Math.Atan((input - threshold) / threshold * scale)) / (Math.PI / 2);
        }

        /// <summary>
        /// Creates a smooth transfer of the output from -1 to 1 as the input crosses the threshold
        /// </summary>
        /// <param name="input">The value to create the transfer on</param>
        /// <param name="threshold">The point around which a transfer is made</param>
        /// <param name="scale">The value by which we scale the function. The higher the value, 
        ///                     the higher the slope</param>>
        /// <returns>asymptotic from -1 to 1</returns>
        public static double tfHigh(double input, double threshold, double scale)
        {
            //arctan provides the smooth transfer function I desire
            //arctan goes from -1.2 to +1.2 as the input goes from -3 to +3
            //the input will be scaled to map a change of 10% threshold to 0-3
            //Note: the default value of scale is 30.0
            return (1.0 * Math.Atan((input - threshold) / threshold * scale)) / (Math.PI / 2);
        }
        #endregion
        
    }
}
