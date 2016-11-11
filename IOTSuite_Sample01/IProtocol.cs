using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTSuite_Sample01
{
    public enum ConnectType
    {
        Local = 0,
        SerialPort,
        Ethernet,
        CAN,
        USB,
        Wireless,
        Zigbee
    }

    /// <summary>
    /// 协议类型
    /// </summary>
    public enum ProtocolType
    {
        OEM = 0,
        Custom,
        Modbus,
    }

    /// <summary>
    /// IO类型
    /// </summary>
    public enum IOType
    {
        I = 0,
        Q,
        A,
        O,
        Block,
    }
    /// <summary>
    /// 协议接口
    /// </summary>
    public interface IProtocol
    {
        byte[] Read(int addr, int count, IOType type, UInt16 deviceAddr);
        void Write(int addr, byte[] buffer, int offset, int count, IOType type, UInt16 deviceAddr);
    }
}
