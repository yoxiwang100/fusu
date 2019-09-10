using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;

namespace TDCube
{
    public  class SelfsimilarityAnalysis
    {
        public SelfsimilarityAnalysis(double dis, int[] i, double[] d,int gd)
        {
            searchMaxDis = dis;
            seleId = i;
            factorMinValue = d;
            grade = gd;
        }

        public SelfsimilarityAnalysis()
        {
            //work2
        }
        /// <summary>
        /// 计算最后一个块体，用于控制整个区域是否计算完毕
        /// </summary>
        public  static bool calcEndOfBlock;
        static bool hasGetDisFromDisk=false;
        /// <summary>
        /// 计算的要素个数
        /// </summary>
        static int factorcount;
        /// <summary>
        /// 分段间隔（米）
        /// </summary>
        double ClassificationInterval;
        /// <summary>
        /// 搜索的最大距离
        /// </summary>
        double searchMaxDis;
        int windowsX = 0, windowsY = 0, windowsZ = 0;
        /// <summary>
        /// 选择的序列号，不同等级的要素和
        /// </summary>
        int[] seleId;
        /// <summary>
        /// 选择要素的最小值，不同等级下符合条件的最小距离
        /// </summary>
        public static double[] factorMinValue, clusterMin, clusterMax;
        
        static  int grade;//计算等级
        List<classWindows> lcw;
        columnsInfo Cinfo = new columnsInfo();
        public static List<cluster> Cluster_StListClu = new List<cluster>();//分析等于当前距离的聚类个数
        public static List<cluster> Cluster_StListClu_2 = new List<cluster>();//分析小于当前距离的聚类个数
        /// <summary>
        /// 存储距离，初始值为-1
        /// </summary>
        static double[, ,][] BlockDis = new double [CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum][];
        public void workByThread(System.Diagnostics.Stopwatch stime)
        {
            Thread t = new Thread(new ParameterizedThreadStart(work1_calcDis));
            t.Start(stime);
        }
        /// <summary>
        /// 计算块体在搜索范围内到不同要素的距离
        /// </summary>
        void work1_calcDis(object stime)
        {
            calcSphere();//计算最小距离
            int selectIndex;
            long[] clusterCount = new long[grade];
            double selectMinValue;

            double value = 0;
            bool tmpbool;
            float tmpfloat;
            short tmpshort;
            double tmpdouble;
            int tmpint;
            List<double> li = new List<double>();
            for (int i = 0; i < grade; i++)
            {
                li.Add(-1);
            }
            #region 标记交叉数组
            for (int iz = 0; iz < CubeInfo.Znum; iz++)
            {
                for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                {
                    for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                    {
                        BlockDis[ix, iy, iz] = li.ToArray();//标记交叉数组里面，存入不同等级的距离，默认为-1
                    }
                }
            }
            #endregion
            markFactOnPerBlock = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum][];//初始化为0，标记当前块是否满足条件 
            #region 初始化交叉数字
            for (int ix = 0; ix < markFactOnPerBlock.GetLength(0); ix++)
            {
                for (int iy = 0; iy < markFactOnPerBlock.GetLength(1); iy++)
                {
                    for (int iz = 0; iz < markFactOnPerBlock.GetLength(2); iz++)
                    {
                        markFactOnPerBlock[ix, iy, iz] = new int[seleId.Length];
                    }
                }
            }
            #endregion

            # region 先获取属性信息到交叉数组
            CacheIO.CacheAccessReader CAR = new CacheIO.CacheAccessReader();
            for (int iz = 0; iz < CubeInfo.Znum; iz++)
            {
                for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                {
                    for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                    {
                        for (int ifa = 0; ifa < seleId.Length; ifa++)
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
                                markFactOnPerBlock[ix, iy, iz][ifa] = 1;
                            }
                        }                                                      
                    }
                }
            }
            CAR.Close();
            #endregion
            System.Diagnostics.Stopwatch sss = (System.Diagnostics.Stopwatch)stime;
            sss.Restart();
            getnextblock_finished = false;
            locker_getblock = new object();
            System.Threading.Thread[] thread = new System.Threading.Thread[(int)(Environment.ProcessorCount * 1.25 + 1)];//Environment.ProcessorCount * 1.25 + 1
            threadCount = thread.Count();
            threadFinished = 0;
            calcEndOfBlock = false;
            //多线程开始计算
            for (int i = 0; i < thread.Length; i++)//thread.Length
            {
                thread[i] = new System.Threading.Thread(M_calc_dis);
                thread[i].Start();
            }
        }
        /// <summary>
        /// 多线程个数
        /// </summary>
        int threadCount;
        object locker_getblock;
        int threadFinished;
        int[, ,][] markFactOnPerBlock;
        public static bool getnextblock_finished = false;
        public bool TellWindowsFinish;
        int x_Lock, y_Lock, z_Lock;
        public long now; //当前块体
        private void getnextblock(ref int x, ref int y, ref int z)
        {
            if (x_Lock < CubeInfo.Xnum)
            {
                x = x_Lock;
                y = y_Lock;
                z = z_Lock;
                x_Lock++;
                if (now < CubeInfo.CubeCount)
                    now++;
                return;
            }
            else
            {
                if (y_Lock < CubeInfo.Ynum - 1)
                {
                    x_Lock = 0;
                    y_Lock++;
                    getnextblock(ref x, ref y, ref z);
                }
                else if (z_Lock < CubeInfo.Znum - 1)
                {
                    x_Lock = 0;
                    y_Lock = 0;
                    z_Lock++;
                    getnextblock(ref x, ref y, ref z);
                }
                else
                {
                    getnextblock_finished = true;
                }
            }
            return;
        }
        /// 计算线程，计算每个小块到要素的最小距离
        void M_calc_dis()
        {
            int m_x = 0, m_y = 0, m_z = 0;//多线程中领取的要计算的当前块坐标
            int dis_x = 0, dis_y = 0, dis_z = 0;
            while (!getnextblock_finished)
            {
                lock (locker_getblock)
                {
                    getnextblock(ref m_x, ref m_y, ref m_z);
                    if (getnextblock_finished)
                    {
                        threadFinished++;
                        if (threadCount == threadFinished && calcEndOfBlock)
                        {
                            SetMinAndMaxDis();
                            TellWindowsFinish = true;
                        }
                        return;
                    }
                }

                #region
                //int[] gradeFactorCount;
                //for (int ig = 0; ig < grade; ig++)
                //{
                //    gradeFactorCount = new int[seleId.Length];//计算不同等级时，新建等级
                //    bool canGoToNext = false;
                //    for (int ilw = 0; ilw < lcw.Count; ilw++)//不同辐射范围的块体
                //    {
                //        dis_x = lcw[ilw].X + m_x;
                //        dis_y = lcw[ilw].Y + m_y;
                //        dis_z = lcw[ilw].Z + m_z;
                //        if (0 <= dis_x && dis_x < CubeInfo.Xnum && 0 <= dis_y && dis_y < CubeInfo.Ynum && 0 <= dis_z && dis_z < CubeInfo.Znum)
                //        {
                //            //具体的辐射范围的块体
                //            if (markFactOnPerBlock[dis_x, dis_y, dis_z].Sum() > 0)//只计算有效块体
                //            {
                //                for (int ifa = 0; ifa < seleId.Length; ifa++)//不同要素
                //                {
                //                    if (markFactOnPerBlock[dis_x, dis_y, dis_z][ifa] == 1)//如果当前要素已经满足，不在计算
                //                    {
                //                        gradeFactorCount[ifa] = 1;
                //                    }
                //                    if (gradeFactorCount.Sum() >= (seleId.Length - ig))
                //                    {
                //                        //满足当前等级
                //                        canGoToNext = true;
                //                        distence = lcw[ilw].Dis;
                //                        goto calcNextGrade;
                //                    }
                //                }
                //            }
                //        }
                //    }
                //calcNextGrade:
                //    if (canGoToNext)
                //    {
                //        canGoToNext = false;
                //        BlockDis[m_x, m_y, m_z][ig] = distence;
                //    }
                //    else
                //    {
                //        BlockDis[m_x, m_y, m_z][ig] = -1;//没有找到符合的，距离为-1
                //    }
                //}
                #endregion

                #region 方法二
                int[] gradeFactorCount = new int[seleId.Length];//计算不同等级时，高等级放在前面，也就是说，gradeFactorCount[0]存放最高等级
                int oldFactorCount = 0;//老的要素个数
                for (int ilw = 0; ilw < lcw.Count; ilw++)//不同距离的块体
                {
                    if (gradeFactorCount.Sum() != gradeFactorCount.Length)//如果要素没有填充完gradeFactorCount
                    {
                        dis_x = lcw[ilw].X + m_x;
                        dis_y = lcw[ilw].Y + m_y;
                        dis_z = lcw[ilw].Z + m_z;
                        //计算每一个偏移的块体
                        if (0 <= dis_x && dis_x < CubeInfo.Xnum && 0 <= dis_y && dis_y < CubeInfo.Ynum && 0 <= dis_z && dis_z < CubeInfo.Znum)//防止超出块体范围
                        {
                            //具体的辐射范围的块体
                            if (markFactOnPerBlock[dis_x, dis_y, dis_z].Sum() > 0)//只计算有效块体，总和小于0的直接跳过
                            {
                                for (int ifa = 0; ifa < seleId.Length; ifa++)//不同要素
                                {
                                    if (markFactOnPerBlock[dis_x, dis_y, dis_z][ifa] == 1)//如果当前要素已经满足，更新到数组中
                                    {
                                        gradeFactorCount[ifa] = 1;
                                        if (gradeFactorCount.Sum() > oldFactorCount)//如果要素个数增加，记录当前的距离为该要素下的最小距离
                                        {
                                            BlockDis[m_x, m_y, m_z][gradeFactorCount.Length - oldFactorCount - 1] = lcw[ilw].Dis;//满足当前等级
                                            oldFactorCount = gradeFactorCount.Sum();//更新要素个数
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else//计算完毕
                    {
                        break;
                    }
                }
                #endregion
                if (m_x==CubeInfo.Xnum-1&&m_y==CubeInfo.Ynum-1&&m_z==CubeInfo.Znum-1)//计算完最后一个块体
                    calcEndOfBlock = true;
            }

            //判断是否线程计算完毕
            if (getnextblock_finished)
            {
                threadFinished++;
                if (threadCount == threadFinished && calcEndOfBlock)
                {
                    //System.Windows.Forms.MessageBox.Show("最小距离计算完毕！！");     
                    SetMinAndMaxDis();
                    TellWindowsFinish = true;
                }
                return;
            }
        }
        /// <summary>
        /// 距离开方
        /// </summary>
        public  void SetMinAndMaxDis()
        {
            //计算最值
            SaveDataToCSVFile();
            //序列化到本地
            write2disk();
        }
        
        #region 硬盘读写
        /// <summary>
        /// 磁盘文件写入线程
        /// </summary>
        void write2disk()
        {
            string savePath = TDCube.selfsimilarity.cluster.Dis_range;
            long post1Count = grade * sizeof(double);//每个块体占用的大小
            if(savePath==null)
                savePath = TDCube.selfsimilarity.cluster.SaveCSVPath.Replace(".csv", "_.range");
            //建立空文件包
            SequenceWriter sw = new SequenceWriter(savePath);
            long headseek = sw.SetRange(seleId.Count(),clusterMin, clusterMax,searchMaxDis);//写头文件
            byte[] buffer = new Byte[post1Count];
            for (int xi = 0; xi < CubeInfo.CubeCount; xi++)
            {
                sw.Append(buffer);
            }
            sw.Close();
            //存值
            AccessWriter aw = new AccessWriter(savePath);            
            long pos;
            for (int iz = 0; iz < CubeInfo.Znum; iz++)
            {
                for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                {
                    for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                    {
                        pos = post1Count * (iz * CubeInfo.Xnum * CubeInfo.Ynum + iy * CubeInfo.Xnum + ix) + headseek;
                        for (int ig = 0; ig < grade; ig++)
                        {
                            aw.Writeblock(BlockDis[ix, iy, iz][ig], pos + ig * sizeof(double));
                        }
                    }
                }
            }
            aw.Close();
            
        }
        /// <summary>
        /// 读取保存本地的距离
        /// </summary>
        private void getDisFromDisk()
        {
            //if (hasGetDisFromDisk)
            //{
            //    return;
            //}
            for (int iz = 0; iz < CubeInfo.Znum; iz++)
            {
                for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                {
                    for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                    {
                        BlockDis[ix, iy, iz] = new double[grade];//标记交叉数组里面，存入不同等级的距离
                    }
                }
            }
            SequenceReader sr = new SequenceReader(TDCube.selfsimilarity.cluster.Dis_range);
            sr.GetRange(out factorcount,out clusterMin, out clusterMax, out searchMaxDis);
            //sr.seek2BlockDataBegin();
            grade = clusterMin.Count();
            for (int iz = 0; iz < CubeInfo.Znum; iz++)
            {
                for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                {
                    for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                    {
                        for (int ig = 0; ig < grade; ig++)
                        {
                            BlockDis[ix, iy, iz][ig] = sr.getNextValue();//标记交叉数组里面，存入不同等级的距离
                        }
                    }
                }
            }
            sr.Close();
            hasGetDisFromDisk = true;
        }
        /// <summary>
        /// 从本地读取最值
        /// </summary>
        /// <returns></returns>
        public string GetMinAndMax()
        {
            string returnstr;
            SequenceReader sr = new SequenceReader(TDCube.selfsimilarity.cluster.Dis_range);
            double[] getmin, getmax;
            sr.GetRange(out factorcount, out getmin, out getmax, out searchMaxDis);
            sr.Close();
            grade = getmin.Count();
            if (getmax.Length > 0)
            {
                returnstr = "参与计算要素的个数：" + factorcount + "最大搜索距离：" + searchMaxDis + "\n等级\t最小距离\t最大距离\n";
                for (int ig = 0; ig < getmax.Length; ig++)
                {
                    returnstr =returnstr+ ig +"\t"+ getmin[ig].ToString() +"\t"+ getmax[ig]+"\n";
                }
            }
            else
                returnstr="没有最值！！";
            return returnstr;
        }
        #endregion

        #region 第二步
        bool readdisk = false;
        bool dv = false;//dv统计
        /// <summary>
        /// 分段统计
        /// </summary>
        bool seg = false;
        /// <summary>
        /// 窗体分析中窗体的长度（包含块体的个数），默认为0，
        /// </summary>
        int analyWinWinLong;

        /// <summary>
        /// 各种统计
        /// </summary>
        /// <param name="n"></param>
        public void Cluster_Thread(double n, bool check, bool d,bool se,int winllong)
        {
            ClassificationInterval = n;//分段间隔
            dv = d;
            seg = se;
            readdisk = check;
            analyWinWinLong = winllong;
            Thread t = new Thread(work2);
            t.Start();
        }
        /// <summary>
        /// 聚类分析之后的工作
        /// </summary>
        void work2()
        {
            if (readdisk)
            {
                getDisFromDisk();//读取数据
            }
            Cluster_StListClu.Clear();
            //创建存储聚类统计的数组
            double max=-1;
            for (int ig = 0; ig < grade; ig++)
            {
                if (max < clusterMax[ig])
                    max = clusterMax[ig];
            }
            int iinterval = (int)Math.Ceiling(max / ClassificationInterval);//需要计算的个数
            for (int ig = 0; ig < grade; ig++)
            {
                
                for (int iw = 1; iw <= iinterval; iw++)
                {
                    cluster c = new cluster();
                    c.WindowsLong_double = ClassificationInterval*iw;//                   
                    c.grade = ig;
                    c.PerClusContaiinBlockCount = new List<long>();
                    Cluster_StListClu.Add(c);
                }
            }
            if (seg)
            {
                //聚类分析
                Cluster_analyPerBlock();
                //Cluster_analyPerBlock_2();
                SegmentStatistics();
                //输出分析结果
                //SaveStListClu(); 
            }
            if (dv)
            {
                dv_statistics();
                dv_SaveToCSV();
            }
            //用滑动窗体计算
            if (analyWinWinLong>0)
            analy_Windows();
            System.Windows.Forms.MessageBox.Show("计算完毕！！");
        }
        /// <summary>
        /// 分段聚类统计
        /// </summary>
        void Cluster_analyPerBlock()
        {
            long[][] ret = new long[grade][];
            long perClusContainBlockCount = 0;
            int nowDis = 0;//当前块体所属的分类的Cluster_StListClu中的序号
            MoveWindows w1, w2;
            //把上一步中最值，按照从小到大，等分为窗体，在聚类。。。。转换公式：dis/ClassificationInterval;
            for (int ig = 0; ig < grade; ig++)//计算不同的等级
            {
                int[, ,] MarkPerBlock = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum];//初始化为0，标记当前块是否被遍历 
                for (int iz = 0; iz < CubeInfo.Znum; iz++)
                {
                    for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                    {
                        for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                        {
                            if (MarkPerBlock[ix, iy, iz] == 0)//如果没被便利
                            {
                                MarkPerBlock[ix, iy, iz] = 1;//修改为1，标记为已经遍历
                                if (BlockDis[ix, iy, iz][ig] !=-1)//如果当前块体当前等级下距离大于-1
                                {
                                    nowDis = (int)(Math.Floor(BlockDis[ix, iy, iz][ig] / ClassificationInterval));//当前窗体的大小,从0开始所以用Floor
                                    
                                    perClusContainBlockCount = 1;
                                    Queue<MoveWindows> q = new Queue<MoveWindows>();

                                    w1 = new MoveWindows();//把当前块体作为起点，遍历26邻域块体
                                    w1.xId = ix;
                                    w1.yId = iy;
                                    w1.zId = iz;
                                    q.Enqueue(w1);

                                    #region 循环邻域
                                    while (q.Count > 0)
                                    {
                                        w1 = q.Dequeue();//获取队列里面的一个窗体
                                        //添加、修改邻域。被遍历后，邻域的值都要修改为1，标记为被遍历过
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
                                                        if (MarkPerBlock[qx, qy, qz] == 0 && nowDis == (int)(Math.Floor(BlockDis[qx, qy, qz][ig] / ClassificationInterval)))
                                                        {//当前窗体未被遍历，而且距离和父节点同属同一分段
                                                            w2 = new MoveWindows();
                                                            w2.xId = qx;
                                                            w2.yId = qy;
                                                            w2.zId = qz;

                                                            q.Enqueue(w2);
                                                            MarkPerBlock[w2.xId, w2.yId, w2.zId] = 1;
                                                            perClusContainBlockCount++;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }//end While
                                    #endregion
                                    //添加到结果list中
                                    if (0 <= nowDis && nowDis < Cluster_StListClu.Count/grade)
                                        Cluster_StListClu[nowDis + ig * Cluster_StListClu.Count / grade].PerClusContaiinBlockCount.Add(perClusContainBlockCount);                                   
                                }
                            }
                        }
                    }
                }
            }
            //保存文件
        }
        /// <summary>
        /// 聚类统计每个块体
        /// </summary>
        //void Cluster_analyPerBlock()
        //{
        //    List<long> ListPerClusCount;
        //    long[][] ret = new long[grade][];
        //    long perClusContainBlockCount = 0;
        //    int nowDis = 0;
        //    MoveWindows w1, w2;
        //    //把上一步中最值，按照从小到大，等分为窗体，在聚类。。。。转换公式：(dis-min)*n/(max-min)；等间距为：(max-min)/n
        //    for (int ig = 0; ig < grade; ig++)//计算不同的等级
        //    {
        //        double calc2 = 1 / (clusterMax[ig] - clusterMin[ig]);
        //        int[, ,] MarkPerBlock = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum];//初始化为0，标记当前块是否被遍历 
        //        ListPerClusCount = new List<long>();
        //        for (int iz = 0; iz < CubeInfo.Znum; iz++)
        //        {
        //            for (int iy = 0; iy < CubeInfo.Ynum; iy++)
        //            {
        //                for (int ix = 0; ix < CubeInfo.Xnum; ix++)
        //                {
        //                    if (MarkPerBlock[ix, iy, iz] == 0)//如果没被便利
        //                    {
        //                        MarkPerBlock[ix, iy, iz] = 1;//修改为2，标记为已经遍历
        //                        if (BlockDis[ix, iy, iz][ig] != -1)//如果当前块体当前等级下距离大于-1
        //                        {
        //                            nowDis = (int)(Math.Floor((BlockDis[ix, iy, iz][ig] - clusterMin[ig]) * ClassificationLevel * calc2));//当前窗体的大小

        //                            perClusContainBlockCount = 1;
        //                            Queue<MoveWindows> q = new Queue<MoveWindows>();

        //                            w1 = new MoveWindows();
        //                            w1.xId = ix;
        //                            w1.yId = iy;
        //                            w1.zId = iz;
        //                            q.Enqueue(w1);

        //                            #region 循环邻域
        //                            while (q.Count > 0)
        //                            {
        //                                w1 = q.Dequeue();//获取队列里面的一个窗体
        //                                //添加、修改邻域。被遍历后，邻域的值都要修改为2，标记为被遍历过
        //                                for (int qx = w1.xId - 1; qx <= w1.xId + 1; qx++)
        //                                {
        //                                    for (int qy = w1.yId - 1; qy <= w1.yId + 1; qy++)
        //                                    {
        //                                        for (int qz = w1.zId - 1; qz <= w1.zId + 1; qz++)
        //                                        {
        //                                            if (qx == ix && qy == iy && qz == iz)
        //                                            { }
        //                                            else if (qx >= 0 && qx < CubeInfo.Xnum && qy >= 0 && qy < CubeInfo.Ynum && qz >= 0 && qz < CubeInfo.Znum)
        //                                            {
        //                                                if (MarkPerBlock[qx, qy, qz] == 0 && nowDis == (int)(Math.Floor((BlockDis[qx, qy, qz][ig] - clusterMin[ig]) * ClassificationLevel * calc2)))//当前窗体的大小
        //                                                {
        //                                                    w2 = new MoveWindows();
        //                                                    w2.xId = qx;
        //                                                    w2.yId = qy;
        //                                                    w2.zId = qz;

        //                                                    q.Enqueue(w2);
        //                                                    MarkPerBlock[w2.xId, w2.yId, w2.zId] = 1;
        //                                                    perClusContainBlockCount++;
        //                                                }
        //                                            }
        //                                        }
        //                                    }
        //                                }
        //                            }//end While
        //                            #endregion
        //                            //添加到结果list中
        //                            if (nowDis > ClassificationLevel - 1)
        //                                nowDis = ClassificationLevel - 1;
        //                            if (nowDis < 1)
        //                                nowDis = 0;
        //                            Cluster_StListClu[nowDis + ig * ClassificationLevel].PerClusContaiinBlockCount.Add(perClusContainBlockCount);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    //保存文件
        //}
        /// <summary>
        /// 距离分析，定距离，定要素个数定统计间隔
        /// </summary>
        void Cluster_analyPerBlock_2(double dis, int fac, double spacedis)
        {
            Cluster_StListClu.Clear();
            int iinterval = (int)Math.Ceiling(dis / spacedis);//需要计算的个数
            for (int iw = 1; iw <= iinterval; iw++)
            {
                cluster c = new cluster();
                c.WindowsLong_double = spacedis * iw;//等间隔递增距离
                c.grade = grade-fac;
                c.PerClusContaiinBlockCount = new List<long>();
                Cluster_StListClu.Add(c);
            }
            //创建存储聚类统计的数组
            int nowDis = 0;//当前块体所属的分类的Cluster_StListClu中的序号
            List<long> ListPerClusCount;
            long perClusContainBlockCount = 0;
            MoveWindows w1, w2;

            //把上一步中最值，按照从小到大，等分为窗体，在聚类。。。。转换公式：(dis-min)*n/(max-min)；等间距为：(max-min)/n
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
                                if (BlockDis[ix, iy, iz][grade - fac] != -1 && dis > BlockDis[ix, iy, iz][grade - fac])//如果当前块体当前等级下距离大于-1
                                {
                                    nowDis = (int)(Math.Floor(BlockDis[ix, iy, iz][grade - fac] / spacedis));//当前窗体的大小,从0开始所以用Floor
                                    
                                    perClusContainBlockCount=1;
                                    Queue<MoveWindows> q = new Queue<MoveWindows>();
                                    int[, ,] MarkPerBlock2 = new int[CubeInfo.Xnum, CubeInfo.Ynum, CubeInfo.Znum];//初始化为0，标记当前块是否被遍历 ,用于聚类分析2                

                                    w1 = new MoveWindows();
                                    w1.xId = ix;
                                    w1.yId = iy;
                                    w1.zId = iz;
                                    q.Enqueue(w1);
                                    MarkPerBlock2[ix, iy, iz] = 1;
                                    #region 循环邻域
                                    while (q.Count > 0)
                                    {
                                        w1 = q.Dequeue();//获取队列里面的一个窗体
                                        //添加、修改邻域。被遍历后，邻域的值都要修改为1，标记为被遍历过
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
                                                        if (nowDis >= (int)(Math.Floor(BlockDis[qx, qy, qz][grade - fac]/ spacedis))&& BlockDis[qx, qy, qz][grade - fac] >= 0 && MarkPerBlock2[qx, qy, qz] == 0 )//当前窗体的大小
                                                        {
                                                            w2 = new MoveWindows();
                                                            w2.xId = qx;
                                                            w2.yId = qy;
                                                            w2.zId = qz;
                                                            q.Enqueue(w2);
                                                            MarkPerBlock2[w2.xId, w2.yId, w2.zId] = 1;
                                                            perClusContainBlockCount++;
                                                            //如果当前块体和开始寻找的块体属于一类，以后不再计算
                                                            if (nowDis == (int)(Math.Floor((BlockDis[qx, qy, qz][grade - fac]/spacedis))))
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
                                    //添加到结果list中
                                    if (0 <= nowDis && nowDis < Cluster_StListClu.Count )
                                        Cluster_StListClu[nowDis].PerClusContaiinBlockCount.Add(perClusContainBlockCount);
                                }
                            }
                        }
                    }
                }
            //保存文件
                System.IO.StreamWriter swriter1 = null;
                System.IO.StreamWriter swriter2 = null;
                swriter1 = new System.IO.StreamWriter(TDCube.selfsimilarity.cluster.Cluster1, false, Encoding.Default);
                swriter2 = new System.IO.StreamWriter(TDCube.selfsimilarity.cluster.Cluster2, false, Encoding.Default);
                //获取最大行数
                try
                {
                    StringBuilder strColumn = new StringBuilder();
                    StringBuilder strValue1 = new StringBuilder();
                    StringBuilder strValue2 = new StringBuilder();
                    strColumn.Append("距离");
                    strColumn.Append(",");    
                    strColumn.Append("聚类个数");
                    strColumn.Append(",");
                    swriter1.WriteLine(strColumn.Remove(strColumn.Length - 1, 1));

                    strColumn.Clear();
                    strColumn.Append("聚类序号数");
                    strColumn.Append(",");
                    strColumn.Append("每个聚类包含的块体个数");
                    strColumn.Append(",");
                    swriter2.WriteLine(strColumn.Remove(strColumn.Length - 1, 1));//先去掉最后一个“，”号

                    for (int iw = 0; iw < Cluster_StListClu.Count ; iw++)//最多输出count行，无数据用空格补充
                    {
                        strValue1.Clear();
                        strValue2.Clear();
                        //先输出间隔距离
                        strValue1.Append(Cluster_StListClu[iw].WindowsLong_double);
                        strValue1.Append(",");
                        strValue1.Append(Cluster_StListClu[iw].PerClusContaiinBlockCount.Count);
                        strValue1.Append(",");
                        swriter1.WriteLine(strValue1.Remove(strValue1.Length - 1, 1));//输出一行，包含每一个等级的结果

                        for (int ic = 0; ic < Cluster_StListClu[iw].PerClusContaiinBlockCount.Count; ic++)
                        {
                            strValue2.Clear();
                            strValue2.Append(iw);
                            strValue2.Append(",");
                            strValue2.Append(Cluster_StListClu[iw].PerClusContaiinBlockCount[ic]);
                            strValue2.Append(",");
                            swriter2.WriteLine(strValue2.Remove(strValue2.Length - 1, 1));//输出一行，包含每一个等级的结果
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show(ex.ToString());
                }
                finally
                {
                    if (swriter1 != null)
                    {
                        swriter1.Dispose();
                    }
                    if (swriter2 != null)
                    {
                        swriter2.Dispose();
                    }
                }

        }
        int SlidingWindows_now;//滑动窗体包含的块体的行数
        public static string strresult = null;
        List<WindowsInfo> SlidingWindows_All;
        /// <summary>
        /// 窗体分析
        /// </summary>
        /// <param name="dis"></param>
        /// <param name="gr"></param>
        void analy_Windows()
        {
            //创建窗体，窗体计算公式：L=min+(max-min)/N*i,转换为块体大小：L/Cubin.x;
            SlidingWindows_All = new List<WindowsInfo>();
            int wl = 0, oldWl = -1;
            #region 创建窗体
            for (int ig = 0; ig < grade; ig++)
            {
                for (int ic = 0; ic <= analyWinWinLong; ic++)
                {
                //    wl = (int)Math.Ceiling((clusterMin[ig] + ic * (clusterMax[ig] - clusterMin[ig]) / ClassificationLevel) / CubeInfo._X);
                //    if (wl != oldWl)
                //    {
                        WindowsInfo wi = new WindowsInfo();
                        wi.WindowsXlength = ic;//窗体X轴包含的块体个数
                        wi.Grade = ig;
                        SlidingWindows_All.Add(wi);
                        //oldWl = wl;
                    //}
                }
            }
            #endregion

            long count = 0, wig = 0;
            strresult = "等级:\t" + "一个大窗体X轴包含的小块体个数：\t" + "满足要求的窗体个数:"+ "\n";            
            for (int iw = 0; iw < SlidingWindows_All.Count; iw++)//遍历所有窗体
            {
                count = 0;
                //设置窗体大小
                SlidingWindows sw=new SlidingWindows(SlidingWindows_All[iw].WindowsXlength);
                SlidingWindows_now =SlidingWindows_All[iw].WindowsXlength ;//当前滑动窗体的
                wig = SlidingWindows_All[iw].Grade;
                while (!sw.SetNextSlidingWindows() && SlidingWindows_now>0)
                {
                    //遍历窗体内块体
                    for (int iwz = sw.ZSearchStart; iwz <sw.ZSearchEnd; iwz++)
                    {
                        for (int iwy =sw.YSearchStart; iwy <sw.YSearchEnd ; iwy++)
                        {
                            for (int iwx = sw.XSearchStart; iwx < sw.XSearchEnd; iwx++)
                            {
                                if (-1 < iwx && iwx < CubeInfo.Xnum && -1 < iwy && iwy < CubeInfo.Ynum && -1 < iwz && iwz < CubeInfo.Znum)
                                {
                                    if (BlockDis[iwx, iwy, iwz][wig] > -1 && BlockDis[iwx, iwy, iwz][wig] < SlidingWindows_now*CubeInfo._X)//前窗体满足条件
                                    //if (BlockDis[iwx, iwy, iwz][wig] >-1)//2017年1月2日修改
                                    {
                                        count++;
                                        goto nextWindows;
                                    }
                                }
                            }
                        }
                    }
                nextWindows:
                    {
                    }
                }
                SlidingWindows_All[iw].WindowsCount = count;
            }//end SlidingWindows_All
            //输出txt            
            System.IO.StreamWriter swriter = null;
            swriter = new System.IO.StreamWriter(TDCube.selfsimilarity.cluster.AnalyWindowsPath, false, Encoding.Default);
            //获取最大行数
            try
            {
                StringBuilder strColumn = new StringBuilder();
                StringBuilder strValue = new StringBuilder();       
                int facCount = 0;
                strColumn.Append("一个大窗体X轴包含的小块体个数");
                strColumn.Append(",");
                for (int ig = 0; ig < grade; ig++)
                {
                    facCount = factorcount - ig;
                    strColumn.Append("满足" + facCount + "个要素的窗体个数");
                    strColumn.Append(",");
                }
                swriter.WriteLine(strColumn.Remove(strColumn.Length - 1, 1));//先去掉最后一个“，”号

                for (int iw = 0; iw < SlidingWindows_All.Count/grade; iw++)//最多输出count行，无数据用空格补充
                {
                    strValue.Clear();
                    //先输出间隔距离
                    strValue.Append(SlidingWindows_All[iw].WindowsXlength);
                    strValue.Append(",");
                    for (int ig = 0; ig < grade; ig++)
                    {
                        strValue.Append(SlidingWindows_All[iw + ig * SlidingWindows_All.Count / grade].WindowsCount);
                        strValue.Append(",");
                    }
                    swriter.WriteLine(strValue.Remove(strValue.Length - 1, 1));//输出一行，包含每一个等级的结果
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                if (swriter != null)
                {
                    swriter.Dispose();
                }
            }

            
        }

        /// <summary>
        /// 窗体分析
        /// </summary>
        /// <param name="dis">窗体包含的最大距离</param>
        /// <param name="gr">要素等级</param>
        /// <param name="Wl">窗体包含的最大块体</param>
        void analy_Windows(double dis,int gr,int Wl)
        {
            SlidingWindows_All = new List<WindowsInfo>();
            for (int ic = 1; ic <= Wl; ic++)
            {
                WindowsInfo wi = new WindowsInfo();
                wi.WindowsXlength = ic;//窗体X轴包含的块体个数
                wi.Grade = gr;
                SlidingWindows_All.Add(wi);
            }

            long count = 0, wig = 0;
            strresult = "等级:\t" + "一个大窗体X轴包含的小块体个数：\t" + "满足要求的窗体个数:" + "\n";
            for (int iw = 0; iw < SlidingWindows_All.Count; iw++)//遍历所有窗体
            {
                count = 0;
                SlidingWindows sw = new SlidingWindows(SlidingWindows_All[iw].WindowsXlength);
                SlidingWindows_now = SlidingWindows_All[iw].WindowsXlength;//当前滑动窗体的
                wig = SlidingWindows_All[iw].Grade;
                while (!sw.SetNextSlidingWindows())
                {
                    //遍历窗体内块体
                    for (int iwz = sw.ZSearchStart; iwz < sw.ZSearchEnd; iwz++)
                    {
                        for (int iwy = sw.YSearchStart; iwy < sw.YSearchEnd; iwy++)
                        {
                            for (int iwx = sw.XSearchStart; iwx < sw.XSearchEnd; iwx++)
                            {
                                if (-1 < iwx && iwx < CubeInfo.Xnum && -1 < iwy && iwy < CubeInfo.Ynum && -1 < iwz && iwz < CubeInfo.Znum)
                                {
                                    if (BlockDis[iwx, iwy, iwz][gr] > -1 && BlockDis[iwx, iwy, iwz][gr] < dis)//前窗体满足条件
                                    {
                                        count++;
                                        goto nextWindows;
                                    }
                                }
                            }
                        }
                    }
                nextWindows:
                    {

                    }
                }
                SlidingWindows_All[iw].WindowsCount = count;
            }//end SlidingWindows_All
            System.IO.StreamWriter swriter = null;
            swriter = new System.IO.StreamWriter(TDCube.selfsimilarity.cluster.AnalyWindowsPath2, false, Encoding.Default);
            //获取最大行数
            try
            {
                StringBuilder strColumn = new StringBuilder();
                StringBuilder strValue = new StringBuilder();
                int facCount = 0;
                strColumn.Append("一个大窗体X轴包含的小块体个数");
                strColumn.Append(",");
                facCount = grade - gr;
                strColumn.Append("距离在"+dis+"米内满足" + facCount + "个要素的窗体个数");
                strColumn.Append(",");
                swriter.WriteLine(strColumn.Remove(strColumn.Length - 1, 1));//先去掉最后一个“，”号

                for (int iw = 0; iw < SlidingWindows_All.Count ; iw++)//最多输出count行，无数据用空格补充
                {
                    strValue.Clear();
                    //先输出间隔距离
                    strValue.Append(SlidingWindows_All[iw].WindowsXlength);
                    strValue.Append(",");
                    strValue.Append(SlidingWindows_All[iw].WindowsCount);
                    strValue.Append(",");
                    swriter.WriteLine(strValue.Remove(strValue.Length - 1, 1));//输出一行，包含每一个等级的结果
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                if (swriter != null)
                {
                    swriter.Dispose();
                }
            }
        }
        #endregion

        #region 第三步dv
        /// <summary>
        /// dv统计
        /// </summary>
        void dv_statistics()
        {
            lddv = new List<Dictionary<double, ItemInfo>>();
            for (int ig = 0; ig < grade; ig++)
            {
                List<double> list = new List<double>();
                for (int iz = 0; iz < CubeInfo.Znum; iz++)
                {
                    for (int iy = 0; iy < CubeInfo.Ynum; iy++)
                    {
                        for (int ix = 0; ix < CubeInfo.Xnum; ix++)
                        {
                            list.Add(BlockDis[ix,iy,iz][ig]);
                        }
                    }
                }
                // 集合 dic 用于存放统计结果
                Dictionary<double, ItemInfo> dic = new Dictionary<double, ItemInfo>();

                // 开始统计每个元素重复次数
                foreach (double  v in list)
                {
                    if (dic.ContainsKey(v))
                    {
                        // 数组元素再次，出现次数增加 1
                        dic[v].RepeatNum += 1;
                    }
                    else
                    {
                        // 数组元素首次出现，向集合中添加一个新项
                        // 注意 ItemInfo类构造函数中，已经将重复
                        // 次数设置为 1
                        dic.Add(v, new ItemInfo(v));
                    }
                }
                lddv.Add(dic.OrderBy(p=>p.Key).ToDictionary(p=>p.Key, p=>p.Value));//升序
            }
        }
        List<Dictionary<double, ItemInfo>> lddv;//存放dv统计数据
        /// <summary>
        ///
        /// </summary>
        void dv_SaveToCSV()
        {
            //获取最大行数
            long count = 0;
            foreach (Dictionary<double, ItemInfo> dic2 in lddv)
            {
                if (count < dic2.Keys.Count)
                    count = dic2.Keys.Count;
            }
            StringBuilder strColumn = new StringBuilder();
            StringBuilder strValue = new StringBuilder();
            System.IO.StreamWriter sw = null;
            string SavePath = TDCube.selfsimilarity.cluster.DVPath;
            //计算最值
            try
            {
                sw = new System.IO.StreamWriter(SavePath,false, Encoding.Default);
                int facCount = 0;//满足要素的个数
                long[] disCount=new long[grade];//存入小于当前值的个数
                double dis;
                long dis_count;
                for (int ig = 0; ig < grade; ig++)
                {
                    facCount = factorcount - ig;
                    strColumn.Append("到" + facCount + "个要素下的最小距离");
                    strColumn.Append(",");
                    strColumn.Append("到" + facCount + "个要素下大于该距离的块体个数");
                    strColumn.Append(",");
                }
                sw.WriteLine(strColumn.Remove(strColumn.Length-1,1));
                //写第一行，统计距离为-1的块
                for (int ig = 0; ig < grade; ig++)
                {
                    strValue.Append(lddv[ig].ElementAt(0).Value.Value);
                    strValue.Append(",");
                    strValue.Append(lddv[ig].ElementAt(0).Value.RepeatNum);
                    strValue.Append(",");
                }
                sw.WriteLine(strValue.Remove(strValue.Length-1,1));
                //统计距离大于-1的块
                for (int ic = 1; ic < count; ic++)//写count行
                {
                    strValue.Clear();
                    for (int ig = 0; ig < grade; ig++)//写每一个等级
                    {
                        if (lddv[ig].Keys.Count > ic)//如果存在值
                        {
                            disCount[ig] = disCount[ig] + lddv[ig].ElementAt(ic).Value.RepeatNum;//小于当前面积的累计个数
                            dis = lddv[ig].ElementAt(ic).Value.Value;
                            dis_count = CubeInfo.CubeCount - disCount[ig];
                            strValue.Append(dis);
                            strValue.Append(",");
                            strValue.Append(dis_count);
                            strValue.Append(",");
                        }
                        else//不存在值，用空格
                        {
                            strValue.Append("");
                            strValue.Append(",");
                            strValue.Append("");
                            strValue.Append(",");
                        }
                    }
                    sw.WriteLine(strValue.Remove(strValue.Length-1,1));
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
        class ItemInfo
        {
            /// <summary>
            /// ItemInfo 类记录数组元素重复次数
            /// </summary>
            /// <param name="value">数组元素值</param>
            public ItemInfo(double value)
            {
                Value = value;
                RepeatNum = 1;
            }
            /// <summary>
            /// 数组元素的值
            /// </summary>
            public double Value { get; set; }
            /// <summary>
            /// 数组元素重复的次数
            /// </summary>
            public int RepeatNum { get; set; }
        }
        #endregion

        #region 第四步
        public void Work4_Thread(double dis,int fac,int wl, bool readdisk)
        {
            if (readdisk)
                getDisFromDisk();
            if (grade > fac)
                analy_Windows(dis, grade-fac, wl);
            System.Windows.Forms.MessageBox.Show("计算完成");
        }
        #endregion

        #region 空间聚类
        public void Work5_Thread(double dis, int fac, double  spd, bool readdisk)
        {
            if (readdisk)
                getDisFromDisk();
            if (grade > fac)
                Cluster_analyPerBlock_2(dis, fac, spd);
            System.Windows.Forms.MessageBox.Show("计算完成");
        }
        
        #endregion
        #region 输出
        /// <summary>
        /// 输出块体距离
        /// </summary>
        void SaveDataToCSVFile()
        {
            StringBuilder strColumn = new StringBuilder();
            StringBuilder strValue = new StringBuilder();
            System.IO.StreamWriter sw = null;
            string SavePath = TDCube.selfsimilarity.cluster.SaveCSVPath;
            //计算最值
            clusterMin = new double[grade];
            clusterMax = new double[grade];
            for (int ig = 0; ig < grade; ig++)
            {
                clusterMin[ig] = double.MaxValue;
                clusterMax[ig] = double.MinValue;
            }

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
                                strValue.Append(BlockDis[ix, iy, iz][ig]);
                                if (BlockDis[ix, iy, iz][ig] > -1)
                                {
                                    if (clusterMin[ig] > BlockDis[ix, iy, iz][ig])
                                        clusterMin[ig] = BlockDis[ix, iy, iz][ig];
                                    if (clusterMax[ig] < BlockDis[ix, iy, iz][ig])
                                        clusterMax[ig] = BlockDis[ix, iy, iz][ig];
                                }
                            }
                            sw.WriteLine(strValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
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
        /// 输出统计结果:聚类分析.csv
        /// </summary>
        void SaveStListClu()
        {
            StringBuilder strColumn = new StringBuilder();
            StringBuilder strValue = new StringBuilder();
            StringBuilder strValue_2 = new StringBuilder();
            StringBuilder strValue_3 = new StringBuilder();
            System.IO.StreamWriter sw = null;
            System.IO.StreamWriter sw_2 = null;
            string SaveStListCluPath = TDCube.selfsimilarity.cluster.SaveGradePath;
            if (SaveStListCluPath == null)
            {
                SaveStListCluPath = TDCube.selfsimilarity.cluster.SaveCSVPath.Replace(".csv", "_聚类分析.csv");
            }
            double spacing = 0;//等间距的距离
            try
            {
                sw = new System.IO.StreamWriter(SaveStListCluPath);
                sw_2 = new System.IO.StreamWriter(SaveStListCluPath.Replace("_聚类分析.csv", "_聚类分析_2.csv"));
                int[] outid=new Int32[grade];//存当前存储的ID
                for (int ig = 0; ig < grade; ig++)
                {
                    strColumn.Append("WindowsLong");
                    strColumn.Append(",");
                    strColumn.Append("grade" + ig);
                    strColumn.Append(",");
                    strColumn.Append("Cluster" + ig);
                    strColumn.Append(",");
                }
                sw.WriteLine(strColumn.Remove(strColumn.Length - 1, 1));
                sw_2.WriteLine(strColumn.Remove(strColumn.Length - 1, 1));
                //for (int iw = 0; iw < ClassificationLevel; iw++)
                //{
                    strValue.Remove(0, strValue.Length);
                    //for (int ig = 0; ig < grade; ig++)
                    //{
                    //    spacing = (iw + 1) * (clusterMax[ig] - clusterMin[ig]) / ClassificationLevel + clusterMin[ig];
                    //    strValue.Append(spacing);//等间距*序号                        
                    //    strValue.Append(",");
                    //    strValue.Append(Cluster_StListClu[iw + ig * ClassificationLevel].PerClusContaiinBlockCount.Sum());
                    //    strValue.Append(",");
                    //    strValue.Append(Cluster_StListClu[iw + ig * ClassificationLevel].PerClusContaiinBlockCount.Count);
                    //    strValue.Append(",");
                    //}
                    sw.WriteLine(strValue.Remove(strValue.Length - 1, 1));

                    strValue_2.Remove(0, strValue_2.Length);
                    for (int ig = 0; ig < grade; ig++)
                    {
                        //spacing = (iw + 1) * (clusterMax[ig] - clusterMin[ig]) / ClassificationLevel + clusterMin[ig];
                        //strValue_2.Append(spacing);//等间距*序号
                        //strValue_2.Append(",");
                        //start = strValue_2.Length;
                        //if (Cluster_StListClu_2[iw + ig * ClassificationLevel].PerClusContaiinBlockCount.Count == 0)
                        //{
                        //    strValue_2.Append(0);//共个数
                        //}
                        //else
                        //    strValue_2.Append(Cluster_StListClu_2[iw + ig * ClassificationLevel].PerClusContaiinBlockCount[0]);//共个数
                        //strValue_2.Append(",");
                        //strValue_2.Append(Cluster_StListClu_2[iw + ig * ClassificationLevel].PerClusContaiinBlockCount.Count);
                        //strValue_2.Append(",");
                        //如果聚类超过一个，往下写一行
                        //if (Cluster_StListClu_2[iw + ig * ClassificationLevel].PerClusContaiinBlockCount.Count > 1)
                        //{
                        //    //sw_2.WriteLine(strValue_2.Remove(strValue_2.Length - 1, 1));//写上一行
                        //    strValue_2.Remove(start, strValue_2.Length - start);
                        //    for (int ic = 1; ic < Cluster_StListClu_2[iw + ig * ClassificationLevel].PerClusContaiinBlockCount.Count; ic++)
                        //    { 
                        //        strValue_2.Append(Cluster_StListClu_2[iw + ig * ClassificationLevel].PerClusContaiinBlockCount[ic]);//共个数
                        //        strValue_2.Append(",");
                        //        strValue_2.Append(Cluster_StListClu_2[iw + ig * ClassificationLevel].PerClusContaiinBlockCount.Count);
                        //        strValue_2.Append(",");
                        //        //sw_2.WriteLine(strValue_2.Remove(strValue_2.Length - 1, 1));                              
                        //    }
                        //    strValue_2.Append(",");
                        //}
                    }
                    //sw_2.WriteLine(strValue_2.Remove(strValue_2.Length - 1, 1));
                //}
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
            }
            finally
            {
                if (sw != null)
                {
                    sw.Dispose();
                }
                if (sw_2 != null)
                {
                    sw_2.Dispose();
                }
            }

        }
        /// <summary>
        /// 分段统，计保存为csv
        /// </summary>
        void SegmentStatistics()
        {
            StringBuilder strColumn = new StringBuilder();
            StringBuilder strValue = new StringBuilder();
            System.IO.StreamWriter sw = null;
            string SavePath = TDCube.selfsimilarity.cluster.SectionPath;
            //获取最大行数
            long count = 0;
            foreach (double d in clusterMax)
            {
                if (count < (int)Math.Ceiling(d / ClassificationInterval))
                    count = (int)Math.Ceiling(d / ClassificationInterval);
            }

            try
            {
                sw = new System.IO.StreamWriter(SavePath, false, Encoding.Default);
                int facCount = 0;
                strColumn.Append("分段距离");
                strColumn.Append(",");
                for (int ig = 0; ig < grade; ig++)
                {
                    facCount = factorcount - ig;
                    strColumn.Append("满足" + facCount + "个要素的分段个数");
                    strColumn.Append(",");
                }
                sw.WriteLine(strColumn.Remove(strColumn.Length-1,1));//先去掉最后一个“，”号

                for (int iw = 0; iw <count; iw++)//最多输出count行，无数据用空格补充
                {
                    strValue.Clear();
                    //先输出间隔距离
                    strValue.Append(Cluster_StListClu[iw].WindowsLong_double);
                    strValue.Append(",");
                    for (int ig = 0; ig < grade; ig++)
                    {
                        if (Cluster_StListClu[iw + ig * Cluster_StListClu.Count / grade].PerClusContaiinBlockCount.Count == 0)
                        {
                            strValue.Append("");
                            strValue.Append(",");
                        }
                        else
                        {
                            strValue.Append(Cluster_StListClu[iw + ig * Cluster_StListClu.Count / grade].PerClusContaiinBlockCount.Sum());
                            strValue.Append(",");
                        }
                    }
                    sw.WriteLine(strValue.Remove(strValue.Length-1,1));//输出一行，包含每一个等级的结果
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
        #endregion
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

        class classWindows
        {
            public int X, Y, Z;
            public double Dis;
        }
        /// <summary>
        /// 计算球的排序
        /// </summary>
        void calcSphere()
        {
            //距离转换为块体
            windowsX = (int)Math.Ceiling(searchMaxDis/CubeInfo._X);
            windowsY = (int)Math.Ceiling(searchMaxDis/CubeInfo._Y);
            windowsZ = (int)Math.Ceiling(searchMaxDis / CubeInfo._Z);
            lcw = new List<classWindows>();
            double cdis;
            for (int iz = -windowsZ ; iz <= windowsZ ; iz++)
            {
                for (int iy = -windowsY; iy <= windowsY ; iy++)
                {
                    for (int ix = -windowsX; ix <= windowsX; ix++)
                    {
                        cdis = Math.Sqrt(Math.Pow(ix * CubeInfo._X, 2) + Math.Pow(iy * CubeInfo._Y, 2) + Math.Pow(iz * CubeInfo._Z, 2));//距离
                        if (cdis <= searchMaxDis)
                        {
                            classWindows c = new classWindows();
                            c.X = ix;
                            c.Y = iy;
                            c.Z = iz;
                            c.Dis = cdis;//距离
                            lcw.Add(c);
                        }
                    }
                }
            }
            quicksort(ref lcw,0,lcw.Count-1);
        }
        
        //快速排序
        void quicksort(ref List<classWindows> array, int begin, int end)
        {
            if (begin < 0 || end < 0 || begin > end)
                return;
            int left = begin;
            int right = end;
            classWindows temp;
            temp = array[left];
            while (right != left)
            {
                while (temp.Dis < array[right].Dis && right > left)
                    right--;
                if (right > left)
                {
                    array[left] = array[right];
                    left++;
                }
                while (temp.Dis > array[left].Dis && right > left)
                    left++;
                if (right > left)
                {
                    array[right] = array[left];
                    right--;
                }
            }
            array[right] = temp;
            quicksort(ref array, right + 1, end);
            quicksort(ref array, begin, right - 1);
        }
        /// <summary>
        /// 窗体的聚类
        /// </summary>
        public class cluster
        {
            /// <summary>
            /// 窗体长度,窗体等级
            /// </summary>
            public int WindowsLong, grade;
            /// <summary>
            /// 窗体
            /// </summary>
            public double WindowsLong_double;
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
            public static int NowWindowsID, Grade, WindowsCount, MinWin, MaxWin;
            public static string SaveCSVPath, SaveGradePath;
        }


        public class SlidingWindows
        {
            /// <summary>
            /// 滑动窗体是否滑动完整
            /// </summary>
            public bool Searchfinished;
            /// <summary>
            /// 滑动窗体的范围
            /// </summary>
            public int XSearchStart, XSearchEnd, YSearchStart, YSearchEnd, ZSearchStart, ZSearchEnd;
            /// <summary>
            /// 滑动窗体所能滑动的范围
            /// </summary>
            int  nowSearcLength;//                    
        
            public SlidingWindows(int d)
            {
                nowSearcLength =d;
                XSearchStart = 0;
                YSearchEnd = nowSearcLength;
                ZSearchEnd = nowSearcLength;
            }
            public bool SetNextSlidingWindows()
            {
                if (XSearchEnd < CubeInfo.Xnum)
                {
                    XSearchStart = XSearchEnd;
                    XSearchEnd += nowSearcLength;
                }
                else
                {
                    if (YSearchEnd<CubeInfo.Ynum)
                    {
                        XSearchStart = 0;
                        XSearchEnd = nowSearcLength;

                        YSearchStart = YSearchEnd;
                        YSearchEnd += nowSearcLength;
                    }
                    else if (ZSearchEnd<CubeInfo.Znum)
                    {
                        XSearchStart = 0;
                        XSearchEnd = nowSearcLength;

                        YSearchStart = 0;
                        YSearchEnd =  nowSearcLength;
                        
                        ZSearchStart = ZSearchEnd;
                        ZSearchEnd += nowSearcLength;
                    }
                    else
                        Searchfinished = true;
                }
                //下一个窗体要总体增加一块。
                return Searchfinished;
            }
        }
    }
}
