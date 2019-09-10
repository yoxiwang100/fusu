using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace TDCube
{
    public partial class Selfsimilarity2 : Form
    {
        /// <summary>
        /// 1222修改： 存储保持内容，拆分分析功能
        /// </summary>
        public Selfsimilarity2()
        {
            InitializeComponent();
            columnsInfo cinfo = new columnsInfo();
            for (int i = 0; i < cinfo.ColumnsCount(); i++)
            {
                int index = this.dataGridView1.Rows.Add();
                this.dataGridView1.Rows[index].Cells[1].Value = cinfo.Getcolumn(i).name;
            }
            dataGridView1.AllowUserToAddRows = false;

            string s = "x:" + CubeInfo.Xnum + "\t" + "XMin:" + CubeInfo.minX + "\t" + "XMax:" + CubeInfo.maxX + "\t" + "X长度：" + CubeInfo._X + "\n" +
                       "Y:" + CubeInfo.Ynum + "\t" + "YMin:" + CubeInfo.minY + "\t" + "YMax:" + CubeInfo.maxY + "\t" + "Y长度：" + CubeInfo._Y + "\n" +
                        "Z:" + CubeInfo.Znum + "\t" + "ZMin:" + CubeInfo.minZ + "\t" + "Zmax" + CubeInfo.maxZ + "\t" + "Z长度：" + CubeInfo._Z + "\n" +
                        "总数：" + CubeInfo.Xnum * CubeInfo.Ynum * CubeInfo.Znum;
            richTextBox2.Text = s;
        }
        SelfsimilarityAnalysis sa;
        DateTime dt = DateTime.Now;
        private void buttonWork1_Click(object sender, EventArgs e)
        {
            try
            {
                List<int> l1;
                List<double> l2;//修改为存储数组，有些因子有多个有利区间                

                string selectValue = null;
                l1 = new List<int>();
                l2 = new List<double>();//修改为存储数组，有些因子有多个有利区间
                string selectFactor = null;
                double value;
                TDCube.selfsimilarity.cluster.SelectFactor = new List<string>();
                StringBuilder strSelect = new StringBuilder();
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    selectValue = dataGridView1.Rows[i].Cells[0].EditedFormattedValue.ToString();
                    if (selectValue == "True")
                    {
                        l1.Add(i);
                        TDCube.selfsimilarity.cluster.SelectFactor.Add(dataGridView1.Rows[i].Cells[1].EditedFormattedValue.ToString());
                        selectFactor = dataGridView1.Rows[i].Cells[2].EditedFormattedValue.ToString();
                        if (double.TryParse(selectFactor, out value))
                        {
                            l2.Add(value);
                            strSelect.Append(dataGridView1.Rows[i].Cells[1].EditedFormattedValue.ToString()+" "+value+";");
                        }
                        else
                        {
                            MessageBox.Show("输入的数字无效");
                            return;
                        }
                    }
                }
                //设置等级
                int grade;
                if (int.TryParse(textBox1.Text, out grade))
                {
                    TDCube.selfsimilarity.cluster.Grade = grade;
                }
                else
                {
                    MessageBox.Show("等级输入错误！！");
                    return;
                }
                if (grade > l2.Count)
                {
                    MessageBox.Show("等级不能大于要素个数");
                    return;
                }

                //检查路径
                string filenameCSV, filenametxt,finenameDIs;
                if (textBox6.Text.Length > 0)
                {
                    filenameCSV = textBox6.Text;
                    if (!textBox6.Text.EndsWith(".csv"))
                        filenameCSV += ".csv";
                    filenametxt=filenameCSV.Replace(".csv", "_选择的参数.txt");
                    finenameDIs=filenameCSV.Replace(".csv", ".range");
                    TDCube.selfsimilarity.cluster.SaveCSVPath = filenameCSV;
                    TDCube.selfsimilarity.cluster.Dis_range = finenameDIs; 
                }
                else
                {
                    MessageBox.Show("等级输入错误！！");
                    return;
                }
                double  dis;
                if (double.TryParse(textBoxSearchMaxDIs.Text, out dis)&&l1.Count>0)
                {

                            label3.Text = null;
                            haveMess = false;
                            timer1.Start();
                            dt = DateTime.Now;
                            string s = "开始计算时间：" + dt.ToString("yyyy-MM-dd HH:mm:ss") + ", 最小距离:" +dis +", 等级:" + grade + " ,参数：" + strSelect;
                            StreamWriter sw = new StreamWriter(filenametxt, true);
                            sw.WriteLine(s);
                            sw.Close();
                            sa = new SelfsimilarityAnalysis(dis, l1.ToArray(), l2.ToArray(), grade );
                            sa.workByThread(time);
                }
                else
                {
                    MessageBox.Show("请输入整数");
                    return;
                }
            }
            catch (Exception)
            {
                MessageBox.Show("输入格式不对");
                return;
            }
        }

        private void buttonWork2_Click(object sender, EventArgs e)
        {
            ////聚类间隔
            double interval=0;
            int  windowsLong=0;
            if (checkBoxSegment.Checked)
            {
                if (!double.TryParse(textBoxSpaceDis.Text, out interval))
                {
                    MessageBox.Show("分段输入错误！！");
                    return;
                }
            }
            if(checkBoxWindows.Checked)
            {
                if (!int.TryParse(textBoxWindowsMax.Text, out windowsLong))
                {
                    MessageBox.Show("最大窗体输入错误！！");
                    return;
                }
            }
            SelfsimilarityAnalysis sa = new SelfsimilarityAnalysis();
            if (TDCube.selfsimilarity.cluster.Dis_range != null)
            {
                TDCube.selfsimilarity.cluster.DVPath = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_" + interval + "米_dv统计.csv");
                TDCube.selfsimilarity.cluster.SectionPath = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_" + interval + "米_分段统计.csv");
                TDCube.selfsimilarity.cluster.AnalyWindowsPath = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_" + windowsLong + "_窗体分析.csv");
                sa.Cluster_Thread(interval, checkBoxReadDisk.Checked, checkBoxDV.Checked, checkBoxSegment.Checked,windowsLong);
            }
            else
            {
                MessageBox.Show("无数据！！");
                return;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox6.Text = saveFileDialog1.FileName;
            }
        }
        /// <summary>
        /// 从硬盘查看参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            SelfsimilarityAnalysis s2 = new SelfsimilarityAnalysis();
            richTextBox3.Clear();
            richTextBox3.Text = s2.GetMinAndMax();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            of.Filter = "range(*.range)|*.range";
            if (of.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox2.Text = of.FileName;
                TDCube.selfsimilarity.cluster.Dis_range = of.FileName;

                //string filenameGrade, filenamewindows;

                //filenameGrade = of.FileName.Replace(".range", "_聚类分析.csv");
                //filenamewindows = of.FileName.Replace(".range", "_窗体分析.txt");
                //TDCube.selfsimilarity.cluster.SaveGradePath = filenameGrade;
                //TDCube.selfsimilarity.cluster.AnalyWindowsPath = filenamewindows;

                //TDCube.selfsimilarity.cluster.SectionPath = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_分段统计.csv");
                //TDCube.selfsimilarity.cluster.DVPath = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_dv.csv");
            }
        }       
        bool haveMess = false;
        Stopwatch time = new Stopwatch();
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (label3.Text.Equals("") && !time.IsRunning)
            {
                label3.Text = "正在计算参数！！";
            }
            else if (sa != null&&sa.now>0)
            {
                TimeSpan ts = time.Elapsed;
                TimeSpan ts2 = TimeSpan.FromSeconds(ts.TotalSeconds * (CubeInfo.CubeCount - sa.now) / sa.now);
                string value = String.Format("{0}天{1}小时{2}分{3}秒；", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
                string value1 = String.Format("{0}天{1}小时{2}分{3}秒。", ts2.Days, ts2.Hours, ts2.Minutes, ts2.Seconds);
                label3.Text = "已用时间:" + value + "预计剩余时间：" + value1;
                label4.Text = (int)Math.Round(1.0 * sa.now / CubeInfo.CubeCount * 100) + "%";
                progressBar1.Value = (int)Math.Round(1.0 * sa.now / CubeInfo.CubeCount * 100); 
                if (sa.TellWindowsFinish && SelfsimilarityAnalysis.calcEndOfBlock)
                {
                    if (!haveMess)
                    {
                        haveMess = true;
                        //time.Stop();
                        timer1.Enabled = false;
                        progressBar1.Value = 0;
                        //求最值和保存到本地
                        label3.Text = "计算完成";
                        richTextBox1.Clear();
                        richTextBox1.Text = "等级\t" + "最小距离\t" + "最大距离\n";
                        for (int i = 0; i < TDCube.selfsimilarity.cluster.Grade; i++)
                        {
                            richTextBox1.Text = richTextBox1.Text + i + "\t" + SelfsimilarityAnalysis.clusterMin[i] + "\t" + SelfsimilarityAnalysis.clusterMax[i] + "\n";
                        }
                        MessageBox.Show("最小距离计算完成！");
                    }
                }
                else if (SelfsimilarityAnalysis.calcEndOfBlock)
                {
                    label3.Text = "计算完成，用时:" + value + "正在保存数据。";
                     if(time.IsRunning)
                         time.Stop();
                }
            }
        }

        private void buttonMaxMin_Click(object sender, EventArgs e)
        {
            //sa.SetMinAndMaxDis();
            richTextBox1.Clear();
            richTextBox1.Text = "等级\t" + "最小距离\t" + "最大距离\n";
            for (int i = 0; i < TDCube.selfsimilarity.cluster.Grade; i++)
            {
                richTextBox1.Text = richTextBox1.Text + i + "\t" + SelfsimilarityAnalysis.clusterMin[i] + "\t" + SelfsimilarityAnalysis.clusterMax[i] + "\n";
            }
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            //确定距离和要素个数后，用窗体覆盖
            double dis;
            int facture,wl;
            if (TDCube.selfsimilarity.cluster.Dis_range != null&&double.TryParse(textBoxMaxDis.Text, out dis) && int.TryParse(textBoxFactor.Text, out facture) && int.TryParse(textBoxWindowsLong.Text, out wl))
            {
                SelfsimilarityAnalysis sa = new SelfsimilarityAnalysis();
                TDCube.selfsimilarity.cluster.AnalyWindowsPath2 = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_" + facture + "要素下_"+dis+"米窗体分析.csv");
                sa.Work4_Thread(dis, facture,wl, checkBoxReadDisk.Checked);                
            }
            else
            {
                MessageBox.Show("无数据！！");
                return;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int x, y, z;
            if (int.TryParse(textBoxX.Text, out x) && int.TryParse(textBoxY.Text, out y) && int.TryParse(textBoxZ.Text, out z))
            {
                textBoxSpaceDis.Text =Math.Ceiling(Math.Sqrt(Math.Pow(x*CubeInfo._X,2)+Math.Pow(y*CubeInfo._Y,2)+Math.Pow(z*CubeInfo._Z,2))).ToString();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //确定距离和要素个数后，用窗体覆盖
            double dis,spd;
            int facture;
            if (TDCube.selfsimilarity.cluster.Dis_range != null && double.TryParse(textBoxMaxDis.Text, out dis) && int.TryParse(textBoxFactor.Text, out facture) &&double.TryParse(textBoxSpaceDis2.Text, out spd))
            {
                SelfsimilarityAnalysis sa = new SelfsimilarityAnalysis();
                TDCube.selfsimilarity.cluster.Cluster1 = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_" + facture + "要素下_" + dis + "米空间分析1.csv");
                TDCube.selfsimilarity.cluster.Cluster2 = TDCube.selfsimilarity.cluster.Dis_range.Replace(".range", "_" + facture + "要素下_" + dis + "米空间分析2.csv");
                sa.Work5_Thread(dis, facture, spd, checkBoxReadDisk.Checked);
            }
            else
            {
                MessageBox.Show("无数据！！");
                return;
            }
        }


    }
}
