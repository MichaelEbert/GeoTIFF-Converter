using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace GeoTIFFConverter
{
    class STLExporter : IDisposable
    {
        bool binary = false;
        FileStream file;
        int triCount = 0;
        int triCountOffset = 80;//byte 80

        public STLExporter(string filename, bool binary = true)
        {
            this.binary = binary;
            file = new FileStream(filename, FileMode.Create, FileAccess.Write);
            //header
            if (binary)
            {
                byte[] bytes = new byte[80];
                file.Write(bytes, 0, 80);
                file.Write(bytes, 0, 4);
            }
            else
            {
                write("solid stlobject\n");
            }
        }

        public void WritePoint(float x, float y, float z)
        {
            if (binary)
            {
                write(0.0F);
                write(0.0F);
                write(0.0F);
                //point 1
                write((float)x);
                write((float)y);
                write((float)z);
                //point 2
                write((float)x);
                write((float)y);
                write((float)z);
                //point 3
                write((float)x);
                write((float)y);
                write((float)z);
                file.WriteByte(0);
                file.WriteByte(0);
                triCount++;
            }
            else
            {
                write("  facet normal 0 0 0\n");
                write("    outer loop\n");
                write($"      vertex {x} {y} {z}\n");
                write($"      vertex {x} {y} {z}\n");
                write($"      vertex {x} {y} {z}\n");
                write("    endloop\n");
                write("  endfacet\n");
            }
        }

        public void WriteTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            if (binary)
            {
                write(0.0F);
                write(0.0F);
                write(0.0F);
                write(a.X);
                write(a.Y);
                write(a.Z);
                write(b.X);
                write(b.Y);
                write(b.Z);
                write(c.X);
                write(c.Y);
                write(c.Z);
                file.WriteByte(0);
                file.WriteByte(0);
                triCount++;
            }
            else
            {
                write("  facet normal 0 0 0\n");
                write("    outer loop\n");
                write($"      vertex {a.X} {a.Y} {a.Z}\n");
                write($"      vertex {b.X} {b.Y} {b.Z}\n");
                write($"      vertex {c.X} {c.Y} {c.Z}\n");
                write("    endloop\n");
                write("  endfacet\n");
            }
        }

        private void write(int intToWrite)
        {
            byte[] bytes = BitConverter.GetBytes(intToWrite);
            file.Write(bytes, 0, bytes.Length);
        }
        private void write(float floatToWrite)
        {
            byte[] bytes = BitConverter.GetBytes(floatToWrite);
            file.Write(bytes, 0, bytes.Length);
        }
        private void write(string stringToWrite)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(stringToWrite);
            file.Write(bytes, 0, bytes.Length);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (binary)
                    {
                        file.Seek(triCountOffset, SeekOrigin.Begin);
                        write(triCount);
                    }
                    else
                    {
                        write("endsolid stlobject");
                    }
                    file.Close();
                    file.Dispose();
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }
        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
