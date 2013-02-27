using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Threading;

namespace AsfMojoUI.Model
{
    public static class IndexerHelper
    {
        public static Guid ToGuid(this byte[] guidBytes)
        {
            return new Guid(guidBytes);
        }
    }

    public static class DispatcherHelper
    {
        public static void DelayInvoke(this Dispatcher dispatcher, TimeSpan ts, Action action)
        {
            DispatcherTimer delayTimer = new DispatcherTimer(DispatcherPriority.Send, dispatcher);
            delayTimer.Interval = ts;
            delayTimer.Tick += (s, e) =>
            {
                delayTimer.Stop();
                action();
            };
            delayTimer.Start();
        }
    }

    public static class ListHelper
    {
        public static int BinarySearchForMatch<T>(this IList<T> list,
            Func<T, int> comparer)
        {
            int min = 0;
            int max = list.Count - 1;

            while (min < max)
            {
                int mid = (min + max) / 2;
                int comparison = comparer(list[mid]);
                if (comparison == 0)
                {
                    return mid;
                }
                if (comparison < 0)
                {
                    min = mid + 1;
                }
                else
                {
                    max = mid - 1;
                }
            }
            return min;
        }

        public static int FullSearchForMatch<T>(this IList<T> list, Func<T, int> match)
        {
            for (int index = 0; index < list.Count; index++)
            {
                int comparison = match(list[index]);
                if (comparison == 0)
                    return index;

            }
            return -1;
        }

        public static int LinearSearchForMatch<T>(this IList<T> list, int start, Func<T, int> match)
        {
            int index = start;


            while (index < list.Count && index>=0)
            {
                int comparison = match(list[index]);
                if (comparison == 0)
                    return index;
                else index += comparison;
            }
            return -1; //not found
        }
    }

    public static class BitmapHelper
    {
        public static BitmapSource ToBitmapSource(this System.Drawing.Bitmap source)
        {
            BitmapSource bitSrc = null;

            var hBitmap = source.GetHbitmap();

            try
            {
                bitSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch (Win32Exception)
            {
                bitSrc = null;
            }
            finally
            {
                NativeMethods.DeleteObject(hBitmap);
            }

            return bitSrc;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr hObject);
    }

}
