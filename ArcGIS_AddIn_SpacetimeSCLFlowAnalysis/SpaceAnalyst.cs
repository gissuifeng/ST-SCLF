using ArcObjectToolbox;
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

namespace SpacetimeSCLFlowAnalysis
{
    class SpaceAnalyst
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

        public Dictionary<string, List<string>> PolygonNeighbors(
            IFeatureClass featureClass,
            string uid_field_name,
            IQueryFilter queryFilter,
            bool areaOverlap = false,
            bool containsSelf = true)
        {
            if (featureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                return null;

            int uid_idx = featureClass.FindField(uid_field_name);

            // 创建输出字典
            Dictionary<string, List<string>> outDict = new Dictionary<string, List<string>>();
            var featureCursor = featureClass.Search(queryFilter, true);
            IFeature feature = null;

            AddStepProgressor(featureClass.FeatureCount(queryFilter), "Processing Neighbors . . . ");
            while ((feature = featureCursor.NextFeature()) != null)
            {
                if (!ContinueStepProgressor(new object[] { featureCursor }))
                    return null;

                IRelationalOperator2 relOperator = (IRelationalOperator2)feature.Shape;

                List<string> nbr_list = new List<string>();
                var nbFeatureCursor = featureClass.Search(queryFilter, true);
                IFeature nbFeature = null;
                while ((nbFeature = nbFeatureCursor.NextFeature()) != null)
                {
                    if (nbFeature.OID == feature.OID) continue;
                    if (areaOverlap) // 分析重合的相邻情况
                    {
                        if (relOperator.IsNear(nbFeature.Shape, 0) || relOperator.Overlaps(nbFeature.Shape))
                        {
                            nbr_list.Add(nbFeature.Value[uid_idx].ToString());
                        }
                    }
                    else
                    {
                        if (relOperator.IsNear(nbFeature.Shape, 0))
                        {
                            nbr_list.Add(nbFeature.Value[uid_idx].ToString());
                        }
                    }
                }
                Marshal.ReleaseComObject(nbFeatureCursor);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (nbr_list.Count > 0) // 插入数据
                {
                    string main_str = feature.Value[uid_idx].ToString();
                    if (containsSelf) nbr_list.Insert(0, main_str);
                    outDict.Add(main_str, nbr_list);
                }

            }
            Marshal.ReleaseComObject(featureCursor);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return outDict;
        }

        public Dictionary<string, List<string>> PolygonNeighbors(
            IFeatureClass featureClass,
            string uid_field_name)
        {
            if (featureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                return null;

            int uid_idx = featureClass.FindField(uid_field_name);

            // 创建输出字典
            Dictionary<string, List<string>> outDict = new Dictionary<string, List<string>>();
            var featureCursor = featureClass.Search(null, true);
            IFeature feature = null;

            AddStepProgressor(featureClass.FeatureCount(null), "Processing Neighbors . . . ");
            while ((feature = featureCursor.NextFeature()) != null)
            {
                if (!ContinueStepProgressor(new object[] { featureCursor }))
                    return null;

                IRelationalOperator2 relOperator = (IRelationalOperator2)feature.Shape;

                List<string> nbr_list = new List<string>();

                ISpatialFilter spatialFilter = new SpatialFilterClass();
                spatialFilter.Geometry = feature.Shape;
                spatialFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelTouches;

                var nbFeatureCursor = featureClass.Search(spatialFilter, true);
                IFeature nbFeature = null;
                while ((nbFeature = nbFeatureCursor.NextFeature()) != null)
                {
                    if (nbFeature.OID == feature.OID) continue;
                     
                    nbr_list.Add(nbFeature.Value[uid_idx].ToString());

                }
                Marshal.ReleaseComObject(nbFeatureCursor);
                GC.Collect();
                GC.WaitForPendingFinalizers();

                if (nbr_list.Count > 0) // 插入数据
                {
                    string main_str = feature.Value[uid_idx].ToString();
                    nbr_list.Insert(0, main_str);
                    outDict.Add(main_str, nbr_list);
                }
            }

            Marshal.ReleaseComObject(featureCursor);
            GC.Collect();
            GC.WaitForPendingFinalizers();

            return outDict;
        }

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

    }
}
