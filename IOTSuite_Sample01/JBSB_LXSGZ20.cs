using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IOTSuite_Sample01.YFSensorModel;

namespace IOTSuite_Sample01
{
    public class JBSB_LXSGZ20 : ModelBase
    {
        private SR188 sr118 = null;
        public string LXSGZ20CommState { get; set; } = "0";
        public string LXSGZ20Ctl { get; set; } = "0";
        public string LXSGZ20Flow { get; set; } = "0";
        public string LXSGZ20State { get; set; } = "0";

        public JBSB_LXSGZ20(SR188 sr118)
        {
            this.sr118 = sr118;
        }

        override protected JObject WriteAndRead()
        {

            JObject rmtd = new JObject();
            float flow = sr118.ReadData(SR188.MeterAddr.BroadcastAddress);
            //Debug.WriteLine("jbsb data is :  \r\n"+ flow);

            if (flow >= 0)
            {
                if (!flow.ToString().Equals(LXSGZ20Flow))
                {
                    LXSGZ20Flow = flow.ToString();
                    rmtd.Add("LXSGZ20Flow", LXSGZ20Flow);
                }

                rmtd.AddFirst(new JProperty("LXSGZ20CommState", "0"));
            }

            return rmtd;
        }

    }
}
