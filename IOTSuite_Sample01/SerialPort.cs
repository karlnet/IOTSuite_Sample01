using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;
using System.Diagnostics;
using System.Threading;

namespace IOTSuite_Sample01
{
    class SerialPort : IStreamProtocol

    {
        private SerialDevice serial = null;
        private DataWriter dataWriteObject = null;
        private DataReader dataReaderObject = null;
        public SerialPort()
        {

        }

        public async Task<int> Open(string portName, int baudRate, SerialParity parity = SerialParity.None, int dataBits = 8, SerialStopBitCount stopBits = SerialStopBitCount.One)
        {
            //new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            try
            {
                string aqs = SerialDevice.GetDeviceSelector(portName);
                var dis = await DeviceInformation.FindAllAsync(aqs);
                if (dis.Count == 0) return -1;

                serial = await SerialDevice.FromIdAsync(dis[0].Id);
                Debug.WriteLine("   open serial OK \r\n", serial.PortName);

                serial.WriteTimeout = TimeSpan.FromMilliseconds(1000);
                serial.ReadTimeout = TimeSpan.FromMilliseconds(1000);
                serial.BaudRate = (uint)baudRate;
                serial.Parity = parity;
                serial.StopBits = stopBits;
                serial.DataBits = (ushort)dataBits;
                serial.Handshake = SerialHandshake.None;
                //serial.Open();
                return 0;
            }
            catch(Exception e)
            {
                return -1;
            }

        }

        public int TimeOver = 0;

        public void Close()
        {
            //if (serial != null && serial.IsOpen) serial.Close();
            if (serial != null)
            {
                serial.Dispose();
            }
            serial = null;
        }

        public void DiscardInBuffer()
        {
            //serial.DiscardInBuffer();
        }

        public void DiscardOutBuffer()
        {
            //serial.DiscardOutBuffer();
        }
        public async Task<int> Read(byte[] buffer, int offset, int count)
        {
            if (serial == null) return -1;

            try
            {
                uint ReadBufferLength = 1024;
                dataReaderObject = new DataReader(serial.InputStream);
                dataReaderObject.InputStreamOptions = InputStreamOptions.Partial;

                CancellationTokenSource cts = new CancellationTokenSource(5000); // cancel after 5000ms
                DataReaderLoadOperation op = dataReaderObject.LoadAsync(ReadBufferLength);
                uint bytesAvailable = await op.AsTask<uint>(cts.Token);
                Debug.WriteLine("get data from  serial port ,length is : {0} \r\n", bytesAvailable);

                if (bytesAvailable <= 0) return -1;

                byte[] dataReceived = new byte[bytesAvailable];
                dataReaderObject.ReadBytes(dataReceived);
                Debug.WriteLine("get data from  serial port OK \r\n");
                Array.Copy(dataReceived, buffer, (int)bytesAvailable);

                return (int)bytesAvailable;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("error , get data to serial port \r\n");
                return -1;  //数据接收失败
            }
            finally
            {
                // Cleanup once complete
                if (dataReaderObject != null)
                {
                    dataReaderObject.DetachStream();
                    dataReaderObject = null;
                }
            }
        }

        public async Task   Write(byte[] buffer, int offset, int count)
        {
            //serial.Write(buffer, offset, count);
            try
            {
                //Debug.WriteLine("7");
                //if (serial == null) return -1;

                dataWriteObject = new DataWriter(serial.OutputStream);

                //byte[] dataToSend = new byte[intSendNum];
                //Array.Copy(bytSendData, dataToSend, intSendNum);
                //dataWriteObject.WriteBytes(dataToSend);

                dataWriteObject.WriteBytes(buffer.Take(count).ToArray());

                uint num = await dataWriteObject.StoreAsync().AsTask();

                Debug.WriteLine("write data to serial port {1},length is : {0}\r\n", num, serial.PortName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("error , write data to serial port {0} \r\n", serial.PortName);
                //return -1;
            }
            finally
            {
                // Cleanup once complete
                if (dataWriteObject != null)
                {
                    dataWriteObject.DetachStream();
                    dataWriteObject = null;
                }
            }
        }
    }
}
