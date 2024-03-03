using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Impinj.OctaneSdk;

namespace RFID_Program
{
    public partial class MainForm : Form
    {
        Buffer buffer;
        string READER_HOSTNAME;
        ImpinjReader reader;
        DateTime startTime;

        public MainForm()
        {
            InitializeComponent();

            reader = new ImpinjReader();
            READER_HOSTNAME = "SpeedwayR-12-F6-22.local";
            buffer = new Buffer(100);

            this.updatePort();
            this.updateChannel();
            this.updateChart();
            this.timer1.Enabled = true;
            this.timer1.Interval = 200;
            startTime = DateTime.Now;
        }

        #region 更新控件
        private void updateChannel()
        {
            this.cmb_channel.Items.Add("全选");
            for (double i = 920.625; i <= 924.375; i += 0.250)
            {
                this.cmb_channel.Items.Add(i.ToString());
            }
        }
        private void updatePort()
        {
            for (int i = 1; i <= 4; i++)
                this.cmb_port.Items.Add(i.ToString());
        }

        private void updateChart()
        {
            chart1.Series[0].ChartType = SeriesChartType.Line;
            chart1.Titles.Add("Phase");
            chart1.Titles[0].Text = "Phase";
            chart1.Legends.Clear();
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.ChartAreas[0].AxisY.Maximum = 7;

            chart2.Series[0].ChartType = SeriesChartType.Line;
            chart2.Titles.Add("RSS");
            chart2.Titles[0].Text = "RSS";
            chart2.Legends.Clear();
            chart2.ChartAreas[0].AxisY.Minimum = -70;
            chart2.ChartAreas[0].AxisY.Maximum = -10;
        }
        #endregion

        private void btn_selectSavePath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dilog = new FolderBrowserDialog();
            dilog.Description = "请选择保存数据的文件夹";
            if (dilog.ShowDialog() == DialogResult.OK)
                this.savePath.Text = dilog.SelectedPath;
        }

        private void btn_read_Click(object sender, EventArgs e)
        {
            if (savePath.Text.Length == 0 || cmb_port.Text.Length == 0 || cmb_channel.Text.Length == 0)
            {
                MessageBox.Show("信息选择不完整！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                startQueryTag();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            chart1.Series[0].Points.Clear();
            chart2.Series[0].Points.Clear();
            foreach (TagPack t in buffer.GetItem())
            {
                if (!cmb_epc.Items.Contains(t.Epc))
                    cmb_epc.Items.Add(t.Epc);

                #region 给chart添加数据
                if (cmb_epc.Text.Length != 0)
                {
                    if ((t.Port == cmb_port.Text) && (t.Epc == cmb_epc.Text))
                    {
                        chart1.Series[0].Points.Add(float.Parse(t.Phase));
                        chart2.Series[0].Points.Add(float.Parse(t.RSS));
                    }
                }
                else
                {
                    if ((t.Port == cmb_port.Text))
                    {
                        chart1.Series[0].Points.Add(float.Parse(t.Phase));
                        chart2.Series[0].Points.Add(float.Parse(t.RSS));
                    }
                }
                #endregion 
            }
        }

        public void ConnectToReader()
        {
            try
            {
                string str = string.Format("Attempting to connect to {0} ({1}).\r", reader.Name, READER_HOSTNAME);
                rtb_info.AppendText(str);
                
                reader.ConnectTimeout = 10000;
                reader.Connect(READER_HOSTNAME);
                rtb_info.AppendText("Successfully connected.\r");
                reader.ResumeEventsAndReports();
            }
            catch (OctaneSdkException e)
            {
                rtb_info.AppendText("Failed to connect.\r");
                throw e;
            }
        }
        public void startQueryTag()
        {
            try
            {
                reader.Name = "My Reader #1";
                ConnectToReader();
                
                reader.KeepaliveReceived += OnKeepaliveReceived;
                reader.ConnectionLost += OnConnectionLost;

                reader.ApplySettings(setParameters());
                reader.SaveSettings();
                reader.TagsReported += OnTagsReported;
                rtb_info.AppendText("Start reading the msg of tag.\r");
            }
            catch (OctaneSdkException e)
            {
                rtb_info.AppendText(string.Format("Octane SDK exception: {0}", e.Message));
            }
            catch (Exception e)
            {
                rtb_info.AppendText(string.Format("Exception : {0}", e.Message));
            }
        }

        public Settings setParameters()
        {
            Settings settings = reader.QueryDefaultSettings();
            settings.AutoStart.Mode = AutoStartMode.Immediate;
            settings.AutoStop.Mode = AutoStopMode.None;
            settings.Gpos.GetGpo(1).Mode = GpoMode.LLRPConnectionStatus;
            settings.Report.IncludeFirstSeenTime = true;
            settings.Report.IncludeLastSeenTime = true;
            settings.Report.IncludePhaseAngle = true;
            settings.Report.IncludeChannel = true;
            settings.Report.IncludePeakRssi = true;
            settings.Report.IncludeSeenCount = true;
            settings.Report.IncludeAntennaPortNumber = true;
            settings.HoldReportsOnDisconnect = true;
            //settings.Keepalives.Enabled = true;
            //settings.Keepalives.PeriodInMs = 5000;
            settings.Keepalives.EnableLinkMonitorMode = true;
            settings.Keepalives.LinkDownThreshold = 5;

            if (cmb_channel.Text == "全选")
            {
                for (double i = 920.625; i <= 924.375; i += 0.250)
                {
                    settings.TxFrequenciesInMhz.Add(i);
                }
            }
            else
            {
                settings.TxFrequenciesInMhz.Add(Double.Parse(cmb_channel.Text));
            }

            return settings;
        }

        public void OnConnectionLost(ImpinjReader reader)
        {
            rtb_info.AppendText(string.Format("Connection lost : {0} ({1})", reader.Name, reader.Address));
            reader.Disconnect();
            ConnectToReader();
        }

        public void OnKeepaliveReceived(ImpinjReader reader)
        {
            rtb_info.AppendText(string.Format("Keepalive received from {0} ({1})", reader.Name, reader.Address));
        }

        public void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            foreach (Tag tag in report)
            {
                //rtb_info.AppendText(string.Format("Epc:{0},Port Number:{1},Frequency:{2},Phase:{3},RSS:{4}\r", tag.Epc.ToString(), tag.AntennaPortNumber.ToString(), tag.ChannelInMhz.ToString(), tag.PhaseAngleInRadians.ToString(), tag.PeakRssiInDbm.ToString()));
                SaveData(tag);
                this.buffer.AddItem(new TagPack(tag.Epc.ToString(), tag.AntennaPortNumber.ToString(), tag.ChannelInMhz.ToString(), tag.PhaseAngleInRadians.ToString(), tag.PeakRssiInDbm.ToString()));
            }
            //rtb_info.Select(rtb_info.TextLength, 0);
            //rtb_info.ScrollToCaret();
        }

        public void SaveData(Tag tag)
        {
            FileStream F = new FileStream(savePath.Text + @"\" + tag.AntennaPortNumber.ToString() + "_" + tag.Epc.ToString() + ".txt", FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(F);
            //开始写入
            sw.Write((DateTime.Now-startTime).TotalMilliseconds);
            sw.Write("   ");
            sw.Write(tag.AntennaPortNumber);
            sw.Write("   ");
            sw.Write(tag.ChannelInMhz);
            sw.Write("   ");
            sw.Write(tag.PhaseAngleInRadians);
            sw.Write("   ");
            sw.Write(tag.PeakRssiInDbm);
            sw.Write("\r\n");
            //清空缓冲区
            sw.Flush();
            //关闭流
            sw.Close();
            F.Close();
        }

        private void btn_stop_Click(object sender, EventArgs e)
        {
            reader.Stop();
            reader.Disconnect();
        }
    }
}
