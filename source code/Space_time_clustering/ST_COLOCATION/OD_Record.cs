using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Space_time_clustering.ST_COLOCATION
{
    public class OD_Record
    {
        public string od_str;
        public Record record;

        public OD_Record(string od_str, Record record)
        {
            this.od_str = od_str;
            this.record = record;
        }
    }
}
