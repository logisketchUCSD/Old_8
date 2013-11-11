using System;
using System.IO;
using ConverterXML;
using System.Collections.Generic;

namespace TruthTables
{
    /// <summary>
    /// author: Sara Sheehan, Sketchers 2007
    /// Goal for this class: help debug truth table recognition
    /// </summary>
    internal class Debugging
    {   
        /// <summary>
        /// main for printing/debugging
        /// </summary>
        /// <param name="args">string array of file names of truth tables</param>
        static void Main(string[] args)
        {
            string[] argss = new string[]{"0128_9-22_tt7.1.xml"};
            // for each truth table
            for (int i = 0; i < argss.Length; i++)
            {
                
                Sketch.Sketch sketch = (new ReadXML(argss[i])).Sketch;
                TruthTable tt = new TruthTable(sketch);

                

                // create the truth table matrix
                MathNet.Numerics.LinearAlgebra.Matrix truthTable = tt.assign();
                sketch = tt.LabeledSketch;

                int cols = tt.NumCols;
                Console.WriteLine("cols: " + cols);
                int rows = tt.NumRows;
                Console.WriteLine("rows: " + rows);


                // print out data about the shapes after grouping
                Sketch.Shape[] shapes = tt.LabeledSketch.Shapes;
                int len = shapes.Length;

                for (int l = 0; l < len; l++)
                {
                    Console.WriteLine("[" + l + "] type: " + shapes[l].XmlAttrs.Type + ", " + tt.assignType(shapes[l]));
                    Console.WriteLine("x,y " + shapes[l].XmlAttrs.X + "," + shapes[l].XmlAttrs.Y);
                    Console.WriteLine("id: " + shapes[l].XmlAttrs.Id + " name: " + shapes[l].XmlAttrs.Name + " time: " + shapes[l].XmlAttrs.Time + "\n");
                }

                // print out the labels
                tt.outputLabels();

                // put the data values into the matrix,
                // including the underscore between inputs and outputs
                string[] list = new string[rows];
                int d = tt.DivIndex;
                Console.WriteLine("divider index: " + d);

                for (int j = 0; j < rows; j++)
                {
                    for (int k = 0; k < cols; k++)
                    {
                        if (k == d)
                        {
                            list[j] += "_"; // separate inputs from outputs
                        }
                        if (truthTable[j, k] == -1)
                        {
                            list[j] += "X";
                        }
                        else
                        {
                            list[j] += truthTable[j, k];
                        }
                    }
                }

                // print out the matrix
                for (int u = 0; u < list.Length; u++)
                {
                    Console.WriteLine(list[u]);
                }

                // make sure all the strokes in the sketch have initialized xml attributes
                for (int k = 0; k < sketch.Strokes.Length; k++)
                {
                    sketch.Strokes[k].XmlAttrs.Id = System.Guid.NewGuid();
                    sketch.Strokes[k].XmlAttrs.Name = "stroke";
                    sketch.Strokes[k].XmlAttrs.Type = "stroke";
                }

                // make sure all the substrokes in the sketch have initialized xml attributes
                for (int a = 0; a < sketch.Substrokes.Length; a++)
                {
                    sketch.Substrokes[a].XmlAttrs.Id = System.Guid.NewGuid();
                    sketch.Substrokes[a].XmlAttrs.Name = "substroke";
                    sketch.Substrokes[a].XmlAttrs.Type = "substroke";
                }
                // print out the final sketch as an xml
                ConverterXML.MakeXML newXml = new ConverterXML.MakeXML(sketch);
                newXml.WriteXML("finaltest.xml");
            }
        }   
    }
}

