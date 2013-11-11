using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ZernikeMomentRecognizer
{
    public class ZernikeMoment
    {
        string m_Name;
        double A20, A22, A31, A33, A40, A42, A44, A51, A53, A55, A60, A62, A64, A66;
        double A71, A73, A75, A77, A80, A82, A84, A86, A88, A91, A93, A95, A97, A99;
        double A100, A102, A104, A106, A108, A1010, A00, A11;

        public ZernikeMoment(string name, Point[] strokePoints)
        {
            m_Name = name;

            List<PointF> polarPoints = GetNormalizedPolarPoints(strokePoints);

            CalculateAndAssignZernikeValues(polarPoints);
        }

        private void CalculateAndAssignZernikeValues(List<PointF> polarPoints)
        {
            A00 = Zernicke(0, 0, polarPoints);
            A11 = Zernicke(1, 1, polarPoints);
            A20 = Zernicke(2, 0, polarPoints);
            A22 = Zernicke(2, 2, polarPoints);
            A31 = Zernicke(3, 1, polarPoints);
            A33 = Zernicke(3, 3, polarPoints);
            A40 = Zernicke(4, 0, polarPoints);
            A42 = Zernicke(4, 2, polarPoints);
            A44 = Zernicke(4, 4, polarPoints);
            A51 = Zernicke(5, 1, polarPoints);
            A53 = Zernicke(5, 3, polarPoints);
            A55 = Zernicke(5, 5, polarPoints);
            A60 = Zernicke(6, 0, polarPoints);
            A62 = Zernicke(6, 2, polarPoints);
            A64 = Zernicke(6, 4, polarPoints);
            A66 = Zernicke(6, 6, polarPoints);
            A71 = Zernicke(7, 1, polarPoints);
            A73 = Zernicke(7, 3, polarPoints);
            A75 = Zernicke(7, 5, polarPoints);
            A77 = Zernicke(7, 7, polarPoints);
            A80 = Zernicke(8, 0, polarPoints);
            A82 = Zernicke(8, 2, polarPoints);
            A84 = Zernicke(8, 4, polarPoints);
            A86 = Zernicke(8, 6, polarPoints);
            A88 = Zernicke(8, 8, polarPoints);
            A91 = Zernicke(9, 1, polarPoints);
            A93 = Zernicke(9, 3, polarPoints);
            A95 = Zernicke(9, 5, polarPoints);
            A97 = Zernicke(9, 7, polarPoints);
            A99 = Zernicke(9, 9, polarPoints);
            A100 = Zernicke(10, 0, polarPoints);
            A102 = Zernicke(10, 2, polarPoints);
            A104 = Zernicke(10, 4, polarPoints);
            A106 = Zernicke(10, 6, polarPoints);
            A108 = Zernicke(10, 8, polarPoints);
            A1010 = Zernicke(10, 10, polarPoints);
        }

        private List<PointF> GetNormalizedPolarPoints(Point[] strokePoints)
        {
            List<Point> RastPoints = RasterizePoints(strokePoints, 48);

            int sumX = 0;
            int sumY = 0;
            foreach (Point pt in RastPoints)
            {
                sumX += pt.X;
                sumY += pt.Y;
            }

            float xBar = (float)sumX / (float)strokePoints.Length;
            float yBar = (float)sumY / (float)strokePoints.Length;

            List<PointF> movedPoints = new List<PointF>(strokePoints.Length);
            foreach (Point pt in strokePoints)
                movedPoints.Add(new PointF((float)pt.X - xBar, (float)pt.Y - yBar));

            double rMax = 0.0;
            foreach (PointF pt in movedPoints)
                rMax = Math.Max(rMax, Radius(pt));

            List<PointF> polarPoints = new List<PointF>(movedPoints.Count);
            foreach (PointF pt in movedPoints)
            {
                double r = Radius(pt) / rMax;
                double theta = Math.Atan2(pt.Y, pt.X);
                if (theta < 0.0)
                    theta += 2 * Math.PI;
                polarPoints.Add(new PointF((float)r, (float)theta));
            }

            return polarPoints;
        }

        private List<Point> RasterizePoints(Point[] strokePoints, int length)
        {
            double diagLength = Math.Sqrt(Math.Pow((double)(length - 1), 2.0));
            List<Point> points = new List<Point>(strokePoints.Length);
            Rectangle box = BoundingBox(strokePoints);
            int maxSide = box.Width;
            if (box.Height > maxSide)
                maxSide = box.Height;
            double boxDiag = Math.Sqrt(Math.Pow((double)maxSide, 2.0));
            double ratio = diagLength / boxDiag;
            Utilities.Matrix.GeneralMatrix matrix = new Utilities.Matrix.GeneralMatrix(length, length, 0.0);

            foreach (Point pt in strokePoints)
            {
                int x = (int)Math.Floor((pt.X - box.Left) * ratio);
                int y = (int)Math.Floor((pt.Y - box.Top) * ratio);
                matrix.SetElement(x, y, 1.0);
            }

            for (int i = 0; i < matrix.RowDimension; i++)
                for (int j = 0; j < matrix.ColumnDimension; j++)
                    if (matrix.GetElement(i, j) > 0.0)
                        points.Add(new Point(i, j));

            return points;
        }

        /// <summary>
        /// Finds the bounding box for a list of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        Rectangle BoundingBox(Point[] points)
        {
            int[] MinMax = ComputeMinMax(points);

            int width = MinMax[1] - MinMax[0];
            int height = MinMax[3] - MinMax[2];

            return new Rectangle(MinMax[0], MinMax[2], width, height);
        }

        /// <summary>
        /// { minX, maxX, minY, maxY }
        /// Determines the minimum and maximum values of X and Y for a set of points
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        int[] ComputeMinMax(Point[] points)
        {
            if (points.Length == 0)
                return new int[] { 0, 0, 0, 0 };

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                minX = Math.Min(minX, points[i].X);
                maxX = Math.Max(maxX, points[i].X);
                minY = Math.Min(minY, points[i].Y);
                maxY = Math.Max(maxY, points[i].Y);
            }

            return new int[] { minX, maxX, minY, maxY };
        }

        private double Radius(PointF pt)
        {
            return Math.Sqrt(Math.Pow(pt.X, 2.0) + Math.Pow(pt.Y, 2.0));
        }

        private double Zernicke(int n, int m, List<PointF> pointsPolar)
        {
            double sum = 0.0;
            foreach (PointF pt in pointsPolar)
                sum += BasisFunction(n, m, pt.X, pt.Y);

            sum *= (n + 1) / Math.PI;
            return Math.Abs(sum);
        }

        private double BasisFunction(int n, int m, double rho, double theta)
        {
            double R = RadialPolynomial(n, m, rho);
            double e = 1.0;// Math.Exp(m * theta);

            return R * e;
        }

        private double RadialPolynomial(int n, int m, double rho)
        {
            double sum = 0.0;
            int k = Math.Abs(m);

            for (int i = k; i <= n; i++)
            {
                if ((n - k) % 2 == 0)
                {
                    double a = Math.Pow(-1.0, (double)(n - k) / 2.0);
                    double b = factorial((n + k) / 2);
                    double c = factorial((n - k) / 2);
                    double d = factorial((k + m) / 2);
                    double e = factorial((k - m) / 2);
                    double f = Math.Pow(rho, (double)k);
                    double value = ((a * b) / (c * d * e)) * f;
                    sum += value;
                }
            }

            return sum;
        }

        private double factorial(int n)
        {
            if (n < 0)
                return 0.0;
            else if (n == 0)
                return 1.0;
            else
                return (double)n * factorial(n - 1);

        }

        public void Print(System.IO.StreamWriter writer)
        {
            writer.Write(A00.ToString() + ",");
            writer.Write(A11.ToString() + ",");
            writer.Write(A20.ToString() + ",");
            writer.Write(A22.ToString() + ",");
            writer.Write(A31.ToString() + ",");
            writer.Write(A33.ToString() + ",");
            writer.Write(A40.ToString() + ",");
            writer.Write(A42.ToString() + ",");
            writer.Write(A44.ToString() + ",");
            writer.Write(A51.ToString() + ",");
            writer.Write(A53.ToString() + ",");
            writer.Write(A55.ToString() + ",");
            writer.Write(A60.ToString() + ",");
            writer.Write(A62.ToString() + ",");
            writer.Write(A64.ToString() + ",");
            writer.Write(A66.ToString() + ",");
            writer.Write(A71.ToString() + ",");
            writer.Write(A73.ToString() + ",");
            writer.Write(A75.ToString() + ",");
            writer.Write(A77.ToString() + ",");
            writer.Write(A80.ToString() + ",");
            writer.Write(A82.ToString() + ",");
            writer.Write(A84.ToString() + ",");
            writer.Write(A86.ToString() + ",");
            writer.Write(A88.ToString() + ",");
            writer.Write(A91.ToString() + ",");
            writer.Write(A93.ToString() + ",");
            writer.Write(A95.ToString() + ",");
            writer.Write(A97.ToString() + ",");
            writer.Write(A99.ToString() + ",");
            writer.Write(A100.ToString() + ",");
            writer.Write(A102.ToString() + ",");
            writer.Write(A104.ToString() + ",");
            writer.Write(A106.ToString() + ",");
            writer.Write(A108.ToString() + ",");
            writer.Write(A1010.ToString() + ",");
            writer.WriteLine(m_Name);
        }
    }

}
