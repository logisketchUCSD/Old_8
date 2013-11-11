using System;
using System.Collections.Generic;
using System.Text;

using Set;
using Sketch;
using Svm;
using libsvm;

namespace StrokeScanner
{
    public class Scanner
    {
        /// <summary>
        /// The scanner box that overlaps with this group of substrokes
        /// </summary>
        private Rect m_scanBox;

        /// <summary>
        /// The substrokes that this scan box overlaps with
        /// </summary>
        private Set.HashSet<Substroke> m_scanGroup;

        /// <summary>
        /// The restricted version of scanGroup containing only the points that are within scanBox.  
        /// Will lose strokes that have no points which overlap the scan box.
        /// </summary>
        private List<Substroke> m_scanWindow;

        private int m_clusterLabel;

        private int NUM_FEATURES = SiteFeatures.NUM_FEATURES + InteractionFeatures.NUM_FEATURES + 1;
        
        /// <summary>
        /// Construct knowing only the scan box and add substrokes in the scan group later
        /// </summary>
        /// <param name="scanBox"></param>
        public Scanner(Rect scanBox)
        {
            m_scanBox = scanBox.Clone();
            m_scanGroup = new Set.HashSet<Substroke>();
            m_scanWindow = new List<Substroke>();
        }

        /// <summary>
        /// Construct the scan window with its scan box and the group of strokes 
        /// that have already been intersected with that box
        /// </summary>
        /// <param name="scanBox"></param>
        /// <param name="scanGroup"></param>
        public Scanner(Rect scanBox, Set.HashSet<Substroke> scanGroup)
        {
            m_scanBox = scanBox.Clone();
            m_scanGroup = new Set.HashSet<Substroke>(scanGroup);

            m_scanWindow = new List<Substroke>();
            foreach (Substroke ss in scanGroup)
            {
                Substroke ws = window(ss);
                if (ws.PointsL.Count > 0)
                {
                    m_scanWindow.Add(ws);
                }   
            }
        }

        /// <summary>
        /// This method takes a substroke and tries to add it to the ScanWindow.  If the substroke
        /// does not contain any points in the ScanWindow, it is not added.
        /// </summary>
        /// <param name="s">Substroke trying to be added</param>        
        /// <returns>Returns true if s was actually added and false if not 
        /// (if s does not overlap with the scan box).</returns>
        public bool testAndAdd(Substroke s)
        {
            Substroke ws = window(s);

            if (ws.PointsL.Count >= 1)
            {
                m_scanGroup.Add(s);
                m_scanWindow.Add(ws);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return a new substroke that is the old substroke minus the points that are not in the scanBox.   
        /// 
        /// A modification of this method will allow us to only keep strokes that have some ink in the scanbox, but
        /// we should keep *all* of the strokes that meet that condition.
        /// </summary>
        /// <param name="fullSubstroke"></param>
        /// <returns></returns>
        private Substroke window(Substroke fullSubstroke)
        {
            List<Sketch.Point> fullPoints = fullSubstroke.PointsL;
            List<Sketch.Point> newPoints = new List<Point>();

            for (int p = 0; p < fullPoints.Count; ++p)
            {
                if (m_scanBox.contains(fullPoints[p]) && !newPoints.Contains(fullPoints[p]))
                {
                    newPoints.Add(fullPoints[p]);
                }
            }

            Substroke windowSubstroke = 
                new Substroke(newPoints, new XmlStructs.XmlShapeAttrs(fullSubstroke.XmlAttrs));
            windowSubstroke.ParentShapes = new List<Shape>(fullSubstroke.ParentShapes);

            windowSubstroke.XmlAttrs.Id = Guid.NewGuid();

            return windowSubstroke;
        }

        #region Getters
        public Rect ScanBox
        {
            get
            {
                return m_scanBox;
            }
        }

        public Set.HashSet<Substroke> ScanGroup
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

        /// <summary>
        /// Compute the feature vector for the scanner box by averaging 
        /// all the feature vectors for all the individual
        /// strokes (restricted to the area of the scanner box)
        /// </summary>
        public List<double> FeatureVector
        {
            get
            {
                List<double> avgFeatures = new List<double>(NUM_FEATURES);
                for (int i = 0; i < NUM_FEATURES; ++i)
                {
                    avgFeatures.Add(0);
                }
                
                List<double> tmp;

                foreach (Substroke s in m_scanWindow)
                {
                    tmp = SiteFeatures.evalStroke(s);                    

                    for (int i = 0; i < SiteFeatures.NUM_FEATURES; ++i) //+InteractionFeatures
                    {
                        avgFeatures[i] += tmp[i];
                        //avgFeatures[i] = Math.Min(avgFeatures[i], tmp[i]);
                        //avgFeatures[i] = Math.Max(avgFeatures[i], tmp[i]);
                    }
                }

                for (int i = 0; i < m_scanWindow.Count; ++i)
                {
                    for (int j = i + 1; j < m_scanWindow.Count; ++j)
                    {
                        tmp = InteractionFeatures.evalStroke(m_scanWindow[i], m_scanWindow[j]);

                        // Modifying the second part of the feature vector
                        for (int f = 0; f < InteractionFeatures.NUM_FEATURES; ++f)
                        {
                            avgFeatures[f+SiteFeatures.NUM_FEATURES] += tmp[f];
                            //avgFeatures[f] = Math.Min(avgFeatures[f], tmp[f - SiteFeatures.NUM_FEATURES]);
                            //avgFeatures[f] = Math.Max(avgFeatures[f], tmp[f - SiteFeatures.NUM_FEATURES]);
                        }
                    }
                }

                for (int i = 0; i < SiteFeatures.NUM_FEATURES; ++i)
                {
                    avgFeatures[i] /= m_scanWindow.Count;
                }

                for (int i = 0; i < InteractionFeatures.NUM_FEATURES; ++i)
                {
                    avgFeatures[i+SiteFeatures.NUM_FEATURES] /= 
                        (m_scanWindow.Count * (m_scanWindow.Count - 1)) / 2;
                }

                avgFeatures[SiteFeatures.NUM_FEATURES + InteractionFeatures.NUM_FEATURES] =
                    edgeIntersections();

                return avgFeatures;
            }
        }

        /// <summary>
        /// Count the number of edges through which strokes pass
        /// </summary>
        /// <returns></returns>
        private int edgeIntersections()
        {
            bool top = false, bot = false, l = false, r = false;

            foreach (Substroke s in m_scanGroup)
            {
                for ( int i = 0; i < s.PointsL.Count - 1; i++ )
                {
                    Point pi = s.PointsL[i];
                    Point pi1 = s.PointsL[i+1];
                    bool c1 = m_scanBox.contains(pi);
                    bool c2 = m_scanBox.contains(pi1);

                    if (c1 && !c2)
                    {
                        if (pi1.X > m_scanBox.botRightX) r = true;
                        if (pi1.X < m_scanBox.topLeftX) l = true;

                        if (pi1.Y > m_scanBox.botRightY) bot = true;
                        if (pi1.Y < m_scanBox.topLeftY) top = true;
                    }

                    if (c2 && !c1)
                    {
                        if (pi.X > m_scanBox.botRightX) r = true;
                        if (pi.X < m_scanBox.topLeftX) l = true;

                        if (pi.Y > m_scanBox.botRightY) bot = true;
                        if (pi.Y < m_scanBox.topLeftY) top = true;
                    }
                }
            }

            int a=0, _=0, c=0, d=0;
            if (top) a = 1;
            if (bot) _ = 1;
            if (l) c = 1;
            if (r) d = 1;
            int res = a + _ + c + d;
            return res;
        }

        /// <summary>
        /// Get the number of edge intersections for a single stroke
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public int edgeIntersections(Substroke s)
        {
            bool top = false, bot = false, l = false, r = false;
            for (int i = 0; i < s.PointsL.Count - 1; i++)
            {
                Point pi = s.PointsL[i];
                Point pi1 = s.PointsL[i + 1];
                bool c1 = m_scanBox.contains(pi);
                bool c2 = m_scanBox.contains(pi1);

                if (c1 && !c2)
                {
                    if (pi1.X > m_scanBox.botRightX) r = true;
                    if (pi1.X < m_scanBox.topLeftX) l = true;

                    if (pi1.Y > m_scanBox.botRightY) bot = true;
                    if (pi1.Y < m_scanBox.topLeftY) top = true;
                }

                if (c2 && !c1)
                {
                    if (pi.X > m_scanBox.botRightX) r = true;
                    if (pi.X < m_scanBox.topLeftX) l = true;

                    if (pi.Y > m_scanBox.botRightY) bot = true;
                    if (pi.Y < m_scanBox.topLeftY) top = true;
                }
            }

            int a = 0, _ = 0, c = 0, d = 0;
            if (top) a = 1;
            if (bot) _ = 1;
            if (l) c = 1;
            if (r) d = 1;
            int res = a + _ + c + d;
            return res;
        }

        public Substroke tJunction()
        {
            List<Substroke> ls = m_scanGroup.AsList();

            bool top, bot, l, r;
            bool top2, bot2, l2, r2;

            whichEdgeIntersections(ls[0], out top, out bot, out l, out r);
            whichEdgeIntersections(ls[1], out top2, out bot2, out l2, out r2);

            if (top && bot)
            {
                if (!l && !r && !top2 && !bot2 && (l2 || r2) && !(l2 && r2))
                {
                    return ls[0];
                }
            }

            if (l && r)
            {
                if (!top && !bot && !l2 && !r2 && (top2 || bot2) && !(top2 && bot2))
                {
                    return ls[0];
                }
            }

            if (top2 && bot2)
            {
                if (!l2 && !r2 && !top && !bot && (l || r) && !(l && r))
                {
                    return ls[1];
                }
            }

            if (l2 && r2)
            {
                if (!top2 && !bot2 && !l && !r && (top || bot) && !(top && bot))
                {
                    return ls[1];
                }
            }

            return null;
        }

        private void whichEdgeIntersections
            (Substroke s, out bool top, out bool bot, out bool l, out bool r)
        {
            top = bot = l = r = false;
            for (int i = 0; i < s.PointsL.Count - 1; i++)
            {
                Point pi = s.PointsL[i];
                Point pi1 = s.PointsL[i + 1];
                bool c1 = m_scanBox.contains(pi);
                bool c2 = m_scanBox.contains(pi1);

                if (c1 && !c2)
                {
                    if (pi1.X > m_scanBox.botRightX) r = true;
                    if (pi1.X < m_scanBox.topLeftX) l = true;

                    if (pi1.Y > m_scanBox.botRightY) bot = true;
                    if (pi1.Y < m_scanBox.topLeftY) top = true;
                }

                if (c2 && !c1)
                {
                    if (pi.X > m_scanBox.botRightX) r = true;
                    if (pi.X < m_scanBox.topLeftX) l = true;

                    if (pi.Y > m_scanBox.botRightY) bot = true;
                    if (pi.Y < m_scanBox.topLeftY) top = true;
                }
            }
        }

        /// <summary>
        /// Get the number of intersections of contained strokes with edges of the scanner
        /// </summary>
        public int EdgeIntersections
        {
            get
            {
                return edgeIntersections();
            }
        }

        public bool between(double mid, double side, double otherside)
        {
            return (mid <= Math.Max(side, otherside) && mid >= Math.Max(side, otherside));
        }

        /// <summary>
        /// Label of 1 means that the window strokes actually contain a transition 
        /// (strokes with more than one distinct label)
        /// Label of 0 means that the windwo strokes do not contain a transition.
        /// </summary>
        public int Label
        {
            get
            {
                if(m_scanWindow.Count < 2)
                {
                    return 0;
                }

                Shape theshape = null;
                foreach (Substroke s in m_scanGroup)
                {
                    if (theshape == null)
                        theshape = s.ParentShapes[0];
                    else
                        return (theshape == s.ParentShapes[0]) ? 1 : 0;
                }
                return 0;
            }
        }

        /// <summary>
        /// We want to discard scan windows that do not have at least two strokes in the window,
        /// as these windows cannot contain a transition.
        /// </summary>
        public bool PotentialTransition
        {
            get
            {
                if (m_scanWindow.Count > 1)
                {
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Store the label of the cluster that this scanner belongs to.
        /// </summary>
        public int ClusterLabel
        {
            get
            {
                return m_clusterLabel;
            }

            set
            {
                m_clusterLabel = value;
            }
        }

        #endregion

            #region Utilities

        /// <summary>
        /// Creates an svm_node array out of the feature vector for this scanbox.
        /// </summary>
        /// <returns></returns>
        public svm_node[] SvmNodes()
        {
            svm_node[] arr = new svm_node[NUM_FEATURES];

            List<double> feats = FeatureVector;

            for (int i = 0; i < feats.Count; i++)
            {
                arr[i] = new svm_node();
                arr[i].index = i;
                arr[i].value_Renamed = feats[i];
            }

            return arr;
        }

        /// <summary>
        /// Calculate bounding box of the substroke s (the smallest rectangle containing all of the
        /// points in s).
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static Rect boundingBox(Substroke s)
        {
            List<Sketch.Point> pts = s.PointsL;
            return boundingBox(pts);
        }

        /// <summary>
        /// Calculate the bounding box of a set of points (the smallest rectangle containing all of the points)
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private static Rect boundingBox(List<Sketch.Point> pts)
        {
            double topLeftX = double.PositiveInfinity;  // Leftmost (minimum) x value
            double topLeftY = double.PositiveInfinity;  // Topmost (minimum) y value
            double botRightX = double.NegativeInfinity; // Rightmost (maximum) x value
            double botRightY = double.NegativeInfinity; // Bottommost (maximum) y value

            for (int p = 0; p < pts.Count; ++p)
            {
                if (pts[p].X < topLeftX)
                {
                    topLeftX = pts[p].X;
                }
                if (pts[p].Y < topLeftY)
                {
                    topLeftY = pts[p].Y;
                }
                if (pts[p].X > botRightX)
                {
                    botRightX = pts[p].X;
                }
                if (pts[p].Y > botRightY)
                {
                    botRightY = pts[p].Y;
                }
            }
            return new Rect(topLeftX, topLeftY, botRightX, botRightY);
        }

        /// <summary>
        /// Add this scanbox to a sketch.
        /// </summary>
        /// <param name="sketch">sketch to add to</param>
        public void addToSketch(ref Sketch.Sketch sketch)
        {
            ScanBox.addToSketch(ref sketch, ClusterLabel);
        }
                                
                    
        #endregion

    }
}
