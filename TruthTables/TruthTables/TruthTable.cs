using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;
using Pins;
using Sketch;

namespace TruthTables
{
    /// <summary>
    /// author: Sara, Sketchers 2007
    /// recognizes the truth table and creates a 2D list of pins 
    /// (see Util/Pin class) representing it
    /// also outputs a list of pins representing the labels (cast to uppercase)
    /// </summary>
    public class TruthTable
    {

        # region INTERNALS

        /// <summary>
        /// the sketch we want to analyze
        /// </summary>
        private Sketch.Sketch sketch;

        /// <summary>
        /// the truth table grouper we will use
        /// </summary>
        private Grouper.TTGroup grouper;

        /// <summary>
        /// the number of columns in the sketch which we will set
        /// </summary>
        private int numCols;

        /// <summary>
        /// the number of rows in the sketch which we will set
        /// </summary>
        private int numRows;

        /// <summary>
        /// the index of the horizontal divider between input/output
        /// </summary>
        private int divIndex;
        
        /// <summary>
        /// the list of x-coordinates of the first data pieces in each column
        /// </summary>
        private List<float> columns;

        /// <summary>
        /// TEMP 2D row-major List of shapes corresponding to data pieces
        /// </summary>
        private List<List<Sketch.Shape>> shapesList;

        /// <summary>
        /// TEMP Sketch containing only the Label shapes that the grouper
        /// grouped.
        /// </summary>
        private Sketch.Sketch labelSketch;

        /// <summary>
        /// TEMP Matrix of last data elements created by assign()
        /// </summary>
        private MathNet.Numerics.LinearAlgebra.Matrix truthTable;

        /// <summary>
        /// if anything in the sketch is longer than this, there is a vertical divider
        /// </summary>
        private const int VERTICAL_DIV_THRESHOLD = 1000;

         /// <summary>
        /// turn debugging mode on and off
        /// </summary>
        private const bool DEBUG = true;

        /// <summary>
        /// 
        /// </summary>
        private WordList labelWordList;

        /// <summary>
        /// 
        /// </summary>
        private const int NUM_ALTERNATES = 5;

        # endregion

        # region GETTERS/SETTERS

        /// <summary>
        /// get the number of columns in the truth table
        /// </summary>
        public int NumCols
        {
            get
            {
                return this.numCols;
            }
        }

        /// <summary>
        /// get the vertical divider index
        /// </summary>
        public int DivIndex
        {
            get
            {
                return this.divIndex;
            }
        }

        /// <summary>
        /// get the number of rows in the truth table
        /// </summary>
        public int NumRows
        {
            get
            {
                return this.numRows;
            }
        }

        ///
        /// TEMP - TT label domain
        ///
        public const string Divider = "Divider";
        public const string TrueLabel = "True";
        public const string FalseLabel = "False";
        public const string DontCare = "X";
        public const string Label = "Label";
        public const string Other = "Other";
        public const double TrueInt = 1.0D;
        public const double FalseInt = 0.0D;
        public const double DontCareInt = -1.0D;

        /// <summary>
        /// Gets the labeled sketch 
        /// </summary>
        public Sketch.Sketch LabeledSketch
        {
            get
            {
                // Labels are already labeled in the sketch; see outputLabels()
                
                // Label the data shapes, if possible
                /*double label;
                Sketch.Shape shape;
                if (numRows == shapesList.Count)
                {
                    if (numCols == shapesList[0].Count)
                    {
                        for (int r = 0; r < this.truthTable.RowCount; ++r)
                        {
                            for (int c = 0; c < this.truthTable.ColumnCount; ++c)
                            {
                                label = this.truthTable[r, c];
                                shape = this.shapesList[r][c];
                                if (label == TrueInt)
                                {
                                    shape.XmlAttrs.Type = TrueLabel;
                                    //this.sketch.AddLabel(new List<Sketch.Substroke>(shape.Substrokes), TrueLabel);
                                }
                                else if (label == FalseInt)
                                {
                                    shape.XmlAttrs.Type = FalseLabel;
                                    //this.sketch.AddLabel(new List<Sketch.Substroke>(shape.Substrokes), FalseLabel);
                                }
                                else if (label == DontCareInt)
                                {
                                    shape.XmlAttrs.Type = DontCare;
                                    //this.sketch.AddLabel(new List<Sketch.Substroke>(shape.Substrokes), DontCare);
                                }
                            }
                        }
                    }
                }*/

                // Label the dividers
                Sketch.Shape horizDivider = grouper.horizontalDiv;
                if (horizDivider != null)
                {
                    //this.sketch.AddLabel(new List<Sketch.Substroke>(horizDivider.Substrokes), Divider);
                }
                //horizDivider.XmlAttrs.Type = Divider;

                Sketch.Shape verticalDivider = grouper.verticalDiv;
                if (verticalDivider != null)
                {
                    //this.sketch.RemoveLabel(verticalDivider);
                    //this.sketch.AddLabel(new List<Sketch.Substroke>(verticalDivider.Substrokes), Divider);
                }
                //verticalDivider.XmlAttrs.Type = Divider;


                //if (verticalDivider != null)
                //{
                //    this.sketch.AddLabel(new List<Sketch.Substroke>(verticalDivider.Substrokes), Divider);
                //}

                return this.sketch;
            }
        }

        # endregion

        # region CONSTRUCTORS

        /// <summary>
        /// constructor for Truth Tables
        /// </summary>
        /// <param name="sketch">the sketch we want to analyze</param>
        public TruthTable(Sketch.Sketch sketch)
        {
            this.grouper = new Grouper.TTGroup(sketch);
            this.sketch = sketch;
            this.numCols = 0;
            this.numRows = 0;
            this.divIndex = 0;
            this.columns = null;
            this.labelWordList = TextRecognition.TextRecognition.createLabelWordList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="wordlist"></param>
        public TruthTable(Sketch.Sketch sketch, WordList wordlist)
        {
            this.grouper = new Grouper.TTGroup(sketch);
            this.sketch = sketch;
            this.numCols = 0;
            this.numRows = 0;
            this.divIndex = 0;
            this.columns = null;
            this.labelWordList = wordlist;
        }

        # endregion

        # region RECOGNITION
        
        /// <summary>
        /// creates the truth table (main recognition method of entire project)
        /// </summary>
        /// <returns>matrix of 0,1,X</returns>
        public MathNet.Numerics.LinearAlgebra.Matrix assign()
        {
            // group the sketch, which returns the labels (as a sketch)
            Sketch.Sketch[] sketches = this.grouper.group();
            this.sketch = sketches[0];
            this.labelSketch = sketches[1];

            double slopeSum = double.PositiveInfinity;
            int numData = data().Count;

            Console.WriteLine(this.sketch);

            // create the blank matrix representing the truth table
            MathNet.Numerics.LinearAlgebra.Matrix tt = new MathNet.Numerics.LinearAlgebra.Matrix(numRows, numCols);

            List<int> possibleCols = generateCols();

            // guess the number of columns and try to build a table based on each guess
            for (int i = 0; i < possibleCols.Count; i++)
            {
                int cols = possibleCols[i];
                numRows = findNumRows(cols);
                MathNet.Numerics.LinearAlgebra.Matrix temp = new MathNet.Numerics.LinearAlgebra.Matrix(numRows, cols);

                List<Sketch.Shape> sortX = data();
                List<Sketch.Shape> sortY = data();

                // sort data by x and y coordinates
                sortX.Sort(new ShapeComparerX());
                sortY.Sort(new ShapeComparerY());

                this.shapesList = new List<List<Sketch.Shape>>();

                // file shapes into rows based on their x-coordinate
                for (int r = 0; r < numRows; r++)
                {
                    List<Sketch.Shape> shapes = new List<Sketch.Shape>();

                    for (int c = 0; c < cols; c++)
                    {
                        int shapesLeft = cols - c;
                        List<Sketch.Shape> inRow = new List<Sketch.Shape>();
                        for (int m = 0; m < shapesLeft; m++)
                        {
                            inRow.Add(sortY[m]);
                        }

                        int k = 0;
                        Sketch.Shape shapeToAdd = null;
                        while (shapeToAdd == null)
                        {
                            if (inRow.Contains(sortX[k]))
                            {
                                shapeToAdd = sortX[k];
                            }
                            k++;
                        }

                        temp[r, c] = assign01X(shapeToAdd);
                        shapes.Add(shapeToAdd);
                        sortX.Remove(shapeToAdd);
                        sortY.Remove(shapeToAdd);
                    }

                    this.shapesList.Add(shapes);
                }

                // determine the sum of slopes of each row
                float sum = sumOfSlopes(shapesList);

                // if the sum of slopes is smaller,
                // set everything based on this new number of columns
                if (slopeSum > sum)
                {
                    slopeSum = sum;
                    tt = temp;
                    this.columns = columnXs(shapesList);
                    this.divIndex = dividerIndex(columns);
                    numCols = cols;
                }
            }

            numRows = findNumRows(numCols);
            ttGroupLabels(this.labelSketch, this.columns);
            //initializeXML();

            this.truthTable = tt;

            ConverterXML.MakeXML newXml = new ConverterXML.MakeXML(this.sketch);
            newXml.WriteXML("001.xml");

            return tt;
        }

        /*
        /// <summary>
        /// alternate assignment method where the number of columns is known
        /// </summary>
        /// <param name="setCols">number of columns</param>
        /// <returns>matrix representing the truth table</returns>
        public MathNet.Numerics.LinearAlgebra.Matrix assign(int setCols)
        {
            // group the sketch, which returns the labels (as a sketch)
            this.labelSketch = this.grouper.group();

            int numData = data().Count;
            this.numCols = setCols;
            this.numRows = findNumRows(numCols);

            // create the blank matrix representing the truth table
            MathNet.Numerics.LinearAlgebra.Matrix tt = new MathNet.Numerics.LinearAlgebra.Matrix(this.numRows, this.numCols);

            List<Sketch.Shape> sortX = data();
            List<Sketch.Shape> sortY = data();

            // sort data by x and y coordinates
            sortX.Sort(new ShapeComparerX());
            sortY.Sort(new ShapeComparerY());

            this.shapesList = new List<List<Sketch.Shape>>();

            for (int r = 0; r < numRows; r++)
            {
                List<Sketch.Shape> shapes = new List<Sketch.Shape>();

                for (int c = 0; c < numCols; c++)
                {
                    int shapesLeft = numCols - c;
                    List<Sketch.Shape> inRow = new List<Sketch.Shape>();
                    for (int m = 0; m < shapesLeft; m++)
                    {
                        inRow.Add(sortY[m]);
                    }

                    int k = 0;
                    Sketch.Shape shapeToAdd = null;
                    while (shapeToAdd == null)
                    {
                        if (inRow.Contains(sortX[k]))
                        {
                            shapeToAdd = sortX[k];
                        }
                        k++;
                    }

                    tt[r, c] = assign01X(shapeToAdd);
                    shapes.Add(shapeToAdd);
                    sortX.Remove(shapeToAdd);
                    sortY.Remove(shapeToAdd);
                }

                this.shapesList.Add(shapes);
            }

            // set things based on the number of columns
            this.columns = columnXs(shapesList);
            this.divIndex = dividerIndex(columns);
            int numlabels = ttGroupLabels(this.labelSketch, this.columns);

            this.truthTable = tt;
            initializeXML();

            return tt;
        }
        */

        /// <summary>
        /// alternate label grouping method that groups labels based on their
        /// x-coordinate proximity to the shapes in the first row
        /// </summary>
        /// <param name="sk">the label sketch</param>
        /// <param name="columns">the list of column x-coordinates</param>
        public int ttGroupLabels(Sketch.Sketch sk, List<float> columns)
        {
            if (columns == null)
                return -1;

            int cols = columns.Count; 

            Sketch.Shape[] shapes = new Sketch.Shape[cols];

            for (int m = 0; m < cols; m++)
            {
                Sketch.XmlStructs.XmlShapeAttrs newXML = new Sketch.XmlStructs.XmlShapeAttrs(true);
                newXML.Name = "shape";
                newXML.Type = "Label";

                Sketch.Shape sh = new Sketch.Shape(new List<Shape>(), new List<Substroke>(), newXML);

                shapes[m] = sh;
            }

            //List<Substroke> subs = new List<Substroke>();

            int len = sk.Substrokes.Length;
            for (int k = 0; k < len; k++)
            {
                Sketch.Substroke sub = sk.Substrokes[k];

                double diff = double.PositiveInfinity;
                int col = 0;
                for (int i = 0; i < cols; i++)
                {
                    if (diff > Math.Abs(subMidPoint(sub).XmlAttrs.X.Value - columns[i]))
                    {
                        diff = Math.Abs(subMidPoint(sub).XmlAttrs.X.Value - columns[i]);
                        col = i;
                    }
                }
                shapes[col].AddSubstroke(sub);
            }

            int numLabels = 0;
            for (int j = 0; j < cols; j++)
            {
                if (shapes[j].Substrokes.Length != 0)
                {
                    this.sketch.AddLabel(shapes[j].SubstrokesL, "Label");
                    numLabels++;
                }
            }

            return numLabels;
        }


        /// <summary>
        /// computes the sum of the slopes of all the row in the truth table
        /// </summary>
        /// <param name="shapesList">2D list of shapes representing the truth table</param>
        /// <returns>the sum of the slopes of the rows</returns>
        public float sumOfSlopes(List<List<Sketch.Shape>> shapesList)
        {
            int numLists = shapesList.Count;
            float slopesSum = 0;

            for (int i = 0; i < numLists; i++)
            {
                List<Sketch.Shape> shapes = shapesList[i];
                int len = shapes.Count;

                shapes.Sort(new ShapeComparerX());
                float minX = shapes[0].XmlAttrs.X.Value;
                float maxX = shapes[len-1].XmlAttrs.X.Value;

                shapes.Sort(new ShapeComparerY());
                float minY = shapes[0].XmlAttrs.Y.Value;
                float maxY = shapes[len-1].XmlAttrs.Y.Value;

                slopesSum += Math.Abs((maxY - minY) / (maxX - minX));
            }

            return slopesSum;
        }

        /// <summary>
        /// generate a list of possible column numbers
        /// based on modular arithmetic
        /// </summary>
        /// <returns></returns>
        public List<int> generateCols()
        {
            List<int> possibleColumns = new List<int>();
            int numData = data().Count;

            // special case when the table is 2x2
            if (numData == 4)
            {
                possibleColumns.Add(2);
            }
            else
            {
                for (int i = 3; i < numData; i++)
                {
                    double ratio = (double)numData / (double)i;

                    // see if the number of cols divides the number of data evenly
                    if (Math.Floor(ratio) == Math.Ceiling(ratio) && ratio != 2)
                    {
                        possibleColumns.Add((int)ratio);

                        if (DEBUG) { Console.WriteLine("cols: " + ratio); }

                    }
                }
            }

            // if no columns numbers were possible
            // aka: the user messed up the number of data points
            if (possibleColumns.Count == 0)
            {
                for (int j = 2; j < Math.Sqrt(numData) + 1; j++)
                {
                    possibleColumns.Add(j);
                }
            }

            return possibleColumns;
        }

        # endregion    

        # region PINS
        
        /// <summary>
        /// creates a 2D list of pins representing the data points
        /// </summary>
        /// <param name="matrix">matrix representing the truth table</param>
        /// <returns>2D list of pins</returns>
        public List<List<Pin>> createPins(MathNet.Numerics.LinearAlgebra.Matrix matrix)
        {
            int rows = matrix.RowCount;
            int cols = matrix.ColumnCount;
            List<List<Pin>> pins = new List<List<Pin>>();

            for (int r = 0; r < rows; r++)
            {
                List<Pin> newRow = new List<Pin>();

                for (int c = 0; c < cols; c++)
                {
                    // find the pinName for each data piece
                    string pinName = matrix[r, c].ToString();
                    if (pinName.Equals("-1"))
                    {
                        pinName = "X";
                    }

                    // determine whether data piece is input or output
                    int divider = this.divIndex;
                    PinPolarity inOut = PinPolarity.Wire;

                    if (divider > c)
                    {
                        inOut = PinPolarity.Input;
                    }
                    if (divider <= c)
                    {
                        inOut = PinPolarity.Ouput;
                    }

                    // create the new pin and add it to the list
                    Pin newPin = new Pin(inOut, pinName, 1);
                    newRow.Add(newPin);

                    if (DEBUG) { Console.WriteLine("pin[" + r + "][" + c + "] in/out: " + inOut + " name: " + pinName); }

                }

                pins.Add(newRow);
            }

            return pins;
        }

        /// <summary>
        /// Outputs the labels in the sketch as a list of pins.  Also labels
        /// the corresponding Label shapes with their respective Pin names.
        /// </summary>
        /// <param name="sketch">the sketch we want to analyze</param>
        /// <returns>list of pins</returns>
        public List<Pin> outputLabels()
        {
            List<Pin> labels = new List<Pin>();
            Sketch.Shape vertical = this.grouper.verticalDiv;

            Sketch.Shape[] shapes = sketch.Shapes;
            int len = shapes.Length;

            for (int i = 0; i < len; i++)
            {
                if (assignType(shapes[i]).Equals("Label"))
                {
                    PinPolarity inOut = PinPolarity.Wire;
                    string[] text = assignLabel(shapes[i]);

                    if (shapes[i].XmlAttrs.X.Value < vertical.XmlAttrs.X.Value)
                    {
                        inOut = PinPolarity.Input;
                    }
                    else
                    {
                        inOut = PinPolarity.Ouput;
                    }

                    Pin label = new Pin(inOut, text, shapes[i]);

                    if (DEBUG) 
                    { 
                        Console.WriteLine(text[0] + "  " + text[1]);
                        Console.WriteLine(label.PinName);
                    }

                    labels.Add(label);
                }
            }

            foreach (Pins.Pin p in labels)
            {
                Sketch.Shape sh = p.Shape;
                sh.XmlAttrs.Type = "Label";
                sh.XmlAttrs.Text = p.PinName;
            }

            return labels;
        }

        /// <summary>
        /// initializes the necessary xml attributes of the strokes and substrokes
        /// </summary>
        public void initializeXML()
        {
            // make sure all the strokes in the sketch have initialized xml attributes
            for (int k = 0; k < this.sketch.Strokes.Length; k++)
            {
                this.sketch.Strokes[k].XmlAttrs.Id = System.Guid.NewGuid();
                this.sketch.Strokes[k].XmlAttrs.Name = "stroke";
                this.sketch.Strokes[k].XmlAttrs.Type = "stroke";
            }

            // make sure all the substrokes in the sketch have initialized xml attributes
            for (int a = 0; a < this.sketch.Substrokes.Length; a++)
            {
                this.sketch.Substrokes[a].XmlAttrs.Id = System.Guid.NewGuid();
                this.sketch.Substrokes[a].XmlAttrs.Name = "substroke";
                this.sketch.Substrokes[a].XmlAttrs.Type = "substroke";
            }
        }

        # endregion

        # region MISC

        /// <summary>
        /// finds the midpoint of a shape
        /// </summary>
        /// <param name="sh">the shape we want to analyze</param>
        /// <returns>the midpoint of the shape</returns>
        public Sketch.Point shapeMidPoint(Sketch.Shape sh)
        {
            Sketch.Point p = new Sketch.Point();
            p.XmlAttrs.X = sh.XmlAttrs.X.Value + (sh.XmlAttrs.Width.Value / 2);
            p.XmlAttrs.Y = sh.XmlAttrs.Y.Value + (sh.XmlAttrs.Height.Value / 2);

            return p;
        }

        /// <summary>
        /// finds the midpoint of a substroke
        /// </summary>
        /// <param name="sh">the substroke we want to analyze</param>
        /// <returns>the midpoint of the substroke</returns>
        public Sketch.Point subMidPoint(Sketch.Substroke sub)
        {
            Sketch.Point p = new Sketch.Point();
            p.XmlAttrs.X = sub.XmlAttrs.X.Value + (sub.XmlAttrs.Width.Value / 2);
            p.XmlAttrs.Y = sub.XmlAttrs.Y.Value + (sub.XmlAttrs.Height.Value / 2);

            return p;
        }

        /// <summary>
        /// returns the number of rows in a truth table based on number of columns
        /// </summary>
        /// <returns>number of rows in the truth table</returns>
        public int findNumRows(int numCols)
        {
            if (numCols != 0)
            {
                return data().Count / numCols;
            }
            else
            {
                return -1;
            }

            
        }

        /// <summary>
        /// returns a list of shapes that are not dividers or labels (they are "data")
        /// </summary>
        /// <returns>list of shapes that are not dividers and not labels</returns>
        public List<Sketch.Shape> data()
        {
            List<Sketch.Shape> data = new List<Sketch.Shape>();

            Sketch.Shape[] shapes = this.sketch.Shapes;
            int len = shapes.Length;

            for (int i = 0; i < len; i++)
            {
                string type = assignType(shapes[i]);

                if (!type.Equals("Label") && !type.EndsWith("Divider"))
                {
                    data.Add(shapes[i]);
                }
            }

            return data;
        }

        /// <summary>
        /// new column method which relies on the first row of data, not the labels
        /// for finding the x-coordinates of the columns
        /// </summary>
        /// <param name="shapes">2D array of data</param>
        /// <returns>list of x-coordinates of the columns</returns>
        public List<float> columnXs(List<List<Sketch.Shape>> shapes)
        {
            List<float> cols = new List<float>();

            List<Sketch.Shape> row0 = shapes[0];
            int len = row0.Count;

            for (int i = 0; i < len; i++)
            {
                cols.Add(shapeMidPoint(row0[i]).XmlAttrs.X.Value);
            }

            cols.Sort(); // sort the list of x-coordinates
            return cols;
        }

        /// <summary>
        /// returns the column index of the divider between input and output
        /// </summary>
        /// <returns>column index of the vertical divider</returns>
        public int dividerIndex(List<float> columns)
        {
            Sketch.Shape vertical = this.grouper.verticalDivider();
            double x = vertical.XmlAttrs.X.Value;
            int index = 0;

            if (vertical.XmlAttrs.Height.Value < VERTICAL_DIV_THRESHOLD) // no vertical divider
            {
                float gap = 0;
                for (int i = 0; i < columns.Count - 1; i++)
                {
                    if (Math.Abs(columns[i] - columns[i + 1]) > gap)
                    {
                        gap = Math.Abs(columns[i] - columns[i + 1]);
                        index = i + 1;
                    }
                }
            }
            else // yes vertical divider
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    if (x < columns[i])
                    {
                        return i;
                    }
                }
            }
            return index;
        }

        # endregion 

        # region TEXT_RECOGNITION

        /// <summary>
        /// assigns a type to shape using microsoft ink recognizer for 1,0,X
        /// </summary>
        /// <param name="sh">the shape we want to analyze</param>
        /// <returns>a string representing that shape's type</returns>
        public string assignType(Sketch.Shape sh)
        {
            Sketch.Shape horizontal = this.grouper.horizontalDiv;
            Sketch.Shape vertical = this.grouper.verticalDiv;

            if (sh.Equals(horizontal))
            {
                sh.XmlAttrs.Type = "Divider";
                return "HorizontalDivider";
            }
            else if (sh.Equals(vertical))
            {
                sh.XmlAttrs.Type = "Divider";
                return "VerticalDivider";
            }
            else if (sh.XmlAttrs.Y.Value < horizontal.XmlAttrs.Y.Value)
            {
                return "Label";
            }
            else
            {
                string factoid = TextRecognition.TextRecognition.data;
                return TextRecognition.TextRecognition.recognize(sh, factoid, RecognitionModes.Coerce);
            }
        }

        /// <summary>
        /// recognizes a data symbol with 0,1,X restrictions
        /// </summary>
        /// <param name="sh">the data shape</param>
        /// <returns>a double representing the recognized shape</returns>
        public double assign01X(Sketch.Shape sh)
        {
            string factoid = TextRecognition.TextRecognition.data;
            string text = TextRecognition.TextRecognition.recognize(sh, factoid, RecognitionModes.Coerce);

            if (text.Equals("1")) 
            {
                sh.XmlAttrs.Type = TrueLabel;
                return 1; 
            }
            if (text.Equals("X")) 
            {
                sh.XmlAttrs.Type = DontCare;
                return -1; 
            }
            else 
            {
                sh.XmlAttrs.Type = FalseLabel;
                return 0; 
            }
        }

        /// <summary>
        /// recognizes a label without restrictions
        /// </summary>
        /// <param name="sh">the label shape</param>
        /// <returns>the recognized string</returns>
        public string[] assignLabel(Sketch.Shape sh)
        {
            return TextRecognition.TextRecognition.recognizeAlternates(sh, this.labelWordList, RecognitionModes.Coerce, NUM_ALTERNATES);
        }

        # endregion
    }
}
