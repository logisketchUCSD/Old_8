using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Sketch;
using Utilities;

namespace ShapeTemplates
{
    public class ImageTemplate
    {
        #region Member Variables

        /// <summary>
        /// All the general information about the shape.
        /// User info, shape descriptions, under what conditions the symbol was drawn.
        /// </summary>
        SymbolInfo _symbolInfo;

        /// <summary>
        ///  Entire shape description
        /// </summary>
        ImageSymbol _shapePoints;

        /// <summary>
        /// Descriptions for each stroke in shape
        /// </summary>
        List<ImageSymbol> _strokePoints;

        Dictionary<ImageSymbol, GatePart> _strokeInfo;

        #endregion

        #region Constructors

        /// <summary>
        /// 
        /// </summary>
        /// <param name="strokes"></param>
        public ImageTemplate(List<Substroke> strokes)
        {
            _symbolInfo = new SymbolInfo();

            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            foreach (Substroke s in strokes)
                foreach (Point pt in s.PointsL)
                    points.Add(pt.SysDrawPoint);

            System.Drawing.Point[] pointsA = points.ToArray();
            System.Drawing.Rectangle bbox = Compute.BoundingBox(pointsA);
            
            _shapePoints = new ImageSymbol(pointsA, bbox, _symbolInfo);

            _strokePoints = new List<ImageSymbol>();
            foreach (Substroke s in strokes)
                _strokePoints.Add(new ImageSymbol(s.PointsAsSysPoints, bbox, _symbolInfo));
        }

        public ImageTemplate(List<Substroke> strokes, Dictionary<Substroke, GatePart> strokeInfo, SymbolInfo info)
        {
            _symbolInfo = info;

            List<System.Drawing.Point> points = new List<System.Drawing.Point>();
            foreach (Substroke s in strokes)
                foreach (Point pt in s.PointsL)
                    points.Add(pt.SysDrawPoint);

            System.Drawing.Point[] pointsA = points.ToArray();
            System.Drawing.Rectangle bbox = Compute.BoundingBox(pointsA);

            _shapePoints = new ImageSymbol(pointsA, bbox, info);

            _strokePoints = new List<ImageSymbol>();
            _strokeInfo = new Dictionary<ImageSymbol, GatePart>();
            foreach (Substroke s in strokes)
            {
                ImageSymbol symbol = new ImageSymbol(s.PointsAsSysPoints, bbox, info);
                _strokePoints.Add(symbol);
                _strokeInfo.Add(symbol, strokeInfo[s]);
            }
        }

        #endregion

        #region Getters

        #endregion

        #region Recognition

        public string Recognize(List<ImageTemplate> templates, Dictionary<Substroke, GatePart> strokeResults)
        {
            List<ImageSymbol> completeSymbols = new List<ImageSymbol>(templates.Count);
            foreach (ImageTemplate t in templates)
                completeSymbols.Add(t._shapePoints);

            SortedList<double, ImageResult> results = _shapePoints.Recognize(completeSymbols);

            List<GatePart> parts = new List<GatePart>(strokeResults.Values);
            Gate bestGuess = GetBestGuess(parts);
            int numSame = 0;
            int numDifferent = 0;
            double threshold = 8.0;
            foreach (KeyValuePair<double, ImageResult> pair in results)
            {
                if (pair.Key <= threshold)
                {
                    if (pair.Value.Name == bestGuess.ToString())
                        numSame++;
                    else
                        numDifferent++;
                }
                else
                    break;
            }

            if (numSame > 0)
                return bestGuess.ToString();
            else
            {
                List<ImageResult> imageResults = new List<ImageResult>(results.Values);
                string m = "Best Guess = " + bestGuess.ToString();
                m += ", Image = " + imageResults[0].Name;
                m += ", Components: ";
                foreach (GatePart p in parts)
                    m += p.ToString() + ", ";
                return m;
            }
        }

        private Gate GetBestGuess(List<GatePart> parts)
        {
            int num = parts.Count;
            Gate bestGuess = Gate.Unknown;
            if (parts.Contains(GatePart.BackLine)
                && parts.Contains(GatePart.FrontArc))
            {
                if (!parts.Contains(GatePart.Bubble) && num == 2)
                    bestGuess = Gate.AND;
                else if (parts.Contains(GatePart.Bubble) && num == 3)
                    bestGuess = Gate.NAND;
            }
            else if (parts.Contains(GatePart.BackArc)
                && parts.Contains(GatePart.FrontArc))
            {
                int numBackArc = 0;
                foreach (GatePart p in parts)
                    if (p == GatePart.BackArc)
                        numBackArc++;

                if (numBackArc == 1)
                {
                    if (!parts.Contains(GatePart.Bubble) && num == 2)
                        bestGuess = Gate.OR;
                    else if (parts.Contains(GatePart.Bubble) && num == 3)
                        bestGuess = Gate.NOR;
                }
                else if (numBackArc > 1)
                {
                    if (!parts.Contains(GatePart.Bubble) && num == 3)
                        bestGuess = Gate.XOR;
                    else if (parts.Contains(GatePart.Bubble) && num == 4)
                        bestGuess = Gate.XNOR;
                }
            }
            else if (parts.Contains(GatePart.BackArc)
                && parts.Contains(GatePart.TopArc)
                && parts.Contains(GatePart.BottomArc))
            {
                int numBackArc = 0;
                foreach (GatePart p in parts)
                    if (p == GatePart.BackArc)
                        numBackArc++;

                if (numBackArc == 1)
                {
                    if (!parts.Contains(GatePart.Bubble) && num == 3)
                        bestGuess = Gate.OR;
                    else if (parts.Contains(GatePart.Bubble) && num == 4)
                        bestGuess = Gate.NOR;
                }
                else if (numBackArc > 1)
                {
                    if (!parts.Contains(GatePart.Bubble) && num == 4)
                        bestGuess = Gate.XOR;
                    else if (parts.Contains(GatePart.Bubble) && num == 5)
                        bestGuess = Gate.XNOR;
                }
            }
            else if (parts.Contains(GatePart.Triangle)
                && parts.Contains(GatePart.Bubble)
                && num == 2)
                bestGuess = Gate.NOT;
            else if (parts.Contains(GatePart.BackLine)
                && parts.Contains(GatePart.TopLine)
                && parts.Contains(GatePart.BottomLine)
                && parts.Contains(GatePart.Bubble)
                && num == 4)
                bestGuess = Gate.NOT;
            else if (parts.Contains(GatePart.BackLine)
                && parts.Contains(GatePart.GreaterThan)
                && parts.Contains(GatePart.Bubble)
                && num == 3)
                bestGuess = Gate.NOT;
            else if (parts.Contains(GatePart.Bubble)
                && num == 1)
                bestGuess = Gate.NOTBUBBLE;

            return bestGuess;
        }

        #endregion

        public string Name
        {
            get { return _symbolInfo.SymbolType; }
        }
    }
}
