using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using Windows.Devices.SerialCommunication;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IOTSuite_Sample01
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static string IOTHUB_RUI = "yfiotazure.azure-devices.net";
        private const string DeviceConnectionString =
            "HostName=yfiotazure.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=zT81MBO8uE8lMB3UWfjD2NcIBAYs2XEDUY+EXY9JgZU=";

        //private const string DeviceConnectionString =
        //    "HostName=hhnext.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=jjHgtQz3AtfnTn6p/I5zH9POHLg9f55WnYlPD4y0Sqw=";
        DeviceClient _deviceClient =
            DeviceClient.Create(IOTHUB_RUI, new DeviceAuthenticationWithRegistrySymmetricKey("Device006", "f04NqhzRUXYf8MDkem642M22WSN7MbEmK9Ozj2HIOYk="), TransportType.Amqp);

        //DeviceClient _deviceClient =
        //    DeviceClient.Create(IOTHUB_RUI, new DeviceAuthenticationWithRegistrySymmetricKey("Device006", "A4neK6GT+miVzAuQXdbaqIZSoRWBtSwbKTfyySOk8L0="), TransportType.Amqp);


        public readonly BlockingCollection<JObject> _blockQueue = new BlockingCollection<JObject>();


        public const string DEVICEID = "Device006";
        public const string PROJECTID = "23";

        YFSensorModel YFSensorModelRTU = null;
        YFDI8Model yfDI8Model = null;
        YFDQ8Model yfDQ8Model = null;
        Standardel_SDT870 SDT870RTU = null;
        JBSB_LXSGZ20 LXSGZ20 = null;

        public MainPage()
        {
            this.InitializeComponent();

            //  yf DQ model
            SerialPort serialPort = new SerialPort();
            yfDQ8Model = new YFDQ8Model(new YFIOCustomProtocol(serialPort), UInt16.Parse("2"));
            //  yf DI model
            yfDI8Model = new YFDI8Model(new YFIOCustomProtocol(serialPort), UInt16.Parse("1"));

            Task.Run(() =>
             {
                 serialPort.Open("com5", 9600).ContinueWith(async t =>
                 {
                     //await yfDQ8Model.AutoRead(3000, 10000, _blockQueue);
                     await yfDI8Model.AutoRead(3000, 30000, _blockQueue);

                 });

             });

            Task.Run(async () =>
            {
                    await yfDQ8Model.AutoRead(3000, 30000, _blockQueue);

            });

            // JBSB_LXSGZ20 model
            SerialPort serialPort5 = new SerialPort();
            LXSGZ20 = new JBSB_LXSGZ20(new SR188(serialPort5));

            LXSGZ20.DeviceId = DEVICEID;
            LXSGZ20.ProjectId = PROJECTID;

            Task.Run(() =>
            {
                serialPort5.Open("com8", 1200, SerialParity.Even).ContinueWith(t =>
                LXSGZ20.AutoRead(3000, 30000, _blockQueue));
            });

            // yf sensor model
            SerialPort serialPort2 = new SerialPort();
            YFSensorModelRTU = new YFSensorModel(new ModbusRTU(serialPort2));

            YFSensorModelRTU.DeviceId = DEVICEID;
            YFSensorModelRTU.ProjectId = PROJECTID;
            Task.Run(() =>
           {
               serialPort2.Open("com6", 9600).ContinueWith(t =>
               YFSensorModelRTU.AutoRead(3000, 30000, _blockQueue));
           });

            // sdt870 power model 
            SerialPort serialPort3 = new SerialPort();
            SDT870RTU = new Standardel_SDT870(new ModbusRTU(serialPort3));

            SDT870RTU.DeviceId = DEVICEID;
            SDT870RTU.ProjectId = PROJECTID;

            Task.Run(() =>
            {
                serialPort3.Open("com7", 9600).ContinueWith(t =>
                SDT870RTU.AutoRead(3000, 30000, _blockQueue));
            });



            Task.Run(async () =>
                {
                    await ReceiveCommands(_deviceClient);
                });


            Task.Run(async () =>
               {
                   JObject rmtd = null;
                   while (true)
                   {
                       if (!_blockQueue.TryTake(out rmtd, 30000))
                       {
                           rmtd = new JObject();
                           rmtd.AddFirst(new JProperty("ProjectId", PROJECTID));
                           rmtd.AddFirst(new JProperty("DeviceId", DEVICEID));
                       }

                       Debug.WriteLine("get data from  queue \r\n");

                       var msgString = JsonConvert.SerializeObject(rmtd);
                       var msg2 = new Message(Encoding.ASCII.GetBytes(msgString));

                       await _deviceClient.SendEventAsync(msg2);

                       Debug.WriteLine("\t{0}> Sending message: {1}, Data: [{2}]", DateTime.Now.ToLocalTime(), 0, msgString);
                   }
               });


        }

        async Task ReceiveCommands(DeviceClient deviceClient)
        {
            Message receivedMessage;
            string messageData;
            JObject jsonMmessageData = null;

            while (true)
            {
                receivedMessage = await deviceClient.ReceiveAsync();
                //Debug.WriteLine("Received message.\r\n");
                if (receivedMessage != null)
                {
                    await deviceClient.CompleteAsync(receivedMessage);

                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    Debug.WriteLine("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);

                    try
                    {
                        jsonMmessageData = JObject.Parse(messageData);

                        if (null == jsonMmessageData) continue;

                        if ("GetAllState".Equals((string)jsonMmessageData["Cmd"]))
                        {
                            var msgString = JsonConvert.SerializeObject(YFSensorModelRTU);
                            var msg2 = new Message(Encoding.ASCII.GetBytes(msgString));
                            await _deviceClient.SendEventAsync(msg2);

                            msgString = JsonConvert.SerializeObject(SDT870RTU);
                            msg2 = new Message(Encoding.ASCII.GetBytes(msgString));
                            await _deviceClient.SendEventAsync(msg2);

                            msgString = JsonConvert.SerializeObject(LXSGZ20);
                            msg2 = new Message(Encoding.ASCII.GetBytes(msgString));
                            await _deviceClient.SendEventAsync(msg2);

                            msgString = JsonConvert.SerializeObject(yfDI8Model);
                            msg2 = new Message(Encoding.ASCII.GetBytes(msgString));
                            await _deviceClient.SendEventAsync(msg2);

                            msgString = JsonConvert.SerializeObject(yfDQ8Model);
                            msg2 = new Message(Encoding.ASCII.GetBytes(msgString));
                            await _deviceClient.SendEventAsync(msg2);

                        }
                        else if ("SetRly".Equals((string)jsonMmessageData["Cmd"]))
                        {
                            JToken rlyState = null;
                            string strRlyState = null;

                            for (int i = 1; i < 8; i++)
                            {
                                rlyState = jsonMmessageData["DQ8Q" + i];
                                if (rlyState != null)
                                {
                                    strRlyState = rlyState.ToString();

                                    if (strRlyState.Equals("1"))
                                    {
                                        yfDQ8Model.Q_Write(i-1, true);
                                        yfDQ8Model.DeviceValue = (byte)(yfDQ8Model.DeviceValue | (0x1 << (i - 1)));

                                    }
                                    else
                                    {
                                        yfDQ8Model.Q_Write(i-1, false);
                                        yfDQ8Model.DeviceValue = (byte)(yfDQ8Model.DeviceValue & (~(0x1 <<( i - 1))));

                                    }

                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }

                await Task.Delay(TimeSpan.FromMilliseconds(1000));
            }
        }


    }


}
