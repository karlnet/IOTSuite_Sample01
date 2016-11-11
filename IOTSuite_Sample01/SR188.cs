/*-----------------------------------------------------------------
 * 作者： 叶帆科技  版权所有
 * 版本： V1.0.0
 * ----------------------------------------------------------------*/
using System;
using System.Threading.Tasks;

namespace IOTSuite_Sample01
{
    public enum MeterType
    {
        WaterMeter_Cold = 0x10,         //冷水表
        WaterMeter_Hot = 0x11,          //生活热水水表
        WaterMeter_Direct = 0x12,       //直饮水水表
        WaterMeter_Reclaimed = 0x13,    //中水水表
        CaloriMeter = 0x20,             //热量表
        GasMeter = 0x30,                //燃气表
        ElectricityMeter = 0x40,        //电表
    }

    public enum ControlType
    {
        Open = 0x55,
        Close = 0x99,
        NotDieValve = 0xAA,
    }

    public class SR188
    {
        private IStreamProtocol Stream;                    //通信流

        public const byte CMD_ReadData = 0x01;       //读数据
        public const byte CMD_WriteData = 0x04;      //写数据

        public const byte CMD_WriteBase = 0x16;      //写表底数

        public const byte CMD_ReadAddr = 0x03;       //读表地址
        public const byte CMD_WriteAddr = 0x15;      //写表地址

        public byte State = 0;                       //表状态

        public class MeterAddr
        {
            public UInt32 SerialNumber = 0;
            public int Year = 0;
            public byte Moon = 0;
            public UInt16 VendorCode = 0;
            public byte[] buffer = new byte[7];
            public MeterAddr(UInt32 SerialNumber, byte Moon, int Year, UInt16 VendorCode)
            {
                buffer[0] = Byte2BCD((byte)(SerialNumber & 0xFF));
                buffer[1] = Byte2BCD((byte)(SerialNumber >> 8 & 0xFF));
                buffer[2] = Byte2BCD((byte)(SerialNumber >> 16 & 0xFF));
                buffer[3] = Byte2BCD(Moon);
                buffer[4] = Byte2BCD((byte)(Year - 2000));
                buffer[5] = Byte2BCD((byte)(VendorCode & 0xFF));
                buffer[6] = Byte2BCD((byte)(VendorCode >> 8 & 0xFF));
            }

            public MeterAddr(byte[] addr)
            {
                Array.Copy(addr, this.buffer, 7);
                Year = 2000 + BCDToByte(addr[4]);
                Moon = BCDToByte(addr[3]);
                VendorCode = (UInt16)(BCDToByte(addr[5]) + BCDToByte(addr[6]) * 100);
                SerialNumber = (UInt16)(BCDToByte(addr[0]) + BCDToByte(addr[1]) * 100 + BCDToByte(addr[2]) * 10000);
            }

            public static bool IsBroadcastAddress(byte[] addr, int offset)
            {
                if (addr == null || (addr.Length - offset) < 7) return false;
                for (int i = 0; i < 7; i++)
                {
                    if (addr[i + offset] != 0xAA) return false;
                }
                return true;
            }

            public static bool IsMate(byte[] addr1, int offset1, byte[] addr2, int offset2)
            {
                if (addr1 == null || (addr1.Length - offset1) < 7) return false;
                if (addr2 == null || (addr2.Length - offset2) < 7) return false;

                for (int i = 0; i < 7; i++)
                {
                    if (addr1[i + offset1] != addr2[i + offset2]) return false;
                }
                return true;
            }

            public static byte[] BroadcastAddress = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };
        }

        public SR188(IStreamProtocol stream)
        {
            this.Stream = stream;
        }

        public MeterAddr ReadAddr()
        {
            byte[] buffer;
            int ret = SendCommand((MeterType)0xAA, MeterAddr.BroadcastAddress, CMD_ReadAddr, new byte[3] { 0x81, 0x0A, SER++ }, out buffer);
            if (ret > 0)
            {
                return new MeterAddr(buffer);
            }
            return null;
        }

        public float ReadData(byte[] addr)
        {
            byte[] buffer;
            int ret = SendCommand(MeterType.WaterMeter_Cold, addr, CMD_ReadData, new byte[3] { 0x90, 0x1F, SER++ }, out buffer);
            if (ret > 0)
            {
                float v = (float)(BCDToByte(buffer[3 + 0]) / 100.0);
                v += (float)(BCDToByte(buffer[3 + 1]));
                v += (float)(BCDToByte(buffer[3 + 2]) * 100.0);
                v += (float)(BCDToByte(buffer[3 + 3]) * 10000.0);
                State = buffer[3 + 4];
                return v;
            }
            return -1;
        }

        public int Control(byte[] addr, ControlType type)
        {
            byte[] buffer;
            int ret = SendCommand(MeterType.WaterMeter_Cold, addr, CMD_WriteData, new byte[4] { 0x17, 0xA0, SER++, (byte)type }, out buffer);
            if (ret > 0)
            {
                State = buffer[3];
                return State;
            }
            return -1;
        }

        public int ReadControlState(byte[] addr)
        {
            byte[] buffer;
            int ret = SendCommand(MeterType.WaterMeter_Cold, addr, CMD_ReadData, new byte[3] { 0x17, 0xA0, SER++ }, out buffer);
            if (ret > 0)
            {
                State = buffer[3];
                return State;
            }
            return -1;
        }


        #region 私有函数
        byte SER = 0;
        private int SendCommand(MeterType type, byte[] addr, byte cmd, byte[] inData, out byte[] outData)
        {
            byte[] SendBuffer = new byte[256];                //发送数据缓冲区
            byte[] ReceiveBuffer = new byte[256];             //接收数据缓冲区

            outData = null;
            if (inData.Length < 1) return -3;
            //=====================================================================================
            //命令发送
            //=====================================================================================
            SendBuffer[0] = 0x68;                           //帧头
            SendBuffer[1] = (byte)type;                     //表类型
            Array.Copy(addr, 0, SendBuffer, 2, 7);          //地址
            SendBuffer[9] = cmd;                            //控制码
            SendBuffer[10] = (byte)inData.Length;           //数据长度
            Array.Copy(inData, 0, SendBuffer, 11, inData.Length);
            SendBuffer[11 + inData.Length] = GetCheckCode(SendBuffer, 0, 11 + inData.Length);   //校验和 
            SendBuffer[11 + inData.Length + 1] = 0x16;        //帧尾

            //发送数据
            //Stream.Write( new byte[] { 0xFE, 0xFE }, 0, 2);
            //Stream.Write( SendBuffer, 0, 11 + inData.Length + 1 + 1);
            byte[] _buffer = new byte[256];
            _buffer[0] = 0xFE;
            _buffer[1] = 0xFE;
            Array.Copy(SendBuffer, 0, _buffer, 2, 11 + inData.Length + 1 + 1);
            Stream.Write(_buffer, 0, 11 + inData.Length + 1 + 1+2);

            //------------------------------
            Task.Delay(1000);

            //-----------------------------
            int num = Stream.Read(ReceiveBuffer, 0, 14).Result;
            //数据接收完毕
            if (num >= 11 + 3)
            {
                Array.Copy(ReceiveBuffer, 3, ReceiveBuffer, 0, num-3);

                bool IsOK = true;

                if (ReceiveBuffer[0] != 0x68) IsOK = false;
                if (!MeterAddr.IsBroadcastAddress(SendBuffer, 2)) //广播地址
                {
                    if (!MeterAddr.IsMate(SendBuffer, 2, ReceiveBuffer, 2))
                    {
                        IsOK = false;
                    }
                    if (SendBuffer[1] != 0xAA)
                    {
                        if (ReceiveBuffer[9] != SendBuffer[9] + 0x80) IsOK = false;
                    }
                }

                if (IsOK)
                {
                    int count = ReceiveBuffer[10];
                    //Stream.Read( ReceiveBuffer, 11, count + 2);
                    byte crc = GetCheckCode(ReceiveBuffer, 0, 11 + count);
                    if (crc == ReceiveBuffer[11 + count] && ReceiveBuffer[11 + count + 1] == 0x16)
                    {
                        if (SendBuffer[1] == 0xAA)
                        {
                            outData = new byte[7];
                            Array.Copy(ReceiveBuffer, 2, outData, 0, 7);
                        }
                        else
                        {
                            outData = new byte[count];
                            Array.Copy(ReceiveBuffer, 11, outData, 0, count);
                        }
                        return count;
                    }
                }

                return -2;
            }
            return -1;
        }

        //校验和
        private byte GetCheckCode(byte[] buffer, int offset, int count)
        {
            byte crc = 0;
            for (int i = offset; i < offset + count; i++)
            {
                crc += buffer[i];
            }
            return crc;
        }

        public static byte Byte2BCD(byte b)
        {
            //高四位  
            byte b1 = (byte)(b / 10);
            //低四位  
            byte b2 = (byte)(b % 10);
            return (byte)((b1 << 4) | b2);
        }

        public static byte BCDToByte(byte b)
        {
            //高四位  
            byte b1 = (byte)((b >> 4) & 0xF);
            //低四位  
            byte b2 = (byte)(b & 0xF);
            return (byte)(b1 * 10 + b2);
        }

        #endregion
    }
}
