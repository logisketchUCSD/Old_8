using System;
using System.Collections.Generic;
using System.Text;
using libsvm;

namespace Svm
{
    /// <summary>
    /// Abstract class serves as base for classifying new examples
    /// </summary>
    public abstract class ClassifySVM
    {
        #region INTERNALS

        private svm_model model;

        #endregion

        #region CONSTRUCTORS

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modelFile">Model file to load</param>
        public ClassifySVM(string modelFile)
        {
            loadModel(modelFile);
        }

        #endregion

        #region CLASSIFY STUFF :p

        /// <summary>
        /// Load model file
        /// </summary>
        /// <param name="modelFile">modelfile to load</param>
        private void loadModel(string modelFile)
        {
             model = svm.svm_load_model(modelFile); // Model file
        }

        /// <summary>
        /// Compute the nodes given a line
        /// </summary>
        /// <param name="line">Line to convert</param>
        /// <returns>Nodes</returns>
        public svm_node[] lineToNodes(string line)
        {
            SupportClass.Tokenizer st = new SupportClass.Tokenizer(line, " \t\n\r\f:");

            double target = Convert.ToDouble(st.NextToken());
            int m = st.Count / 2;
            svm_node[] x = new svm_node[m];
            for (int j = 0; j < m; j++)
            {
                x[j] = new svm_node();
                x[j].index = Convert.ToInt32(st.NextToken());
                x[j].value_Renamed = Convert.ToDouble(st.NextToken());
            }

            return x;
        }

        /// <summary>
        /// Predict without probabilities
        /// </summary>
        /// <param name="line">Line to predict</param>
        /// <returns>Best Classification</returns>
        public double predict(string line)
        {
            svm_node[] x = lineToNodes(line);
            return predict(x);
        }

        /// <summary>
        /// Predict without probabilities
        /// </summary>
        /// <param name="x">Nodes to predict</param>
        /// <returns>Best Classification</returns>
        public double predict(svm_node[] x)
        {
            int svm_type = svm.svm_get_svm_type(model);
            double v = svm.svm_predict(model, x);
            return v;
        }

        /// <summary>
        /// Predict with probabilities
        /// </summary>
        /// <param name="line">Line to predict</param>
        /// <param name="probabilityEstimate">Returns probabilities</param>
        /// <returns>Best classification</returns>
        public double predict(string line, out double[] probabilityEstimate)
        {
            svm_node[] x = lineToNodes(line);
            int[] labels;
            return predict(x, out probabilityEstimate, out labels);
        }

        /// <summary>
        /// Predict with probabilities and labels
        /// </summary>
        /// <param name="x">Nodes to predict</param>
        /// <param name="probabilityEstimate">Returns probabilities</param>
        /// <param name="labels">Returns labels</param>
        /// <returns>Best classification</returns>
        public double predict(svm_node[] x, out double[] probabilityEstimate, out int[] labels)
        {
            int svm_type = svm.svm_get_svm_type(model);
		    int nr_class = svm.svm_get_nr_class(model);
		    
            labels = new int[nr_class];
            probabilityEstimate = new double[nr_class];

            svm.svm_get_labels(model, labels);
            double v = svm.svm_predict_probability(model, x, probabilityEstimate);
            return v;
        }

        /// <summary>
        /// Predict with probabilities and labels
        /// </summary>
        /// <param name="x">Nodes to predict</param>
        /// <param name="probabilityEstimate">Returns probabilities</param>
        /// <param name="labels">Returns labels</param>
        /// <returns>Best classification</returns>
        public double predict(svm_node[] x, out double[] probabilityEstimate, out string[] labels)
        {
            //Predict
            int[] intLabels;
            double v = predict(x, out probabilityEstimate, out intLabels);

            //Change intLabels into strings
            int i, len = intLabels.Length;
            labels = new string[len];
            for (i = 0; i < len; ++i)
            {
                labels[i] = predictToString(intLabels[i]);
            }

            return v;
        }

        /// <summary>
        /// Predict with probabilities and labels
        /// </summary>
        /// <param name="line">Line to predict</param>
        /// <param name="probabilityEstimate">Return probabilities</param>
        /// <param name="labels">Return labels</param>
        /// <returns>Best classification</returns>
        public double predict(string line, out double[] probabilityEstimate, out string[] labels)
        {
            return predict(lineToNodes(line), out probabilityEstimate, out labels);
        }

        /// <summary>
        /// Method must be created in child class.  
        /// Takes a classification, returns the best string label.
        /// </summary>
        /// <param name="predict">Best classification</param>
        /// <returns>String label</returns>
        public abstract string predictToString(int predict);
     
        #endregion
    }    
}
