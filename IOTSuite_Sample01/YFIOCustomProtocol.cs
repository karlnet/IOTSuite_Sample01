using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTSuite_Sample01
{
    public enum IOType
    {
        I = 0,
        Q,
        A,
        O,
        Block,
    }

    //叶帆科技OEM协议(YFBus)
    //帧标识(1byte:0xAC)+通信计数（1byte）+ 数据类型（1byte）+ 设备地址(2byte）+数据长度/命令内容（2byte)+帧头字段循环冗余校验（1Byte）+数据（n Byte)+CRC16（2Byte）

    /// <summary>
    ///OEM通信协议类（Client）
    /// </summary>
    public class YFIOCustomProtocol
    {
        private IStreamProtocol Stream;                    //通信流

        private const byte FrameID = 0xAC;                 //帧头
        private const byte ResponseFrameMask = 0x80;
        private byte FrameNum = 0;                         //帧计数

        public int TimeOver = 100;
        private byte[] ReceiveBuffer = new byte[1024];
        private byte[] SendBuffer = new byte[512];
        private int ReceiveCount;
        private int SendCount;

        private enum FrameType
        {
            CommandFrame = 0x2A,
            DataFrame = 0x25,
            ResponseCommandFrame = 0xAA,
            ResponseDataFrame = 0xA5,
        }


        public YFIOCustomProtocol(IStreamProtocol stream)
        {
            Stream = stream;
        }

        public byte[] Read(int addr, int count, IOType type, UInt16 deviceAddr)
        {
            byte[] inBuffer = null;
            int ret = 0;

            switch (type)
            {
                case IOType.I:
                case IOType.Q:
                    inBuffer = new byte[2];
                    inBuffer[0] = (byte)(type);           //类型
                    inBuffer[1] = (byte)(addr & 0xFF);    //偏移
                    UInt16 cmd = (UInt16)(inBuffer[1] << 8 | inBuffer[0]);
                    ret = SendCommand(deviceAddr, cmd);
                    if (ret >= 0)
                    {
                        return new byte[] { (byte)ret };
                    }
                    break;
                case IOType.A:
                case IOType.Block:
                    inBuffer = new byte[5];
                    inBuffer[0] = (byte)(type);
                    inBuffer[1] = (byte)(addr & 0xFF);
                    inBuffer[2] = (byte)(addr >> 8 & 0xFF);
                    inBuffer[3] = (byte)(count & 0xFF);
                    inBuffer[4] = (byte)(count >> 8 & 0xFF);
                    byte[] outBuffer = new byte[count];
                    ret = SendData(deviceAddr, inBuffer, 0, 5, outBuffer, 0);
                    if (ret == count)
                    {
                        return outBuffer;
                    }
                    break;
            }
            throw new NotSupportedException();
        }

        public void Write(int addr, byte[] buffer, int offset, int count, IOType type, UInt16 deviceAddr)
        {
            byte[] inBuffer = null;
            int ret = 0;

            switch (type)
            {
                case IOType.Q:
                    inBuffer = new byte[2];
                    inBuffer[0] = (byte)(((byte)type & 0xF) | (buffer[0] << 4));
                    inBuffer[1] = (byte)(addr & 0xFF);
                    UInt16 cmd = (UInt16)(inBuffer[1] << 8 | inBuffer[0]);
                    ret = SendCommand(deviceAddr, cmd);
                    if (ret >= 0)
                    {
                        return;
                    }
                    break;
                case IOType.O:
                case IOType.Block:
                    inBuffer = new byte[5 + count];
                    inBuffer[0] = (byte)(type);
                    inBuffer[1] = (byte)(addr & 0xFF);
                    inBuffer[2] = (byte)(addr >> 8 & 0xFF);
                    inBuffer[3] = (byte)(count & 0xFF);
                    inBuffer[4] = (byte)(count >> 8 & 0xFF);
                    Array.Copy(buffer, offset, inBuffer, 5, count);
                    ret = SendData(deviceAddr, inBuffer, 0, 5 + count);
                    if (ret >= 0)
                    {
                        return;
                    }
                    break;
            }
            throw new NotSupportedException();
        }

        /// <summary>
        ///  发送命令
        /// </summary>
        /// <param name="addr">设备地址</param>
        /// <param name="cmd">命令</param>
        /// <param name="outBuffer">存放返回数据的字节数组</param>
        /// <param name="outOffset">存放偏移</param>
        /// <returns>大于等于0，成功，小于0失败，大于0表示返回的数据长度</returns>
        public int SendCommand(UInt16 addr, UInt16 cmd, byte[] outBuffer = null, int outOffset = 0)
        {
            lock (Stream)
            {
                byte[] inBuffer = new byte[2] { (byte)(cmd & 0xFF), (byte)(cmd >> 8 & 0xFF) };
                return DataDispose(FrameType.CommandFrame, addr, inBuffer, 0, 2, outBuffer, outOffset).Result;
            }
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="addr">设备地址</param>
        /// <param name="inBuffer">发送数据的字节数组</param>
        /// <param name="inOffset">偏移</param>
        /// <param name="inCount">数据大小</param>
        /// <param name="outBuffer">存放返回数据的字节数组</param>
        /// <param name="outOffset">存放偏移</param>
        /// <returns>大于等于0，成功，小于0失败，大于0表示返回的数据长度</returns>
        public int SendData(UInt16 addr, byte[] inBuffer, int inOffset, int inCount, byte[] outBuffer = null, int outOffset = 0)
        {
            lock (Stream)
            {
                return DataDispose(FrameType.DataFrame, addr, inBuffer, inOffset, inCount, outBuffer, outOffset).Result;
            }
        }

        #region 私有函数

        private async Task<int> DataDispose(FrameType type, UInt16 addr, byte[] inBuffer, int inOffset, int inCount, byte[] outBuffer, int outOffset)
        {
            UInt16 crc16 = 0;
            FrameType frameType;

            if (inCount < 1) return -1;
            //=====================================================================================
            //命令发送
            //=====================================================================================
            SendBuffer[0] = FrameID;                            //帧头
            SendBuffer[1] = FrameNum++;                         //帧计数
            SendBuffer[2] = (byte)type;                         //功能模式
            SendBuffer[3] = (byte)(addr & 0xFF);                //设备地址低位  
            SendBuffer[4] = (byte)(addr >> 8);                  //设备地址高位                  
            if (type == FrameType.CommandFrame)
            {
                SendBuffer[5] = inBuffer[0];
                SendBuffer[6] = inBuffer[1];
                SendCount = 8;
            }
            else
            {
                SendBuffer[5] = (byte)((inCount + 2) & 0xFF);   //数据长度低位
                SendBuffer[6] = (byte)((inCount + 2) >> 8);       //数据长度高位               
                Array.Copy(inBuffer, 0, SendBuffer, 8, inCount);
                crc16 = GetCRC16(SendBuffer, 8, inCount);

                SendCount = 8 + inCount + 2;
                SendBuffer[SendCount - 2] = (byte)(crc16 & 0xFF);                    //CRC校验低位
                SendBuffer[SendCount - 1] = (byte)((crc16 >> 8) & 0xFF);             //CRC校验高位   
            }
            SendBuffer[7] = GetCRC(SendBuffer, 0, 7);

            //清空接收和发生缓冲区
            //Stream.DiscardInBuffer();
            //Stream.DiscardOutBuffer();
            //发送数据
            Stream.Write(SendBuffer, 0, SendCount);

            //广播命令，没有数据返回
            if (addr == 0) return 0;

            //=====================================================================================
            //信息接收
            //=====================================================================================
            //延时 直到数据接收完毕
            //for (int t = 0; t < 8 * 2 + TimeOver; t++)
            //{
            //    ReceiveCount = Stream.BytesToRead().Result;
            //    if (ReceiveCount >= 8) break;
            //    Task.Delay(10);
            //}

            //找帧头
            //if (ReceiveCount >= 8)
            //{
           return await Stream.Read(ReceiveBuffer, 0, 8).ContinueWith((res) => {
                //不是对应的响应帧
                if (ReceiveBuffer[1] != SendBuffer[1])
                {
                    return -2;
                }

                frameType = (FrameType)ReceiveBuffer[2];
                //不是响应帧
                if (frameType != FrameType.ResponseCommandFrame && frameType != FrameType.ResponseDataFrame)
                {
                    return -3;
                }

                //不是自己的帧
                if (addr != (UInt16)(ReceiveBuffer[4] << 8 | ReceiveBuffer[3]))
                {
                    return -4;
                }

                //是命令帧
                if (frameType == FrameType.ResponseCommandFrame)
                {
                    return (ReceiveBuffer[6] << 8 | ReceiveBuffer[5]);
                }
                else
                {
                    //数据包长度（含校验）
                    ReceiveCount = (ReceiveBuffer[6] << 8 | ReceiveBuffer[5]);

                   //延时 直到数据接收完毕
                   // for (int t = 0; t < ReceiveCount * 2 + TimeOver; t++)
                   //{
                   //    if (Stream.BytesToRead().Result >= ReceiveCount) break;
                   //    Task.Delay(10);
                   //}

                   //数据没有接收到
                   // if (Stream.BytesToRead().Result < ReceiveCount)
                   //{
                   //    return -5;
                   //}

                   //接收数据
                   //Stream.Read(ReceiveBuffer, 0, ReceiveCount);
                   Array.Copy(ReceiveBuffer, 8, ReceiveBuffer, 0, ReceiveCount);
                   crc16 = GetCRC16(ReceiveBuffer, 0, ReceiveCount - 2);
                    if (ReceiveBuffer[ReceiveCount - 2] == (crc16 & 0xFF) && ReceiveBuffer[ReceiveCount - 1] == ((crc16 >> 8) & 0xff))
                    {
                        Array.Copy(ReceiveBuffer, 0, outBuffer, outOffset, ReceiveCount - 2);
                        return ReceiveCount - 2;

                    }
                    else
                    {
                        return -6;
                    }

                }
            });
        //}
            //return -7;
        }


        #region "CRC校验算法"

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

        //CRC16校验
        private UInt16 GetCRC16(byte[] buffer, int offset, int count)
        {
            UInt16 crc = (UInt16)0xffff;
            for (int i = 0; i < count; i++)
            {
                crc ^= (UInt16)buffer[i + offset];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                        crc >>= 1;
                }
            }
            return crc;
        }

        //循环冗余校验
        private byte GetCRC(byte[] buffer, int offset, int count)
        {
            byte crc = 0;
            for (int i = 0; i < count; i++)
            {
                crc ^= buffer[i + offset];
            }
            return crc;
        }

        #endregion

        #endregion
    }
}
