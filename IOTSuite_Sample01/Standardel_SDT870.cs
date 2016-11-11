using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IOTSuite_Sample01.YFSensorModel;

namespace IOTSuite_Sample01
{
    public class Standardel_SDT870 : ModelBase
    {
        private ModbusRTU rtu = null;
        public string SDT870CommState { get; set; } = "0";
        public string SDT870P { get; set; } = "1";
        public string SDT870T { get; set; } = "0";

        public Standardel_SDT870(ModbusRTU rtu)
        {
            this.rtu = rtu;
        }

        private JObject ParseData(UInt16[] buffer)
        {
            JObject changedDataProps = new JObject();

            string[] infos = new string[12];
            if (buffer != null && buffer.Length == 12)
            {
                //10-11   : 有功电能
                string power = string.Concat<UInt16>(new UInt16[] { buffer[10], buffer[11] });

                if (!SDT870P.Equals(power))
                {
                    SDT870P = power;
                    changedDataProps.Add("SDT870P", (buffer[10] * 65536 + buffer[11]) / 100);
                }

            }
            return changedDataProps;
        }

        override protected JObject WriteAndRead()
        {

            UInt16[] buffer = new UInt16[12];
            JObject rmtd = new JObject();

            if (rtu.Read(1, ModbusType.V, 0, buffer, (UInt16)buffer.Length) == 0)
            {
                rmtd = ParseData(buffer);

                rmtd.AddFirst(new JProperty("SDT870CommState", "0"));
            }

            return rmtd;
        }

    }
}
