//----------------------------------------------------------------
// 叶帆科技(www.yfiot.com)  版权所有
//----------------------------------------------------------------


namespace IOTSuite_Sample01
{
    public class LXSGZ20 
    {
        public string Name = "YFSoft.JBSB.LXSGZ20";   //驱动名称要保证唯一，否则加载时要报错

        #region IDriver Members

        //--------------------------------------------------------------------
        //	DeviceID －－ IO的ID号：设备类型标识_厂家名称_设备类型（＋协议名称） 
        //              注：长度不能超过30个字符
        //  --------------
        //  (1)设备ID  厂家自定义
        //  --------------
        //  (2)厂家名称（厂家的汉语拼音或英文名称）
        //  --------------
        //  如：西门子     Siemens
        //  --------------
        //  (3.0)设备类型。如S7200
        //  --------------
        //  (3.1)设备通信模式。如RS232 RS485 TCP UDP等
        //  --------------
        //  DeviceID示例：PLC_Siemens_S7200ModbusRTU,PLC_Siemens_S7200PPI,II_JRIC_WH
        //  --------------------------------------------------------------------
        //  驱动标识： 通信方式 - 厂家 - 驱动名称    
        //--------------------------------------------------------------------
        public DriverInfo GetDriverInfo()
        {
            DriverInfo info = new DriverInfo();

            //驱动名称
            info.Name = this.Name;
            //版本号
            info.Ver = "V1.0.0";
            //说明
            info.Explain = "宁波牌水表";
            //开发者
            info.Developer = "yefan";
            //开发日期
            info.Date = "2016-01-29";

            //-----
            //自动化标志
            //0 bit 0 - 系统为你初始化通信接口  1 - 由驱动程序本身完成通信接口初始化
            //1 bit 0 - 无操作                  1 - 由驱动程序本身完成IO变量添加 
            //2~31 bit 备用     
            info.AutoFlag = 0x0|0x2;
            //ConnMode-Manufacturer-DeviceType 是驱动的唯一标识
            //通信方式
            info.ConnMode = DeviceConnMode.SerialPort;
            //制造商
            info.Manufacturer = "JBSB(宁波水表股份有限公司)";
            //设备的类型
            info.DeviceType = "LXSGZ20";

            //-----
            //端口配置
            info.PortAddrExplain = "串 口 号:";
            info.PortAddrValue = "COM1|COM2|COM3|COM4|COM5|COM6|COM7|COM8";
            info.PortConfigExplain = "串口参数:";
            info.PortConfigValue = "1200,e,8,1";
            //设备配置
            info.DeviceAddrExplain = "";
            info.DeviceAddrValue = "0";
            info.DeviceConfigExplain = "设备地址:";
            info.DeviceConfigValue = "";

            //--//
            info.ItemExplain = new string[5];
            info.ItemValue = new string[5];
            //--
            info.ItemExplain[0] = "数据类型:";
            info.ItemValue[0] = "Flow|Ctl|State|CommState";

            //IO类型
            // b - [内部]布尔型 i - [内部]整型 f - [内部]浮点型 s - [内部]字符串
            // B - [外部]布尔型 I - [外部]整型 F - [外部]浮点型 S - [外部]字符串
            info.ItemExplain[1] = "";
            info.ItemValue[1] = "F|I|I|I";

            //IO读写模式
            //0 - 只读  1 - 只写 2 - 读写（自动读）3 - 读写（手动读）4 - 只读（手动）
            info.ItemExplain[2] = "";
            info.ItemValue[2] = "0|0|0|0";

            //IO初值
            info.ItemExplain[3] = "";
            info.ItemValue[3] = "0|0|0|0";

            //IO说明
            info.ItemExplain[4] = "";
            info.ItemValue[4] = "水流量|控制|水表状态|设备状态";

            return info;
        }

        SR118 sr118 = null;
        public int OnLoad(Device dv, IOperate op, object arg)
        {
            if (dv.DebugMode != 0) op.Print(MessageType.SysDebug, "OnLoad", this.Name);
            sr118 = new SR118(dv, op);
            return 0;
        }

        public int OnRun(Device dv, IOperate op, object arg)
        {
            //if (dv.DebugMode != 0) op.Print(MessageType.SysDebug, "OnRun", this.Name);
            int ReturnValue = 0;
            float flow = 0;
            try
            {
                flow = sr118.ReadData(SR118.MeterAddr.BroadcastAddress);
                if (flow < 0) ReturnValue = -1;
            }
            catch
            {
                ReturnValue = -2;
            }

            foreach (IOItem item in dv.IOItems)
            {
                if (ReturnValue == 0)
                {
                     string rwFlag = op.IOReadEx(item.Name + "." + "RWFlag");
                     
                    if (rwFlag == "R")
                     {
                         if (item.Param[0] == 0)
                         {
                             op.IOWrite(item.Name, flow.ToString("F2"));
                         }
                     }                   
                }

                if (item.Param[0] == 2)   //CommState
                {
                    string rwFlag = op.IOReadEx(item.Name + "." + "RWFlag");
                    if (rwFlag == "R")
                    {
                        op.IOWrite(item.Name, ReturnValue);
                    }
                }
            }
            return ReturnValue;
        }

        public int OnUnload(Device dv, IOperate op, object arg)
        {
            if (dv.DebugMode != 0) op.Print(MessageType.SysDebug, "OnUnload", this.Name);
            return 0;
        }

        #endregion
    }
}

