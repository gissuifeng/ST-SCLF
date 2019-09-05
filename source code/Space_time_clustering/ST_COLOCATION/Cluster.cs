using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Space_time_clustering.ST_COLOCATION
{
    public class Cluster
    {
        private List<String> OCluster = new List<String>();
        private List<String> DCluster = new List<String>();
        private List<Record> recordCluster = new List<Record>();
        private DateTime startTime;
        private DateTime stopTime;
        private int duringBeginTime;
        private int duringEndTime;
        private int Cid;
        private int OSum = 0;//该模式所在时间段，从源区域出发的条数
        private int DSum = 0;//该模式所在时间段，到达目的区域的所有记录条数
        private int AllSum = 0;//该模式所在时间段所有记录的条数
        private int rightSum = 0;


        public int getRightSum()
        {
            return rightSum;
        }
        public void setRightSum(int rightSum)
        {
            this.rightSum = rightSum;
        }
        public int getOSum()
        {
            return OSum;
        }
        public void setOSum(int oSum)
        {
            OSum = oSum;
        }
        public int getDSum()
        {
            return DSum;
        }
        public void setDSum(int dSum)
        {
            DSum = dSum;
        }
        public int getAllSum()
        {
            return AllSum;
        }
        public void setAllSum(int allSum)
        {
            AllSum = allSum;
        }

        public void addOEle(String ele)
        {
            MyListTool.listAddEle(OCluster, ele);
        }
        public void addDEle(String ele)
        {
            MyListTool.listAddEle(DCluster, ele);
        }

        public void addRecord(Record r)
        {
            recordCluster.Add(r);
        }
        public DateTime getFirstStartTime()
        {//获取该模式的开始时间
            DateTime firstTime = ((Record)recordCluster[0]).BeginTime;
            for (int i = 0; i < recordCluster.Count; i++)
            {
                DateTime curTime = recordCluster[i].BeginTime;
                if (DateTime.Compare(firstTime, curTime) > 0)
                {
                    firstTime = curTime;
                }
            }
            return firstTime;
        }
        public DateTime getLastStartTime()
        {//获取该模式的开始时间
            DateTime lastTime = ((Record)recordCluster[0]).BeginTime;
            for (int i = 0; i < recordCluster.Count; i++)
            {
                DateTime curTime = recordCluster[i].BeginTime;
                if (DateTime.Compare(lastTime, curTime) < 0)
                {
                    lastTime = curTime;
                }
            }
            return lastTime;
        }
        public DateTime getFirstStopTime()
        {//获取该模式到达终点最早时间
            DateTime firstTime = ((Record)recordCluster[0]).EndTime;
            for (int i = 0; i < recordCluster.Count; i++)
            {
                DateTime curTime = recordCluster[i].EndTime;
                if (DateTime.Compare(firstTime, curTime) > 0)
                {
                    firstTime = curTime;
                }
            }
            return firstTime;
        }
        public DateTime getLastStopTime()
        {//获取该模式到达终点最晚时间
            DateTime lastTime = ((Record)recordCluster[0]).EndTime;
            for (int i = 0; i < recordCluster.Count; i++)
            {
                DateTime curTime = recordCluster[i].EndTime;
                if (DateTime.Compare(lastTime, curTime) < 0)
                {
                    lastTime = curTime;
                }
            }
            return lastTime;
        }
        private static long diffDatetime(DateTime dt1, DateTime dt2)
        {
            long diff = Math.Abs((dt2 - dt1).Ticks / 10000000);

            return diff;
        }
        public int getDuringStartTime()
        {
            return (int)diffDatetime(getFirstStartTime(), getLastStartTime());
        }
        public List<string> getOCluster()
        {
            return OCluster;
        }
        public void setOCluster(List<string> oCluster)
        {
            OCluster = oCluster;
        }
        public List<string> getDCluster()
        {
            return DCluster;
        }
        public void setDCluster(List<string> dCluster)
        {
            DCluster = dCluster;
        }
        public List<Record> getRecordCluster()
        {
            return recordCluster;
        }
        public void setRecordCluster(List<Record> recordCluster)
        {
            this.recordCluster = recordCluster;
        }
        public int getCid()
        {
            return Cid;
        }
        public void setCid(int cid)
        {
            Cid = cid;
        }
    }
}
