using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVTest01
{
    public class ImageInfo
    {
        public Mat Image { set; get; }
        public int ImageGroupId { set; get; }
        public int ImageId { set; get; }
    }
}
