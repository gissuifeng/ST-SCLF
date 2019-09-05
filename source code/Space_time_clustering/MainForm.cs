using Space_time_clustering.ST_COLOCATION;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Space_time_clustering
{
    public partial class MainForm : Form
    {

        /// spatial neihbor metrix list
        List<ODPair> topoList = null;
        /// OD unit record list
        List<Record> recordList = null;
        /// <OD ID joint string, Record object>  Dictionary list.
        Dictionary<string, List<Record>> od_recordList = null;

        /// Total outbound of all cities per day
        Dictionary<string, OStaRecord> osta_RecordList = null;
        /// Total inbound of all cities per day
        Dictionary<string, DStaRecord> dsta_RecordList = null;

        /// Time threashold
        int TimeThread = 172800;
        /// flow rate threashold
        double flow_threshHold = 0.08;

        /// The origin city total outbound file and the total inbound table need to add a header when loading data for the first time.
        bool isFirstWrite_O = true; // Is inbound first write
        bool isFirstWrite_D = true; // Is outbound first write

        /// Create deleget for progressbar, button and output message window.   
        public delegate void ChangeProgress(int value); //progresssbar  
        public delegate void ChangeButton(int value); //button 
        public delegate void ChangeMemoedit(string value); //message window 

        /// Create deleget variables for progressbar, button and messge window.  
        public ChangeProgress changeProgerss;
        public ChangeButton changebtn;
        public ChangeMemoedit changeMemo;

        
        /// Real data set
        /// default workspace that store all temporal file and result data.
        string WorkspacePath = @"F:\experimental data\result"; 
        /// file path of spatial neighbor metrix.
        string TopoMetrixFileName = @"F:\experimental data\spatial_neighbor_metrix.csv";
        /// file path of OD flow unit data
        string ODFlowUnitFileName = @"F:\experimental data\flow_data_sub_2018.csv";
        /// result file
        string resultfile = @"F:\experimental data\result\result.csv";

        /// Time interval of all data
        DateTime dt1 = Convert.ToDateTime("2018-1-1 00:00:00");
        DateTime dt2 = Convert.ToDateTime("2018-12-31 12:00:00");

        /// <summary>
        /// Construction method--Form1
        /// </summary>
        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            #region <data read>
            /// Get the topological relationship Metrix data set
            topoList = GetTopologyHashMap(TopoMetrixFileName);

            /// Get OD flow unit data set
            recordList = GetRecordList(ODFlowUnitFileName);
            #endregion

            /// Set Max size of progressBar to the total OD units. 
            progressBar1.Maximum = recordList.Count;

            /// Obtain and count the total output of each O and store it
            string O_Statis_VAL_CsvFile = string.Format(@"{0}\{1}", WorkspacePath, "O_stat_val.csv");
            osta_RecordList = O_DegreeCalbyDay(recordList, O_Statis_VAL_CsvFile, dt1, dt2);

            /// Obtain and count the total input of each O and store it
            string D_Statis_VAL_CsvFile = string.Format(@"{0}\{1}", WorkspacePath, "D_stat_val.csv");
            dsta_RecordList = D_DegreeCalbyDay(recordList, D_Statis_VAL_CsvFile, dt1, dt2);

            /// Get hashMap of recordList
            od_recordList = getRecordHashMap(recordList);

            /// set buttton1 unable active while executing the task.
            btn_execute.Enabled = false;

            //Create a new thread and execut computing task.  
            System.Threading.Thread thr = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(executeFun));
            thr.Start();

        }

        /// <summary>
        /// Dictionary for construct Origine object contains many attributes.
        /// </summary>
        /// <param name="recordList">OD record list</param>
        /// <param name="filePath">Origine topo file</param>
        /// <param name="fromDatetime">start time of flow</param>
        /// <param name="toDatetime">end time of flow</param>
        /// <returns></returns>
        public Dictionary<string, OStaRecord> O_DegreeCalbyDay(List<Record> recordList, string filePath, DateTime fromDatetime, DateTime toDatetime)
        {
            List<Record> list = null; 
            DateTime dt_1;
            DataTable temp_tb = null;
            Dictionary<string, OStaRecord> olist = new Dictionary<string, OStaRecord>();
            for (DateTime dt = fromDatetime; dt < toDatetime; dt=dt.AddDays(1))
            {
                list = new List<Record>();

                foreach (Record item in recordList)
                {
                    
                    if (item.BeginTime >= dt && item.BeginTime < (dt_1 = dt.AddDays(1)))
                    {
                        list.Add(item);
                    }
                }

                if (list.Count > 0)
                {
                    if (isFirstWrite_O)
                    {
                        temp_tb = Generate_O_Statis_VAL_Table(list, filePath, dt);

                        isFirstWrite_O = false;
                    }
                    else
                    {
                        temp_tb = Generate_O_Statis_VAL_Table(list, filePath, dt, true);
                    }

                    OStaRecord record = null;
                    for (int i = 0; i < temp_tb.Rows.Count; i++)
                    {
                        record = new OStaRecord(Convert.ToDateTime(temp_tb.Rows[i]["o_datetime"]), Convert.ToString(temp_tb.Rows[i]["o_stop"]), Convert.ToInt32(temp_tb.Rows[i]["val"]));
                        olist.Add(record.datetime + "," + record.o_stop, record);
                    }
                }

            }
            return olist;
        }

        /// <summary>
        /// Dictionary for construct Destination object contains many attributes.
        /// </summary>
        /// <param name="recordList">OD record list</param>
        /// <param name="filePath">Destination topo file</param>
        /// <param name="fromDatetime">start time of flow</param>
        /// <param name="toDatetime">end time of flow</param>
        /// <returns></returns>
        public Dictionary<string, DStaRecord> D_DegreeCalbyDay(List<Record> recordList, string filePath, DateTime fromDatetime, DateTime toDatetime)
        {
            List<Record> list = null;
            DateTime dt_1;
            DataTable temp_tb = null;
            Dictionary<string, DStaRecord> dlist = new Dictionary<string, DStaRecord>();
            for (DateTime dt = fromDatetime; dt < toDatetime; dt = dt.AddDays(1))
            {
                list = new List<Record>();;

                foreach (Record item in recordList)
                {

                    if (item.EndTime >= dt && item.EndTime < (dt_1 = dt.AddDays(1)))
                    {
                        list.Add(item);
                    }
                }

                if (list.Count > 0)
                {
                    if (isFirstWrite_D)
                    {
                        temp_tb = Generate_D_Statis_VAL_Table(list, filePath, dt);

                        isFirstWrite_D = false;
                    }
                    else
                    {
                        temp_tb = Generate_D_Statis_VAL_Table(list, filePath, dt, true);
                    }

                    DStaRecord record = null;
                    for (int i = 0; i < temp_tb.Rows.Count; i++)
                    {
                        record = new DStaRecord(Convert.ToDateTime(temp_tb.Rows[i]["d_datetime"]), Convert.ToString(temp_tb.Rows[i]["d_stop"]), Convert.ToInt32(temp_tb.Rows[i]["val"]));
                        dlist.Add(record.datetime + "," + record.d_stop, record);
                    }
                }
            }

            return dlist;
        }

        /// <summary>
        /// Get record list whthin time threshold of target flow unit----Origin.
        /// </summary>
        /// <param name="recordList">Origine record list</param>
        /// <param name="filePath"></param>
        /// <param name="fromDatetime"></param>
        /// <param name="toDatetime"></param>
        /// <returns></returns>
        public List<OStaRecord> O_DegreeCalbyMinites(List<Record> recordList, string filePath, DateTime fromDatetime, DateTime toDatetime)
        {
            List<Record> list = null;
            DateTime dt_1;
            DataTable temp_tb = null;
            List<OStaRecord> olist = new List<OStaRecord>();
            for (DateTime dt = fromDatetime; dt < toDatetime; dt = dt.AddMinutes(1))
            {
                list = new List<Record>();
                //Console.WriteLine(dt.ToString());

                foreach (Record item in recordList)
                {

                    if (item.BeginTime >= dt && item.BeginTime < (dt_1 = dt.AddMinutes(1)))
                    {
                        list.Add(item);
                    }
                }

                if (list.Count > 0)
                {
                    if (isFirstWrite_O)
                    {
                        temp_tb = Generate_O_Statis_VAL_Table(list, filePath, dt);

                        isFirstWrite_O = false;
                    }
                    else
                    {
                        temp_tb = Generate_O_Statis_VAL_Table(list, filePath, dt, true);
                    }

                    OStaRecord record = null;
                    for (int i = 0; i < temp_tb.Rows.Count; i++)
                    {
                        record = new OStaRecord(Convert.ToDateTime(temp_tb.Rows[i]["o_datetime"]), Convert.ToString(temp_tb.Rows[i]["o_stop"]), Convert.ToInt32(temp_tb.Rows[i]["val"]));
                        olist.Add(record);
                    }
                }

            }
            return olist;
        }

        /// <summary>
        /// Get record list whthin time threshold of target flow unit----Destination.
        /// </summary>
        /// <param name="recordList">Destination record list</param>
        /// <param name="filePath"></param>
        /// <param name="fromDatetime"></param>
        /// <param name="toDatetime"></param>
        /// <returns></returns>
        public List<DStaRecord> D_DegreeCalbyMinites(List<Record> recordList, string filePath, DateTime fromDatetime, DateTime toDatetime)
        {
            List<Record> list = null;
            DateTime dt_1;
            DataTable temp_tb = null;
            List<DStaRecord> dlist = new List<DStaRecord>();
            for (DateTime dt = fromDatetime; dt < toDatetime; dt = dt.AddMinutes(1))
            {
                list = new List<Record>();
                //Console.WriteLine(dt.ToString());

                foreach (Record item in recordList)
                {

                    if (item.EndTime >= dt && item.EndTime < (dt_1 = dt.AddMinutes(1)))
                    {
                        list.Add(item);
                    }
                }

                if (list.Count > 0)
                {
                    if (isFirstWrite_D)
                    {
                        temp_tb = Generate_D_Statis_VAL_Table(list, filePath, dt);

                        isFirstWrite_D = false;
                    }
                    else
                    {
                        temp_tb = Generate_D_Statis_VAL_Table(list, filePath, dt, true);
                    }

                    DStaRecord record = null;
                    for (int i = 0; i < temp_tb.Rows.Count; i++)
                    {
                        record = new DStaRecord(Convert.ToDateTime(temp_tb.Rows[i]["d_datetime"]), Convert.ToString(temp_tb.Rows[i]["d_stop"]), Convert.ToInt32(temp_tb.Rows[i]["val"]));
                        dlist.Add(record);
                    }
                }
            }

            return dlist;
        }

        //Update progress  
        public void FunChangeProgress(int value)
        {
            progressBar1.Value = value;
        }

        //Update messageWindow  
        public void FunChangeMemo(string value)
        {
        }

        //Update button  
        public void FunChangebutton(int value)
        {
            if (value == progressBar1.Maximum)
            {
                btn_execute.Text = "Starting a new process";
                btn_execute.Enabled = true;
            }
            else
            {
                //相除保留两位小数 且四舍五入 Math.Round(1.00 * value / 100, 2,MidpointRounding.AwayFromZero)  
                btn_execute.Text = value.ToString();
            }
        }

        public void executeFun(object obj)
        {
            List<Cluster> finalResultList = new List<Cluster>(); //store result date

            everyRecord(recordList, topoList, finalResultList);

            //MessageBox.Show(finalResultList.Count.ToString());

            //Get evaluation result.
            getEvaluation(finalResultList, recordList);
            writeFinalResultListToFile(finalResultList);
        }

        /// <summary>
        /// Get Origine statistical data of flow rate
        /// </summary>
        /// <param name="recordList"></param>
        /// <param name="csvFileName"></param>
        /// <param name="datetime"></param>
        /// <param name="isAppend"></param>
        /// <returns></returns>
        public DataTable Generate_O_Statis_VAL_Table(List<Record> recordList, string csvFileName, DateTime datetime, bool isAppend = false)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("o_datetime", typeof(string));
            dt.Columns.Add("o_stop", typeof(string));
            dt.Columns.Add("d_stop", typeof(string));
            dt.Columns.Add("val", typeof(int));

            DataRow dr = null;
            foreach (var item in recordList)
            {
                dr = dt.NewRow();
                dr["o_datetime"] = item.BeginTime;
                dr["o_stop"] = item.Begin;
                dr["d_stop"] = item.End;
                dr["val"] = item.FlowCount;

                dt.Rows.Add(dr);
            }

            dt.Columns.Add("val1", typeof(int));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i][dt.Columns.IndexOf("val1")] = dt.Rows[i][dt.Columns.IndexOf("val")];
            }
            dt.Columns.Remove("val");
            dt.Columns["val1"].ColumnName = "val";

            DataColumn dc0 = new DataColumn("o_datetime", typeof(string));
            DataColumn dc1 = new DataColumn("o_stop", typeof(string));
            DataColumn dc2 = new DataColumn("val", typeof(int));
            DataTable O_degreeTB = new DataTable("o_degree");
            O_degreeTB.Columns.AddRange(new DataColumn[] { dc0, dc1, dc2 });

            var query1 = from t in dt.AsEnumerable()
                         group t by new { t1 = t.Field<string>("o_stop") } into m
                         select new
                         {
                             dtinfo = m,
                             o_stop = m.Key.t1,

                             val = m.Sum(n => n.Field<int>("val"))
                         };
            if (query1.ToList().Count > 0)
            {
                query1.ToList().ForEach(q =>
                {
                    O_degreeTB.Rows.Add(new object[] { datetime, q.o_stop, q.val });

                });
            }

            SaveDatatableToCSV(O_degreeTB, csvFileName, isAppend);

            //Console.WriteLine(string.Format("CSV data export ok: {0}", csvFileName));

            return O_degreeTB;
        }

        /// <summary>
        /// Get Destination statistical data of flow rate
        /// </summary>
        /// <param name="recordList"></param>
        /// <param name="csvFileName"></param>
        /// <param name="datetime"></param>
        /// <param name="isAppend"></param>
        /// <returns></returns>
        public DataTable Generate_D_Statis_VAL_Table(List<Record> recordList, string csvFileName, DateTime datetime, bool isAppend = false)
        {
            DataTable dt = new DataTable();

            dt.Columns.Add("d_datetime", typeof(string));
            dt.Columns.Add("o_stop", typeof(string));
            dt.Columns.Add("d_stop", typeof(string));
            dt.Columns.Add("val", typeof(int));

            DataRow dr = null;
            foreach (var item in recordList)
            {
                dr = dt.NewRow();
                dr["d_datetime"] = item.EndTime;
                dr["o_stop"] = item.Begin;
                dr["d_stop"] = item.End;
                dr["val"] = item.FlowCount;

                dt.Rows.Add(dr);
            }

            dt.Columns.Add("val1", typeof(int));
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                dt.Rows[i][dt.Columns.IndexOf("val1")] = dt.Rows[i][dt.Columns.IndexOf("val")];
            }
            dt.Columns.Remove("val");
            dt.Columns["val1"].ColumnName = "val";

            DataColumn dc0 = new DataColumn("d_datetime", typeof(string));
            DataColumn dc1 = new DataColumn("d_stop", typeof(string));
            DataColumn dc2 = new DataColumn("val", typeof(int));
            DataTable O_degreeTB = new DataTable("d_degree");
            O_degreeTB.Columns.AddRange(new DataColumn[] { dc0, dc1, dc2 });

            var query1 = from t in dt.AsEnumerable()
                         group t by new { t1 = t.Field<string>("d_stop") } into m
                         select new
                         {
                             dtinfo = m,
                             d_stop = m.Key.t1,

                             val = m.Sum(n => n.Field<int>("val"))
                         };
            if (query1.ToList().Count > 0)
            {
                query1.ToList().ForEach(q =>
                {
                    O_degreeTB.Rows.Add(new object[] { datetime, q.d_stop, q.val });

                });

            }

            SaveDatatableToCSV(O_degreeTB, csvFileName, isAppend);

            return O_degreeTB;
        }

        /// <summary>
        /// Convert DataTable to CSV
        /// </summary>
        /// <param name="dt">DataTable object</param>
        /// <param name="pathFile">CSV file path</param>
        /// <returns></returns>
        public static bool SaveDatatableToCSV(DataTable dt, string pathFile, bool isAppend)
        {
            string strLine = "";
            StreamWriter sw;
            try
            {
                sw = new StreamWriter(pathFile, isAppend, System.Text.Encoding.GetEncoding(-0)); //override
                //table header
                if (!isAppend)
                {
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (i > 0)
                            strLine += ",";
                        strLine += dt.Columns[i].ColumnName;
                    }
                    strLine.Remove(strLine.Length - 1);
                    sw.WriteLine(strLine);
                    strLine = "";
                }
                //table records
                for (int j = 0; j < dt.Rows.Count; j++)
                {
                    strLine = "";
                    int colCount = dt.Columns.Count;
                    for (int k = 0; k < colCount; k++)
                    {
                        if (k > 0 && k < colCount)
                            strLine += ",";
                        if (dt.Rows[j][k] == null)
                            strLine += "";
                        else
                        {
                            string cell = dt.Rows[j][k].ToString().Trim();
                            
                            strLine += cell;
                        }
                    }
                    sw.WriteLine(strLine);
                }
                sw.Close();
                string msg = "Data export successfully：" + pathFile;
                //Console.WriteLine(msg);
            }
            catch (Exception ex)
            {
                string msg = "Data export error：" + pathFile;
                //Console.WriteLine(msg);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Write result data to file.
        /// </summary>
        /// <param name="finalResultList">list contains result data</param>
        private void writeFinalResultListToFile(List<Cluster> finalResultList)
        {
            if (File.Exists(resultfile))
            {
                File.Delete(resultfile);
            }

            WriteFile("OStr,DStr,Start_time1,Start_time2,End_time1,End_time2,rightSum,OSum,DSum,AllSum,v_val,a_val,c_val");

            for (int i = 0; i < finalResultList.Count; i++)
            {
                Cluster cluster = finalResultList[i];
                if (cluster.getOCluster().Count >= 2 || cluster.getDCluster().Count >= 2)
                {
                    List<string> OList = cluster.getOCluster();
                    string OStr = "";
                    for (int j = 0; j < OList.Count; j++)
                    {
                        OStr += OList[j] + " ";
                    }

                    List<String> DList = cluster.getDCluster();
                    string DStr = "";
                    for (int j = 0; j < DList.Count; j++)
                    {
                        DStr += DList[j] + " ";
                    }

                    double v_value = 1.0 * cluster.getRightSum() / cluster.getAllSum();
                    double a_value = 1.0 * cluster.getAllSum() * cluster.getRightSum() / (cluster.getOSum() * cluster.getDSum());
                    double c_value = Math.Sqrt(v_value * a_value);
                    String str = cluster.getFirstStartTime() + "," + cluster.getLastStartTime() + "," + cluster.getFirstStopTime() + "," + cluster.getLastStopTime() + "," +
                            cluster.getRightSum() + "," + cluster.getOSum() + "," + cluster.getDSum() + "," + cluster.getAllSum() +
                            ", " + v_value + ", " + a_value + ", " + c_value;
                    str = str.Replace("Wed Apr 01 ", "");
                    str = str.Replace("CST 2015", "");
                    WriteFile(OStr + "," + DStr + "," + str);

                }
            }

            MessageBox.Show("Taks finished and result save successfully!");
        }

        /// <summary>
        /// Write record to console.
        /// </summary>
        /// <param name="str"></param>
        public void WriteFile(String str)
        {
            
            if (!Directory.Exists(Path.GetDirectoryName(resultfile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(resultfile));
            }
            StreamWriter sw = new StreamWriter(resultfile, true, System.Text.Encoding.Default);
            sw.WriteLine(str);
            sw.Close();
        }

        /// <summary>
        /// Get evaluation variables
        /// </summary>
        /// <param name="finalResultList"></param>
        /// <param name="RecordList"></param>
        public void getEvaluation(List<Cluster> finalResultList, List<Record> RecordList)
        {
            for (int i = 0; i < RecordList.Count; i++)
            {
                Record r = RecordList[i];
                for (int j = 0; j < finalResultList.Count; j++)
                {
                    Cluster c = finalResultList[j];

                    //Get value of AllSum
                    if (
                        diffDatetimeWithoutAbs(r.BeginTime, c.getFirstStartTime()) >= 0 && 
                        diffDatetimeWithoutAbs(r.BeginTime, c.getLastStartTime()) <= 0 ||
                        diffDatetimeWithoutAbs(r.EndTime, c.getFirstStopTime()) >= 0 && 
                        diffDatetimeWithoutAbs(r.EndTime, c.getLastStopTime()) <= 0
                       )
                    {
                        c.setAllSum(c.getAllSum() + 1);
                    }
                    else
                    {
                        continue;
                    }

                    if (diffDatetimeWithoutAbs(r.BeginTime, c.getFirstStartTime()) >= 0 && diffDatetimeWithoutAbs(r.BeginTime, c.getLastStartTime()) <= 0 &&
                            c.getOCluster().Contains(r.Begin))
                    {
                        c.setOSum(c.getOSum() + 1);
                    }

                    if (diffDatetimeWithoutAbs(r.EndTime, c.getFirstStopTime()) >= 0 && diffDatetimeWithoutAbs(r.EndTime, c.getLastStopTime()) <= 0 &&
                            c.getOCluster().Contains(r.Begin))
                    {
                        c.setDSum(c.getDSum() + 1);
                    }

                    if (diffDatetimeWithoutAbs(r.BeginTime, c.getFirstStartTime()) >= 0 && diffDatetimeWithoutAbs(r.BeginTime, c.getLastStartTime()) <= 0 &&
                            diffDatetimeWithoutAbs(r.EndTime, c.getFirstStopTime()) >= 0 && diffDatetimeWithoutAbs(r.EndTime, c.getLastStopTime()) <= 0 &&
                            c.getOCluster().Contains(r.Begin) && c.getOCluster().Contains(r.Begin))
                    {
                      c.setRightSum(c.getRightSum() + 1);
                    }
                }
            }
        }
     
        /// <summary>
        /// Find time neighbor record metrix.
        /// </summary>
        /// <param name="record"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public List<Record> funn(Record record, List<Record> list)
        {
            List<Record> newList = new List<Record>();

            for (int i = 0; i < list.Count; i++)
            {
                Record r = list[i];

                if (r.Equals(record))
                {
                    continue;
                }

                string s01 = record.BeginTime.ToString("yyyy/M/d 0:00:00") + "," + record.Begin;
                var record_olist = osta_RecordList[s01];
                var record_dlist = dsta_RecordList[record.EndTime.ToString("yyyy/M/d 0:00:00") + "," + record.End];

                var r_olist = osta_RecordList[r.BeginTime.ToString("yyyy/M/d 0:00:00") + "," + r.Begin];
                var r_dlist = dsta_RecordList[r.EndTime.ToString("yyyy/M/d 0:00:00") + "," + r.End];

                //var record_olist = osta_RecordList.Where(x => x.datetime.ToString("yyyy-MM-dd") == record.BeginTime.ToString("yyyy-MM-dd") && x.o_stop == record.Begin).ToList();
                //var record_dlist = dsta_RecordList.Where(x => x.datetime.ToString("yyyy-MM-dd") == record.EndTime.ToString("yyyy-MM-dd") && x.d_stop == record.End).ToList();

                //var r_olist = osta_RecordList.Where(x => x.datetime.ToString("yyyy-MM-dd") == r.BeginTime.ToString("yyyy-MM-dd") && x.o_stop == r.Begin).ToList();
                //var r_dlist = dsta_RecordList.Where(x => x.datetime.ToString("yyyy-MM-dd") == r.EndTime.ToString("yyyy-MM-dd") && x.d_stop == r.End).ToList();

                double record_OFlowCount = record_olist.val;
                double record_DFlowCount = record_dlist.val;
                double r_OFlowCount = r_olist.val;
                double r_DFlowCount = r_dlist.val;

                double d = Math.Pow((r.FlowCount + record.FlowCount), 2) / ((record_OFlowCount + r_OFlowCount) * (record_DFlowCount + r_DFlowCount));

                if (d >= flow_threshHold)
                {
                    newList.Add(r);
                }
            }

            return newList;
        }

        /// <summary>
        /// Iterator each flow unit and execute algrithm---key code.
        /// </summary>
        /// <param name="RecordList"></param>
        /// <param name="TopologyHashMap"></param>
        /// <param name="finalResultList"></param>
        public void everyRecord(List<Record> RecordList, List<ODPair> TopologyHashMap, List<Cluster> finalResultList)
        {
            int m = -1;
            List<Record> neibor1 = null;
            for (int i = 0; i < RecordList.Count; i++)
            {
                Record record = RecordList[i];
                if (record.Visited == true)
                {
                    m++;
                    //Executint delegate and update button--important 
                    this.btn_execute.Invoke(changebtn, m);
                    //Executint delegate and update progressbar--important  
                    this.progressBar1.Invoke(changeProgerss, m);
                    //Console.WriteLine(string.Format("+++++++++ total：{0}, stop current order：{1} +++++++++", RecordList.Count, i+1));
                    continue;
                }
                record.Visited = true;

                //Console.WriteLine(i + " Mark the record have iteratored：" + record);
                List<Record> neighborRecords = getNeighborRecords(record);


                neibor1 = funn(record, neighborRecords);

                if (neibor1.Count < 2)
                {//Set mimimum neighbor count
                    record.IsNoise = true;

                    RecordList.RemoveAt(i);
                    //Console.WriteLine(record + ":  noise");
                    //Console.WriteLine("+++++++++find out a noise record+++++++++\n");
                    m++;m++;
                    
                    //executing delegate and update button--import  
                    this.btn_execute.Invoke(changebtn, m);
                    //executing delegate and update button--import
                    this.progressBar1.Invoke(changeProgerss, m);
                    //Console.WriteLine(string.Format("+++++++++ toal：{0}, stop current order：{1} +++++++++", RecordList.Count, i+1));
                    continue;
                }
                

                Cluster cluster = new Cluster(); 
                cluster.addRecord(record);
                cluster.addOEle(record.Begin);
                cluster.addDEle(record.End);
                record.BelongCluster = true;
                expandCluster(record, neibor1, cluster);
                if (cluster.getOCluster().Count >1 || cluster.getDCluster().Count > 1)
                {
                    finalResultList.Add(cluster);
                }
                //MessageBox.Show("cluster: " + cluster.getRecordCluster().Count.ToString());
                foreach (var item in cluster.getRecordCluster())
                {
                    string ss = item.Begin + "," + item.End;

                    od_recordList.Remove(ss);
                }
                Console.WriteLine("od_recordList: " + od_recordList.Count);
                m++;
              
                //executing delegate and update button--import  
                this.btn_execute.Invoke(changebtn, i);
                //executing delegate and update button--import  
                this.progressBar1.Invoke(changeProgerss, i);
                //Console.WriteLine(string.Format("+++++++++ total：{0}, stop current order：{1} +++++++++", RecordList.Count, i+1));
            }
        }

        // noise record have not been marked.
        private void expandCluster(Record record, List<Record> neighborRecords, Cluster cluster)
        {
            //neighborRecords example：2003156208,2015-04-01 04:54:03,衡山路站,2015-04-01 05:14:33,人民广场站
            for (int i = 0; i < neighborRecords.Count; i++)
            {
                if (neighborRecords[i].Equals(record))
                {// skip marked record
                    continue;
                }
                Record r = neighborRecords[i];
                //Console.WriteLine(i + ":sub record：" + r + "，total：" + neighborRecords.Count);
                if (r.Visited == false)
                {
                    r.Visited = true;
                    List<Record> subNeighborRecords = getNeighborRecords(r);

                    List<Record> selList = funn(r, subNeighborRecords);
                    if (selList.Count > 0)
                    {
                        Console.Write("+++++++++++ok");
                    }
                    MyListTool.RecordListAddList(neighborRecords, selList);
  
                }
                if (r.BelongCluster == false)
                {
                    cluster.addRecord(r);
                    cluster.addOEle(r.Begin);
                    cluster.addDEle(r.End);
                    r.BelongCluster = true;
                }
            }
        }

        /// <summary>
        /// Read spatial neighbor metrix from a file
        /// </summary>
        /// <param name="TopoMetrixFileName">full file name</param>
        /// <returns>return spatial neighbor metrix list</returns>
        public List<ODPair> GetTopologyHashMap(string TopoMetrixFileName)
        {

            List<ODPair> list = new List<ODPair>();

            StreamReader sr = new StreamReader(TopoMetrixFileName);
            string str = "";
            string[] strs = null;
            while (!sr.EndOfStream)
            {
                str = sr.ReadLine();
                strs = str.Split(',');
                list.Add(new ODPair(strs[0], strs[1]));
            }

            return list;
        }

        /// <summary>
        /// Read OD flow unit data form a file
        /// </summary>
        /// <param name="ODFlowUnitFile">full file name</param>
        /// <returns>return od flow unit list</returns>
        private List<Record> GetRecordList(string ODFlowUnitFile)
        {
            List<Record> recordList = new List<Record>();

            StreamReader sr = new StreamReader(ODFlowUnitFile);
            string str = "";
            string[] strs = null;
            while (!sr.EndOfStream)
            {
                str = sr.ReadLine();
                strs = str.Split(',');

                Record r = new Record(strs[0], strs[2], Convert.ToDateTime(strs[1]), strs[4], Convert.ToDateTime(strs[3]), Convert.ToInt32(strs[5]));
                recordList.Add(r);
            }

            return recordList;
        }

        /// <summary>
        /// Obtain OD record table of O
        /// </summary>
        /// <returns>return all record</returns>
        private Dictionary<string, Record> getRecordODic()
        {
            Dictionary<string, Record> recordList = new Dictionary<string, Record>();

            string fileName = ODFlowUnitFileName;

            StreamReader sr = new StreamReader(fileName);
            string str = "";
            string[] strs = null;
            while (!sr.EndOfStream)
            {
                str = sr.ReadLine();
                strs = str.Split(',');

                Record r = new Record(strs[0], strs[2], Convert.ToDateTime(strs[1]), strs[4], Convert.ToDateTime(strs[3]), Convert.ToInt32(strs[5]));
                recordList.Add(r.Begin, r);
            }

            return recordList;
        }

        /// <summary>
        ///  Obtain OD record table of D
        /// </summary>
        /// <returns>return all record</returns>
        private Dictionary<string, Record> getRecordDDic()
        {
            Dictionary<string, Record> recordList = new Dictionary<string, Record>();

            string fileName = ODFlowUnitFileName;

            StreamReader sr = new StreamReader(fileName);
            string str = "";
            string[] strs = null;
            while (!sr.EndOfStream)
            {
                str = sr.ReadLine();
                strs = str.Split(',');

                Record r = new Record(strs[0], strs[2], Convert.ToDateTime(strs[1]), strs[4], Convert.ToDateTime(strs[3]), Convert.ToInt32(strs[5]));
                recordList.Add(r.End, r);
            }

            return recordList;
        }

        /// <summary>
        /// create <od，record> list.
        /// </summary>
        /// <param name="recordList">od records list</param>
        /// <returns>return list</returns>
        private Dictionary<string, List<Record>> getRecordHashMap(List<Record> recordList)
        {
            Dictionary<string, List<Record>> dic = new Dictionary<string, List<Record>>();
            string od_str;
            foreach (var item in recordList)
            {
                od_str = item.Begin + "," + item.End;

                if (dic.ContainsKey(od_str))
                {
                    dic[od_str].Add(item);
                }
                else
                {
                    var tempList = new List<Record>();
                    tempList.Add(item);
                    dic.Add(od_str, tempList);
                }
            }
            return dic;
        }

        /// <summary>
        /// Get flow rate value from each flow unit.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string getValueByRecord(string str)
        {
            foreach (var item in topoList)
            {
                if (item.o == str)
                {
                    return item.d;
                }
            }

            return null;
        }

        /// <summary>
        /// Determine if time is close
        /// </summary>
        /// <param name="r1">flow unit 1</param>
        /// <param name="r2">flow unit 2</param>
        /// <returns>true indicates flow unit 1 and flow unit 2 are close</returns>
        public bool isTimeNeighbor(Record r1, Record r2)
        {
            DateTime d11 = r1.BeginTime;
            DateTime d12 = r1.EndTime;
            DateTime d21 = r2.BeginTime;
            DateTime d22 = r2.EndTime;
            int beginTimeInterval = (int)diffDatetime(d11, d21);//Get time interval between origines，second as time unit.
            int endTimeInterval = (int)diffDatetime(d12, d22);//Get time interval between destinations，second as time unit.
            if (beginTimeInterval <= TimeThread && endTimeInterval <= TimeThread)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// diff between two time object.
        /// </summary>
        /// <param name="dt1"></param>
        /// <param name="dt2"></param>
        /// <returns></returns>
        public long diffDatetime(DateTime dt1, DateTime dt2)
        {
            long diff = Math.Abs((dt2 - dt1).Ticks / 10000000);

            return diff;
        }

        public long diffDatetimeWithoutAbs(DateTime dt1, DateTime dt2)
        {
            long diff = (dt1 - dt2).Ticks / 10000000;

            return diff;
        }


        /// <summary>
        /// Get neighbors of Origine and Destination respectively according to target flow unit.
        /// </summary>
        /// <param name="record">target record</param>
        /// <returns></returns>
        public List<Record> getNeighborRecords(Record record)
        {
            //record example：2302367513,2015-04-01 04:48:28,上海南站,2015-04-01 06:00:19,莲花路站
            List<Record> neighborRecords = new List<Record>();

            List<string> OList = new List<String>();
            List<string> DList = new List<String>();

            string ONeighStr = getValueByRecord(record.Begin);

            if (ONeighStr == null)
            {
                ONeighStr = getValueByRecord(record.Begin) + "city";
            }

            string[] Ostr = ONeighStr.Split(' ');

            for (int i = 0; i < Ostr.Length; i++)
            {
                MyListTool.listAddEle(OList, Ostr[i]);
            }
            //Console.WriteLine("current O city：" + record.Begin + ",   ONeighStr: " + printStringList(OList));

            String DNeighStr = getValueByRecord(record.End);

            if (DNeighStr == null)
            {

                DNeighStr = getValueByRecord(record.End) + "node";
            }

            string[] Dstr = DNeighStr.Split(' ');

            for (int i = 0; i < Dstr.Length; i++)
            {

                MyListTool.listAddEle(DList, Dstr[i]);
            }

            //Console.WriteLine("current D city：" + record.End + "，DNeighStr: " + printStringList(DList));

            foreach (var oitem in OList)
            {
                foreach (var ditem in DList)
                {
                    string strKey = oitem + "," + ditem;

                    if (od_recordList.ContainsKey(strKey))
                    {
                        foreach (var item in od_recordList[strKey])
                        {
                            if (isTimeNeighbor(record, item))
                            {
                                neighborRecords.Add(item);
                            }
                        }
                    }
                }
            }
            return neighborRecords;
        }

        /// <summary>
        /// Get list for full name and short name of cities.
        /// </summary>
        /// <returns></returns>
        public List<FullShortPair> getFullShortPairList()
        {
            StreamReader sr = new StreamReader(Application.StartupPath + @"\full_short_tb.csv");

            string str = null;
            List<FullShortPair> list = new List<FullShortPair>();
            sr.ReadLine();
            while ((str = sr.ReadLine()) != null)
            {
                var strs = str.Split(',');
                if (strs.Length > 0)
                {
                    list.Add(new FullShortPair(strs[12], strs[8], strs[7]));
                }
            }

            return list;
        }

        /// <summary>
        /// Event method while closeing Form1
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            //Force exit and destroy the process  
            System.Environment.Exit(System.Environment.ExitCode);
            this.Dispose();
            this.Close();
        }

        /// <summary>
        /// Initial method before loading Form1(a visual window)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            changeProgerss = FunChangeProgress;
            changebtn = FunChangebutton;
            changeMemo = FunChangeMemo;
        }

        private void btn_validate_Click(object sender, EventArgs e)
        {
            txt_SpatialMetrix.Text = TopoMetrixFileName;
            txt_odfile.Text = ODFlowUnitFileName;
            txt_result.Text = resultfile;
            txt_workspace.Text = WorkspacePath;

            txt_time.Text = TimeThread.ToString();
            txt_flow.Text = flow_threshHold.ToString();
        }

        private void btn_close_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }

    /// <summary>
    /// OD value class
    /// </summary>
    public class ODVAL
    {
        public string begin;        // orgin uid
        public string end;          // destination uid
        public string beginDate;    // begin date of flow
        public string endDate;      // end date of flow
        public int val;             // flow rate

        /// <summary>
        /// Construction method
        /// </summary>
        /// <param name="begin">orgine id</param>
        /// <param name="end">destination uid</param>
        /// <param name="beginDate">begin date of flow</param>
        /// <param name="endDate">end date of flow</param>
        /// <param name="val">flow rate</param>
        public ODVAL(string begin, string end, string beginDate, string endDate, int val)
        {
            this.begin = begin;
            this.end = end;
            this.beginDate = beginDate;
            this.endDate = endDate;
            this.val = val;
        }
           
    }

    /// <summary>
    /// OD pair class
    /// </summary>
    public class ODPair_2
    {
        public int o;   // Origine id
        public int d;   // Destination id

        /// <summary>
        /// Construction method
        /// </summary>
        /// <param name="o">origine id</param>
        /// <param name="d">destination id</param>
        public ODPair_2(int o, int d)
        {
            this.o = o;
            this.d = d;
        }
    }

    /// <summary>
    /// Mapping class between Short name and full name of cities
    /// </summary>
    public class FullShortPair
    {
        public string uid;          // City id
        public string shortName;    // Short name of city
        public string fullName;     // Full name of city

        /// <summary>
        /// Construction method
        /// </summary>
        /// <param name="uid">city id</param>
        /// <param name="shortName">short name of city</param>
        /// <param name="fullName">full name of city</param>
        public FullShortPair(string uid, string shortName, string fullName)
        {
            this.uid = uid;
            this.shortName = shortName;
            this.fullName = fullName;
        }
    }
}
