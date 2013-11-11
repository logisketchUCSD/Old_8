using System;
using System.Collections.Generic;
using System.Text;
using libsvm;

namespace Svm
{
    /// <summary>
    /// Trains the SVM
    /// </summary>
    public class TrainSVM
    {
        #region INTERNAL

        private bool DEBUG = true;

        private svm_parameter param;
        private svm_problem prob;
        private svm_model model;

        private string outFile;

        private int nr_fold;

        private int m_numGrid, m_cSteps, m_gSteps, m_numCross;
        private double m_cStart, m_cEnd, m_cStep, m_gStart, m_gEnd, m_gStep;

        private bool useProbability;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inFile">Training file in</param>
        /// <param name="outFile">Model file out</param>
        public TrainSVM(string inFile, string outFile)
        {
            setParam();
            loadProblem(inFile);
            checkErrors();

            this.outFile = outFile;
        }

        /// <summary>
        /// Constructor avoids file I/O
        /// </summary>
        /// <param name="prob">properly formatted problem</param>
        /// <param name="outFile">filename to output model file to</param>
        public TrainSVM(svm_problem prob, string outFile)
        {
            setParam();
            this.prob = prob;
            checkErrors();

            this.outFile = outFile;
        }
        #endregion

        #region TRAINING STUFF

        /// <summary>
        /// Start training
        /// </summary>
        public void start()
        {
            doTraining(this.outFile);
        }

        private double doCrossValidation()
        {
            int i;
	        int total_correct = 0;
	        double total_error = 0;
	        double sumv = 0, sumy = 0, sumvv = 0, sumyy = 0, sumvy = 0;
	        double[] target = new double[prob.l];
        	
	        svm.svm_cross_validation(prob, param, nr_fold, target);
            
	        if (param.svm_type == svm_parameter.EPSILON_SVR || param.svm_type == svm_parameter.NU_SVR)
	        {
		        for (i = 0; i < prob.l; i++)
		        {
			        double y = prob.y[i];
			        double v = target[i];
			        total_error += (v - y) * (v - y);
			        sumv += v;
			        sumy += y;
			        sumvv += v * v;
			        sumyy += y * y;
			        sumvy += v * y;
		        }
                if (DEBUG)
                {
                    //System.Console.Out.Write("Cross Validation Mean squared error = " + total_error / prob.l + "\n");
                    //System.Console.Out.Write("Cross Validation Squared correlation coefficient = " + (((prob.l * sumvy - sumv * sumy) * (prob.l * sumvy - sumv * sumy)) / ((prob.l * sumvv - sumv * sumv) * (prob.l * sumyy - sumy * sumy))) + "\n");
                }
	        }
	        else
		        for (i = 0; i < prob.l; i++)
			        if (target[i] == prob.y[i])
				        ++total_correct;

            double dist = (double)total_correct / prob.l;

            if (DEBUG)
            {
                //System.Console.Out.Write("Cross Validation Accuracy = " + 100.0 * dist + "%\n");
            }
            return dist;
        }

        /// <summary>
        /// Starts Training
        /// </summary>
        /// <param name="outName">Model file out</param>
        private void doTraining(string outName)
        {
            double cost, gamma;
            gridSearch(out cost, out gamma);
            save(cost, gamma, outName);
        }

        /// <summary>
        /// Check for errors in problem
        /// </summary>
        private void checkErrors()
        {
            string error_msg = svm.svm_check_parameter(prob, param);

            if (error_msg != null)
            {
                System.Console.Error.Write("Error: " + error_msg + "\n");
                System.Environment.Exit(1);
            }
        }

        /// <summary>
        /// Sets default parameters
        /// </summary>
        private void setParam()
        {
            param = new svm_parameter();

            // default values
            param.svm_type = svm_parameter.C_SVC;
            param.kernel_type = svm_parameter.RBF;
            param.degree = 3;
            param.gamma = 0; // 1/k
            param.coef0 = 0;
            param.nu = 0.5;
            param.cache_size = 80;
            param.C = 1;
            param.eps = 1e-3;
            param.p = 0.1;
            param.shrinking = 1;
            
            param.probability = 0; //default 0
            useProbability = false;

            param.nr_weight = 0;
            param.weight_label = new int[0];
            param.weight = new double[0];

            //this.cross_validation = 1;
            this.nr_fold = 4;

            setParam(20, -6.0, 20.0, 20, 7.0, -19.0, 5, 5);
        }

        /// <summary>
        /// Use probability for the final run
        /// </summary>
        /// <param name="probability"></param>
        public void setParam(bool probability)
        {
            useProbability = probability;
        }

        /// <summary>
        /// Set the grid search parameters
        /// </summary>
        /// <param name="cSteps">Number of steps between cStart and cEnd</param>
        /// <param name="cStart">Initial cost parameter</param>
        /// <param name="cEnd">Final cost parameter</param>
        /// <param name="gSteps">Number of steps between gStart and gEnd</param>
        /// <param name="gStart">Initial gamma parameter</param>
        /// <param name="gEnd">Final gamma parameter</param>
        /// <param name="numGrid">Number of grid searches</param>
        /// <param name="numCross">Number of cross validations</param>
        public void setParam(int cSteps, double cStart, double cEnd, int gSteps, double gStart, double gEnd, int numGrid, int numCross)
        {
            m_cSteps = cSteps;

            m_cStart = cStart;
            m_cEnd = cEnd;

            m_gSteps = gSteps;

            m_gStart = gStart;
            m_gEnd = gEnd;

            m_cStep = (m_cEnd - m_cStart) / m_cSteps;
            m_gStep = (m_gEnd - m_gStart) / m_gSteps;

            m_numGrid = numGrid;

            m_numCross = numCross;
        }

        /// <summary>
        /// Perform multiple grid searches
        /// </summary>
        /// <param name="cost">Returns best cost</param>
        /// <param name="gamma">Returns best gamma</param>
        private void gridSearch(out double cost, out double gamma)
        {
            cost = 0.0;
            gamma = 0.0;

            int i, times = m_numGrid;
            
            double dist, cStart = m_cStart, cEnd = m_cEnd, cStep = m_cStep, gStart = m_gStart, gEnd = m_gEnd, gStep = m_gStep;
            for (i = 0; i < times; ++i)
            {

                dist = gridSearch(cStart, cEnd, cStep, gStart, gEnd, gStep, out cost, out gamma);
                if (DEBUG)
                    Console.WriteLine("Grid search {0}: {1} with cost: 2^({2}), gamma: 2^({3})", i, dist, cost, gamma);
                Console.WriteLine();
                
                cStart = cost - 3.0 * cStep;
                cEnd = cost + 3.0 * cStep;
                cStep = (cEnd - cStart) / m_cSteps;

                gStart = gamma + 3.0 * gStep;
                gEnd = gamma - 3.0 * gStep;
                gStep = (gEnd - gStart) / m_gSteps;
            }
        }

        /// <summary>
        /// Perform a grid search
        /// </summary>
        /// <param name="cStart">Initial cost parameter</param>
        /// <param name="cEnd">Final cost parameter</param>
        /// <param name="cStep">Size of step</param>
        /// <param name="gStart">Initial gamma parameter</param>
        /// <param name="gEnd">Final gamma parameter</param>
        /// <param name="gStep">Size of step</param>
        /// <param name="cost">Return best cost</param>
        /// <param name="gamma">Return best gamma</param>
        /// <returns>Best percentage correct</returns>
        private double gridSearch(double cStart, double cEnd, double cStep, double gStart, double gEnd, double gStep, out double cost, out double gamma)
        {
#if DEBUG
                Console.WriteLine("Starting grid search... C:({0}, {1}) dC:{2} G:({3}, {4}) dG:{5}", cStart, cEnd, cStep, gStart, gEnd, gStep);
#endif

            double dist, max = double.NegativeInfinity;

            double bestC = -1, bestG = -1;

            double c, g;
#if DEBUG
            double progress = 0;
            double total = (int)(((cEnd-cStart)/cStep)*((gEnd-gStart)/gStep));
            Console.Write("  0% done");
#endif
            for (c = cStart; !equal(c, cEnd); c += cStep)
            {
                for (g = gStart; !equal(g, gEnd); g += gStep)
                {
                    dist = run(Math.Pow(2.0, c), Math.Pow(2.0, g));
                    if (dist > max)
                    {
                        max = dist;
                        bestC = c;
                        bestG = g;
                    }
#if DEBUG
                    Console.Write("\b\b\b\b\b\b\b\b\b");
                    ++progress;
                    if (progress / total < .1) Console.Write(" ");
                    if (progress / total < .999) Console.Write(" ");
                    Console.Write("{0:0}% done", (double)(progress * 100.0 / total));
#endif
                }
            }
            cost = bestC;
            gamma = bestG;
#if DEBUG
            Console.Write("\n");
#endif
            return max;
        }

        /// <summary>
        /// Compute whether a and b are equal
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private bool equal(double a, double b)
        {
            return Math.Abs(a - b) < 0.00001;
        }

        /// <summary>
        /// Run the following cost and gamma trainings, do cross validations
        /// </summary>
        /// <param name="cost">Cost parameter</param>
        /// <param name="gamma">Gamma parameter</param>
        /// <returns>Percentage correct</returns>
        private double run(double cost, double gamma)
        {
            param.C = cost;
            param.gamma = gamma;

            int i, times = m_numCross;
            double total = 0.0;
            for (i = 0; i < times; ++i)
                total += doCrossValidation();

            total /= times;

            return total;
        }

        /// <summary>
        /// Manually save if you choose to
        /// </summary>
        /// <param name="cost"></param>
        /// <param name="gamma"></param>
        /// <param name="outName"></param>
        public void save(double cost, double gamma, string outName)
        {
            param.C = cost;
            param.gamma = gamma;

            if (useProbability)
                param.probability = 1;
            else
                param.probability = 0;

            model = svm.svm_train(prob, param);
            svm.svm_save_model(outName, model);
        }

        /// <summary>
        /// Load the training file
        /// </summary>
        /// <param name="filename">training file</param>
        private void loadProblem(string filename)
        {
            System.IO.StreamReader fp = new System.IO.StreamReader(filename);
            List<string> vy = new List<string>(10);
            List<svm_node[]> vx = new List<svm_node[]>(10);
            int max_index = 0;

            while (true)
            {
                string line = fp.ReadLine();
                if (line == null)
                    break;

                SupportClass.Tokenizer st = new SupportClass.Tokenizer(line, " \t\n\r\f:");

                vy.Add(st.NextToken());
                int m = st.Count / 2;
                svm_node[] x = new svm_node[m];
                for (int j = 0; j < m; j++)
                {
                    x[j] = new svm_node();
                    x[j].index = Convert.ToInt32(st.NextToken());
                    x[j].value_Renamed = Convert.ToDouble(st.NextToken());
                }
                if (m > 0)
                    max_index = System.Math.Max(max_index, x[m - 1].index);
                vx.Add(x);
            }

            prob = new svm_problem();
            prob.l = vy.Count;
            prob.x = new svm_node[prob.l][];
            for (int i = 0; i < prob.l; i++)
                prob.x[i] = vx[i];
            prob.y = new double[prob.l];
            for (int i = 0; i < prob.l; i++)
                prob.y[i] = Convert.ToDouble(vy[i]);

            if (param.gamma == 0)
                param.gamma = 1.0 / max_index;

            fp.Close();
        }
     
        #endregion
    }
}
