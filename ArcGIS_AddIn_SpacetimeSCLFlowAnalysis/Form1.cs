using ArcObjectToolbox;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Catalog;
using ESRI.ArcGIS.CatalogUI;
using ESRI.ArcGIS.DataSourcesGDB;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Framework;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.SystemUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpacetimeSCLFlowAnalysis
{
    public partial class Form1 : Form
    {
        List<ITable> FlowTableList = null;
        List<IFeatureClass> OriginFeatureClassList = null;
        List<IFeatureClass> DestFeatureClassList = null;

        ITable FlowTable = null;
        IFeatureClass OriginFeatureClass = null;
        IFeatureClass DestFeatureClass = null;

        // 递归获取 GroupLayer 中的图层
        private List<IFeatureLayer> GetFeatureLayerFromGroupLayer(IGroupLayer groupLayer)
        {
            List<IFeatureLayer> outLayerList = new List<IFeatureLayer>();
            var compositeLayer = (ICompositeLayer)groupLayer;
            for (int j = 0; j < compositeLayer.Count; j++)
            {
                var subLayer = compositeLayer.Layer[j];
                if (subLayer is IFeatureLayer)
                {
                    var featureLayer = (IFeatureLayer)subLayer;
                    var featureClass = featureLayer.FeatureClass;
                    if (featureClass == null) continue;
                    if (featureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                        outLayerList.Add(featureLayer);
                }
                else if (subLayer is IGroupLayer)
                {
                    GetFeatureLayerFromGroupLayer((IGroupLayer)subLayer).ForEach(p => outLayerList.Add(p));
                }
            }

            return outLayerList;
        }

        public Form1()
        {
            InitializeComponent();

            FlowTableList = new List<ITable>();
            OriginFeatureClassList = new List<IFeatureClass>();
            DestFeatureClassList = new List<IFeatureClass>();

        }

        private void cmbFlowTable_DropDown(object sender, EventArgs e)
        {
            FlowTable = null;
            FlowTableList.Clear();
            cmbFlowTable.Items.Clear();

            var documentDsets = (IDocumentDatasets)ArcMap.Document;
            var enumDatasets = documentDsets.Datasets;
            IDataset dataset = null;
            while ((dataset = enumDatasets.Next()) != null)
            {
                try
                {
                    if (dataset.Type == esriDatasetType.esriDTTable)
                    {
                        FlowTableList.Add((ITable)dataset);
                        cmbFlowTable.Items.Add(dataset.Name);
                    }

                    if (dataset.Type == esriDatasetType.esriDTFeatureClass)
                    {
                        var table = (ITable)dataset;
                        for (int i = 0; i < table.Fields.FieldCount; i++)
                        {
                            var field = table.Fields.Field[i];
                            if (field.Type == esriFieldType.esriFieldTypeGeometry &&
                                field.GeometryDef.GeometryType == esriGeometryType.esriGeometryPolyline)
                            {
                                FlowTableList.Add((ITable)dataset);
                                cmbFlowTable.Items.Add(dataset.Name);
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private void cmbFlowTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            FlowTable = FlowTableList[cmbFlowTable.SelectedIndex];

            cmbFlowOrigin.Items.Clear(); // 清空Origin字段的控件选择
            cmbFlowOrigin.Text = "";

            cmbFlowOriginTime.Items.Clear(); // 清空Origin时间字段的控件选择
            cmbFlowOriginTime.Text = "";
            cmbFlowOriginTime.Items.Add("");

            cmbFlowDest.Items.Clear(); // 清空Destination字段的控件选择
            cmbFlowDest.Text = "";

            cmbFlowDestTime.Items.Clear(); // 清空Destination时间字段的控件选择
            cmbFlowDestTime.Text = "";
            cmbFlowDestTime.Items.Add("");

            cmbFlowValue.Items.Clear(); // 清空flow attribute value字段的控件选择
            cmbFlowValue.Text = "";

            var flowTable = (ITable)FlowTable;
            for (int i = 0; i < flowTable.Fields.FieldCount; i++)
            {
                var tableField = flowTable.Fields.Field[i];

                cmbFlowOrigin.Items.Add(tableField.Name); // 添加Origin字段的控件选择
                cmbFlowDest.Items.Add(tableField.Name); // 添加Destination字段的控件选择

                if (tableField.Type == esriFieldType.esriFieldTypeDate)
                {
                    cmbFlowOriginTime.Items.Add(tableField.Name); // 添加Origin时间字段的控件选择
                    cmbFlowDestTime.Items.Add(tableField.Name); // 添加Destination时间字段的控件选择
                }

                if (tableField.Type == esriFieldType.esriFieldTypeDouble ||  // double
                    tableField.Type == esriFieldType.esriFieldTypeInteger || // long int
                    tableField.Type == esriFieldType.esriFieldTypeSingle || // float
                    tableField.Type == esriFieldType.esriFieldTypeSmallInteger) // short int
                {
                    cmbFlowValue.Items.Add(tableField.Name);
                }
            }

            var dataset = (IDataset)flowTable;
            txtOutPath.Text = dataset.Workspace.PathName;
            txtOutName.Text = dataset.Name + "_SCLF";
        }

        private void cmbFlowTable_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbFlowOrigin_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbFlowOriginTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFlowOriginTime.Text != "" && cmbFlowDestTime.Text != "")
            {
                txtTimeDistance.Enabled = true;
                cmbTimeUnit.Enabled = true;
            }
            else
            {
                txtTimeDistance.Enabled = false;
                cmbTimeUnit.Enabled = false;
            }
        }

        private void cmbFlowOriginTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbFlowDest_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbFlowDestTime_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFlowOriginTime.Text != "" && cmbFlowDestTime.Text != "")
            {
                txtTimeDistance.Enabled = true;
                cmbTimeUnit.Enabled = true;
            }
            else
            {
                txtTimeDistance.Enabled = false;
                cmbTimeUnit.Enabled = false;
            }
        }

        private void cmbFlowDestTime_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbFlowAttrValue_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbOriginFeatures_DropDown(object sender, EventArgs e)
        {
            OriginFeatureClass = null;
            OriginFeatureClassList.Clear();
            cmbOriginFeatures.Items.Clear();

            var enumLayer = ArcMap.Document.FocusMap.Layers[null, true];
            ILayer layer = null;
            while ((layer = enumLayer.Next()) != null)
            {
                if (layer is IFeatureLayer)
                {
                    var featureLayer = (IFeatureLayer)layer;
                    var featureClass = featureLayer.FeatureClass;
                    if (featureClass == null) continue;
                    if (featureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        OriginFeatureClassList.Add(featureClass);
                        cmbOriginFeatures.Items.Add(featureLayer.Name);
                    }
                }
                else if (layer is IGroupLayer)
                {
                    GetFeatureLayerFromGroupLayer((IGroupLayer)layer).ForEach(p =>
                    {
                        OriginFeatureClassList.Add(p.FeatureClass);
                        cmbOriginFeatures.Items.Add(p.Name);
                    });
                }
            }
        }

        private void cmbOriginFeatures_SelectedIndexChanged(object sender, EventArgs e)
        {
            OriginFeatureClass = OriginFeatureClassList[cmbOriginFeatures.SelectedIndex];

            cmbOriginField.Items.Clear(); // 清空Feature Origin字段的控件选择
            cmbOriginField.Text = "";

            var table = (ITable)OriginFeatureClass;
            for (int i = 0; i < table.Fields.FieldCount; i++)
            {
                var tableField = table.Fields.Field[i];
                if (tableField.Type != esriFieldType.esriFieldTypeDate)
                {
                    cmbOriginField.Items.Add(tableField.Name); // 添加Feature UID字段的控件选择
                }
            }
        }

        private void cmbOriginFeatures_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbDestFeatures_DropDown(object sender, EventArgs e)
        {
            DestFeatureClass = null;
            DestFeatureClassList.Clear();
            cmbDestFeatures.Items.Clear();

            var enumLayer = ArcMap.Document.FocusMap.Layers[null, true];
            ILayer layer = null;
            while ((layer = enumLayer.Next()) != null)
            {
                if (layer is IFeatureLayer)
                {
                    var featureLayer = (IFeatureLayer)layer;
                    var featureClass = featureLayer.FeatureClass;
                    if (featureClass == null) continue;
                    if (featureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
                    {
                        DestFeatureClassList.Add(featureClass);
                        cmbDestFeatures.Items.Add(featureLayer.Name);
                    }
                }
                else if (layer is IGroupLayer)
                {
                    GetFeatureLayerFromGroupLayer((IGroupLayer)layer).ForEach(p =>
                    {
                        DestFeatureClassList.Add(p.FeatureClass);
                        cmbDestFeatures.Items.Add(p.Name);
                    });
                }
            }
        }

        private void cmbDestField_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void cmbDestFeatures_SelectedIndexChanged(object sender, EventArgs e)
        {
            DestFeatureClass = DestFeatureClassList[cmbDestFeatures.SelectedIndex];

            cmbDestField.Items.Clear();
            cmbDestField.Text = "";

            var table = (ITable)DestFeatureClass;
            for (int i = 0; i < table.Fields.FieldCount; i++)
            {
                var field = table.Fields.Field[i];
                if (field.Type != esriFieldType.esriFieldTypeDate)
                {
                    cmbDestField.Items.Add(field.Name);
                }
            }
        }

        private void cmbOriginField_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void txtThreshold_TextChanged(object sender, EventArgs e)
        {
            if (txtThreshold.Text == "")
            {
                txtThreshold.Tag = "";
                return;
            }

            Regex regex = new Regex(@"^\+?(0|(0\.)|(0\.\d+))$");
            if (!regex.IsMatch(txtThreshold.Text))
            {
                txtThreshold.Text = (string)txtThreshold.Tag;
                toolTip1.Show("Invalid value, requires a value (0.0 ≤ x < 1.0).", lbl_threshold_error, 1650);
            }
            else
            {
                txtThreshold.Tag = txtThreshold.Text;
            }
        }

        private void txtTimeDistance_TextChanged(object sender, EventArgs e)
        {
            if (txtTimeDistance.Text == "")
            {
                txtTimeDistance.Tag = "";
                return;
            }

            Regex regex = new Regex(@"^\+?\d+?$");
            if (!regex.IsMatch(txtTimeDistance.Text))
            {
                txtTimeDistance.Text = (string)txtTimeDistance.Tag;
                toolTip1.Show("Invalid value, requires a integer (n=1,2,3...).", lbl_distance_error, 1650);
            }
            else
            {
                txtTimeDistance.Tag = txtTimeDistance.Text;
            }

        }

        private void cmbTimeUnit_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void txtOutPath_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;
        }

        private void btnSCLFPath_Click(object sender, EventArgs e)
        {
            IGxDialog gxDialog = new GxDialogClass();
            var gxFilterCol = (IGxObjectFilterCollection)gxDialog;
            gxFilterCol.AddFilter(new GxFilterFileFolder(), false);
            //gxFilterCol.AddFilter(new GxFilterFileGeodatabases(), true);
            //gxFilterCol.AddFilter(new GxFilterFGDBFeatureDatasets(), false);
            gxFilterCol.AddFilter(new GxFilterPersonalGeodatabases(), false);
            gxFilterCol.AddFilter(new GxFilterPGDBFeatureDatasets(), false);
            //gxFilterCol.AddFilter(new GxFilterPGDBTables(), false);
            //gxFilterCol.AddFilter(new GxFilterFeatureClasses(), false);
            gxDialog.Title = " 结果输出路径";
            gxDialog.AllowMultiSelect = false;

            IEnumGxObject gxSelection = null;
            gxDialog.DoModalOpen(ArcMap.Application.hWnd, out gxSelection);

            IGxObject gxObject = null;
            while ((gxObject = gxSelection.Next()) != null)
            {
                txtOutPath.Text = gxObject.FullName;
            }

            //OutputPath = gxDialog.FinalLocation.FullName;
            //OutputName = gxDialog.Name;

            //if (string.IsNullOrWhiteSpace(OutputName))
            //    txtSCLFPath.Text = "";
            //else
            //    txtSCLFPath.Text = OutputPath + @"\" + OutputName;
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var flowOriginField = cmbFlowOrigin.Text;
            var flowDestField = cmbFlowDest.Text;
            var flowOriginTimeField = cmbFlowOriginTime.Text;
            var flowDestTimeField = cmbFlowDestTime.Text;
            var flowValueField = cmbFlowValue.Text;
            var originUIDField = cmbOriginField.Text;
            var destUIDField = cmbDestField.Text;
            var timeUnit = cmbTimeUnit.Text;
            var outputPath = txtOutPath.Text;
            var outputName = txtOutName.Text;

            double threshold = 0;
            if (txtThreshold.Text != "")
                threshold = Convert.ToDouble(txtThreshold.Text);

            int timeDistance = 0;
            if (txtTimeDistance.Text != "")
                timeDistance = Convert.ToInt32(txtTimeDistance.Text);

            if (FlowTable == null ||
                string.IsNullOrWhiteSpace(flowOriginField) ||
                string.IsNullOrWhiteSpace(flowDestField) ||
                string.IsNullOrWhiteSpace(flowValueField) || 
                string.IsNullOrWhiteSpace(originUIDField) ||
                string.IsNullOrWhiteSpace(destUIDField) ||
                string.IsNullOrWhiteSpace(outputPath) ||
                string.IsNullOrWhiteSpace(outputName))
                return;
            
            if (OriginFeatureClass == null || DestFeatureClass == null)
            {
                MessageBox.Show("Error in Origin/Destination Feature Class.", "Error", MessageBoxButtons.OK);
                return;
            }

            // 程序运行进度窗体设置
            ITrackCancel trackCancel = new CancelTrackerClass();
            var progressDialogFactory = (IProgressDialogFactory)new ProgressDialogFactoryClass();
            var progressDialog = (IProgressDialog2)progressDialogFactory.Create(
                trackCancel, ArcMap.Application.hWnd);
            var statusBar = ArcMap.Application.StatusBar;

            progressDialog.CancelEnabled = true;
            progressDialog.Title = "Processing...";
            progressDialog.Description = "Detecting and analysing spatiotemporal self-co-location flow patterns.";
            progressDialog.Animation = esriProgressAnimationTypes.esriProgressSpiral;
            progressDialog.ShowDialog();
            var stepProgressor = (IStepProgressor)progressDialog;

            this.Hide();

            // 空间邻近分析
            var spaceAnalyst = new SpaceAnalyst();
            spaceAnalyst.TrackCancel = trackCancel;
            spaceAnalyst.StepProgressor = stepProgressor;

            var originNearDict = spaceAnalyst.PolygonNeighbors(OriginFeatureClass, originUIDField);
            if (originNearDict == null)
                goto app_end;

            Dictionary<string, List<string>> destNearDict = null;
            if (DestFeatureClass == OriginFeatureClass && destUIDField == originUIDField)
                destNearDict = originNearDict;
            else
                destNearDict = spaceAnalyst.PolygonNeighbors(DestFeatureClass, destUIDField);

            if (destNearDict == null)
                goto app_end;

            // 同位流分析与结果评估
            var sclFlowAnalyst = new SCLFlowAnalyst();
            sclFlowAnalyst.TrackCancel = trackCancel;
            sclFlowAnalyst.StepProgressor = stepProgressor;

            // 读取流数据
            var flowDataTable = sclFlowAnalyst.ReadFlowTable(FlowTable,
                                                             flowOriginField, flowOriginTimeField,
                                                             flowDestField, flowDestTimeField,
                                                             flowValueField,
                                                             timeDistance, timeUnit);

            // 同位流模式分析
            var clusterList = sclFlowAnalyst.PatternAnalysis(flowDataTable, threshold,
                                                              ref originNearDict, ref destNearDict);
            if (clusterList == null)
                goto app_end;

            bool hasTime = true; // 判断是否有时间字段
            if (flowOriginTimeField == "" && flowDestTimeField == "") hasTime = false;

            // 同位流结果评估
            var patternTable = sclFlowAnalyst.PatternEvaluation(clusterList, hasTime);
            if (patternTable == null)
                goto app_end;

            IFeatureClass objectClass = null;
            IFeatureClass flowClass = null;
            string message = "";

            // 生成结果要素类
            sclFlowAnalyst.CreatePatternFeatures(OriginFeatureClass, originUIDField, DestFeatureClass, destUIDField,
                                                 patternTable, outputPath, outputName, ref objectClass, ref flowClass, ref message);
            if (patternTable == null)
            {
                if (message != "")
                    MessageBox.Show(message, "Error", MessageBoxButtons.OK);
                goto app_end;
            }

            // 添加要素类图层
            IFeatureClass[] featureClassArr = new IFeatureClass[2] { objectClass, flowClass };
            for (int i = 0; i < featureClassArr.Length; i++)
            {
                var featureClass = featureClassArr[i];
                IFeatureLayer newFeatureLayer = new FeatureLayerClass();
                newFeatureLayer.FeatureClass = featureClass;
                newFeatureLayer.Name = ((IDataset)featureClass).Name;
                ArcMap.Document.FocusMap.AddLayer(newFeatureLayer);
                var extent = ((IGeoDataset)featureClass).Extent;
                ArcMap.Document.ActiveView.PartialRefresh(esriViewDrawPhase.esriViewGeography, newFeatureLayer, extent);
            }

            ArcMap.Document.UpdateContents();
            var gxApplication = (IGxApplication)ArcMap.Application;
            gxApplication.Refresh(outputPath);

            app_end:
            progressDialog.HideDialog();
            statusBar.ProgressBar.Hide();
            this.Show();
        }

        private void btnFlowTable_Click(object sender, EventArgs e)
        {
            IGxDialog gxDialog = new GxDialogClass();
            var gxFilterCol = (IGxObjectFilterCollection)gxDialog;
            gxFilterCol.AddFilter(new GxFilterTablesClass(), true);
            gxFilterCol.AddFilter(new GxFilterPolylineFeatureClasses(), false);
            gxDialog.AllowMultiSelect = false;
            gxDialog.Title = " 添加表";

            IEnumGxObject gxSelection = null;
            gxDialog.DoModalOpen(ArcMap.Application.hWnd, out gxSelection);
            var gxObject = gxSelection.Next();
            if (gxObject != null)
            {
                cmbFlowOrigin.Text = "";
                cmbFlowOrigin.Items.Clear();
                cmbFlowOriginTime.Text = "";
                cmbFlowOriginTime.Items.Clear();

                cmbFlowDest.Text = "";
                cmbFlowDest.Items.Clear();
                cmbFlowDestTime.Text = "";
                cmbFlowDestTime.Items.Clear();

                cmbFlowValue.Text = "";
                cmbFlowValue.Items.Clear();

                DataManagementTools dataManagementTools = new DataManagementTools();

                string table_path = gxObject.Parent.FullName;
                string table_name = gxObject.Name;
                FlowTable = dataManagementTools.OpenTable(table_path, table_name);

                if (FlowTable == null)
                {
                    cmbFlowTable.Text = "";
                    txtOutPath.Text = "";
                    txtOutName.Text = "";
                    return;
                }

                txtOutPath.Text = table_path;
                txtOutName.Text = table_name + "_SCLF";

                cmbFlowTable.Text = gxObject.FullName;

                for (int i = 0; i < FlowTable.Fields.FieldCount; i++)
                {
                    var field = FlowTable.Fields.Field[i];

                    cmbFlowOrigin.Items.Add(field.Name);
                    cmbFlowDest.Items.Add(field.Name);

                    if (field.Type == esriFieldType.esriFieldTypeDouble ||  // double
                        field.Type == esriFieldType.esriFieldTypeInteger || // long int
                        field.Type == esriFieldType.esriFieldTypeSingle || // float
                        field.Type == esriFieldType.esriFieldTypeSmallInteger) // short int
                        cmbFlowValue.Items.Add(field.Name);

                    if (field.Type == esriFieldType.esriFieldTypeDate)
                    {
                        cmbFlowOriginTime.Items.Add(field.Name);
                        cmbFlowDestTime.Items.Add(field.Name);
                    }
                }
            }
        }

        private void btnOriginFeatures_Click(object sender, EventArgs e)
        {
            IGxDialog gxDialog = new GxDialogClass();
            var gxFilterCol = (IGxObjectFilterCollection)gxDialog;
            gxFilterCol.AddFilter(new GxFilterPolygonFeatureClasses(), false);
            gxDialog.AllowMultiSelect = false;
            gxDialog.Title = " 添加源要素类";

            IEnumGxObject gxSelection = null;
            gxDialog.DoModalOpen(ArcMap.Application.hWnd, out gxSelection);
            var gxObject = gxSelection.Next();
            if (gxObject != null)
            {
                string features_path = gxObject.Parent.FullName;
                string features_name = gxObject.Name;
                string features_parent_path = System.IO.Path.GetDirectoryName(features_path);
                string features_parent_path_ext = System.IO.Path.GetExtension(features_parent_path);
                if (features_parent_path_ext == ".gdb" || features_parent_path_ext == ".mdb")
                    features_path = features_parent_path;

                cmbOriginField.Text = "";
                cmbOriginField.Items.Clear();

                DataManagementTools dataManagementTools = new DataManagementTools();
                OriginFeatureClass = dataManagementTools.OpenFeatureClass(features_path, features_name);

                if (OriginFeatureClass == null)
                {
                    cmbOriginFeatures.Text = "";
                    return;
                }

                cmbOriginFeatures.Text = gxObject.FullName;

                var table = (ITable)OriginFeatureClass;
                for (int i = 0; i < table.Fields.FieldCount; i++)
                {
                    var field = table.Fields.Field[i];
                    if (field.Type != esriFieldType.esriFieldTypeDate)
                    {
                        cmbOriginField.Items.Add(field.Name); // 添加Feature UID字段的控件选择
                    }
                }
            }
        }

        private void btnDestFeatures_Click(object sender, EventArgs e)
        {
            IGxDialog gxDialog = new GxDialogClass();
            var gxFilterCol = (IGxObjectFilterCollection)gxDialog;
            gxFilterCol.AddFilter(new GxFilterPolygonFeatureClasses(), false);
            gxDialog.AllowMultiSelect = false;
            gxDialog.Title = " 添加目标要素类";

            IEnumGxObject gxSelection = null;
            gxDialog.DoModalOpen(ArcMap.Application.hWnd, out gxSelection);
            var gxObject = gxSelection.Next();
            if (gxObject != null)
            {
                string features_path = gxObject.Parent.FullName;
                string features_name = gxObject.Name;
                string features_parent_path = System.IO.Path.GetDirectoryName(features_path);
                string features_parent_path_ext = System.IO.Path.GetExtension(features_parent_path);
                if (features_parent_path_ext == ".gdb" || features_parent_path_ext == ".mdb")
                    features_path = features_parent_path;

                cmbDestFeatures.Text = "";
                cmbDestFeatures.Items.Clear();

                DataManagementTools dataManagementTools = new DataManagementTools();
                DestFeatureClass = dataManagementTools.OpenFeatureClass(features_path, features_name);

                if (DestFeatureClass == null)
                {
                    cmbDestFeatures.Text = "";
                    return;
                }

                cmbDestFeatures.Text = gxObject.FullName;

                var table = (ITable)DestFeatureClass;
                for (int i = 0; i < table.Fields.FieldCount; i++)
                {
                    var field = table.Fields.Field[i];
                    if (field.Type != esriFieldType.esriFieldTypeDate)
                    {
                        cmbDestField.Items.Add(field.Name); // 添加Feature UID字段的控件选择
                    }
                }
            }
        }

        private void txtOutName_TextChanged(object sender, EventArgs e)
        {
            if (txtOutName.Text == "")
            {
                txtOutName.Tag = "";
                return;
            }

            Regex regex = new Regex(@"^[a-zA-Z\u4e00-\u9fa5][a-zA-Z0-9\u4e00-\u9fa5_]*$");
            if (!regex.IsMatch(txtOutName.Text))
            {
                txtOutName.Text = (string)txtOutName.Tag;
                toolTip1.Show("Invalid char in output name.", lbl_outname_error, 1650);
            }
            else
            {
                txtOutName.Tag = txtOutName.Text;
            }
        }
    }
}
