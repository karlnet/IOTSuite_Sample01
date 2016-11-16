using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTSuite_Sample01
{
    public enum ModbusType
    {
        Q = 1,
        I = 2,
        V = 3,
        AI = 4,
    }

    public class ModbusRTU
    {
        //-------------------------------------------------------------------------
        //Modbus的功能分配
        //-------------------------------------------------------------------------
        //1     读单/多线圈（离散输出）状态     功能1  返回输出点（Q ）任何数目的开/关状态。
        //2     读单/多接点（离散输入）状态     功能2  返回输入点（I ）任何数目的开/关状态。
        //3     读单/多保持寄存器               功能3  返回V 内存的内容。保持寄存器是Modbus 下的字数值，允许在一个请求中
        //                                             读至多120 个字?D:\MFRelease\library\V4.2\YFSoft.Protocol.Modbus.RTU\source\YFSoft.Protocol.Modbus.RTU\YFSoft.Protocol.Modbus.RTU.cs
        //4     读单/多输入寄存器               功能4  返回“模拟输入”数值。
        //5     写单线圈（离散输出）            功能5  将离散输出点设置为指定的数值。该点不是强制的，程序可以重写由Modbus
        //                                             请求写的数值?
        //6     写单保持寄存器                  功能6  将单保持寄存器数值写到VW内存区。
        //15    写多线圈（离散输出）            功能15 将多离散输出数值写到IO输出寄存器。开始输出点必须以字节边
        //                                             界开始（例如Q0.0 或Q2.0 ），被写的输出数字必须是八的倍数。这是对Modbus
        //                                             从属协议指令的限制。该点不是强制的，程序可以重写由Modbus 请求写的数值。
        //16    写多保持寄存器                  功能16 将多保持寄存器写到VW内存。在一个请求中可以写至多120 个字。
        //-------------------------------------------------------------------------
        //Modbus 从设备错误信息
        //-------------------------------------------------------------------------
        //0     无错
        //1     内存范围出错
        //2     非法的波特率或奇偶校验
        //3     非法的从属装置地址
        //4     Modbus 参数的非法数值
        //5     保持寄存器重叠Modbus 从属装置符号
        //6     接收奇偶校验出错
        //7     接收CRC 出错
        //8     非法的功能请求/功能不支持
        //9     请求中有非法内存地址
        //10    从属功能未启用
        //*************************************************************************


        private IStreamProtocol Stream;                    //通信流

        private enum FunctionCode
        {
            ReadQ = 1,
            ReadI = 2,
            ReadVW = 3,
            ReadAIW = 4,
            WriteQ = 5,
            WriteVW = 6,
            WriteMQ = 15,
            WriteMVW = 16
        }
        public ModbusRTU(IStreamProtocol stream)
        {
            this.Stream = stream;
        }
         public int Read(byte Addr, ModbusType type, UInt16 DataAddr, out UInt16 DataValue)
        {
            DataValue = 0;

            byte[] bytData = new byte[2];
            int intRet;
            FunctionCode code = FunctionCode.ReadI;

            switch (type)
            {
                case ModbusType.I:
                    code = FunctionCode.ReadI;
                    break;
                case ModbusType.Q:
                    code = FunctionCode.ReadQ;
                    break;
                case ModbusType.V:
                    code = FunctionCode.ReadVW;
                    break;
                case ModbusType.AI:
                    code = FunctionCode.ReadAIW;
                    break;
            }

            lock (this)
            {
                intRet = RtuData(Addr, code, DataAddr, bytData, 1);
            }

            if (intRet == 0)
            {
                switch (type)
                {
                    case ModbusType.I:
                    case ModbusType.Q:
                        DataValue = bytData[0];
                        break;
                    case ModbusType.V:
                    case ModbusType.AI:
                        DataValue = (UInt16)(bytData[0] << 8 | bytData[1]);
                        break;
                }
            }
            return intRet;
        }

        public int Write(byte Addr, ModbusType type, UInt16 DataAddr, UInt16 DataValue)
        {

            byte[] bytData = new byte[2];
            FunctionCode code = FunctionCode.WriteQ;

            switch (type)
            {
                case ModbusType.I:
                case ModbusType.AI:
                    return 2;
                case ModbusType.Q:
                    code = FunctionCode.WriteQ;
                    bytData[0] = (byte)(DataValue > 0 ? 0xFF : 0x00);
                    bytData[1] = 0;
                    break;
                case ModbusType.V:
                    code = FunctionCode.WriteVW;
                    bytData[1] = (byte)(DataValue & 0xFF);
                    bytData[0] = (byte)(DataValue >> 8);
                    break;
            }

            lock (this)
            {
                return RtuData(Addr, code, DataAddr, bytData, 1);
            }
        }

        private bool IsIOx8 = true;
        public int Read(byte Addr, ModbusType type, UInt16 DataAddr, UInt16[] DataValue, UInt16 DataNum)
        {

            byte[] bytData = new byte[1024];
            int intRet;
            UInt16 i;
            FunctionCode code = FunctionCode.ReadI;

            switch (type)
            {
                case ModbusType.I:
                    code = FunctionCode.ReadI;
                    if (IsIOx8)
                    {
                        DataAddr *= 8;
                        DataNum *= 8;
                    }
                    break;
                case ModbusType.Q:
                    code = FunctionCode.ReadQ;
                    if (IsIOx8)
                    {
                        DataAddr *= 8;
                        DataNum *= 8;
                    }
                    break;
                case ModbusType.V:
                    code = FunctionCode.ReadVW;
                    break;
                case ModbusType.AI:
                    code = FunctionCode.ReadAIW;
                    break;
            }

            lock (this)
            {
                intRet = RtuData(Addr, code, DataAddr, bytData, DataNum);
            }

            if (intRet == 0)
            {
                switch (type)
                {
                    case ModbusType.I:
                    case ModbusType.Q:
                        for (i = 0; i < DataNum + 7 / 8; i++)
                        {
                            DataValue[i] = bytData[i];
                        }
                        break;
                    case ModbusType.V:
                    case ModbusType.AI:
                        for (i = 0; i < DataNum; i++)
                        {
                            DataValue[i] = (UInt16)(bytData[i * 2] << 8 | bytData[i * 2 + 1]);
                        }
                        break;
                }
            }
            return intRet;
        }

        public int Write(byte Addr, ModbusType type, UInt16 DataAddr, UInt16[] DataValue, UInt16 DataNum)
        {

            byte[] bytData = new byte[1024];
            UInt16 i;

            FunctionCode code = FunctionCode.WriteMQ;

            switch (type)
            {
                case ModbusType.I:
                case ModbusType.AI:
                    return 2;
                case ModbusType.Q:
                    code = FunctionCode.WriteMQ;
                    if (IsIOx8)
                    {
                        for (i = 0; i < DataNum; i++)
                        {
                            bytData[i] = (byte)(DataValue[i]);
                        }
                        DataAddr *= 8;
                        DataNum *= 8;
                    }
                    else
                    {
                        for (i = 0; i < (DataNum + 7) / 8; i++)
                        {
                            bytData[i] = (byte)(DataValue[i]);
                        }
                    }
                    break;
                case ModbusType.V:
                    code = FunctionCode.WriteMVW;
                    for (i = 0; i < DataNum * 2; i++, i++)
                    {
                        bytData[i + 1] = (byte)(DataValue[i / 2] & 0xFF);
                        bytData[i] = (byte)(DataValue[i / 2] >> 8);
                    }
                    break;
            }

            lock (this)
            {
                return RtuData(Addr, code, DataAddr, bytData, DataNum);
            }
        }

        int RtuData(byte Addr, FunctionCode Code, UInt16 DataStart, byte[] DataArray, UInt16 DataNum)
        {
            byte[] bytSendArray = new byte[1024];             //发送数据缓冲区
            byte[] bytReceiveArray = new byte[512];          //接收数据缓冲区
            UInt16 intReceiveNum;                             //接收的数据个数
            UInt16 intCRC16;
            UInt16 i;
            UInt16 intOffSet = 0;
            UInt16 intSendNum;
            UInt16 intGetDataLen = 0;                             //实际接收数据的帧长度

            if (DataNum < 1) return -3;
            //=====================================================================================
            //命令发送
            //=====================================================================================
            bytSendArray[0] = Addr;                           //设备地址
            bytSendArray[1] = (byte)Code;                     //功能模式
            bytSendArray[2] = (byte)(DataStart >> 8);         //地址高位
            bytSendArray[3] = (byte)(DataStart & 0xFF);       //地址低位                

            switch (Code)
            {
                case FunctionCode.ReadI:
                case FunctionCode.ReadQ:
                case FunctionCode.ReadVW:
                case FunctionCode.ReadAIW:
                case FunctionCode.WriteMQ:
                case FunctionCode.WriteMVW:
                    bytSendArray[4] = (byte)(DataNum >> 8);           //数据个数高位
                    bytSendArray[5] = (byte)(DataNum & 0xFF);         //数据个数低位;
                    break;
                case FunctionCode.WriteQ:
                case FunctionCode.WriteVW:
                    bytSendArray[4] = DataArray[0];                   //数据高位
                    bytSendArray[5] = DataArray[1];                   //数据低位;
                    break;
            }

            switch (Code)
            {
                case FunctionCode.ReadI:
                case FunctionCode.ReadQ:
                case FunctionCode.ReadVW:
                case FunctionCode.ReadAIW:
                case FunctionCode.WriteQ:
                case FunctionCode.WriteVW:
                    intOffSet = 6;
                    break;
                case FunctionCode.WriteMQ:
                    bytSendArray[6] = (byte)((DataNum + 7) / 8);        //数据的字节个数
                    for (i = 1; i < (DataNum + 7) / 8 + 1; i++)
                        bytSendArray[6 + i] = DataArray[i - 1];
                    intOffSet = (UInt16)(7 + (DataNum + 7) / 8);
                    break;
                case FunctionCode.WriteMVW:
                    bytSendArray[6] = (byte)(DataNum * 2);        //数据的字节个数
                    for (i = 1; i < DataNum * 2 + 1; i++)
                        bytSendArray[6 + i] = DataArray[i - 1];

                    intOffSet = (UInt16)(7 + DataNum * 2);
                    break;
            }

            intCRC16 = GetCheckCode(bytSendArray, intOffSet);
            bytSendArray[intOffSet] = (byte)(intCRC16 & 0xFF);                       //CRC校验低位
            bytSendArray[intOffSet + 1] = (byte)((intCRC16 >> 8) & 0xff);            //CRC校验高位
            intSendNum = (UInt16)(intOffSet + 2);

            switch (Code)
            {
                case FunctionCode.ReadI:
                case FunctionCode.ReadQ:
                    intGetDataLen = (UInt16)(5 + (DataNum - 1) / 8 + 1);
                    break;
                case FunctionCode.ReadVW:
                case FunctionCode.ReadAIW:
                    intGetDataLen = (UInt16)(5 + DataNum * 2);
                    break;
                case FunctionCode.WriteMQ:
                case FunctionCode.WriteMVW:
                case FunctionCode.WriteQ:
                case FunctionCode.WriteVW:
                    intGetDataLen = 8;
                    break;
            }

            int intRet = SendCommand(intSendNum, bytSendArray, intGetDataLen, bytReceiveArray, 0).Result;
            if (intRet < 0)
            {
                return -1;
            }
            intReceiveNum = intGetDataLen;
            //=====================================================================================
            //信息接收
            //=====================================================================================
            //信息处理
            intCRC16 = GetCheckCode(bytReceiveArray, intReceiveNum - 2);
            //CRC16校验检验
            if (bytReceiveArray[intReceiveNum - 2] == (intCRC16 & 0xFF) && bytReceiveArray[intReceiveNum - 1] == ((intCRC16 >> 8) & 0xff))
            {
                //帧数据是否正确
                if (bytReceiveArray[0] == bytSendArray[0] && bytReceiveArray[1] == bytSendArray[1])
                {
                    switch (Code)
                    {
                        case FunctionCode.ReadI:
                        case FunctionCode.ReadQ:
                        case FunctionCode.ReadVW:
                        case FunctionCode.ReadAIW:
                            for (i = 0; i < bytReceiveArray[2]; i++)
                                DataArray[i] = bytReceiveArray[3 + i];
                            break;
                        case FunctionCode.WriteMQ:
                        case FunctionCode.WriteMVW:
                            for (i = 2; i < 6; i++)
                            {
                                if (bytReceiveArray[i] != bytSendArray[i])
                                {
                                    return 1;   //接收的数据错误
                                }
                            }
                            break;
                        case FunctionCode.WriteQ:
                        case FunctionCode.WriteVW:
                            break;
                    }
                    return 0;
                }
                else
                {
                    if (bytReceiveArray[0] == bytSendArray[0] && (bytReceiveArray[1] & 0xFF) == bytSendArray[1])
                    {
                        return bytReceiveArray[2];
                    }
                    return 2;
                }
            }
            return 3;
        }
        public async Task<int> SendCommand(UInt16 intSendNum, byte[] bytSendData, UInt16 intInceptNum, byte[] bytInceptData, int intOverTimeIdx)
        {
            await Stream.Write(bytSendData, 0, intSendNum);

            if (intInceptNum == 0)
            {
                return 0;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(1000));

            return await Stream.Read(bytInceptData, 0, intInceptNum);

        }
        private UInt16 GetCheckCode(byte[] buf, int nEnd)
        {
            byte bytCRCHi = 0xFF;
            byte bytCRCLo = 0xFF;
            byte bytIndex = 0;
            for (int i = 0; i < nEnd; i++)
            {
                bytIndex = (byte)(bytCRCHi ^ buf[i]);
                bytCRCHi = (byte)(bytCRCLo ^ bytCRC16H[bytIndex]);
                bytCRCLo = bytCRC16L[bytIndex];
            }
            return (UInt16)(bytCRCHi | bytCRCLo << 8);
        }

        //CRC16校验高位预存值
        private byte[] bytCRC16H = new byte[]{
        0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,
        0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,
        0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,
        0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,
        0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,
        0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
        0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x01,0xC0,
        0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40,0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,
        0x00,0xC1,0x81,0x40,0x01,0xC0,0x80,0x41,0x01,0xC0,0x80,0x41,0x00,0xC1,0x81,0x40
        };

        //CRC16校验低位预存值
        private byte[] bytCRC16L = new byte[]{
        0x00,0xC0,0xC1,0x01,0xC3,0x03,0x02,0xC2,0xC6,0x06,0x07,0xC7,0x05,0xC5,0xC4,0x04,0xCC,0x0C,0x0D,0xCD,0x0F,0xCF,0xCE,0x0E,0x0A,0xCA,0xCB,0x0B,0xC9,0x09,
        0x08,0xC8,0xD8,0x18,0x19,0xD9,0x1B,0xDB,0xDA,0x1A,0x1E,0xDE,0xDF,0x1F,0xDD,0x1D,0x1C,0xDC,0x14,0xD4,0xD5,0x15,0xD7,0x17,0x16,0xD6,0xD2,0x12,0x13,0xD3,
        0x11,0xD1,0xD0,0x10,0xF0,0x30,0x31,0xF1,0x33,0xF3,0xF2,0x32,0x36,0xF6,0xF7,0x37,0xF5,0x35,0x34,0xF4,0x3C,0xFC,0xFD,0x3D,0xFF,0x3F,0x3E,0xFE,0xFA,0x3A,
        0x3B,0xFB,0x39,0xF9,0xF8,0x38,0x28,0xE8,0xE9,0x29,0xEB,0x2B,0x2A,0xEA,0xEE,0x2E,0x2F,0xEF,0x2D,0xED,0xEC,0x2C,0xE4,0x24,0x25,0xE5,0x27,0xE7,0xE6,0x26,
        0x22,0xE2,0xE3,0x23,0xE1,0x21,0x20,0xE0,0xA0,0x60,0x61,0xA1,0x63,0xA3,0xA2,0x62,0x66,0xA6,0xA7,0x67,0xA5,0x65,0x64,0xA4,0x6C,0xAC,0xAD,0x6D,0xAF,0x6F,
        0x6E,0xAE,0xAA,0x6A,0x6B,0xAB,0x69,0xA9,0xA8,0x68,0x78,0xB8,0xB9,0x79,0xBB,0x7B,0x7A,0xBA,0xBE,0x7E,0x7F,0xBF,0x7D,0xBD,0xBC,0x7C,0xB4,0x74,0x75,0xB5,
        0x77,0xB7,0xB6,0x76,0x72,0xB2,0xB3,0x73,0xB1,0x71,0x70,0xB0,0x50,0x90,0x91,0x51,0x93,0x53,0x52,0x92,0x96,0x56,0x57,0x97,0x55,0x95,0x94,0x54,0x9C,0x5C,
        0x5D,0x9D,0x5F,0x9F,0x9E,0x5E,0x5A,0x9A,0x9B,0x5B,0x99,0x59,0x58,0x98,0x88,0x48,0x49,0x89,0x4B,0x8B,0x8A,0x4A,0x4E,0x8E,0x8F,0x4F,0x8D,0x4D,0x4C,0x8C,
        0x44,0x84,0x85,0x45,0x87,0x47,0x46,0x86,0x82,0x42,0x43,0x83,0x41,0x81,0x80,0x40
        };
    }
}
