using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Windows.Storage.Streams;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;


namespace IOTSuite_Sample01
{
    public class YFSensorModel : ModelBase
    {

        private ModbusRTU rtu = null;

        public string YFSECommState { get; set; } = "0";
        public string YFSECO2 { get; set; } = "0";
        public string YFSEH { get; set; } = "0";
        public string YFSELux { get; set; } = "0";
        public string YFSENH3 { get; set; } = "0";
        public string YFSEO2 { get; set; } = "0";
        public string YFSEPM25 { get; set; } = "0";
        public string YFSET1 { get; set; } = "0";
        public string YFSET2 { get; set; } = "0";
        public string YFSET3 { get; set; } = "0";
        public YFSensorModel(ModbusRTU rtu)
        {
            this.rtu = rtu;
        }

        private JObject ParseData(UInt16[] buffer)
        {
            JObject changedDataProps = new JObject();

            string[] infos = new string[10];
            if (buffer != null && buffer.Length == 10)
            {
                //00-01   : 光照
                //02-03   : 湿度*100
                //04-05   : 二氧化碳 400-5000    
                //06-07   : 氨气 
                //08-09   : PM2.5   
                //10-11   : 氧气*100 （从传感器取出的就是*10的值了） 
                //12-13   : 温度1*100 
                //14-15   : 温度2*100
                //16-17   : 温度3*100  
                //18-19   : 内部温度*100
                infos[0] = (buffer[0]).ToString();
                if (!infos[0].Equals(YFSELux))
                {
                    YFSELux = infos[0];
                    changedDataProps.Add("YFSELux", infos[0]);
                }
                infos[1] = (buffer[1] / 100.0).ToString("F2"); 
                if (!infos[1].Equals(YFSEH))
                {
                    YFSEH = infos[1];
                    changedDataProps.Add("YFSEH", infos[1]);
                }
                infos[2] = buffer[2].ToString(); 
                if (!infos[2].Equals(YFSECO2))
                {
                    YFSECO2 = infos[2];
                    changedDataProps.Add("YFSECO2", infos[2]);
                }

                infos[3] = buffer[3].ToString(); 
                if (!infos[3].Equals(YFSENH3))
                {
                    YFSENH3 = infos[3];
                    changedDataProps.Add("YFSENH3", infos[3]);
                }
                infos[4] = buffer[4].ToString(); 
                if (!infos[4].Equals(YFSEPM25))
                {
                    YFSEPM25 = infos[4];
                    changedDataProps.Add("YFSEPM25", infos[4]);
                }
                infos[5] = (buffer[5] / 100.0).ToString("F2"); 
                if (!infos[5].Equals(YFSEO2))
                {
                    YFSEO2 = infos[5];
                    changedDataProps.Add("YFSEO2", infos[5]);
                }
                infos[6] = (buffer[6] / 100.0).ToString("F2"); 
                if (!infos[6].Equals(YFSET1))
                {
                    YFSET1 = infos[6];
                    changedDataProps.Add("YFSET1", infos[6]);
                }
                infos[7] = (buffer[7] / 100.0).ToString("F2"); 
                if (!infos[7].Equals(YFSET2))
                {
                    YFSET2 = infos[7];
                    changedDataProps.Add("YFSET2", infos[7]);
                }
                infos[8] = (buffer[8] / 100.0).ToString("F2"); 
                if (!infos[8].Equals(YFSET3))                                                          
                {
                    YFSET3 = infos[8];
                    changedDataProps.Add("YFSET3", infos[8]);
                }
                //infos[9] = (buffer[9] / 100.0).ToString("F2");
                //if (!infos[9].Equals(YFSET3))
                //{
                //    YFSET3 = infos[9];
                //    changedDataProps.Add("YFSET3", infos[9]);
                //}

            }
            return changedDataProps;
        }

        override protected JObject WriteAndRead()
        {

            UInt16[] buffer = new UInt16[10];
            JObject rmtd = new JObject();

            if (rtu.Read(1, ModbusType.V, 0, buffer, (UInt16)buffer.Length) == 0)
            {
                rmtd = ParseData(buffer);
                rmtd.AddFirst(new JProperty("YFSECommState", "0"));
            }

            return rmtd;
        }


    }
}
