using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SM64AppBase
{
    public class FontHelper
    {
        public static PrivateFontCollection pfc = new PrivateFontCollection();


        public static void AddFontFromResource(PrivateFontCollection privateFontCollection, byte[] Resource)
        {
            var fontData = Marshal.AllocCoTaskMem(Resource.Length);
            Marshal.Copy(Resource, 0, fontData, Resource.Length);
            privateFontCollection.AddMemoryFont(fontData, Resource.Length);
            Marshal.FreeCoTaskMem(fontData);
        }

        private static byte[] GetFontResourceBytes(Assembly assembly, string fontResourceName)
        {
            var resourceStream = assembly.GetManifestResourceStream(fontResourceName);
            if (resourceStream == null)
                throw new Exception(string.Format("Unable to find font '{0}' in embedded resources.", fontResourceName));
            var fontBytes = new byte[resourceStream.Length];
            resourceStream.Read(fontBytes, 0, (int)resourceStream.Length);
            resourceStream.Close();
            return fontBytes;
        }


    }
}
