using Sketch;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrokeScanner
{
    class Scanner
    {
        /// <summary>
        /// The scanner box that overlaps with this group of substrokes
        /// </summary>
        private Rect m_scanBox;

        /// <summary>
        /// The substrokes that this scan box overlaps with
        /// </summary>
        private List<Substroke> m_scanGroup;

        /// <summary>
        /// The restricted version of scanGroup containing only the points that are within scanBox
        /// </summary>
        private List<Substroke> m_scanWindow;

        /// <summary>
        /// Construct knowing only the scan box and add substrokes in the scan group later
        /// </summary>
        /// <param name="scanBox"></param>
        public Scanner(Rect scanBox)
        {
            m_scanBox = scanBox.Clone();
            m_scanGroup = new List<Substroke>();
            m_scanWindow = new List<Substroke>();
        }

        /// <summary>
        /// Construct the scan window with its scan box and the group of strokes that have already been intersected with that box
        /// </summary>
        /// <param name="scanBox"></param>
        /// <param name="scanGroup"></param>
        public Scanner(Rect scanBox, List<Substroke> scanGroup)
        {
            m_scanBox = scanBox.Clone();
            m_scanGroup = new List<Substroke>(scanGroup);

            m_scanWindow = new List<Substroke>();
            for (int ss = 0; ss < m_scanGroup.Count; ++ss)
            {
                m_scanWindow.Add(window(m_scanGroup[ss]));
            }
        }

        /// <summary>
        /// This method takes a substroke and tries to add it to the ScanWindow.  If the substroke's bounding box does not overlap
        /// with that of the scan window then the substroke will not be added.
        /// FIXME: HACK!!! Passing in the bounding box like this is a problem...
        /// </summary>
        /// <param name="s">Substroke trying to be added</param>
        /// <param name="boundingBox">The bounding box of substroke s</param>
        /// <returns>Returns true if s was actually added and false if not (if s does not overlap with the scan box).</returns>
        public bool testAndAdd(Substroke s, Rect boundingBox)
        {
            if (Rect.overlap(m_scanBox, boundingBox))
            {
                m_scanGroup.Add(s);
                m_scanWindow.Add(window(s));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Return a new substroke that is the old substroke minus the points that are not in the scanBox
        /// </summary>
        /// <param name="fullSubstroke"></param>
        /// <returns></returns>
        private Substroke window(Substroke fullSubstroke)
        {
            List<Sketch.Point> fullPoints = fullSubstroke.PointsL;
            Substroke windowSubstroke = fullSubstroke.Clone();            
            windowSubstroke.DeleteRange(0, fullPoints.Count);

            for (int p = 0; p < fullPoints.Count; ++p)
            {
                if (m_scanBox.contains(fullPoints[p]))
                {
                    windowSubstroke.AddPoint(fullPoints[p]);
                }
            }

            return windowSubstroke;
        }


        public Rect ScanBox
        {
            get
            {
                return m_scanBox;
            }
        }

        public List<Substroke> ScanGroup
        {
            get
            {
                return m_scanGroup;
            }
        }

        public List<Substroke> ScanWindow
        {
            get
            {
                return m_scanWindow;
            }
        }       
    }
}
