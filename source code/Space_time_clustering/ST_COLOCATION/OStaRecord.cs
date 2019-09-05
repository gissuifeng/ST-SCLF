using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Space_time_clustering.ST_COLOCATION
{
    public class OStaRecord
    {
        public DateTime datetime;

        public string o_stop;

        public int val;
        
        public OStaRecord(DateTime datetime, string o_stop, int val)
        {
            this.datetime = datetime;
            this.o_stop = o_stop;
            this.val = val;
        }
    }

    public class DStaRecord
    {
        public DateTime datetime;
        public string d_stop;
        public int val;

        public DStaRecord(DateTime datetime, string d_stop, int val)
        {
            this.datetime = datetime;
            this.d_stop = d_stop;
            this.val = val;
        }
    }
}
