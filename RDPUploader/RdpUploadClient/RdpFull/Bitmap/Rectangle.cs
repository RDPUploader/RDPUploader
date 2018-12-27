using System;

namespace RdpUploadClient
{
    internal class Rectangle
    {
        private int m_Bottom;
        private int m_Height;
        private int m_Right;
        private int m_Width;
        private int m_X;
        private int m_Y;

        public Rectangle(int X, int Y, int Width, int Height)
        {
            this.m_X = X;
            this.m_Y = Y;
            this.m_Width = Width;
            this.m_Height = Height;
            this.m_Right = X + Width;
            this.m_Bottom = Y + Height;
        }

        public void Clip(ref int x, ref int y, ref int cx, ref int cy)
        {
            int right = (x + cx) - 1;
            if (right > this.Right)
            {
                right = this.Right;
            }
            if (x < this.Left)
            {
                x = this.Left;
            }
            cx = (right - x) + 1;
            int bottom = (y + cy) - 1;
            if (bottom > this.Bottom)
            {
                bottom = this.Bottom;
            }
            if (y < this.Top)
            {
                y = this.Top;
            }
            cy = (bottom - y) + 1;
        }

        public Rectangle Clone()
        {
            return new Rectangle(this.X, this.Y, this.Width, this.Height);
        }

        public bool Contains(int x, int y)
        {
            if (x < this.X)
            {
                return false;
            }
            if (y < this.Y)
            {
                return false;
            }
            if (x > this.Right)
            {
                return false;
            }
            if (y > this.Bottom)
            {
                return false;
            }
            return true;
        }

        public static Rectangle Union(Rectangle a, Rectangle b)
        {
            int x = Math.Min(a.m_X, b.m_X);
            int num2 = Math.Max(a.m_Right, b.m_Right);
            int y = Math.Min(a.m_Y, b.m_Y);
            int num4 = Math.Max(a.m_Bottom, b.m_Bottom);
            return new Rectangle(x, y, num2 - x, num4 - y);
        }

        public int Bottom
        {
            get
            {
                return this.m_Bottom;
            }
            set
            {
                this.m_Bottom = value;
                this.m_Height = this.m_Bottom - this.m_Y;
            }
        }

        public int Height
        {
            get
            {
                return this.m_Height;
            }
            set
            {
                this.m_Height = value;
                this.m_Bottom = this.m_Y + this.m_Height;
            }
        }

        public int Left
        {
            get
            {
                return this.m_X;
            }
            set
            {
                this.X = value;
            }
        }

        public int Right
        {
            get
            {
                return this.m_Right;
            }
            set
            {
                this.m_Right = value;
                this.m_Width = this.m_Right - this.m_X;
            }
        }

        public int Top
        {
            get
            {
                return this.m_Y;
            }
            set
            {
                this.Y = value;
            }
        }

        public int Width
        {
            get
            {
                return this.m_Width;
            }
            set
            {
                this.m_Width = value;
                this.m_Right = this.m_X + this.m_Width;
            }
        }

        public int X
        {
            get
            {
                return this.m_X;
            }
            set
            {
                this.m_X = value;
                this.m_Right = this.m_X + this.m_Width;
            }
        }

        public int Y
        {
            get
            {
                return this.m_Y;
            }
            set
            {
                this.m_Y = value;
                this.m_Bottom = this.m_Y + this.m_Height;
            }
        }

    }
}