using System;

namespace RdpUploadClient
{
    internal class ChangedRect
    {
        internal static Rectangle _b = new Rectangle(0, 0, 0, 0);
        private static Rectangle changeRect = new Rectangle(0, 0, 0, 0);

        internal static Rectangle Clone()
        {
            if (IsEmpty())
            {
                return changeRect;
            }
            return new Rectangle(changeRect.X, changeRect.Y, changeRect.Width, changeRect.Height);
        }

        internal static void Invalidate(int x, int y, int cx, int cy)
        {
            if (IsEmpty())
            {
                changeRect = new Rectangle(x, y, cx, cy);
            }
            else
            {
                changeRect = Rectangle.Union(changeRect, new Rectangle(x, y, cx, cy));
            }
        }

        public static bool IsEmpty()
        {
            if (changeRect.Width != 0)
            {
                return (changeRect.Height == 0);
            }
            return true;
        }

        internal static void Reset()
        {
            changeRect = new Rectangle(0, 0, 0, 0);
        }

    }
}