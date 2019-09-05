using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Space_time_clustering.ST_COLOCATION
{
    /// <summary>
    /// Flow unit record object 
    /// </summary>
    public class Record
    {
        private int flowCount = 100;
        public int FlowCount
        {
            get { return flowCount; }
            set { flowCount = value; }
        }
        public int beginCount = 200;
        public int endCount = 300;

        private string userId;
        public string UserId
        {
            get { return userId; }
            set { userId = value; }
        }
        private bool visited = false;//标记该记录是否已读
        public bool Visited
        {
            get { return visited; }
            set { visited = value; }
        }
        private string begin;//表示该记录的起点
        public string Begin
        {
            get { return begin; }
            set { begin = value; }
        }
        private string end;//表示该记录的终点
        public string End
        {
            get { return end; }
            set { end = value; }
        }
        private bool isNoise = false;//表示该记录是否为噪声
        public bool IsNoise
        {
            get { return isNoise; }
            set { isNoise = value; }
        }
        private DateTime beginTime;
        public System.DateTime BeginTime
        {
            get { return beginTime; }
            set { beginTime = value; }
        }
        private DateTime endTime;
        public System.DateTime EndTime
        {
            get { return endTime; }
            set { endTime = value; }
        }
        private bool belongCluster = false;
        public bool BelongCluster
        {
            get { return belongCluster; }
            set { belongCluster = value; }
        }

        public Record(String userId, String begin, DateTime beginTime, string end, DateTime endTime, int flowCount)
        {
            this.userId = userId;
            this.begin = begin;
            this.end = end;
            this.beginTime = beginTime;
            this.endTime = endTime;
            this.flowCount = flowCount;
        }
    }
}
