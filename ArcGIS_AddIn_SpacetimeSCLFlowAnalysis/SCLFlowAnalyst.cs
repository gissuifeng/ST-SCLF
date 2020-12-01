using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArcObjectToolbox;

namespace SpacetimeSCLFlowAnalysis
{
    class SCLFlowAnalyst
    {
        private ITrackCancel trackCancel;
        public ITrackCancel TrackCancel
        {
            get { return trackCancel; }
            set { this.trackCancel = value; }
        }

        private IStepProgressor stepProgressor;
        public IStepProgressor StepProgressor
        {
            get { return stepProgressor; }
            set { this.stepProgressor = value; }
        }

        private Dictionary<string, List<DataRow>> odFlowDataRowsDict;

        private Dictionary<string, List<DataRow>> timeFlowDataRowsDict;

        public SCLFlowAnalyst()
        {

        }

        // 进度条运行
        private bool ContinueStepProgressor(object[] releaseCOMs)
        {
            if (this.stepProgressor != null)
            {
                this.stepProgressor.Step(); // 设置进度
                if (!this.trackCancel.Continue())  // 判断是否中途取消操作
                {
                    if (releaseCOMs != null)
                    {
                        for (int i = 0; i < releaseCOMs.Length; i++)
                            Marshal.ReleaseComObject(releaseCOMs[i]);

                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                    }

                    return false;
                }

                return true;
            }

            return true;
        }

        // 添加进度设置
        private void AddStepProgressor(int maxRange, string message)
        {
            if (this.stepProgressor != null)
            {
                this.trackCancel.Reset();
                this.stepProgressor.MinRange = 0;
                this.stepProgressor.Position = 1;
                this.stepProgressor.StepValue = 1;
                this.stepProgressor.MaxRange = maxRange;
                this.stepProgressor.Message = message;
            }
        }

        // 创建弧线
        private IPolyline CreateCirculeArc(IPoint fromPoint, IPoint toPoint, double chordCoef)
        {
            // 如果起点和终点相等，返回一个直线
            if (fromPoint.X == toPoint.X && fromPoint.Y == toPoint.Y)
            {
                object missing = Type.Missing;
                IPolyline polyline = new PolylineClass();
                var pointCol = (IPointCollection)polyline;
                pointCol.AddPoint(fromPoint, missing, missing);
                pointCol.AddPoint(toPoint, missing, missing);
                return polyline;
            }

            // 根据起止点建立直线A方程，
            // 利用A线上的中点建立与A线垂直的线方程，
            // 利用与中点的距离确定直线A两侧对称点
            double midX = (fromPoint.X + toPoint.X) / 2;
            double midY = (fromPoint.Y + toPoint.Y) / 2;
            double k = (fromPoint.Y - toPoint.Y) / (fromPoint.X - toPoint.X); // 直线斜率
            double h = Math.Pow(Math.Pow((fromPoint.Y - toPoint.Y), 2.0) + Math.Pow((fromPoint.X - toPoint.X), 2.0), 0.5); // 直线长
            double h2 = h * chordCoef; // 获得0.1, 0.2, 0.3...倍的直线长
            //double sign = Math.Pow(-1, offsetCount - 1); // 左右对称点符号
            double sign = 1; // 表示正负号
            if (fromPoint.X > toPoint.X)
                sign = -1;
            else if (fromPoint.X == toPoint.X && fromPoint.Y > toPoint.Y)
                sign = -1;
            double x = midX + sign * (-1) * chordCoef * h2 * k / Math.Pow(1 + k * k, 0.5); // 求解后的x
            double y = midY + sign * chordCoef * h2 / Math.Pow(1 + k * k, 0.5); // 求解后的y
            IPoint arcPoint = new PointClass();
            arcPoint.PutCoords(x, y);
            IConstructCircularArc2 circularArc = new CircularArcClass();
            circularArc.ConstructThreePoints(fromPoint, arcPoint, toPoint, true);
            IPolyline circularPolyline = new PolylineClass();
            var segementColl = (ISegmentCollection)circularPolyline;
            var segment = (ISegment)circularArc;
            segementColl.AddSegment(segment);

            return circularPolyline;
        }

        private Dictionary<string, IPolygon> GetUIDPolygonDict(IFeatureClass featureClass, string uidFieldName)
        {
            if (featureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                return null;

            Dictionary<string, IPolygon> outputDict = new Dictionary<string, IPolygon>();
 
            int uid_idx = featureClass.FindField(uidFieldName);

            IFeatureCursor featureCursor = featureClass.Search(null, true);
            IFeature feature = null;

            AddStepProgressor(featureClass.FeatureCount(null), "Reading polygon in feature class . . .");
            while ((feature = featureCursor.NextFeature()) != null)
            {
                if (!ContinueStepProgressor(new object[] { featureCursor }))
                    return null;

                string keyStr = feature.Value[uid_idx].ToString();

                outputDict[keyStr] = (IPolygon)feature.ShapeCopy;
            }

            Marshal.ReleaseComObject(featureCursor);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return outputDict;
        }

        /// <summary>
        /// 创建同位流模式分析结果要素
        /// </summary>
        /// <param name="originFeatureClass">输出的矢量要素</param>
        /// <param name="originUIDField">区分要素的唯一字段名</param>
        /// <param name="patternTable">同位流模式表</param>
        /// <param name="out_path">输出要素的路径</param>
        /// <param name="out_name">输出要素的名称</param>
        /// <param name="outObjectClass">输出区域要素类</param>
        /// <param name="outFlowClass">输出流线要素类</param>
        public void CreatePatternFeatures(IFeatureClass originFeatureClass,
                                          string originUIDField,
                                          IFeatureClass destFeatureClass,
                                          string destUIDField,
                                          DataTable patternTable,
                                          string out_path,
                                          string out_name,
                                          ref IFeatureClass outObjectClass,
                                          ref IFeatureClass outFlowClass,
                                          ref string message)
        {
            string parent_path = System.IO.Path.GetDirectoryName(out_path);
            string featuredataset_name = "";
            string extension = System.IO.Path.GetExtension(parent_path);
            if (extension == ".gdb" || extension == ".mdb")
            {
                featuredataset_name = System.IO.Path.GetFileName(out_path);
                out_path = parent_path;
            }

            var originSpatialRef = ((IGeoDataset)originFeatureClass).SpatialReference;
            var destSpatialRef = ((IGeoDataset)destFeatureClass).SpatialReference;
            IClone comparison = originSpatialRef as IClone;
            if (!comparison.IsEqual((IClone)destSpatialRef))
            {
                message = "Different Spatial Reference in Origin and Destination FeatureClass.";
                return;
            }

            // 获取 UID字段-要素 字典
            var originPolygonDict = GetUIDPolygonDict(originFeatureClass, originUIDField);
            Dictionary<string, IPolygon> destPolygonDict = null;
            if (originFeatureClass == destFeatureClass)
                destPolygonDict = originPolygonDict;
            else
                destPolygonDict = GetUIDPolygonDict(destFeatureClass, destUIDField);


            // 字段的最大长度
            int d_length = patternTable.AsEnumerable().Max(r => ((string)r["Dests"]).Length);
            int o_length = patternTable.AsEnumerable().Max(r => ((string)r["Origins"]).Length);
            int max_length = d_length;
            if (o_length > d_length) max_length = o_length;

            var dataMangementTools = new DataManagementTools();

            // 合并后的区域要素 
            outObjectClass = dataMangementTools.CreateFeatureClass(
                out_path, out_name + "_OBJ", featuredataset_name, originFeatureClass.ShapeType, originSpatialRef);
            // 同位流要素
            outFlowClass = dataMangementTools.CreateFeatureClass(
                out_path, out_name + "_FLOW", featuredataset_name, esriGeometryType.esriGeometryPolyline, originSpatialRef);

            // 复制合并前要素的字段
            var pID = dataMangementTools.AddField((ITable)outObjectClass, "PID", esriFieldType.esriFieldTypeSmallInteger, true);
            var pType = dataMangementTools.AddField((ITable)outObjectClass, "PTYPE", esriFieldType.esriFieldTypeString, true);
            var pGUID = dataMangementTools.AddField((ITable)outObjectClass, "PGUID", esriFieldType.esriFieldTypeString, true, max_length);
            var pGCount = dataMangementTools.AddField((ITable)outObjectClass, "PGCOUNT", esriFieldType.esriFieldTypeString, true, max_length);
            int pID_idx = outObjectClass.FindField(pID);
            int pType_idx = outObjectClass.FindField(pType);
            int pZUID_idx = outObjectClass.FindField(pGUID);
            int pZCount_idx = outObjectClass.FindField(pGCount);

            // 复制同位流字段
            var flowFieldDict = dataMangementTools.CopyFields(patternTable, (ITable)outFlowClass, max_length);

            IFeatureBuffer outObjectBuffer = null;
            IFeatureCursor outObjectCursor = outObjectClass.Insert(true);

            IFeatureBuffer outFlowBuffer = null;
            IFeatureCursor outFlowCursor = outFlowClass.Insert(true);

            AddStepProgressor(patternTable.Rows.Count, "Creating spatiotemporal self-co-location flow features . . . ");
            for (int i = 0; i < patternTable.Rows.Count; i++)
            {
                if (!ContinueStepProgressor(new object[] { outObjectCursor, outFlowCursor }))
                    return;

                var dataRow = patternTable.Rows[i];
                var rowPID = (int)dataRow["PID"];
               
                string[] originStrArr = ((string)dataRow["Origins"]).Split(';');
                IPolygon originPolygon = DissolvePolygon(originSpatialRef, originPolygonDict, originStrArr);
                outObjectBuffer = outObjectClass.CreateFeatureBuffer();
                outObjectBuffer.Shape = originPolygon;
                outObjectBuffer.Value[pID_idx] = rowPID;
                outObjectBuffer.Value[pType_idx] = "Origin";
                outObjectBuffer.Value[pZUID_idx] = string.Join(";", originStrArr);
                outObjectBuffer.Value[pZCount_idx] = originStrArr.Length;
                outObjectCursor.InsertFeature(outObjectBuffer);

                IPoint originPoint = ((IArea)originPolygon).Centroid;

                string[] destStrArr = ((string)dataRow["Dests"]).Split(';');
                IPolygon destPolygon = DissolvePolygon(destSpatialRef, destPolygonDict, destStrArr);
                outObjectBuffer = outObjectClass.CreateFeatureBuffer();
                outObjectBuffer.Shape = destPolygon;
                outObjectBuffer.Value[pID_idx] = rowPID;
                outObjectBuffer.Value[pType_idx] = "Destination";
                outObjectBuffer.Value[pZUID_idx] = string.Join(";", destStrArr);
                outObjectBuffer.Value[pZCount_idx] = destStrArr.Length;
                outObjectCursor.InsertFeature(outObjectBuffer);

                IPoint destPoint = ((IArea)destPolygon).Centroid;
                
                // 创建同位流要素
                outFlowBuffer = outFlowClass.CreateFeatureBuffer();
                outFlowBuffer.Shape = CreateCirculeArc(originPoint, destPoint, 0.3);
               
                foreach (var item in flowFieldDict)
                {
                    outFlowBuffer.Value[item.Value] = dataRow[item.Key];
                }

                outFlowCursor.InsertFeature(outFlowBuffer);

            } // for (int i = 0; i < patternTable.Rows.Count; i++)
            outObjectCursor.Flush();
            outFlowCursor.Flush();

            Marshal.ReleaseComObject(outObjectCursor);
            Marshal.ReleaseComObject(outFlowCursor);
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private IPolygon DissolvePolygon(ISpatialReference spatialRef, 
                                         Dictionary<string, IPolygon> originPolygonDict, 
                                         string[] originStrArr)
        {
            IGeometry geometryBag = new GeometryBagClass();
            var geoCollection = (IGeometryCollection)geometryBag;

            ((IGeometry)geoCollection).SpatialReference = spatialRef;

            for (int i = 0; i < originStrArr.Length; i++)
            {
                var originStr = originStrArr[i];
                object missing = Type.Missing;
                //var polygon = (IPolygon)((IClone)originPolygonDict[originStr]).Clone();

                geoCollection.AddGeometry(originPolygonDict[originStr], ref missing, ref missing);
            }

            // 找到的要素合并为一个要素
            IPolygon outPolygon = new PolygonClass();
            ITopologicalOperator topologicalOperator = (ITopologicalOperator)outPolygon;
            topologicalOperator.ConstructUnion((IEnumGeometry)geometryBag);

            return outPolygon;
        }

        /// <summary>
        /// 同位流模式分析结果评估
        /// </summary>
        /// <param name="clusterList">同位流模式表</param>
        /// <returns></returns>
        public DataTable PatternEvaluation(List<List<DataRow>> clusterList, bool hasTime)
        {
            DataTable patternTable = new DataTable();
            patternTable.Columns.Add("PID", typeof(int));
            patternTable.Columns.Add("Origins", typeof(string));
            patternTable.Columns.Add("Dests", typeof(string));
            patternTable.Columns.Add("OriginCount", typeof(int));
            patternTable.Columns.Add("DestCount", typeof(int));
            if (hasTime)
            {
                patternTable.Columns.Add("OFirstTime", typeof(DateTime));
                patternTable.Columns.Add("OLastTime", typeof(DateTime));
                patternTable.Columns.Add("DFirstTime", typeof(DateTime));
                patternTable.Columns.Add("DLastTime", typeof(DateTime));
            }
            patternTable.Columns.Add("FlowCount", typeof(int));
            patternTable.Columns.Add("S_Value", typeof(double));
            patternTable.Columns.Add("M_Value", typeof(double));
            patternTable.Columns.Add("V_Value", typeof(double));
            patternTable.Columns.Add("A_Value", typeof(double));
            patternTable.Columns.Add("C_Value", typeof(double));

            AddStepProgressor(clusterList.Count, "Evaluating spatiotemporal self-co-location flow patterns . . .");
            for (int i = 0; i < clusterList.Count; i++)
            {
                if (!ContinueStepProgressor(null))
                    return null;

                List<DataRow> cluster = clusterList[i];

                double OSi_DSi = cluster.Sum(x => x.Field<double>("Value"));
                int flowCount = cluster.Count;

                // 计算流单元平均流量 m-value
                double m_value = OSi_DSi / flowCount;

                var cOriginFirstTime = cluster.Min(r => r.Field<DateTime>("OriginTime"));
                var cOriginLastTime = cluster.Max(r => r.Field<DateTime>("OriginTime"));
                var cDestFirstTime = cluster.Min(r => r.Field<DateTime>("DestTime"));
                var cDestLastTime = cluster.Max(r => r.Field<DateTime>("DestTime"));

                // 在该同位流模式时间段内的所有流
                var enumList = this.timeFlowDataRowsDict.Where(d =>
                {
                    var timeArr = d.Key.Split(',');
                    if (Convert.ToDateTime(timeArr[0]) <= cOriginLastTime &&
                        Convert.ToDateTime(timeArr[0]) >= cOriginFirstTime &&
                        Convert.ToDateTime(timeArr[1]) <= cDestLastTime &&
                        Convert.ToDateTime(timeArr[1]) >= cDestFirstTime)
                        return true;
                    else
                        return false;
                }).Select(l => l.Value);

                double O_D = enumList.Sum(l => l.Sum(r => r.Field<double>("Value")));

                // 计算覆盖率 v_value
                double v_value = OSi_DSi / O_D;

                var cOriginList = cluster.Select(t => t.Field<string>("Origin")).Distinct().ToList();

                var OSi_DSx = enumList.Sum(l => l.Where(r => cOriginList.Contains(r.Field<string>("Origin"))).Sum(x => x.Field<double>("Value")));

                var cDestList = cluster.Select(t => t.Field<string>("Dest")).Distinct().ToList();

                var OSx_DSi = enumList.Sum(l => l.Where(r => cDestList.Contains(r.Field<string>("Dest"))).Sum(x => x.Field<double>("Value")));

                // 计算精度 a-value，值越大关联性越强，反之则弱
                double a_value = O_D * OSi_DSi / (OSi_DSx * OSx_DSi);

                double c_value = Math.Sqrt(a_value * v_value);

                DataRow newRow = patternTable.NewRow();
                newRow["PID"] = i;
                newRow["Origins"] = string.Join(";", cOriginList);
                newRow["Dests"] = string.Join(";", cDestList);
                newRow["OriginCount"] = cOriginList.Count;
                newRow["DestCount"] = cDestList.Count;
                if (hasTime)
                {
                    newRow["OFirstTime"] = cOriginFirstTime;
                    newRow["OLastTime"] = cOriginLastTime;
                    newRow["DFirstTime"] = cDestFirstTime;
                    newRow["DLastTime"] = cDestLastTime;
                }
                newRow["FlowCount"] = flowCount;
                newRow["S_Value"] = OSi_DSi;
                newRow["M_Value"] = m_value;
                newRow["V_Value"] = v_value;
                newRow["A_Value"] = a_value;
                newRow["C_Value"] = c_value;
                patternTable.Rows.Add(newRow);
            }

            return patternTable;
        }

        // 扩展同位流
        private void ExpandCluster(
            List<DataRow> flowDataRecords,
            List<DataRow> clusterRows,
            double threshold,
            ref Dictionary<string, List<string>> originNearDict,
            ref Dictionary<string, List<string>> destNearDict)
        {
            //var fdr = flowDataRecords.AsEnumerable().ToList(); // 方便调试

            for (int i = 0; i < flowDataRecords.Count; i++)
            {
                DataRow flow = flowDataRecords[i];

                var nearFlowDataRecords = GetNearFlowDataRows(flow, ref originNearDict, ref destNearDict);

                // 没有找到时空邻近的流
                if (nearFlowDataRecords.Count == 0) continue;

                var sclFlowDataRows = GetSCLFlowDataRows(flow, nearFlowDataRecords, threshold);

                // 没有时空同位流
                if (sclFlowDataRows.Count == 0) continue;

                int initCount = clusterRows.Count;

                for (int j = 0; j < sclFlowDataRows.Count; j++)
                {
                    var sclFlow = sclFlowDataRows[j];
                    if (clusterRows.Any(r => r == sclFlow))
                        continue; // 已经在Cluster中，跳过

                    sclFlow["IsCluster"] = true;
                    clusterRows.Add(sclFlow);

                }

                // 没有添加新的时空同位流，则跳到下一个索引
                if (initCount == clusterRows.Count) continue;

                ExpandCluster(sclFlowDataRows, clusterRows, threshold, ref originNearDict, ref destNearDict);
            }

        }

        // 获得同位流
        private List<DataRow> GetSCLFlowDataRows(
            DataRow flow,
            List<DataRow> nearFlowDataRows,
            double threshold)
        {
            var fid = (int)flow["FID"];
            var origin = (string)flow["Origin"];
            var dest = (string)flow["Dest"];
            var value = (double)flow["Value"];

            var originSumValue = nearFlowDataRows.Where(r => r.Field<string>("Origin") == origin)
                                                    .Sum(r => r.Field<double>("Value"));
            var destSumValue = nearFlowDataRows.Where(r => r.Field<string>("Dest") == dest)
                                                  .Sum(r => r.Field<double>("Value"));

            Dictionary<string, double> nearOriginSumValueDict = new Dictionary<string, double>();

            nearFlowDataRows.AsEnumerable()
                               .GroupBy(r => new { origin = r.Field<string>("Origin") })
                               .Select(x => new
                               {
                                   origin = x.Key.origin,
                                   value = x.Sum(z => z.Field<double>("Value"))
                               }).ToList().ForEach(q => nearOriginSumValueDict[q.origin] = q.value);

            Dictionary<string, double> nearDestSumValueDict = new Dictionary<string, double>();

            nearFlowDataRows.AsEnumerable()
                               .GroupBy(r => new { dest = r.Field<string>("Dest") })
                               .Select(x => new
                               {
                                   dest = x.Key.dest,
                                   value = x.Sum(z => z.Field<double>("Value"))
                               }).ToList().ForEach(q => nearDestSumValueDict[q.dest] = q.value);

            //var destQuery = from rows in nearFlowDataRecords.AsEnumerable()
            //                group rows by new { g = rows.Field<string>("Dest") } into m
            //                select new
            //                {
            //                    dest = m.Key.g,
            //                    value = m.Sum(n => n.Field<double>("Value"))
            //                };
            //destQuery.ToList().ForEach(q => nearDestSumValueDict[q.dest] = q.value);

            // 时空同位流
            List<DataRow> sclFLowDataRecords = new List<DataRow>();

            foreach (DataRow nearFlow in nearFlowDataRows)
            {
                var nearFid = (int)nearFlow["FID"];
                var nearOrigin = (string)nearFlow["Origin"];
                var nearDest = (string)nearFlow["Dest"];
                var nearValue = (double)nearFlow["Value"];
                var isCluster = (bool)nearFlow["IsCluster"];

                if (nearFid == fid || isCluster == true) continue;

                double nearOriginSumValue = nearOriginSumValueDict[nearOrigin];
                double nearDestSumValue = nearDestSumValueDict[nearDest];

                double P = Math.Pow((value + nearValue), 2) /
                    ((originSumValue + nearOriginSumValue) * (destSumValue + nearDestSumValue));

                if (P >= threshold)
                    sclFLowDataRecords.Add(nearFlow);
            }

            return sclFLowDataRecords;
        }

        // 获得时空邻近流
        private List<DataRow> GetNearFlowDataRows(
            DataRow flow,
            ref Dictionary<string, List<string>> originNearDict,
            ref Dictionary<string, List<string>> destNearDict)
        {
            var origin = (string)flow["Origin"];
            var dest = (string)flow["Dest"];
            var originMaxTime = Convert.ToDateTime(flow["OriginMaxTime"]);
            var originMinTime = Convert.ToDateTime(flow["OriginMinTime"]);
            var destMaxTime = Convert.ToDateTime(flow["DestMaxTime"]);
            var destMinTime = Convert.ToDateTime(flow["DestMinTime"]);

            List<DataRow> nearFlowDataRecords = new List<DataRow>();

            if (!originNearDict.Keys.Contains(origin) ||
                !destNearDict.Keys.Contains(dest))
                return nearFlowDataRecords;

            var originNearList = originNearDict[origin];
            var destNearList = destNearDict[dest];
            for (int j = 0; j < originNearList.Count; j++)
            {
                var originNear = originNearList[j];

                for (int k = 0; k < destNearList.Count; k++)
                {
                    var destNear = destNearList[k];

                    var keyStr = originNear + "," + destNear;

                    if (!this.odFlowDataRowsDict.ContainsKey(keyStr)) continue;

                    this.odFlowDataRowsDict[keyStr].Where(
                        row => row.Field<DateTime>("OriginTime") >= originMinTime &&
                               row.Field<DateTime>("OriginTime") <= originMaxTime &&
                               row.Field<DateTime>("DestTime") >= destMinTime &&
                               row.Field<DateTime>("DestTime") <= destMaxTime)
                               .ToList().ForEach(p => nearFlowDataRecords.Add(p));
                }
            }

            return nearFlowDataRecords;
        }

        /// <summary>
        /// 同位流模式分析
        /// </summary>
        /// <param name="threshold">模式分析阈值</param>
        public List<List<DataRow>> PatternAnalysis(
            DataTable flowDataTable,
            double threshold,
            ref Dictionary<string, List<string>> originNearDict,
            ref Dictionary<string, List<string>> destNearDict) // 时间单位
        {
            List<List<DataRow>> clusterList = new List<List<DataRow>>();

            AddStepProgressor(flowDataTable.Rows.Count, "Analysing spatiotemporal self-co-location flow patterns . . . ");
            for (int i = 0; i < flowDataTable.Rows.Count; i++)
            {
                if (!ContinueStepProgressor(null)) return null;

                DataRow flow = flowDataTable.Rows[i];

                var isCluster = (bool)flow["IsCluster"];
                if (isCluster) continue;

                var nearFlowDataRecords = GetNearFlowDataRows(flow, ref originNearDict, ref destNearDict);

                // 没有找到时空邻近的流
                if (nearFlowDataRecords.Count == 0) continue;

                var sclFlowDataRecords = GetSCLFlowDataRows(flow, nearFlowDataRecords, threshold);

                // 没有找到时空同位流
                if (sclFlowDataRecords.Count == 0) continue;

                // 记录同位模式流群
                List<DataRow> clusterRows = new List<DataRow>();

                flow["IsCluster"] = true;
                clusterRows.Add(flow);

                for (int j = 0; j < sclFlowDataRecords.Count; j++)
                {
                    var sclFlow = sclFlowDataRecords[j];
                    sclFlow["IsCluster"] = true;
                    clusterRows.Add(sclFlow);
                }

                ExpandCluster(sclFlowDataRecords, clusterRows, threshold, ref originNearDict, ref destNearDict);

                clusterList.Add(clusterRows);
            }

            return clusterList;
        }

        // 获得时间段
        private void GetDateTimePeriods(
            DateTime currentDateTime,
            int timeInterval,
            string timeUnit,
            out DateTime maxTime,
            out DateTime minTime)
        {
            maxTime = currentDateTime;
            minTime = currentDateTime;

            if (timeUnit == "seconds")
            {
                minTime = currentDateTime.AddSeconds(-1 * timeInterval);
                maxTime = currentDateTime.AddSeconds(1 * timeInterval);
            }
            else if (timeUnit == "minutes")
            {
                minTime = currentDateTime.AddMinutes(-1 * timeInterval);
                maxTime = currentDateTime.AddMinutes(1 * timeInterval);
            }
            else if (timeUnit == "hours")
            {
                minTime = currentDateTime.AddHours(-1 * timeInterval);
                maxTime = currentDateTime.AddHours(1 * timeInterval);
            }
            else if (timeUnit == "days")
            {
                minTime = currentDateTime.AddDays(-1 * timeInterval);
                maxTime = currentDateTime.AddDays(1 * timeInterval);
            }
            else if (timeUnit == "months")
            {
                minTime = currentDateTime.AddMonths(-1 * timeInterval);
                maxTime = currentDateTime.AddMonths(1 * timeInterval);
            }
            else if (timeUnit == "years")
            {
                minTime = currentDateTime.AddYears(-1 * timeInterval);
                maxTime = currentDateTime.AddYears(1 * timeInterval);
            }
        }

        // ArcGIS流数据表转 .NET DataTable数据表
        public DataTable ReadFlowTable(ITable flowTable,
                                        string originField,
                                        string originTimeField,
                                        string destField,
                                        string destTimeField,
                                        string attrValueField,
                                        int timeDistance,
                                        string timeUnit)
        {
            int originFiled_idx = flowTable.FindField(originField);
            int destField_idx = flowTable.FindField(destField);
            int originTimeField_idx = flowTable.FindField(originTimeField);
            int destTimeField_idx = flowTable.FindField(destTimeField);
            int attrValue_idx = flowTable.FindField(attrValueField);

            DataTable flowDataTable = new DataTable();
            flowDataTable.TableName = ((IDataset)flowTable).Name;
            flowDataTable.Columns.Add("FID", typeof(int));
            flowDataTable.Columns.Add("Origin", typeof(string));
            flowDataTable.Columns.Add("OriginTime", typeof(DateTime));
            flowDataTable.Columns.Add("OriginMaxTime", typeof(DateTime));
            flowDataTable.Columns.Add("OriginMinTime", typeof(DateTime));
            flowDataTable.Columns.Add("Dest", typeof(string));
            flowDataTable.Columns.Add("DestTime", typeof(DateTime));
            flowDataTable.Columns.Add("DestMaxTime", typeof(DateTime));
            flowDataTable.Columns.Add("DestMinTime", typeof(DateTime));
            flowDataTable.Columns.Add("Value", typeof(double));
            flowDataTable.Columns.Add("IsCluster", typeof(bool));

            ICursor cursor = flowTable.Search(null, true);
            IRow row = null;
            int fid = 0;
            AddStepProgressor(flowTable.RowCount(null), "Reading flow data from Table . . .");
            while ((row = cursor.NextRow()) != null)
            {
                ContinueStepProgressor(new object[] { cursor });

                var origin = row.Value[originFiled_idx].ToString();
                var dest = row.Value[destField_idx].ToString();
                var value = Convert.ToDouble(row.Value[attrValue_idx]);
                var originTime = new DateTime();
                var originMaxTime = new DateTime();
                var originMinTime = new DateTime();
                var destTime = new DateTime();
                var destMaxTime = new DateTime();
                var destMinTime = new DateTime();

                if (originTimeField_idx != -1 && destTimeField_idx != -1)
                {
                    originTime = Convert.ToDateTime(row.Value[originTimeField_idx].ToString());
                    destTime = Convert.ToDateTime(row.Value[destTimeField_idx].ToString());

                    DateTime maxTime;
                    DateTime minTime;
                    GetDateTimePeriods(originTime, timeDistance, timeUnit, out maxTime, out minTime);
                    originMaxTime = maxTime;
                    originMinTime = minTime;
                    GetDateTimePeriods(destTime, timeDistance, timeUnit, out maxTime, out minTime);
                    destMaxTime = maxTime;
                    destMinTime = minTime;
                }

                var flowRow = flowDataTable.NewRow();
                flowRow["FID"] = fid++;
                flowRow["Origin"] = origin;
                flowRow["Dest"] = dest;
                flowRow["Value"] = value;
                flowRow["OriginTime"] = originTime;
                flowRow["OriginMaxTime"] = originMaxTime;
                flowRow["OriginMinTime"] = originMinTime;
                flowRow["DestTime"] = destTime;
                flowRow["DestMaxTime"] = destMaxTime;
                flowRow["DestMinTime"] = destMinTime;
                flowRow["IsCluster"] = false;

                flowDataTable.Rows.Add(flowRow);
            }

            Marshal.ReleaseComObject(cursor);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // 建立 OD与流数据表的字典
            this.odFlowDataRowsDict = new Dictionary<string, List<DataRow>>();
            flowDataTable.AsEnumerable().GroupBy(r => r.Field<string>("Origin") + "," + r.Field<string>("Dest"))
                                        .ToLookup(g => this.odFlowDataRowsDict[g.Key] = g.ToList());

            // 建立 时间与流数据表的字典
            this.timeFlowDataRowsDict = new Dictionary<string, List<DataRow>>();
            flowDataTable.AsEnumerable().GroupBy(r => r["OriginTime"].ToString() + "," + r["DestTime"].ToString())
                                        .ToLookup(g => this.timeFlowDataRowsDict[g.Key] = g.ToList());

            return flowDataTable;
        }

        #region << 淘汰的方法 >>
        // DataTable转CSV文件
        public static bool SaveDatatableToCSV(DataTable dt, string pathFile)
        {
            string strLine = "";
            StreamWriter sw;
            try
            {
                sw = new StreamWriter(pathFile, false, Encoding.GetEncoding(-0));

                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    if (i > 0)
                        strLine += ",";
                    strLine += dt.Columns[i].ColumnName;
                }
                sw.WriteLine(strLine);

                for (int j = 0; j < dt.Rows.Count; j++) // 写入DataTable记录
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
        #endregion
    }
}
