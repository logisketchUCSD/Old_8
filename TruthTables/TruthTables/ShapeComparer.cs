using System;
using System.Collections.Generic;
using System.Text;

namespace TruthTables
{
    /// <summary>
    /// compares shapes by their x-coordinate (sorts low to high)
    /// </summary>
    internal class ShapeComparerX : IComparer<Sketch.Shape>
    {
        public int Compare(Sketch.Shape s1, Sketch.Shape s2)
        {
            return (int)s1.XmlAttrs.X.Value - (int)s2.XmlAttrs.X.Value;
        }
    }

    /// <summary>
    /// compares shapes by their y-coordinate (sorts low to high)
    /// </summary>
    internal class ShapeComparerY : IComparer<Sketch.Shape>
    {
        public int Compare(Sketch.Shape s1, Sketch.Shape s2)
        {
            return (int)s1.XmlAttrs.Y.Value - (int)s2.XmlAttrs.Y.Value;
        }
    }
    
    /// <summary>
    /// compares substrokes by their height (sorts high to low)
    /// </summary>
    internal class ShapeComparerHeight : IComparer<Sketch.Shape>
    {
        public int Compare(Sketch.Shape s1, Sketch.Shape s2)
        {
            return (int)s2.XmlAttrs.Height.Value - (int)s1.XmlAttrs.Height.Value;
        }
    }

    /// <summary>
    /// compares substrokes by their width (sorts high to low)
    /// </summary>
    internal class ShapeComparerWidth : IComparer<Sketch.Shape>
    {
        public int Compare(Sketch.Shape s1, Sketch.Shape s2)
        {
            return (int)s2.XmlAttrs.Width.Value - (int)s1.XmlAttrs.Width.Value;
        }
    }
}
