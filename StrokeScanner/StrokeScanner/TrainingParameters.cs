using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace StrokeScanner
{
    [Serializable]
    public class ScaleParameters
    {

        public double[] scale_factors;

        public ScaleParameters(int n)
        {
            scale_factors = new double[n];
        }

        /// <summary>
        /// Write this TrainingParameters to a file.
        /// </summary>
        /// <param name="file">file path to write to</param>
        public void writeToFile(string file)
        {
            FileStream fs = new FileStream(file, FileMode.Create);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, this);
            fs.Close();
        }

        /// <summary>
        /// Loads a saved TrainingParameters.
        /// </summary>
        /// <param name="path">file path to load from</param>
        /// <returns>new TrainingParameters created from the given file</returns>
        public static ScaleParameters LoadDesignation(string path)
        {
            FileStream fs = new FileStream(path, FileMode.Open);
            BinaryFormatter bf = new BinaryFormatter();
            ScaleParameters res = (ScaleParameters)bf.Deserialize(fs);
            fs.Close();
            return res;
        }
    }
}
