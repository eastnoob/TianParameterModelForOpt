using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace TianParameterModelForOpt
{
    public class TianParameterModelForOptComponent : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public TianParameterModelForOptComponent()
          : base("TianParameterModelForOpt", "Nickname",
            "Description",
            "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // �պϵ��������룬��Ϊbase�����ڱ�ﾭ���˽��Ļ�������
            pManager.AddCurveParameter("Base", "B", "Base", GH_ParamAccess.list);
            // �б����������룬��ΪfloorNum�����ڱ��ÿһ�������Ĳ���
            pManager.AddNumberParameter("FloorNum", "F", "Floornum", GH_ParamAccess.list);
            // ���������룬��ΪfloorHeight�����ڱ��ÿһ����Ĳ��
            pManager.AddNumberParameter("FloorHeight", "H", "FloorHeight", GH_ParamAccess.item);
            // ���������룬��ΪGroundFloorHeight�����ڱ���ײ�Ĳ��
            pManager.AddNumberParameter("GroundFloorHeight", "G", "GroundFloorHeight", GH_ParamAccess.item);
            // ���������룬��ΪRoomDepth�����ڱ�﷿������
            pManager.AddNumberParameter("RoomDepth", "D", "RoomDepth", GH_ParamAccess.item);
            // ���������룬��ΪRoomWidth�����ڱ�﷿��Ŀ��
            pManager.AddNumberParameter("RoomWidth", "W", "RoomWidth", GH_ParamAccess.item);
            // ���������룬��ΪCorridorHeight�����������ȵĿ��
            pManager.AddNumberParameter("CorridorHeight", "C", "CorridorHeight", GH_ParamAccess.item);
            // ���������룬��ΪStaircaseWidth������¥�ݼ�Ŀ��
            pManager.AddNumberParameter("StaircaseWidth", "S", "StaircaseWidth", GH_ParamAccess.item);
            // ���������룬��ΪElevatorWidth�����ڵ��ݼ�Ŀ��
            pManager.AddNumberParameter("ElevatorWidth", "E", "ElevatorWidth", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // --- Building's geometry ---�����ڱ�ʶ�·��Ľڵ�Ϊ�����ļ����壬��ʵ������
            pManager.AddTextParameter("Building's geometry", "G", "Building's geometry", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // allGreenLand���б����棬������е��̵�
            pManager.AddSurfaceParameter("AllGreenLand", "G", "AllGreenLand", GH_ParamAccess.list);
            // allGroundFloorsPaths���б���������������ײ�ƽ��ķ������������˼�������
            pManager.AddVectorParameter("AllGroundFloorsPaths", "G", "AllGroundFloorsPaths", GH_ParamAccess.list);
            // allGroundFloors���б����棬��������ײ�ƽ��
            pManager.AddSurfaceParameter("AllGroundFloors", "G", "AllGroundFloors", GH_ParamAccess.list);
            // allOthersFloorsPaths���б�������������з��ײ�ƽ��ķ������������˼�������
            pManager.AddVectorParameter("AllOthersFloorsPaths", "O", "AllOthersFloorsPaths", GH_ParamAccess.list);
            // allOthersFloors���б����棬������з��ײ�ƽ��
            pManager.AddSurfaceParameter("AllOthersFloors", "O", "AllOthersFloors", GH_ParamAccess.list);

            //-----------------------------------------------------------------------------------------------------

            // --- Economic indicators ---�����ڱ�ʶ�·��Ľڵ�Ϊ����ָ�꣬��ʵ������
            pManager.AddTextParameter("Economic indicators", "E", "Economic indicators", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // plotRatio���������֣������Ŀ�ݻ���
            pManager.AddNumberParameter("PlotRatio", "P", "PlotRatio", GH_ParamAccess.item);
            // greenRatio���������֣������Ŀ�̻���
            pManager.AddNumberParameter("GreenRatio", "G", "GreenRatio", GH_ParamAccess.item);
            // buildingDensity���������֣������Ŀ�����ܶ�
            pManager.AddNumberParameter("BuildingDensity", "B", "BuildingDensity", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c454ce44-3863-40bc-b880-d335536a6570");
    }
}