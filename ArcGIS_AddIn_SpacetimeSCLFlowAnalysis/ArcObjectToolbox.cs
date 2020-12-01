using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.DataSourcesOleDB;
using ESRI.ArcGIS.DataSourcesRaster;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.GeoAnalyst;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.NetworkAnalyst;
using ESRI.ArcGIS.SpatialAnalyst;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// 基于ArcObject实现的ArcMap的Toolbox功能
/// 2018-09-08更新内容：1. 添加了EditingTools类
///                    2. 新增EditingTools类的Generalize方法
///                    3. 新增DataManagementTools类的FeatureVerticesToPoints、SplitLineAtVertices方法
/// 2018-11-11更新内容：1. 完善DataManagementTools类的OpenWorkspace方法，添加对工作空间的判断
/// 2018-11-20更新内容：1. 在有数据读取/输出的方法中，添加了对对路径存在性的try{}catch{}语句
/// 2018-11-21更新内容：1. 添加了.mdb格式的数据库数据读取/写入
///                    2. 添加了CreatePersonalGDB方法，创建.mdb数据库
///                    3. 函数的输入参数为坐标系时，将对坐标系进行判断，如果是null，则会退出方法/new UnknowCoordinationClass();
/// 2018-11-22更新内容：1. 添加了SpatialStatisticsTool类，并实现了ExportFeatureAttributeToText方法       
/// 2018-11-25更新内容：1. 新增DataManagementTools类的SplitLineAtPoint方法、Merge方法
///                    2. 新增DataManagementTools类方法参数的ParamPointType枚举类型
///                    3. 新增NetworkAnalystTools类方法参数的ParamLocationsItem枚举类型
///                    4. 精简了一些函数, 加强了函数中对Shapefile与Geodatabase路径的判断
/// 2018-11-26更新内容：1. 修改了DataManagementTools类的CreateNeighborhoodNet方法
///                    2. 新增DataManagementTools类的PointsToLine方法
///                    3. 新增DataManagementTools类的Point2ToLine方法
/// 2018-11-27更新内容：1. 新增AnalysisTools类, 以及其Buffer方法
/// 2018-12-04更新内容：1. 新增DataManagementTools类的CreateArcInforWorkspace方法
///                    2. 完善DataManagementTools类的AddFiled方法对输入参数field_name的判断
/// 2018-12-10更新内容：1. 新增DataManagementTools类的RecalculateFeatureClassExtent方法
/// 2019-01-29更新内容：1. 新增PersistTools类的ByteToIProjectedCoordinateSys方法
///                    2. 重载DataManagementTools类的Clip方法，新的Clip方法具备象元捕捉
///                    3. 重载DataManagementTools类的ProjectRaster方法，新方法结果与ArcToolBox，ProjectRaster工具相同
/// 2019-01-30更新内容：1. 重载ConversionTools类的RasterToOtherFormat方法，支持对IRaster借口对象输出
///                    2. 新增DataManagementTools类的ProjectRasterWithDatumTransformation方法
/// 2019-01-31更新内容：1. 对ConversionTools类的RasterToGeodatabase方法的参数进行了类型修改
///                    2. 对ConversionTools类的RasterToOtherFormat方法的参数进行了类型修改
/// 2019-02-02更新内容：1. 修改了对所有类中COM对象的释放
/// 2019-03-18更新内容：1. 类PersistTools更名为SerializeTools
///                    2. 删除原PersistTools类中的所有方法，在SerializeTools类中添加了新方法
/// 2019-08-21更新内容：1. 修改DataManagementTools类的AddFiled方法，增加了对字段名的检验
///                    2. 新增DataManagementTools类的OpenExcelTable方法
///                    3. 新增DataManagementTools类的OpenTextFileTable方法
///                    4. 新增DataManagementTools类的OpenCSVUTF8File方法
/// 2020-03-06更新内容：1. 增加AnalysisTools类的PolygonNeighbors方法
///                    2. 新增DataManagementTools类的CreateTable方法
/// 2020-03-16更新内容：1. 增加DataManagementTools类的OpenTable方法
/// </summary>
namespace ArcObjectToolbox
{
    public enum ParamEndType
    {
        ROUND, FLAT
    }

    public enum ParamSideType
    {
        FULL, LEFT, RIGHT, OUTSIDE_ONLY
    }

    public class AnalysisTools
    {
        /// <summary>
        /// 围绕输入要素创建指定距离的缓冲区多边形
        /// </summary>
        /// <param name="inFeatureClass">输入要素类</param>
        /// <param name="inQueryFilter">输入要素类查询条件</param>
        /// <param name="outFeatureClass_path">输出缓冲区要素类路径</param>
        /// <param name="outFeatureClass_name">输出缓冲区要素类名</param>
        /// <param name="featureDataset_name">输出路径为数据库时,其要素数据集名</param>
        /// <param name="distance_linear">缓冲区指定距离</param>
        /// <param name="distance_field">输入要素某字段值作为缓冲区距离,优先级高于distance_linear</param>
        /// <param name="sideType">缓冲外围形状,此参数对多边形要素无效</param>
        /// <param name="endType">缓冲在要素的周边位置</param>
        /// <returns></returns>
        public IFeatureClass Buffer(
            IFeatureClass inFeatureClass,
            IQueryFilter inQueryFilter,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name,
            double distance_linear,
            string distance_field = null,
            ParamSideType sideType = ParamSideType.FULL,
            ParamEndType endType = ParamEndType.ROUND,
            bool IsDissolve = false)
        {
            if (inFeatureClass == null ||
                String.IsNullOrWhiteSpace(outFeatureClass_path) ||
                String.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            // 判断字段是否可用
            int distanceFieldIndex = -1;
            if (distance_field != null)
            {
                distanceFieldIndex = inFeatureClass.FindField(distance_field);
                if (distanceFieldIndex >= 0)
                {
                    var distanceField = inFeatureClass.Fields.get_Field(distanceFieldIndex);
                    if (distanceField.Type != esriFieldType.esriFieldTypeDouble &&
                        distanceField.Type != esriFieldType.esriFieldTypeInteger &&
                        distanceField.Type != esriFieldType.esriFieldTypeSingle &&
                        distanceField.Type != esriFieldType.esriFieldTypeSmallInteger)
                        distanceFieldIndex = -1;
                }
            }

            // 距离与字段都不可用
            if (distance_linear <= 0 && distanceFieldIndex == -1)
                return null;

            // 获取输入要素类的图形
            IGeometryCollection inGeometryBag = new GeometryBagClass();
            IDoubleArray distanceValueArray = new DoubleArray();
            var inFeatureCursor = inFeatureClass.Search(inQueryFilter, false);
            IFeature inFeature = inFeatureCursor.NextFeature();
            while (inFeature != null)
            {
                // 将要素形状放入容器内
                inGeometryBag.AddGeometry(inFeature.ShapeCopy);

                // 获取要素距离字段值，放入distanceFieldValueArray容器中
                if (distanceFieldIndex != -1)
                    distanceValueArray.Add((double)inFeature.get_Value(distanceFieldIndex));

                inFeature = inFeatureCursor.NextFeature();
            }

            Marshal.ReleaseComObject(inFeatureCursor);

            var inEnumGeometry = (IEnumGeometry)inGeometryBag;
            if (inEnumGeometry == null) return null;

            // 设置缓冲区属性
            IBufferConstruction bufferConstruction = new BufferConstructionClass();
            var bufferConstructProperties = (IBufferConstructionProperties)bufferConstruction;
            bufferConstructProperties.UnionOverlappingBuffers = IsDissolve;

            // 设置要素缓冲区的外围形状, 此参数对多边形要素无效
            if (endType == ParamEndType.FLAT)
                bufferConstructProperties.EndOption = esriBufferConstructionEndEnum.esriBufferFlat;
            else
                bufferConstructProperties.EndOption = esriBufferConstructionEndEnum.esriBufferRound;

            // 设置生成的缓冲在要素的周边位置
            if (sideType == ParamSideType.OUTSIDE_ONLY)
                bufferConstructProperties.OutsideOnly = true;
            else if (sideType == ParamSideType.FULL)
                bufferConstructProperties.SideOption = esriBufferConstructionSideEnum.esriBufferFull;
            else if (sideType == ParamSideType.LEFT)
                bufferConstructProperties.SideOption = esriBufferConstructionSideEnum.esriBufferLeft;
            else if (sideType == ParamSideType.RIGHT)
                bufferConstructProperties.SideOption = esriBufferConstructionSideEnum.esriBufferRight;

            // 计算缓冲区
            IGeometryCollection outBufferGeometryBag = new GeometryBagClass();
            if (distanceFieldIndex == -1)
                bufferConstruction.ConstructBuffers(inEnumGeometry, distance_linear, outBufferGeometryBag);
            else
                bufferConstruction.ConstructBuffersByDistances2(inEnumGeometry, distanceValueArray, outBufferGeometryBag);

            // 检查缓冲区结果,偶尔会出现第一个缓冲区重复出现
            if (outBufferGeometryBag == null) return null;
            var outEnumGeometry = (IEnumGeometry)outBufferGeometryBag;
            if (outEnumGeometry.Count > inEnumGeometry.Count)
                outBufferGeometryBag.RemoveGeometries(0, 1);

            // 建立输出的结果要素类, 并添加属性字段名
            var inGeoDataset = (IGeoDataset)inFeatureClass;
            DataManagementTools dataManagementTools = new DataManagementTools();
            var outFeatureClass = dataManagementTools.CreateFeatureClass(
                outFeatureClass_path,
                outFeatureClass_name,
                featureDataset_name,
                esriGeometryType.esriGeometryPolygon,
                inGeoDataset.SpatialReference);
            int buffDistIndex = -1;
            if (IsDissolve == false) // 没有融合要素，则获得BUFF_DIST字段索引
            {
                for (int fieldIndex = 0; fieldIndex < inFeatureClass.Fields.FieldCount; fieldIndex++)
                {
                    IField inField = inFeatureClass.Fields.get_Field(fieldIndex);
                    if (inField.Editable == true)
                        dataManagementTools.AddField((ITable)outFeatureClass, inField.Name, inField.Type, true);
                }
                dataManagementTools.AddField((ITable)outFeatureClass, "BUFF_DIST", esriFieldType.esriFieldTypeDouble, true);
                buffDistIndex = outFeatureClass.FindField("BUFF_DIST");
            }

            // 向输出要素类中写入属性信息
            inFeatureCursor = inFeatureClass.Search(inQueryFilter, true);
            var outFeatureCursor = outFeatureClass.Insert(true);
            var outFeatureBuffer = outFeatureClass.CreateFeatureBuffer();
            IGeometry bufferShape = null;
            for (int bufferIndex = 0; bufferIndex < outEnumGeometry.Count; bufferIndex++)
            {
                bufferShape = outEnumGeometry.Next();
                inFeature = inFeatureCursor.NextFeature();
                for (int inFieldIndex = 0; inFieldIndex < inFeature.Fields.FieldCount; inFieldIndex++)
                {
                    IField inField = inFeature.Fields.get_Field(inFieldIndex);
                    if (inField.Editable == false) continue;
                    if (inField.Type == esriFieldType.esriFieldTypeGeometry) continue;
                    int outFieldIndex = outFeatureBuffer.Fields.FindField(inField.Name);
                    if (outFieldIndex == -1) continue;
                    outFeatureBuffer.set_Value(outFieldIndex, inFeature.get_Value(inFieldIndex));
                }
                if (buffDistIndex != -1)
                {
                    if (distanceFieldIndex == -1)
                        outFeatureBuffer.set_Value(buffDistIndex, distance_linear);
                    else
                        outFeatureBuffer.set_Value(buffDistIndex, distanceValueArray.Element[bufferIndex]);
                }

                outFeatureBuffer.Shape = bufferShape;
                outFeatureCursor.InsertFeature(outFeatureBuffer);
            }
            outFeatureCursor.Flush();

            Marshal.ReleaseComObject(inFeatureCursor);
            Marshal.ReleaseComObject(outFeatureCursor);
            Marshal.ReleaseComObject(outFeatureBuffer);
            Marshal.ReleaseComObject(outEnumGeometry);
            Marshal.ReleaseComObject(inEnumGeometry);

            return outFeatureClass;
        }

        /// <summary>
        /// 根据面邻接（重叠、重合边或结点）创建统计数据表。
        /// </summary>
        /// <param name="inFeatureClass">输入面要素类</param>
        /// <param name="inQueryFilter">输入要素类查询条件</param>
        /// <param name="areaOverlap">确定是否会在输出中分析重叠面</param>
        /// <returns></returns>
        public ITable PolygonNeighbors(
            IFeatureClass inFeatureClass,
            string[] fieldNames,
            string outTable_path,
            string outTable_name,
            IQueryFilter inQueryFilter,
            bool areaOverlap = false)
        {
            if (inFeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                return null;

            // 建立字段
            IFields tbFields = new FieldsClass();
            IFieldsEdit tbFieldEdit = (IFieldsEdit)tbFields;
            List<Dictionary<int, int>> fieldDictList = new List<Dictionary<int, int>>();
            string[] types = { "str_", "nbr_" };
            foreach (var item in types)
            {
                Dictionary<int, int> fieldDict = new Dictionary<int, int>();
                for (int i = 0; i < fieldNames.Length; i++)
                {
                    int idx = inFeatureClass.FindField(fieldNames[i]);
                    int t_idx = tbFields.FieldCount;
                    fieldDict.Add(idx, t_idx);

                    var tmpField = (IField)((IClone)inFeatureClass.Fields.Field[idx]).Clone(); // 克隆字段
                    ((IFieldEdit)tmpField).Name_2 = item + tmpField.Name;
                    tbFieldEdit.AddField(tmpField);
                }

                fieldDictList.Add(fieldDict);
            }

            // 创建输出列表
            var dataMgTools = new DataManagementTools();
            ITable outputTable = dataMgTools.CreateTable(outTable_path, outTable_name, tbFields);


            IRowBuffer rowBuffer = null;
            ICursor rowCursor = outputTable.Insert(true);
            var featureCursor = inFeatureClass.Search(inQueryFilter, false);
            IFeature feature = null;
            while ((feature = featureCursor.NextFeature()) != null)
            {
                IRelationalOperator2 relOperator = (IRelationalOperator2)feature.Shape;

                var nbCursor = inFeatureClass.Search(inQueryFilter, false);
                IFeature nbfeature = null;
                while ((nbfeature = nbCursor.NextFeature()) != null)
                {
                    if (nbfeature.OID == feature.OID) continue;
                    if (areaOverlap) // 分析重合的相邻情况
                    {
                        if (relOperator.Touches(nbfeature.Shape) ||
                            relOperator.Overlaps(nbfeature.Shape))
                        {
                            // 在表中写入信息
                            rowBuffer = outputTable.CreateRowBuffer();

                            foreach (var item in fieldDictList[0])
                                rowBuffer.Value[item.Value] = feature.Value[item.Key];
                            foreach (var item in fieldDictList[1])
                                rowBuffer.Value[item.Value] = nbfeature.Value[item.Key];

                            rowCursor.InsertRow(rowBuffer);
                        }
                    }
                    else
                    {
                        if (relOperator.Touches(nbfeature.Shape) || relOperator.IsNear(nbfeature.Shape, 0))
                        {
                            // 在表中写入信息
                            rowBuffer = outputTable.CreateRowBuffer();
                            foreach (var item in fieldDictList[0])
                                rowBuffer.Value[item.Value] = feature.Value[item.Key];
                            foreach (var item in fieldDictList[1])
                                rowBuffer.Value[item.Value] = nbfeature.Value[item.Key];

                            rowCursor.InsertRow(rowBuffer);
                        }
                    }
                }
            }

            rowCursor.Flush();
            Marshal.ReleaseComObject(rowCursor);
            Marshal.ReleaseComObject(rowBuffer);
            Marshal.ReleaseComObject(tbFields);

            return outputTable;
        }
    }

    public class CartographyTools
    {
        /// <summary>
        /// 对Polylin类型的要素类要素进行Bezier平滑处理
        /// </summary>
        /// <param name="featureClass">要素类</param>
        /// <param name="maxOffset">平滑的最偏移量</param>
        public void SmoothLine(IFeatureClass featureClass, double maxOffset)
        {
            if (featureClass.ShapeType != esriGeometryType.esriGeometryPolyline)
                return;

            var featureCursor = featureClass.Update(null, true);
            var feature = featureCursor.NextFeature();
            while (feature != null)
            {
                var polyline = (IPolycurve)feature.ShapeCopy;
                polyline.Smooth(maxOffset);
                feature.Shape = polyline;
                featureCursor.UpdateFeature(feature);

                feature = featureCursor.NextFeature();
            }

            Marshal.ReleaseComObject(featureCursor);
        }

        /// <summary>
        /// 简化Polyline类型的要素类中的要素
        /// *该方法存在问题，尚待解决中，不推荐使用
        /// </summary>
        /// <param name="featureClass"></param>
        public void SimplifyLine(IFeatureClass featureClass)
        {
            if (featureClass.ShapeType != esriGeometryType.esriGeometryPolyline)
                return;

            var featureCursor = featureClass.Update(null, true);
            var feature = featureCursor.NextFeature();
            while (feature != null)
            {
                var polycurve0 = (IPolycurve)feature.Shape;
                var geometryCollection0 = (IGeometryCollection)polycurve0;
                int count0 = geometryCollection0.GeometryCount;

                var geometry = feature.ShapeCopy;
                var pointCollection = (IPointCollection)geometry;
                var enumVertices = pointCollection.EnumVertices;
                var polycurve = (IPolycurve2)geometry;
                polycurve.SplitAtPoints(enumVertices, true, true, 0.0001);
                var geometryCollection = (IGeometryCollection)polycurve;

                IPolyline newPolyline = new PolylineClass();
                var newSegCollection = (ISegmentCollection)newPolyline;
                object obj = Type.Missing;
                for (int i = 0; i < geometryCollection.GeometryCount; i++)
                {
                    var segmentCollection = (ISegmentCollection)geometryCollection.get_Geometry(i);
                    newSegCollection.AddSegment(segmentCollection.get_Segment(0), ref obj, ref obj);
                }

                var topoOp2 = (ITopologicalOperator2)newSegCollection;
                topoOp2.IsKnownSimple_2 = false;
                //bool bIsSimple = topoOp2.IsSimple;
                topoOp2.Simplify();

                geometryCollection = (IGeometryCollection)topoOp2;
                int count = geometryCollection.GeometryCount;

                feature.Shape = (IGeometry)topoOp2;
                featureCursor.UpdateFeature(feature);
                feature = featureCursor.NextFeature();
            }

            Marshal.ReleaseComObject(featureCursor);
        }
    }

    public class ConversionTools
    {
        /// <summary>
        /// 该函数目前不可用
        /// 要素类转为ArcInforWorkspace的Coverage数据
        /// </summary>
        /// <param name="featureClass">要素类</param>
        /// <param name="coverage_path">Coverage数据集路径</param>
        /// <param name="coverage_name">Coverage数据集名</param>
        public void FeatureClassToCoverage(
            IFeatureClass featureClass,
            string coverage_path,
            string coverage_name)
        {
            if (featureClass == null
               || String.IsNullOrWhiteSpace(coverage_path)
               || String.IsNullOrWhiteSpace(coverage_name)
               || System.IO.Path.GetExtension(coverage_path) != "") return;

            // 输出*shp文件的工作空间
            IWorkspaceName outWorkspaceName = new WorkspaceNameClass();
            outWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesFile.ArcInfoWorkspaceFactory";
            outWorkspaceName.PathName = coverage_path;
            IWorkspace outWorkspace = null;
            try
            { outWorkspace = outWorkspaceName.WorkspaceFactory.OpenFromFile(coverage_path, 0); }
            catch (Exception)
            {
                if (outWorkspaceName != null) Marshal.ReleaseComObject(outWorkspaceName);
                if (outWorkspace != null) Marshal.ReleaseComObject(outWorkspace);
                return;
            }
            // 判断是否存在将要输出的*.shp文件
            //IWorkspace2 out_workspace2 = (IWorkspace2)out_workspace; // 报错不能通过IWorkspace2删除存在的对象了
            IEnumDataset enumDataset = outWorkspace.get_Datasets(esriDatasetType.esriDTFeatureDataset);
            List<string> datasetNameList = new List<string>();
            IDataset dataset = enumDataset.Next();
            while (dataset != null)
            {
                if (dataset.Name == coverage_name)
                {
                    if (dataset.CanDelete())
                    {
                        dataset.Delete();
                        break;
                    }

                    Marshal.ReleaseComObject(enumDataset);
                    Marshal.ReleaseComObject(dataset);
                    return;
                }

                dataset = enumDataset.Next();
            }

            // 输出要素数据集名
            var arcInfowWorkspace = (IArcInfoWorkspace)outWorkspace;
            var outFeatureDataset = arcInfowWorkspace.CreateCoverage(
                coverage_name, null, esriCoveragePrecisionType.esriCoveragePrecisionDouble);

            // 复制要素类到输出路径中
            string copyName = "";
            if (featureClass.ShapeType == esriGeometryType.esriGeometryPoint)
                copyName = "point";
            else if (featureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                copyName = "route.line";
            else if (featureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                copyName = "polygon";
            else
                return;

            var coverageFeatureClass = (ICoverageFeatureClass2)featureClass;
            coverageFeatureClass.Copy(copyName, outFeatureDataset);

            Marshal.ReleaseComObject(outWorkspaceName);
            Marshal.ReleaseComObject(outWorkspace);
            Marshal.ReleaseComObject(outFeatureDataset);

        }

        /// <summary>
        /// 导出要素类到Shapefile中
        /// </summary>
        /// <param name="featureClass">待导出的要素类</param>
        /// <param name="queryFilter">要素过滤器</param>
        /// <param name="featureClass_path">要素类的文件夹路径</param>
        /// <param name="featureClass_name">Shapefile要素类名</param>
        public void FeatureClassToShapefile(
            IFeatureClass featureClass,
            IQueryFilter queryFilter,
            string featureClass_path,
            string featureClass_name)
        {
            if (featureClass == null
                || string.IsNullOrWhiteSpace(featureClass_path)
                || string.IsNullOrWhiteSpace(featureClass_name)
                || System.IO.Path.GetExtension(featureClass_path) != "") return;

            // 输出*shp文件的工作空间
            IWorkspaceName outWorkspaceName = new WorkspaceNameClass();
            outWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesFile.ShapefileWorkspaceFactory";
            outWorkspaceName.PathName = featureClass_path;
            IWorkspace outWorkspace = null;
            try
            {
                outWorkspace = outWorkspaceName.WorkspaceFactory.OpenFromFile(featureClass_path, 0);
            }
            catch (Exception)
            {
                if (outWorkspaceName != null) Marshal.ReleaseComObject(outWorkspaceName);
                if (outWorkspace != null) Marshal.ReleaseComObject(outWorkspace);
                return;
            }

            // 判断是否存在将要输出的*.shp文件
            IFeatureWorkspace outFeatureWorkspace = (IFeatureWorkspace)outWorkspace;
            IWorkspace2 outWorkspace2 = (IWorkspace2)outWorkspace;
            if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureClass, featureClass_name))
            {
                IDataset outDataset = (IDataset)outFeatureWorkspace.OpenFeatureClass(featureClass_name);
                outDataset.Delete();
                Marshal.ReleaseComObject(outDataset);
            }

            // 输出*.shp文件名
            IFeatureClassName outFeatureClassName = new FeatureClassNameClass();
            IDatasetName outDatasetName = (IDatasetName)outFeatureClassName;
            outDatasetName.WorkspaceName = outWorkspaceName;
            outDatasetName.Name = featureClass_name;

            // 输入要素类的工作空间
            IDataset inDataset = featureClass as IDataset;
            IFeatureClassName inFeatureClassName = inDataset.FullName as IFeatureClassName;
            IWorkspace inWorkspace = inDataset.Workspace;

            //检查字段的有效性
            IFieldChecker fieldChecker = new FieldCheckerClass();
            fieldChecker.InputWorkspace = inWorkspace;
            fieldChecker.ValidateWorkspace = outWorkspace;
            IFields inFields = featureClass.Fields;
            IEnumFieldError enumFieldError;
            IFields outFields;
            fieldChecker.Validate(inFields, out enumFieldError, out outFields);

            // 调用IFeatureDataConverter接口进行数据转换
            IFeatureDataConverter featureDataConverter = new FeatureDataConverterClass();
            featureDataConverter.ConvertFeatureClass(
                inFeatureClassName,
                queryFilter,
                null,
                outFeatureClassName,
                null,
                outFields,
                "",
                1000,
                0);

            Marshal.ReleaseComObject(outWorkspaceName);
            Marshal.ReleaseComObject(outWorkspace);
            Marshal.ReleaseComObject(outFeatureClassName);
            Marshal.ReleaseComObject(inFeatureClassName);
            Marshal.ReleaseComObject(inWorkspace);
            Marshal.ReleaseComObject(fieldChecker);
            Marshal.ReleaseComObject(inFields);
            Marshal.ReleaseComObject(outFields);
            Marshal.ReleaseComObject(featureDataConverter);
        }

        /// <summary>
        /// 将要素类输出到Geodatabase中
        /// </summary>
        /// <param name="featureClass">待导出的要素类</param>
        /// <param name="queryFilter">要素过滤器</param>
        /// <param name="featureClass_path">Geodatabase全名(路径+名+.gdb)</param>
        /// <param name="featureClass_name">输出要素类名</param>
        /// <param name="featureDatast_name">Geodatabase中要素数据集名，若不存在该数据集则创建</param>
        public void FeatureClassToGeodatabase(
            IFeatureClass featureClass,
            IQueryFilter queryFilter,
            string featureClass_path,
            string featureClass_name,
            string featureDataset_name = null)
        {
            if (featureClass == null ||
                String.IsNullOrWhiteSpace(featureClass_path) ||
                String.IsNullOrWhiteSpace(featureClass_name))
                return;

            string extension = System.IO.Path.GetExtension(featureClass_path);
            // 输出的工作空间名
            IWorkspaceName outWorkspaceName = new WorkspaceNameClass();
            if (extension == ".gdb")
                outWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesGDB.FileGDBWorkspaceFactory";
            else if (extension == ".mdb")
                outWorkspaceName.WorkspaceFactoryProgID = "esriDataSourcesGDB.AccessWorkspaceFactory";
            else
                return;

            outWorkspaceName.PathName = featureClass_path;
            IWorkspace outWorkspace = null;
            try
            {
                outWorkspace = outWorkspaceName.WorkspaceFactory.OpenFromFile(featureClass_path, 0);
            }
            catch (Exception)
            {
                if (outWorkspaceName != null) Marshal.ReleaseComObject(outWorkspaceName);
                if (outWorkspace != null) Marshal.ReleaseComObject(outWorkspace);
                return;
            }

            // 检查要素集存在
            IWorkspace2 outWorkspace2 = (IWorkspace2)outWorkspace;
            IFeatureWorkspace outFeatureWorkspace = (IFeatureWorkspace)outWorkspace2;
            if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureClass, featureClass_name))
            {
                IDataset outDataset = (IDataset)outFeatureWorkspace.OpenFeatureClass(featureClass_name);
                outDataset.Delete();
                Marshal.ReleaseComObject(outDataset);
            }

            // 输出要素数据集名
            IFeatureDatasetName outFeatureDatasetName = null;
            if (!String.IsNullOrWhiteSpace(featureDataset_name))
            {
                IFeatureDataset outFeatureDataset = null;
                if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featureDataset_name))
                {
                    outFeatureDataset = outFeatureWorkspace.OpenFeatureDataset(featureDataset_name);
                }
                else
                {
                    IGeoDataset geoDataset = (IGeoDataset)featureClass;
                    outFeatureDataset = outFeatureWorkspace.CreateFeatureDataset(featureDataset_name, geoDataset.SpatialReference);
                }

                outFeatureDatasetName = (IFeatureDatasetName)outFeatureDataset.FullName;

                Marshal.ReleaseComObject(outFeatureDataset);
            }

            // 输出FeatureClass名
            IFeatureClassName outFeatureClassName = new FeatureClassNameClass();
            IDatasetName outDatasetName = outFeatureClassName as IDatasetName;
            outDatasetName.WorkspaceName = outWorkspaceName;
            outDatasetName.Name = featureClass_name;

            // 输入的工作空间
            IDataset inDataset = featureClass as IDataset;
            IFeatureClassName inFeatureClassName = inDataset.FullName as IFeatureClassName;
            IWorkspace inWorkspace = inDataset.Workspace;

            //检查字段的有效性
            IFieldChecker fieldChecker = new FieldCheckerClass();
            fieldChecker.InputWorkspace = inWorkspace;
            fieldChecker.ValidateWorkspace = outWorkspace;
            IFields inFields = featureClass.Fields;
            IEnumFieldError enumFieldError;
            IFields outFields;
            fieldChecker.Validate(inFields, out enumFieldError, out outFields);

            // 调用IFeatureDataConverter接口进行数据转换
            IFeatureDataConverter featureDataConverter = new FeatureDataConverterClass();
            featureDataConverter.ConvertFeatureClass(
                inFeatureClassName,
                queryFilter,
                outFeatureDatasetName,
                outFeatureClassName,
                null,
                outFields,
                "",
                1000,
                0);

            Marshal.ReleaseComObject(outWorkspaceName);
            Marshal.ReleaseComObject(outWorkspace);
            if (outFeatureDatasetName != null) Marshal.ReleaseComObject(outFeatureDatasetName);
            Marshal.ReleaseComObject(outFeatureClassName);
            Marshal.ReleaseComObject(inFeatureClassName);
            Marshal.ReleaseComObject(inWorkspace);
            Marshal.ReleaseComObject(fieldChecker);
            Marshal.ReleaseComObject(inFields);
            Marshal.ReleaseComObject(outFields);
            Marshal.ReleaseComObject(featureDataConverter);
        }

        /// <summary>
        /// 栅格数据导出到Geodatabase
        /// </summary>
        /// <param name="rasterDataset">导出的栅格数据</param>
        /// <param name="rasterDataset_path">Geodatabase全名(路径+名+.gdb)</param>
        /// <param name="rasterDataset_name">栅格数据集名</param>
        public void RasterToGeodatabase(
            IRasterDataset rasterDataset,
            string rasterDataset_path,
            string rasterDataset_name)
        {
            string extension = System.IO.Path.GetExtension(rasterDataset_path);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else
                return;

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            { workspace = workspaceFactory.OpenFromFile(rasterDataset_path, 0); }
            catch
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return;
            }

            IRasterWorkspaceEx rasterWorkspaceEx = workspace as IRasterWorkspaceEx;
            IWorkspace2 workspace2 = (IWorkspace2)workspace;

            // 已经同名栅格文件, 则删除
            if (workspace2.get_NameExists(esriDatasetType.esriDTRasterDataset, rasterDataset_name))
                rasterWorkspaceEx.DeleteRasterDataset(rasterDataset_name);

            // 导入到GDB
            IRasterStorageDef2 rasterStorageDef2 = new RasterStorageDefClass();
            rasterStorageDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionLZ77;

            ISaveAs2 saveAs2 = (ISaveAs2)rasterDataset;
            saveAs2.SaveAsRasterDataset(rasterDataset_name, workspace, "GDB", rasterStorageDef2);

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(rasterStorageDef2);
        }

        /// <summary>
        /// 栅格数据导出为其他格式文件
        /// </summary>
        /// <param name="rasterDataset">栅格数据</param>
        /// <param name="out_path">导出路径</param>
        /// <param name="out_name">文件名</param>
        /// <param name="out_ext">文件格式(.tif/.image/.jpg/.jp2/.bmp/.png/.gif)</param>
        public void RasterToOtherFormat(
            IRasterDataset rasterDataset,
            string out_path,
            string out_name,
            string out_ext)
        {
            // 删除已经存在的*.tif文件
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesRaster.RasterWorkspaceFactory");
            IWorkspaceFactory outWorkspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace outWorkspace = null;
            try
            {
                outWorkspace = outWorkspaceFactory.OpenFromFile(out_path, 0);
            }
            catch
            {
                if (outWorkspaceFactory != null) Marshal.ReleaseComObject(outWorkspaceFactory);
                if (outWorkspace != null) Marshal.ReleaseComObject(outWorkspace);
                return;
            }

            IRasterWorkspace outRasterWorkspace = (IRasterWorkspace)outWorkspace;

            if (File.Exists(out_path + @"\" + out_name + out_ext)) // 检查是否有同名文件
            {
                var outGeoDataset = (IGeoDataset)outRasterWorkspace.OpenRasterDataset(
                    out_name + out_ext);
                var outDataset = (IDataset)outGeoDataset;
                outDataset.Delete();
                Marshal.FinalReleaseComObject(outGeoDataset);
            }

            IRasterStorageDef2 rasterSDef2 = new RasterStorageDefClass();
            ISaveAs2 saveAs2 = (ISaveAs2)rasterDataset;
            if (out_ext == ".tif")
            {
                rasterSDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionLZW;
                saveAs2.SaveAsRasterDataset(out_name + ".tif", outWorkspace, "TIFF", rasterSDef2);
            }
            else if (out_ext == ".image")
            {
                rasterSDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionLZ77;
                saveAs2.SaveAsRasterDataset(out_name + ".img", outWorkspace, "IMAGINE", rasterSDef2);
            }
            else if (out_ext == ".jpg")
            {
                rasterSDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionJPEG;
                saveAs2.SaveAsRasterDataset(out_name + ".jpg", outWorkspace, "JPG", rasterSDef2);
            }
            else if (out_ext == ".jp2")
            {
                rasterSDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionJPEG2000;
                saveAs2.SaveAsRasterDataset(out_name + ".jp2", outWorkspace, "JP2", rasterSDef2);
            }
            else if (out_ext == ".bmp")
            {
                rasterSDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionUncompressed;
                saveAs2.SaveAsRasterDataset(out_name + ".bmp", outWorkspace, "BMP", rasterSDef2);
            }
            else if (out_ext == ".png")
            {
                rasterSDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionUncompressed;
                saveAs2.SaveAsRasterDataset(out_name + ".png", outWorkspace, "PNG", rasterSDef2);
            }
            else if (out_ext == ".gif")
            {
                rasterSDef2.CompressionType = esriRasterCompressionType.esriRasterCompressionUncompressed;
                saveAs2.SaveAsRasterDataset(out_name + ".gif", outWorkspace, "GIF", rasterSDef2);
            }

            Marshal.ReleaseComObject(outWorkspaceFactory);
            Marshal.ReleaseComObject(outWorkspace);
            Marshal.ReleaseComObject(rasterSDef2);
        }

        /// <summary>
        /// 栅格转点要素类
        /// </summary>
        /// <param name="rasterGeoDataset">输入栅格数据</param>
        /// <param name="featureClass_path">输出点要素类路径(无空格字符)</param>
        /// <param name="featureClass_name">输出点要素类名</param>
        public IFeatureClass RasterToPoint(
            IGeoDataset rasterGeoDataset,
            string featureClass_path,
            string featureClass_name)
        {
            // 判断输入路径类型
            string extension = System.IO.Path.GetExtension(featureClass_path);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            else
                return null;

            // 删除已经存在的同名点要素类
            IWorkspaceFactory outWorkspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace outWorkspace = null;
            try
            { outWorkspace = outWorkspaceFactory.OpenFromFile(featureClass_path, 0); }
            catch
            {
                if (outWorkspaceFactory != null) Marshal.ReleaseComObject(outWorkspaceFactory);
                if (outWorkspace != null) Marshal.ReleaseComObject(outWorkspace);
                return null;
            }

            IFeatureWorkspace outFeatureWorkspace = (IFeatureWorkspace)outWorkspace;
            IWorkspace2 outWorkspace2 = (IWorkspace2)outWorkspace;
            if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureClass, featureClass_name))
            {
                IFeatureClass outFeatureClass = outFeatureWorkspace.OpenFeatureClass(featureClass_name);
                IDataset outDataset = (IDataset)outFeatureClass;
                outDataset.Delete();
                Marshal.FinalReleaseComObject(outFeatureClass);
            }

            IGeoDataset outGeoDataset = null;
            RasterConversionOp rasterConversionOp = new RasterConversionOp();
            IConversionOp conversionOp = (IConversionOp)rasterConversionOp;

            try // outWorkspace的磁盘路径不能有包含空格
            {
                outGeoDataset = conversionOp.RasterDataToPointFeatureData(
                    rasterGeoDataset, outWorkspace, featureClass_name);
            }
            catch (Exception)
            { }

            Marshal.ReleaseComObject(outWorkspaceFactory);
            Marshal.ReleaseComObject(outWorkspace);
            Marshal.ReleaseComObject(rasterConversionOp);

            return (IFeatureClass)outGeoDataset;
        }
    }

    // DataManagementTools类FeatureVerticesToPoints函数参数类型
    public enum ParamPointType
    {
        ALL, MID, START, END, BOTH_ENDS
    }

    public class DataManagementTools
    {
        /// <summary>
        /// 向要素类中添加未存在的字段名的属性字段；
        /// 要求字段名首字符为英文字母或汉字，Shapefile要素类字段名只保留前10个字符
        /// </summary>
        /// <param name="table">要素类</param>
        /// <param name="field_name">字段名</param>
        /// <param name="field_type">字段类型</param>
        /// <param name="isNullable">默认值是否为null</param>
        public string AddField(
            ITable table,
            string field_name,
            esriFieldType field_type,
            bool isNullable = true,
            int length = 50)
        {
            var dataset = (IDataset)table;
            var workspace = dataset.Workspace;

            // 新建字段
            IField newField = new FieldClass();
            IFieldEdit newFieldEdit = newField as IFieldEdit;
            newFieldEdit.Name_2 = field_name;
            newFieldEdit.Type_2 = field_type;
            newFieldEdit.IsNullable_2 = isNullable;
            newFieldEdit.Length_2 = length;

            // 获取要素类的字段集，并添加字段
            IClone fieldClone = ((IClone)table.Fields).Clone();
            IFields tmpFields = (IFields)fieldClone;
            IFieldsEdit tmpFieldsEdit = (IFieldsEdit)tmpFields;
            tmpFieldsEdit.AddField(newField);

            // 字段检查
            IFieldChecker fieldChecker = new FieldCheckerClass();
            fieldChecker.ValidateWorkspace = workspace;
            IEnumFieldError enumError = null;
            IFields fixedFields = null;
            fieldChecker.Validate(tmpFields, out enumError, out fixedFields);

            // 获取并修改错误字段
            if (enumError != null)
            {
                enumError.Reset();
                IFieldError fieldError = null;
                while ((fieldError = enumError.Next()) != null)
                {
                    var errorField = tmpFields.get_Field(fieldError.FieldIndex);
                    Console.WriteLine("Field '{0}': Error '{1}'", errorField.Name, fieldError.FieldError);
                    var fixedField = fixedFields.Field[fieldError.FieldIndex];
                    newField = fixedField;
                }
            }

            table.AddField(newField);

            string newFieldName = newField.Name; 

            Marshal.ReleaseComObject(newField);
            Marshal.ReleaseComObject(tmpFields);
            Marshal.ReleaseComObject(fieldChecker);

            return newFieldName;
        }

        /// <summary>
        /// 将源表的字段复制到目标要素类
        /// </summary>
        /// <param name="sourceDataTable">源表</param>
        /// <param name="targetTable">目标要素类</param>
        /// <param name="length">字符串字段的长度</param>
        /// <returns></returns>
        public Dictionary<int, int> CopyFields(
            DataTable sourceDataTable, ITable targetTable, int length = 50)
        {
            Dictionary<Type, esriFieldType> esriFieldTypeDict = new Dictionary<Type, esriFieldType>()
            {
                { typeof(int), esriFieldType.esriFieldTypeSmallInteger },
                { typeof(long), esriFieldType.esriFieldTypeInteger },
                { typeof(double), esriFieldType.esriFieldTypeDouble },
                { typeof(float), esriFieldType.esriFieldTypeSingle },
                { typeof(string), esriFieldType.esriFieldTypeString },
                { typeof(DateTime), esriFieldType.esriFieldTypeDate }
            };

            Dictionary<int, int> outFieldDict = new Dictionary<int, int>();

            var columnColls = sourceDataTable.Columns;
            for (int i = 0; i < columnColls.Count; i++)
            {
                DataColumn column = columnColls[i];
                var columnName = column.ColumnName;
                var fieldType = esriFieldTypeDict[column.DataType];

                var fieldName = this.AddField(targetTable, columnName, fieldType, true, length);

                outFieldDict[i] = targetTable.FindField(fieldName); ;
            }

            return outFieldDict;
        }

        /// <summary>
        /// 将源表的字段复制到目标要素类
        /// </summary>
        /// <param name="sourceTable">源表</param>
        /// <param name="targetTable">目标要素类</param>
        /// <returns></returns>
        public Dictionary<int, int> CopyFields(
            ITable sourceTable, ITable targetTable)
        {
            Dictionary<int, int> outFieldDict = new Dictionary<int, int>();
            IFields inFields = sourceTable.Fields;
            for (int i = 0; i < inFields.FieldCount; i++)
            {
                var field = inFields.Field[i];
                if (!field.Editable) continue;

                var fieldName = this.AddField(targetTable, field.Name, field.Type, true);

                outFieldDict[i] = targetTable.FindField(fieldName); ;
            }

            return outFieldDict;
        }

        /// <summary>
        /// 向要素类中添加要素，并添加要素几何属性与字段属性
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name="fieldNameList"></param>
        /// <param name="valueList"></param>
        public void AddFeature(
            IFeatureClass featureClass,
            List<string> fieldNameList,
            List<object> valueList)
        {
            if (fieldNameList.Count != valueList.Count)
                return;

            IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
            IFeatureCursor featureCursor = featureClass.Insert(true);
            for (int i = 0; i < fieldNameList.Count; i++)
            {
                string fieldName = fieldNameList[i];

                int index = featureClass.FindField(fieldName);
                //if (index == -1 || featureClass.Fields.Field[i].Editable == false)
                //    continue;
                try
                {
                    if (fieldName == featureClass.ShapeFieldName)
                    {
                        //var clone = (IClone)valueList[i];
                        //var copyGeometry = (IGeometry)clone.Clone();
                        var geometry = (IGeometry)valueList[i];
                        featureBuffer.Shape = geometry;
                    }
                    else
                        featureBuffer.set_Value(index, valueList[i]);
                }
                catch (Exception)
                {
                    continue;
                }
            }
            featureCursor.InsertFeature(featureBuffer);
            featureCursor.Flush();

            Marshal.ReleaseComObject(featureBuffer);
            Marshal.ReleaseComObject(featureCursor);
        }

        /// <summary>
        /// 向要素类中添加要素，新添加的要素只有几何属性
        /// </summary>
        /// <param name="featureClass"></param>
        /// <param name=""></param>
        public void AddFeatures(
            IFeatureClass featureClass,
            List<IGeometry> geometryList)
        {
            var geometryType = featureClass.ShapeType;

            IFeatureBuffer featureBuffer = featureClass.CreateFeatureBuffer();
            IFeatureCursor featureCursor = featureClass.Insert(true);

            for (int i = 0; i < geometryList.Count; i++)
            {
                if (geometryList[i].GeometryType != geometryType)
                    continue;

                var geometry = geometryList[i];
                featureBuffer.Shape = geometry;
                featureCursor.InsertFeature(featureBuffer);
            }
            featureCursor.Flush();

            Marshal.ReleaseComObject(featureBuffer);
            Marshal.ReleaseComObject(featureCursor);
        }

        /// <summary>
        /// 点要素类添加坐标XY值, 对应属性字段POINT_X与POINT_Y
        /// </summary>
        /// <param name="featureClass">点要素类</param>
        /// <param name="isGeographicCoordinateSystem">是否为地理坐标系的XY值</param>
        /// <param name="index_X">X值属性字段索引</param>
        /// <param name="index_Y">Y值属性字段索引</param>
        public void AddXYCoordinates(
            IFeatureClass featureClass,
            bool isGeographicCoordinateSystem)
        {

            if (featureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                return;

            IField fieldX = null;
            if (featureClass.FindField("POINT_X") < 0)
            {
                fieldX = new FieldClass();
                IFieldEdit fieldEdit = fieldX as IFieldEdit;
                fieldEdit.Name_2 = "POINT_X";
                fieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;
                fieldEdit.IsNullable_2 = true;
                featureClass.AddField(fieldX);
            }

            IField fieldY = null;
            if (featureClass.FindField("POINT_Y") < 0)
            {
                fieldY = new FieldClass();
                IFieldEdit fieldEdit = fieldY as IFieldEdit;
                fieldEdit.Name_2 = "POINT_Y";
                fieldEdit.Type_2 = esriFieldType.esriFieldTypeDouble;
                fieldEdit.IsNullable_2 = true;
                featureClass.AddField(fieldY);
            }

            if (fieldX != null) Marshal.ReleaseComObject(fieldX);
            if (fieldY != null) Marshal.ReleaseComObject(fieldY);

            if (featureClass.FindField("POINT_X") >= 0 && featureClass.FindField("POINT_Y") >= 0)
            {
                int index_X = featureClass.FindField("POINT_X");
                int index_Y = featureClass.FindField("POINT_Y");

                IGeoDataset geoDataset = (IGeoDataset)featureClass;
                IFeatureCursor featureCursor = featureClass.Update(null, false);
                IFeature feature = null;
                ISpatialReference spatialReference = geoDataset.SpatialReference;
                if (isGeographicCoordinateSystem && spatialReference is IProjectedCoordinateSystem)
                {
                    var projectedCoordinateSystem = (IProjectedCoordinateSystem)spatialReference;
                    WKSPoint wksPoint = new WKSPoint();
                    feature = featureCursor.NextFeature();
                    while (feature != null)
                    {
                        IPoint point = (IPoint)feature.Shape;

                        wksPoint.X = point.X;
                        wksPoint.Y = point.Y;
                        projectedCoordinateSystem.Inverse(1, ref wksPoint);
                        feature.set_Value(index_X, wksPoint.X);
                        feature.set_Value(index_Y, wksPoint.Y);

                        featureCursor.UpdateFeature(feature);
                        feature = featureCursor.NextFeature();
                    }

                    Marshal.ReleaseComObject(spatialReference);
                    Marshal.ReleaseComObject(wksPoint);
                }
                else
                {
                    feature = featureCursor.NextFeature();
                    while (feature != null)
                    {
                        IPoint point = (IPoint)feature.Shape;
                        feature.set_Value(index_X, point.X);
                        feature.set_Value(index_Y, point.Y);

                        featureCursor.UpdateFeature(feature);
                        feature = featureCursor.NextFeature();
                    }
                }

                Marshal.ReleaseComObject(featureCursor);
            }
        }

        /// <summary>
        /// 裁剪栅格数据
        /// </summary>
        /// <param name="rasterDataset">栅格数据</param>
        /// <param name="extent">输出范围</param>
        public IRasterDataset Clip(
            IRasterDataset rasterDataset,
            IEnvelope extent)
        {
            IClipFunctionArguments clipFArguments;
            IRasterFunction clipRasterFunction;
            IFunctionRasterDataset clipRasterDS;

            clipFArguments = new ClipFunctionArgumentsClass();
            clipFArguments.Raster = rasterDataset;
            clipFArguments.Extent = extent;
            clipFArguments.ClippingGeometry = extent;
            clipFArguments.ClippingType = esriRasterClippingType.esriRasterClippingOutside;

            clipRasterFunction = new ClipFunctionClass();
            clipRasterDS = new FunctionRasterDataset();
            clipRasterDS.Init(clipRasterFunction, clipFArguments);

            Marshal.ReleaseComObject(clipFArguments);
            Marshal.ReleaseComObject(clipRasterFunction);

            return (IRasterDataset)clipRasterDS;
        }

        /// <summary>
        /// 裁剪栅格数据
        /// </summary>
        /// <param name="rasterDataset">栅格数据</param>
        /// <param name="extent">输出范围</param>
        /// <param name="snapRasterDataset">象元捕捉栅格</param>
        /// <returns></returns>
        public IGeoDataset Clip(
            IRasterDataset rasterDataset,
            IEnvelope extent,
            IRasterDataset snapRasterDataset)
        {
            var geoDataset = (IGeoDataset)rasterDataset;

            ITransformationOp transformationOp = new RasterTransformationOpClass();

            // 设置环境变量
            var rasterAnaysisEnvironment = (IRasterAnalysisEnvironment)transformationOp;
            object extentProvider = extent;
            object snapRasterData = snapRasterDataset;
            rasterAnaysisEnvironment.SetExtent(
                esriRasterEnvSettingEnum.esriRasterEnvMaxOf, ref extentProvider, ref snapRasterData);
            rasterAnaysisEnvironment.OutSpatialReference = geoDataset.SpatialReference;

            // 执行裁剪
            var outGeoDataset = transformationOp.Clip(geoDataset, extent);

            Marshal.ReleaseComObject(transformationOp);

            return outGeoDataset;
        }


        public ITable CreateTable(
            string table_path,
            string table_name,
            IFields template_fields)
        {
            if (string.IsNullOrWhiteSpace(table_path) ||
               string.IsNullOrWhiteSpace(table_name))
                return null;

            string path_ext = System.IO.Path.GetExtension(table_path);

            IWorkspaceFactory workspaceFactory = null;
            if (path_ext == "")
                workspaceFactory = new ShapefileWorkspaceFactoryClass();
            else if (path_ext == ".gdb")
                workspaceFactory = new FileGDBWorkspaceFactoryClass();
            else if (path_ext == ".mdb")
                workspaceFactory = new AccessWorkspaceFactoryClass();
            else
                return null;

            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(table_path, 0);
            }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            var featureWorkspace = (IFeatureWorkspace)workspace;

            // 删掉存在的同名表
            ITable outTable = null;
            var workspace2 = (IWorkspace2)workspace;
            if (workspace2.get_NameExists(esriDatasetType.esriDTTable, table_name))
            {
                outTable = featureWorkspace.OpenTable(table_name);
                ((IDataset)outTable).Delete();
            }

            // 检查表明和路径是否合法
            IFieldChecker fieldChecker = new FieldCheckerClass();
            fieldChecker.InputWorkspace = workspace;
            fieldChecker.ValidateWorkspace = workspace;
            IFields inFields = template_fields;
            IEnumFieldError enumFieldError;
            IFields fixedFields;
            fieldChecker.Validate(inFields, out enumFieldError, out fixedFields);
            string fixedTableName;
            fieldChecker.ValidateTableName(table_name, out fixedTableName);
            outTable = featureWorkspace.CreateTable(fixedTableName, fixedFields, null, null, "");

            return outTable;
        }

        /// <summary>
        /// 在指定输出路径中的创建空要素类
        /// </summary>
        /// <param name="featurClass_path">要素类输出路径</param>
        /// <param name="featureClass_name">要素类名</param>
        /// <param name="featureDataset_name">要素数据集,当输出路径为Shapefile路径,该参数无效</param>
        /// <param name="geometryType">要素类型</param>
        /// <param name="spatialReference">要素类空间坐标系</param>
        public IFeatureClass CreateFeatureClass(
            string featurClass_path,
            string featureClass_name,
            string featureDataset_name,
            esriGeometryType geometryType,
            ISpatialReference spatialReference)
        {
            if (string.IsNullOrWhiteSpace(featurClass_path) ||
                string.IsNullOrWhiteSpace(featureClass_name))
                return null;

            string extension = System.IO.Path.GetExtension(featurClass_path);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            else
                return null;

            var workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(featurClass_path, 0);
            }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            var featureWorkspace = (IFeatureWorkspace)workspace;

            // 删掉存在的同名要素类
            IFeatureClass outFeatureClass = null;
            var workspace2 = (IWorkspace2)workspace;
            if (workspace2.get_NameExists(esriDatasetType.esriDTFeatureClass, featureClass_name))
            {
                outFeatureClass = featureWorkspace.OpenFeatureClass(featureClass_name);
                ((IDataset)outFeatureClass).Delete();
            }

            // 通过 FeatureClassDescription 创建数据必要字段信息
            // fcDescription 中包含了: Object/FID, Shape 字段, 无需自己写代码创建
            IFeatureClassDescription fcDescription = new FeatureClassDescriptionClass();
            IObjectClassDescription ocDescription = (IObjectClassDescription)fcDescription;
            IFields fields = ocDescription.RequiredFields;
            IFieldsEdit fieldsEdit = (IFieldsEdit)fields;

            // 设置 Shape 字段
            int shapeFieldIndex = fields.FindField(fcDescription.ShapeFieldName);
            IField field_Shape = fields.get_Field(shapeFieldIndex);
            IGeometryDef geometryDef = field_Shape.GeometryDef;
            IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
            geometryDefEdit.GeometryType_2 = geometryType;
            if (spatialReference != null)
                geometryDefEdit.SpatialReference_2 = spatialReference;

            // 创建要素类
            if (extension == "" || String.IsNullOrWhiteSpace(featureDataset_name))
            {
                outFeatureClass = featureWorkspace.CreateFeatureClass(
                    featureClass_name,
                    fields,
                    ocDescription.InstanceCLSID,
                    ocDescription.ClassExtensionCLSID,
                    esriFeatureType.esriFTSimple,
                    "Shape",
                    "");
            }
            else
            {   // 在Geodatabase的FeatureDataset建立要素类
                IFeatureDataset featureDataset = null;
                if (workspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featureDataset_name))
                    featureDataset = featureWorkspace.OpenFeatureDataset(featureDataset_name);
                else
                    featureDataset = featureWorkspace.CreateFeatureDataset(featureDataset_name, spatialReference);

                outFeatureClass = featureDataset.CreateFeatureClass(
                    featureClass_name,
                    fields,
                    ocDescription.InstanceCLSID,
                    ocDescription.ClassExtensionCLSID,
                    esriFeatureType.esriFTSimple,
                    "Shape",
                    "");

                Marshal.ReleaseComObject(featureDataset);
            }

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(fcDescription);
            Marshal.ReleaseComObject(fields);
            Marshal.ReleaseComObject(field_Shape);
            Marshal.ReleaseComObject(geometryDef);

            return outFeatureClass;
        }

        /// <summary>
        /// 在数据库中创建要素数据集
        /// </summary>
        /// <param name="geodatabase_file">数据库/全名(路径+名+.gdb/.mdb)</param>
        /// <param name="featuredataset_name">要素数据集名</param>
        /// <param name="spatialReference">空间参考系</param>
        public IFeatureDataset CreateFeatureDataset(
            string geodatabase_file,
            string featuredataset_name,
            ISpatialReference spatialReference)
        {
            string extension = System.IO.Path.GetExtension(geodatabase_file);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else
                return null;

            var workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(geodatabase_file, 0);
            }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            var featureWorkspace = (IFeatureWorkspace)workspace;

            IFeatureDataset featureDataset = null;
            IWorkspace2 workspace2 = (IWorkspace2)workspace;
            if (workspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featuredataset_name))
            {
                featureDataset = featureWorkspace.OpenFeatureDataset(featuredataset_name);
            }
            else
            {
                if (spatialReference == null)
                    spatialReference = new UnknownCoordinateSystemClass();
                featureDataset = featureWorkspace.CreateFeatureDataset(featuredataset_name, spatialReference);
            }

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);

            return featureDataset;
        }

        /// <summary>
        /// 创建.gdb格式的File Geodatabase数据库，若已经存在则打开
        /// </summary>
        /// <param name="geodatabase_path">数据库路径</param>
        /// <param name="geodatabase_name">数据库名(不含.gdb扩展名)</param>
        public IWorkspace CreateFileGDB(string geodatabase_path, string geodatabase_name)
        {
            IWorkspace workspace = null;
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            var workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            try
            {
                workspace = workspaceFactory.OpenFromFile(
                    geodatabase_path + "\\" + geodatabase_name + ".gdb", 0);
            }
            catch
            {
                IWorkspaceName workspaceName = workspaceFactory.Create(
                    geodatabase_path, geodatabase_name + ".gdb", null, 0);
                IName pName = (IName)workspaceName;
                workspace = (IWorkspace)pName.Open();

                Marshal.ReleaseComObject(workspaceName);
            }

            Marshal.ReleaseComObject(workspaceFactory);

            return workspace;
        }

        /// <summary>
        /// 创建.mdb格式的Personal Geodatabase数据库，若已经存在则打开
        /// </summary>
        /// <param name="geodatabase_path">数据库路径</param>
        /// <param name="geodatabase_name">数据库名(不含.mdb扩展名)</param>
        public IWorkspace CreatePersonalGDB(string geodatabase_path, string geodatabase_name)
        {
            IWorkspace workspace = null;
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            try
            {
                workspace = workspaceFactory.OpenFromFile(
                    geodatabase_path + "\\" + geodatabase_name + ".mdb", 0);
            }
            catch (Exception)
            {
                IWorkspaceName workspaceName = workspaceFactory.Create(
                    geodatabase_path, geodatabase_name + ".mdb", null, 0);
                IName pName = (IName)workspaceName;
                workspace = (IWorkspace)pName.Open();

                Marshal.ReleaseComObject(workspaceName);
            }

            Marshal.ReleaseComObject(workspaceFactory);

            return workspace;
        }

        /// <summary>
        /// 创建ArcInfo工作空间
        /// </summary>
        /// <param name="workspace_path">工作空间路径</param>
        /// <param name="workspace_name">工作空间名</param>
        /// <returns></returns>
        public IWorkspace CreateArcInforWorkspace(string workspace_path, string workspace_name)
        {
            IWorkspace workspace = null;
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ArcInfoWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);

            try
            {
                workspace = workspaceFactory.OpenFromFile(
                    workspace_path + "\\" + workspace_name, 0);
            }
            catch (Exception)
            {
                IWorkspaceName workspaceName = workspaceFactory.Create(
                    workspace_path, workspace_name, null, 0);
                IName pName = (IName)workspaceName;
                workspace = (IWorkspace)pName.Open();

                Marshal.ReleaseComObject(workspaceName);
            }

            Marshal.ReleaseComObject(workspaceFactory);

            return workspace;
        }

        /// <summary>
        /// 创建内存中的要素类
        /// </summary>
        /// <param name="memoryWorkspace_name">内存工作空间名字</param>
        /// <param name="featureClass_name">要素类名</param>
        /// <param name="geometryType">要素类型</param>
        /// <param name="spatialReference">要素空间参考系</param>
        /// <returns></returns>
        public IFeatureClass CreateMemoryFeatureClass(
            string memoryWorkspace_name,
            string featureClass_name,
            esriGeometryType geometryType,
            ISpatialReference spatialReference)
        {
            if (string.IsNullOrWhiteSpace(memoryWorkspace_name) ||
                string.IsNullOrWhiteSpace(featureClass_name))
                return null;

            IWorkspaceFactory memoryWorkspaceFactory = new InMemoryWorkspaceFactoryClass();
            IWorkspaceName memoryWorkspaceName = memoryWorkspaceFactory.Create(
                null, memoryWorkspace_name, null, 0);
            var memoryName = (IName)memoryWorkspaceName;
            var memoryWorkspace = (IWorkspace)memoryName.Open();



            // 通过 FeatureClassDescription 创建数据必要字段信息
            // fcDescription 中包含了: Object/FID, Shape 字段, 无需自己写代码创建
            IFeatureClassDescription fcDescription = new FeatureClassDescriptionClass();
            IObjectClassDescription ocDescription = (IObjectClassDescription)fcDescription;
            IFields fields = ocDescription.RequiredFields;
            IFieldsEdit fieldsEdit = (IFieldsEdit)fields;



            // 设置 Shape 字段
            int shapeFieldIndex = fields.FindField(fcDescription.ShapeFieldName);
            IField field_Shape = fields.get_Field(shapeFieldIndex);
            IGeometryDef geometryDef = field_Shape.GeometryDef;
            IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
            geometryDefEdit.GeometryType_2 = geometryType;
            if (spatialReference != null)
                geometryDefEdit.SpatialReference_2 = spatialReference;

            // 创建要素类
            var featureWorkspace = (IFeatureWorkspace)memoryWorkspace;
            var featureClass = featureWorkspace.CreateFeatureClass(
                featureClass_name,
                fields,
                ocDescription.InstanceCLSID,
                ocDescription.ClassExtensionCLSID,
                esriFeatureType.esriFTSimple,
                "Shape",
                "");

            Marshal.ReleaseComObject(memoryWorkspaceFactory);
            Marshal.ReleaseComObject(memoryWorkspaceName);
            Marshal.ReleaseComObject(memoryWorkspace);
            Marshal.ReleaseComObject(fcDescription);
            Marshal.ReleaseComObject(fields);
            Marshal.ReleaseComObject(field_Shape);
            Marshal.ReleaseComObject(geometryDef);

            return featureClass;
        }

        /// <summary>
        /// 创建内存工作空间
        /// 内存工作空间不支持以下功能：
        /// FeatureDataset, Topology, Network Datasets, GeometricNetworks, 
        /// Terrains, Representations, Locators, Raster Catalogs, Raster Datasets,
        /// Annotation/Dimension FeatureClass, 
        /// </summary>
        /// <param name="workspace_name">工作空间名</param>
        /// <returns></returns>
        public IWorkspace CreateMemoryWorkspace(string workspace_name)
        {
            // Feature datasets cannot be created within an InMemory workspace 
            // Subtypes, Domains and Relationship Classes are not supported
            // Advanced datasets such as Topology, Geometric Networks, Terrains, Representations, Locators, Cadastral Fabrics and Network Datasets are not supported
            // Raster Catalogs and Raster Datasets are not supported within InMemory workspaces
            // Annotation and Dimension feature classes cannot be used within an InMemory Workspace
            // Custom feature classes, such as those with a class extension are not supported

            IWorkspaceFactory memoryWorkspaceFactory = new InMemoryWorkspaceFactoryClass();
            IWorkspaceName memoryWorkspaceName = memoryWorkspaceFactory.Create(null, workspace_name, null, 0);
            IName memoryName = (IName)memoryWorkspaceName;
            IWorkspace memoryWorkspace = (IWorkspace)memoryName.Open(); // 打开内存工作空间

            Marshal.ReleaseComObject(memoryWorkspaceFactory);
            Marshal.ReleaseComObject(memoryWorkspaceName);

            return memoryWorkspace;
        }

        /// <summary>
        /// 创建栅格数据集的8向邻域网
        /// </summary>
        /// <param name="rasterGeoDataset">栅格数据集</param>
        /// <param name="featureClass_path">邻域网要素类所在路径</param>
        /// <param name="featureClass_name">邻域网要素类名</param>
        /// <param name="featureDataset_name">若所在路径为空间数据库,其要素数据集名</param>
        /// <param name="net_extent">邻域网覆盖范围</param>
        /// <param name="noDataValue">noData像元的值</param>
        public IFeatureClass CreateNeighborhoodNet(
            IGeoDataset rasterGeoDataset,
            string featureClass_path,
            string featureClass_name,
            string featureDataset_name,
            IEnvelope net_extent,
            object noDataValue)
        {
            if (rasterGeoDataset == null ||
                string.IsNullOrWhiteSpace(featureClass_path) ||
                string.IsNullOrWhiteSpace(featureClass_name))
                return null;

            var spatialReference = rasterGeoDataset.SpatialReference;

            // 1 创建要素类 并 添加字段
            IFeatureClass netFeatureClass = this.CreateFeatureClass(
                featureClass_path, featureClass_name, featureDataset_name, esriGeometryType.esriGeometryPolyline, spatialReference);

            this.AddField((ITable)netFeatureClass, "FROM_VALUE", esriFieldType.esriFieldTypeDouble, true);
            this.AddField((ITable)netFeatureClass, "TO_VALUE", esriFieldType.esriFieldTypeDouble, true);
            this.AddField((ITable)netFeatureClass, "ALIGNMENT", esriFieldType.esriFieldTypeString, true);

            // 2 提取out_extent范围的rasterGeoDataset为newRasterGeoDataset
            if (net_extent == null)
                net_extent = rasterGeoDataset.Extent;
            object extent = net_extent;
            object snap_raster = rasterGeoDataset;
            IExtractionOp extractionOp = new RasterExtractionOpClass();
            var rasterAnaysisEnvironment = (IRasterAnalysisEnvironment)extractionOp;
            rasterAnaysisEnvironment.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, ref extent, ref snap_raster);
            rasterAnaysisEnvironment.OutSpatialReference = rasterGeoDataset.SpatialReference;
            var tmpGeoDataset = extractionOp.Rectangle(rasterGeoDataset, net_extent, true);

            // 3 设置 tmpRasterGeoDataset 的空值
            var tmpRasterBands = (IRasterBandCollection)tmpGeoDataset;
            var tmpRasterDataset = tmpRasterBands.Item(0).RasterDataset;
            var tmpRaster2 = (IRaster2)tmpRasterDataset.CreateDefaultRaster();
            var tmpRasterProps = (IRasterProps)tmpRaster2;
            if (noDataValue != null)
                tmpRasterProps.NoDataValue = noDataValue;

            // 4 获取像素中心点坐标，排除空值像素，向netFeatureClass中写入8向邻域网
            int numRow = tmpRasterProps.Height;
            int numColumn = tmpRasterProps.Width;

            IFeatureBuffer featureBuffer = null;
            IFeatureCursor featureCursor = netFeatureClass.Insert(true);
            IPoint point;
            IPoint point2;
            IPolyline polyline;
            IPointCollection point_collection;
            object missing = Type.Missing;
            int fromGridIndex = netFeatureClass.FindField("FROM_VALUE");
            int toGridIndex = netFeatureClass.FindField("TO_VALUE");
            int aligmentIndex = netFeatureClass.FindField("ALIGNMENT");
            for (int i = 0; i < numRow; i++)
            {
                for (int j = 0; j < numColumn; j++)
                {
                    if (tmpRaster2.GetPixelValue(0, j, i) == null)
                        continue;

                    // 水平线 (i,j)----(i,j+1) 
                    if (j < numColumn - 1)
                    {
                        if (tmpRaster2.GetPixelValue(0, j + 1, i) == null)
                            continue;

                        point = new PointClass();
                        point2 = new PointClass();
                        polyline = new PolylineClass();
                        point_collection = polyline as IPointCollection;
                        point.PutCoords(tmpRaster2.ToMapX(j), tmpRaster2.ToMapY(i));
                        point2.PutCoords(tmpRaster2.ToMapX(j + 1), tmpRaster2.ToMapY(i));

                        point_collection.AddPoint(point, missing, missing);
                        point_collection.AddPoint(point2, missing, missing);

                        featureBuffer = netFeatureClass.CreateFeatureBuffer();
                        featureBuffer.Shape = polyline;
                        double fromPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j, i));
                        double toPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j + 1, i));
                        string alignment = "Horizontal";
                        featureBuffer.Value[fromGridIndex] = fromPixelValue;
                        featureBuffer.Value[toGridIndex] = toPixelValue;
                        featureBuffer.Value[aligmentIndex] = alignment;
                        featureCursor.InsertFeature(featureBuffer);
                    }

                    // 垂直线 (i,j)
                    //         |
                    //        (i+1,j)
                    if (i < numRow - 1)
                    {
                        if (tmpRaster2.GetPixelValue(0, j, i + 1) == null)
                            continue;

                        point = new PointClass();
                        point2 = new PointClass();
                        polyline = new PolylineClass();
                        point_collection = polyline as IPointCollection;
                        point.PutCoords(tmpRaster2.ToMapX(j), tmpRaster2.ToMapY(i));
                        point2.PutCoords(tmpRaster2.ToMapX(j), tmpRaster2.ToMapY(i + 1));

                        point_collection.AddPoint(point, missing, missing);
                        point_collection.AddPoint(point2, missing, missing);

                        featureBuffer = netFeatureClass.CreateFeatureBuffer();
                        featureBuffer.Shape = polyline;
                        double fromPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j, i));
                        double toPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j, i + 1));
                        string alignment = "Vertical";
                        featureBuffer.Value[fromGridIndex] = fromPixelValue;
                        featureBuffer.Value[toGridIndex] = toPixelValue;
                        featureBuffer.Value[aligmentIndex] = alignment;
                        featureCursor.InsertFeature(featureBuffer);
                    }

                    // 右斜线 .(i,j)
                    //        \
                    //         .(i+1, j+1)
                    if (i < numRow - 1 && j < numColumn - 1)
                    {
                        if (tmpRaster2.GetPixelValue(0, j + 1, i + 1) == null)
                            continue;

                        point = new PointClass();
                        point2 = new PointClass();
                        polyline = new PolylineClass();
                        point_collection = polyline as IPointCollection;
                        point.PutCoords(tmpRaster2.ToMapX(j), tmpRaster2.ToMapY(i));
                        point2.PutCoords(tmpRaster2.ToMapX(j + 1), tmpRaster2.ToMapY(i + 1));

                        point_collection.AddPoint(point, missing, missing);
                        point_collection.AddPoint(point2, missing, missing);

                        featureBuffer = netFeatureClass.CreateFeatureBuffer();
                        featureBuffer.Shape = polyline;
                        double fromPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j, i));
                        double toPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j + 1, i + 1));
                        string alignment = "BackwardSlash";
                        featureBuffer.Value[fromGridIndex] = fromPixelValue;
                        featureBuffer.Value[toGridIndex] = toPixelValue;
                        featureBuffer.Value[aligmentIndex] = alignment;
                        featureCursor.InsertFeature(featureBuffer);
                    }

                    // 左斜线 .(i,j)
                    //       /
                    //      .(i+1, j-1)
                    if (i < numRow - 1 && j > 0)
                    {
                        if (tmpRaster2.GetPixelValue(0, j - 1, i + 1) == null)
                            continue;

                        point = new PointClass();
                        point2 = new PointClass();
                        polyline = new PolylineClass();
                        point_collection = polyline as IPointCollection;
                        point.PutCoords(tmpRaster2.ToMapX(j), tmpRaster2.ToMapY(i));
                        point2.PutCoords(tmpRaster2.ToMapX(j - 1), tmpRaster2.ToMapY(i + 1));

                        point_collection.AddPoint(point, missing, missing);
                        point_collection.AddPoint(point2, missing, missing);

                        featureBuffer = netFeatureClass.CreateFeatureBuffer();
                        featureBuffer.Shape = polyline;
                        double fromPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j, i));
                        double toPixelValue = Convert.ToDouble(tmpRaster2.GetPixelValue(0, j - 1, i + 1));
                        string alignment = "ForwardSlash";
                        featureBuffer.Value[fromGridIndex] = fromPixelValue;
                        featureBuffer.Value[toGridIndex] = toPixelValue;
                        featureBuffer.Value[aligmentIndex] = alignment;
                        featureCursor.InsertFeature(featureBuffer);
                    }
                }

                if ((i % 1000) == 0) // 每1000个数据将缓存写入一次数据库
                    featureCursor.Flush();
            }
            featureCursor.Flush();

            Marshal.ReleaseComObject(extractionOp);
            Marshal.ReleaseComObject(tmpGeoDataset);
            Marshal.ReleaseComObject(tmpRasterDataset);
            Marshal.ReleaseComObject(tmpRaster2);
            Marshal.ReleaseComObject(featureCursor);
            return netFeatureClass;
        }

        /// <summary>
        /// 创建栅格要素集
        /// </summary>
        /// <param name="geodatabase_file">Geodatabase全名(路径+名+.gdb/.mdb)</param>
        /// <param name="raster_name">栅格要素集名</param>
        /// <param name="cellsize">像元尺寸</param>
        /// <param name="pixelType">像元类型</param>
        /// <param name="spatialReference">空间参考系</param>
        /// <param name="numberOfBand">波段数</param>
        /// <returns></returns>
        public IRasterDataset CreateRasterDataset(
            string geodatabase_file,
            string raster_name,
            double cellsize,
            rstPixelType pixelType,
            ISpatialReference spatialReference,
            int numberOfBand,
            int width,
            int height,
            double lowerLeftX,
            double lowerLeftY,
            double noDataValue)
        {
            string extension = System.IO.Path.GetExtension(geodatabase_file);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else
                return null;

            var workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            { workspace = workspaceFactory.OpenFromFile(geodatabase_file, 0); }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            var workspace2 = (IWorkspace2)workspace;
            var rasterWorkspace2 = (IRasterWorkspace2)workspace;
            var rasterWorkspaceEx = (IRasterWorkspaceEx)rasterWorkspace2;
            bool is_exist = workspace2.get_NameExists(esriDatasetType.esriDTRasterDataset, raster_name);
            if (is_exist) // 已经同名栅格文件, 则删除
                rasterWorkspaceEx.DeleteRasterDataset(raster_name);

            if (spatialReference == null)
                spatialReference = new UnknownCoordinateSystemClass();

            IPoint origin = new PointClass();
            origin.PutCoords(lowerLeftX, lowerLeftY);

            IRasterDataset rasterDataset = rasterWorkspace2.CreateRasterDataset(
                raster_name,
                "GRID",
                origin,
                width,
                height,
                cellsize,
                cellsize,
                numberOfBand,
                pixelType,
                spatialReference,
                true);

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);

            return rasterDataset;
        }

        /// <summary>
        /// 创建栅格要素集
        /// </summary>
        /// <param name="raster_path">栅格要素集路径</param>
        /// <param name="raster_name">栅格文件名称</param>
        /// <param name="extension">栅格文件扩展名(.tif/.image/.jpg/.jp2/.bmp/.png/.gif)</param>
        /// <param name="cellsize">像元尺寸</param>
        /// <param name="pixelType">像元类型</param>
        /// <param name="spatialReference">空间参考系</param>
        /// <param name="numberOfBand">波段数</param>
        /// <param name="width">栅格要素集宽</param>
        /// <param name="height">栅格要素集高</param>
        /// <param name="lowerLeftX">栅格要素集左下角坐标X</param>
        /// <param name="lowerLeftY">栅格要素集左下角坐标Y</param>
        /// <param name="noDataValue">空值</param>
        /// <returns></returns>
        public IRasterDataset CreateRasterDataset(
            string raster_path,
            string raster_name,
            string extension,
            double cellsize,
            rstPixelType pixelType,
            ISpatialReference spatialReference,
            int numberOfBand,
            int width,
            int height,
            double lowerLeftX,
            double lowerLeftY,
            double noDataValue)
        {

            if (System.IO.Path.GetExtension(raster_path) != "")
                return null;

            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesRaster.RasterWorkspaceFactory");
            var workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(raster_path, 0);
            }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            var rasterWorkspace2 = (IRasterWorkspace2)workspace;
            if (spatialReference == null)
                spatialReference = new UnknownCoordinateSystemClass();

            IPoint origin = new PointClass();
            origin.PutCoords(lowerLeftX, lowerLeftY);

            string formate = "TIFF";
            if (extension == ".image")
                formate = "IMAGINE";
            else if (extension == ".jpg")
                formate = "JPG";
            else if (extension == ".jp2")
                formate = "JP2";
            else if (extension == ".bmp")
                formate = "BMP";
            else if (extension == ".png")
                formate = "PNG";
            else if (extension == ".gif")
                formate = "GIF";

            IRasterDataset rasterDataset = rasterWorkspace2.CreateRasterDataset(
                raster_name, formate,
                origin,
                width,
                height,
                cellsize,
                cellsize,
                numberOfBand,
                pixelType,
                spatialReference,
                true);

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);

            return rasterDataset;
        }


        /// <summary>
        /// 用于未知空间参考坐标系的数据集赋予空间参考坐标系，且会覆盖已存在的空间参考系
        /// </summary>
        /// <param name="geoDataset">要素类/栅格数据集</param>
        /// <param name="spatialReference">空间参考坐标系</param>
        /// <param name="is_succeed"></param>
        public void DefineProjection(
            IGeoDataset geoDataset,
            ISpatialReference spatialReference,
            out bool is_succeed)
        {
            is_succeed = false;
            if (spatialReference == null)
                spatialReference = new UnknownCoordinateSystemClass();

            IGeoDatasetSchemaEdit geoDatasetSchemaEidt = (IGeoDatasetSchemaEdit)geoDataset;
            if (geoDatasetSchemaEidt.CanAlterSpatialReference == true)
            {
                geoDatasetSchemaEidt.AlterSpatialReference(spatialReference);
                is_succeed = true;
            }
        }

        /// <summary>
        /// 删除要素类中的要素
        /// </summary>
        /// <param name="featureClass">要素类</param>
        /// <param name="queryFilter">要素选择器</param>
        public void DeleteFeatures(IFeatureClass featureClass, IQueryFilter queryFilter)
        {
            ITable pTable = featureClass as ITable;
            pTable.DeleteSearchedRows(queryFilter);
        }

        /// <summary>
        /// 输出要素类的折点/指定点
        /// </summary>
        /// <param name="inFeatureClass">输入要素类</param>
        /// <param name="outFeatureClass_path">输出路径</param>
        /// <param name="outFeatureClass_name">输出要素类名</param>
        /// <param name="featureDataset_name">输出路径为Geodatabase时, 要素数据集名</param>
        /// <param name="point_type">输出点类型(ALL, BOTH_ENDS, START, END, MID)</param>
        /// <returns></returns>
        public IFeatureClass FeatureVerticesToPoints(
            IFeatureClass inFeatureClass,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name,
            ParamPointType point_type) // ALL, MID, START, END, BOTH_ENDS
        {
            if (inFeatureClass == null ||
                string.IsNullOrWhiteSpace(outFeatureClass_path) ||
                string.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            if (inFeatureClass.ShapeType != esriGeometryType.esriGeometryPolyline &&
                inFeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                return null;

            var geoDataset = (IGeoDataset)inFeatureClass;
            var spatialRefernce = geoDataset.SpatialReference;

            // 建立输出要素类
            var outFeatureClass = this.CreateFeatureClass(
                   outFeatureClass_path, outFeatureClass_name, featureDataset_name, esriGeometryType.esriGeometryPoint, spatialRefernce);
            if (outFeatureClass == null) return null;

            var inFields = inFeatureClass.Fields;
            for (int i = 0; i < inFields.FieldCount; i++)
            {
                var field = inFields.Field[i];
                if (field.Editable == true)
                    this.AddField((ITable)outFeatureClass, field.Name, field.Type, true);
            }
            this.AddField(
                (ITable)outFeatureClass, "ORIG_" + inFeatureClass.OIDFieldName, esriFieldType.esriFieldTypeInteger, true);

            var outFeatureCursor = outFeatureClass.Insert(true);
            var outFeatureBuffer = outFeatureClass.CreateFeatureBuffer();
            var outFieldCount = outFeatureClass.Fields.FieldCount;
            var inFeatureCursor = inFeatureClass.Search(null, true);
            var inFeature = inFeatureCursor.NextFeature();
            while (inFeature != null)
            {
                var geometry = inFeature.ShapeCopy;

                if (point_type == ParamPointType.ALL)
                {
                    var pointCollection = (IPointCollection)geometry;
                    for (int pointIndex = 0; pointIndex < pointCollection.PointCount; pointIndex++)
                    {
                        for (int inFieldIndex = 0; inFieldIndex < inFields.FieldCount; inFieldIndex++) // 添加字段属性值
                        {
                            var inField = inFields.Field[inFieldIndex];
                            if (inField.Editable == true && inField.Name != inFeatureClass.ShapeFieldName)
                                outFeatureBuffer.set_Value(inFieldIndex, inFeature.Value[inFieldIndex]);
                        }
                        outFeatureBuffer.set_Value(outFieldCount - 1, inFeature.Value[0]); // 为ORIG_FID字段添加属性值
                        outFeatureBuffer.Shape = pointCollection.Point[pointIndex];
                        outFeatureCursor.InsertFeature(outFeatureBuffer);
                    }
                }
                else if (point_type == ParamPointType.BOTH_ENDS)
                {
                    List<IPoint> pointList = new List<IPoint>();
                    if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        pointList.Add(((IPolyline5)geometry).FromPoint);
                        pointList.Add(((IPolyline5)geometry).ToPoint);
                    }
                    else if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        pointList.Add(((IPolygon)geometry).FromPoint);
                        pointList.Add(((IPolygon)geometry).ToPoint);
                    }

                    pointList.ForEach(point =>
                    {
                        for (int inFieldIndex = 0; inFieldIndex < inFields.FieldCount; inFieldIndex++) // 添加字段属性值
                        {
                            var inField = inFields.Field[inFieldIndex];
                            if (inField.Editable == true && inField.Name != inFeatureClass.ShapeFieldName)
                                outFeatureBuffer.set_Value(inFieldIndex, inFeature.Value[inFieldIndex]);
                        }
                        outFeatureBuffer.set_Value(outFieldCount - 1, inFeature.Value[0]); // 为ORIG_FID字段添加属性值
                        outFeatureBuffer.Shape = point;
                        outFeatureCursor.InsertFeature(outFeatureBuffer);
                    });
                }
                else if (point_type == ParamPointType.START)
                {
                    IPoint fromPoint = null;
                    if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                        fromPoint = ((IPolyline5)geometry).FromPoint;
                    else if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                        fromPoint = ((IPolygon)geometry).FromPoint;

                    for (int inFieldIndex = 0; inFieldIndex < inFields.FieldCount; inFieldIndex++) // 添加字段属性值
                    {
                        var inField = inFields.Field[inFieldIndex];
                        if (inField.Editable == true && inField.Name != inFeatureClass.ShapeFieldName)
                            outFeatureBuffer.set_Value(inFieldIndex, inFeature.Value[inFieldIndex]);
                    }
                    outFeatureBuffer.set_Value(outFieldCount - 1, inFeature.Value[0]); // 为ORIG_FID字段添加属性值
                    outFeatureBuffer.Shape = fromPoint;
                    outFeatureCursor.InsertFeature(outFeatureBuffer);
                }
                else if (point_type == ParamPointType.END)
                {
                    IPoint toPoint = null;
                    if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                        toPoint = ((IPolyline5)geometry).FromPoint;
                    else if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                        toPoint = ((IPolygon)geometry).FromPoint;

                    for (int inFieldIndex = 0; inFieldIndex < inFields.FieldCount; inFieldIndex++) // 添加字段属性值
                    {
                        var inField = inFields.Field[inFieldIndex];
                        if (inField.Editable == true && inField.Name != inFeatureClass.ShapeFieldName)
                            outFeatureBuffer.set_Value(inFieldIndex, inFeature.Value[inFieldIndex]);
                    }
                    outFeatureBuffer.set_Value(outFieldCount - 1, inFeature.Value[0]); // 为ORIG_FID字段添加属性值
                    outFeatureBuffer.Shape = toPoint;
                    outFeatureCursor.InsertFeature(outFeatureBuffer);
                }
                else if (point_type == ParamPointType.MID)
                {
                    IPoint midPoint = new PointClass();
                    if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolyline)
                    {
                        var polyline = (IPolyline5)geometry;
                        polyline.QueryPoint(esriSegmentExtension.esriNoExtension, polyline.Length / 2, false, midPoint);
                    }
                    else if (inFeatureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        var polygon = (IPolygon4)geometry;
                        polygon.QueryPoint(esriSegmentExtension.esriNoExtension, polygon.Length / 2, false, midPoint);
                    }

                    for (int inFieldIndex = 0; inFieldIndex < inFields.FieldCount; inFieldIndex++) // 添加字段属性值
                    {
                        var inField = inFields.Field[inFieldIndex];
                        if (inField.Editable == true && inField.Name != inFeatureClass.ShapeFieldName)
                            outFeatureBuffer.set_Value(inFieldIndex, inFeature.Value[inFieldIndex]);
                    }
                    outFeatureBuffer.set_Value(outFieldCount - 1, inFeature.Value[0]); // 为ORIG_FID字段添加属性值
                    outFeatureBuffer.Shape = midPoint;
                    outFeatureCursor.InsertFeature(outFeatureBuffer);
                }
                else
                    return null;

                outFeatureCursor.Flush();
            }

            Marshal.ReleaseComObject(inFeatureCursor);
            Marshal.ReleaseComObject(outFeatureCursor);
            Marshal.ReleaseComObject(outFeatureBuffer);
            Marshal.ReleaseComObject(spatialRefernce);

            return outFeatureClass;
        }



        /// <summary>
        /// 将相同类型的多个要素类合并为一个新的要素类
        /// </summary>
        /// <param name="inFeatureClassList">输入要素类集合</param>
        /// <param name="outFeatureClass_path">新要素类输出路径</param>
        /// <param name="outFeatureClass_name">新要素类名</param>
        /// <param name="featureDataset_name">若输出路径为Geodatabase, 其中要素数据集名</param>
        /// <returns></returns>
        public IFeatureClass Merge(
            List<IFeatureClass> inFeatureClassList,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name)
        {
            if (inFeatureClassList == null ||
                string.IsNullOrWhiteSpace(outFeatureClass_path) ||
                string.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            int inFeatureClassCount = inFeatureClassList.Count;
            esriGeometryType geometryType = inFeatureClassList[0].ShapeType;

            var geoDataset = (IGeoDataset)inFeatureClassList[0];
            var spatialReference = geoDataset.SpatialReference;
            IClone comparison = (IClone)spatialReference;
            for (int i = 1; i < inFeatureClassCount; i++)
            {
                // 比较要素类数据类型
                if (inFeatureClassList[i].ShapeType != geometryType)
                    return null;

                // 比较要素类投影
                var tmpGeoDataset = (IGeoDataset)inFeatureClassList[i];
                var tmpSpatialReference = tmpGeoDataset.SpatialReference;
                if (!comparison.IsEqual((IClone)tmpSpatialReference))
                    return null;
            }

            // 创建输出的要素类
            var outFeatureClass = this.CreateFeatureClass(
                outFeatureClass_path, outFeatureClass_name, featureDataset_name, geometryType, spatialReference);
            if (outFeatureClass == null) return null;

            // 输出的要素类添加字段
            for (int i = 0; i < inFeatureClassCount; i++)
            {
                var inFeatureClass = inFeatureClassList[i];
                for (int j = 0; j < inFeatureClass.Fields.FieldCount; j++)
                {
                    var field = inFeatureClass.Fields.get_Field(j);
                    if (field.Editable == true)
                        this.AddField((ITable)outFeatureClass, field.Name, field.Type, true);
                }
            }

            IFeatureCursor outFeatureCursor = outFeatureClass.Insert(true);
            IFeatureBuffer outFeatureBuffer = outFeatureClass.CreateFeatureBuffer();

            // 向输出的要素类添加要素
            for (int i = 0; i < inFeatureClassCount; i++)
            {
                var inFeatureClass = inFeatureClassList[i];
                var inFeatureCursor = inFeatureClass.Search(null, false);
                var inFeature = inFeatureCursor.NextFeature();
                while (inFeature != null)
                {
                    IField inField = null;
                    IField outField = null;
                    for (int inFieldIndex = 0; inFieldIndex < inFeature.Fields.FieldCount; inFieldIndex++)
                    {
                        inField = inFeature.Fields.get_Field(inFieldIndex);
                        int outFieldIndex = outFeatureClass.FindField(inField.Name);
                        if (outFieldIndex == -1) continue;

                        outField = outFeatureClass.Fields.Field[outFieldIndex];
                        if (outField.Type == inField.Type && outField.Editable == true)
                        {
                            object value = inFeature.get_Value(inFieldIndex);
                            if (outField.IsNullable == false && value.ToString() == "")
                                continue;
                            outFeatureBuffer.set_Value(outFieldIndex, value);
                        }
                    }

                    outFeatureBuffer.Shape = inFeature.ShapeCopy;
                    outFeatureCursor.InsertFeature(outFeatureBuffer);

                    inFeature = inFeatureCursor.NextFeature();
                } // while (inFeature != null)
                outFeatureCursor.Flush();
                Marshal.ReleaseComObject(inFeatureCursor);
            } //for (int i = 0; i < inFeatureClassCount; i++)

            Marshal.ReleaseComObject(outFeatureCursor);
            Marshal.ReleaseComObject(outFeatureBuffer);
            Marshal.ReleaseComObject(spatialReference);

            return outFeatureClass;
        }

        /// <summary>
        /// 多栅格数据集拼接成一个新的栅格数据集
        /// </summary>
        /// <param name="rasterGeoDatasetList"></param>
        /// <param name="mosaicOperatorType"></param>
        /// <returns></returns>
        public IGeoDataset MosaicToNewRaster(
            List<IGeoDataset> rasterGeoDatasetList,
            rstMosaicOperatorType mosaicOperatorType)
        {
            IRaster tmpRaster = new RasterClass();
            var tmpRasterBandCollection = (IRasterBandCollection)tmpRaster;

            foreach (var rasterGeoDataset in rasterGeoDatasetList)
            {
                var rasterBandCollection = (IRasterBandCollection)rasterGeoDataset;
                tmpRasterBandCollection.AppendBands(rasterBandCollection);
            }

            var rasterTransformationOp = new RasterTransformationOp();
            var transformationOp = (ITransformationOp)rasterTransformationOp;
            var mosaicGeoDataset = transformationOp.Mosaic(
                tmpRasterBandCollection,
                mosaicOperatorType);

            Marshal.ReleaseComObject(tmpRaster);
            Marshal.ReleaseComObject(rasterTransformationOp);

            return mosaicGeoDataset;
        }

        /// <summary>
        /// 打开表,支持.gdb/.mdb/Excel工作簿中的工作表/.csv/.txt格式的表
        /// </summary>
        /// <param name="table_path">表所在路径（excel为文件名)</param>
        /// <param name="table_name">表名(excel为工作簿名+$)</param>
        /// <returns></returns>
        public ITable OpenTable(
            string table_path,
            string table_name)
        {
            string path_ext = System.IO.Path.GetExtension(table_path);
            string table_ext = System.IO.Path.GetExtension(table_name);

            IWorkspaceFactory workspaceFactory;
            if (path_ext == ".gdb")
            { workspaceFactory = new FileGDBWorkspaceFactory(); }
            else if (path_ext == ".mdb")
            { workspaceFactory = new AccessWorkspaceFactory(); }
            else if ((path_ext == ".xlsx" || path_ext == ".xls") && table_ext == "" && table_name.Contains("$"))
            { workspaceFactory = new ExcelWorkspaceFactory(); }
            else if (path_ext == "" && (table_ext == ".csv" || table_ext == ".txt") && File.Exists(table_path + "\\" + table_name))
            { workspaceFactory = new TextFileWorkspaceFactoryClass(); }
            else if (path_ext == "" && table_ext == ".dbf" && File.Exists(table_path + "\\" + table_name))
            {
                workspaceFactory = new ShapefileWorkspaceFactory();
                table_name = System.IO.Path.GetFileNameWithoutExtension(table_name);
            }
            else if (path_ext == "")
            { workspaceFactory = new ArcInfoWorkspaceFactory(); }
            else
                return null;

            IWorkspace workspace = null;
            try
            { workspace = workspaceFactory.OpenFromFile(table_path, 0); }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            ITable table = null;
            IEnumDataset enumDataset = workspace.get_Datasets(esriDatasetType.esriDTTable);
            List<string> datasetNameList = new List<string>();
            IDataset dataset = null;
            while ((dataset = enumDataset.Next()) != null)
            {
                if (dataset.Name == table_name)
                {
                    IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
                    table = featureWorkspace.OpenTable(table_name);

                    Marshal.ReleaseComObject(enumDataset);
                    break;
                }
            }

            Marshal.ReleaseComObject(enumDataset);

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);

            return table;
        }

        /// <summary>
        /// 获得Excel工作簿中的工作表
        /// </summary>
        /// <param name="excel_file">Excel工作簿文件全名(路径+名+.xlsx)</param>
        /// <param name="table_name">Excel工作表名(Sheet1)</param>
        /// <returns></returns>
        public ITable OpenExcelTable(string excel_file, string table_name)
        {
            ITable excelTable = null;
            Type factoryType = Type.GetTypeFromProgID("esriDataSourcesOleDB.ExcelWorkspaceFactory");
            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            //IWorkspaceFactory workspaceFactory = new ExcelWorkspaceFactoryClass();

            IWorkspace workspace = workspaceFactory.OpenFromFile(excel_file, 0);
            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            excelTable = featureWorkspace.OpenTable(table_name);

            return excelTable;
        }

        /// <summary>
        /// 获取UTF-8编码的CSV文件
        /// </summary>
        /// <param name="file_path">CSV文件路径</param>
        /// <param name="file_name">CSV文件名（含扩展名）</param>
        /// <returns></returns>
        private ITable OpenCSVUTF8File(string file_path, string file_name)
        {
            ITable csvTable = null;

            IWorkspaceFactory workspaceFactory = new OLEDBWorkspaceFactory();
            IPropertySet propSet = new PropertySet();
            //注意如果csv文件的字符编码是utf-8
            //propSet.SetProperty("CONNECTSTRING", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + csvPath + ";Extended Properties='Text;HDR=Yes;IMEX=1;CharacterSet=65001;'");
            propSet.SetProperty("CONNECTSTRING", "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + file_path + ";Extended Properties='Text;HDR=Yes;IMEX=1;'");
            IWorkspace workspace = workspaceFactory.Open(propSet, 0);

            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            csvTable = featureWorkspace.OpenTable(file_name);

            return csvTable;
        }

        /// <summary>
        /// 获得文本文件(*.csv, *.txt)的数据
        /// </summary>
        /// <param name="text_path">文本文件所在路径</param>
        /// <param name="text_name">文本文件名（含扩展名）</param>
        /// <returns></returns>
        public ITable OpenTextFileTable(string text_path, string text_name)
        {
            ITable textTable = null;
            //Type factoryType = Type.GetTypeFromProgID("esriDataSourcesOleDB.TextFileWorkspaceFactory");
            //IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspaceFactory workspaceFactory = new TextFileWorkspaceFactoryClass();

            IWorkspace workspace = workspaceFactory.OpenFromFile(text_path, 0);

            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            textTable = featureWorkspace.OpenTable(text_name);

            return textTable;
        }

        /// <summary>
        /// 从*.gdb/*.mdb/Shapefile工作空间中获得要素类
        /// </summary>
        /// <param name="featureClass_path">要素类所在路径</param>
        /// <param name="featureClass_name">要素类名</param>
        /// <returns></returns>
        public IFeatureClass OpenFeatureClass(
            string featureClass_path,
            string featureClass_name)
        {
            Type factoryType = null;
            string extension = System.IO.Path.GetExtension(featureClass_path);
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            else
                return null;

            var workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            { workspace = workspaceFactory.OpenFromFile(featureClass_path, 0); }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            IFeatureClass featureClass = null;
            var workspace2 = (IWorkspace2)workspace;
            if (workspace2.get_NameExists(esriDatasetType.esriDTFeatureClass, featureClass_name))
            {
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
                featureClass = featureWorkspace.OpenFeatureClass(featureClass_name);
            }

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);

            return featureClass;
        }

        /// <summary>
        /// 打开Geodatabase指定的FeatureDataset
        /// </summary>
        /// <param name="featureDataset_path">要素数据集所在的空间数据库全名(路径+名+.gdb/.mdb)</param>
        /// <param name="featureDataset_name">要素数据集名</param>
        /// <returns></returns>
        public IFeatureDataset OpenFeatureDataset(
            string featureDataset_path,
            string featureDataset_name)
        {
            if (string.IsNullOrWhiteSpace(featureDataset_path) ||
                string.IsNullOrWhiteSpace(featureDataset_name))
                return null;

            Type factoryType = null;
            string extension = System.IO.Path.GetExtension(featureDataset_path);
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else
                return null;

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            { workspace = workspaceFactory.OpenFromFile(featureDataset_path, 0); }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            IWorkspace2 workspace2 = (IWorkspace2)workspace;
            if (!workspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featureDataset_name))
            {
                Marshal.ReleaseComObject(workspaceFactory);
                Marshal.ReleaseComObject(workspace);
                return null;
            }

            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            IFeatureDataset featureDataset = featureWorkspace.OpenFeatureDataset(featureDataset_name);

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);

            return featureDataset;
        }

        /// <summary>
        /// 从工作空间中获得栅格数据集
        /// </summary>
        /// <param name="raster_path">工作空间</param>
        /// <param name="raster_name_with_ext">有扩展名的栅格数据集名</param>
        public IRasterDataset OpenRasterDataset(
            string raster_path,
            string raster_name_with_ext)
        {
            string extension = System.IO.Path.GetExtension(raster_path);

            Type factoryType = null;
            IWorkspaceFactory workspaceFactory = null;
            IWorkspace workspace = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesRaster.RasterWorkspaceFactory");
            else
                return null;

            workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            try
            { workspace = workspaceFactory.OpenFromFile(raster_path, 0); }
            catch (Exception)
            {
                if (workspaceFactory != null) Marshal.ReleaseComObject(workspaceFactory);
                if (workspace != null) Marshal.ReleaseComObject(workspace);
                return null;
            }

            IRasterDataset rasterDataset = null;
            if (workspace is IRasterWorkspaceEx)
            {
                var rasterWorkspaceEx = (IRasterWorkspaceEx)workspace;
                rasterDataset = rasterWorkspaceEx.OpenRasterDataset(raster_name_with_ext);
            }
            else if (workspace is IRasterWorkspace)
            {
                var rasterWorkspace = (IRasterWorkspace)workspace;
                rasterDataset = rasterWorkspace.OpenRasterDataset(raster_name_with_ext);
            }

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);

            return rasterDataset;
        }

        /// <summary>
        /// 根据路径类型, 打开工作空间
        /// </summary>
        /// <param name="workspace_file">工作空间</param>
        /// <returns></returns>
        public IWorkspace OpenWorkspace(string workspace_file)
        {
            // 判读 workspace_file 的扩展名, 打开工作空间
            string extension = System.IO.Path.GetExtension(workspace_file);
            Type factoryType = null;
            if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ArcInfoWorkspaceFactory");
            else if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else
                return null;

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;

            try { workspace = workspaceFactory.OpenFromFile(workspace_file, 0); }
            catch (Exception)
            { }

            Marshal.ReleaseComObject(workspaceFactory);

            return workspace;
        }

        /// <summary>
        /// 点要素依次连接成线要素
        /// </summary>
        /// <param name="inFeatureClass">创建线的点要素</param>
        /// <param name="outFeatureClass_path">线要素输出路径</param>
        /// <param name="outFeatureClass_name">线要素名</param>
        /// <param name="featureDataset_name">输出路径位空间数据库时,其要素数据集名</param>
        /// <param name="lineFieldName">判断点要素该字段的唯一值,符合该唯一值的点将连成线</param>
        /// <param name="sortFieldName">点要素升序排序字段,按照排好顺序的点连接成线</param>
        /// <param name="closeLine">是否为闭合线</param>
        /// <returns></returns>
        public IFeatureClass PointsToLine(
            IFeatureClass inFeatureClass,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name,
            string lineFieldName = null,
            string sortFieldName = null,
            bool closeLine = false)
        {
            if (inFeatureClass == null ||
                string.IsNullOrWhiteSpace(outFeatureClass_path) ||
                string.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            if (inFeatureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                return null;

            var inGeoDataset = (IGeoDataset)inFeatureClass;

            var outFeatureClass = this.CreateFeatureClass(
                outFeatureClass_path,
                outFeatureClass_name,
                featureDataset_name,
                esriGeometryType.esriGeometryPolyline,
                inGeoDataset.SpatialReference);
            if (outFeatureClass == null) return null;

            List<string> uniqueValueArray = new List<string>();
            IField lineField = null;
            int outLineFieldIndex = -1;
            if (!string.IsNullOrWhiteSpace(lineFieldName))
            {
                int inLineFieldIndex = inFeatureClass.FindField(lineFieldName);
                if (inLineFieldIndex >= 0 && lineFieldName != inFeatureClass.OIDFieldName)
                {
                    // 输出要素添加字段
                    lineField = inFeatureClass.Fields.Field[inLineFieldIndex];
                    this.AddField((ITable)outFeatureClass, lineFieldName, lineField.Type, true);
                    outLineFieldIndex = outFeatureClass.FindField(lineFieldName);

                    var inFeatureCursor = inFeatureClass.Search(null, true);
                    IDataStatistics dataStatistcs = new DataStatisticsClass();
                    dataStatistcs.Field = lineFieldName;
                    dataStatistcs.Cursor = (ICursor)inFeatureCursor;
                    IEnumerator enumUniqueValue = dataStatistcs.UniqueValues;
                    enumUniqueValue.Reset();

                    // 获取唯一值的查找语句
                    while (enumUniqueValue.MoveNext())
                    {
                        uniqueValueArray.Add(enumUniqueValue.Current.ToString());
                    }

                    Marshal.ReleaseComObject(inFeatureCursor);
                }
            }

            if (uniqueValueArray.Count == 0)
                uniqueValueArray.Add("");

            var outFeatureCurosr = outFeatureClass.Insert(true);
            var outFeatureBuffer = outFeatureClass.CreateFeatureBuffer();

            for (int valueIndex = 0; valueIndex < uniqueValueArray.Count; valueIndex++)
            {
                ITable inTable = (ITable)inFeatureClass;
                string whereClause = "";
                string uniqueValue = uniqueValueArray[valueIndex];
                if (lineField != null)
                {
                    whereClause = lineFieldName + " = " + uniqueValue;
                    if (lineField.Type == esriFieldType.esriFieldTypeString)
                        whereClause = lineFieldName + " = '" + uniqueValue + "'";

                    outFeatureBuffer.set_Value(outLineFieldIndex, uniqueValue);
                }

                IQueryFilter queryFilter = new QueryFilterClass();
                queryFilter.WhereClause = whereClause;

                int rowCount = inTable.RowCount(queryFilter);
                if (rowCount < 2) continue; // 不够2个点，不能连线

                // 按照字段排序
                ITableSort inTableSort = null;
                if (!String.IsNullOrWhiteSpace(sortFieldName) &&
                    sortFieldName != inFeatureClass.ShapeFieldName)
                {
                    int sortFieldIndex = inFeatureClass.FindField(sortFieldName);
                    if (sortFieldIndex >= 0)
                    {
                        inTableSort = new TableSortClass();
                        inTableSort.QueryFilter = queryFilter;
                        inTableSort.Table = inTable;
                        inTableSort.Fields = sortFieldName;
                        inTableSort.set_Ascending(sortFieldName, true);
                        inTableSort.Sort(null);
                    }
                }

                IPolyline polyline = new PolylineClass();
                IPointCollection pointCollection = (IPointCollection)polyline;

                ICursor inCursor = null;
                if (inTableSort == null)
                { inCursor = inTable.Search(queryFilter, true); } // 获取没有排序结果
                else
                { inCursor = inTableSort.Rows; } // 获取排序结果

                IRow inRow = inCursor.NextRow();
                var startPoint = (IPoint)((IFeature)inRow).ShapeCopy;
                while (inRow != null)
                {
                    IFeature inFeature = (IFeature)inRow;
                    var inPoint = (IPoint)inFeature.ShapeCopy;
                    pointCollection.AddPoint(inPoint);
                    inRow = inCursor.NextRow();
                }

                if (closeLine == true)
                {
                    pointCollection.AddPoint(startPoint);
                }

                outFeatureBuffer.Shape = polyline;
                outFeatureCurosr.InsertFeature(outFeatureBuffer);
            }

            outFeatureCurosr.Flush();

            Marshal.ReleaseComObject(outFeatureCurosr);
            Marshal.ReleaseComObject(outFeatureBuffer);

            return outFeatureClass;
        }

        /// <summary>
        /// 任意两个点要素连接成线要素
        /// </summary>
        /// <param name="inFeatureClass">创建线的点要素</param>
        /// <param name="outFeatureClass_path">线要素输出路径</param>
        /// <param name="outFeatureClass_name">线要素名</param>
        /// <param name="featureDataset_name">输出路径位空间数据库时,其要素数据集名</param>
        /// <param name="lineFieldName">判断点要素该字段的唯一值,符合该唯一值的点将连成线</param>
        /// <param name="sortFieldName">点要素升序排序字段,按照排好顺序的点连接成线</param>
        /// <param name="closeLine">是否为闭合线</param>
        /// <returns></returns>
        public IFeatureClass Point2ToLine(
            IFeatureClass inFeatureClass,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name,
            string lineFieldName = null,
            string sortFieldName = null)
        {
            if (inFeatureClass == null ||
                String.IsNullOrWhiteSpace(outFeatureClass_path) ||
                String.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            if (inFeatureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                return null;

            var inGeoDataset = (IGeoDataset)inFeatureClass;

            var outFeatureClass = this.CreateFeatureClass(
                outFeatureClass_path,
                outFeatureClass_name,
                featureDataset_name,
                esriGeometryType.esriGeometryPolyline,
                inGeoDataset.SpatialReference);
            if (outFeatureClass == null) return null;

            List<string> uniqueValueList = new List<string>();
            IField inLineField = null;
            int outLineFieldIndex = -1;
            if (!String.IsNullOrWhiteSpace(lineFieldName))
            {
                int inLineFieldIndex = inFeatureClass.FindField(lineFieldName);
                if (inLineFieldIndex >= 0 && lineFieldName != inFeatureClass.OIDFieldName)
                {
                    // 输出要素添加字段
                    inLineField = inFeatureClass.Fields.Field[inLineFieldIndex];
                    this.AddField((ITable)outFeatureClass, lineFieldName, inLineField.Type, true);
                    outLineFieldIndex = outFeatureClass.FindField(lineFieldName);

                    var inFeatureCursor = inFeatureClass.Search(null, true);
                    IDataStatistics dataStatistcs = new DataStatisticsClass();
                    dataStatistcs.Field = lineFieldName;
                    dataStatistcs.Cursor = (ICursor)inFeatureCursor;
                    IEnumerator enumUniqueValue = dataStatistcs.UniqueValues;
                    enumUniqueValue.Reset();

                    // 获取唯一值的查找语句
                    while (enumUniqueValue.MoveNext())
                    {
                        uniqueValueList.Add(enumUniqueValue.Current.ToString());
                    }

                    Marshal.ReleaseComObject(inFeatureCursor);
                }
            }

            if (uniqueValueList.Count == 0)
                uniqueValueList.Add("");

            this.AddField((ITable)outFeatureClass, "FromID", esriFieldType.esriFieldTypeInteger, true);
            this.AddField((ITable)outFeatureClass, "ToID", esriFieldType.esriFieldTypeInteger, true);
            int fromIDIndex = outFeatureClass.FindField("FromID");
            int toIDIndex = outFeatureClass.FindField("ToID");

            var outFeatureCurosr = outFeatureClass.Insert(true);
            var outFeatureBuffer = outFeatureClass.CreateFeatureBuffer();

            for (int valueIndex = 0; valueIndex < uniqueValueList.Count; valueIndex++)
            {
                ITable inTable = (ITable)inFeatureClass;
                string whereClause = "";
                string uniqueValue = uniqueValueList[valueIndex];
                if (inLineField != null)
                {
                    whereClause = lineFieldName + " = " + uniqueValue;
                    if (inLineField.Type == esriFieldType.esriFieldTypeString)
                        whereClause = lineFieldName + " = '" + uniqueValue + "'";

                    outFeatureBuffer.set_Value(outLineFieldIndex, uniqueValue);
                }

                IQueryFilter queryFilter = new QueryFilterClass();
                queryFilter.WhereClause = whereClause;

                int rowCount = inTable.RowCount(queryFilter);
                if (rowCount < 2) continue; // 不够2个点，不能连线

                // 按照字段排序
                ITableSort inTableSort = null;
                if (!String.IsNullOrWhiteSpace(sortFieldName) &&
                    sortFieldName != inFeatureClass.ShapeFieldName)
                {
                    int sortFieldIndex = inFeatureClass.FindField(sortFieldName);
                    if (sortFieldIndex >= 0)
                    {
                        inTableSort = new TableSortClass();
                        inTableSort.QueryFilter = queryFilter;
                        inTableSort.Table = inTable;
                        inTableSort.Fields = sortFieldName;
                        inTableSort.set_Ascending(sortFieldName, true);
                        inTableSort.Sort(null);
                    }
                }

                // 两点创建直线
                object missing = Type.Missing;
                for (int fromPointIndex = 0; fromPointIndex < rowCount; fromPointIndex++)
                {
                    ICursor inCursor = null;
                    if (inTableSort == null) // 获取没有排序结果
                        inCursor = inTable.Search(queryFilter, true);
                    else // 获取排序结果
                        inCursor = inTableSort.Rows;

                    IPoint fromPoint = null;
                    for (int toPointIndex = 0; toPointIndex < rowCount; toPointIndex++)
                    {
                        IRow inRow = inCursor.NextRow();
                        IFeature inFeature = (IFeature)inRow;
                        var currentPoint = (IPoint)inFeature.ShapeCopy;
                        if (fromPointIndex > toPointIndex)
                        {
                            continue;
                        }
                        else if (fromPointIndex == toPointIndex)
                        {
                            fromPoint = currentPoint;
                            outFeatureBuffer.set_Value(fromIDIndex, inFeature.OID);
                        }
                        else if (fromPointIndex < toPointIndex)
                        {
                            IPoint toPoint = currentPoint;
                            outFeatureBuffer.set_Value(toIDIndex, inFeature.OID);

                            IPolyline polyline = new PolylineClass();
                            var point_collection = polyline as IPointCollection;
                            point_collection.AddPoint(fromPoint, missing, missing);
                            point_collection.AddPoint(toPoint, missing, missing);

                            outFeatureBuffer.Shape = polyline;
                            outFeatureCurosr.InsertFeature(outFeatureBuffer);
                        }
                    } //for (int toPointIndex = 0; toPointIndex < rowCount; toPointIndex++)
                } //for (int fromPointIndex = 0; fromPointIndex < rowCount; fromPointIndex++)
            } //for (int valueIndex = 0; valueIndex < whereClauseArray.Count; valueIndex++)

            outFeatureCurosr.Flush();

            Marshal.ReleaseComObject(outFeatureCurosr);
            Marshal.ReleaseComObject(outFeatureBuffer);

            return outFeatureClass;
        }

        /// <summary>
        /// 获得极地点至指定纬度圈区域的外接矩形
        /// </summary>
        /// <param name="latitude">纬度, (正值为北纬,负值为南纬)</param>
        /// <param name="spatialReference">投影坐标系</param>
        public IPolygon PolarLatitudeToPolygon(double latitude, ISpatialReference spatialReference)
        {
            if (spatialReference == null)
                return null;

            IProjectedCoordinateSystem projectedCoordinateSystem = null;
            if (spatialReference is IProjectedCoordinateSystem)
                projectedCoordinateSystem = (IProjectedCoordinateSystem)spatialReference;
            else
                return null;

            double interval = 1;
            int count = Convert.ToInt32(360.0 / interval);
            var multipoint = new MultipointClass();
            var pointCollection = (IPointCollection)multipoint;
            for (int i = 0; i < count; i++)
            {
                WKSPoint wksPoint = new WKSPoint();
                wksPoint.X = i * interval;
                wksPoint.Y = latitude;
                projectedCoordinateSystem.Forward(1, ref wksPoint);
                pointCollection.AddWKSPoints(1, ref wksPoint);
            }

            object missing = Type.Missing;
            var geometry = (IGeometry)pointCollection;
            IPointCollection polgonPointCollection = new PolygonClass();
            polgonPointCollection.AddPoint(geometry.Envelope.UpperLeft, ref missing, ref missing);
            polgonPointCollection.AddPoint(geometry.Envelope.UpperRight, ref missing, ref missing);
            polgonPointCollection.AddPoint(geometry.Envelope.LowerRight, ref missing, ref missing);
            polgonPointCollection.AddPoint(geometry.Envelope.LowerLeft, ref missing, ref missing);
            ITopologicalOperator topologicalOperator = (ITopologicalOperator)polgonPointCollection;
            topologicalOperator.Simplify();
            var polygon = (IPolygon)polgonPointCollection;

            Marshal.ReleaseComObject(multipoint);

            return polygon;
        }

        /// <summary>
        /// 要素类从现有投影转换为新的投影
        /// </summary>
        /// <param name="inFeatureClass">转投影的要素类</param>
        /// <param name="queryFilter">要素类过滤器</param>
        /// <param name="newSpatialReference">新的坐标参考系</param>
        /// <param name="outFeatureClass_path">输出路径</param>
        /// <param name="outFeatureClass_name">输出要素类名</param>
        /// <param name="featureDataset_name">若输出路径为空间数据库,其要素数据集名</param>
        public void Project(
            IFeatureClass inFeatureClass,
            IQueryFilter queryFilter,
            ISpatialReference newSpatialReference,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name)
        {
            if (newSpatialReference == null ||
                inFeatureClass == null ||
                string.IsNullOrWhiteSpace(outFeatureClass_path) ||
                string.IsNullOrWhiteSpace(outFeatureClass_name))
                return;

            var geoDataset = (IGeoDataset)inFeatureClass;
            var spatialReference = geoDataset.SpatialReference;

            IClone comparison = spatialReference as IClone;
            if (comparison.IsEqual((IClone)newSpatialReference))
                return;

            Type factoryType = null;
            string extension = System.IO.Path.GetExtension(outFeatureClass_path);
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            else
                return;

            var outWorkspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            var outWorkspace = outWorkspaceFactory.OpenFromFile(outFeatureClass_path, 0);

            // 删除已存在同名要素类
            IWorkspace2 outWorkspace2 = (IWorkspace2)outWorkspace;
            IFeatureWorkspace outFeatureWorkspace = (IFeatureWorkspace)outWorkspace2;
            if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureClass, outFeatureClass_name))
            {
                var tmpDataset = (IDataset)outFeatureWorkspace.OpenFeatureClass(outFeatureClass_name);
                tmpDataset.Delete();
            }

            // 获取输出要素数据集名
            IFeatureDataset outFeatureDataset = null;
            IFeatureDatasetName outFeatureDatasetName = null;
            if (extension == "" && !string.IsNullOrWhiteSpace(featureDataset_name))
            {
                // 判断要素数据集是否存在
                if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featureDataset_name))
                    outFeatureDataset = outFeatureWorkspace.OpenFeatureDataset(featureDataset_name);
                else
                    outFeatureDataset = outFeatureWorkspace.CreateFeatureDataset(
                        featureDataset_name, spatialReference);

                // 判断要素数据集的空间参考系与新参考系是否相同
                var tmp_geoDataset = (IGeoDataset)outFeatureDataset;
                var tmp_comparison = tmp_geoDataset.SpatialReference as IClone;
                if (!tmp_comparison.IsEqual((IClone)newSpatialReference))
                    return;

                outFeatureDatasetName = (IFeatureDatasetName)outFeatureDataset.FullName;
            }

            // 输出工作空间名
            IDataset outDataset = (IDataset)outWorkspace;
            IWorkspaceName outWorkspaceName = (IWorkspaceName)outDataset.FullName;

            // 输出FeatureClass名
            IFeatureClassName outFeatureClassName = new FeatureClassNameClass();
            IDatasetName outDatasetName = (IDatasetName)outFeatureClassName;
            outDatasetName.WorkspaceName = outWorkspaceName;
            outDatasetName.Name = outFeatureClass_name;

            // 输入的工作空间
            IDataset inDataset = (IDataset)inFeatureClass;
            IFeatureClassName inFeatureClassName = (IFeatureClassName)inDataset.FullName;
            IWorkspace inWorkspace = inDataset.Workspace;

            //检查字段的有效性
            IFieldChecker fieldChecker = new FieldCheckerClass();
            fieldChecker.InputWorkspace = inWorkspace;
            fieldChecker.ValidateWorkspace = outWorkspace;
            IFields inFields = inFeatureClass.Fields;
            IEnumFieldError enumFieldError;
            IFields outFields;
            fieldChecker.Validate(inFields, out enumFieldError, out outFields);

            // 获取源要素类的空间参考，可以通过获取源要素类中Shape字段的GeometryDef字段获得
            // 这里应该也可以自定义GeometryDef，实现源要素类的投影变换？
            IGeometryDef geometryDef = new GeometryDefClass();
            IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
            geometryDefEdit.GeometryType_2 = inFeatureClass.ShapeType;
            geometryDefEdit.SpatialReference_2 = newSpatialReference;

            // 调用IFeatureDataConverter接口进行数据转换
            IFeatureDataConverter featureDataConverter = new FeatureDataConverterClass();
            featureDataConverter.ConvertFeatureClass(
                inFeatureClassName,
                queryFilter,
                outFeatureDatasetName,
                outFeatureClassName,
                geometryDef,
                outFields,
                "",
                1000,
                0);

            Marshal.ReleaseComObject(outWorkspaceFactory);
            Marshal.ReleaseComObject(outWorkspace);
            Marshal.ReleaseComObject(outFeatureDataset);
            Marshal.ReleaseComObject(outFeatureDatasetName);
            Marshal.ReleaseComObject(outWorkspaceName);
            Marshal.ReleaseComObject(outFeatureClassName);
            Marshal.ReleaseComObject(fieldChecker);
            Marshal.ReleaseComObject(inFields);
            Marshal.ReleaseComObject(geometryDef);
            Marshal.ReleaseComObject(featureDataConverter);
        }

        /// <summary>
        /// 要素类从现有投影转换为新的投影
        /// </summary>
        /// <param name="inFeatureClass">转投影的要素类</param>
        /// <param name="newSpatialReference">新的坐标参考系</param>
        /// <param name="outFeatureClass_path">输出路径</param>
        /// <param name="outFeatureClass_name">输出要素类名</param>
        /// <param name="featureDataset_name">若输出路径为空间数据库,其要素数据集名</param>
        /// <param name="esriSRGeoTransformationTypeObject">地理基准面转换方式</param>
        /// <returns></returns>
        public IFeatureClass Project(
            IFeatureClass inFeatureClass,
            ISpatialReference newSpatialReference,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name,
            object esriSRGeoTransformationTypeObject = null)
        {
            if (inFeatureClass == null ||
                newSpatialReference == null ||
                string.IsNullOrWhiteSpace(outFeatureClass_path) ||
                string.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            var geoDataset = (IGeoDataset)inFeatureClass;
            var spatialReference = geoDataset.SpatialReference;

            IClone comparison = spatialReference as IClone;
            if (comparison.IsEqual((IClone)newSpatialReference))
                return null;

            // 检查地理基准面转换
            int gTransformationType = int.MinValue;
            if (esriSRGeoTransformationTypeObject is esriSRGeoTransformationType)
                gTransformationType = (int)esriSRGeoTransformationTypeObject;
            else if (esriSRGeoTransformationTypeObject is esriSRGeoTransformation2Type)
                gTransformationType = (int)esriSRGeoTransformationTypeObject;
            else if (esriSRGeoTransformationTypeObject is esriSRGeoTransformation3Type)
                gTransformationType = (int)esriSRGeoTransformationTypeObject;
            IGeoTransformation geoTransformation = null;
            if (gTransformationType != int.MinValue)
            {
                ISpatialReferenceFactory2 spatialReferenceFactory2 = new SpatialReferenceEnvironmentClass();
                geoTransformation = (IGeoTransformation)spatialReferenceFactory2.CreateGeoTransformation(gTransformationType);
            }

            // 打开输出路径的工作空间
            Type factoryType = null;
            string extension = System.IO.Path.GetExtension(outFeatureClass_path);
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            else
                return null;

            var outWorkspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            var outWorkspace = outWorkspaceFactory.OpenFromFile(outFeatureClass_path, 0);
            var outFeatureWorkspace = (IFeatureWorkspace)outWorkspace;

            // 删掉存在的同名要素类
            IFeatureClass outFeatureClass = null;
            var outWorkspace2 = (IWorkspace2)outWorkspace;
            if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureClass, outFeatureClass_name))
            {
                outFeatureClass = outFeatureWorkspace.OpenFeatureClass(outFeatureClass_name);
                ((IDataset)outFeatureClass).Delete();
            }

            // 复制属性字段，添加修改的SHAPE字段
            int shapeFieldIndex = inFeatureClass.FindField(inFeatureClass.ShapeFieldName); // 获得Shape字段索引
            IFields outFields = new FieldsClass();
            IFieldsEdit outFieldsEdit = (IFieldsEdit)outFields;
            for (int fieldIndex = 0; fieldIndex < inFeatureClass.Fields.FieldCount; fieldIndex++)
            {
                if (fieldIndex == shapeFieldIndex)
                {
                    IGeometryDef geometryDef = new GeometryDefClass();
                    IGeometryDefEdit geometryDefEdit = (IGeometryDefEdit)geometryDef;
                    geometryDefEdit.GeometryType_2 = inFeatureClass.ShapeType;
                    geometryDefEdit.SpatialReference_2 = newSpatialReference;

                    IField shpField = new FieldClass();
                    IFieldEdit shpFieldEdit = (IFieldEdit)shpField;
                    shpFieldEdit.Name_2 = inFeatureClass.ShapeFieldName;
                    shpFieldEdit.AliasName_2 = inFeatureClass.ShapeFieldName;
                    shpFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;
                    shpFieldEdit.GeometryDef_2 = geometryDef;
                    outFieldsEdit.AddField(shpField);
                }
                else
                {
                    outFieldsEdit.AddField(inFeatureClass.Fields.Field[fieldIndex]);
                }
            }

            // 创建要素类
            if (extension == "" || featureDataset_name == "")
            {   // 在Shapefile或Geodatabase建立要素类
                outFeatureClass = outFeatureWorkspace.CreateFeatureClass(
                    outFeatureClass_name,
                    outFields,
                    inFeatureClass.CLSID,
                    inFeatureClass.EXTCLSID,
                    esriFeatureType.esriFTSimple,
                    inFeatureClass.ShapeFieldName,
                    "");
            }
            else
            {   // 在Geodatabase的FeatureDataset建立要素类
                IFeatureDataset featureDataset = null;
                if (outWorkspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featureDataset_name))
                    featureDataset = outFeatureWorkspace.OpenFeatureDataset(featureDataset_name);
                else
                    featureDataset = outFeatureWorkspace.CreateFeatureDataset(featureDataset_name, spatialReference);

                outFeatureClass = featureDataset.CreateFeatureClass(
                    outFeatureClass_name,
                    outFields,
                    inFeatureClass.CLSID,
                    inFeatureClass.EXTCLSID,
                    esriFeatureType.esriFTSimple,
                    inFeatureClass.ShapeFieldName,
                    "");

                Marshal.ReleaseComObject(featureDataset);
            }

            // 生成两个要素类字段的对应表
            Dictionary<int, int> fieldsDictionary = new Dictionary<int, int>();
            for (int i = 0; i < inFeatureClass.Fields.FieldCount; i++)
            {
                if (inFeatureClass.Fields.Field[i].Editable == false)
                    continue; // 跳过系统自动生成的不可编辑的字段 

                string field_name = inFeatureClass.Fields.Field[i].Name.ToUpper();
                for (int j = 0; j < outFeatureClass.Fields.FieldCount; j++)
                {
                    string field_name2 = outFeatureClass.Fields.Field[j].Name.ToUpper();
                    if (field_name == field_name2)
                        fieldsDictionary.Add(i, j);
                }
            }

            // 向输出要素类中添加要素
            var inFeatureCursor = inFeatureClass.Search(null, false);
            var outFeatureCursor = outFeatureClass.Insert(true);
            IFeatureBuffer outFeatureBuffer = outFeatureClass.CreateFeatureBuffer();
            IFeature feature = inFeatureCursor.NextFeature();
            int index = 0;
            while (feature != null)
            {
                // 复制要素的属性值
                foreach (KeyValuePair<int, int> keyValue in fieldsDictionary)
                {
                    if (keyValue.Key == shapeFieldIndex) // 投影转换
                    {
                        IGeometry5 geometry = feature.ShapeCopy as IGeometry5;
                        if (geoTransformation == null)
                            geometry.Project(newSpatialReference);
                        else
                            geometry.ProjectEx(newSpatialReference,
                                esriTransformDirection.esriTransformForward,
                                geoTransformation, false, 0, 0);
                        outFeatureBuffer.Shape = geometry;
                    }
                    else
                    {
                        outFeatureBuffer.set_Value(keyValue.Value, feature.get_Value(keyValue.Key));
                    }
                }

                outFeatureCursor.InsertFeature(outFeatureBuffer);
                feature = inFeatureCursor.NextFeature();

                if (index++ % 1000 == 0) outFeatureCursor.Flush();
            }
            outFeatureCursor.Flush();

            Marshal.ReleaseComObject(spatialReference);
            Marshal.ReleaseComObject(outWorkspaceFactory);
            Marshal.ReleaseComObject(outWorkspace);
            Marshal.ReleaseComObject(outFields);
            Marshal.ReleaseComObject(inFeatureCursor);
            Marshal.ReleaseComObject(outFeatureCursor);
            Marshal.ReleaseComObject(outFeatureBuffer);

            return outFeatureClass;
        }

        /// <summary>
        /// 栅格数据转投影, 实现了ArcMapToolBox中的ProjectRaster功能
        /// </summary>
        /// <param name="inputRasterDataset">输入栅格数据</param>
        /// <param name="outputSpatialReference">输出坐标系</param>
        /// <param name="outCellSizeX">输出栅格数据的象元X尺寸</param>
        /// <param name="outCellSizeY">输出栅格数据的象元Y尺寸</param>
        /// <param name="upperLeftX">捕捉栅格左角点X坐标</param>
        /// <param name="upperLeftY">捕捉栅格左角点Y坐标</param>
        /// <returns></returns>
        public IRasterDataset ProjectRaster(
            IRasterDataset inputRasterDataset,
            ISpatialReference outputSpatialReference,
            rstResamplingTypes resamplingType,
            double outCellSizeX,
            double outCellSizeY,
            double upperLeftX,
            double upperLeftY)
        {
            var inputGeoDataset = (IGeoDataset)inputRasterDataset;
            var inputSpatialReference = inputGeoDataset.SpatialReference;

            IClone comparison = inputSpatialReference as IClone;
            if (comparison.IsEqual((IClone)outputSpatialReference))
                return null;

            //
            // 调用IRasterFunction函数，先对源栅格进行重采样，然后进行投影转换，得到最终结果
            // 只有按照上述方式，得到的结果才与ArcMap计算的象元值相同，但是栅格行列数略有不同
            //

            // 重采样
            IRasterFunction resampleFunction;
            IResampleFunctionArguments resampleFArguments;
            IFunctionRasterDataset resampleFRasterDS;

            resampleFArguments = new ResampleFunctionArgumentsClass();
            resampleFArguments.Raster = inputRasterDataset;
            resampleFArguments.OutputCellsize = new PntClass() { X = outCellSizeX, Y = outCellSizeY };
            resampleFArguments.ResamplingType = resamplingType;

            resampleFRasterDS = new FunctionRasterDataset();
            resampleFunction = new ResampleFunctionClass();
            resampleFRasterDS.Init(resampleFunction, resampleFArguments);

            // 转投影
            IRasterFunction reprojectFunction;
            IReprojectFunctionArguments reprojectFArguments;
            IFunctionRasterDataset reprojectFRasterDS;

            reprojectFArguments = new ReprojectFunctionArgumentsClass();
            reprojectFArguments.Raster = resampleFRasterDS;
            reprojectFArguments.SpatialReference = outputSpatialReference;
            reprojectFArguments.XCellsize = outCellSizeX;
            reprojectFArguments.YCellsize = outCellSizeY;
            reprojectFArguments.XOrigin = upperLeftX;
            reprojectFArguments.YOrigin = upperLeftY;

            reprojectFRasterDS = new FunctionRasterDataset();
            reprojectFunction = new ReprojectFunctionClass();
            reprojectFRasterDS.Init(reprojectFunction, reprojectFArguments);

            Marshal.ReleaseComObject(inputSpatialReference);
            Marshal.ReleaseComObject(resampleFunction);
            Marshal.ReleaseComObject(resampleFArguments);
            Marshal.ReleaseComObject(resampleFRasterDS);
            Marshal.ReleaseComObject(reprojectFunction);
            Marshal.ReleaseComObject(reprojectFArguments);

            return (IRasterDataset)reprojectFRasterDS;
        }

        /// <summary>
        /// 栅格数据转投影
        /// </summary>
        /// <param name="inRasterDataset">栅格数据集</param>
        /// <param name="outSpatialReference">新投影</param>
        /// <param name="esriSRGeoTransformationTypeObject">地理参考系转换类型</param>
        public IGeoDataset ProjectRaster(
            IRasterDataset inRasterDataset,
            ISpatialReference outSpatialReference,
            esriGeoAnalysisResampleEnum resampleType,
            object outExtent,
            object snapRaster,
            object outCellSize)
        {
            var geoDataset = (IGeoDataset)inRasterDataset;
            var inSpatialReference = geoDataset.SpatialReference;

            IClone comparison = inSpatialReference as IClone;
            if (comparison.IsEqual((IClone)outSpatialReference))
                return null;

            // 投影转换
            ITransformationOp transformationOp = new RasterTransformationOpClass();

            var rasterAnaysisEnvironment = (IRasterAnalysisEnvironment)transformationOp;
            object extentProvider = outExtent;
            object snapRasterData = snapRaster;
            rasterAnaysisEnvironment.SetExtent(
                esriRasterEnvSettingEnum.esriRasterEnvMaxOf, ref extentProvider, ref snapRaster);

            var outGeoDataset = transformationOp.ProjectFast(
                geoDataset, outSpatialReference, resampleType, outCellSize);

            Marshal.ReleaseComObject(inSpatialReference);
            Marshal.ReleaseComObject(transformationOp);

            return outGeoDataset;
        }

        /// <summary>
        /// 不同大地基准面转换的栅格数据，投影转换
        /// </summary>
        /// <param name="rasterDataset"></param>
        /// <param name="outSpatialReference"></param>
        /// <param name="geoSRTransType"></param>
        public IRaster ProjectRasterWithDatumTransformation(
            IRasterDataset2 rasterDataset,
            ISpatialReference outSpatialReference,
            esriSRGeoTransformation2Type geoSRTransType)
        {
            //This example shows how to specify a datum transformation when projecting raster data.
            //rasterDataset—Represents input of a raster dataset that has a known spatial reference.
            //outSR—Represents the spatial reference of the output raster dataset.
            //geoTrans—Represents the geotransformation between the input and output spatial reference.
            //Set output spatial reference.

            IRaster raster = rasterDataset.CreateFullRaster();
            IRasterProps rasterProps = (IRasterProps)raster;
            rasterProps.SpatialReference = outSpatialReference;

            //Specify the geotransformation.
            ISpatialReferenceFactory2 srFactory = new SpatialReferenceEnvironmentClass();
            IGeoTransformation geoTransformation = (IGeoTransformation)
                srFactory.CreateGeoTransformation((int)geoSRTransType);

            //Add to the geotransformation operation set.
            IGeoTransformationOperationSet operationSet = new GeoTransformationOperationSetClass();
            operationSet.Set(esriTransformDirection.esriTransformForward, geoTransformation);
            operationSet.Set(esriTransformDirection.esriTransformReverse, geoTransformation);

            //Set the geotransformation on the raster.
            IRaster2 raster2 = (IRaster2)raster;
            raster2.GeoTransformations = operationSet;

            Marshal.ReleaseComObject(srFactory);
            Marshal.ReleaseComObject(operationSet);

            return raster;
        }

        /// <summary>
        /// 重新计算要素类的XY，Z和M范围属性
        /// </summary>
        /// <param name="featureClass">要重新计算范围属性的要素类</param>
        public void RecalculateFeatureClassExtent(IFeatureClass featureClass)
        {
            var featureClassManager = (IFeatureClassManage)featureClass;
            featureClassManager.UpdateExtent();
        }

        /// <summary>
        /// 设置栅格数据集属性值
        /// </summary>
        /// <param name="rasterDataset">栅格数据集</param>
        /// <param name="pixelValues">像元值数组</param>
        /// <param name="bandIndex">设置像元值的波段索引</param>
        public void SetRasterDatasetPixelValue(
            IRasterDataset rasterDataset,
            object[,] pixelValues,
            int bandIndex = 0)
        {
            var rasterBands = (IRasterBandCollection)rasterDataset;
            var rasterBand = rasterBands.Item(bandIndex);
            var rasterProps = (IRasterProps)rasterBand;
            var raster = rasterDataset.CreateDefaultRaster();

            IPnt blocksize = new PntClass();
            blocksize.SetCoords(rasterProps.Width, rasterProps.Height);
            var pixelblock = (IPixelBlock3)raster.CreatePixelBlock(blocksize);
            var pixels = (System.Array)pixelblock.get_PixelData(0);
            for (int i = 0; i < rasterProps.Width; i++)
                for (int j = 0; j < rasterProps.Height; j++)
                {
                    switch (rasterProps.PixelType)
                    {
                        case rstPixelType.PT_DOUBLE:
                            pixels.SetValue(Convert.ToDouble(pixelValues[i, j]), i, j);
                            break;
                        case rstPixelType.PT_FLOAT:
                            pixels.SetValue(Convert.ToSingle(pixelValues[i, j]), i, j);
                            break;
                        case rstPixelType.PT_LONG:
                            pixels.SetValue(Convert.ToInt64(pixelValues[i, j]), i, j);
                            break;
                        case rstPixelType.PT_CHAR:
                            pixels.SetValue(Convert.ToString(pixelValues[i, j]), i, j);
                            break;
                        default:
                            break;
                    }
                }

            IPnt upperLeft = new PntClass();
            upperLeft.SetCoords(0, 0);
            pixelblock.set_PixelData(0, (System.Array)pixels);
            var rasterEdit = (IRasterEdit)raster;
            rasterEdit.Write(upperLeft, (IPixelBlock)pixelblock);

            Marshal.ReleaseComObject(raster);
            Marshal.ReleaseComObject(rasterBand);
            Marshal.ReleaseComObject(blocksize);
            Marshal.ReleaseComObject(upperLeft);

        }

        /// <summary>
        /// 在折点处打断要素类
        /// </summary>
        /// <param name="inFeatureClass">输入要素类</param>
        /// <param name="outFeatureClass_path">输出路径</param>
        /// <param name="outFeatureClass_name">输出要素类名</param>
        /// <param name="featureDataset_name">若输出路径为空间数据库,其要素数据集名</param>
        public IFeatureClass SplitLineAtVertices(
            IFeatureClass inFeatureClass,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name)
        {
            if (inFeatureClass == null ||
                string.IsNullOrWhiteSpace(outFeatureClass_path) ||
                string.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            if (inFeatureClass.ShapeType != esriGeometryType.esriGeometryPolyline &&
                inFeatureClass.ShapeType != esriGeometryType.esriGeometryPolygon)
                return null;

            var geoDataset = (IGeoDataset)inFeatureClass;
            var spatialRefernce = geoDataset.SpatialReference;

            // 生成输出要素类
            var outFeatureClass = this.CreateFeatureClass(
                outFeatureClass_path, outFeatureClass_name, featureDataset_name,
                esriGeometryType.esriGeometryPolyline, spatialRefernce);

            // 添加字段
            IFields inFields = inFeatureClass.Fields;
            for (int i = 0; i < inFields.FieldCount; i++)
            {
                if (inFields.Field[i].Editable == true)
                {
                    var field = inFields.Field[i];
                    this.AddField((ITable)outFeatureClass, field.Name, field.Type, true);
                }
            }

            // 根据字段名生成两个要素类字段索引对应的Key-Value表
            Dictionary<int, int> fieldsDictionary = new Dictionary<int, int>();
            for (int i = 0; i < inFeatureClass.Fields.FieldCount; i++)
            {
                if (inFeatureClass.Fields.Field[i].Editable == false)
                    continue; // 跳过系统自动生成的不可编辑的字段 

                string field_name = inFeatureClass.Fields.Field[i].Name.ToUpper();
                for (int j = 0; j < outFeatureClass.Fields.FieldCount; j++)
                {
                    string field_name2 = outFeatureClass.Fields.Field[j].Name.ToUpper();
                    if (field_name == field_name2)
                        fieldsDictionary.Add(i, j);
                }
            }

            IFeatureCursor searchCursor = inFeatureClass.Search(null, true);
            int featureCount = inFeatureClass.FeatureCount(null);
            int shapeFieldIndex = inFeatureClass.FindField(inFeatureClass.ShapeFieldName);

            IFeatureCursor insertCursor = outFeatureClass.Insert(true);
            IFeatureBuffer featureBuffer = outFeatureClass.CreateFeatureBuffer();
            for (int i = 0; i < featureCount; i++)
            {
                var feature = searchCursor.NextFeature();

                var geometry = feature.ShapeCopy;
                var pointCollection = (IPointCollection)geometry;
                var enumVertices = pointCollection.EnumVertices;
                var polycurve = (IPolycurve3)geometry;
                polycurve.SplitAtPoints(enumVertices, true, true, 0.000000000001);
                polycurve.SnapToSpatialReference();

                var segmentCollection = (ISegmentCollection)polycurve;
                for (int j = 0; j < segmentCollection.SegmentCount; j++)
                {
                    foreach (KeyValuePair<int, int> keyValue in fieldsDictionary)
                    {
                        if (keyValue.Key == shapeFieldIndex)
                            continue;
                        featureBuffer.set_Value(keyValue.Value, feature.get_Value(keyValue.Key));
                    }

                    ISegmentCollection newSegmentCollection = new PolylineClass();
                    newSegmentCollection.AddSegment(segmentCollection.Segment[j]);
                    featureBuffer.Shape = (IPolyline)newSegmentCollection;
                    insertCursor.InsertFeature(featureBuffer);
                }
                insertCursor.Flush();
            }

            Marshal.ReleaseComObject(spatialRefernce);
            Marshal.ReleaseComObject(inFields);
            Marshal.ReleaseComObject(searchCursor);
            Marshal.ReleaseComObject(insertCursor);
            Marshal.ReleaseComObject(featureBuffer);

            return outFeatureClass;
        }

        /// <summary>
        /// 按照点要素的相交或接近来打断线要素
        /// </summary>
        /// <param name="lineFeatureClass">线要素类</param>
        /// <param name="pointFeatureClass">点要素类</param>
        /// <param name="outFeatureClass_path">输出路径</param>
        /// <param name="outFeatureClass_name">输出要素类名</param>
        /// <param name="featureDataset_name">若输出路径为空间数据库,其要素数据集名</param>
        /// <param name="cutDistance">点与线的接近距离</param>
        public IFeatureClass SplitLineAtPoint(
            IFeatureClass lineFeatureClass,
            IFeatureClass pointFeatureClass,
            string outFeatureClass_path,
            string outFeatureClass_name,
            string featureDataset_name,
            double cutDistance)
        {
            if (lineFeatureClass == null ||
                pointFeatureClass == null ||
                string.IsNullOrWhiteSpace(outFeatureClass_path) ||
                string.IsNullOrWhiteSpace(outFeatureClass_name))
                return null;

            // 判断类型
            if (lineFeatureClass.ShapeType != esriGeometryType.esriGeometryPolyline ||
                pointFeatureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                return null;

            var lineGeoDataset = (IGeoDataset)lineFeatureClass;
            var lineSpatialRefernce = lineGeoDataset.SpatialReference;
            var pointGeoDataset = (IGeoDataset)pointFeatureClass;
            var pointSpatialRefernce = pointGeoDataset.SpatialReference;

            // 判断坐标系
            IClone comparison = lineSpatialRefernce as IClone;
            if (!comparison.IsEqual((IClone)pointSpatialRefernce))
                return null;

            // 生成输出要素类
            var outFeatureClass = this.CreateFeatureClass(
               outFeatureClass_path, outFeatureClass_name, featureDataset_name, esriGeometryType.esriGeometryPolyline, lineSpatialRefernce);

            // 添加字段
            var inFields = lineFeatureClass.Fields;
            for (int i = 0; i < inFields.FieldCount; i++)
            {
                var field = inFields.Field[i];
                if (field.Editable == true)
                    this.AddField((ITable)outFeatureClass, field.Name, field.Type, true);
            }

            // 根据字段名生成两个要素类字段索引对应的Key-Value表
            Dictionary<int, int> fieldsDictionary = new Dictionary<int, int>();
            for (int i = 0; i < lineFeatureClass.Fields.FieldCount; i++)
            {
                if (lineFeatureClass.Fields.Field[i].Editable == false)
                    continue; // 跳过系统自动生成的不可编辑的字段 

                string field_name = lineFeatureClass.Fields.Field[i].Name.ToUpper();
                for (int j = 0; j < outFeatureClass.Fields.FieldCount; j++)
                {
                    string field_name2 = outFeatureClass.Fields.Field[j].Name.ToUpper();
                    if (field_name == field_name2)
                        fieldsDictionary.Add(i, j);
                }
            }

            // 获得所有点要素
            IPointCollection pointCollection = new MultipointClass();
            object missing = Type.Missing;
            IFeatureCursor pointCursor = pointFeatureClass.Search(null, false);
            IFeature pointFeature = pointCursor.NextFeature();
            IPoint point0 = (IPoint)pointFeature.ShapeCopy;
            while (pointFeature != null)
            {
                IPoint point = (IPoint)pointFeature.ShapeCopy;
                pointCollection.AddPoint(point, ref missing, ref missing);
                pointFeature = pointCursor.NextFeature();
            }

            // 打断线要素类中的所有要素
            IFeatureCursor lineCursor = lineFeatureClass.Search(null, false);
            int lineFeatureCount = lineFeatureClass.FeatureCount(null);
            int shapeFieldIndex = lineFeatureClass.FindField(lineFeatureClass.ShapeFieldName);

            IFeatureCursor outFeatureCursor = outFeatureClass.Insert(true);
            IFeatureBuffer outFeatureBuffer = outFeatureClass.CreateFeatureBuffer();
            for (int i = 0; i < lineFeatureCount; i++)
            {
                var lineFeature = lineCursor.NextFeature();
                var polyline = (IPolyline)lineFeature.ShapeCopy;
                var polycurve = (IPolycurve2)polyline;
                polycurve.SplitAtPoints(pointCollection.EnumVertices, true, true, cutDistance);
                var geometryCollection = (IGeometryCollection)polycurve;
                geometryCollection.GeometriesChanged();

                for (int j = 0; j < geometryCollection.GeometryCount; j++)
                {
                    foreach (KeyValuePair<int, int> keyValue in fieldsDictionary)
                    {
                        if (keyValue.Key == shapeFieldIndex)
                            continue;
                        outFeatureBuffer.set_Value(keyValue.Value, lineFeature.get_Value(keyValue.Key));
                    }

                    IGeometryCollection newPolyline = new PolylineClass();
                    newPolyline.AddGeometry(geometryCollection.Geometry[j] as IGeometry, ref missing, ref missing);
                    outFeatureBuffer.Shape = (IPolyline)newPolyline;
                    outFeatureCursor.InsertFeature(outFeatureBuffer);
                }
                outFeatureCursor.Flush();
            }

            Marshal.ReleaseComObject(lineSpatialRefernce);
            Marshal.ReleaseComObject(pointSpatialRefernce);
            Marshal.ReleaseComObject(inFields);
            Marshal.ReleaseComObject(pointCollection);
            Marshal.ReleaseComObject(pointCursor);
            Marshal.ReleaseComObject(lineCursor);
            Marshal.ReleaseComObject(outFeatureCursor);
            Marshal.ReleaseComObject(outFeatureBuffer);

            return outFeatureClass;
        }

    }

    public class EditingTools
    {
        /// <summary>
        /// 通过 Douglas-Peucker(道格拉斯-普克) 算法简化要素
        /// </summary>
        /// <param name="featureClass">Polylin/Polygon类型的要素类</param>
        /// <param name="tolerance">最大偏移量(单位:米)</param>
        public void Generalize(
            IFeatureClass featureClass,
            double tolerance)
        {
            if (featureClass == null)
                return;

            if (featureClass.ShapeType != esriGeometryType.esriGeometryPolygon
                && featureClass.ShapeType != esriGeometryType.esriGeometryPolyline)
                return;

            IFeatureCursor featureCursor = featureClass.Update(null, true);
            IFeature feature = featureCursor.NextFeature();
            while (feature != null)
            {
                var polycurve = (IPolycurve)feature.ShapeCopy;
                polycurve.Generalize(tolerance);

                feature.Shape = polycurve;
                featureCursor.UpdateFeature(feature);
                feature = featureCursor.NextFeature();
            }

            Marshal.ReleaseComObject(featureCursor);
        }

    }

    // NetworkAnalystTools类AddLocations函数参数类型
    public enum ParamLocationsItem
    {
        Stops, PointBarriers, LineBarriers, PolygonBarriers
    }

    public class NetworkAnalystTools
    {
        /// <summary>
        /// 将网络分析对象添加到网络分析内容中
        /// 可添加的项目名为:Stops, Point Barriers, Line Barriers, Polygon Barriers
        /// </summary>
        /// <param name="naContext"></param>
        /// <param name="locations"></param>
        /// <param name="item_type"></param>
        public void AddLocations(
            INAContext naContext,
            IFeatureClass locations,
            ParamLocationsItem item_type)
        {
            var geometryType = locations.ShapeType; // 要素类型
            if ((item_type == ParamLocationsItem.Stops || item_type == ParamLocationsItem.PointBarriers)
                && geometryType != esriGeometryType.esriGeometryPoint)
                return;
            if (item_type == ParamLocationsItem.LineBarriers
                && geometryType != esriGeometryType.esriGeometryPolyline)
                return;
            if (item_type == ParamLocationsItem.PolygonBarriers
                && geometryType != esriGeometryType.esriGeometryPolygon)
                return;

            string item_name = "Stops";
            if (item_type == ParamLocationsItem.Stops)
                item_name = "Stops";
            else if (item_type == ParamLocationsItem.PointBarriers)
                item_name = "Point Barriers";
            else if (item_type == ParamLocationsItem.LineBarriers)
                item_name = "Line Barriers";
            else if (item_type == ParamLocationsItem.PolygonBarriers)
                item_name = "Polygon Barriers";

            ICursor cursor = locations.Search(null, false) as ICursor;
            INAClassLoader naClassLoader = new NAClassLoaderClass() as INAClassLoader;
            naClassLoader.NAClass = naContext.NAClasses.get_ItemByName(item_name) as INAClass;
            naClassLoader.Locator = naContext.Locator;
            int rowsInCursor = 0;
            int rowsLocated = 0;
            naClassLoader.Load(cursor, null, ref rowsInCursor, ref rowsLocated);
        }

        /// <summary>
        /// 基于Geodatabase建立网络数据集(Geodatabase Network Dataset)
        /// </summary>
        /// <param name="geodatabase_file">Geodatabase全名(路径+名+.gdb)</param>
        /// <param name="featureDataset_name">要素数据集名</param>
        /// <param name="featureClass_name">要素类名</param>
        /// <param name="costAttribute_name">花费属性字段名</param>
        /// <param name="networkDataset_name">网络数据集名</param>
        public void CreateNetworkDataset(
            string geodatabase_file,
            string featureDataset_name,
            string featureClass_name,
            string costAttribute_name,
            string networkDataset_name)
        {
            if (String.IsNullOrWhiteSpace(geodatabase_file) ||
                String.IsNullOrWhiteSpace(featureDataset_name) ||
                String.IsNullOrWhiteSpace(featureClass_name) ||
                String.IsNullOrWhiteSpace(costAttribute_name) ||
                String.IsNullOrWhiteSpace(networkDataset_name))
                return;

            // 打开Geodatabase的要素数据集
            string extension = System.IO.Path.GetExtension(geodatabase_file);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else
                return;

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(geodatabase_file, 0);
            }
            catch
            {
                Marshal.ReleaseComObject(workspaceFactory);
                return;
            }

            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            IFeatureDataset featureDataset = featureWorkspace.OpenFeatureDataset(featureDataset_name);

            // 获取 feature dataset extension, 用来创建 NetworkDataset.
            IFeatureDatasetExtensionContainer featureDatasetExtensionContainer = (IFeatureDatasetExtensionContainer)featureDataset;
            IFeatureDatasetExtension featureDatasetExtension = featureDatasetExtensionContainer.FindExtension(esriDatasetType.esriDTNetworkDataset);
            IDatasetContainer3 datasetContainer3 = (IDatasetContainer3)featureDatasetExtension;

            // 删除同名网络数据集
            IWorkspace2 workspace2 = (IWorkspace2)workspace;
            if (workspace2.get_NameExists(esriDatasetType.esriDTNetworkDataset, networkDataset_name))
            {
                IDataset dataset = datasetContainer3.get_DatasetByName(
                    esriDatasetType.esriDTNetworkDataset, networkDataset_name);
                dataset.Delete();
            }

            // ------------------------------------------------------------------------
            // 创建网络数据集数据元素
            // ------------------------------------------------------------------------
            // 创建一个空的 DENetworkDatasetClass, 用来创建 NetworkDataset.
            IDENetworkDataset deNetworkDataset = new DENetworkDatasetClass();
            deNetworkDataset.Buildable = true;
            deNetworkDataset.NetworkType = esriNetworkDatasetType.esriNDTGeodatabase;

            // 将 Geodatabase 中的 FeatureDataset 转为 GeoDataset.
            IGeoDataset geoDataset = (IGeoDataset)featureDataset;

            // 将 FeatureDataset 的 Extent 与 SpatialReference 赋给 DENetworkDataset.
            IDEGeoDataset deGeoDataset = (IDEGeoDataset)deNetworkDataset;
            deGeoDataset.Extent = geoDataset.Extent;
            deGeoDataset.SpatialReference = geoDataset.SpatialReference;

            // 指定 Network Dataset 的名字.
            IDataElement dataDlement = (IDataElement)deNetworkDataset;
            dataDlement.Name = networkDataset_name;

            // ------------------------------------------------------------------------
            // 为 Network Dataset 添加数据 Sources
            // ------------------------------------------------------------------------
            // 创建一个 edge feature source 对象.
            INetworkSource edgeNetworkSource = new EdgeFeatureSourceClass();
            edgeNetworkSource.Name = featureClass_name;
            edgeNetworkSource.ElementType = esriNetworkElementType.esriNETEdge;

            // 为 edge feature source 对象设置连通性
            IEdgeFeatureSource edgeFeatureSource = (IEdgeFeatureSource)edgeNetworkSource;
            edgeFeatureSource.UsesSubtypes = false;
            edgeFeatureSource.ClassConnectivityGroup = 1;
            edgeFeatureSource.ClassConnectivityPolicy = esriNetworkEdgeConnectivityPolicy.esriNECPEndVertex;

            // 将 edge feature source 对象添加到 DENetworkDatasetClass
            IArray source_array = new ArrayClass();
            source_array.Add(edgeNetworkSource);
            deNetworkDataset.Sources = source_array;

            // 指定 deNetworkDataset 的是否支持转向模型
            deNetworkDataset.SupportsTurns = true; ;

            // ------------------------------------------------------------------------
            // 添加 NetworkDataset 属性
            // ------------------------------------------------------------------------
            IArray attribute_array = new ArrayClass();

            // ----- 网络属性 -----
            // 创建 EvaluatedNetworkAttribute 对象, 并进行设置.
            IEvaluatedNetworkAttribute evaluatedNetworkAtrribute = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 networkAttribute2 = (INetworkAttribute2)evaluatedNetworkAtrribute;
            networkAttribute2.Name = costAttribute_name;
            networkAttribute2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            networkAttribute2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            networkAttribute2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            networkAttribute2.UseByDefault = true;

            // 创建 INetworkFieldEvaluator 对象, 基于 IEvaluatedNetworkAttribute 对象, 对其进行设置.
            INetworkFieldEvaluator networkFieldEvaluator = new NetworkFieldEvaluatorClass();
            networkFieldEvaluator.SetExpression("[" + costAttribute_name + "]", "");
            INetworkEvaluator field_networkEvaluator = (INetworkEvaluator)networkFieldEvaluator;
            evaluatedNetworkAtrribute.set_Evaluator(
                edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, field_networkEvaluator);
            evaluatedNetworkAtrribute.set_Evaluator(
                edgeNetworkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, field_networkEvaluator);

            INetworkConstantEvaluator networkConstantEvaluator = new NetworkConstantEvaluatorClass();
            networkConstantEvaluator.ConstantValue = 0;
            INetworkEvaluator constant_networkEvaluator = (INetworkEvaluator)networkConstantEvaluator;
            evaluatedNetworkAtrribute.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, constant_networkEvaluator);
            evaluatedNetworkAtrribute.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, constant_networkEvaluator);
            evaluatedNetworkAtrribute.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, constant_networkEvaluator);

            // 在 attributeArray 中添加属性
            attribute_array.Add(evaluatedNetworkAtrribute);
            deNetworkDataset.Attributes = attribute_array;

            // ------------------------------------------------------------------------
            // 创建与构建 Network Dataset
            // ------------------------------------------------------------------------
            // 获取 feature dataset extension, 基于 DENetworkDataset 创建 NetworkDataset.
            //IFeatureDatasetExtensionContainer featureDatasetExtensionContainer = (IFeatureDatasetExtensionContainer)featureDataset;
            //IFeatureDatasetExtension featureDatasetExtension = featureDatasetExtensionContainer.FindExtension(esriDatasetType.esriDTNetworkDataset);
            //IDatasetContainer3 datasetContainer3 = (IDatasetContainer3)featureDatasetExtension;
            IDEDataset deDataset = (IDEDataset)deNetworkDataset;
            INetworkDataset networkDataset = (INetworkDataset)datasetContainer3.CreateDataset(deDataset);

            // 建立网络
            INetworkBuild networkBuild = (INetworkBuild)networkDataset;
            networkBuild.BuildNetwork(geoDataset.Extent);

            // 释放COM占用的内存
            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(featureDataset);
            Marshal.ReleaseComObject(featureDatasetExtension);
            Marshal.ReleaseComObject(networkDataset);

        }

        /// <summary>
        /// 基于Shapefile建立网络数据集(Shapefile-base Network Dataset)
        /// </summary>
        /// <param name="shapefile_path">Shapefile工作空间</param>
        /// <param name="featureClass_name">要素类的名字</param>
        /// <param name="costAttribute_name">花费属性字段名</param>
        /// <param name="networkDataset_name">网络数据集名</param>
        public void CreateNetworkDataset(
            string shapefile_path,
            string featureClass_name,
            string costAttribute_name,
            string networkDataset_name)
        {
            if (String.IsNullOrWhiteSpace(shapefile_path) ||
                String.IsNullOrWhiteSpace(featureClass_name) ||
                String.IsNullOrWhiteSpace(costAttribute_name) ||
                String.IsNullOrWhiteSpace(networkDataset_name) ||
                System.IO.Path.GetExtension(shapefile_path) != "")
                return;

            // 打开Shapefile的要素类
            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(shapefile_path, 0);
            }
            catch
            {
                Marshal.ReleaseComObject(workspaceFactory);
                return;
            }

            IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
            IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(featureClass_name);

            // 创建一个 UID, 用来引用 NetworkDatasetWorkspaceExtension.
            UID networkID = new UIDClass();
            networkID.Value = "esriGeoDatabase.NetworkDatasetWorkspaceExtension";

            // 获取 workspace extension, 用来创建 NetworkDataset.
            IWorkspaceExtensionManager workspaceExtensionManager = (IWorkspaceExtensionManager)workspace;
            IWorkspaceExtension workspaceExtension = workspaceExtensionManager.FindExtension(networkID);
            IDatasetContainer3 datasetContainer3 = datasetContainer3 = workspaceExtension as IDatasetContainer3;

            // 删除同名网络数据集
            IWorkspace2 workspace2 = (IWorkspace2)workspace;
            if (workspace2.get_NameExists(esriDatasetType.esriDTNetworkDataset, networkDataset_name))
            {
                IDataset dataset = datasetContainer3.get_DatasetByName(
                    esriDatasetType.esriDTNetworkDataset, networkDataset_name);
                dataset.Delete();
            }

            // ------------------------------------------------------------------------
            // 创建网络数据集数据元素
            // ------------------------------------------------------------------------
            // 创建一个空的 DENetworkDatasetClass, 用来创建 NetworkDataset.
            IDENetworkDataset deNetworkDataset = new DENetworkDatasetClass();
            deNetworkDataset.Buildable = true;
            deNetworkDataset.NetworkType = esriNetworkDatasetType.esriNDTShapefile;

            // 将Shapefile的 FeatureClass 转为 GeoDataset
            IGeoDataset geoDataset = (IGeoDataset)featureClass;

            // 将 *.shp 的 Extent 与 SpatialReference 赋给 DENetworkDataset.
            IDEGeoDataset deGeoDataset = (IDEGeoDataset)deNetworkDataset;
            deGeoDataset.Extent = geoDataset.Extent;
            deGeoDataset.SpatialReference = geoDataset.SpatialReference;

            // 指定 Network Dataset 的名字.
            IDataElement dataElement = (IDataElement)deNetworkDataset;
            dataElement.Name = networkDataset_name;

            // ------------------------------------------------------------------------
            // 为 Network Dataset 添加数据 Sources
            // ------------------------------------------------------------------------
            // 创建一个 edge feature source 对象.
            IEdgeFeatureSource edgeFeatureSource = new EdgeFeatureSourceClass();
            INetworkSource networkSource = (INetworkSource)edgeFeatureSource;
            networkSource.Name = featureClass_name;
            networkSource.ElementType = esriNetworkElementType.esriNETEdge;

            // 为 edge feature source 对象设置连通性
            edgeFeatureSource.UsesSubtypes = false;
            edgeFeatureSource.ClassConnectivityGroup = 1;
            edgeFeatureSource.ClassConnectivityPolicy = esriNetworkEdgeConnectivityPolicy.esriNECPEndVertex;

            // 对 Network Dataset 进行高程设置, 不进行设置会报错
            // 通过 edge feature source 设置高程, 或通过 IDENetworkDataset2 设置
            //edgeFeatureSource.FromElevationFieldName = "F_ZLEV";
            //edgeFeatureSource.ToElevationFieldName = "T_ZLEV";
            IDENetworkDataset2 deNetworkDataset2 = (IDENetworkDataset2)deNetworkDataset;
            deNetworkDataset2.ElevationModel = esriNetworkElevationModel.esriNEMNone;

            // 将 edge feature source 对象添加到 DENetworkDatasetClass
            IArray sourceArray = new ArrayClass();
            sourceArray.Add(edgeFeatureSource);
            deNetworkDataset.Sources = sourceArray;

            // 指定 deNetworkDataset 的是否支持转向模型
            deNetworkDataset.SupportsTurns = true;

            // ------------------------------------------------------------------------
            // 添加 NetworkDataset 属性
            // ------------------------------------------------------------------------
            IArray attributeArray = new ArrayClass();

            // ----- 网络属性 -----
            // 创建 EvaluatedNetworkAttribute 对象, 并进行设置.
            IEvaluatedNetworkAttribute evaluatedNetworkAtrribute = new EvaluatedNetworkAttributeClass();
            INetworkAttribute2 networkAttribute2 = (INetworkAttribute2)evaluatedNetworkAtrribute;
            networkAttribute2.Name = costAttribute_name;
            networkAttribute2.UsageType = esriNetworkAttributeUsageType.esriNAUTCost;
            networkAttribute2.Units = esriNetworkAttributeUnits.esriNAUUnknown;
            networkAttribute2.DataType = esriNetworkAttributeDataType.esriNADTDouble;
            networkAttribute2.UseByDefault = true;

            // 创建 NetworkFieldEvaluator 对象, 基于 EvaluatedNetworkAttribute 对象, 对其进行设置.
            INetworkFieldEvaluator networkFieldEvaluator = new NetworkFieldEvaluatorClass();
            networkFieldEvaluator.SetExpression("[" + costAttribute_name + "]", "");
            INetworkEvaluator field_networkEvaluator = (INetworkEvaluator)networkFieldEvaluator;
            evaluatedNetworkAtrribute.set_Evaluator(networkSource, esriNetworkEdgeDirection.esriNEDAlongDigitized, field_networkEvaluator);
            evaluatedNetworkAtrribute.set_Evaluator(networkSource, esriNetworkEdgeDirection.esriNEDAgainstDigitized, field_networkEvaluator);

            INetworkConstantEvaluator networkConstantEvaluator = new NetworkConstantEvaluatorClass();
            networkConstantEvaluator.ConstantValue = 0;
            INetworkEvaluator constant_networkEvaluator = (INetworkEvaluator)networkConstantEvaluator;
            evaluatedNetworkAtrribute.set_DefaultEvaluator(esriNetworkElementType.esriNETEdge, constant_networkEvaluator);
            evaluatedNetworkAtrribute.set_DefaultEvaluator(esriNetworkElementType.esriNETJunction, constant_networkEvaluator);
            evaluatedNetworkAtrribute.set_DefaultEvaluator(esriNetworkElementType.esriNETTurn, constant_networkEvaluator);

            // 在 attributeArray 中添加属性
            attributeArray.Add(evaluatedNetworkAtrribute);
            deNetworkDataset.Attributes = attributeArray;

            // ------------------------------------------------------------------------
            // 创建与构建 Network Dataset
            // ------------------------------------------------------------------------
            // 创建一个 UID, 用来引用 NetworkDatasetWorkspaceExtension.
            //UID networkDatasetWorkspaceExtension_UID = new UIDClass();
            //networkDatasetWorkspaceExtension_UID.Value = "esriGeoDatabase.NetworkDatasetWorkspaceExtension";

            // 获取 workspace extension, 基于 DENetworkDataset 创建 NetworkDataset.
            //IWorkspaceExtensionManager workspaceExtensionManager = workspace as IWorkspaceExtensionManager;
            //IWorkspaceExtension workspaceExtension = workspaceExtensionManager.FindExtension(networkDatasetWorkspaceExtension_UID);
            //IDatasetContainer3 datasetContainer3 = (IDatasetContainer3)workspaceExtension;
            IDEDataset deDataset = (IDEDataset)deNetworkDataset;
            INetworkDataset networkDataset = (INetworkDataset)datasetContainer3.CreateDataset(deDataset);

            // 构建网络
            INetworkBuild networkBuild = (INetworkBuild)networkDataset;
            networkBuild.BuildNetwork(geoDataset.Extent);

            // 释放COM占用的内存
            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(featureClass);
            Marshal.ReleaseComObject(workspaceExtension);
            Marshal.ReleaseComObject(networkDataset);
        }

        /// <summary>
        /// 获得Geodatabase工作空间的网络数据集数量
        /// </summary>
        /// <param name="networkDataset_path">网络数据集文件所在路径</param>
        /// <param name="featureDataset_name">Geodatabase路径的要素数据集名</param>
        /// <returns>无法找到要素数据集返回:-1</returns>
        public int GetNetworkDatasetCount(
            string networkDataset_path,
            string featureDataset_name)
        {
            if (String.IsNullOrWhiteSpace(networkDataset_path) ||
                String.IsNullOrWhiteSpace(featureDataset_name))
                return -1;

            string extension = System.IO.Path.GetExtension(networkDataset_path);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            else
                return -1;

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(networkDataset_path, 0);
            }
            catch (Exception)
            {
                Marshal.ReleaseComObject(workspaceFactory);
                return -1;
            }

            IDatasetContainer3 datasetContainer3 = null;
            if (extension == ".gdb" || extension == ".mdb")
            {
                IWorkspace2 workspace2 = (IWorkspace2)workspace;
                if (!workspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featureDataset_name))
                    return -1;

                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
                IFeatureDataset featureDataset = featureWorkspace.OpenFeatureDataset(featureDataset_name);
                IFeatureDatasetExtensionContainer featureDatasetExtensionContainer =
                    (IFeatureDatasetExtensionContainer)featureDataset;
                IFeatureDatasetExtension featureDatasetExtension =
                    featureDatasetExtensionContainer.FindExtension(esriDatasetType.esriDTNetworkDataset);
                datasetContainer3 = featureDatasetExtension as IDatasetContainer3;

                Marshal.ReleaseComObject(featureDataset);
            }
            else if (extension == "")
            {
                IWorkspaceExtensionManager workspaceExtensionManager = (IWorkspaceExtensionManager)workspace;
                UID networkID = new UIDClass();
                networkID.Value = "esriGeoDatabase.NetworkDatasetWorkspaceExtension";
                IWorkspaceExtension workspaceExtension = workspaceExtensionManager.FindExtension(networkID);
                datasetContainer3 = workspaceExtension as IDatasetContainer3;
            }

            if (datasetContainer3 == null)
                return -1;

            int networkDatasetCount = datasetContainer3.get_DatasetCount(esriDatasetType.esriDTNetworkDataset);

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(datasetContainer3);

            return networkDatasetCount;
        }

        /// <summary>
        /// 建立路线网络分析图层并设置其分析属性
        /// </summary>
        /// <param name="networkDataset">网络数据集</param>
        /// <param name="layer_name">路线网络分析图层名</param>
        /// <param name="impedanceAttribute">成本字段属性</param>
        /// <returns></returns>
        public INALayer2 MakeRouteLayer(
            INetworkDataset networkDataset,
            string layer_name,
            string impedanceAttribute)
        {
            // 通过 网络数据集(networkDataset) 获得 数据元素网络数据集(deNetworkDataset)
            IDatasetComponent datasetComponent = (IDatasetComponent)networkDataset;
            IDENetworkDataset deNetworkDataset = (IDENetworkDataset)datasetComponent.DataElement;

            // 创建并设置 网络分析路径解算器(naRouteSolver)
            INASolver routeSolver = new NARouteSolverClass();
            INASolverSettings naSolverSettings = (INASolverSettings)routeSolver;
            naSolverSettings.ImpedanceAttributeName = impedanceAttribute;

            // 通过 网络分析路径解算器(naRouteSolver) 创建与设置 网络分析环境(naContext)
            INAContext context = routeSolver.CreateContext(deNetworkDataset, routeSolver.Name);
            INAContextEdit contextEdit = (INAContextEdit)context;
            IGPMessages gpMessages = new GPMessagesClass();
            contextEdit.Bind(networkDataset, gpMessages);

            var naLayer = (INALayer2)routeSolver.CreateLayer(context);

            return naLayer;
        }

        /// <summary>
        /// 打开Geodatabase工作空间的网络数据集
        /// </summary>
        /// <param name="networkDataset_path">网络数据集所在路径</param>
        /// <param name="networkDataset_name">网络数据集名</param>
        /// <param name="featureDataset_name">若所在路径为空间数据库,其要素数据集名</param>
        /// <returns></returns>
        public INetworkDataset OpenNetworkDataset(
            string networkDataset_path,
            string networkDataset_name,
            string featureDataset_name)
        {
            if (String.IsNullOrWhiteSpace(networkDataset_path) ||
                String.IsNullOrWhiteSpace(networkDataset_name))
                return null;

            string extension = System.IO.Path.GetExtension(networkDataset_path);
            Type factoryType = null;
            if (extension == ".gdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.FileGDBWorkspaceFactory");
            else if (extension == ".mdb")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesGDB.AccessWorkspaceFactory");
            else if (extension == "")
                factoryType = Type.GetTypeFromProgID("esriDataSourcesFile.ShapefileWorkspaceFactory");
            else
                return null;

            IWorkspaceFactory workspaceFactory = (IWorkspaceFactory)Activator.CreateInstance(factoryType);
            IWorkspace workspace = null;
            try
            {
                workspace = workspaceFactory.OpenFromFile(networkDataset_path, 0);
            }
            catch (Exception)
            {
                Marshal.ReleaseComObject(workspaceFactory);
                throw;
            }

            IWorkspace2 workspace2 = (IWorkspace2)workspace;
            if (!workspace2.get_NameExists(esriDatasetType.esriDTNetworkDataset, networkDataset_name))
                return null;

            IDatasetContainer3 datasetContainer3 = null;
            if (extension == ".gdb" || extension == ".mdb")
            {
                if (!workspace2.get_NameExists(esriDatasetType.esriDTFeatureDataset, featureDataset_name))
                    return null;
                IFeatureWorkspace featureWorkspace = (IFeatureWorkspace)workspace;
                IFeatureDataset featureDataset = featureWorkspace.OpenFeatureDataset(featureDataset_name);
                IFeatureDatasetExtensionContainer featureDatasetExtensionContainer =
                    (IFeatureDatasetExtensionContainer)featureDataset;
                IFeatureDatasetExtension featureDatasetExtension =
                    featureDatasetExtensionContainer.FindExtension(esriDatasetType.esriDTNetworkDataset);
                datasetContainer3 = featureDatasetExtension as IDatasetContainer3;

                Marshal.ReleaseComObject(featureDataset);
            }
            else if (extension == "")
            {
                IWorkspaceExtensionManager workspaceExtensionManager = (IWorkspaceExtensionManager)workspace;
                UID networkID = new UIDClass();
                networkID.Value = "esriGeoDatabase.NetworkDatasetWorkspaceExtension";
                IWorkspaceExtension workspaceExtension = workspaceExtensionManager.FindExtension(networkID);
                datasetContainer3 = workspaceExtension as IDatasetContainer3;
            }

            if (datasetContainer3 == null)
                return null;

            IDataset dataset = datasetContainer3.get_DatasetByName(esriDatasetType.esriDTNetworkDataset, networkDataset_name);
            INetworkDataset networkDataset = (INetworkDataset)dataset;

            // Some methods, such as INASolver.Bind, require an IDENetworkDataset.
            // You can access the DataElement from the network dataset via the IDatasetComponent interface
            //IDatasetComponent datasetComponent = (IDatasetComponent)networkDataset;
            //IDENetworkDataset deNetworkDataset = (IDENetworkDataset)datasetComponent.DataElement;

            Marshal.ReleaseComObject(workspaceFactory);
            Marshal.ReleaseComObject(workspace);
            Marshal.ReleaseComObject(datasetContainer3);

            return networkDataset;
        }

        /// <summary>
        /// 解算简单路径设置
        /// </summary>
        /// <param name="networkDataset">网络数据集</param>
        /// <param name="stopsFeatureClass">停靠点要素类</param>
        /// <param name="impedanceAttribute">阻抗属性字段</param>
        public IFeatureClass SolveSimpleRouteSteup(
            INetworkDataset networkDataset,
            IFeatureClass stopsFeatureClass,
            string impedanceAttribute)
        {
            // 通过 网络数据集(networkDataset) 获得 数据元素网络数据集(deNetworkDataset)
            IDatasetComponent datasetComponent = (IDatasetComponent)networkDataset;
            IDENetworkDataset deNetworkDataset = (IDENetworkDataset)datasetComponent.DataElement;

            // 创建并设置 网络分析路径解算器(naRouteSolver)
            INASolver routeSolver = new NARouteSolverClass();
            INASolverSettings naSolverSettings = (INASolverSettings)routeSolver;
            naSolverSettings.ImpedanceAttributeName = impedanceAttribute;

            // 通过 网络分析路径解算器(naRouteSolver) 创建与设置 网络分析环境(naContext)
            INAContext context = routeSolver.CreateContext(deNetworkDataset, routeSolver.Name);
            INAContextEdit contextEdit = context as INAContextEdit;
            IGPMessages gpMessages = new GPMessagesClass();
            contextEdit.Bind(networkDataset, gpMessages);

            // 通过 NAClassLoader 加载 Stops点要素
            ICursor cursor = stopsFeatureClass.Search(null, false) as ICursor;
            INAClassLoader classLoader = new NAClassLoaderClass() as INAClassLoader;
            classLoader.NAClass = context.NAClasses.get_ItemByName("Stops") as INAClass;
            classLoader.Locator = context.Locator;
            int rowsInCursor = 0;
            int rowsLocated = 0;
            classLoader.Load(cursor, null, ref rowsInCursor, ref rowsLocated);

            // 在当前设置下解算路径，
            // 如果遇到异常，可以在添加try-catch，并查看GPMessages以获取有关失败原因的具体信息。 
            // 可以在成功完成解算后查看GPMessages, 以便获得更多消息。
            bool partialSolution = routeSolver.Solve(context, gpMessages, null);

            // 获得包含解算路径的FeatureClass，可以查看属性信息
            var routesClass = context.NAClasses.get_ItemByName("Routes") as IFeatureClass;
            //IFeatureClass routesClass = null;

            Marshal.ReleaseComObject(deNetworkDataset);
            Marshal.ReleaseComObject(routeSolver);
            Marshal.ReleaseComObject(gpMessages);
            Marshal.ReleaseComObject(cursor);
            Marshal.ReleaseComObject(classLoader);

            return routesClass;
        }
    }

    public class SerializeTools
    {
        /// <summary>
        /// ArcObject特定对象序列化为byte数组
        /// 支持序列化的ArcObject特定对象详见官方帮助
        /// </summary>
        /// <param name="arcObject">ArcObjec对象</param>
        /// <param name="arcBytes">序列化的byte数组</param>
        /// <returns></returns>
        public bool ArcObjectToByte(object arcObject, ref byte[] arcBytes)
        {
            // 判断是否支持IPersistStream接口
            if (arcObject is IPersistStream)
            {
                IMemoryBlobStream memoryBlobStream = new MemoryBlobStream();

                IObjectStream objectStream = new ObjectStreamClass();
                objectStream.Stream = memoryBlobStream;

                IPersistStream persistStream = (IPersistStream)arcObject;
                persistStream.Save((IStream)objectStream, 0);

                object tmp_object = new object();
                var memoryBolbStreamVariant = (IMemoryBlobStreamVariant)memoryBlobStream;
                memoryBolbStreamVariant.ExportToVariant(out tmp_object);

                arcBytes = tmp_object as byte[]; // 强制转换为Byte数组，并返回

                return true;
            }

            return false;
        }

        /// <summary>
        /// byte数组反序列化为ArcObject特定类对象
        /// </summary>
        /// <param name="arcBytes">序列化的byte数组</param>
        /// <param name="arcObject">ArcObject特定类对象</param>
        /// <returns></returns>
        public bool ByteToArcObject(byte[] arcByte, ref object arcObject)
        {
            if (arcByte.Length > 0)
            {
                IMemoryBlobStream memoryBlobStream = new MemoryBlobStreamClass();
                var memoryBlobStreamVariant = (IMemoryBlobStreamVariant)memoryBlobStream;
                memoryBlobStreamVariant.ImportFromVariant((object)arcByte);

                IObjectStream objectStream = new ObjectStreamClass();
                objectStream.Stream = memoryBlobStream;

                IPersistStream persistStream = (IPersistStream)arcObject;
                persistStream.Load((IStream)objectStream);

                return true;
            }

            return false;

        }

        /// <summary>
        /// ArcObject特定对象序列化为文件
        /// </summary>
        /// <param name="arcObject">ArcObjec对象</param>
        /// <param name="file">文件全名</param>
        /// <returns></returns>
        public bool ArcObjectToFile(object arcObject, string file)
        {
            // 判断是否支持IPersistStream接口
            if (arcObject is IPersistStream)
            {
                IMemoryBlobStream memoryBlobStream = new MemoryBlobStream();

                IObjectStream objectStream = new ObjectStreamClass();
                objectStream.Stream = memoryBlobStream;

                IPersistStream persistStream = (IPersistStream)arcObject;
                persistStream.Save((IStream)objectStream, 0);

                memoryBlobStream.SaveToFile(file);

                return true;
            }

            return false;
        }

        /// <summary>
        /// 文件反序列化为ArcObject特定类对象
        /// </summary>
        /// <param name="file">文件全名</param>
        /// <param name="arcObject">ArcObjec对象</param>
        /// <returns></returns>
        public bool FileToArcObject(string file, ref object arcObject)
        {
            if (System.IO.File.Exists(file))
            {
                IMemoryBlobStream memoryBlobStream = new MemoryBlobStreamClass();
                memoryBlobStream.LoadFromFile(file);

                IObjectStream objectStream = new ObjectStreamClass();
                objectStream.Stream = memoryBlobStream;

                IPersistStream persistStream = arcObject as IPersistStream;
                persistStream.Load((IStream)objectStream);

                return true;
            }

            return false;
        }

        /// <summary>
        /// ArcObject特定对象序列化为XML字符串
        /// </summary>
        /// <param name="arcObject">ArcObjec对象</param>
        /// <param name="xml">XML字符串</param>
        /// <returns></returns>
        public bool ArcOjbectToXMLString(object arcObject, ref string xml)
        {
            // 判断是否支持IXMLSerializer接口
            if (arcObject is IXMLSerializer)
            {
                IXMLStream xmlStream = new XMLStreamClass();
                IXMLWriter xmlWriter = new XMLWriterClass();
                xmlWriter.WriteTo((IStream)xmlStream);

                IXMLSerializer xmlSerializer = new XMLSerializerClass();
                xmlSerializer.WriteObject(xmlWriter, null, null, "", "", arcObject);

                xml = xmlStream.SaveToString();

                return true;
            }

            return false;
        }

        /// <summary>
        /// XML字符串反序列化ArcObject特定对象
        /// </summary>
        /// <param name="xml">XML字符串</param>
        /// <param name="arcObject">ArcObjec对象</param>
        /// <returns></returns>
        public bool XMLStringToArcOjbect(string xml, ref object arcObject)
        {
            if (string.IsNullOrWhiteSpace(xml))
            {
                IXMLStream xmlStream = new XMLStreamClass();
                xmlStream.LoadFromString(xml);

                IXMLReader xmlReader = new XMLReaderClass();
                xmlReader.ReadFrom((IStream)xmlStream);

                IXMLSerializer xmlSerializer = new XMLSerializerClass();
                arcObject = xmlSerializer.ReadObject(xmlReader, null, null);

                return true;
            }

            return false;
        }
    }

    public class SpatialAnalystTools
    {
        /// <summary>
        /// 提取矩形内的栅格数据集
        /// </summary>
        /// <param name="rasterGeoDataset">栅格数据集</param>
        /// <param name="envelope">矩形</param>
        /// <param name="isInside">内部提取</param>
        /// <param name="extent">处理范围</param>
        /// <param name="snap_raster">捕捉栅格</param>
        public IGeoDataset ExtractByRectangle(
            IGeoDataset rasterGeoDataset,
            IEnvelope envelope,
            bool isInside,
            object extent = null,
            object snap_raster = null)
        {
            IExtractionOp extractionOp = new RasterExtractionOpClass();
            var rasterAnaysisEnvironment = (IRasterAnalysisEnvironment)extractionOp;
            if (extent == null) extent = rasterGeoDataset.Extent;
            if (snap_raster == null) snap_raster = rasterGeoDataset;
            rasterAnaysisEnvironment.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, ref extent, ref snap_raster);
            rasterAnaysisEnvironment.OutSpatialReference = rasterGeoDataset.SpatialReference;
            IGeoDataset outGeoDataset = extractionOp.Rectangle(rasterGeoDataset, envelope, isInside);

            Marshal.ReleaseComObject(extractionOp);

            return outGeoDataset;
        }

        /// <summary>
        /// 提取多边形范围内的栅格数据集
        /// </summary>
        /// <param name="rasterGeoDataset">栅格数据集</param>
        /// <param name="polygon">多边形</param>
        /// /<param name="isInsid">内部提取</param>
        /// <param name="extent">处理范围</param>
        /// <param name="snap_raster">捕捉栅格</param>
        public IGeoDataset ExtractByPolygon(
            IGeoDataset rasterGeoDataset,
            IPolygon polygon,
            bool isInside,
            object extent = null,
            object snap_raster = null)
        {
            IExtractionOp extractionOp = new RasterExtractionOpClass();
            var rasterAnaysisEnvironment = (IRasterAnalysisEnvironment)extractionOp;
            if (extent == null) extent = rasterGeoDataset.Extent;
            if (snap_raster == null) snap_raster = rasterGeoDataset;
            rasterAnaysisEnvironment.SetExtent(esriRasterEnvSettingEnum.esriRasterEnvValue, ref extent, ref snap_raster);
            rasterAnaysisEnvironment.OutSpatialReference = rasterGeoDataset.SpatialReference;
            IGeoDataset outGeoDataset = extractionOp.Polygon(rasterGeoDataset, polygon, isInside);

            Marshal.ReleaseComObject(outGeoDataset);

            return outGeoDataset;
        }

        /// <summary>
        /// 反距离权重插值
        /// </summary>
        /// <param name="featureClass">点要素类</param>
        /// <param name="z_value_field">作为z值的要素类字段</param>
        /// <param name="out_path">输出路径</param>
        /// <param name="out_name">输出栅格数据名</param>
        /// <param name="out_cellSize">输出栅格像元大小</param>
        /// <param name="out_cellSize">输出栅格数据覆盖范围</param>
        public IGeoDataset IDW(
            IFeatureClass featureClass,
            IQueryFilter queryFilter,
            string z_value_field,
            object out_cellSize,
            object out_extent,
            object out_snapRaster)
        {
            if (featureClass.ShapeType != esriGeometryType.esriGeometryPoint)
                return null;

            object maxDistance = null;
            double power = 2;
            object barrier = null;

            if (out_extent == null)
                out_extent = featureClass;

            IFeatureClassDescriptor featureClassDescriptor = new FeatureClassDescriptorClass();
            featureClassDescriptor.Create(featureClass, queryFilter, z_value_field);

            IRasterRadius rasterRadius = new RasterRadiusClass();
            rasterRadius.SetVariable(12, ref maxDistance);

            IInterpolationOp interpolationOp = new RasterInterpolationOpClass();
            var rasterAnalysisEnvironment = (IRasterAnalysisEnvironment)interpolationOp;
            rasterAnalysisEnvironment.SetCellSize(
                esriRasterEnvSettingEnum.esriRasterEnvValue,
                ref out_cellSize);

            rasterAnalysisEnvironment.SetExtent(
                esriRasterEnvSettingEnum.esriRasterEnvValue,
                ref out_extent,
                ref out_snapRaster);

            IGeoDataset rasterGeoDataset = interpolationOp.IDW(
                (IGeoDataset)featureClassDescriptor,
                power,
                rasterRadius,
                ref barrier);

            Marshal.ReleaseComObject(featureClassDescriptor);
            Marshal.ReleaseComObject(rasterRadius);
            Marshal.ReleaseComObject(interpolationOp);

            return rasterGeoDataset;
        }
    }

    public class SpatialStatisticsTools
    {
        /// <summary>
        /// 将FeatureClass的属性输出为*.txt文件
        /// </summary>
        /// <param name="featureClass">要素类</param>
        /// <param name="text_path">文本文件路径</param>
        /// <param name="text_name">文本文件名(不含.txt扩展名)</param>
        public void ExportFeatureAttributeToText(
            IFeatureClass featureClass, string text_path, string text_name)
        {
            if (featureClass == null ||
                !Directory.Exists(text_path) ||
                string.IsNullOrWhiteSpace(text_name))
                return;

            string text_file = text_path + "\\" + text_name + ".txt";
            StreamWriter streamWiter = null;
            if (File.Exists(text_file))
                File.Delete(text_file);

            streamWiter = File.CreateText(text_file);

            string delimiter = ",";
            ITable table = (ITable)featureClass;

            // 获得属性字段名
            string columnHeaderString = "";
            IField field = null;
            for (int i = 0; i < table.Fields.FieldCount; i++)
            {
                if (i > 0) columnHeaderString += delimiter;
                field = table.Fields.get_Field(i);

                string columnName = field.Name;
                if (field.Type == esriFieldType.esriFieldTypeGeometry)
                {
                    columnName = "X,Y";
                }

                columnHeaderString += columnName;
            }
            streamWiter.WriteLine(columnHeaderString);

            // 获得具体内容
            ICursor cursor = table.Search(null, true);
            IRow row = cursor.NextRow();
            while (row != null)
            {
                string rowString = "";
                for (int fieldIndex = 0; fieldIndex < row.Fields.FieldCount; fieldIndex++)
                {
                    if (fieldIndex > 0) rowString += delimiter;
                    field = row.Fields.get_Field(fieldIndex);

                    if (field.Type == esriFieldType.esriFieldTypeGeometry)
                    {
                        if (field.GeometryDef.GeometryType == esriGeometryType.esriGeometryPoint)
                        {
                            IPoint point = (IPoint)row.get_Value(fieldIndex);
                            rowString += point.X.ToString() + delimiter + point.Y.ToString();
                        }
                        else if (field.GeometryDef.GeometryType == esriGeometryType.esriGeometryPolyline)
                        {
                            IPoint point = new PointClass();
                            IPolyline polyline = (IPolyline5)row.get_Value(fieldIndex);
                            polyline.QueryPoint(esriSegmentExtension.esriNoExtension, polyline.Length / 2, false, point);
                            rowString += point.X.ToString() + delimiter + point.Y.ToString();
                        }
                        else if (field.GeometryDef.GeometryType == esriGeometryType.esriGeometryPolygon)
                        {
                            var polygon = (IPolygon)row.get_Value(fieldIndex);
                            var area = (IArea)polygon;
                            IPoint point = area.Centroid;
                            rowString += point.X.ToString() + delimiter + point.Y.ToString();
                        }
                    }
                    else if (field.Name == "Locations" && field.Type == esriFieldType.esriFieldTypeBlob)
                    {
                        StringBuilder stringBuffer = new StringBuilder();
                        var naLocRangesObject = row as INALocationRangesObject;
                        if (naLocRangesObject == null)
                            rowString += row.get_Value(fieldIndex).ToString();

                        var naLocRanges = naLocRangesObject.NALocationRanges;
                        if (naLocRanges == null)
                            rowString += row.get_Value(fieldIndex).ToString();

                        stringBuffer.Append("{Junctions:{");
                        long junctionCount = naLocRanges.JunctionCount;
                        int junctionEID = -1;
                        for (int i = 0; i < junctionCount; i++)
                        {
                            naLocRanges.QueryJunction(i, ref junctionEID);
                            if (i > 0) stringBuffer.Append(";");

                            stringBuffer.Append("{");
                            stringBuffer.Append(junctionEID);
                            stringBuffer.Append("}");
                        }
                        stringBuffer.Append("}");

                        stringBuffer.Append(",EdgeRanges:{");
                        long edgeRangeCount = naLocRanges.EdgeRangeCount;
                        int edgeEID = -1;
                        double fromPosition, toPosition;
                        fromPosition = toPosition = -1;
                        esriNetworkEdgeDirection edgeDirection = esriNetworkEdgeDirection.esriNEDNone;
                        for (int i = 0; i < edgeRangeCount; i++)
                        {
                            naLocRanges.QueryEdgeRange(i, ref edgeEID, ref edgeDirection, ref fromPosition, ref toPosition);

                            string directionValue = "";
                            if (edgeDirection == esriNetworkEdgeDirection.esriNEDAlongDigitized) directionValue = "Along Digitized";
                            else if (edgeDirection == esriNetworkEdgeDirection.esriNEDAgainstDigitized) directionValue = "Against Digitized";

                            if (i > 0) stringBuffer.Append(";");
                            stringBuffer.Append("{");
                            stringBuffer.Append(edgeEID);
                            stringBuffer.Append(";");
                            stringBuffer.Append(directionValue);
                            stringBuffer.Append(";");
                            stringBuffer.Append(fromPosition);
                            stringBuffer.Append(";");
                            stringBuffer.Append(toPosition);
                            stringBuffer.Append("}");
                        }
                        stringBuffer.Append("}");

                        rowString += stringBuffer.ToString();
                    }
                    else
                    {
                        rowString += row.get_Value(fieldIndex).ToString();
                    }
                } //for (int fieldIndex = 0; fieldIndex < row.Fields.FieldCount; fieldIndex++)

                streamWiter.WriteLine(rowString);
                row = cursor.NextRow();

            } //while (row != null)

            streamWiter.Close();

            Marshal.ReleaseComObject(cursor);
        }

    }
}