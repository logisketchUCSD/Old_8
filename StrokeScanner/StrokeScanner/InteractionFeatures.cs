using System;
using System.Collections.Generic;
using System.Text;
using Sketch;
using Featurefy;

namespace StrokeScanner
{
    class InteractionFeatures
    {
        public const int NUM_FEATURES = 6;

        public static List<double> evalStroke(Substroke s, Substroke t)
        {
            List<double> result = new List<double>(NUM_FEATURES);

            result.Add(minDistBetweenFrag(s, t));
            result.Add(minDistBetweenEnds(s, t));            
            result.Add(minEndToLineDistance(s, t));
            result.Add(minEndToLineDistance(t, s));
            result.Add(minTimeBetweenSubstrokes(s, t));
            result.Add(maxDistBetweenFrag(s, t));

            for (int i = 0; i < NUM_FEATURES; ++i)
            {
                if ((double.IsInfinity(result[i])) || (double.IsNaN(result[i])))
                {
                    Console.WriteLine("BAD FEATURE: {0}", i);
                }
            }

            return result;
        }


        #region Interaction Features

        /// <summary>
        /// Gives the minimum euclidean distance between two substrokes
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns>Minimum distance between two substrokes</returns>
        private static double minDistBetweenFrag(Substroke s, Substroke t)
        {
            double minDist = double.PositiveInfinity;
            
            for (int i = 0; i < s.Points.Length; i++)
            {
                for (int j = 0; j < t.Points.Length; j++)
                {
                    double tempDist = StrokeScanner.euclideanDistance(s.Points[i], t.Points[j]);                    

                    if (tempDist < minDist)
                    {
                        minDist = tempDist;
                    }
                }
            }

            return minDist;
        }


        /// <summary>
        /// Minimum distance between endpoints
        /// Finds the shortest distance between two endpoints of the two substrokes
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns>Minimum distance between two end points of substrokes</returns>
        private static double minDistBetweenEnds(Substroke s, Substroke t)
        {
            double minDist = double.PositiveInfinity;
            double tempDist;
            int index1;
            int index2;

            for (int i = 0; i <= 1; i++)
            {
                // Index for the first or last point
                index1 = (s.Points.Length * i) - i;

                for (int j = 0; j <= 1; j++)
                {
                    index2 = (t.Points.Length * j) - j;

                    // Grab the distance between the specified endpoints
                    tempDist = StrokeScanner.euclideanDistance(s.Points[index1], t.Points[index2]);                    

                    // If it's the min, set it so
                    if (tempDist < minDist)
                    {
                        minDist = tempDist;
                    }
                }
            }
            return minDist;
        }


        /// <summary>
        /// Finds the minimum distance from the end points of one line to the body of another
        /// </summary>       
        /// <param name="ends">Measure from the ends of this substroke</param>
        /// <param name="line">Measure from the body (anywhere) on this substroke</param>
        /// <returns>minimum euclidean distance</returns>
        private static double minEndToLineDistance(Substroke ends, Substroke line)
        {
            double minDist = double.PositiveInfinity;
            double tempDist;
            int endIndex;

            for (int i = 0; i <= 1; i++)
            {
                // Index for the first and last point in substroke (ends)
                endIndex = (ends.Points.Length * i) - i;

                for (int j = 0; j < line.Points.Length; j++)
                {                 
                    tempDist = StrokeScanner.euclideanDistance(ends.Points[endIndex], line.Points[j]);                    
                    
                    if (tempDist < minDist)
                    {
                        minDist = tempDist;
                    }
                }
            }

            return minDist;
        }


        /// <summary>
        /// Gives the minimum time between two substrokes 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns>Minimum time between two substrokes</returns>
        private static double minTimeBetweenSubstrokes(Substroke s, Substroke t)
        {
            double startTime1 = (double)s.Points[0].Time;
            double endTime1 = (double)s.Points[s.Points.Length - 1].Time;

            double startTime2 = (double)t.Points[0].Time;
            double endTime2 = (double)t.Points[t.Points.Length - 1].Time;            

            return Math.Min(Math.Abs(((double)startTime1) - ((double)endTime2)),
                Math.Abs(((double)endTime1) - ((double)startTime2)));
        }

        /// <summary>
        /// Gives the maximum distance between two substrokes (pointwise)
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        /// <returns>maximum distance between the strokes</returns>
        private static double maxDistBetweenFrag(Substroke s, Substroke t)
        {
            double maxDist = 0.0;
            double tempDist;

            for (int i = 0; i < s.Points.Length; i++)
            {
                for (int j = 0; j < t.Points.Length; j++)
                {
                    tempDist = StrokeScanner.euclideanDistance(s.Points[i], t.Points[j]);                    
                    if (tempDist > maxDist)
                    {
                        maxDist = tempDist;
                    }
                }
            }

            return maxDist;
        }

        #endregion
    }
}
