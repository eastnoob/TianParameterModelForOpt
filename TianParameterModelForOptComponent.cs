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
            // 闭合的曲线输入，名为base，用于表达经过退界后的基地轮廓
            pManager.AddCurveParameter("Base", "B", "Base", GH_ParamAccess.list);
            // 列表数字输输入，名为floorNum，用于表达每一个建筑的层数
            pManager.AddNumberParameter("FloorNum", "F", "Floornum", GH_ParamAccess.list);
            // 单数字输入，名为floorHeight，用于表达每一个层的层高
            pManager.AddNumberParameter("FloorHeight", "H", "FloorHeight", GH_ParamAccess.item);
            // 单数字输入，名为GroundFloorHeight，用于表达首层的层高
            pManager.AddNumberParameter("GroundFloorHeight", "G", "GroundFloorHeight", GH_ParamAccess.item);
            // 单数字输入，名为RoomDepth，用于表达房间的深度
            pManager.AddNumberParameter("RoomDepth", "D", "RoomDepth", GH_ParamAccess.item);
            // 单数字输入，名为RoomWidth，用于表达房间的宽度
            pManager.AddNumberParameter("RoomWidth", "W", "RoomWidth", GH_ParamAccess.item);
            // 单数字输入，名为CorridorHeight，用于外走廊的宽度
            pManager.AddNumberParameter("CorridorHeight", "C", "CorridorHeight", GH_ParamAccess.item);
            // 单数字输入，名为StaircaseWidth，用于楼梯间的宽度
            pManager.AddNumberParameter("StaircaseWidth", "S", "StaircaseWidth", GH_ParamAccess.item);
            // 单数字输入，名为ElevatorWidth，用于电梯间的宽度
            pManager.AddNumberParameter("ElevatorWidth", "E", "ElevatorWidth", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // --- Building's geometry ---，用于标识下方的节点为建筑的几何体，无实际意义
            pManager.AddTextParameter("Building's geometry", "G", "Building's geometry", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // allGreenLand，列表曲面，表达所有的绿地
            pManager.AddSurfaceParameter("AllGreenLand", "G", "AllGreenLand", GH_ParamAccess.list);
            // allGroundFloorsPaths，列表向量，表达所有首层平面的法向量，定义了挤出方向
            pManager.AddVectorParameter("AllGroundFloorsPaths", "G", "AllGroundFloorsPaths", GH_ParamAccess.list);
            // allGroundFloors，列表曲面，表达所有首层平面
            pManager.AddSurfaceParameter("AllGroundFloors", "G", "AllGroundFloors", GH_ParamAccess.list);
            // allOthersFloorsPaths，列表向量，表达所有非首层平面的法向量，定义了挤出方向
            pManager.AddVectorParameter("AllOthersFloorsPaths", "O", "AllOthersFloorsPaths", GH_ParamAccess.list);
            // allOthersFloors，列表曲面，表达所有非首层平面
            pManager.AddSurfaceParameter("AllOthersFloors", "O", "AllOthersFloors", GH_ParamAccess.list);

            //-----------------------------------------------------------------------------------------------------

            // --- Economic indicators ---，用于标识下方的节点为经济指标，无实际意义
            pManager.AddTextParameter("Economic indicators", "E", "Economic indicators", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // plotRatio，单项数字，表达项目容积率
            pManager.AddNumberParameter("PlotRatio", "P", "PlotRatio", GH_ParamAccess.item);
            // greenRatio，单项数字，表达项目绿化率
            pManager.AddNumberParameter("GreenRatio", "G", "GreenRatio", GH_ParamAccess.item);
            // buildingDensity，单项数字，表达项目建筑密度
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