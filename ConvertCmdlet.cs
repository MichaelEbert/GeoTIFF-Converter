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

        [Parameter(HelpMessage = "Output STL file as text")]
        public SwitchParameter Text = false;

        [Parameter]
        public double Scale = 0.1;

        [Parameter(HelpMessage = "Minimum Y value to convert")]
        public double YMin = -1.0;

        [Parameter(HelpMessage = "Maximum Y value to convert")]
        public double YMax = -1.0;

        [Parameter(HelpMessage = "Minimum X value to convert")]
        public double XMin = -1.0;

        [Parameter(HelpMessage = "Maximum X value to convert")]
        public double XMax = -1.0;

        [Parameter(HelpMessage = "Output every Nth row and column")]
        public int Stride = 1;

        [Parameter]
        public SwitchParameter Stats = false;


        protected override void BeginProcessing()
        {
            GdalConfiguration.ConfigureGdal();
            System.Console.WriteLine("Starting...");
            Gdal.AllRegister();
            if (Destination == null)
            {
                Destination = System.IO.Path.GetDirectoryName(Path) + "/Output.stl";
            }
        }

        protected override void ProcessRecord()
        {
            var dataset = Gdal.Open(Path, Access.GA_ReadOnly);
            Driver drv = dataset.GetDriver();

            //Console.WriteLine("number of things is:");
            //Console.WriteLine(dataset.RasterCount);
            //band numbers start at 1
            var heightdata = dataset.GetRasterBand(1);
            double[] minmax = new double[2];
            heightdata.ComputeRasterMinMax(minmax, 0);
            Console.WriteLine($"minmax is {minmax[0]}, {minmax[1]}");

            if (Stats)
            {
                var statblock = new Dictionary<string, object>();
                statblock.Add("XSize", heightdata.XSize);
                statblock.Add("YSize", heightdata.YSize);
                statblock.Add("ZMin", minmax[0]);
                statblock.Add("ZMax", minmax[1]);
                WriteObject(statblock);
                return;
            }

            //decide what max and min index should be
            int YMinIndex;
            int YMaxIndex;
            (YMinIndex, YMaxIndex) = GetMinMax(YMin, YMax, heightdata.YSize);
            int XMinIndex;
            int XMaxIndex;
            (XMinIndex, XMaxIndex) = GetMinMax(XMin, XMax, heightdata.XSize);

            int YIndexSize = YMaxIndex - YMinIndex;
            int XIndexSize = XMaxIndex - XMinIndex;
            //our buffer indices go from 0 to xxx
            int bufferXIndexSize = XIndexSize / Stride;
            int bufferYIndexSize = YIndexSize / Stride;

            //create a buffer to hold data in memory.
            double[] databuffer = new double[bufferXIndexSize * bufferYIndexSize];

            heightdata.ReadRaster(XMinIndex, YMinIndex, XIndexSize, YIndexSize, databuffer, bufferXIndexSize, bufferYIndexSize, 0, 0);
            //misc stuff
            heightdata.GetNoDataValue(out double nodataval, out int hasnodataval);
            Console.WriteLine($"has nodata val: {hasnodataval}, is {nodataval}");
            var thing = heightdata.GetRasterColorInterpretation();
            var thing2 = Gdal.GetDataTypeSize(heightdata.DataType);

            using (var outfile = new STLExporter(Destination, !Text))
            {
                if (RenderFaces)
                {
                    for (int bufferYIndex = 0; bufferYIndex < bufferYIndexSize - 1; ++bufferYIndex)
                    {
                        for (int bufferXIndex = 0; bufferXIndex < bufferXIndexSize - 1; ++bufferXIndex)
                        {
                            int index = bufferYIndex * bufferXIndexSize + bufferXIndex;
                            int xValue = bufferXIndex * Stride + XMinIndex;
                            int yValue = bufferYIndex * Stride + YMinIndex;
                            double zvalue = databuffer[index];

                            Vector3 pointA = new Vector3((float)(xValue * Scale), (float)(yValue * Scale), (float)(zvalue * Scale));
                            Vector3 pointB = new Vector3((float)(xValue * Scale), (float)((yValue + Stride) * Scale), (float)(databuffer[index + bufferXIndexSize] * Scale));
                            Vector3 pointC = new Vector3((float)((xValue + Stride) * Scale), (float)(yValue * Scale), (float)(databuffer[index + 1] * Scale));
                            Vector3 pointD = new Vector3((float)((xValue + Stride) * Scale), (float)((yValue + Stride) * Scale), (float)(databuffer[index + 1 + bufferXIndexSize] * Scale));
                            outfile.WriteTriangle(pointA, pointB, pointC);
                            outfile.WriteTriangle(pointB, pointC, pointD);
                        }
                    }
                }
                else
                {
                    for (int bufferYIndex = 0; bufferYIndex < bufferYIndexSize; ++bufferYIndex)
                    {
                        for (int bufferXIndex = 0; bufferXIndex < bufferXIndexSize; ++bufferXIndex)
                        {
                            int index = bufferYIndex * bufferXIndexSize + bufferXIndex;
                            int xValue = bufferXIndex * Stride + XMinIndex;
                            int yValue = bufferYIndex * Stride + YMinIndex;
                            double zvalue = databuffer[index];
                            outfile.WritePoint((float)(xValue * Scale), (float)(yValue * Scale), (float)(zvalue * Scale));
                        }
                    }
                }
            }
            //if we want to do triangles, we need:
            //x,y, x+1,y, x,y+1, x+1,y, x, y+1, x+1, y+1.
            Console.WriteLine($"end");
        }

        private (int MinIndex, int MaxIndex) GetMinMax(double Min, double Max, int Size)
        {
            int MaxIndex;
            int MinIndex;
            //YMax
            if (Max >= 1.0)
            {
                MaxIndex = (int)Max;
            }
            else if (Max >= 0.0)
            {
                MaxIndex = (int)(Max * Size);
            }
            else
            {
                MaxIndex = Size;
            }

            if (Min >= 1.0)
            {
                MinIndex = (int)Min;
            }
            else if (Min >= 0.0)
            {
                MinIndex = (int)(Min * Size);
            }
            else
            {
                MinIndex = 0;
            }

            return (MinIndex, MaxIndex);
        }
    }

}
