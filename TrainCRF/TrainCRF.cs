/**
 * File: TrainCRF.cs
 * 
 * Authors: Aaron Wolin, Devin Smith, Jason Fennell, and Max Pflueger (Sketchers 2006).
 *          Code expanded by Anton Bakalov (Sketchers 2007).
 *          Harvey Mudd College, Claremont, CA 91711.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using ConverterXML;
using Sketch;
using CRF;
using System.Collections.Generic;
using Featurefy;
using System.IO;

namespace TrainCRF
{
	/// <summary>
	/// Class that allows for training of a CRF
	/// </summary>
	public class TrainCRF
	{
		/// <summary>
		/// CRF to do training on
		/// </summary>
		CRF.CRF[] trainCRF;

		/// <summary>
		/// Simple constructor to initialize the class data members
		/// </summary>
		/// <param name="inFrags">The raw training data</param>
		/// <param name="inLabels">Labels for the raw training data</param>
		public TrainCRF(Substroke[][] inFrags, FeatureSketch[] features, int[][] inLabels, CRF.CRF inCRF)
		{
			trainCRF = new CRF.CRF[inFrags.Length];

			// Need to make sure matlab code only init'd once
			bool loopyBPinit = false;

			Console.WriteLine("{0} files total", inFrags.Length);
			Console.WriteLine();				

			// Initialize each CRF
			for (int i = 0; i < inFrags.Length; i++)
			{
				Console.WriteLine("Starting initialization of {0}th CRF.  Current Time is {1}", i, DateTime.Now);
				trainCRF[i] = new CRF.CRF(inCRF.numLabels, loopyBPinit);
				loopyBPinit = true;
				trainCRF[i].initGraph(inFrags[i], ref features[i]);
				trainCRF[i].loadParameters(inCRF.getParameters());
				trainCRF[i].trueLabels = inLabels[i];
			}
		}

		/// <summary>
		/// Sets up the call to conjugate gradient to train the CRF.
		/// </summary>
		/// <returns>The optimal set of parameters for classification, according to conjugate gradient</returns>
		public double[] train()
		{
            //debug
            //List<string> output = new List<string>();
            //output.Add("================ Run Train ================");
            //printToTestFile(output);

            //CRF.ConjugateGradient.myFuncDel f = new CRF.ConjugateGradient.myFuncDel(logLikelihoodTEST);
            //CRF.ConjugateGradient.myFuncDel f = new CRF.ConjugateGradient.myFuncDel(logLikelihoodJTREE); 

			CRF.ConjugateGradient.myFuncDel f = new CRF.ConjugateGradient.myFuncDel( logLikelihood );
            CRF.ConjugateGradient.gradDel grad = new CRF.ConjugateGradient.gradDel( gradient );
			//CRF.ConjugateGradient.gradDel grad = new CRF.ConjugateGradient.gradDel( numGrad );
			//CRF.ConjugateGradient.gradDel grad = new CRF.ConjugateGradient.gradDel( anotherGrad );

			// Find my parameters
			Console.WriteLine("Prepare for the Descent!");
			
			// HACK!
			return CRF.ConjugateGradient.conjGradDescent(f, trainCRF[0].getParameters(), grad, CRF.CRF.ERROR_TOLERANCE, CRF.CRF.PARAMETER_TOLERANCE);
		}

		/// <summary>
		/// Calculates and returns the gradient of the CRF at the specified point in parameter space.
		/// </summary>
		/// <param name="parameters">site and interaction parameters concatenated to a vector</param>
		/// <returns>gradient at input vector</returns>
		public double[] gradient(double[] parameters)
		{
			//Console.WriteLine("GRADIENT");

			double[] grad = new double[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
				grad[i] = 0.0;

			for (int cases = 0; cases < trainCRF.Length; cases++)
			{
				// Save the paramters
				double[] savedParameters = trainCRF[cases].loadParameters(parameters);

				// Update all feature values according to new parameters
				trainCRF[cases].calculateFeatures();

				// Update all belief with the new parameters
				trainCRF[cases].infer();

				double[] expSiteVal = expectedSiteValue(trainCRF[cases]);
				double[] trueSiteVal = trueSiteValue(trainCRF[cases]);
				double[] expInterVal = expectedInteractionValue(trainCRF[cases]);
				double[] trueInterVal = trueInteractionValue(trainCRF[cases]);

				int siteParamLength = trainCRF[cases].numLabels * trainCRF[cases].numSiteFeatures;
				for (int i = 0; i < siteParamLength; i++)
				{
					grad[i] += (trueSiteVal[i] - expSiteVal[i]);

					//if (Math.Abs(grad[i]) > 100)
					//	break;
				}

				int interParamLength = trainCRF[cases].numLabels * trainCRF[cases].numLabels * trainCRF[cases].numInteractionFeatures;
				for (int i = 0; i < interParamLength; i++)
				{
					grad[i + siteParamLength] += (trueInterVal[i] - expInterVal[i]);
				}

				// Restore the parameters
				trainCRF[cases].loadParameters(savedParameters);
			}
			
			// Regularization factor.  This subtracts by the parameter value divided by sigma^2, where sigma is a regularization paramter
			// that is determined in some way unknown to me.  It may have something to do with the variance of the parameters.  Here I leave it as 1
			for (int i = 0; i < grad.Length; i++)
			{
				grad[i] -= parameters[i];
			}

			// ****Set gradient to zero for redundant parameters****
			for (int k = 0; k < trainCRF[0].numSiteFeatures; ++k)
			{
				grad[k] = 0.0;
			}
			
			int skipBeginningIndices = trainCRF[0].numLabels * trainCRF[0].numSiteFeatures;
			for (int k = 0; k < trainCRF[0].numInteractionFeatures; ++k)
			{
				grad[skipBeginningIndices + k] = 0.0;
			}

			// We're using the negative Log Likelihood so make the gradient negative too.
			grad = ConjugateGradient.scalMult(-1, grad);
			
            /*
			bool foo = false;
			if (foo)
			{
				// Lets do a numerical gradient and compare
				double[] numGradVal = numGrad(parameters);
				
				// Print 'em both and relationship
				Console.WriteLine("*****Gradient and numGradient*****");
				Console.WriteLine("index \tgrad             \tnumGrad          \tdiff             \tg/nG             \tnG/g");
				for (int i = 0; i < grad.Length; i++)
				{
					// Print each line
					Console.WriteLine("{0, 6} \t{1, 17} \t{2, 17} \t{3, 17} \t{4, 17} \t{5, 17}", i, grad[i], numGradVal[i], grad[i]-numGradVal[i], grad[i]/numGradVal[i], numGradVal[i]/grad[i]);
				}

				grad = numGradVal;
			}
            */

			return grad;
		}

		/// <summary>
		/// Gradient based on "Learning Diagram Parts with Hidden Random Fields" by Martin Szummer.
		/// </summary>
		/// <param name="parameters">Place in parameter space at which to evaluated the gradient</param>
		/// <returns>Gradient of log likelihood function at point in parameter space</returns>
		public double[] anotherGrad(double[] parameters)
		{
			Console.WriteLine("anotherGRADIENT");

			// prepare the grad holder
			double[] grad = new double[parameters.Length];
			for(int i=0; i<parameters.Length; i++)
				grad[i] = 0.0;

			for(int cases = 0; cases < trainCRF.Length; cases++)
			{
				// save the paramters
				double[] savedParameters = trainCRF[cases].loadParameters(parameters);

				// update all feature values according to new parameters
				trainCRF[cases].calculateFeatures();

				// update all belief with the new parameters
				trainCRF[cases].infer();
				
				// Hold this in a variable to minimize repeated calculation
				int numSiteLabels = trainCRF[cases].numLabels * trainCRF[cases].numSiteFeatures;

				// Calculate all of the site parts of gradient
				for(int n1 = 0; n1 < trainCRF[cases].nodes.Length; n1++)
				{
					for(int i = 0; i < trainCRF[cases].numLabels; i++) // If we start the loop at j = 1, then we keep all the 0 based parameters 0
					{
						// This is the 'true' probability distribution
						double trueLabelRight = 0.0;
						if(trainCRF[cases].trueLabels[n1] == i)
						{
							trueLabelRight = 1.0;
						}

						// Empirical probability distribution based on the current parameter model
						//double inferLabelRight = trainCRF[cases].nodes[n1].siteBelief[i];

						//double trueVsInferDiff = trueLabelRight - inferLabelRight;
						double trueVsInferDiff = trueLabelRight - trainCRF[cases].nodes[n1].siteBelief[i];

						for(int p = 0; p < trainCRF[cases].numSiteFeatures; p++)
						{
							grad[i*trainCRF[cases].numSiteFeatures + p] += (trueVsInferDiff * trainCRF[cases].nodes[n1].siteFeatureFunctionVals[i][p]);
						}
					}

					//Calculated all of the interaction parts of gradient
					for(int n2 = 0; n2 < trainCRF[cases].nodes.Length; n2++)
					{
						for(int i = 0; i < trainCRF[cases].numLabels; i++)
						{
							for(int j = 0; j < trainCRF[cases].numLabels; j++)
							{
								if(trainCRF[cases].nodes[n1].neighbors.Contains(trainCRF[cases].nodes[n2]))
								{
									int n1IndexOfn2 = trainCRF[cases].nodes[n1].neighbors.IndexOf(trainCRF[cases].nodes[n2]);

									// This is the 'true' probability distribution
									double trueLabelRight = 0.0;
									if((trainCRF[cases].trueLabels[n1] == i) && (trainCRF[cases].trueLabels[n2] == j))
									{
										trueLabelRight = 1.0;
									}					
									
									// Empirical probability distribution based on the current parameter model
									double inferLabelRight = ((double[][])trainCRF[cases].nodes[n1].interactionBeliefVals[n1IndexOfn2])[i][j];

									double trueVsInferDiff = trueLabelRight - inferLabelRight;

									for(int p = 0; p < trainCRF[cases].numInteractionFeatures; p++)
									{
										grad[numSiteLabels + 
											(i * trainCRF[cases].numLabels * trainCRF[cases].numInteractionFeatures) + 
											(j * trainCRF[cases].numInteractionFeatures) + p] +=
											(trueVsInferDiff * ((double[][][])trainCRF[cases].nodes[n1].interactionFeatureFunctionVals[n1IndexOfn2])[i][j][p]);
									}
								}
							}
						}
					}								
				}

				// restore the parameters
				trainCRF[cases].loadParameters(savedParameters);
			}

			// ****Set gradient to zero for redundant parameters****
			for(int k = 0; k < trainCRF[0].numSiteFeatures; ++k)
			{
				grad[k] = 0.0;
			}
			for(int k = 0; k < trainCRF[0].numInteractionFeatures; ++k)
			{
				grad[(trainCRF[0].numLabels * trainCRF[0].numSiteFeatures) + k] = 0.0;
			}

			bool foo = false;
			if(foo)
			{
				//lets do a numerical gradient and compare
				double[] numGradVal = numGrad(parameters);
				
				//print 'em both and relationship
				Console.WriteLine("*****Gradient and numGradient*****");
				Console.WriteLine("index \tgrad             \tnumGrad          \tdiff             \tg/nG             \tnG/g");
				for(int i=0; i < grad.Length; i++)
				{
					//print each line
					Console.WriteLine("{0, 6} \t{1, 17} \t{2, 17} \t{3, 17} \t{4, 17} \t{5, 17}", i, grad[i], numGradVal[i], grad[i]-numGradVal[i], grad[i]/numGradVal[i], numGradVal[i]/grad[i]);
				}

				grad = numGradVal;
			}

			grad = ConjugateGradient.scalMult(-1, grad);
			return grad;
		}

		/// <summary>
		/// Calculates the gradient of the logLikelihood function numericaly
		/// </summary>
		/// <param name="parameters">site and interaction parameters concatenated to a vector</param>
		/// <returns>gradient at input vector</returns>
		public double[] numGrad(double[] parameters)
		{
			Console.WriteLine("numGRADIENT");

			// prepare the grad holder
			double[] grad = new double[parameters.Length];
			for(int i=0; i<parameters.Length; i++)
				grad[i] = 0.0;

			for(int cases = 0; cases < trainCRF.Length; cases++)
			{
				//find the base case
				double baseLikelihood = logLikelihood(parameters);

				// ***find the numerical gradient***
				for(int i=0; i<grad.Length; i++)
				{
					double[] newParams = parameters;

					//create a small displacement and evaluate at the new place
					double deltaX = 0.000001;   // ARBITRARY.  MAYBE SCALE BASED ON PREVIOUS INFORMATION?
					newParams[i] += deltaX;				
					double newLikelihood = logLikelihood(newParams);

					//find this component of the gradient
					grad[i] = (newLikelihood - baseLikelihood)/ deltaX;
				}
			}

			// ****Set gradient to zero for redundant parameters****
			for(int k = 0; k < trainCRF[0].numSiteFeatures; ++k)
			{
				grad[k] = 0.0;
			}
			for(int k = 0; k < trainCRF[0].numInteractionFeatures; ++k)
			{
				grad[(trainCRF[0].numLabels * trainCRF[0].numSiteFeatures) + k] = 0.0;
			}

			return grad;
		}


		/// <summary>
		/// Calculates the negative log likelihood of a given set of parameters and labels.  This works
		/// as an error function, where minimizing it maximizes probability (as L -> 0, P -> 1)
		/// </summary>
		/// <param name="parameters">Parameters with which to evaluate the likelihood function</param>
		/// <returns>Log likelihood of a given labelset given parameters</returns>
		public double logLikelihood(double[] parameters)
		{
			double logLikelihood = 0.0;			

			for (int cases = 0; cases < trainCRF.Length; cases++)
			{
				double[] oldParameters = trainCRF[cases].loadParameters(parameters);

				trainCRF[cases].calculateFeatures();
                trainCRF[cases].infer();
                double joint = trainCRF[cases].getLogJoint(trainCRF[cases].trueLabels);
                logLikelihood += joint;

				trainCRF[cases].loadParameters(oldParameters);
			}

			// Normalizing factor lots of people have.  This is the (Euclidian) norm of the parameter vector squared, divided by two
			// Technically is logLikelihood -= (ConjugateGradient.dot(paramters,parameters) / 2 * sigma) where
			// sigma is the regularization parameter.  I'm not sure how to determine it, so I'm leaving it as 1 here.
			logLikelihood -= (ConjugateGradient.dot(parameters, parameters) / 2);

			return (-logLikelihood);		
		}

        #region other log likelihood calculations
        /// <summary>
        /// Calculates the negative log likelihood of a given set of parameters and labels.  This works
        /// as an error function, where minimizing it maximizes probability (as L -> 0, P -> 1)
        /// </summary>
        /// <param name="parameters">Parameters with which to evaluate the likelihood function</param>
        /// <returns>Log likelihood of a given labelset given parameters</returns>
        public double logLikelihoodLBP(double[] parameters)
        {
            double logLikelihood = 0.0;

            for (int cases = 0; cases < trainCRF.Length; cases++)
            {
                double[] oldParameters = trainCRF[cases].loadParameters(parameters);

                trainCRF[cases].calculateFeatures();
                trainCRF[cases].inferCSharp();
                double joint = trainCRF[cases].getLogJointFromLBP(trainCRF[cases].trueLabels);
                logLikelihood += joint;

                trainCRF[cases].loadParameters(oldParameters);
            }

            // Normalizing factor lots of people have.  This is the (Euclidian) norm of the parameter vector squared, divided by two
            // Technically is logLikelihood -= (ConjugateGradient.dot(paramters,parameters) / 2 * sigma) where
            // sigma is the regularization parameter.  I'm not sure how to determine it, so I'm leaving it as 1 here.
            logLikelihood -= (ConjugateGradient.dot(parameters, parameters) / 2);

            return (-logLikelihood);
        }

        /// <summary>
        /// Calculates the negative log likelihood of a given set of parameters with the true labels,
        /// but using the results from junction tree inference (no logZ)
        /// This works as an error function, where minimizing it maximizes probability (as L -> 0, P -> 1)
        /// </summary>
        /// <param name="parameters">Parameters with which to evaluate the likelihood function</param>
        /// <returns>Log likelihood of a given labelset given parameters</returns>
        public double logLikelihoodJTREE(double[] parameters)
        {
            double logLikelihood = 0.0;

            for (int cases = 0; cases < trainCRF.Length; cases++)
            {
                double[] oldParameters = trainCRF[cases].loadParameters(parameters);

                trainCRF[cases].calculateFeatures();
                trainCRF[cases].inferJTree(true); 
                double joint = trainCRF[cases].getLogJointFromJtree(trainCRF[cases].trueLabels);
                logLikelihood += joint;

                trainCRF[cases].loadParameters(oldParameters);
            }

            // Normalizing factor lots of people have.  This is the (Euclidian) norm of the parameter vector squared, divided by two
            // Technically is logLikelihood -= (ConjugateGradient.dot(paramters,parameters) / 2 * sigma) where
            // sigma is the regularization parameter.  I'm not sure how to determine it, so I'm leaving it as 1 here.
            logLikelihood -= (ConjugateGradient.dot(parameters, parameters) / 2);

            return (-logLikelihood);
        }

        /// <summary>
        /// Calculates the negative log likelihood of a given set of parameters with the true labels,
        /// but using the result configuration table from exact inference.
        /// This works as an error function, where minimizing it maximizes probability (as L -> 0, P -> 1)
        /// </summary>
        /// <param name="parameters">Parameters with which to evaluate the likelihood function</param>
        /// <returns>Log likelihood of a given labelset given parameters</returns>
        public double logLikelihoodEXACT(double[] parameters)
        {
            double logLikelihood = 0.0;

            for (int cases = 0; cases < trainCRF.Length; cases++)
            {
                double[] oldParameters = trainCRF[cases].loadParameters(parameters);

                trainCRF[cases].calculateFeatures();
                trainCRF[cases].inferExact();  // Needed to get config table
                double joint = trainCRF[cases].getLogJointFromExact(trainCRF[cases].trueLabels);
                logLikelihood += joint;

                trainCRF[cases].loadParameters(oldParameters);
            }

            // Normalizing factor lots of people have.  This is the (Euclidian) norm of the parameter vector squared, divided by two
            // Technically is logLikelihood -= (ConjugateGradient.dot(paramters,parameters) / 2 * sigma) where
            // sigma is the regularization parameter.  I'm not sure how to determine it, so I'm leaving it as 1 here.
            logLikelihood -= (ConjugateGradient.dot(parameters, parameters) / 2);

            return (-logLikelihood);
        }

        //debug
        private void printToTestFile(List<string> lines)
        {
            // Write to file
            TextWriter oldOut = Console.Out;
            FileStream ostrm;
            StreamWriter writer;
            try
            {
                ostrm = new FileStream("../../jtreeTests/loglikelihoodComparison.txt", FileMode.Append, FileAccess.Write);
                writer = new StreamWriter(ostrm);
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot open loglikelihoodComparison for writing");
                Console.WriteLine(e.Message);
                return;
            }
            Console.SetOut(writer);

            foreach (string line in lines)
                System.Console.WriteLine(line);

            // Close file
            Console.SetOut(oldOut);
            writer.Close();
            ostrm.Close();
        }

        // Used in place of logLikelihood.  Outputs the same value, but does some testing
        // in between.
        public double logLikelihoodTEST(double[] parameters)
        {
            double jtreeVersion = logLikelihoodJTREE(parameters);
            double exactVersion = logLikelihoodEXACT(parameters);
            double lbpVersion = logLikelihood(parameters);
            double originalVersion = logLikelihood(parameters);
            double diff = jtreeVersion - exactVersion;

            List<string> output = new List<string>();
            output.Add("J: " + jtreeVersion);
            output.Add("E: " + exactVersion);
            output.Add("L: " + lbpVersion);
            output.Add("O: " + originalVersion);
            //output.Add("Diff: " + diff);
            output.Add("");
            printToTestFile(output);

            return originalVersion;
        }
        #endregion

		/// <summary>
		/// Calculates the expected value of the site potentials for the probability distribution.  We use marginal probabilities,
		/// and because of this have lots of repeated terms.  This fact allows us to factor our sum into a bunch node/label specific
		/// terms multiplied by the number of times they appear.  This also changes the running time of this function from exponential 
		/// to polynomial.
		/// </summary>
		/// <returns>The expected value of the site potentials of this probability distribution</returns>
		public double[] expectedSiteValue(CRF.CRF trainCRF)
		{
			// Initialize the "sum" array
			double[] sum = new double[trainCRF.numSiteFeatures * trainCRF.numLabels];
			
			for (int i = 0; i < sum.Length; ++i)
			{
				sum[i] = 0.0;
			}

			bool[] indexUsed = new bool[sum.Length];

			double multiplier = Math.Pow(trainCRF.numLabels, trainCRF.nodes.Length - 1);

			// Go through each of the nodes
			for (int i = 0; i < trainCRF.nodes.Length; i++)
			{
				// Go through the labels for each node
				for (int j = 0; j < trainCRF.numLabels; j++)
				{
					// Go through the site features for each label
					for (int k = 0; k < trainCRF.numSiteFeatures; k++)
					{
                        // DEBUGGING
                        // Compare the site beliefs from LBP to exact marginalization
                        //Console.WriteLine("LBP belief is {0}, exact marginal is {1}", trainCRF.nodes[i].siteBelief[j]);
                        //End debugging

						sum[(j * trainCRF.numSiteFeatures) + k] += 
							trainCRF.nodes[i].siteFeatureFunctionVals[j][k] * trainCRF.nodes[i].siteBelief[j];
					
						//	sum[j*trainCRF.numSiteFeatures + k] += //multiplier *
						//	n1.siteFeatureFunctionVals[j][k] *
						//	n1.siteBelief[j];
					}
				}
			}

			return sum;
		}

		/// <summary>
		/// Computes and returns the true site value for the CRF (used for gradient)
		/// </summary>
		/// <param name="labels">Set of true labels for the nodes of the graph</param>
		/// <returns>True site value for the CRF</returns>
		public double[] trueSiteValue(CRF.CRF trainCRF)
		{
			// Initialize the "sum" array
			double[] sum = new double[trainCRF.numSiteFeatures * trainCRF.numLabels];
			
			for(int i = 0; i < sum.Length; ++i)
			{
				sum[i] = 0.0;
			}

			// Go through each of the nodes
			for (int i = 0; i < trainCRF.nodes.Length; i++)
			{
				int index = (trainCRF.trueLabels[trainCRF.nodes[i].index] * trainCRF.numSiteFeatures);

				for (int j = 0; j < trainCRF.numSiteFeatures; j++)
				{
					sum[index + j] += trainCRF.nodes[i].siteFeatureFunctionVals[trainCRF.trueLabels[trainCRF.nodes[i].index]][j];
				}
			}

			return sum;
		}

		/// <summary>
		/// Calculates the expected value of the interaction potentials for the probability distribution.  We use marginal probabilities,
		/// and because of this have lots of repeated terms.  This fact allows us to factor our sum into a bunch node/label specific
		/// terms multiplied by the number of times they appear.  This also changes the running time of this function from exponential 
		/// to polynomial.
		/// </summary>
		/// <returns>The expected value of the interaction potentials of this probability distribution</returns>
		public double[] expectedInteractionValue(CRF.CRF trainCRF)
		{
			// Initialize the "sum" array
			double[] sum = new double[trainCRF.numInteractionFeatures * trainCRF.numLabels * trainCRF.numLabels];

			for(int i = 0; i < sum.Length; ++i)
			{
				sum[i] = 0.0;
			}

			int numLabels = trainCRF.numLabels;
			int numInteractionFeatures = trainCRF.numInteractionFeatures;

			double multiplier = Math.Pow(numLabels, 2 * (trainCRF.nodes.Length - 1));

			for (int a = 0; a < trainCRF.nodes.Length; a++)
			{
				for (int b = a + 1; b < trainCRF.nodes.Length; b++)
				{
					Node n1 = trainCRF.nodes[a];
					Node n2 = trainCRF.nodes[b];

					if (n1.neighbors.Contains(n2))
					{
						int n1IndexOfn2 = n1.neighbors.IndexOf(n2);

						// Pulled these out of the loop
						double[][][] n1_iFFVs = (double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2];
						double[][] n1_iBVs = (double[][])n1.interactionBeliefVals[n1IndexOfn2];

						for (int i = 0; i < numLabels; i++)
						{
							for (int j = 0; j < numLabels; j++)
							{
								for (int k = 0; k < numInteractionFeatures; k++)
								{
									sum[(i * numLabels * numInteractionFeatures) + (j * numInteractionFeatures) + k] +=
										//multiplier *
										n1_iFFVs[i][j][k] * n1_iBVs[i][j];
								}
							}
						}
					}
				}
			}

			/*double tmp = 0.0;
			for (int i = 0; i < sum.Length; i++)
			{
				tmp += sum[i];
			}*/
					
			return sum;
		}

		/// <summary>
		/// Computes and returns the true interaction value for the CRF (used for gradient)
		/// </summary>
		/// <param name="labels">Set of true labels for the nodes of the graph</param>
		/// <returns>True interaction value for the CRF</returns>
		public double[] trueInteractionValue(CRF.CRF trainCRF)
		{
			// Initialize the "sum" array
			double[] sum = new double[trainCRF.numInteractionFeatures * trainCRF.numLabels * trainCRF.numLabels];
			
			for (int i = 0; i < sum.Length; ++i)
			{
				sum[i] = 0.0;
			}

			for (int i = 0; i < trainCRF.nodes.Length; i++)
			{
				for (int j = i + 1; j < trainCRF.nodes.Length; j++)
				{
					Node n1 = trainCRF.nodes[i];
					Node n2 = trainCRF.nodes[j];

					if (n1.neighbors.Contains(n2))
					{
						int n1IndexOfn2 = n1.neighbors.IndexOf(n2);
						double[][][] n1_iFFVs = ((double[][][])n1.interactionFeatureFunctionVals[n1IndexOfn2]);

						int index = (trainCRF.trueLabels[n1.index] * trainCRF.numLabels * trainCRF.numInteractionFeatures) + 
							(trainCRF.trueLabels[n2.index] * trainCRF.numInteractionFeatures);

						for (int k = 0; k < trainCRF.numInteractionFeatures; k++)
						{
							sum[index + k] += n1_iFFVs[trainCRF.trueLabels[n1.index]][trainCRF.trueLabels[n2.index]][k];
						}
					}
				}
			}		

			return sum;
		}
	}
}
