using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unio
{
    public class NetData
    {
        public int serial;
        public string uuid;
        public int[] data;
    }

    public class Data
    {
        public int serial;
        public string uuid;
        public byte[] data;
    }

    public class DataConverter
    {
        public static Data Convert(NetData rd)
        {
            if (rd == null || rd.uuid == null || rd.uuid == "" || rd.data == null)
                return null;

            Data data = new Data();
            data.serial = rd.serial;
            data.uuid = rd.uuid.ToLower();
            data.data = new byte[rd.data.Length];
            for (int i = 0; i < rd.data.Length; i++)
            {
                if (rd.data[i] < 0)
                    rd.data[i] = 0;
                else if (rd.data[i] > 255)
                    rd.data[i] = 255;

                data.data[i] = (byte)rd.data[i];
            }

            return data;
        }

        public static NetData Convert(Data rd)
        {
            if (rd == null || rd.uuid == null || rd.uuid == "" || rd.data == null)
                return null;

            NetData data = new NetData();
            data.serial = rd.serial;
            data.uuid = rd.uuid.ToUpper();
            data.data = new int[rd.data.Length];
            for (int i = 0; i < rd.data.Length; i++)
            {
                data.data[i] = rd.data[i];
            }

            return data;
        }
    }
}