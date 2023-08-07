using Eto.Forms;
using Grasshopper.Kernel.Types.Transforms;
using MoreLinq;
using MoreLinq.Extensions;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using TianParameterModelForOpt._4_repair;
using static Rhino.DocObjects.PhysicallyBasedMaterial;
//using static Grasshopper.DataTree<T>;

namespace TianParameterModelForOpt
{
    /// <summary>
    /// 这个类是用来描述建筑的，目的是生成建筑的形体<\br>
    /// 这个函数仅仅生成单体的建筑
    /// </summary>
    public class Building
    {
        // 属性
        // land
        Land land;

        // 楼层高度
        double groundFloorHeight;
        double standardFloorHeight;

        // 楼层数量
        int floorNum;

        // 楼层平面图
        public Dictionary<Curve, Curve> allEdgeWithOffsetBaseCurve;
        public Dictionary<string, string> offsetBehavioursOfDirections;

        public Dictionary<Curve, string> offsetBehavioursOfLandcurves;
        //public Curve floorSketch;

        // 所有楼层的平面图
        public Dictionary<Curve, Vector3d> allfloorPlanesAndItsVectors;

        //// 是否是boundage
        //bool isBoundageOrNot;

        // *********** 楼层实体图形
        public List<Brep> buildingBrep;


        //// 底面，用于算面积
        //public BrepFace bottomSurface;

        // 建筑面积
        public double singleFloorArea;

        // 这个用于检查
        public double bottomarea;

        // ----------------------- debug用变量 -------------------------------
        public List<Curve> singleBlocks;
        public Brep[] singleBlockBreps;
        public Brep[] brepOfTheBuilding;
        public Brep[] judgeBrepArray;
        public List<Brep> allFloors;
        public List<Brep> allUnjugedFloors;
        public Brep[] judgeBrep;

        // 构造器，传入Land类的land，楼层高度，楼层数量，和楼层平面图
        public Building(Land land, double groundFloorHeight, double standardFloorHeight, int floorNum/*, Curve floorSketchs*/)
        {


            this.land = land;
            this.groundFloorHeight = groundFloorHeight;
            this.standardFloorHeight = standardFloorHeight;
            this.floorNum = floorNum;
            //this.floorSketch = Draw.DrawSketchOfABuilding(land);
            this.allEdgeWithOffsetBaseCurve = Draw.DrawSketchOfABuilding(land, out offsetBehavioursOfDirections);



            offsetBehavioursOfLandcurves = new Dictionary<Curve, string>();


            // 建立偏移结果与condition
            foreach (string direction in offsetBehavioursOfDirections.Keys)
            {
                foreach (Curve curve in allEdgeWithOffsetBaseCurve.Keys)
                {
                    if (land.dispatchedEdges[direction].Contains(curve))
                    {
                        offsetBehavioursOfLandcurves[curve] = offsetBehavioursOfDirections[direction];
                    }
                }
            }

            // 建立JudgeBrep

            Brep judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, land, 100);

            if (judgeBrep == null)
            {
                Curve judgeBase = land.landCurve;
                judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, land, 100);
            }

            this.judgeBrep = new Brep[] { judgeBrep };



            //// 临时变量
            //singleBlockBreps = DebugMethods.CreateAllFloors(this.allEdgeWithOffsetBaseCurve, offsetBehavioursOfLandcurves, land.landCurve);

            allFloors = CreateAllFloors(allEdgeWithOffsetBaseCurve, offsetBehavioursOfLandcurves);

            
            //// 生成单体建筑物形体
            //singleBlockBreps = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(land.baseCurve, land.landCurve, );

            // 尝试处理None的问题
            //brepOfTheBuilding = Brep.CreateBooleanIntersection(judgeBrepArray, singleBlockBreps, 0.001);

            Console.WriteLine("judgedSingleBlockBrep.Length: " );

            /*if (brepOfTheBuilding == null*//* || judgedSingleBlockBrep.Length == 0 *//*)
            {
                Brep maxvolumebrep = null;

                if (singleBlockBreps.Length > 1)
                {
                    // 取出最大的那一个
                    Brep[] unionSecond = Brep.CreateBooleanUnion(singleBlockBreps, 0.001);

                    if (unionSecond.Length > 1)
                    {
                        //Brep maxvolumebrep = null;
                        double maxvolume = 0.0;

                        foreach (Brep brep in singleBlockBreps)
                        {
                            VolumeMassProperties vmp = VolumeMassProperties.Compute(brep);
                            double volume = vmp.Volume;

                            if (volume > maxvolume)
                            {
                                maxvolume = volume;
                                maxvolumebrep = brep;
                            }
                        }
                    }

                    else if(unionSecond.Length == 0)
                        maxvolumebrep = null;
                    else
                        maxvolumebrep = unionSecond[0];
                }
                Brep[] judgedSingleBlockBreps = new Brep[] { maxvolumebrep };


                if (maxvolumebrep == null)
                    brepOfTheBuilding = null;

                else
                {
                    // 换用Split方法
                    Brep[] judged = maxvolumebrep.Split(judgeBrepArray, 0.01);

                    var largestOne = FindTheLargestBrep(judged);
                    if(largestOne.IsSolid == false)
                        largestOne = largestOne.CapPlanarHoles(0.01);
                    
                    brepOfTheBuilding = new Brep[] { largestOne };
                    
                    Console.WriteLine("Repair Complete ! !");
                }


            }*/



            //judgedSingleBlockBreps[0] = judgeBrep;
            //judgedSingleBlockBreps = Brep.CreateBooleanIntersection(judgeBrep, singleBlockBreps[0], 0.01);

            //this.buildingBrep = createallfloors(alledgewithoffsetbasecurve, offsetbehavioursoflandcurves);

            this.bottomarea = GetBottomSurfaceArea();

            //if (! allFloors.Contains(null))
            //{
            //    this.buildingBrep = allFloors;
            //    //this.bottomSurface = GetBottomSurface();
            //    double bottomarea = GetBottomSurfaceArea();
            //}
            //else
            //{
            //    //double bottomarea = GetBottomSurfaceArea();
            //    //this.bottomSurface = null;
            //}

           
        }


        /*---------------------------------------建筑物形体---------------------------------------*/

        //附属方法：单层建筑物形体
        public Brep DrawSingleFloorBlock(Curve sketchOfFloor, Vector3d extrudeVector)
            {
                // 创建Extrusion
                Extrusion extrusion = Extrusion.Create(sketchOfFloor, extrudeVector.Length, true);

                //// 对齐方向
                //extrusion.Transform(Transform.Translation(extrudeVector));

                // 将Extrusion对象转换成Brep对象
                Brep brep = extrusion.ToBrep(true);

                return brep;
            }


        /* ------------------- 新方法Brep -------------------------------------------*/

        public Brep[] CreateSingleFloorBrep(Dictionary<Curve, Curve> curveWithOffsetedResults, Dictionary<Curve, string> offsetBehavioursOfLandcurves, string isStandard)
        {



            this.allEdgeWithOffsetBaseCurve = Draw.DrawSketchOfABuilding(land, out offsetBehavioursOfDirections);

            Dictionary<Curve, Brep> curveWithBrep = new Dictionary<Curve, Brep>();
            Curve landCurve = land.landCurve;
            double buildingDepth = land.GetBuildingDepth();

            double floorHight;
            if (isStandard == "standard")
                floorHight = this.standardFloorHeight;
            else if (isStandard == "ground")
                floorHight = this.groundFloorHeight;
            else
                floorHight = this.standardFloorHeight;

            var directionWithCurve = land.dispatchedEdges;


            // 太小了久不参与实体的生成，让大的来顶上

            //bool haveReallyShort = false;
            List<Curve> landShortCurve = new List<Curve>();

            //foreach (Curve landcurve in curveWithBrep.Keys)
            //{
            //    if (landcurve.GetLength() <= 7 && offsetBehavioursOfLandcurves[landcurve].Contains("edge")) 
            //    {
            //        haveReallyShort = true;
            //        reallyShortCurve.Add(landcurve);
            //    }
            //}
            
            Curve shortestEdge = land.shortestEdgeOfBase;
            foreach (Curve landcurve in curveWithOffsetedResults.Keys)
            {
                //var start = landcurve.PointAtStart;
                //var end = landcurve.PointAtEnd;
                //var mid = landcurve.PointAtNormalizedLength(0.5);

                //if(land.shortestEdge.Contains(start,Plane.WorldXY, 0.001) == PointContainment.Coincident
                //    && land.shortestEdge.Contains(end, Plane.WorldXY, 0.001) == PointContainment.Coincident
                //    && land.shortestEdge.Contains(mid, Plane.WorldXY, 0.001) == PointContainment.Coincident)

                if(Find.CheckOverlap(landcurve, shortestEdge))
                {
                    //haveReallyShort = true;
                    landShortCurve.Add(landcurve);
                }
            }


            // 此时curveWithOffsetedResults每个只有一条线
            foreach (KeyValuePair<Curve, Curve> pair in curveWithOffsetedResults)
            {
                if(pair.Key.GetLength() <= 11 && offsetBehavioursOfLandcurves[pair.Key].Contains("edge"))
                {
                    continue;
                }

                double xLength;
                if (landShortCurve.Contains(pair.Key)) { xLength = pair.Value.GetLength(); }
                else {xLength = pair.Value.GetLength() + 11; }

/*                if(!reallyShortCurve.Contains( pair.Key ))
                {*/
                    // 如果是end线，那么生成的体块深度应该是building spacing，仅用来裁剪
                if (offsetBehavioursOfLandcurves[pair.Key] == "end")
                {
                    //Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, pair.Value.GetLength() + 10, land.buildingLandSpacing, floorHight);
                    //curveWithBrep[pair.Key] = curveBrep;
                    // 反方向生成
                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, xLength + 10, -buildingDepth, floorHight);
                    curveWithBrep[pair.Key] = curveBrep;
                }

                // 如果是end-boundage线，那么直接不生成
                else if (offsetBehavioursOfLandcurves[pair.Key] == "end-boundage")
                {
                    //curveWithBrep[pair.Key] = null;
                    // 尝试向外反方向生成
                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, xLength + 10, -land.buildingLandSpacing, floorHight);
                    curveWithBrep[pair.Key] = curveBrep;
                }

                else
                {
                    Brep curveBrep = GenerateTheBuildingBrepDirectly.GenerateSingleFloor(pair.Value, landCurve, xLength, buildingDepth, floorHight);
                    curveWithBrep[pair.Key] = curveBrep;
                }
/*                }*/
            }

            List<Brep> sideBreps = new List<Brep>();

            List<Brep> endBreps = new List<Brep>();

            // edge 就加， end就减
            foreach (Curve curve in curveWithBrep.Keys)
            {
                if (!offsetBehavioursOfLandcurves[curve].Contains("end"))
                {
                    sideBreps.Add(curveWithBrep[curve]);
                }
                else
                {
                    endBreps.Add(curveWithBrep[curve]);
                }
            }

            sideBreps.RemoveAll(item => item == null);

            Brep[] finalBrep;

            //if(sideBreps!= null && !sideBreps.Contains(null) || sideBreps.Count != 0)
            //{
            //    foreach (Brep brep in sideBreps)
            //    {
            //        this
            //    }
            //}



            Brep[] solidBreps = Brep.CreateBooleanUnion(sideBreps, 0.01);

            //// 尝试先处理单个再组合
            //Judge judgeBrep = new Judge(curveWithOffsetedResults, land);

            ////return judgeBrep.CreateJudgeBrep();
            //List<Brep> repairedFinalBrep = new List<Brep>();

            //foreach(Brep sideBrench in sideBreps) {
            //    Brep repaired = judgeBrep.RepairFloorBlock(sideBrench);
            //    repairedFinalBrep.Add(repaired);
            //}

            //Brep[] solidBreps = Brep.CreateBooleanUnion(repairedFinalBrep, 0.01);
            //// 处理结束


            //solidBreps = solidBreps.Where(item => item != null).ToArray();

            endBreps.RemoveAll(item => item == null);

            foreach (Brep brep in solidBreps)
            {
                if (brep != null) {
                    if (brep.IsSolid == false) {
                        brep.CapPlanarHoles(0.01);
                    }
                }

            }

            
            if (endBreps.Count() > 0)
            {
                // ---------------------------------------endBrep方法存档-----------------------------------------
                //foreach (Brep brep in endBreps)
                //{
                //    if (brep != null)
                //    {
                //        if (brep.IsSolid == false)
                //        {
                //            brep.CapPlanarHoles(0.01);
                //        }
                //    }
                //}
                ////return solidBreps;
                //finalBrep = Brep.CreateBooleanDifference(solidBreps, endBreps, 0.01);

                //if (solidBreps != null
                //    && endBreps != null
                //    && !solidBreps.Contains(null)
                //    && !endBreps.Contains(null))
                //{
                //    finalBrep = solidBreps;

                //    if (finalBrep == null || finalBrep.Contains(null) || finalBrep.Length == 0)
                //    {

                //        //// 睡眠半秒
                //        //System.Threading.Thread.Sleep(500);

                //        // 换用Split方法
                //        Brep[] judged = solidBreps[0].Split(endBreps, 0.01);

                //        if (judged == null || judged.Contains(null) || judged.Length == 0)
                //            finalBrep = solidBreps;
                //        else
                //        {
                //            var largestOne = Find.FindTheLargestBrep(judged);

                //            if (largestOne.IsSolid == false)
                //                largestOne = largestOne.CapPlanarHoles(0.01);

                //            finalBrep = new Brep[] { largestOne };
                //        }

                //    }
                //}
                //// ---------------------------------------存档完了-----------------------------------------

                finalBrep = solidBreps;
            }

            else
            {
                finalBrep = solidBreps;
            }


            foreach (Brep brep in finalBrep)
            {
                if (brep.IsSolid == false)
                    brep.CapPlanarHoles(0.01);
            }

            if (finalBrep.Length > 1)
                //return new Brep[] { Find.FindTheLargestBrep(finalBrep) };
                finalBrep = new Brep[] { Find.FindTheLargestBrep(finalBrep) };

            else { }
            //this.judgeBrep = new Judge(curveWithOffsetedResults, land).CreateJudgeBrep();

            //return solidBreps;

            // 如果judgebrep不是Solid，那么直接放弃
            if (this.judgeBrep[0].IsSolid == false || this.judgeBrep.Length > 1 || this.judgeBrep == null || this.judgeBrep.Length == 0)
                return finalBrep;

            else
            {
                Brep[] repairedBrep = Brep.CreateBooleanIntersection(this.judgeBrep, finalBrep, 0.01);
                if (repairedBrep == null || repairedBrep.Length == 0 || repairedBrep.Contains(null))
                {
                    Console.WriteLine("Need repair ! !");
                    Brep maxvolumebrep = null;

                    if (finalBrep.Length > 1)
                    {
                        // 取出最大的那一个
                        Brep[] unionSecond = Brep.CreateBooleanUnion(finalBrep, 0.001);

                        if (unionSecond.Length > 1)
                        {
                            //Brep maxvolumebrep = null;
                            double maxvolume = 0.0;

                            foreach (Brep brep in finalBrep)
                            {
                                VolumeMassProperties vmp = VolumeMassProperties.Compute(brep);
                                double volume = vmp.Volume;

                                if (volume > maxvolume)
                                {
                                    maxvolume = volume;
                                    maxvolumebrep = brep;
                                }
                            }
                        }

                        else if (unionSecond.Length == 0)
                            maxvolumebrep = null;
                        else
                            maxvolumebrep = unionSecond[0];
                    }
                    Brep[] judgedSingleBlockBreps = new Brep[] { maxvolumebrep };


                    if (maxvolumebrep == null)
                    {
                        Console.WriteLine("Cannot be completed!");
                        Trace.WriteLine("Cannot be completed!");
                        repairedBrep = null;
                    }


                    else
                    {
                        // 换用Split方法
                        Brep[] judged = maxvolumebrep.Split(this.judgeBrep, 0.01);

                        var largestOne = Find.FindTheLargestBrep(judged);
                        if (largestOne.IsSolid == false)
                            largestOne = largestOne.CapPlanarHoles(0.01);

                        repairedBrep = new Brep[] { largestOne };

                        Console.WriteLine("Repair Complete ! !");
                        Trace.WriteLine("Repair Complete ! !");
                    }

                    return repairedBrep;
                }
                else
                {
                    return repairedBrep;
                }

                return finalBrep;


                //return finalBrep;
                Judge judgeBrep = new Judge(curveWithOffsetedResults, land);
                var repair = judgeBrep.RepairFloorBlock(finalBrep);
                return repair;
            }

        }

        // 判断如何生成并移动
        public List<Brep> CreateAllFloors(Dictionary<Curve, Curve> curveWithOffsetedResults, Dictionary<Curve, string> offsetBehavioursOfLandcurves)
        {
            bool isBoundage = land.isABoundageLand;

            //Dictionary<Curve, Brep> curveWithBrep = new Dictionary<Curve, Brep>();

            List<Brep> copies = new List<Brep>();

            if (isBoundage == true)
                {
                    Brep[] groundfloors = CreateSingleFloorBrep(curveWithOffsetedResults, offsetBehavioursOfLandcurves, "ground");
                    Brep[] otherfloors = CreateSingleFloorBrep(curveWithOffsetedResults, offsetBehavioursOfLandcurves, "others");

                    Brep groundFloor;
                    Brep otherFloor;

                    if (groundfloors == null) { groundFloor = null; }
                        
                    else if (groundfloors.Length==0) { groundFloor = null; }

                    else if (groundfloors.Count() > 1) { groundFloor = FindTheLargestBrep(groundfloors); }

                    else { groundFloor = CreateSingleFloorBrep(curveWithOffsetedResults, offsetBehavioursOfLandcurves, "ground")[0]; }




                    if (otherfloors == null) { otherFloor = null; }

                    else if (otherfloors.Length == 0) { otherFloor = null; }

                    else if (otherfloors.Count() > 1) { otherFloor = FindTheLargestBrep(otherfloors); }

                    else { otherFloor = CreateSingleFloorBrep(curveWithOffsetedResults, offsetBehavioursOfLandcurves, "standard")[0]; }
                        


                    if(groundFloor == null && otherFloor == null)
                    {
                        int index = 0;

                    //while(index < floorNum){
                    //copies.Add(null);
                    //index++;
                    //}

                        for (int i = 0; i < floorNum; i++)
                        {
                            copies.Add(null);
                        }

                    return copies;
                    }

                    var upToFloor2 = new Vector3d(0, 0, this.groundFloorHeight);
                    var upToFloorOthers = new Vector3d(0, 0, this.standardFloorHeight);


                    // 首层
                    copies.Add(groundFloor);

                    // 二层
                    otherFloor.Translate(upToFloor2);
                    copies.Add(otherFloor);

                    // 三层及以上
                    int time = this.floorNum - 2;

                    RecursiveDuplicate(otherFloor, time, copies, upToFloorOthers, 11);

                    return copies;
                }
            else
                {
                    Brep[] baseFloors = CreateSingleFloorBrep(curveWithOffsetedResults, offsetBehavioursOfLandcurves, "standard");

                    Brep baseFloor;

                    if (baseFloors.Count() > 1)
                        baseFloor = FindTheLargestBrep(baseFloors);
                    else
                        baseFloor = CreateSingleFloorBrep(curveWithOffsetedResults, offsetBehavioursOfLandcurves, "standard")[0];
               
                    copies.Add(baseFloor);
                    var upToFloorOthers = new Vector3d(0, 0, this.standardFloorHeight);

                    RecursiveDuplicate(baseFloor, floorNum - 1, copies, upToFloorOthers, 11);

                    return copies;
                }
        }



        // 实验：用递归代替while循环

        public List<Brep> RecursiveDuplicate(Brep brepNeedDuplicate, int time, List<Brep> allBreps, Vector3d upToFloorOthers, int maxDepth)
        {
            if (time > 0 && maxDepth > 0)
            {
                // 先复制一份
                Brep brepAbove = brepNeedDuplicate.DuplicateBrep();

                // 再向上移动形成新的楼层
                Transform translation = Transform.Translation(upToFloorOthers);
                brepAbove.Transform(translation);

                // 加入列表里面
                allBreps.Add(brepAbove);

                // 递归调用，同时减少剩余的复制次数和最大深度
                RecursiveDuplicate(brepAbove, time - 1, allBreps, upToFloorOthers, maxDepth - 1);
            }

            else
            {
                return allBreps;
            }

            return allBreps;
        }


        //public List<Brep> RecursiveDuplicate(Brep brepNeedDuplicate, int time, List<Brep> allBreps, Vector3d upToFloorOthers)
        //{
        //    Stack<Tuple<Brep, int>> stack = new Stack<Tuple<Brep, int>>();
        //    stack.Push(new Tuple<Brep, int>(brepNeedDuplicate, time));

        //    while (stack.Count > 0)
        //    {
        //        Tuple<Brep, int> current = stack.Pop();
        //        Brep currentBrep = current.Item1;
        //        int currentTime = current.Item2;

        //        if (currentTime == 0)
        //        {
        //            continue;
        //        }

        //        // 先复制一份
        //        Brep brepAbove = currentBrep.DuplicateBrep();
        //        //再向上移动形成新的楼层

        //        Transform translation = Transform.Translation(upToFloorOthers);
        //        brepAbove.Transform(translation);
        //        // 加入字典里面
        //        allBreps.Add(brepAbove);

        //        stack.Push(new Tuple<Brep, int>(brepAbove, currentTime - 1));
        //    }

        //    return allBreps;
        //}


    //    public List<Brep> RecursiveDuplicate(Brep brepNeedDuplicate, int time, List<Brep> allBreps, Vector3d upToFloorOthers)
    //{

    //    if (time == 0)
    //    {
    //        return allBreps;
    //    }
    //    else
    //    {
    //        // 先复制一份
    //        Brep brepAbove = brepNeedDuplicate.DuplicateBrep();
    //        //再向上移动形成新的楼层

    //        Transform translation = Transform.Translation(upToFloorOthers);
    //        brepAbove.Transform(translation);
    //        // 加入字典里面
    //        allBreps.Add(brepAbove);
    //        //emptySketchWithDirection[sketchAbove] = vectorOfTheSketch;
    //        //递归
    //        return RecursiveDuplicate(brepNeedDuplicate: brepAbove,
    //                                        time: time - 1,
    //                                        allBreps,
    //                                        upToFloorOthers: upToFloorOthers);
    //    }

    //}

        public Brep FindTheLargestBrep(Brep[] multipleBreps)
        {
            // 判断最大的
            Brep maxVolumeBrep = null;
            double maxVolume = 0.0;

            foreach (Brep brep in multipleBreps)
            {
                //VolumeMassProperties vmp = VolumeMassProperties.Compute(brep);
                //double volume = vmp.Volume;
                double volume = brep.GetVolume();

                if (volume > maxVolume)
                {
                    maxVolume = volume;
                    maxVolumeBrep = brep;
                }
            }
            //if (maxVolumeBrep != null)
            //    maxVolumeBrep = multipleBreps[0];
            return maxVolumeBrep;
        }
  

        /*-------------------------------指标计算-----------------------------------------*/

        //// 获取底面
        //public /*BrepFace*/ void GetBottomSurface()
        //{
        //    if (allFloors == null || allFloors.Count == 0 || allFloors.Contains(null) == true || allFloors[0] == null)
        //    {
        //        this.singleFloorArea += 0;
        //        //return null;
        //    }

        //    else
        //    {
        //        Brep brep = this.allFloors[0];
        //        BrepFace bottomFace = null;

        //        //bottomFace = Find.FindTheLargestBrepFace(brep.Faces.ToArray());

        //        List<BrepFace> bottomFaces = new List<BrepFace>();

        //        foreach (BrepFace face in brep.Faces)
        //        {
        //            Vector3d normal = face.NormalAt(face.Domain(0).Mid, face.Domain(1).Mid);
        //            if (normal.Z < -0.5) // 底面法线方向朝下
        //            {
        //                bottomFaces.Add(face);
        //                //bottomFace = face;
        //                /*break*/
        //                ;
        //            }
        //        }

        //        foreach (var brepFace in bottomFaces)
        //        {
        //            this.singleFloorArea += AreaMassProperties.Compute(brepFace).Area;
        //        }

        //        //return bottomFace;
        //    }

        //}

        public /*BrepFace*/ double GetBottomSurfaceArea()
        {
            if (allFloors == null || allFloors.Count == 0 || allFloors.Contains(null) == true || allFloors[0] == null)
            {
                this.singleFloorArea += 0;
                return 0;
            }

            else
            {
                Brep brep = this.allFloors[0];



                List<BrepFace> bottomFaces = new List<BrepFace>();

                foreach (BrepFace face in brep.Faces)
                {
                    Vector3d normal = face.NormalAt(face.Domain(0).Mid, face.Domain(1).Mid);

                    if (normal.Z < -0.5) // 底面法线方向朝下
                    {
                        bottomFaces.Add(face);
                    }
                }

                foreach (var brepFace in bottomFaces)
                {
                    this.singleFloorArea += AreaMassProperties.Compute(brepFace).Area;
                }
                return this.singleFloorArea;
                //return bottomFace;
            }

        }


        public double GetBuildingArea(List<Curve> floors)
    {
        double totalAreaOfBuildingFloors = 0;
        foreach (Curve floor in floors)
        {
            // 判断floor是不是封闭曲线
            if (floor.IsClosed == false)
            {
                // 如果不是则尝试封闭
                floor.MakeClosed(0.001);
            }
            // 封闭曲线floor的面积
            double area = AreaMassProperties.Compute(floor).Area;
            totalAreaOfBuildingFloors += area;
        }
        return totalAreaOfBuildingFloors;
    }

        public double GetBaseProjectedArea(Curve sketch)
    {
        // 获得封闭曲线sketch的面积
        double area = AreaMassProperties.Compute(sketch).Area;
        return area;
    }

        public double GetEstimatedRoomAccount(/*List<Curve> floors*/)
        {
            //// 获得单层楼的sketch
            //List <Curve> floors = new List<Curve> { floorSketch };
            // 单层楼的面积
            double areaOfSingleFloor = 0;

            if(this.bottomarea != 0)
            {
                areaOfSingleFloor = this.singleFloorArea;
            }

            
            //if (this.bottomSurface != null)
            //    areaOfSingleFloor = AreaMassProperties.Compute(bottomSurface).Area;


            double buildingDepth = land.GetBuildingDepth();

            // 总的建筑面积
            double totalAreaOfBuildingFloors;

            if (floorNum == 0)
            {
                return 0;
            }
            else
            {
                totalAreaOfBuildingFloors = areaOfSingleFloor * floorNum;
            }

            // 楼梯间的面积
            double staircaseArea = this.land.staircaseWidth * buildingDepth;

            // 电梯间的面积
            double elevatorArea = this.land.elevatorWidth * buildingDepth;

            // 房间的面积
            double roomArea = buildingDepth * land.roomWidth;

            // 房间的数量
            return (totalAreaOfBuildingFloors - floorNum * (staircaseArea + elevatorArea)) / roomArea;
        }

    }

} 

