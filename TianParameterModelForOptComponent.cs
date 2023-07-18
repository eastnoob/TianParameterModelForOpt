using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special.SketchElements;
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
            pManager.AddCurveParameter("Base", "B", "Base", GH_ParamAccess.item);
            // 一组闭合曲线输入，名为lands，表示细分后的土地
            pManager.AddCurveParameter("Lands", "L", "Divided Lands", GH_ParamAccess.list);
            // 列表数字输输入，名为floorNum，用于表达每一个建筑的层数
            pManager.AddNumberParameter("FloorNum", "F", "Floornum", GH_ParamAccess.list);
            // 单数字输入，名为floorHeight，用于表达每一个层的层高
            pManager.AddNumberParameter("StandardFloorHeight", "SFH", "FloorHeight", GH_ParamAccess.item);
            // 单数字输入，名为GroundFloorHeight，用于表达首层的层高
            pManager.AddNumberParameter("GroundFloorHeight", "GFH", "GroundFloorHeight", GH_ParamAccess.item);
            // 单数字输入，名为RoomDepth，用于表达房间的深度
            pManager.AddNumberParameter("RoomDepth", "D", "RoomDepth", GH_ParamAccess.item);
            // 单数字输入，名为RoomWidth，用于表达房间的宽度
            pManager.AddNumberParameter("RoomWidth", "W", "RoomWidth", GH_ParamAccess.item);
            // 单数字输入，名为CorridorHeight，用于外走廊的宽度
            pManager.AddNumberParameter("CorridorWidth", "C", "CorridorHeight", GH_ParamAccess.item);
            // 单数字输入，名为StaircaseWidth，用于楼梯间的宽度
            pManager.AddNumberParameter("StaircaseWidth", "S", "StaircaseWidth", GH_ParamAccess.item);
            // 单数字输入，名为ElevatorWidth，用于电梯间的宽度
            pManager.AddNumberParameter("ElevatorWidth", "E", "ElevatorWidth", GH_ParamAccess.item);
            // 单数字输入，名为BuildingSpacing，用于建筑的间距
            pManager.AddNumberParameter("BuildingSpacing", "BS", "BuildingSpacing", GH_ParamAccess.item);
            // 列表输入，名为ZoneWestEast，指示东区与西区
            pManager.AddBooleanParameter("ZoneWestEast", "Z:WE'", "ZoneWestEast", GH_ParamAccess.list);
            // 列表输入，名为ZoneNorthSouth，指示南区与北区
            pManager.AddBooleanParameter("ZoneNorthSouth", "Z-NS", "ZoneNorthSouth", GH_ParamAccess.list);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // --- Building's geometry ---，用于标识下方的节点为建筑的几何体，无实际意义
            pManager.AddTextParameter("---- Geometries ----", "---- G ----", "Building's geometry", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // allGreenLand，列表曲面，表达所有的绿地
            pManager.AddSurfaceParameter("AllGreenLand", "G", "AllGreenLand", GH_ParamAccess.list);
            //allBuildings, 列表brep，表达所有的建筑
            pManager.AddBrepParameter("AllBuildings", "B", "AllBuildings", GH_ParamAccess.list);

            //// allGroundFloorsPaths，列表向量，表达所有首层平面的法向量，定义了挤出方向
            //pManager.AddVectorParameter("AllGroundFloorsPaths", "G", "AllGroundFloorsPaths", GH_ParamAccess.list);
            //// allGroundFloors，列表曲面，表达所有首层平面
            //pManager.AddSurfaceParameter("AllGroundFloors", "G", "AllGroundFloors", GH_ParamAccess.list);
            //// allOthersFloorsPaths，列表向量，表达所有非首层平面的法向量，定义了挤出方向
            //pManager.AddVectorParameter("AllOthersFloorsPaths", "O", "AllOthersFloorsPaths", GH_ParamAccess.list);
            //// allOthersFloors，列表曲面，表达所有非首层平面
            //pManager.AddSurfaceParameter("AllOthersFloors", "O", "AllOthersFloors", GH_ParamAccess.list);

            //-----------------------------------------------------------------------------------------------------

            // --- Economic indicators ---，用于标识下方的节点为经济指标，无实际意义
            pManager.AddTextParameter("--- Economic indicators ---", "--- E ---", "Economic indicators", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // plotRatio，单项数字，表达项目容积率
            pManager.AddNumberParameter("PlotRatio", "P", "PlotRatio", GH_ParamAccess.item);
            // greenRatio，单项数字，表达项目绿化率
            pManager.AddNumberParameter("GreenRatio", "G", "GreenRatio", GH_ParamAccess.item);
            // buildingDensity，单项数字，表达项目建筑密度
            pManager.AddNumberParameter("BuildingDensity", "B", "BuildingDensity", GH_ParamAccess.item);
            // roomNum 单项数字，估算的房间数量
            pManager.AddIntegerParameter("RoomNum", "R", "RoomNum", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // 用于存储输入的数据
            Curve baseCurve = Curve.CreateControlPointCurve(new Point3d[0], 1);
            List<Curve> lands = new List<Curve>();
            List<int> floorNum = new List<int>();
            double standardFloorHeight = double.NaN;
            double groundFloorHeight = double.NaN;
            double roomDepth = double.NaN;
            double roomWidth = double.NaN;
            double corridorWidth = double.NaN;
            double staircaseWidth = double.NaN;
            double elevatorWidth = double.NaN;
            double buildingSpacing = double.NaN;
            List<Curve> zoneWestEast = new List<Curve>();
            List<Curve> zoneNorthSouth = new List<Curve>();

            // 注册
            if (!DA.GetData("Base", ref baseCurve)) return;
            if(!DA.GetDataList("Lands", lands)) return;
            if(!DA.GetDataList("FloorNum", floorNum)) return;
            if(!DA.GetData("StandardFloorHeight", ref standardFloorHeight)) return;
            if(!DA.GetData("GroundFloorHeight", ref groundFloorHeight)) return;
            if(!DA.GetData("RoomDepth", ref roomDepth)) return;
            if(!DA.GetData("RoomWidth", ref roomWidth)) return;
            if(!DA.GetData("CorridorWidth", ref corridorWidth)) return;
            if(!DA.GetData("StaircaseWidth", ref staircaseWidth)) return;
            if(!DA.GetData("ElevatorWidth", ref elevatorWidth)) return;
            if (!DA.GetData("BuildingSpacing", ref buildingSpacing)) return;
            if(!DA.GetDataList("ZoneWestEast", zoneWestEast)) return;
            if(!DA.GetDataList("ZoneNorthSouth", zoneNorthSouth)) return;

            // 装载变量
            // == 图形
            // 建筑实体
            List<List<Brep>> allBuildings = new List<List<Brep>>();
            // 绿地图形
            List<Brep> allGreenLand = new List<Brep>();

            // == 参数
            // 基底面积
            double baseCurveArea = AreaMassProperties.Compute(baseCurve).Area;
            // 规划建设用地面积
            double offsetedBaseCurveArea = 0;


            double totalGreenLandArea = 0;
            double totalBuildingArea = 0;
            double totalConstructArea = 0;

            //double plotRatio = 0;
            //double greenRatio = 0;
            //double buildingDensity = 0;
            int roomNum = 0;

            /*--------------------------------------------------图形--------------------------------------------------------*/

            // 用于测试
            List<Curve> sketchs = new List<Curve>();

            // 函数执行
            int index = 0;
            while (index < lands.Count)
            {
                // 初始化Land
                Land thisLand = new Land(baseCurve, lands[index], roomDepth, roomWidth, corridorWidth, staircaseWidth, elevatorWidth, buildingSpacing, zoneWestEast, zoneNorthSouth);

                //------------------------------------------------以下用于测试-----------------------------------------------

                //// 生成这个land的sketch 
                //Curve thisSketch = Draw.DrawSketchOfABuilding(thisLand);

                //// 加入sketch
                //sketchs.Add(thisSketch);

                //------------------------------------------------以上用于测试-----------------------------------------------

                // 初始化building
                Building thisBuilding = new Building(thisLand, groundFloorHeight, standardFloorHeight, floorNum[index]);

                //// 生成建筑的形体
                //thisBuilding.DrawBuildingFloorBlocks();
                // 装载保存
                allBuildings.Add(thisBuilding.buildingBrep);

                // 绿地
                GreenLand greenLand = new GreenLand(thisLand, roomWidth);
                allGreenLand.Add(greenLand.greenLandBrep);

                /*-------------------------------------------指标--------------------------------------------------------*/

                // 材料

                if (offsetedBaseCurveArea == 0)
                    offsetedBaseCurveArea = AreaMassProperties.Compute(thisLand.baseCurve).Area;


                // 建筑基底面积（单个）
                totalBuildingArea += AreaMassProperties.Compute(thisBuilding.floorSketch).Area;
                //double buildingArea = AreaMassProperties.Compute(thisBuilding.floorSketch).Area;

                // 建造面积（单个
                totalConstructArea += (AreaMassProperties.Compute(thisBuilding.floorSketch).Area) * floorNum[index];
                //double constructArea = buildingArea * floorNum[index];

                // 绿地面积(单个)
                totalGreenLandArea += greenLand.greenLandArea;
                //double greenLandArea = greenLand.greenLandArea;

                // 房间数量（单个）
                roomNum += Convert.ToInt32(Math.Round(thisBuilding.GetEstimatedRoomAccount()));

                index ++;
                    
            }


            /*------------------------------------------指标--------------------------------------------------------*/

            double plotRatio = totalConstructArea / baseCurveArea;
            double greenRatio = totalGreenLandArea / baseCurveArea;
            double buildingDensity = totalBuildingArea / offsetedBaseCurveArea;


            /*-------------------------------------------输出-------------------------------------------------------*/

            DA.SetData("Building", allBuildings);
            DA.SetData("GreenLand", allGreenLand);
            DA.SetData("PlotRatio", plotRatio);
            DA.SetData("GreenRatio", greenRatio);
            DA.SetData("BuildingDensity", buildingDensity);
            DA.SetData("RoomNum", roomNum);

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