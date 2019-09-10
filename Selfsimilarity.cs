using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace TDCube
{
    public partial class Selfsimilarity : Form
    {
        public Selfsimilarity()
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
        List<int> l1 ;
        List<double> l2 ;//修改为存储数组，有些因子有多个有利区间
        private void buttonOK_Click(object sender, EventArgs e)
        {
            try
            {
                //检查路径
                string filenameCSV, filenameGrade;
                if (textBox6.Text.Length > 0)
                {
                    filenameCSV = textBox6.Text;
                    if (!textBox6.Text.EndsWith(".csv"))
                        filenameCSV += ".csv";
                    filenameGrade = filenameCSV.Replace(".csv", "_统计.csv");
                }
                else
                {
                    MessageBox.Show("等级输入错误！！");
                    return;
                }
                //窗体大小
                List<WindowsInfo> listWi = new List<WindowsInfo>();
                for (int id = 0; id < dataGridView2.RowCount-1; id++)
                {
                    WindowsInfo wi = new WindowsInfo();
                    wi.WindowsXlength =Convert.ToInt32(dataGridView2.Rows[id].Cells[0].Value);
                    wi.WindowsYlength = Convert.ToInt32(dataGridView2.Rows[id].Cells[1].Value);
                    wi.WindowsZlength = Convert.ToInt32(dataGridView2.Rows[id].Cells[2].Value);
                    listWi.Add(wi);
                }
                //窗体约束
                string selectValue = null;
                l1 = new List<int>();
                l2 = new List<double>();//修改为存储数组，有些因子有多个有利区间
                string selectFactor = null;
                double value;
                TDCube.selfsimilarity.cluster.SelectFactor = new List<string>();
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
                        }
                        else
                        {
                            MessageBox.Show("输入的数字无效");
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
                
                TDCube.selfsimilarity.cluster.WindowsCount = dataGridView2.RowCount-1;
                TDCube.selfsimilarity.cluster.MinWin = li.Min();
                TDCube.selfsimilarity.cluster.MaxWin = li.Max();
                TDCube.selfsimilarity.cluster.SaveCSVPath = filenameCSV;
                TDCube.selfsimilarity.cluster.SaveGradePath = filenameGrade;
                MessageBox.Show("开始计算！！");
                selfsimilarity self = new selfsimilarity(listWi, l1.ToArray(), l2.ToArray());
                self.workForThread();
            }
            catch (Exception)
            {
                MessageBox.Show("输入格式不对");
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            foreach(string s in selfsimilarity.tempSesult )
            {
                richTextBox1.Text = richTextBox1.Text + s + "\n\n";
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            Analysis an = new Analysis();
            an.ShowDialog();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            switch (((System.Windows.Forms.Control)(sender)).Text)
            {
                case "等间隔":
                    groupBox2.Enabled = true;
                    groupBox3.Enabled = false;
                    break;
                case "自定义":
                    groupBox3.Enabled = true;
                    groupBox2.Enabled = false;
                    break;
                default:
                    break;
            }
        }
        List<int> li = new List<int>();
        private void button3_Click(object sender, EventArgs e)
        {
            int min, max, step;
            li.Clear();
            if (int.TryParse(textBox3.Text, out min) && int.TryParse(textBox4.Text, out max) && int.TryParse(textBox5.Text, out step) && min < max)
            {
                for (int i = max; i >= min; i = i - step)
                {
                    li.Add(i);
                }
                li.Sort();
                setdataGridView2();
            }
            else
            {
                MessageBox.Show("输入有误！！");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string s = textBox2.Text;
            string[] s1 = s.Split(';');
            int value;
            li.Clear();
            for (int i = 0; i < s1.Length; i++)
            {
                if (int.TryParse(s1[i], out value))
                    li.Add(value);
            }
            li.Sort();
            setdataGridView2();
        }
        void setdataGridView2()
        {
            if (li.Count != dataGridView2.RowCount - 1)
            {
                dataGridView2.Rows.Clear();
                for (int i = 0; i < li.Count; i++)
                {
                    int index = this.dataGridView2.Rows.Add();
                }
            }            
            if (checkBoxX.Checked)
            {
                for (int i = 0; i < li.Count; i++)
                {
                    dataGridView2.Rows[i].Cells[0].Value = li[i];
                }
            }
            if (checkBoxY.Checked)
            {
                for (int i = 0; i < li.Count; i++)
                {
                    dataGridView2.Rows[i].Cells[1].Value = li[i];
                }
            }
            if (checkBoxZ.Checked)
            {
                for (int i = 0; i < li.Count; i++)
                {
                    dataGridView2.Rows[i].Cells[2].Value = li[i];
                }
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox6.Text = saveFileDialog1.FileName;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            //1124修改计算方法，不用移动窗体，再用最近距离
            try
            {
                //检查路径                
                //窗体大小
                //窗体约束
                string selectValue = null;
                l1 = new List<int>();
                l2 = new List<double>();//修改为存储数组，有些因子有多个有利区间
                string selectFactor = null;
                double value;
                TDCube.selfsimilarity.cluster.SelectFactor = new List<string>();
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
                        }
                        else
                        {
                            MessageBox.Show("输入的数字无效");
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
                int n;
                if (int.TryParse(textBox10.Text, out n))
                {
                }
                else
                {
                    MessageBox.Show("等级输入错误！！");
                    return;
                }
                //检查路径
                string filenameCSV, filenameGrade;
                if (textBox6.Text.Length > 0)
                {
                    filenameCSV = textBox6.Text;
                    if (!textBox6.Text.EndsWith(".csv"))
                        filenameCSV += ".csv";
                    filenameGrade = filenameCSV.Replace(".csv", "_统计.csv");
                    TDCube.selfsimilarity.cluster.SaveCSVPath = filenameCSV;
                    TDCube.selfsimilarity.cluster.SaveGradePath = filenameGrade;
                }
                else
                {
                    MessageBox.Show("等级输入错误！！");
                    return;
                }
                int x, y, z;
                if (int.TryParse(textBox7.Text, out x))
                {
                    if (int.TryParse(textBox8.Text, out y))
                    {
                        if (int.TryParse(textBox9.Text, out z))
                        {
                            //SelfsimilarityAnalysis sa = new SelfsimilarityAnalysis(x, y, z,n, l1.ToArray(), l2.ToArray());
                            //sa.workByThread();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("请输入整数");
                }
            }
            catch (Exception)
            {
                MessageBox.Show("输入格式不对");
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {

        }

        private void Selfsimilarity_Load(object sender, EventArgs e)
        {

        }
    }
    /// <summary>
    /// 自相似
    /// </summary>
    public class selfsimilarity
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="iw">窗体</param>
        /// <param name="i">选择的列的序号</param>
        /// <param name="d">对应的值</param>
        public selfsimilarity(List<WindowsInfo> wi, int[] i, double[] d)
        {
            SlidingWindows_All = wi;
            seleId = i;
            factorCount = new int[d.Length];
            factorMinValue = d;
            minWindowsLong = wi[0].WindowsXlength;
            maxWindowsLong = wi[0].WindowsXlength;//窗体的最值
        }
        columnsInfo Cinfo = new columnsInfo();
        List<WindowsInfo>  SlidingWindows_All;
        int[] seleId;
        int[] factorCount, factorCountQueue;//存储所选要素是否满足条件
        double[] factorMinValue;
        int grade = cluster.Grade;//计算等级
        int startZ = 0, startY = 0, startX = 0,
                endZ = 0,
                endY = 0,
                endX = 0;
        public static  List<string> tempSesult;
        public static List<cluster> StListClu=new List<cluster>();
        int SlidingWindows_now;//滑动窗体包含的块体的行数
        static  int[, ,][] BlockMark = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum][];//初始化为0，标记当前块是否满足条件
        int minWindowsLong ,maxWindowsLong;//窗体的最值
        public void workForThread()
        {            
            Thread t = new Thread(work1);
            t.Start();
        }
        //用线程计算
        void work1()
        {
            tempSesult = null;
            StListClu.Clear();
            //创建存储聚类统计的数组
            for (int ig = 0; ig < grade; ig++)
            {
                for(int iw=0;iw<SlidingWindows_All.Count;iw++)
                {
                    cluster c = new cluster();
                    c.WindowsLong = SlidingWindows_All[iw].WindowsXlength;
                    c.grade = ig;
                    c.PerClusContaiinBlockCount = new List<long>();
                    StListClu.Add(c); 
                }
            }
            int selectIndex;
            long[] clusterCount = new long[grade];
            double selectMinValue;
            
            double value = 0;
            bool tmpbool;
            float tmpfloat;
            short tmpshort;
            double tmpdouble;
            int tmpint;
            bool endWindows = false;
            tempSesult = new List<string>();//不同等级存入不同list
            for (int ig = 0; ig < grade; ig++)
            {
                tempSesult.Add("移动窗体的尺寸" + "\t" + "窗体总个数" + "\t" + "聚类" + "\t" + "等级\n");
            }
            for (int ix = 0; ix < CubeInfo.Xnum; ix++)
            {
                for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                {
                    for (int iz = 0; iz < CubeInfo.Znum; iz++)
                    {
                        BlockMark[ix, iy, iz] = new int[grade];//标记交叉数组里面，存入不同等级的标记
                    }
                }
            }
            int[, ,][] arrPerBlock = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum][];//初始化为0，标记当前块是否满足条件，值为所属窗体的边长
            #region 初始化交叉数字
            for (int ix = 0; ix < arrPerBlock.GetLength(0); ix++)
            {
                for (int iy = 0; iy < arrPerBlock.GetLength(1); iy++)
                {
                    for (int iz = 0; iz < arrPerBlock.GetLength(2); iz++)
                    {
                        arrPerBlock[ix, iy, iz] = new int[factorCount.Length];
                    }
                }
            }
            #endregion
            //先获取属性信息到交叉数组
            #region 取值
            CacheIO.CacheAccessReader CAR = new CacheIO.CacheAccessReader();
            for(int iz=0;iz<CubeInfo.Znum;iz++)
            {
                for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                {
                    for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                    {

                        for (int ifa = 0; ifa < factorCount.Length; ifa++)
                        {
                            #region 取值
                            selectIndex = seleId[ifa];
                            selectMinValue = factorMinValue[ifa];
                            switch (Cinfo.Getcolumn(selectIndex).type)
                            {
                                case Difftype.布尔:
                                    CAR.GetCell(ix, iy, iz, Cinfo.GetByteStartPositionWithFlag(selectIndex), out tmpbool);
                                    value = Convert.ToDouble(tmpbool);
                                    break;
                                case Difftype.单精度:
                                    CAR.GetCell(ix, iy, iz, Cinfo.GetByteStartPositionWithFlag(selectIndex), out tmpfloat);
                                    value = tmpfloat;
                                    break;
                                case Difftype.短整型:
                                    CAR.GetCell(ix, iy, iz, Cinfo.GetByteStartPositionWithFlag(selectIndex), out tmpshort);
                                    value = tmpshort;
                                    break;
                                case Difftype.双精度:
                                    CAR.GetCell(ix, iy, iz, Cinfo.GetByteStartPositionWithFlag(selectIndex), out tmpdouble);
                                    value = tmpdouble;
                                    break;
                                case Difftype.长整型:
                                    CAR.GetCell(ix, iy, iz, Cinfo.GetByteStartPositionWithFlag(selectIndex), out tmpint);
                                    value = tmpint;
                                    break;
                            }
                            #endregion 取值
                            if (value > selectMinValue)
                            {
                                arrPerBlock[ix,iy,iz][ifa] = 1;
                            }
                        }
                    }
                }
            }
            CAR.Close();
            # endregion
            int tempStartX;//优化算法的时候，修改窗体计算的范围
            tempStartX  = 0;
            for (int iw = 0; iw < SlidingWindows_All.Count; iw++)//遍历所有窗体
            {
                SlidingWindows_now = SlidingWindows_All[iw].WindowsXlength;//当前滑动窗体的X长度
                startZ = 0; startY = 0; startX = 0;
                endZ = SlidingWindows_All[iw].WindowsXlength;
                endY = SlidingWindows_All[iw].WindowsYlength;
                endX = SlidingWindows_All[iw].WindowsZlength;
                double tempSlidingWindows_now = SlidingWindows_now;
                Queue<int[]> qeMarkFactor=new Queue<int[]>();//存储的当前沿着Y轴上的一个切片的要素个数，右侧每添加一片，左侧减少一片
                int[] markFactor = new int[SlidingWindows_All[iw].WindowsXlength];
                bool[] firstSee = new bool[grade];//是否第一次遇到满足条件的窗体
                factorCountQueue = new int[SlidingWindows_All[iw].WindowsXlength];
                while (!endWindows)
                {
                    #region 计算窗体内所有的块体
                    factorCount = new int[factorCount.Length];//要素是否满足条件
                    if (startX == 0)
                    {
                        qeMarkFactor.Clear();
                        firstSee.Initialize();
                        for (int ix = startX; ix < startX + SlidingWindows_All[iw].WindowsXlength; ix++)
                        {
                            for (int iz = startZ; iz < startZ + SlidingWindows_All[iw].WindowsZlength; iz++)
                            {
                                for (int iy = startY; iy < startY + SlidingWindows_All[iw].WindowsYlength; iy++)//把最右侧的Y轴侧所有的值变成一个值
                                {
                                    //判断所选要素是否符合条件
                                    if (CubeInfo.Xnum > ix && CubeInfo.Ynum > iy && CubeInfo.Znum > iz)
                                    {
                                        if (arrPerBlock[ix, iy, iz].Max() > 0)
                                        {//具体到每一个块体
                                            for (int ifa = 0; ifa < factorCount.Length; ifa++)
                                            {
                                                if (arrPerBlock[ix, iy, iz][ifa] == 1)
                                                {
                                                    factorCount[ifa] = 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            qeMarkFactor.Enqueue(factorCount);//添加当前片
                        }
                    }
                    else
                    {
                        int ix =startX + SlidingWindows_All[iw].WindowsXlength-1;//向右侧滑动一个块体
                        if (ix < CubeInfo.Xnum)
                        {
                            //添加一个最新的Y轴侧，取出一个最左侧
                            qeMarkFactor.Dequeue();
                            for (int iz = startZ; iz < startZ + SlidingWindows_All[iw].WindowsZlength; iz++)
                            {
                                for (int iy = startY; iy < startY + SlidingWindows_All[iw].WindowsYlength; iy++)//把最右侧的Y轴侧所有的值变成一个值
                                {
                                    //判断所选要素是否符合条件
                                    if (CubeInfo.Ynum > iy && CubeInfo.Znum > iz)
                                    {
                                        if (arrPerBlock[ix, iy, iz].Max() > 0)
                                        {//具体到每一个块体
                                            for (int ifa = 0; ifa < factorCount.Length; ifa++)
                                            {
                                                if (arrPerBlock[ix, iy, iz][ifa] == 1)
                                                {
                                                    factorCount[ifa] = 1;
                                                }
                                                if (factorCount.Min() > 0)//如果最小值大于0，说明都大于0，跳出当前计算
                                                {
                                                    qeMarkFactor.Enqueue(factorCount);
                                                    goto canbreakWindows;
                                                }
                                            }
                                            //统计markFactor
                                        }
                                    }
                                }
                            }
                            qeMarkFactor.Enqueue(factorCount);
                        }
                            
                    }
                    
                    #endregion
                    #region 窗体计算
                canbreakWindows://优化修改1112
                    //计算队列里面的内容
                    factorCount = new int[factorCount.Length];
                for (int ix = 0; ix < qeMarkFactor.Count; ix++)
                    {
                        factorCountQueue=qeMarkFactor.ToArray()[ix];
                        for (int iF = 0; iF < factorCount.Length; iF++)
                        {
                            if (factorCountQueue[iF] == 1)
                                factorCount[iF]= 1;
                        }
                    }

                for (int ig = 0; ig < grade; ig++)
                {
                    
                    if (factorCount.Sum() >= (seleId.Length - ig))
                    {
                        //修改是否遇到符合要求的窗体的标记
                        firstSee[ig] =!firstSee[ig];//取反
                        //修改窗体的范围
                        if (startX == 0 || firstSee[ig])
                        {
                            tempStartX = startX;                            
                        }
                        else
                        {
                            tempStartX = startX + SlidingWindows_All[iw].WindowsXlength - 1;
                        }
                        //修改块体所属最小窗体的值
                        for (int iz = startZ; iz < startZ + SlidingWindows_All[iw].WindowsZlength; iz++)
                        {
                            for (int iy = startY; iy < startY + SlidingWindows_All[iw].WindowsYlength; iy++)
                            {
                                for (int ix = tempStartX; ix < startX + SlidingWindows_All[iw].WindowsYlength; ix++)
                                {
                                    if (CubeInfo.Xnum > ix && CubeInfo.Ynum > iy && CubeInfo.Znum > iz)
                                    {
                                        if (BlockMark[ix, iy, iz][ig] == 0)
                                        {
                                            BlockMark[ix, iy, iz][ig] = SlidingWindows_now;
                                        }
                                        else if (BlockMark[ix, iy, iz][ig] > SlidingWindows_now)
                                        {
                                            BlockMark[ix, iy, iz][ig] = SlidingWindows_now;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        firstSee[ig]=false;
                    }
                }
                    #endregion
                    #region 修改窗体范围
                    if (startX >= CubeInfo.Xnum - SlidingWindows_All[iw].WindowsXlength)//X到头
                    {
                        if (startY >= CubeInfo.Ynum - SlidingWindows_All[iw].WindowsYlength)
                        {
                            if (startZ >= CubeInfo.Znum - SlidingWindows_All[iw].WindowsZlength)
                            {
                                endWindows = true;
                            }
                            else
                            {
                                startZ = startZ + 1;
                                startY = 0;
                                startX = 0;
                            }
                        }
                        else
                        {
                            startY = startY + 1;
                            startX = 0;
                        }
                    }
                    else
                    {
                        startX = startX + 1;
                    }
                    #endregion 修改窗体范围                    
                }//end while
                //聚类，统计
                endWindows = false;//计算下一个窗体
            }
            //聚类分析
            analyPerBlock();
            SaveDataToCSVFile();
            MessageBox.Show("计算完成");
        }
        /// <summary>
        /// 聚类统计每个块体
        /// </summary>
        void analyPerBlock()
        {
            List<long> ListPerClusCount;
            long[][] ret = new long[grade][];
            long perClusContainBlockCount = 0;
            int nowWindow=0;
            MoveWindows w1,w2;
            for (int ig = 0; ig < grade; ig++)//计算不同的等级
            {
                int[, ,] MarkPerBlock = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum];//初始化为0，标记当前块是否被遍历 
                ListPerClusCount = new List<long>();
                for (int iz = 0; iz < CubeInfo.Znum; iz++)
                {
                    for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                    {
                        for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                        {
                            if (MarkPerBlock[ix, iy, iz] == 0)//如果没被遍历
                            {
                                MarkPerBlock[ix, iy, iz] = 1;//修改为2，标记为已经遍历
                                nowWindow = BlockMark[ix, iy, iz][ig];//当前窗体的大小
                                if (nowWindow != 0)
                                {
                                    perClusContainBlockCount = 1;
                                    Queue<MoveWindows> q = new Queue<MoveWindows>();
                                    int[, ,] MarkPerBlock2 = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum];//初始化为0，标记当前块是否被遍历 ,用于聚类分析2                

                                    w1 = new MoveWindows();
                                    w1.xId = ix;
                                    w1.yId = iy;
                                    w1.zId = iz;
                                    q.Enqueue(w1);

                                    #region 循环邻域
                                    while (q.Count > 0)
                                    {
                                        w1 = q.Dequeue();//获取队列里面的一个窗体
                                        //添加、修改邻域。被遍历后，邻域的值都要修改为2，标记为被遍历过
                                        for (int qx = w1.xId - 1; qx <= w1.xId + 1; qx++)
                                        {
                                            for (int qy = w1.yId - 1; qy <= w1.yId + 1; qy++)
                                            {
                                                for (int qz = w1.zId - 1; qz <= w1.zId + 1; qz++)
                                                {
                                                    if (qx == ix && qy == iy && qz == iz)
                                                    { }
                                                    else if (qx >= 0 && qx < CubeInfo.Xnum && qy >= 0 && qy < CubeInfo.Ynum && qz >= 0 && qz < CubeInfo.Znum)
                                                    {
                                                        if (MarkPerBlock[qx, qy, qz] == 0 && nowWindow > BlockMark[qx, qy, qz][ig] && BlockMark[qx, qy, qz][ig]>0)
                                                        {
                                                            w2 = new MoveWindows();
                                                            w2.xId = qx;
                                                            w2.yId = qy;
                                                            w2.zId = qz;

                                                            if (MarkPerBlock2[w2.xId, w2.yId, w2.zId] == 1)//已经添加
                                                            {

                                                            }
                                                            else
                                                            {
                                                                q.Enqueue(w2);
                                                                MarkPerBlock2[w2.xId, w2.yId, w2.zId] = 1;
                                                                perClusContainBlockCount++;
                                                            }
                                                            if (nowWindow == BlockMark[w2.xId, w2.yId, w2.zId][ig])
                                                            {
                                                                MarkPerBlock[w2.xId, w2.yId, w2.zId] = 1;
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }//end While
                                    #endregion 
                                    for (int il = 0; il < StListClu.Count; il++)
                                    {
                                        if (StListClu[il].grade == ig && StListClu[il].WindowsLong == nowWindow)
                                        {
                                            StListClu[il].PerClusContaiinBlockCount.Add(perClusContainBlockCount);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            //保存文件
            SaveStListClu();
        }
        /// <summary>
        /// 分析窗体
        /// </summary>
        /// <param name="arrset"></param>
        /// <param name="clus"></param>
        long[][] work2(int[, ,][] arrset, ref long[] clus, ref long con)
        {
            //遇到1时，修改为2，标记为已经便利，遍历26邻域，存入到队列中，遍历队列，
            List<long> ListPerClusCount;
            long[][] ret=new long[grade][];
            long perClusContainBlockCount = 0;
            int conCount = 0;
            MoveWindows w;
            int windowsXNub = arrset.GetLength(0),
                windowsYNum = arrset.GetLength(1),
                windowsZNub = arrset.GetLength(2);//窗体最值            
            for (int ig = 0; ig < grade; ig++)
            {
                conCount = 0;
                ListPerClusCount = new List<long>();
                for (int x = 0; x < windowsXNub; x++)
                {
                    for (int y = 0; y < windowsYNum; y++)
                    {
                        for (int z = 0; z < windowsZNub; z++)
                        {
                            if (arrset[x, y, z][ig] == 1)
                            {
                                arrset[x, y, z][ig] = 2;//修改为2，标记为已经遍历
                                perClusContainBlockCount = 1;
                                Queue<MoveWindows> q = new Queue<MoveWindows>();

                                w = new MoveWindows();
                                w.xId = x;
                                w.yId = y;
                                w.zId = z;
                                q.Enqueue(w);

                                #region 循环邻域
                                while (q.Count > 0)
                                {
                                    w = q.Dequeue();//获取队列里面的一个窗体
                                    //添加、修改邻域。被遍历后，邻域的值都要修改为2，标记为被遍历过
                                    for (int qx = w.xId - 1; qx <= w.xId + 1; qx++)
                                    {
                                        for (int qy = w.yId - 1; qy <= w.yId + 1; qy++)
                                        {
                                            for (int qz = w.zId - 1; qz <= w.zId + 1; qz++)
                                            {
                                                if (qx == x && qy == y && qz == z)
                                                { }
                                                else if (qx >= 0 && qx < windowsXNub && qy >= 0 && qy < windowsYNum && qz >= 0 && qz < windowsZNub)
                                                {
                                                    if (arrset[qx, qy, qz][ig] == 1)
                                                    {
                                                        w = new MoveWindows();
                                                        w.xId = qx;
                                                        w.yId = qy;
                                                        w.zId = qz;

                                                        q.Enqueue(w);
                                                        arrset[w.xId, w.yId, w.zId][ig] = 2;
                                                        perClusContainBlockCount++;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }//end While
                                #endregion
                                conCount++;
                                ListPerClusCount.Add(perClusContainBlockCount );
                            }//end for(y)
                        }//end for(x)
                    }
                }
               ret[ig]= ListPerClusCount.ToArray();
               clus[ig] = conCount;
            }
            return ret;
        }
        /// <summary>
        /// 修改块体值
        /// </summary>
        void SaveDataToCSVFile()
        {
            StringBuilder strColumn = new StringBuilder();
            StringBuilder strValue = new StringBuilder();
            System.IO.StreamWriter sw = null;
            string SavePath = TDCube.selfsimilarity.cluster.SaveCSVPath;// @"D:\111\自相似.csv";
            try
            {
                sw = new System.IO.StreamWriter(SavePath);
                strColumn.Append("X");
                strColumn.Append(",");
                strColumn.Append("Y");
                strColumn.Append(",");
                strColumn.Append("Z");
                for (int ig = 0; ig < grade; ig++)
                {
                    strColumn.Append(",");
                    strColumn.Append("grade" + ig);
                }
                sw.WriteLine(strColumn);
                for (int iz = 0; iz < CubeInfo.Znum; iz++)
                {
                    for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                    {
                        for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                        {
                            strValue.Remove(0, strValue.Length);
                            strValue.Append(CubeInfo.minX + CubeInfo._X * ix);
                            strValue.Append(",");
                            strValue.Append(CubeInfo.minY + CubeInfo._Y * iy);
                            strValue.Append(",");
                            strValue.Append(CubeInfo.minZ + CubeInfo._Z * iz);
                            for (int ig = 0; ig < grade; ig++)
                            {
                                strValue.Append(",");
                                strValue.Append(BlockMark[ix, iy, iz][ig]);
                            }
                            sw.WriteLine(strValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error in SaveDataToCSVFile!");
            }
            finally
            {
                if (sw != null)
                {
                    sw.Dispose();
                }
            }
        }
        void SaveStListClu()
        {
            StringBuilder strColumn = new StringBuilder();
            StringBuilder strValue = new StringBuilder();
            System.IO.StreamWriter sw = null;
            string SaveStListCluPath = TDCube.selfsimilarity.cluster.SaveGradePath;// 
            try
            {
                sw = new System.IO.StreamWriter(SaveStListCluPath);
                strColumn.Append("WindowsLong");
                for (int ig = 0; ig < grade; ig++)
                {
                    strColumn.Append(",");
                    strColumn.Append("grade" + ig);
                    strColumn.Append(",");
                    strColumn.Append("Cluster" + ig);
                }

                sw.WriteLine(strColumn);
                for (int iw = 0; iw < SlidingWindows_All.Count; iw++)
                {
                    strValue.Remove(0, strValue.Length);
                    strValue.Append(SlidingWindows_All[iw].WindowsXlength);//添加窗体边长
                    for (int ig = 0; ig < grade; ig++)
                    {
                        strValue.Append(",");
                        strValue.Append(StListClu[iw + ig * SlidingWindows_All.Count].PerClusContaiinBlockCount.Sum());//添加窗体个数
                        strValue.Append(",");
                        strValue.Append(StListClu[iw + ig * SlidingWindows_All.Count].PerClusContaiinBlockCount.Count); //添加聚类个数
                    }
                    sw.WriteLine(strValue);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("Error in SaveDataToCSVFile!");
            }
            finally
            {
                if (sw != null)
                {
                    sw.Dispose();
                }
            }
 
        }
        /// <summary>
        /// 移动窗体
        /// </summary>
        public class MoveWindows
        {
            /// <summary>
            /// 窗体最大坐标
            /// </summary>
            public int xId, yId, zId;//为了邻域分析用
        }
        /// <summary>
        /// 窗体的聚类
        /// </summary>
        public class cluster
        {
            /// <summary>
            /// 窗体长度,窗体等级
            /// </summary>
            public int WindowsLong,grade;
            /// <summary>
            /// 聚类个数
            /// </summary>
            //public long ClusterCount;//
            /// <summary>
            /// 每个聚类里面包含的块体个数
            /// </summary>
            public List<long> PerClusContaiinBlockCount;
            /// <summary>
            /// 所选要素
            /// </summary>
            public static List<string> SelectFactor;
            /// <summary>
            /// 当前选择的窗体ID
            /// </summary>
            public static int NowWindowsID, Grade, WindowsCount,MinWin,MaxWin;
            /// <summary>
            /// SaveCSVPath:到处csv的路径
            /// SaveGradePath：聚类分析
            /// AnalyWindowsPath
            /// Dis：存储分析的不同块头中的满足要求的最小距离
            /// </summary>
            public static string SaveCSVPath, SaveGradePath, AnalyWindowsPath;
            /// <summary>
            /// range路径
            /// </summary>
            public static string Dis_range;
            /// <summary>
            /// 等距离分段统计路径
            /// </summary>
            public static string SectionPath;
            /// <summary>
            /// dv统计
            /// </summary>
            public static string DVPath;
            /// <summary>
            /// 定距离和要素个数下的窗体分析
            /// </summary>
            public static string AnalyWindowsPath2;
            /// <summary>
            /// 定要素个数和距离的空间分析
            /// </summary>
            public static string Cluster1, Cluster2;
        }
    }
    
    public class WindowsInfo
    {
        public int WindowsXlength, WindowsYlength, WindowsZlength;
        public long WindowsCount,Grade;
    }
}
    

