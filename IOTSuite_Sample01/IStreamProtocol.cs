using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;

namespace IOTSuite_Sample01
{
    public interface IStreamProtocol
    {
        Task<int> Read(byte[] buffer, int offset, int count);
        Task Write(byte[] buffer, int offset, int count);
    }
}
