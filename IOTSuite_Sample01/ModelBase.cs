using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;
using static IOTSuite_Sample01.YFSensorModel;

namespace IOTSuite_Sample01
{
    public class ModelBase
    {
        public string ProjectId { get; set; } = "23";
        public string DeviceId { get; set; } = "Device006";

        virtual protected JObject WriteAndRead() { return null; }

        public async Task AutoRead(int freq, int timeout, BlockingCollection<JObject> _blockQueue)
        {
            DateTime dt0 = DateTime.Now;

            while (true)
            {
                await Task.Delay(freq);

                try
                {
                    JObject rmtd = WriteAndRead();

                    //  error
                    if (rmtd.Count == 0) continue;

                    rmtd.AddFirst(new JProperty("ProjectId", ProjectId));
                    rmtd.AddFirst(new JProperty("DeviceId", DeviceId));

                    //  no change , check for timeout
                    if (rmtd.Count == 3)
                    {
                        TimeSpan lastTime = DateTime.Now - dt0;
                        if (lastTime.TotalMilliseconds > timeout)
                        {
                            dt0 = DateTime.Now;
                            _blockQueue.Add(rmtd);
                        }
                    }

                    //  data changed
                    if (rmtd.Count > 3)
                    {
                        dt0 = DateTime.Now;
                        _blockQueue.Add(rmtd);
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("error , read serial  port.  \r\n");
                }
            }
        }
    }
}