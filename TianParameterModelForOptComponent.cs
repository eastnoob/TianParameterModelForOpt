using Eto.Forms;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special.SketchElements;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            pManager.AddCurveParameter("Base", "B", "Base", GH_ParamAccess.item);

            // һ��պ��������룬��Ϊlands����ʾϸ�ֺ������
            pManager.AddCurveParameter("Lands", "L", "Divided Lands", GH_ParamAccess.list);
            // �б����������룬��ΪfloorNum�����ڱ��ÿһ�������Ĳ���
            pManager.AddIntegerParameter("FloorNum", "F", "Floornum", GH_ParamAccess.list);

            // ���������룬��ΪfloorHeight�����ڱ��ÿһ����Ĳ��
            pManager.AddNumberParameter("StandardFloorHeight", "SFH", "FloorHeight", GH_ParamAccess.item);
            // ���������룬��ΪGroundFloorHeight�����ڱ���ײ�Ĳ��
            pManager.AddNumberParameter("GroundFloorHeight", "GFH", "GroundFloorHeight", GH_ParamAccess.item);
            // ���������룬��ΪRoomDepth�����ڱ�﷿������
            pManager.AddNumberParameter("RoomDepth", "D", "RoomDepth", GH_ParamAccess.item);
            // ���������룬��ΪRoomWidth�����ڱ�﷿��Ŀ��
            pManager.AddNumberParameter("RoomWidth", "W", "RoomWidth", GH_ParamAccess.item);
            // ���������룬��ΪCorridorHeight�����������ȵĿ��
            pManager.AddNumberParameter("CorridorWidth", "C", "CorridorHeight", GH_ParamAccess.item);
            // ���������룬��ΪStaircaseWidth������¥�ݼ�Ŀ��
            pManager.AddNumberParameter("StaircaseWidth", "S", "StaircaseWidth", GH_ParamAccess.item);
            // ���������룬��ΪElevatorWidth�����ڵ��ݼ�Ŀ��
            pManager.AddNumberParameter("ElevatorWidth", "E", "ElevatorWidth", GH_ParamAccess.item);
            // ���������룬��ΪBuildingSpacing�����ڽ����ļ��
            pManager.AddNumberParameter("BuildingSpacing", "BS", "BuildingSpacing", GH_ParamAccess.item);

            // �б����룬��ΪZoneWestEast��ָʾ����������
            pManager.AddCurveParameter("ZoneWestEast", "Z:WE'", "ZoneWestEast", GH_ParamAccess.list);
            // �б����룬��ΪZoneNorthSouth��ָʾ�����뱱��
            pManager.AddCurveParameter("ZoneNorthSouth", "Z-NS", "ZoneNorthSouth", GH_ParamAccess.list);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // --- Building's geometry ---�����ڱ�ʶ�·��Ľڵ�Ϊ�����ļ����壬��ʵ������
            pManager.AddTextParameter("---- Geometries ----", "---- G ----", "Building's geometry", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // allGreenLand���б����棬������е��̵�
            pManager.AddBrepParameter("AllGreenLand", "G", "AllGreenLand", GH_ParamAccess.list);
            //allBuildings, �б�brep��������еĽ���
            pManager.AddBrepParameter("AllBuildings", "B", "AllBuildings", GH_ParamAccess.tree);

            //// allGroundFloorsPaths���б���������������ײ�ƽ��ķ������������˼�������
            //pManager.AddVectorParameter("AllGroundFloorsPaths", "G", "AllGroundFloorsPaths", GH_ParamAccess.list);
            //// allGroundFloors���б����棬��������ײ�ƽ��
            //pManager.AddSurfaceParameter("AllGroundFloors", "G", "AllGroundFloors", GH_ParamAccess.list);
            //// allOthersFloorsPaths���б�������������з��ײ�ƽ��ķ������������˼�������
            //pManager.AddVectorParameter("AllOthersFloorsPaths", "O", "AllOthersFloorsPaths", GH_ParamAccess.list);
            //// allOthersFloors���б����棬������з��ײ�ƽ��
            //pManager.AddSurfaceParameter("AllOthersFloors", "O", "AllOthersFloors", GH_ParamAccess.list);

            // ------------------------------ ��������ʱ���������ڴ������������棬�Ա�����Rhino�ж������� --------------------------------

            pManager.AddTextParameter("---- Temporary ----", "---- T ----", "Temporary items, which are floors and vectors", GH_ParamAccess.item);

            //-------------------------------------------------------------------------------------------------------------------

            // allFloors���б����ߣ���ｨ�������е�ƽ��
            pManager.AddCurveParameter(Name = "AllFloors", NickName = "AF", Description = "AllGroundFloorsPaths", GH_ParamAccess.list);
            // allFloorsPath, �б���������ｨ�������е�ƽ��ķ�����
            pManager.AddVectorParameter(Name = "AllFloorsPath", NickName = "AFP", Description = "AllGroundFloorsPaths", GH_ParamAccess.list);

            pManager.AddBrepParameter(Name = "SingleBlocks", NickName = "SB", Description = "AllGroundFloorsPaths", GH_ParamAccess.list);
            pManager.AddBrepParameter(Name = "JudgeBlocks", NickName = "JB", Description = "AllJudgeBlockForALand", GH_ParamAccess.list);
            //-----------------------------------------------------------------------------------------------------

            // --- Economic indicators ---�����ڱ�ʶ�·��Ľڵ�Ϊ����ָ�꣬��ʵ������
            pManager.AddTextParameter("--- Economic indicators ---", "--- E ---", "Economic indicators", GH_ParamAccess.item);

            //-----------------------------------------------------------------------------------------------------

            // plotRatio���������֣������Ŀ�ݻ���
            pManager.AddNumberParameter("PlotRatio", "P", "PlotRatio", GH_ParamAccess.item);
            // greenRatio���������֣������Ŀ�̻���
            pManager.AddNumberParameter("GreenRatio", "G", "GreenRatio", GH_ParamAccess.item);
            // buildingDensity���������֣������Ŀ�����ܶ�
            pManager.AddNumberParameter("BuildingDensity", "B", "BuildingDensity", GH_ParamAccess.item);
            // roomNum �������֣�����ķ�������
            pManager.AddIntegerParameter("RoomNum", "R", "RoomNum", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // ���ڴ洢���������
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

            // ע��
            if (!DA.GetData("Base", ref baseCurve)) return;
            //DA.GetDataList("Lands", lands);
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

            //DA.GetDataList("ZoneWestEast", new List<Curve>());
            if (!DA.GetDataList("ZoneWestEast", zoneWestEast)) return;
            if (!DA.GetDataList("ZoneNorthSouth", zoneNorthSouth)) return;

            // װ�ر���
            // == ͼ��
            // ����ʵ��
            List<List<Brep>> allBuildings = new List<List<Brep>>();
            // �̵�ͼ��
            List<Brep> allGreenLand = new List<Brep>();

            // �Ż���
            List<Brep>judgeBlocks = new List<Brep>(); 

            // == ����
            // �������
            double baseCurveArea = AreaMassProperties.Compute(baseCurve).Area;
            // �滮�����õ����
            double offsetedBaseCurveArea = 0;


            double totalGreenLandArea = 0;
            double totalBuildingArea = 0;
            double totalConstructArea = 0;

            //double plotRatio = 0;
            //double greenRatio = 0;
            //double buildingDensity = 0;
            int roomNum = 0;

            /*------------------------------------ ��ʱ���� -------------------------------------*/

            List<Curve> allFloorsPlanes = new List<Curve>();
            List<Vector3d> allFloorsPaths = new List<Vector3d>();
            List<Brep> allFloors = new List<Brep>();
            /*--------------------------------------------------ͼ��--------------------------------------------------------*/

            // ���ڲ���
            List<Curve> sketchs = new List<Curve>();
            List<Brep> singleBlocks = new List<Brep>();
            //floorNum = new List<int>() { 1, 1, 1, 1, 1, 1, 1, 1, 1 };

            // --------------------------------- ��־ϵͳ
            // ��ȡ��ǰ���ں�ʱ��
            DateTime now = DateTime.Now;

            // �����ں�ʱ��ת��Ϊָ����ʽ���ַ���
            string formattedDate = now.ToString("yyyyMMdd_HHmm");

            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));

            // ����ִ��
            int landIndex = 0;

            while (landIndex < lands.Count)
            {
                //var a = floorNum.ToString();
                Rhino.RhinoApp.WriteLine("-------------------------------- New building ------------------------------------");
                //Console.WriteLine("======================================== FloorNum: ======================================================" + floorNum.ToString());
                // ��ʼ��Land
                Land thisLand = new Land(baseCurve, lands[landIndex], roomDepth, roomWidth, corridorWidth, staircaseWidth, elevatorWidth, buildingSpacing, zoneWestEast, zoneNorthSouth);

                //------------------------------------------------�������ڲ���-----------------------------------------------

                //// �������land��sketch 
                //Curve thisSketch = Draw.DrawSketchOfABuilding(thisLand);

                //// ����sketch
                //sketchs.Add(thisSketch);

                //------------------------------------------------�������ڲ���-----------------------------------------------

                // �ȴ���base�����
                if (offsetedBaseCurveArea == 0)
                    offsetedBaseCurveArea = AreaMassProperties.Compute(thisLand.baseCurve).Area;


                // �����жϣ����land�����ɽ���������������һ��null���뽨���б�ռλ
                if (thisLand.buildingTypeOfThisLandCurve.Contains("NO"))
                {
                    Console.WriteLine("The Land" + landIndex.ToString() + "do not generate, pass!");
                    allBuildings.Add(null);

                    // �̵�
                    GreenLand greenLand = new GreenLand(thisLand, roomWidth);
                    allGreenLand.Add(greenLand.greenLandBrep);

                    // �̵����(����)
                    totalGreenLandArea += greenLand.greenLandArea;

                    // ---------------------��ʱ����---------------------
                    allFloorsPlanes.Add(null);
                }
                else
                {

                    Console.WriteLine("The Land" + landIndex.ToString() + "is valied!");
                    Console.WriteLine("Building Type:" + thisLand.buildingTypeOfThisLandCurve.ToString());

                    Console.WriteLine("Land index: " + landIndex);
                    Console.WriteLine("Floor number: " + floorNum[landIndex]);

                    Trace.WriteLine("The Land" + landIndex.ToString() + "is valied!");
                    Trace.WriteLine("Building Type:" + thisLand.buildingTypeOfThisLandCurve.ToString());
                    Trace.WriteLine("Land index: " + landIndex);
                    Trace.WriteLine("Floor number: " + floorNum[landIndex]);

                    // ��ʼ��building
                    try
                    {
                        Building thisBuilding = new Building(thisLand, groundFloorHeight, standardFloorHeight, floorNum[landIndex]);

                        //// װ�ر���
                        allBuildings.Add(thisBuilding.allFloors);

                        // ����JudgeList
                        judgeBlocks.Add(thisBuilding.judgeBrep[0]);

                        // �̵�
                        GreenLand thisGreenLand = new GreenLand(thisLand, roomWidth);
                        allGreenLand.Add(thisGreenLand.greenLandBrep);

                        // �������
                        // �����ɵ������
                        if (thisBuilding.allFloors == null || thisBuilding.bottomSurface == null)
                        {
                            Console.WriteLine("�õؿ鲻������");
                            totalBuildingArea += 0;
                            totalConstructArea += 0;
                            roomNum += 0;
                            totalGreenLandArea += thisGreenLand.greenLandArea;
                        }
                        else
                        {
                            // �������������������
                            totalBuildingArea += AreaMassProperties.Compute(thisBuilding.bottomSurface).Area;
                            //double buildingArea = AreaMassProperties.Compute(thisBuilding.floorSketch).Area;

                            // �������������
                            totalConstructArea += (AreaMassProperties.Compute(thisBuilding.bottomSurface).Area) * floorNum[landIndex];
                            //double constructArea = buildingArea * floorNum[index];

                            // �̵����(����)
                            totalGreenLandArea += thisGreenLand.greenLandArea;
                            //double greenLandArea = greenLand.greenLandArea;

                            // ����������������
                            roomNum += Convert.ToInt32(Math.Round(thisBuilding.GetEstimatedRoomAccount()));
                        }

                    }
                    catch (Exception ex)
                    {
                        //// װ�ر���
                        allBuildings.Add(new List<Brep>());

                        // ����JudgeList
                        judgeBlocks.Add(null);

                        // �̵�
                        GreenLand greenLandException = new GreenLand(thisLand, roomWidth);
                        allGreenLand.Add(greenLandException.greenLandBrep);

                        // �������
                        // �����ɵ������

                        Console.WriteLine("�õؿ鲻������");
                        totalBuildingArea += 0;
                        totalConstructArea += 0;
                        roomNum += 0;
                        totalGreenLandArea += greenLandException.greenLandArea;

                        landIndex++;

                        continue;
                    }

                    ////-----------------------------------ԭ�еĳ���-----------------------------------

                    //Building thisBuilding = new Building(thisLand, groundFloorHeight, standardFloorHeight, floorNum[landIndex]);

                    ////allBuildings.Add(thisBuilding.allFloors);

                    ////// ------------------------��ʱ����------------------------
                    ////Brep aaa = thisBuilding.judgeBrep;
                    ////Brep[] bbb =
                    ////allFloors.Add(aaa);

                    ////Brep[] allBreps = thisBuilding.allFloors;
                    ////List<Brep> allBreps = thisBuilding.allFloors;
                    ////allFloors.AddRange(allBreps);

                    ////singleBlocks.AddRange(thisBuilding.brepOfTheBuilding);

                    ////singleBlocks.AddRange(thisBuilding.singleBlocks);

                    //// --------------------------------------ԭ�г�����-------------------------------------

                    ////Building thisBuilding = new Building(thisLand, groundFloorHeight, standardFloorHeight, floorNum[landIndex]);

                    ////// װ�ر���
                    //allBuildings.Add(thisBuilding.allFloors);

                    //// ����JudgeList
                    //judgeBlocks.Add(thisBuilding.judgeBrep[0]);

                    //// �̵�
                    //GreenLand greenLand = new GreenLand(thisLand, roomWidth);
                    //allGreenLand.Add(greenLand.greenLandBrep);

                    //// �������
                    //// �����ɵ������
                    //if(thisBuilding.allFloors == null || thisBuilding.bottomSurface == null)
                    //{
                    //    Console.WriteLine("�õؿ鲻������");
                    //    totalBuildingArea += 0;
                    //    totalConstructArea += 0;
                    //    roomNum += 0;
                    //    totalGreenLandArea += greenLand.greenLandArea;
                    //}
                    //else
                    //{
                    //    // �������������������
                    //    totalBuildingArea += AreaMassProperties.Compute(thisBuilding.bottomSurface).Area;
                    //    //double buildingArea = AreaMassProperties.Compute(thisBuilding.floorSketch).Area;

                    //    // �������������
                    //    totalConstructArea += (AreaMassProperties.Compute(thisBuilding.bottomSurface).Area) * floorNum[landIndex];
                    //    //double constructArea = buildingArea * floorNum[index];

                    //    // �̵����(����)
                    //    totalGreenLandArea += greenLand.greenLandArea;
                    //    //double greenLandArea = greenLand.greenLandArea;

                    //    // ����������������
                    //    roomNum += Convert.ToInt32(Math.Round(thisBuilding.GetEstimatedRoomAccount()));
                    //}



                    //// ---------------------��ʱ����---------------------
                    //allFloorsPlanes.AddRange(thisBuilding.allfloorPlanesAndItsVectors.Keys.ToList());
                    //allFloorsPaths.AddRange(thisBuilding.allfloorPlanesAndItsVectors.Values.ToList());

                }


                landIndex ++;
                    
            }


            /*------------------------------------------ָ��--------------------------------------------------------*/

            double plotRatio = totalConstructArea / baseCurveArea;
            double greenRatio = totalGreenLandArea / baseCurveArea;
            double buildingDensity = totalBuildingArea / offsetedBaseCurveArea;

            // ת��Ϊdatatree
            var dataTree = new DataTree<Brep>();

            if(allBuildings.Contains (null))
            {
                dataTree.Add(null);
            }
            else
            {
                for (int i = 0; i < allBuildings.Count; i++)
                {
                    for (int j = 0; j < allBuildings[i].Count; j++)
                    {
                        dataTree.Add(allBuildings[i][j], new GH_Path(i));
                    }
                }

            }


            //DA.SetDataTree(0, dataTree);


            /*-------------------------------------------���-------------------------------------------------------*/


            DA.SetDataTree(2, dataTree);
            //DA.SetData(2, ghBrepList);

            DA.SetDataList("AllGreenLand", allGreenLand);
            DA.SetData("PlotRatio", plotRatio);
            DA.SetData("GreenRatio", greenRatio);
            DA.SetData("BuildingDensity", buildingDensity);
            DA.SetData("RoomNum", roomNum);

            // ----------------------------------��ʱ����---------------------------
            DA.SetDataList("AllFloors", allFloorsPlanes) ;
            DA.SetDataList("AllFloorsPath", allFloorsPaths);
            DA.SetDataList("SingleBlocks", singleBlocks);

            DA.SetDataList("JudgeBlocks", judgeBlocks);

            Trace.Close();

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        //protected override System.Drawing.Bitmap Icon => null;
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // �������·�������ͼ���ļ�·��
                return new System.Drawing.Bitmap("D:\\Github\\TianParameterModelForOpt\\resources\\Iconarchive-Gift-Red-3-Gift.32.png");
            }
        }



        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("c454ce44-3863-40bc-b880-d335536a6570");


        
    }
}