using Sketch;

using System;
using System.Collections.Generic;
using System.Text;


namespace StrokeScanner
{
    /// <summary>
    /// A class that defines a rectangle for our bounding boxes.
    /// </summary>
    public class Rect : IComparable<Rect>
    {
        public double topLeftX;
        public double topLeftY;
        public double botRightX;
        public double botRightY;

        public Rect(double topLeftX, double topLeftY, double botRightX, double botRightY)
        {
            this.topLeftX = topLeftX;
            this.topLeftY = topLeftY;
            this.botRightX = botRightX;
            this.botRightY = botRightY;
        }

        public Rect Clone()
        {
            return new Rect(topLeftX, topLeftY, botRightX, botRightY);
        }

        /// <summary>
        /// Get the area of this rectangle
        /// </summary>
        /// <returns></returns>
        public double area()
        {
            return (botRightX - topLeftX) * (botRightY - topLeftY);
        }

        public double height()
        {
            return botRightY - topLeftY;
        }

        public double width()
        {
            return botRightX - topLeftX;
        }

        public Sketch.Point centerPoint()
        {
            Sketch.Point center = new Sketch.Point();
            center.XmlAttrs.X = (float)(topLeftX + (width() / 2));
            center.XmlAttrs.Y = (float)(topLeftY + (height() / 2));
            return center;
        }

        public List<double> centerPointL()
        {
            List<double> center = new List<double>(2);
            center.Add(0); center.Add(0);  // Know it is 2-D
            center[0] = topLeftX + (width() / 2);
            center[1] = topLeftY + (height() / 2);
            return center;
        }

        /// <summary>
        /// This method actually alters the Rect by scaling it around its center point
        /// </summary>
        /// <param name="xScale"></param>
        /// <param name="yScale"></param>
        public void scale(double xScale, double yScale)
        {
            Sketch.Point center = centerPoint();
            double newWidth = xScale * width();
            double newHeight = yScale * height();

            topLeftX = center.X - (newWidth / 2);
            botRightX = center.X + (newWidth / 2);

            topLeftY = center.Y - (newHeight / 2);
            botRightY = center.Y + (newHeight / 2);
        }

        public void addToSketch(ref Sketch.Sketch sketch)
        {
            addToSketch(ref sketch, -1);
        }

        /// <summary>
        /// This method proviedes an example of the horrible hackery necessary to add anything to a Sketch.Sketch
        /// </summary>
        /// <param name="sketch"></param>
        /// <param name="label"></param>
        public void addToSketch(ref Sketch.Sketch sketch, int label)
        {    
            // Set up corner points
            List<Sketch.Point> pts = new List<Sketch.Point>();
            pts.Add(makeCornerPoint(topLeftX, topLeftY)); // Top left point
            pts.Add(makeCornerPoint(topLeftX, botRightY)); // Bottom left point
            pts.Add(makeCornerPoint(botRightX, botRightY)); // Bottom right point
            pts.Add(makeCornerPoint(botRightX, topLeftY)); // Top right point
            pts.Add(makeCornerPoint(topLeftX, topLeftY)); // Top left point to bring stroke back around

            Sketch.Substroke ss = new Sketch.Substroke(pts, new XmlStructs.XmlShapeAttrs());
            ss.XmlAttrs.Type = "substroke";
            ss.XmlAttrs.Name = "substroke";
            ss.XmlAttrs.Id = Guid.NewGuid();
            ss.XmlAttrs.Time = (ulong)DateTime.Now.Ticks;

            Sketch.Stroke s = new Sketch.Stroke(ss);
            s.XmlAttrs.Type = "stroke";
            s.XmlAttrs.Name = "stroke";
            s.XmlAttrs.Id = Guid.NewGuid();
            s.XmlAttrs.Time = (ulong)DateTime.Now.Ticks;
            sketch.AddStroke(s);

            string labelName;
            if (label == -1)
            {
                labelName = "Background";
            }
            else
            {
                labelName = String.Format("Group{0}", label);
            }

            Sketch.Shape sh = new Sketch.Shape();
            sh.XmlAttrs.Type = labelName;
            sh.XmlAttrs.Name = "shape";
            sh.XmlAttrs.Id = Guid.NewGuid();
            sh.XmlAttrs.Time = (ulong)DateTime.Now.Ticks;
            sh.AddSubstroke(ss);
            sketch.AddShape(sh);
        }

        private Sketch.Point makeCornerPoint(double x, double y)
        {
            Sketch.Point p = new Sketch.Point();
            p.XmlAttrs.Time = (ulong)DateTime.Now.Ticks;
            p.XmlAttrs.X = (float)x;
            p.XmlAttrs.Y = (float)y;
            p.XmlAttrs.Pressure = 127;
            p.XmlAttrs.Name = "point";
            p.XmlAttrs.Id = Guid.NewGuid();
            return p;
        }

        /// <summary>
        /// Detect if the rectangles r and s overlap at all.
        /// Works by finding out if r and s do not overlap, then taking the logical not.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="s"></param>
        public static bool overlap(Rect r, Rect s)
        {
            // Note that this uses the assumption that the origin of the coordinate
            // system is in the upper left corner (so we work in quadrant IV).

            // The two rectangles *do not* intersect if
            // r.botRightX is left of s.topLeftX (<=)
            // r.topLeftX is right of s.botRightX (>=)
            // r.botRightY is above s.topLeftY (<=)
            // r.topLeftY is below s.botRightY (>=)
            if (!((r.botRightX <= s.topLeftX) || (r.topLeftX >= s.botRightX) ||
                 (r.botRightY <= s.topLeftY) || (r.topLeftY >= s.botRightY)))
            {
                return true;  // There is overlap
            }

            return false;
        }

        /// <summary>
        /// Return true if point p is within this rectangle
        /// </summary>
        /// <param name="p">Point to check if it is contained in this rectangle</param>
        /// <returns></returns>
        public bool contains(Sketch.Point p)
        {
            return ((p.X >= topLeftX) && (p.Y >= topLeftY) && (p.X <= botRightX) && (p.Y <= botRightY));
        }

        public override string ToString()
        {
            return String.Format("Top-left point is {0},{1}.  Bottom-right point is {2},{3}.", topLeftX, topLeftY, botRightX, botRightY);
        }

        /// <summary>
        /// < 0 means that this object is less than other
        /// 0 means that both objects are equal
        /// > 0 means that this object is greater than other
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Rect other)
        {
            if (topLeftX == other.topLeftX && topLeftY == other.topLeftY &&
                botRightX == other.botRightX && botRightY == other.botRightY)
                return 0;

            return (int)Math.Round(area() - other.area(), MidpointRounding.AwayFromZero);
        }

        public override bool Equals(Object other)
        {
            return (CompareTo((Rect)other) == 0);
        }

        public override int GetHashCode()
        {
            return (int)(topLeftX * topLeftY * botRightX * botRightY);
        }

        public static Rect operator +(Rect one, Rect other)
        {
            return new Rect(one.topLeftX + other.topLeftX, one.topLeftY + other.topLeftY,
                one.botRightX + other.botRightX, one.botRightY + other.botRightY);
        }

        public static Rect operator /(Rect one, double d)
        {
            return new Rect(one.topLeftX/d, one.topLeftY/d, one.botRightX/d, one.botRightY/d);
        }

        public static Rect operator *(Rect one, double d)
        {
            return new Rect(one.topLeftX*d, one.topLeftY*d, one.botRightX*d, one.botRightY*d);
        }
    }
}
