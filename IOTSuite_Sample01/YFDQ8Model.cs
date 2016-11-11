using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOTSuite_Sample01
{

    /// <summary>
    ///IO模块客户端类
    /// </summary>
    public class YFDQ8Model : ModelBase
    {
        private YFIOCustomProtocol protocol = null;

        private UInt16 deviceAddr = 1;
        [JsonIgnore]
        public byte DeviceValue { set; get; } = 0;
        public string this[int index]
        {
            set
            {

                switch (index)
                {
                    case 0:
                        DQ8Q1 = value;
                        break;
                    case 1:
                        DQ8Q2 = value;
                        break;
                    case 2:
                        DQ8Q3 = value;
                        break;
                    case 3:
                        DQ8Q4 = value;
                        break;
                    case 4:
                        DQ8Q5 = value;
                        break;
                    case 5:
                        DQ8Q6 = value;
                        break;
                    case 6:
                        DQ8Q7 = value;
                        break;
                    case 7:
                        DQ8Q8 = value;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("index");
                }
            }
            get
            {
                switch (index)
                {
                    case 0: return DQ8Q1;
                    case 1: return DQ8Q2;
                    case 2: return DQ8Q3;
                    case 3: return DQ8Q4;
                    case 4: return DQ8Q5;
                    case 5: return DQ8Q6;
                    case 6: return DQ8Q7;
                    case 7: return DQ8Q8;
                    default:
                        throw new ArgumentOutOfRangeException("index");
                }
            }
        }
        public string DQ8CommState { get; set; } = "0";
        public string DQ8Q1 { get; set; } = "0";
        public string DQ8Q2 { get; set; } = "0";
        public string DQ8Q3 { get; set; } = "0";
        public string DQ8Q4 { get; set; } = "0";
        public string DQ8Q5 { get; set; } = "0";
        public string DQ8Q6 { get; set; } = "0";
        public string DQ8Q7 { get; set; } = "0";
        public string DQ8Q8 { get; set; } = "0";

        override protected JObject WriteAndRead()
        {

            JObject rmtd = new JObject();

            try
            {
                //byte[] buffer = Block_Read(0, 33);

                rmtd = ParseData(DeviceValue);

                rmtd.AddFirst(new JProperty("DQ8CommState", "0"));

            }
            catch (Exception e) { }

            return rmtd;
        }

        private JObject ParseData(byte buffer)
        {
            JObject changedDataProps = new JObject();
            string res;

            for (int i = 0; i < 8; i++)
            {
                res= ((buffer >> i & 1) > 0) ? "1" : "0";

                if (!this[i].Equals(res))
                {
                    this[i] = res;
                    changedDataProps.Add("DQ8Q"+(i+1), res);
                }
            }
            return changedDataProps;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="modbus">具体的模块类</param>
        public YFDQ8Model(YFIOCustomProtocol protocol, UInt16 addr = 1)
        {
            this.deviceAddr = addr;
            this.protocol = protocol;
        }

        /// <summary>
        /// 开关量输入状态读取
        /// </summary>
        /// <param name="addr">地址 0,1,2...</param>
        /// <returns>True - 高电平 Flash - 低电平</returns>
        public bool I_Read(int addr)
        {
            return protocol.Read(addr, 1, IOType.I, deviceAddr)[0] == 1;
        }

        /// <summary>
        /// 开关量输出状态读取
        /// </summary>
        /// <param name="addr">地址 0,1,2...</param>
        /// <returns>True - 通 Flash - 断开</returns>
        public bool Q_Read(int addr)
        {
            return protocol.Read(addr, 1, IOType.Q, deviceAddr)[0] == 1;
        }

        /// <summary>
        /// 写开关量输出状态
        /// </summary>
        /// <param name="addr">地址 0,1,2...</param>
        /// <param name="state">True - 通 Flash - 断开</param>
        public void Q_Write(int addr, bool state)
        {
            protocol.Write(addr, new byte[] { (byte)(state ? 1 : 0) }, 0, 1, IOType.Q, deviceAddr);
        }

        /// <summary>
        /// 模拟量读取
        /// </summary>
        /// <param name="addr">地址 0,1,2...</param>
        /// <returns>模拟量的值</returns>
        public float A_Read(int addr)
        {
            byte[] buffer = protocol.Read(addr, 4, IOType.A, deviceAddr);
            return BitConverter.ToSingle(buffer, 0);
        }

        /// <summary>
        /// 模拟量写
        /// </summary>
        /// <param name="addr">地址 0,1,2...</param>
        /// <param name="state">模拟量的值</param>
        public void O_Write(int addr, float state)
        {
            byte[] buffer = BitConverter.GetBytes(state);
            protocol.Write(addr, buffer, 0, 4, IOType.O, deviceAddr);
        }

        /// <summary>
        /// 数据块读取
        /// </summary>
        /// <param name="addr">地址 0,1,2...</param>
        /// <param name="count">数据个数</param>
        /// <returns>数据内容</returns>
        public byte[] Block_Read(int addr, int count)
        {
            return protocol.Read(addr, count, IOType.Block, deviceAddr);
        }

        /// <summary>
        /// 数据块写
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public void Block_Write(int addr, byte[] buffer, int offset, int count)
        {
            protocol.Write(addr, buffer, offset, count, IOType.Block, deviceAddr);
        }
    }
}
