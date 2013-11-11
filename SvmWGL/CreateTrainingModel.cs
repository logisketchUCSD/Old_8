/* 
 * File: CreateTrainingModel.cs
 *
 * Author: Anton Bakalov (Sketchers 2007)
 * Harvey Mudd College, Claremont, CA 91711.
 * 
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using Sketch;
using System.Collections;
using System.Diagnostics;
using Recognizers;


namespace SeparateText
{
    public class CreateTrainingModel
    {
        #region Global Variables
        private static double[] boundBox;
        private static int numLabels;
        private enum TypeRecog { WGL, WG, GL, WL, GNG, WNW, LNL }
        private enum TypeWork { SubToLine, WriteToSw, SkipSub, CorLabel, DoStats }
        private static TypeRecog typerecog;
        #endregion

        //-a -t "C:\Documents and Settings\Student\My Documents\Trunk2\Code\Recognition\SvmWGL\Train" -i "C:\Documents and Settings\Student\My Documents\Trunk2\Code\Recognition\RunCRF\INPUT-MULTIPASS-EASY-2" -s "Results.txt"
        //-f trainfile.train -t "C:\Documents and Settings\Student\My Documents\Trunk2\Code\Recognition\SvmWGL\Train" -i "C:\Documents and Settings\Student\My Documents\Trunk2\Code\Recognition\RunCRF\INPUT-MULTIPASS-EASY-2" -s FinalResults.txt

        public static void Main(string[] args)
        {
            #region Local Variables
            bool createFile = false;      
            bool train = false;        
            bool inference = false;      
            bool recursion = false;       
            string trainFile = "";        
            bool regularUse = true;       
            bool autoPilot = false;       
            bool groupCorrection = false; 
            bool labelSketches = false;   
            bool showStats = false;       
            StreamWriter sWriter = null;  
            List<string> dataFilesTrain = new List<string>();
            List<string> dataFilesLabel = new List<string>();
            List<string> dataCluster = new List<string>();
            List<string> dataCRF = new List<string>();

            #endregion

            #region Command Line Parsing
            for (int i = 0; i < args.Length; i++)
			{
				switch(args[i].ToLower())
				{
                    // See printHelp() to see what each flag is used for.
					case "-h":
					case "-help":
					case "--help":
						printHelp();
                        return;
                    case "-f":
                        createFile = true;
                        ++i;
                        #region error check
                        if (i >= args.Length)
                        {
                            Console.WriteLine("ERROR: -f must be followed by a name for the train file.");
                            return;
                        }
                        #endregion
                        trainFile = args[i];
                        break;
                    case "-t":
                        train = true;
                        ++i;
                        #region error check
                        if (i >= args.Length)
                        {
                            Console.WriteLine("ERROR: -t must be followed by a directory containing the training data.");
                            return;
                        }
                        #endregion
                        parseDir(args[i], ref dataFilesTrain, recursion);
                        break;
                    case "-i":
                        inference = true;
                        ++i;
                        #region error check
                        if (i >= args.Length)
                        {
                            Console.WriteLine("ERROR: -i must be followed by a directory containing data for labeling.");
                            return;
                        }
                        #endregion
                        parseDir(args[i], ref dataFilesLabel, recursion);
                        break;
                    case "-l":
                        labelSketches = true;
                        break;
                    case "-s":
                        showStats = true;
                        ++i;
                        #region error check
                        if (i >= args.Length)
                        {
                            Console.WriteLine("ERROR: -s must be followed by a filename to write stats to.");
                            return;
                        }
                        #endregion
                        sWriter = new StreamWriter(args[i]);
                        break;
                    case "-a":
                        autoPilot = true;
                        createFile = true;
                        regularUse = false;
                        break;
                    case "-g":
                        groupCorrection = true;
                        regularUse = false;
                        ++i;
                        #region error check
                        if (i >= args.Length)
                        {
                            Console.WriteLine("ERROR: -g must be followed by a dir containing data from the UCR's clustering"); 
                            Console.WriteLine("algoritm and a dir containg files from the CRF.");
                            return;
                        }
                        #endregion
                        parseDir(args[i], ref dataCluster, recursion);
                        ++i;
                        #region error check
                        if (i >= args.Length)
                        {
                            Console.WriteLine("ERROR: -g must be followed by a dir containing data from the UCR's clustering");
                            Console.WriteLine("algoritm and a dir containg files from the CRF.");
                            return;
                        }
                        #endregion
                        parseDir(args[i], ref dataCRF, recursion);
                        break;
					default:
						printHelp();
                        return;
				}
			}
			#endregion 

            #region Error Check
            if (labelSketches && showStats)
            {
                Console.WriteLine("You cannot label sketches and have stats info at the same time.");
                Console.WriteLine("Specify one at a time");
                return;
            }
            #endregion

            Console.WriteLine("Program started: {0}", DateTime.Now);
            Console.WriteLine();

            if (regularUse)
            {
                #region regularUse
                //typerecog = TypeRecog.WL; // Change this for classification other than wire-label.
                typerecog = TypeRecog.WG; // Hopefully 'wire-gate-label'
                numLabels = 2;  // Make sure to change this too!

                if (createFile)
                {
                    createTrainingFile(trainFile, dataFilesTrain);
                }

                if (train)
                {
                    // Double check these variables and comment.
                    //     0 refers to the svm type C-SVC
                    //         |
                    doTraining(0, 1, trainFile);
                    //            |
                    //     0 refers to the linear kernel type.
                }

                if (inference)
                {
                    doInference(trainFile, dataFilesLabel, ref sWriter, labelSketches, showStats);
                }
                #endregion
            }
            else if (autoPilot)
            {
                #region autoPilot
                // svmType - 0: C-SVC, 1: nu-SVC, 2: Once-class SVM, 3: epsilon-SVR, 4: nu-SVR 
                // It works best for C-SVC and nu-SVC (see www.csie.ntu.edu.tw/~cjlin/libsvm)
                for (int svmType = 0; svmType < 2; ++svmType) // restrict the range
                {
                    //if (svmType == 2) continue; // This type does not provide probabilities.

                    //kernelType - 0: linear, 1: polynomial, 2: radial basis function, 3: sigmoid
                    for (int kernelType = 0; kernelType < 4; ++kernelType)
                    {
                        // See the region below for an explanation of typeRec
                        for (int typeRec = 0; typeRec < 7; ++typeRec)
                        {
                            #region Set the recognition type.
                            switch (typeRec)
                            {
                                case 0:
                                    typerecog = TypeRecog.WGL;
                                    numLabels = 3;
                                    break;
                                case 1:
                                    typerecog = TypeRecog.WG;
                                    numLabels = 2;
                                    break;
                                case 2:
                                    typerecog = TypeRecog.GL;
                                    numLabels = 2;
                                    break;
                                case 3:
                                    typerecog = TypeRecog.WL;
                                    numLabels = 2;
                                    break;
                                case 4:
                                    typerecog = TypeRecog.GNG;
                                    numLabels = 2;
                                    break;
                                case 5:
                                    typerecog = TypeRecog.WNW;
                                    numLabels = 2;
                                    break;
                                case 6:
                                    typerecog = TypeRecog.LNL;
                                    numLabels = 2;
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong in the third switch.");
                                    break;
                            
                        }

                        #endregion

                            trainFile = "sType" + svmType + "kType" + kernelType + "rType" + typeRec + ".train";
                            Console.WriteLine("Progress: {0}", trainFile);

                            if (createFile)
                            {
                                createTrainingFile(trainFile, dataFilesTrain);
                            }

                            if (train)
                            {
                                doTraining(svmType, kernelType, trainFile);
                            }

                            if (inference)
                            {
                                doInference(trainFile, dataFilesLabel, ref sWriter, labelSketches, showStats);
                            }
                        }

                    }
                }
                #endregion
            }
            else if (groupCorrection)
            {
                #region groupCorrection
                doGroupCorrection(dataCluster, ref dataCRF);
                #endregion
            }

            if (showStats) sWriter.Close();
            Console.WriteLine("\nProgram ended: {0}", DateTime.Now);
            //Console.ReadLine();
        }

        #region Group Correction
        /// <summary>
        /// Makes corrections to the files created by CRF.
        /// </summary>
        /// <param name="dataCluster">Files created with UCR's clustering algorithm.</param>
        /// <param name="dataCRF">Files created with the CRF.</param>
        private static void doGroupCorrection(List<string> dataCluster, ref List<string> dataCRF)
        {
            string errorMessage = "Improper usage of doGroupCorrection(). The size of the List<>-s " +
                                  "which the function takes should be equal.";
            Debug.Assert(dataCluster.Count == dataCRF.Count, errorMessage);

            int size = dataCRF.Count;
            for (int j = 0; j < size; ++j)
            {
                Sketch.Sketch sketch = (new ConverterXML.ReadXML(dataCRF[j])).Sketch;
                StreamReader sReader = new StreamReader(dataCluster[j]);
                Dictionary<int, List<Sketch.Substroke>> groupToSubstrokes = new Dictionary<int, List<Substroke>>();

                int count = sketch.Substrokes.Length;
                for (int i = 0; i < count; ++i)
                {
                    int group = getNextGroupNumber(ref sReader);

                    if (!groupToSubstrokes.ContainsKey(group))
                        groupToSubstrokes.Add(group, new List<Substroke>());

                    groupToSubstrokes[group].Add(sketch.Substrokes[i]);
                }

                doCorrectionsToSketch(ref sketch, groupToSubstrokes);
            }
        }

        /// <summary>
        /// Does corrections to a sketch. More specifically it goes through all the shapes in the sketch
        /// and if the number of strokes in 
        /// </summary>
        /// <param name="sketch">Sketch to be changed.</param>
        /// <param name="groupToSubstrokes">Dictionary telling us which strokes are in which group.</param>
        private static void doCorrectionsToSketch(ref Sketch.Sketch sketch,
            Dictionary<int, List<Sketch.Substroke>> groupToSubstrokes)
        {

            // NOTE that this function is not tested since we did not get the data from
            // UC Riverside on time.

            int len = groupToSubstrokes.Count;
            for (int index = 0; index < len; ++index )
            {
                int circuitElements = 0; // i.e. wires and gates
                int labels = 0;

                foreach (Sketch.Substroke subStr in groupToSubstrokes[index])
                {
                    if (subStr.GetFirstLabel().Equals("Label"))
                        ++labels;
                    else
                        ++circuitElements;
                }

                if (circuitElements >= labels)
                    foreach(Sketch.Substroke subStr in groupToSubstrokes[index])
                        if (subStr.GetFirstLabel().Equals("Label"))
                        {
                            foreach(Sketch.Shape s in subStr.ParentShapes)
                                sketch.RemoveShape(s);
                            sketch.AddLabel(subStr, "IncorrectlyLabeledAsText", -1);
                        }
                if (circuitElements < labels)
                    foreach(Sketch.Substroke subStr in groupToSubstrokes[index])
                        if (!subStr.GetFirstLabel().Equals("Label"))
                        {
                            foreach(Sketch.Shape s in subStr.ParentShapes)
                                sketch.RemoveShape(s);
                            sketch.AddLabel(subStr, "CorrectedToLabel", -1);
                        }
            }
        }
        /// <summary>
        /// Gets the next group number from a StreamReader.
        /// </summary>
        /// <param name="sReader"></param>
        /// <returns></returns>
        private static int getNextGroupNumber(ref StreamReader sReader)
        {
            string[] lineSegments = null;

            while (true)
            {
                lineSegments = sReader.ReadLine().Split('\t');
                if (lineSegments.Length == 2)
                    break;
            }

            return Convert.ToInt32(lineSegments[1]);
        }        
        #endregion

        #region Create File, Training, and Inference
        /// <summary>
        /// This function generates a training file which contains all the outputs from the
        /// feature functions.
        /// </summary>
        /// <param name="trainFile">Name of the train file specified by the user.</param>
        /// <param name="dataFilesTrain">Contains all the files.</param>
        private static void createTrainingFile(string trainFile, List<string> dataFilesTrain)
        {
            List<Sketch.Sketch> sketches = new List<Sketch.Sketch>();

            foreach (string file in dataFilesTrain)
                sketches.Add((new ConverterXML.ReadXML(file)).Sketch);

            CreateTrainingModel ct = new CreateTrainingModel(sketches, trainFile);
        }

        /// <summary>
        /// Does training.
        /// </summary>
        /// <param name="svmType">Type of svm to use.</param>
        /// <param name="kernelType">Type of kernel to use.</param>
        /// <param name="trainFile">Name of the trainfile.</param>
        private static void doTraining(int svmType, int kernelType, string trainFile)
        {
            // Currently we are using a compiled version of the svmlib
            // Once we port it from C++ or call the functions we need from a dll we should
            // modify the code below accordingly.
            bool trainWithExe = true;
            if (!trainWithExe)
            {
                Svm.TrainSVM tsvm = new Svm.TrainSVM("file.train", "file.model");
                tsvm.setParam(true);
                tsvm.start();
            }
            else
            {
                Process runLatest = new Process();
                runLatest.StartInfo.FileName = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + @"\svmtrain.exe";
                runLatest.StartInfo.Arguments = " -s " + svmType + " -t " + kernelType + " -b 1 " + trainFile;
                runLatest.Start();
                runLatest.WaitForExit();
            }
        }

        /// <summary>
        /// Does inference.
        /// </summary>
        /// <param name="trainFile">Name of trainfile.</param>
        /// <param name="dataFilesLabel">Contains all the files to label.</param>
        /// <param name="sWriter">StreamWriter to write info to.</param>
        /// <param name="labelSketches">Are we labeling?</param>
        /// <param name="showStats">Are we printing stats?</param>
        private static void doInference(string trainFile, List<string> dataFilesLabel, ref StreamWriter sWriter, 
            bool labelSketches, bool showStats)
        {
            if ((sWriter == null && showStats))
            {
               Console.WriteLine("Cannot write to file if sWriter is null.");
               return;
            }

            Recognizers.WGLRecognizer wglr = new Recognizers.WGLRecognizer(trainFile + ".model");
            const int NUM_STATS = 12;
            double[] results = new double[NUM_STATS];
            #region Info about resuts
            /*
                results[0]  -> totalWires,      results[1]  -> correctWires 
                results[2]  -> totalGates,      results[3]  -> correctGates 
                results[4]  -> totalLabels,     results[5]  -> correctLabels 
                results[6]  -> totalNonwires,   results[7]  -> correctNonwires 
                results[8]  -> totalNongates,   results[9]  -> correctNongates 
                results[10] -> totalNonlabels,  results[11] -> correctNonlabels 
            */
            #endregion

            
            foreach (string file in dataFilesLabel)
            {
                Sketch.Sketch sketch = (new ConverterXML.ReadXML(file)).Sketch;
                if (labelSketches) cleanUpSketch(ref sketch, "Wire", "Gate", "Label");
                boundBox = FeatureFunctions.bbox(sketch);

                foreach (Sketch.Substroke sub in sketch.Substrokes)
                {
                    string correctLabel = "";
                    if (!labelSketches)
                    {
                        bool skip = false;
                        correctLabel = sub.GetFirstLabel();
                        skip = (bool)doStuff(TypeWork.SkipSub, typerecog, sub, correctLabel, null, null);
                        if (skip) continue;
                    }
                    Recognizers.Recognizer.Results res = wglr.Recognize(sub, boundBox, numLabels);
                    string checkLabel = res.BestLabel;
                    double prob = res.BestMeasure;
                    
                    if (!labelSketches)
                        correctLabel = (string)doStuff(TypeWork.CorLabel, typerecog, sub, correctLabel, null, null);

                    if (labelSketches)
                    {
                        sketch.AddLabel(sub, checkLabel);
                        ConverterXML.MakeXML xmlOut = new ConverterXML.MakeXML(sketch);
                        xmlOut.WriteXML(file.Remove(file.Length - 4, 4) + ".LABELED.xml");
                    }
                    if (showStats)
                        results = (double[])doStuff(TypeWork.DoStats, typerecog, null, checkLabel, correctLabel, results);
                }
            }

            if (showStats)
            {
                sWriter.WriteLine("Case: {0}", trainFile);
                sWriter.WriteLine("Percentage of correct Wires: {0:##.000%}",
                    results[1] / results[0]);
                sWriter.WriteLine("Percentage of correct Gates: {0:##.000%}",
                    results[3] / results[2]);
                sWriter.WriteLine("Percentage of correct Labels: {0:##.000%}",
                    results[5] / results[4]);
                sWriter.WriteLine("Percentage of correct Nonwires: {0:##.000%}",
                    results[7] / results[6]);
                sWriter.WriteLine("Percentage of correct Nongates: {0:##.000%}",
                    results[9] / results[8]);
                sWriter.WriteLine("Percentage of correct Nonlabels: {0:##.000%}",
                    results[11] / results[10]);
                sWriter.WriteLine();
            }
        }
        #endregion

        #region Command Line Processing
        /// <summary>
        /// Prints help info.
        /// </summary>
        public static void printHelp()
        {
            Console.WriteLine(@"
*****************
* MAIN OPTIONS: *
*****************

Note: All commands should be one line.

(1) For each possible svm type, kernel type (supported by libsvm version 2.84) 
and classification (e.g. wire vs. gate), creates a training file, does 
training, and does inference:
SeparateText.exe -a -t dirWithTrainFiles -i dirContainingFilesToLabel 
                [-l] -s FinalResults.txt

(2) Creates a training file, do training, and do inference:
SeparateText.exe -f trainFile.train -t dirContainingTrainFiles 
                 -i dirContainingFilesToLabel [-l] 
                 -s FinalResults.txt

(3) Takes a directory containing files created by RunCRF (inference mode) and 
make corrections using the clustering algorithm from UC Riverside
SeparateText.exe -g dirContainingFiles

*********************************
* Explanations of flags/options *
*********************************

-h or -help or --help prints the help info.

-f specifies the name of the train file created by the program.

-t specifies that we are in training mode and is followed by a directory 
   containing training files.

-i specifies that we are in inference mode and is followed by a directory 
   containing files to be labeled.

-l specifies that we are in labeling mode.

-s writes statistical data to the filename following the flag.

-a specifies that we are in autopilot mode i.e. we are testing all possible 
   combinations of svm types and kernel types as written above.

-g makes corrections using the clustering algorithm from UC Riverside 
");
        }
        /// <summary>
        /// Searches the specified directory and saves filenames.
        /// </summary>
        /// <param name="dirCheck">Directory to be searched.</param>
        /// <param name="dataFiles">Container for the filenames.</param>
        /// <param name="recursion">If true, recursive search is performed.</param>
        static void parseDir(string dirCheck, ref List<string> dataFiles, bool recursion)
        {
            if (Directory.Exists(dirCheck))
            {
                string[] files = Directory.GetFiles(dirCheck, "*.xml");
                // Very very ugly hack to get the .out files 
                if (files.Length == 0) 
                    files = Directory.GetFiles(dirCheck, "*.out");
                dataFiles = new List<string>(files);

                if (recursion)
                    recParseDir(dirCheck, ref dataFiles);
            }
            else
            {
                Console.WriteLine("Directory does not exist.");
                printHelp();
                return;
            }
        }

        /// <summary>
        /// Searches the directory recursively and saves files.
        /// </summary>
        /// <param name="dirCheck">Parent directory.</param>
        /// <param name="dataFiles">Container storing the filenames.</param>
        static void recParseDir(string dirCheck, ref List<string> dataFiles)
        {
            foreach (string dir in Directory.GetDirectories(dirCheck))
            {
                foreach (string file in Directory.GetFiles(dir, "*.xml"))
                {
                    dataFiles.Add(file);
                }

                recParseDir(dir, ref dataFiles);
            }
        }
        #endregion

        #region Helper Functions
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sketches">Sketches to use.</param>
        /// <param name="outfile">Name of the trainFile.</param>
        public CreateTrainingModel(List<Sketch.Sketch> sketches, string outfile)
        {
            write(sketches, outfile);
        }

        /// <summary>
        /// Takes in a stroke and outputs the results of the feature funtions for
        /// this stroke.
        /// </summary>
        /// <param name="sub"></param>
        /// <returns></returns>
        static private string substrokeToLine(Sketch.Substroke sub)
        {
            int category = -1;
            string label = sub.GetFirstLabel();
            category = (int)doStuff(TypeWork.SubToLine, typerecog, sub, label, null, null);
            

            Featurefy.FeatureStroke fragFeat = new Featurefy.FeatureStroke(sub);
            string line = category.ToString();

            line += " 1:" + FeatureFunctions.arcLengthLong(fragFeat).ToString();
            line += " 2:" + FeatureFunctions.arcLengthShort(fragFeat).ToString();
            line += " 3:" + FeatureFunctions.distBetweenEndsLarge(fragFeat).ToString();
            line += " 4:" + FeatureFunctions.distBetweenEndsSmall(fragFeat).ToString();
            line += " 5:" + FeatureFunctions.turning360(fragFeat).ToString();
            line += " 6:" + FeatureFunctions.turningLarge(fragFeat).ToString();
            line += " 7:" + FeatureFunctions.turningSmall(fragFeat).ToString();
            line += " 8:" + FeatureFunctions.turningZero(fragFeat).ToString();
            line += " 9:" + FeatureFunctions.squareInkDensityHigh(fragFeat).ToString();
            line += " 10:" + FeatureFunctions.squareInkDensityLow(fragFeat).ToString();
            line += " 11:" + FeatureFunctions.distFromLR(fragFeat, boundBox).ToString();
            line += " 12:" + FeatureFunctions.distFromTB(fragFeat, boundBox).ToString();
            return line;
        }
        /// <summary>
        /// Writes a line to StreamWriter
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sw"></param>
        /// <returns></returns>
        private static StreamWriter write(string line, StreamWriter sw)
        {
            sw.WriteLine(line);
            return sw;
        }

        /// <summary>
        /// Creates the training file which contains the outputs of all the 
        /// substrokes of all the sketches.
        /// </summary>
        /// <param name="sketches"></param>
        /// <param name="outfile">Training file</param>
        private void write(List<Sketch.Sketch> sketches, string outfile)
        {
            StreamWriter sw = new StreamWriter(outfile);
            foreach (Sketch.Sketch sketch in sketches)
            {
                boundBox = FeatureFunctions.bbox(sketch); 
                Sketch.Substroke[] subs = sketch.Substrokes;
                int i, len = subs.Length;

                for (i = 0; i < len; ++i)
                {
                    string label = subs[i].GetFirstLabel();
                    sw = (StreamWriter)doStuff(TypeWork.WriteToSw, typerecog, subs[i], label, sw, null);
                }
            }
            sw.Close();
        }

        /// <summary>
        /// Does specific work based on TypeWork and TypeRecog.
        /// </summary>
        /// <param name="typeWork"></param>
        /// <param name="typeRecog"></param>
        /// <param name="sub"></param>
        /// <param name="label"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        /// <returns></returns>
        private static Object doStuff(TypeWork typeWork, TypeRecog typeRecog,
            Sketch.Substroke sub, string label, Object obj1, Object obj2)
        {

            switch (typerecog)
            {
                case TypeRecog.WGL:
                    switch (typeWork)
                    {
                        #region Type.SubToLine
                        case TypeWork.SubToLine:
                            if (label.Equals("Wire")) return 1;
                            if (label.Equals("Gate")) return 2;
                            if (label.Equals("Label")) return 3;
                            return -1;
                        #endregion

                        #region TypeWork.WireToSw
                        case TypeWork.WriteToSw:
                            if (label.Equals("Gate") || label.Equals("Wire") ||
                                label.Equals("Label"))
                                return write(substrokeToLine(sub), (StreamWriter)obj1);
                            return (StreamWriter)obj1;
                        #endregion

                        #region TypeWork.SkipSub
                        case TypeWork.SkipSub:
                            if (!(label.Equals("Gate") || label.Equals("Wire") ||
                                  label.Equals("Label")))
                                return true;
                            return false;
                        #endregion

                        #region TypeWork.CorLabel
                        case TypeWork.CorLabel:
                            return label;
                        #endregion

                        #region TypeWork.Dostats
                        case TypeWork.DoStats:
                            string correctLabel = (string)obj1;
                            double[] results = (double[])obj2;
                            switch (correctLabel)
                            {
                                case "Wire":
                                    ++results[0];
                                    if (label.Equals("Wire")) ++results[1];
                                    break;
                                case "Gate":
                                    ++results[2];
                                    if (label.Equals("Gate")) ++results[3];
                                    break;
                                case "Label":
                                    ++results[4];
                                    if (label.Equals("Label")) ++results[5];
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong with TypeRecog.WGL -> TypeWork.DoStats.");
                                    break;
                            }
                            return results;
                        #endregion

                        #region Default
                        default:
                            Console.WriteLine("Something went wrong in doStuff().");
                            return null;
                        #endregion
                    }

                case TypeRecog.WG:
                    switch (typeWork)
                    {
                        #region Type.SubToLine
                        case TypeWork.SubToLine:
                            if (label.Equals("Wire")) return 1;
                            if (label.Equals("Gate")) return 2;
                            if (label.Equals("Label")) return 3;
                            return -1;
                        #endregion

                        #region TypeWork.WriteToSw
                        case TypeWork.WriteToSw:
                            if (label.Equals("Gate") || label.Equals("Wire"))
                                return write(substrokeToLine(sub), (StreamWriter)obj1);
                            return (StreamWriter)obj1;
                        #endregion

                        #region TypeWork.SkipSub
                        case TypeWork.SkipSub:
                            if (!(label.Equals("Gate") || label.Equals("Wire")))
                                return true;
                            return false;
                        #endregion

                        #region TypeWork.CorLabel
                        case TypeWork.CorLabel:
                            return label;
                        #endregion

                        #region TypeWork.DoStats
                        case TypeWork.DoStats:
                            string correctLabel = (string)obj1;
                            double[] results = (double[])obj2;
                            switch (correctLabel)
                            {
                                case "Wire":
                                    ++results[0];
                                    if (label.Equals("Wire")) ++results[1];
                                    break;
                                case "Gate":
                                    ++results[2];
                                    if (label.Equals("Gate")) ++results[3];
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong with TypeRecog.WG -> TypeWork.DoStats.");
                                    break;
                            }
                            return results;
                        #endregion

                        #region Default
                        default:
                            Console.WriteLine("Something went wrong in doStuff().");
                            return null;
                        #endregion
                    }
                case TypeRecog.GL:
                    switch (typeWork)
                    {
                        #region Type.SubToLine
                        case TypeWork.SubToLine:
                            if (label.Equals("Wire")) return 1;
                            if (label.Equals("Gate")) return 2;
                            if (label.Equals("Label")) return 3;
                            return -1;
                        #endregion

                        #region TypeWork.WriteToSw
                        case TypeWork.WriteToSw:
                            if (label.Equals("Gate") || label.Equals("Label"))
                                return write(substrokeToLine(sub), (StreamWriter)obj1);
                            return (StreamWriter)obj1;
                        #endregion

                        #region TypeWork.SkipSub
                        case TypeWork.SkipSub:
                            if (!(label.Equals("Gate") || label.Equals("Label")))
                                return true;
                            return false;
                        #endregion

                        #region TypeWork.CorLabel
                        case TypeWork.CorLabel:
                            return label;
                        #endregion

                        #region TypeWork.DoStats
                        case TypeWork.DoStats:
                            string correctLabel = (string)obj1;
                            double[] results = (double[])obj2;
                            switch (correctLabel)
                            {
                                case "Gate":
                                    ++results[2];
                                    if (label.Equals("Gate")) ++results[3];
                                    break;
                                case "Label":
                                    ++results[4];
                                    if (label.Equals("Label")) ++results[5];
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong with TypeWork.GL -> TypeWork.DoStats.");
                                    break;
                            }
                            return results;
                        #endregion

                        #region Default
                        default:
                            Console.WriteLine("Something went wrong in doStuff().");
                            return null;
                        #endregion
                    }
                case TypeRecog.WL:
                    switch (typeWork)
                    {
                        #region Type.SubToLine
                        case TypeWork.SubToLine:
                            if (label.Equals("Wire")) return 1;
                            if (label.Equals("Gate")) return 2;
                            if (label.Equals("Label")) return 3;
                            return -1;
                        #endregion

                        #region TypeWork.WriteToSw
                        case TypeWork.WriteToSw:
                            if (label.Equals("Wire") || label.Equals("Label"))
                                return write(substrokeToLine(sub), (StreamWriter)obj1);
                            return (StreamWriter)obj1;
                        #endregion

                        #region TypeWork.SkipSub
                        case TypeWork.SkipSub:
                            if (!(label.Equals("Wire") || label.Equals("Label")))
                                return true;
                            return false;
                        #endregion

                        #region TypeWork.CorLabel
                        case TypeWork.CorLabel:
                            return label;
                        #endregion

                        #region TypeWork.DoStats
                        case TypeWork.DoStats:
                            string correctLabel = (string)obj1;
                            double[] results = (double[])obj2;
                            switch (correctLabel)
                            {
                                case "Wire":
                                    ++results[0];
                                    if (label.Equals("Wire")) ++results[1];
                                    break;
                                case "Label":
                                    ++results[4];
                                    if (label.Equals("Label")) ++results[5];
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong with TypeRecog.WL -> TypeWork.DoStats.");
                                    break;
                            }
                            return results;
                       #endregion

                        #region Default
                        default:
                            Console.WriteLine("Something went wrong in doStuff().");
                            return null;
                        #endregion
                    }
                case TypeRecog.GNG:
                    switch (typeWork)
                    {
                        #region TypeWork.SubToLine
                        case TypeWork.SubToLine:
                            if (label.Equals("Gate"))
                                return 2;
                            else
                                return 5;
                        #endregion

                        #region TypeWork.WireToSw
                        case TypeWork.WriteToSw:
                            if (label.Equals("Gate") || label.Equals("Wire") ||
                                label.Equals("Label"))
                                return write(substrokeToLine(sub), (StreamWriter)obj1);
                            return (StreamWriter)obj1;
                        #endregion

                        #region TypeWork.SkipSub
                        case TypeWork.SkipSub:
                            if (!(label.Equals("Gate") || label.Equals("Wire") ||
                                  label.Equals("Label")))
                                return true;
                            return false;
                        #endregion

                        #region TypeWork.CorLabel
                        case TypeWork.CorLabel:
                            if (!label.Equals("Gate"))
                                return "Nongate";
                            return label;
                        #endregion

                        #region TypeWork.DoStats
                        case TypeWork.DoStats:
                            string correctLabel = (string)obj1;
                            double[] results = (double[])obj2;
                            switch (correctLabel)
                            {
                                case "Gate":
                                    ++results[2];
                                    if (label.Equals("Gate")) ++results[3];
                                    break;
                                case "Nongate":
                                    ++results[8];
                                    if (label.Equals("Nongate")) ++results[9];
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong with TypeRecog.GNG -> TypeWork.DoStats.");
                                    break;
                            }
                            return results;
                        #endregion

                        #region Default
                        default:
                            Console.WriteLine("Something went wrong in doStuff().");
                            return null;
                        #endregion
                    }

                case TypeRecog.WNW:
                    switch(typeWork)
                    {
                        #region TypeWork.SubToLine
                        case TypeWork.SubToLine:
                            if (label.Equals("Wire"))
                                return 1;
                            else
                                return 4;
                        #endregion

                        #region TypeWork.WireToSw
                        case TypeWork.WriteToSw:
                            if (label.Equals("Gate") || label.Equals("Wire") ||
                                label.Equals("Label"))
                                return write(substrokeToLine(sub), (StreamWriter)obj1);
                            return (StreamWriter)obj1;
                        #endregion

                        #region TypeWork.SkipSub
                        case TypeWork.SkipSub:
                            if (!(label.Equals("Gate") || label.Equals("Wire") ||
                                  label.Equals("Label")))
                                return true;
                            return false;
                        #endregion

                        #region TypeWork.CorLabel
                        case TypeWork.CorLabel:
                            if (!label.Equals("Wire"))
                                return "Nonwire";
                            return label;
                        #endregion

                        #region TypeWork.DoStats
                        case TypeWork.DoStats:
                            string correctLabel = (string)obj1;
                            double[] results = (double[])obj2;
                            switch (correctLabel)
                            {
                                case "Wire":
                                    ++results[0];
                                    if (label.Equals("Wire")) ++results[1];
                                    break;
                                case "Nonwire":
                                    ++results[6];
                                    if (label.Equals("Nonwire")) ++results[7];
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong with TypeRecog.WNW -> TypeWork.DoStats.");
                                    break;
                            }
                            return results;
                        #endregion

                        #region Default
                        default:
                            Console.WriteLine("Something went wrong in doStuff().");
                            return null;
                        #endregion
                    }

                case TypeRecog.LNL:
                    switch(typeWork)
                    {
                        #region TypeWork.SubToLine
                        case TypeWork.SubToLine:
                            if (label.Equals("Label"))
                                return 3;
                            else
                                return 6;
                        #endregion

                        #region TypeWork.WireToSw
                        case TypeWork.WriteToSw:
                            if (label.Equals("Gate") || label.Equals("Wire") ||
                                label.Equals("Label"))
                                return write(substrokeToLine(sub), (StreamWriter)obj1);
                            return (StreamWriter)obj1;
                        #endregion

                        #region TypeWork.SkipSub
                        case TypeWork.SkipSub:
                            if (!(label.Equals("Gate") || label.Equals("Wire") ||
                                  label.Equals("Label")))
                                return true;
                            return false;
                        #endregion

                        #region TypeWork.CorLabel
                        case TypeWork.CorLabel:
                            if (!label.Equals("Label"))
                                return "Nonlabel";
                            return label;
                        #endregion

                        #region TypeWork.DoStats
                        case TypeWork.DoStats:
                            string correctLabel = (string)obj1;
                            double[] results = (double[])obj2;
                            switch (correctLabel)
                            {
                                case "Nonlabel":
                                    ++results[10];
                                    if (label.Equals("Nonlabel")) ++results[11];
                                    break;
                                case "Label":
                                    ++results[4];
                                    if (label.Equals("Label")) ++results[5];
                                    break;
                                default:
                                    Console.WriteLine("Something went wrong with TypeRecog.LNL -> TypeWork.DoStats");
                                    break;
                            }
                            return results;
                        #endregion

                        #region Default
                        default:
                            Console.WriteLine("Something went wrong in doStuff().");
                            return null;
                        #endregion
                    }
                default:
                    Console.WriteLine("Something went wrong with doStuff().");
                    return null;

            }

        }
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
        #endregion
    }
    
}
