using System;
using System.Collections.Generic;
using System.Text;

// An arbitrary data point is just a vector of doubles of a fixed dimension
using KMeanPoint = System.Collections.Generic.List<double>;

// Represent clusters as lists of indices that index the data points passed in to KMeans
using Cluster = System.Collections.Generic.List<int>;   

namespace StrokeScanner
{
    class Kmeans
    {        
        private int m_K;
        private List<KMeanPoint> m_data;
        private int m_dimension;

        public Kmeans(int K, List<KMeanPoint> data)
        {
            if (K > data.Count)
            {
                throw new Exception("There must be at least as many data points as clusters in KMeans.");
            }

            m_K = K;
            m_data = data;
            m_dimension = data[0].Count;
        }      
  
        // HACK to get out data
        public List<KMeanPoint> post_cluster_centroids;

        /// <summary>
        /// This method actuall does the clustering.  It returns a list of clusters
        /// </summary>
        /// <returns></returns>
        public List<Cluster> cluster()
        {
            bool converged = false;
            int counter = 0;
            int restartCounter = 0;

            List<KMeanPoint> centroids = initCentroids();
            List<Cluster> clusters = new List<Cluster>();
            
            // Commented out the debugging output in the loop.
            // It turns out that things converge *very* quickly if they have a decent starting clustering.
            // Thus, we have lots of random restarts after not that many iterations of Kmeans.
            while (!converged  && (restartCounter < 50))
            {
                //Console.WriteLine("Iteration {0}: ", counter);
                if ((counter % 30) == 0)
                {
                    //Console.WriteLine("Restart with random clusters for the {0}th time", restartCounter+1);                    
                    ++restartCounter;
                    centroids = initCentroids();
                }

                clusters = assignDataToCentroids(centroids);
                List<KMeanPoint> newCentroids = findCentroids(clusters);

                //double deltaCentroids = 0.0;
                //for (int i = 0; i < m_K; ++i)
                //{
                //    deltaCentroids += distance(centroids[i], newCentroids[i]);
                //}
                //Console.WriteLine("{0}", deltaCentroids);
                //Console.Write("The size of each cluster is:");
                //for (int i = 0; i < m_K; ++i)
                //{
                //    Console.Write("{0} ", clusters[i].Count);
                //}
                //Console.WriteLine();

                if (sameSetOfCentroids(centroids, newCentroids))
                {
                    converged = true;
                }
                centroids = newCentroids;

                ++counter;
            }

            // HACK to allow access to the centroids from outside after clustering is complete.
            post_cluster_centroids = centroids;

            Console.WriteLine("Finished clustering with K={0}.", m_K);
            return clusters;
        }

        /// <summary>
        /// Chooses an initial set of centroids for the algorithm randomly by assigning points
        /// to clusters such that they are of equal sizes, then calculating the centroids of those
        /// clusters.        
        /// </summary>
        private List<KMeanPoint> initCentroids()
        {
            List<Cluster> clusters = new List<Cluster>();
            for (int i = 0; i < m_K; ++i)
            {
                clusters.Add(new Cluster());
            }

            List<int> dataIndices = new List<int>(m_data.Count);
            for (int i = 0; i < m_data.Count; ++i)
            {
                dataIndices.Add(i);
            }

            // Cycle through each cluster in turn, and for each cluster choose
            // a data point index uniformly randomly (without replacement) to go 
            // into that cluster.
            Random rand = new Random();
            int clusterIdx = 0;
            int dataIdx;
            int dataPointsLeft = m_data.Count;
            while (dataPointsLeft > 0)
            {
                dataIdx = rand.Next(dataPointsLeft);
                clusters[clusterIdx].Add(dataIndices[dataIdx]);
                dataIndices.RemoveAt(dataIdx);
                --dataPointsLeft;
                ++clusterIdx;
                clusterIdx %= m_K;
            }

            return findCentroids(clusters);
        }


        /// <summary>
        /// Find the centroids of a set of clusters by averaging all of the points in each cluster.
        /// </summary>
        /// <param name="clusters"></param>
        /// <returns></returns>
        private List<KMeanPoint> findCentroids(List<Cluster> clusters)
        {
            List<KMeanPoint> centroids = new List<KMeanPoint>();

            for (int cluster = 0; cluster < m_K; ++cluster)
            {
                // Centroid for this cluster is the average point of all the points in the cluster
                KMeanPoint centroid =  new KMeanPoint();
                for(int dim = 0; dim < m_dimension; ++dim)
                {
                    centroid.Add(0);
                }

                for (int pt = 0; pt < clusters[cluster].Count; ++pt)
                {
                    for (int dim = 0; dim < m_dimension; ++dim)
                    {
                        centroid[dim] += m_data[clusters[cluster][pt]][dim];
                    }
                }

                for (int dim = 0; dim < m_dimension; ++dim)
                {
                    centroid[dim] /= clusters[cluster].Count;
                }

                centroids.Add(centroid);
            }

            return centroids;
        }
        
        /// <summary>
        /// Data points are clustered.  Each point is added to the cluster which has a centroid
        /// closest to it.
        /// </summary>
        /// <param name="centroids"></param>
        /// <returns></returns>
        private List<Cluster> assignDataToCentroids(List<KMeanPoint> centroids)
        {
            List<Cluster> clusters = new List<Cluster>();
        
            // Initialize clusters
            for (int i = 0; i < m_K; ++i)
            {
                clusters.Add(new Cluster());
            }

            // For each data point find the centroid that it is closest to and add that
            // data point to that centroid's group.
            for(int p = 0; p < m_data.Count; ++p)
            {            
                int idx = indexOfClosestCentroid(m_data[p], centroids);
                clusters[idx].Add(p);
            }

            return clusters;
        }

        /// <summary>
        /// Calculate the index of the centroid that is closest to the point p
        /// </summary>
        /// <param name="p"></param>
        /// <param name="centroids"></param>
        /// <returns></returns>
        private int indexOfClosestCentroid(KMeanPoint p, List<KMeanPoint> centroids)
        {
            int idx = 0;
            double minDist = double.PositiveInfinity;
            double tmpDist;

            for (int i = 0; i < m_K; ++i)
            {
                tmpDist = distance(p, centroids[i]);
                if ((tmpDist < minDist) && (!double.IsNaN(tmpDist)))
                {
                    idx = i;
                    minDist = tmpDist;
                }
            }

            return idx;
        }

        /// <summary>
        /// Return true if both provided lists of KMeanPoints are equal and *in the same order.*
        /// 
        /// TODO: Check to see if permuting the order of the KMeanPoints can cause KMeans to infinite loop
        /// because the permutations are different ways of representing the same solution point.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private bool sameSetOfCentroids(List<KMeanPoint> x, List<KMeanPoint> y)
       {
            // Check each point...
            for (int i = 0; i < x.Count; ++i)
            {
                // ...by checking each dimension of each point.
                for (int dim = 0; dim < m_dimension; ++dim)
                {
                    // != returns true if both arguments are NaN, so add logic so that this is false if
                    // the values are not equal and not both NaN.
                    if ((x[i][dim] != y[i][dim]) && 
                        !(double.IsNaN(x[i][dim]) && double.IsNaN(y[i][dim])))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Calculate the Euclidean distance between a pair of n-dimensional points
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private double distance(KMeanPoint x, KMeanPoint y)
        {
            //HACK to avoid NaNs
            bool allNaN = true;
            for (int i = 0; i < x.Count; ++i)
            {
                if (!double.IsNaN(x[i]))
                {
                    allNaN = false;
                }
            }
            if (allNaN) { return 0.0; }
            allNaN = true;
            for (int i = 0; i < y.Count; ++i)
            {
                if (!double.IsNaN(y[i]))
                {
                    allNaN = false;
                }
            }
            if (allNaN) { return 0.0; }
            

            double dist = 0.0;
            for (int i = 0; i < x.Count; ++i)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }
            return Math.Sqrt(dist);
        }
    }
}