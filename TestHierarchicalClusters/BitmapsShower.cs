using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace TestHierarchicalClusters
{
    public partial class BitmapsShower : Form
    {
        public BitmapsShower(Bitmap test, Bitmap template)
        {
            InitializeComponent();

            pictureBoxTest.Image = test;
            pictureBoxTemplate.Image = template;

            System.Drawing.Bitmap flag = new System.Drawing.Bitmap(10, 10);
            for (int x = 0; x < flag.Height; ++x)
                for (int y = 0; y < flag.Width; ++y)
                    flag.SetPixel(x, y, Color.White);
            for (int x = 0; x < flag.Height; ++x)
                flag.SetPixel(x, x, Color.Red);
            pictureBoxOverlay.Image = flag;
        }
    }
}