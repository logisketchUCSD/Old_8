using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using Utilities;
using Utilities.Matrix;

namespace ShapeTemplates
{
    [Serializable]
    public class ImageSymbol
    {
        #region Member Variables & Constants

        private int GRID_SIZE = 48;
        private const double REL_DIST_SCALE_FACTOR = 2.4;
        private const double HAUSDORFF_QUANTILE = 0.94;

        private SymbolInfo _SymbolInfo;

        /// <summary>
        /// Points that have been quantized to the specified grid size
        /// </summary>
        private List<Point> _screenQuantizedPoints;

        /// <summary>
        /// Distance Transform Matrix for fast computation of an 
        /// overlayed symbols distances
        /// </summary>
        private GeneralMatrix _sDTM;

        /// <summary>
        /// Points that have been quantized to the specified grid size
        /// </summary>
        private List<Point> _polarQuantizedPoints;

        /// <summary>
        /// Distance Transform Matrix for fast computation of an 
        /// overlayed symbols distances
        /// </summary>
        private GeneralMatrix _pDTM;

        private int _polarYmax;

        #endregion

        #region Constructors

        public ImageSymbol(Point[] points, SymbolInfo info)
        {
            _SymbolInfo = info;
            Process(points);
        }

        public ImageSymbol(Point[] points, Rectangle bbox, SymbolInfo info)
        {
            _SymbolInfo = info;
            BoundingPoints corners = new BoundingPoints(bbox);
            Process(points, corners);
        }

        public ImageSymbol()
        {
        }

        #endregion

        #region Getters

        private List<Point> QuantizedScreenPoints
        {
            get { return _screenQuantizedPoints; }
        }

        private GeneralMatrix DistanceTransformMatrixScreen
        {
            get { return _sDTM; }
        }
        
        public string Name
        {
            get { return _SymbolInfo.SymbolType; }
        }

        #endregion

        #region Processing

        /// <summary>
        /// Processes the original points to get the "Bitmap" image of the shape
        /// in both screen and polar coordinates
        /// </summary>
        /// <param name="points">Original points in the shape</param>
        private void Process(Point[] points)
        {
            BoundingPoints corners = new BoundingPoints(points);
            Process(points, corners);
        }

        /// <summary>
        /// Processes the original points to get the "Bitmap" image of the shape
        /// in both screen and polar coordinates
        /// </summary>
        /// <param name="points">Original points in the shape</param>
        /// <param name="corners">Boundaries of the shape</param>
        private void Process(Point[] points, BoundingPoints corners)
        {
            _screenQuantizedPoints = QuantizePointsScreen(points, corners);
            _sDTM = DistanceTransformScreen(_screenQuantizedPoints);
            List<PolarPoint> polarPoints = Transform_Screen2Polar(points);
            _polarQuantizedPoints = QuantizePointsPolar(polarPoints);
            _pDTM = DistanceTransformPolar(_polarQuantizedPoints);
            _polarYmax = FindYmax(_polarQuantizedPoints);
        }

        /// <summary>
        /// Determines the coordinates of the "rasterized" points (in screen coordinates)
        /// </summary>
        /// <param name="points">Original screen points</param>
        /// <param name="corners">The corners of the shape's bounding box</param>
        /// <returns>Quantized points for screen coordinate system</returns>
        private List<Point> QuantizePointsScreen(Point[] points, BoundingPoints corners)
        {
            List<Point> qPoints = new List<Point>();
            if (points.Length == 0) return qPoints;

            double sq_side = Math.Max(corners.Height, corners.Width);

            double step = sq_side / (double)(GRID_SIZE - 1);

            PointF c = corners.Center;

            GeneralMatrix mesh = new GeneralMatrix(GRID_SIZE, GRID_SIZE, 0.0);

            // For each points calculate its relative location inside the bounding box
            for (int i = 0; i < points.Length; i++)
            {
                int x_index = (int)Math.Floor(((points[i].X - c.X) / step) + GRID_SIZE / 2);
                int y_index = (int)Math.Floor(((points[i].Y - c.Y) / step) + GRID_SIZE / 2);

                if (x_index < 0)
                    x_index = 0;
                else if (x_index >= GRID_SIZE)
                    x_index = GRID_SIZE - 1;

                if (y_index < 0)
                    y_index = 0;
                else if (y_index >= GRID_SIZE)
                    y_index = GRID_SIZE - 1;

                mesh.SetElement(y_index, x_index, 1.0);
            }

            // Go throught the entire matrix and create new points 
            // whenever you encounter a value greater than 0
            for (int i = 0; i < GRID_SIZE; i++)
                for (int j = 0; j < GRID_SIZE; j++)
                    if (mesh.GetElement(i, j) > 0.0)
                        qPoints.Add(new Point(j, i));

            return qPoints;
        }

        /// <summary>
        /// Determines the coordinates of the "rasterized" points (in polar coordinates)
        /// </summary>
        /// <param name="points">Regular polar points</param>
        /// <returns>Quantized points for polar coordinate system</returns>
        private List<Point> QuantizePointsPolar(List<PolarPoint> points)
        {
            List<Point> qPoints = new List<Point>();
            if (points.Count == 0) return qPoints;

            GeneralMatrix mesh = new GeneralMatrix(GRID_SIZE, GRID_SIZE, 0.0);

            double stepX = 2.0 * Math.PI / GRID_SIZE;
            double stepY = REL_DIST_SCALE_FACTOR / GRID_SIZE;

            // For each points calculate its relative location inside the bounding box
            foreach (PolarPoint pt in points)
            {
                int x_index = (int)Math.Floor(((pt.AngularPosition + Math.PI) / stepX));
                int y_index = (int)Math.Floor((pt.RelativeDistance / stepY));

                if (x_index < 0)
                    x_index = 0;
                else if (x_index >= GRID_SIZE)
                    x_index = GRID_SIZE - 1;

                if (y_index < 0)
                    y_index = 0;
                else if (y_index >= GRID_SIZE)
                    y_index = GRID_SIZE - 1;

                mesh.SetElement(y_index, x_index, 1.0);
            }

            // Go throught the entire matrix and create new points 
            // whenever you encounter a value greater than 0
            for (int i = 0; i < GRID_SIZE; i++)
                for (int j = 0; j < GRID_SIZE; j++)
                    if (mesh.GetElement(i, j) > 0.0)
                        qPoints.Add(new Point(j, i));

            return qPoints;
        }

        /// <summary>
        /// Computes the distance transform matrix for screen coordinates
        /// </summary>
        /// <param name="qPoints">quantized points in screen coordinates</param>
        /// <returns>Distance Transform Matrix</returns>
        private GeneralMatrix DistanceTransformScreen(List<Point> qPoints)
        {
            GeneralMatrix DTM = new GeneralMatrix(GRID_SIZE, GRID_SIZE, double.PositiveInfinity);

            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    Point current = new Point(j, i);

                    double mindist = double.PositiveInfinity;

                    foreach (Point pt in qPoints)
                        mindist = Math.Min(mindist, Compute.EuclideanDistance(current, pt));

                    DTM.SetElement(i, j, mindist);
                }
            }

            return DTM;
        }

        /// <summary>
        /// Computes the distance transform matrix for polar coordinates.
        /// In polar coordinates this means we have to check for a shift of 
        /// 2 pi
        /// </summary>
        /// <param name="qPoints">quantized points in polar coordinates</param>
        /// <returns>Distance Transform Matrix</returns>
        private GeneralMatrix DistanceTransformPolar(List<Point> qPoints)
        {
            GeneralMatrix DTM = new GeneralMatrix(GRID_SIZE, GRID_SIZE, double.PositiveInfinity);

            for (int i = 0; i < GRID_SIZE; i++)
            {
                for (int j = 0; j < GRID_SIZE; j++)
                {
                    Point current = new Point(j, i);

                    double mindist = double.PositiveInfinity;

                    foreach (Point pt in qPoints)
                    {
                        double distance1 = Compute.EuclideanDistance(current, pt);
                        double dx = (double)GRID_SIZE - Math.Abs(current.X - pt.X);
                        double dy = Math.Abs(current.Y - pt.Y);
                        double distance2 = Math.Sqrt(Math.Pow(dx, 2.0) + Math.Pow(dy, 2.0));
                        double distance = Math.Min(distance1, distance2);
                        mindist = Math.Min(mindist, distance);
                    }

                    DTM.SetElement(i, j, mindist);
                }
            }

            return DTM;
        }

        /// <summary>
        /// Creates polar points from the screen coordinates
        /// </summary>
        /// <param name="points">points in screen coordinates</param>
        /// <returns>Polar points - unquantized</returns>
        private List<PolarPoint> Transform_Screen2Polar(Point[] points)
        {
            int sumX = 0;
            int sumY = 0;
            foreach (Point pt in points)
            {
                sumX += pt.X;
                sumY += pt.Y;
            }
            Point center = new Point(sumX / points.Length, sumY / points.Length);

            double avgDistance = 0.0;

            Dictionary<Point, double> distances = new Dictionary<Point, double>(points.Length);
            foreach (Point pt in points)
            {
                double dist = Compute.EuclideanDistance(pt, center);
                if (!distances.ContainsKey(pt))
                    distances.Add(pt, dist);
                avgDistance += dist;
            }

            avgDistance /= points.Length;

            List<PolarPoint> polarPoints = new List<PolarPoint>(points.Length);
            foreach (Point pt in points)
            {
                double angPosition = Math.Atan2(pt.Y - center.Y, pt.X - center.X);
                double relDistance;
                if (distances.ContainsKey(pt))
                    relDistance = distances[pt] / avgDistance;
                else
                    relDistance = Compute.EuclideanDistance(pt, center) / avgDistance;
                polarPoints.Add(new PolarPoint(angPosition, relDistance));
            }

            return polarPoints;
        }

        /// <summary>
        /// Find the maximum "Y" value of the polar quantized points
        /// </summary>
        /// <param name="points">polar quantized points</param>
        /// <returns>Max value - used for scaling</returns>
        private int FindYmax(List<Point> points)
        {
            int max = int.MinValue;
            foreach (Point pt in points)
                max = Math.Max(pt.Y, max);

            return max;
        }

        #endregion

        #region Recognition

        /// <summary>
        /// Assume no rotation!
        /// </summary>
        /// <param name="templates"></param>
        /// <returns></returns>
        public SortedList<double, ImageResult> Recognize(List<ImageSymbol> templates)
        {
            SortedList<double, ImageResult> results = new SortedList<double, ImageResult>(templates.Count);

            foreach (ImageSymbol symbol in templates)
            {
                ImageResult r = Recognize(symbol);
                double score = r.Score;
                while (results.ContainsKey(score))
                    score += double.Epsilon;

                results.Add(score, r);
            }

            return results;
        }

        /// <summary>
        /// Calculates each of the image-based metrics between two symbols
        /// </summary>
        /// <param name="template">ImageSymbol to compare 'this' to</param>
        /// <returns>ImageResult for the two symbols</returns>
        public ImageResult Recognize(ImageSymbol template)
        {
            double haus = HausdorffDistance(template);
            double modHaus = ModifiedHausdorffDistance(template);
            double tanimoto = TanimotoCoefficient(template);
            double yule = YuleCoefficient(template);

            return new ImageResult(this, template, haus, modHaus, tanimoto, yule);
        }

        /// <summary>
        /// Calculates the Hausdorff Distance (HD) between the two ImageSymbols.
        /// HD is defined as the max pixel distance (in Xth quantile) between the images (max of 2 directed).
        /// </summary>
        /// <param name="template">ImageSymbol to compare 'this' to</param>
        /// <returns>Hausdorff Distance</returns>
        private double HausdorffDistance(ImageSymbol template)
        {
            double hAB = this.DirectedHausdorffDistance(template);
            double hBA = template.DirectedHausdorffDistance(this);

            return Math.Max(hAB, hBA);
        }

        /// <summary>
        /// Calculates the directed Hausdorff Distance between the two ImageSymbols.
        /// Uses a quantile to filter out most extreme outliers
        /// </summary>
        /// <param name="template">ImageSymbol to compare 'this' to</param>
        /// <returns>Directed Hausdorff Distance</returns>
        private double DirectedHausdorffDistance(ImageSymbol template)
        {
            List<double> distances = new List<double>(template.QuantizedScreenPoints.Count);
            foreach (Point pt in template.QuantizedScreenPoints)
            {
                if (pt.Y < _sDTM.RowDimension && pt.Y >= 0 && pt.X >= 0 && pt.X < _sDTM.ColumnDimension)
                {
                    double distan = _sDTM.GetElement(pt.Y, pt.X);
                    distances.Add(distan);
                }
            }

            distances.Sort();
            if (distances.Count == 0) return double.PositiveInfinity;

            return distances[(int)Math.Floor(((distances.Count - 1) * HAUSDORFF_QUANTILE))];
        }

        /// <summary>
        /// Calculates the Modified Hausdorff Distance (MHD) between the two ImageSymbols.
        /// MHD is defined as the average pixel distance between the images (max of 2 directed).
        /// </summary>
        /// <param name="template">ImageSymbol to compare 'this' to</param>
        /// <returns>Modified Hausdorff Distance</returns>
        private double ModifiedHausdorffDistance(ImageSymbol template)
        {
            double distan1 = 0.0;
            foreach (Point pt in _screenQuantizedPoints)
                if (pt.Y >= 0 && pt.X >= 0 
                    && pt.Y < template.DistanceTransformMatrixScreen.RowDimension 
                    && pt.X < template.DistanceTransformMatrixScreen.ColumnDimension)
                    distan1 += template.DistanceTransformMatrixScreen.GetElement(pt.Y, pt.X);

            double AB = distan1 / _screenQuantizedPoints.Count;

            double distan2 = 0.0;
            foreach (Point pt in template.QuantizedScreenPoints)
                if (pt.Y >= 0 && pt.X >= 0 && pt.Y < _sDTM.RowDimension && pt.X < _sDTM.ColumnDimension)
                    distan2 += _sDTM.GetElement(pt.Y, pt.X);

            double BA = distan2 / template.QuantizedScreenPoints.Count;

            return Math.Max(AB, BA);
        }

        /// <summary>
        /// Calculates the Tanimoto coefficient between the two ImageSymbols
        /// </summary>
        /// <param name="template">ImageSymbol to compare 'this' to</param>
        /// <returns>Tanimoto Coefficient</returns>
        private double TanimotoCoefficient(ImageSymbol template)
        {
            int a, b, c, d;
            a = b = c = d = 0;
            double E = 1.0 / 15.0 * Math.Sqrt(Math.Pow(GRID_SIZE, 2.0) + Math.Pow(GRID_SIZE, 2.0));

            for (int i = 0; i < _sDTM.ColumnDimension; i++)
            {
                for (int j = 0; j < _sDTM.RowDimension; j++)
                {
                    if (_sDTM.GetElement(i, j) < E)
                        a++;

                    if (template.DistanceTransformMatrixScreen.GetElement(i, j) < E)
                        b++;

                    if (_sDTM.GetElement(i, j) < E && template.DistanceTransformMatrixScreen.GetElement(i, j) < E)
                        c++;

                    if (_sDTM.GetElement(i, j) >= E && template.DistanceTransformMatrixScreen.GetElement(i, j) >= E)
                        d++;
                }
            }

            double T1 = (double)c / (double)(a + b - c);
            double T0 = (double)d / (double)(a + b - 2 * c + d);
            double p = (double)(a + b) / 2.0 / _sDTM.ColumnDimension / _sDTM.RowDimension;
            double alpha = (3.0 - p) / 4.0;

            return 1.0 - (alpha * T1 + (1.0 - alpha) * T0);
        }

        /// <summary>
        /// Calculates the Yule coefficient between the two ImageSymbols
        /// </summary>
        /// <param name="template">ImageSymbol to compare 'this' to</param>
        /// <returns>Yule Coefficient</returns>
        private double YuleCoefficient(ImageSymbol template)
        {
            int n10, n01, n11, n00;
            n10 = n01 = n11 = n00 = 0;
            double E = 1.0 / 15.0 * Math.Sqrt(Math.Pow(GRID_SIZE, 2.0) + Math.Pow(GRID_SIZE, 2.0));

            for (int i = 0; i < _sDTM.ColumnDimension; i++)
            {
                for (int j = 0; j < _sDTM.RowDimension; j++)
                {
                    if (_sDTM.GetElement(i, j) < E && template.DistanceTransformMatrixScreen.GetElement(i, j) >= E)
                        n10++;

                    if (_sDTM.GetElement(i, j) >= E && template.DistanceTransformMatrixScreen.GetElement(i, j) < E)
                        n01++;

                    if (_sDTM.GetElement(i, j) < E && template.DistanceTransformMatrixScreen.GetElement(i, j) < E)
                        n11++;

                    if (_sDTM.GetElement(i, j) >= E && template.DistanceTransformMatrixScreen.GetElement(i, j) >= E)
                        n00++;
                }
            }

            return 1.0 - (double)(n11 * n00 - n10 * n01) / (double)(n11 * n00 + n10 * n01);
        }

        #endregion
    }

    class BoundingPoints
    {
        #region Member Variables

        Point _topLeft;
        Point _topRight;
        Point _bottomLeft;
        Point _bottomRight;

        #endregion

        #region Constructors

        public BoundingPoints(Point[] points)
        {
            FindBoundaries(points);
        }

        public BoundingPoints(Rectangle bbox)
        {
            FindBoundaries(bbox);
        }

        #endregion

        private void FindBoundaries(Point[] points)
        {
            Rectangle bbox = Compute.BoundingBox(points);
            FindBoundaries(bbox);
        }

        private void FindBoundaries(Rectangle bbox)
        {
            _topLeft = new Point(bbox.Left, bbox.Top);
            _topRight = new Point(bbox.Right, bbox.Top);
            _bottomLeft = new Point(bbox.Left, bbox.Bottom);
            _bottomRight = new Point(bbox.Right, bbox.Bottom);
        }

        public double Height
        {
            get { return Compute.EuclideanDistance(_topLeft, _bottomLeft); }
        }

        public double Width
        {
            get { return Compute.EuclideanDistance(_topLeft, _topRight); }
        }

        public PointF Center
        {
            get
            {
                float x = (_topLeft.X + _bottomRight.X) / 2f;
                float y = (_topLeft.Y + _bottomRight.Y) / 2f;

                return new PointF(x, y);
            }
        }

    }

    [Serializable]
    [DebuggerDisplay("angPosition={_angPosition} relDistance={_relDistance}")]
    class PolarPoint : ICloneable
    {
        private double _angPosition;
        private double _relDistance;
        private Guid _id;

        public PolarPoint()
        {
            _id = Guid.NewGuid();
            _angPosition = 0.0;
            _relDistance = 0.0;
        }

        public PolarPoint(double angPosition, double relDistance)
        {
            _id = Guid.NewGuid();
            _angPosition = angPosition;
            _relDistance = relDistance;
        }

        public PolarPoint(PolarPoint pt)
        {
            PolarPoint temp = (PolarPoint)pt.Clone();
            this._id = temp._id;
            this._angPosition = temp._angPosition;
            this._relDistance = temp._relDistance;
        }

        public object Clone()
        {
            PolarPoint pt = (PolarPoint)this.MemberwiseClone();
            return pt;
        }

        public Guid Id
        {
            get { return _id; }
        }

        public double AngularPosition
        {
            get { return _angPosition; }
            set { _angPosition = value; }
        }

        public double RelativeDistance
        {
            get { return _relDistance; }
            set { _relDistance = value; }
        }
    }
}
