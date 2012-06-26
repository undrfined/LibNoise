// This file is part of libnoise-dotnet.
//
// libnoise-dotnet is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// libnoise-dotnet is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with libnoise-dotnet.  If not, see <http://www.gnu.org/licenses/>.

namespace LibNoise.Demo.Ext.Dotnet
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Runtime.InteropServices;
    using LibNoise.Renderer;
    using Color = LibNoise.Renderer.Color;

    /// <summary>
    /// Implements an image, a 2-dimensional array of color values.
    ///
    /// An image can be used to store a color texture.
    ///
    /// These color values are of type IColor.
    /// 
    /// TODO Implement unimplemented method
    /// TODO Create a dotnet projet for this extension
    /// Utiliser lockbits
    /// http://msdn.microsoft.com/fr-fr/library/5ey6h79d.aspx
    /// </summary>
    public class BitmapAdaptater : IMap2D<IColor>
    {
        #region Fields

        /// <summary>
        /// The bitmap
        /// </summary>
        protected Bitmap Adaptatee;

        /// <summary>
        /// Flags that indicates if some bitmap changes need to be applied.
        /// As is an expansive operation, BitmapAdaptater.apply is only called
        /// in the Bitmap accessor if changes have been previously done
        /// </summary>
        protected bool BitsLocked = false;

        /// <summary>
        /// Bitmap information
        /// </summary>
        protected BitmapData BmData;

        /// <summary>
        /// The value used for all positions outside of the map.
        /// </summary>
        protected IColor _borderValue;

        /// <summary>
        /// Internal data buffer for performances purpose
        /// </summary>
        protected byte[] Data;

        /// <summary>
        /// The height of the map, internal use
        /// </summary>
        protected int _height = 0;

        /// <summary>
        /// Size in byte of one pixel data
        /// </summary>
        protected byte _structSize = 1;

        /// <summary>
        /// The width of the map, internal use
        /// </summary>
        protected int _width = 0;

        #endregion

        #region Accessors

        /// <summary>
        /// Gets the adaptated System.Drawing.Bitmap
        /// </summary>
        public Bitmap Bitmap
        {
            get
            {
                if (BitsLocked)
                    Apply();

                return Adaptatee;
            }
            /*
			set { 
				_adaptatee = value; 
				_width = _adaptatee.Width;
				_height = _adaptatee.Height;
			}
			*/
        }

        /// <summary>
        /// Gets the width of the map
        /// </summary>
        public int Width
        {
            get { return _width; }
        }


        /// <summary>
        /// Gets the height of the map
        /// </summary>
        public int Height
        {
            get { return _height; }
        }


        /// <summary>
        /// Gets the border value of the map
        /// </summary>
        public IColor BorderValue
        {
            get { return _borderValue; }
            set { _borderValue = value; }
        }

        #endregion

        #region Ctor/Dtor

        /// <summary>
        /// Create a new Bitmap with the given values
        /// </summary>
        /// <param name="bitmap">The bitmap to adapt.</param>
        public BitmapAdaptater(Bitmap bitmap)
        {
            switch (bitmap.PixelFormat)
            {
                case PixelFormat.Canonical: //RGBA
                case PixelFormat.Format8bppIndexed: //R
                case PixelFormat.Format24bppRgb: // RGB
                case PixelFormat.Format32bppRgb: // RGB_
                case PixelFormat.Format32bppArgb: //RGBA
                    //ok

                    break;
                default:
                    throw new ArgumentException("Unsupported image format : " + bitmap.PixelFormat.ToString());
            }

            Adaptatee = bitmap;
            _borderValue = Color.WHITE;

            _width = Adaptatee.Width;
            _height = Adaptatee.Height;

            AllocateBuffer();
        }


        /// <summary>
        /// Create a new Bitmap with the given values
        /// </summary>
        /// <param name="width">The width of the new bitmap.</param>
        /// <param name="height">The height of the new bitmap</param>
        public BitmapAdaptater(int width, int height)
        {
            Adaptatee = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            _borderValue = Color.WHITE;

            _width = Adaptatee.Width;
            _height = Adaptatee.Height;

            AllocateBuffer();
        }

        #endregion

        #region IMap2D<IColor> Members

        /// <summary>
        /// Gets a value at a specified position in the map.
        ///
        /// This method does nothing if the map object is empty or the
        /// position is outside the bounds of the noise map.
        /// </summary>
        /// <param name="x">The x coordinate of the position</param>
        /// <param name="y">The y coordinate of the position</param>
        public IColor GetValue(int x, int y)
        {
            if (Adaptatee != null
                && (x >= 0 && x < _width)
                && (y >= 0 && y < _height)
                )
            {
                if (BitsLocked)
                {
                    // Noise.Image start to bottom left
                    // Drawing.Bitmap start to top left
                    int indexBase = BmData.Stride*(_height - 1 - y) + x*_structSize;

                    switch (BmData.PixelFormat)
                    {
                        case PixelFormat.Format8bppIndexed: //R
                            return new Color(Data[indexBase], Data[indexBase], Data[indexBase], 255);

                        case PixelFormat.Format24bppRgb: // RGB
                        case PixelFormat.Format32bppRgb: // RGB_
                            return new Color(Data[indexBase + 2], Data[indexBase + 1], Data[indexBase], 255);

                        case PixelFormat.Canonical: //RGBA
                        case PixelFormat.Format32bppArgb: //RGBA
                            return new Color(Data[indexBase + 2], Data[indexBase + 1], Data[indexBase],
                                Data[indexBase + 3]);
                    }
                }
                else
                {
                    // Noise.Image start to bottom left
                    // Drawing.Bitmap start to top left
                    System.Drawing.Color sysColor = Adaptatee.GetPixel(x, _height - 1 - y);
                    return new Color(sysColor.R, sysColor.G, sysColor.B, sysColor.A);
                }
            }

            return _borderValue;
        }


        /// <summary>
        /// Sets the new size for the map.
        /// 
        /// </summary>
        /// <param name="width">width The new width for the bitmap</param>
        /// <param name="height">height The new height for the bitmap</param>
        public void SetSize(int width, int height)
        {
            if (Adaptatee.Width != width || Adaptatee.Height != height)
                throw new NotImplementedException("System.Drawing.Bitmap does not support resize");
        }


        /// <summary>
        /// Resets the bitmap
        /// </summary>
        public void Reset()
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Clears the bitmap to a Color.WHITE value
        /// </summary>
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets a value at a specified position in the map.
        ///
        /// This method does nothing if the map object is empty or the
        /// position is outside the bounds of the noise map.
        /// </summary>
        /// <param name="x">The x coordinate of the position</param>
        /// <param name="y">The y coordinate of the position</param>
        /// <param name="value">The value to set at the given position</param>
        public void SetValue(int x, int y, IColor value)
        {
            if (Adaptatee != null
                && (x >= 0 && x < _width)
                && (y >= 0 && y < _height)
                )
            {
                if (BitsLocked)
                {
                    // Noise.Image start to bottom left
                    // Drawing.Bitmap start to top left
                    int indexBase = BmData.Stride*(_height - 1 - y) + x*_structSize;

                    switch (BmData.PixelFormat)
                    {
                        case PixelFormat.Format8bppIndexed: //R
                            Data[indexBase] = value.Red;

                            break;

                        case PixelFormat.Format24bppRgb: // RGB
                            Data[indexBase] = value.Blue;
                            Data[indexBase + 1] = value.Green;
                            Data[indexBase + 2] = value.Red;

                            break;

                        case PixelFormat.Format32bppRgb: // RGB_
                            Data[indexBase] = value.Blue;
                            Data[indexBase + 1] = value.Green;
                            Data[indexBase + 2] = value.Red;
                            Data[indexBase + 3] = 255;

                            break;

                        case PixelFormat.Canonical: //RGBA
                        case PixelFormat.Format32bppArgb: //RGBA
                            Data[indexBase] = value.Blue;
                            Data[indexBase + 1] = value.Green;
                            Data[indexBase + 2] = value.Red;
                            Data[indexBase + 3] = value.Alpha;

                            break;
                    }
                }
                else
                {
                    // Noise.Image start to bottom left
                    // Drawing.Bitmap start to top left
                    Adaptatee.SetPixel(
                        x,
                        Adaptatee.Height - 1 - y,
                        System.Drawing.Color.FromArgb(value.Alpha, value.Red, value.Green, value.Blue)
                        );
                }
            }
        }

        /// <summary>
        /// Clears the bitmap to a specified value.
        /// This method is a O(n) operation, where n is equal to width * height.
        /// </summary>
        /// <param name="color">The color that all positions within the bitmap are cleared to.</param>
        public void Clear(IColor color)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Find the lowest and highest value in the map
        /// </summary>
        /// <param name="min">the lowest value</param>
        /// <param name="max">the highest value</param>
        public void MinMax(out IColor min, out IColor max)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Internal

        /// <summary>
        /// Allocate a buffer
        /// </summary>
        protected void AllocateBuffer()
        {
            if (BitsLocked)
                throw new Exception("Buffer already allocated");

            try
            {
                switch (Adaptatee.PixelFormat)
                {
                    case PixelFormat.Canonical: //RGBA
                    case PixelFormat.Format32bppRgb: // RGB_
                    case PixelFormat.Format32bppArgb: //RGBA
                        _structSize = 4;
                        break;

                    case PixelFormat.Format8bppIndexed: //R
                        _structSize = 1;
                        break;

                    case PixelFormat.Format24bppRgb: // RGB
                        _structSize = 3;
                        break;

                    default:
                        throw new ArgumentException("Unsupported image format : " + Adaptatee.PixelFormat.ToString());
                }

                // Lock memory region
                var region = new Rectangle(0, 0, _width, _height);
                BmData = Adaptatee.LockBits(region, ImageLockMode.ReadWrite, Adaptatee.PixelFormat);

                // BitmapData.Stride could be a negative number
                int size = Math.Abs(BmData.Stride)*_height;

                // Create buffer
                if (Data == null)
                    Data = new byte[size];
                else
                    Array.Resize(ref Data, size);

                // Memcopy
                Marshal.Copy(BmData.Scan0, Data, 0, size);

                BitsLocked = true;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to lock bitmap memory", e);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        protected void Apply()
        {
            if (!BitsLocked)
                throw new Exception("Buffer is empty");

            try
            {
                // Memcopy
                Marshal.Copy(Data, 0, BmData.Scan0, Data.Length);

                // Unlock region
                Adaptatee.UnlockBits(BmData);

                BitsLocked = true;
                BmData = null;
                Data = null;
            }
            catch (Exception e)
            {
                throw new Exception("Unable to unlock bitmap memory", e);
            }
        }

        #endregion
    }
}
