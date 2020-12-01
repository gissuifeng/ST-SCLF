using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SpacetimeSCLFlowAnalysis
{
    public class ToolButton : ESRI.ArcGIS.Desktop.AddIns.Button
    {
        public ToolButton()
        {
        }

        protected override void OnClick()
        {
            //
            //  TODO: Sample code showing how to access button host
            //
            ArcMap.Application.CurrentTool = null;
            Form1 form1 = new Form1();
            form1.ShowDialog();

        }
        protected override void OnUpdate()
        {
            Enabled = ArcMap.Application != null;
        }
    }

}
