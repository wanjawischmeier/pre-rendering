using System;
using Emgu.CV;
using cv = Emgu.CV.CvInvoke;

namespace Decoding
{
    public class Decocder
    {
        string mapFile;
        Mat img;

        public Decocder(string mapFile)
        {
            this.mapFile = mapFile;

            img = cv.Imread(mapFile);
        }

        public void Show(string windowName)
        {
            cv.Imshow(windowName, img);

            cv.WaitKey();
        }


    }
}
