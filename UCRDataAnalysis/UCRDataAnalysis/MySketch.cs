using System;
using System.Collections.Generic;
using System.Text;

namespace UCRDataAnalysis
{
    /// <summary>
    /// A Sketch with 2 extra attributes:
    /// integer author
    /// string for activity
    /// </summary>
    public class MySketch
    {

        private Sketch.Sketch sketch;
        private int author;
        private string activity;

        public MySketch(Sketch.Sketch s)
        {
            sketch = s;
        }

        public MySketch(Sketch.Sketch s, int author)
        {
            sketch = s;
            this.author = author;
        }

        public MySketch(Sketch.Sketch s, string activity)
        {
            sketch = s;
            this.activity = activity;
        }

        public MySketch(Sketch.Sketch s, int author, string activity)
        {
            sketch = s;
            this.author = author;
            this.activity = activity;
        }

        public Sketch.Sketch Sketch
        {
            get { return sketch; }
        }

        public int Author
        {
            get
            {
                return author;
            }
            set
            {
                author = value;
            }
        }

        public string Activity
        {
            get
            {
                return activity;
            }
            set
            {
                activity = value;
            }
        }

        public Guid Id
        {
            get
            {
                return sketch.XmlAttrs.Id.Value;
            }
            set
            {
                sketch.XmlAttrs.Id = value;
            }
        }

    }
}
