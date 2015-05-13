﻿using System;
using System.Runtime.InteropServices;
using System.Text;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Core.MediaFiles.MediaInfo
{
    public enum StreamKind
    {
        General,
        Video,
        Audio,
        Text,
        Other,
        Image,
        Menu,
    }

    public enum InfoKind
    {
        Name,
        Text,
        Measure,
        Options,
        NameText,
        MeasureText,
        Info,
        HowTo
    }

    public enum InfoOptions
    {
        ShowInInform,
        Support,
        ShowInSupported,
        TypeOfValue
    }

    public enum InfoFileOptions
    {
        FileOption_Nothing = 0x00,
        FileOption_NoRecursive = 0x01,
        FileOption_CloseAll = 0x02,
        FileOption_Max = 0x04
    };


    public class MediaInfo : IDisposable
    {
        private IntPtr _handle;

        public bool MustUseAnsi { get; set; }
        public Encoding Encoding { get; set; }

        public MediaInfo()
        {
            _handle = MediaInfo_New();

            InitializeEncoding();
        }

        ~MediaInfo()
        {
            MediaInfo_Delete(_handle);
        }

        public void Dispose()
        {
            MediaInfo_Delete(_handle);
            GC.SuppressFinalize(this);
        }

        private void InitializeEncoding()
        {
            if (Environment.OSVersion.ToString().IndexOf("Windows") != -1)
            {
                // Windows guaranteed UCS-2
                MustUseAnsi = false;
                Encoding = Encoding.Unicode;
            }
            else
            {
                // Linux normally UCS-4. As fallback we try UCS-2 and plain Ansi.
                MustUseAnsi = false;
                Encoding = Encoding.UTF32;

                if (Option("Info_Version", "").StartsWith("MediaInfoLib"))
                {
                    return;
                }
                
                Encoding = Encoding.Unicode;

                if (Option("Info_Version", "").StartsWith("MediaInfoLib"))
                {
                    return;
                }

                MustUseAnsi = true;
                Encoding = Encoding.Default;

                if (Option("Info_Version", "").StartsWith("MediaInfoLib"))
                {
                    return;
                }

                throw new NotSupportedException("Unsupported MediaInfoLib encoding");
            }
        }

        private IntPtr MakeStringParameter(string value)
        {
            var buffer = Encoding.GetBytes(value);

            Array.Resize(ref buffer, buffer.Length + 4);

            var buf = Marshal.AllocHGlobal(buffer.Length);
            Marshal.Copy(buffer, 0, buf, buffer.Length);

            return buf;
        }

        private string MakeStringResult(IntPtr value)
        {
            if (Encoding == Encoding.Unicode)
            {
                return Marshal.PtrToStringUni(value);
            }
            else if (Encoding == Encoding.UTF32)
            {
                int i = 0;
                for (; i < 1024; i+=4)
                {
                    var data = Marshal.ReadInt32(value, i);
                    if (data == 0)
                    {
                        break;
                    }
                }

                var buffer = new byte[i];
                Marshal.Copy(value, buffer, 0, i);

                return Encoding.GetString(buffer, 0, i);
            }
            else
            {
                return Marshal.PtrToStringAnsi(value);
            }
        }
        public int Open(string fileName)
        {
            var pFileName = MakeStringParameter(fileName);
            try
            {
                if (MustUseAnsi)
                {
                    return (int)MediaInfoA_Open(_handle, pFileName);
                }
                else
                {
                    return (int)MediaInfo_Open(_handle, pFileName);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pFileName);
            }
        }

        public void Close()
        {
            MediaInfo_Close(_handle);
        }

        public string Get(StreamKind streamKind, int streamNumber, string parameter, InfoKind infoKind = InfoKind.Text, InfoKind searchKind = InfoKind.Name)
        {
            var pParameter = MakeStringParameter(parameter);
            try
            {
                if (MustUseAnsi)
                {
                    return MakeStringResult(MediaInfoA_Get(_handle, (IntPtr)streamKind, (IntPtr)streamNumber, pParameter, (IntPtr)infoKind, (IntPtr)searchKind));
                }
                else
                {
                    return MakeStringResult(MediaInfo_Get(_handle, (IntPtr)streamKind, (IntPtr)streamNumber, pParameter, (IntPtr)infoKind, (IntPtr)searchKind));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pParameter);
            }
        }

        public string Get(StreamKind streamKind, int streamNumber, int parameter, InfoKind infoKind)
        {
            if (MustUseAnsi)
            {
                return MakeStringResult(MediaInfoA_GetI(_handle, (IntPtr)streamKind, (IntPtr)streamNumber, (IntPtr)parameter, (IntPtr)infoKind));
            }
            else
            {
                return MakeStringResult(MediaInfo_GetI(_handle, (IntPtr)streamKind, (IntPtr)streamNumber, (IntPtr)parameter, (IntPtr)infoKind));
            }
        }

        public String Option(String option, String value)
        {
            var pOption = MakeStringParameter(option);
            var pValue = MakeStringParameter(value);
            try
            {
                if (MustUseAnsi)
                {
                    return MakeStringResult(MediaInfoA_Option(_handle, pOption, pValue));
                }
                else
                {
                    return MakeStringResult(MediaInfo_Option(_handle, pOption, pValue));
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pOption);
                Marshal.FreeHGlobal(pValue);
            }
        }

        public int State_Get()
        {
            return (int)MediaInfo_State_Get(_handle);
        }

        public int Count_Get(StreamKind streamKind, int streamNumber = -1)
        {
            return (int)MediaInfo_Count_Get(_handle, (IntPtr)streamKind, (IntPtr)streamNumber);
        }

        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_New();
        [DllImport("MediaInfo.dll")]
        private static extern void MediaInfo_Delete(IntPtr handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Open(IntPtr handle, IntPtr fileName);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Open(IntPtr handle, IntPtr fileName);
        [DllImport("MediaInfo.dll")]
        private static extern void MediaInfo_Close(IntPtr handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr infoKind);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_GetI(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr infoKind);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr infoKind, IntPtr searchKind);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Get(IntPtr handle, IntPtr streamKind, IntPtr streamNumber, IntPtr parameter, IntPtr infoKind, IntPtr searchKind);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Option(IntPtr handle, IntPtr option, IntPtr value);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfoA_Option(IntPtr handle, IntPtr option, IntPtr value);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_State_Get(IntPtr handle);
        [DllImport("MediaInfo.dll")]
        private static extern IntPtr MediaInfo_Count_Get(IntPtr handle, IntPtr StreamKind, IntPtr streamNumber);

    }
}
