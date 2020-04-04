using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using OSGeo.GDAL;
using System.Numerics;

namespace GeoTIFFConverter
{
    [Cmdlet(VerbsData.Convert, "GeoTIFF")]
    public class ConvertCmdlet : Cmdlet
    {
        [Parameter(Mandatory = true)]
        public string Path;

        [Parameter]
        public string Destination;

        [Parameter]
        public SwitchParameter RenderFaces = false;

        [Parameter]
        public SwitchParameter Text = false;

        [Parameter]
        public double Scale = 0.1;

        [Parameter]
        public int MaxY = -1;

        protected override void BeginProcessing()
        {
            GdalConfiguration.ConfigureGdal();
            System.Console.WriteLine("Starting...");
            Gdal.AllRegister();
            if(Destination == null)
            {
                Destination = System.IO.Path.GetDirectoryName(Path) + "/Output.stl";
            }
        }

        protected override void ProcessRecord()
        {
            var dataset = Gdal.Open(Path, Access.GA_ReadOnly);
            Driver drv = dataset.GetDriver();
            Console.WriteLine("number of things is:");
            Console.WriteLine(dataset.RasterCount);
            //band numbers start at 1
            var heightdata = dataset.GetRasterBand(1);
            double[] minmax = new double[2];
            heightdata.ComputeRasterMinMax(minmax, 0);
            Console.WriteLine($"minmax is {minmax[0]}, {minmax[1]}");

            //create a buffer to hold data in memory.
            int maxX = heightdata.XSize;
            if (MaxY == -1)
            {
                MaxY = heightdata.YSize;
            }

            double[] databuffer = new double[maxX * MaxY];
            heightdata.ReadRaster(0, 0, maxX, MaxY, databuffer, maxX, MaxY, 0, 0);
            //heightdata.ReadRaster()
            //misc stuff
            heightdata.GetNoDataValue(out double nodataval, out int hasnodataval);
            Console.WriteLine($"nodata vals are {nodataval}, {hasnodataval}");
            var thing = heightdata.GetRasterColorInterpretation();
            var thing2 = Gdal.GetDataTypeSize(heightdata.DataType);

            using (var outfile = new STLExporter(Destination, !Text))
            {
                if (RenderFaces)
                {
                    for (int y = 0; y < MaxY - 1; ++y)
                    {
                        for (int x = 0; x < maxX - 1; ++x)
                        {
                            int index = y * maxX + x;
                            double zvalue = databuffer[index];

                            Vector3 pointA = new Vector3((float)(x * Scale),(float)(y * Scale), (float)(zvalue * Scale));
                            Vector3 pointB = new Vector3((float)(x * Scale),(float)((y+1) * Scale), (float)(databuffer[index+maxX] * Scale));
                            Vector3 pointC = new Vector3((float)((x+1) * Scale),(float)(y * Scale), (float)(databuffer[index+1] * Scale));
                            Vector3 pointD = new Vector3((float)((x+1) * Scale),(float)((y+1) * Scale), (float)(databuffer[index+1+maxX] * Scale));
                            outfile.WriteTriangle(pointA, pointB, pointC);
                            outfile.WriteTriangle(pointB, pointC, pointD);
                        }
                    }
                }
                else
                {
                    for (int y = 0; y < MaxY; ++y)
                    {
                        for (int x = 0; x < maxX; ++x)
                        {
                            int index = y * maxX + x;
                            double zvalue = databuffer[index];
                            outfile.WritePoint((float)(x * Scale), (float)(y * Scale), (float)(zvalue * Scale));
                        }
                    }
                }
            }
            //if we want to do triangles, we need:
            //x,y, x+1,y, x,y+1, x+1,y, x, y+1, x+1, y+1.
            Console.WriteLine($"mend");
        }

    }
}
