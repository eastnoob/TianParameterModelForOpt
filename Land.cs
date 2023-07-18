using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Grasshopper.Kernel.Geometry;
using Rhino.Geometry.Intersect;
using static Rhino.Render.ChangeQueue.Light;
using System.Security.Cryptography;
using System.Runtime.Remoting.Messaging;
using Rhino.Commands;
using System.Numerics;
using System.Security.Policy;
using System.Collections;

namespace TianParameterModelForOpt
{
    public class Land
    {
        // 输入参数
        //public List<Curve> baseCurves;
        //public Curve baseCurve;
        //public List<Curve> lands;
        public Curve landCurve;
        public Curve baseCurve;
        public double roomDepth;
        public double roomWidth;
        public double corridorWidth;
        public double staircaseWidth;
        public double elevatorWidth;
        public double buildingSpacing;
        public List<Curve> zoneWestEast;
        public List<Curve> zoneNorthSouth;


        // 计算参数
        public double buildingLandSpacing;

        public double absulatTolerance;

        // 判断地块是否处于基地边界上
        public bool isABoundageLand;
        
        // 判断地块的方向隶属
        public string isWestOrEast;
        public string isNorthOrSouth;

        // 初始化时候分好方向
        public Dictionary<string, List<Curve>> dispatchedEdges;

        // 初始化时候确定好地块的边缘问题
        public Dictionary<string, List<Curve>> onBoundage;
        public Dictionary<string, List<Curve>> notOnBoundage;
        public List<string> boundageDirections;
        public bool boundageOrNot;

        // 这个land中属于boundage的方向
        public List<string> boundageDirectionsInLand;

        // 炸开后的land边缘集合，以及land中每个边缘的长度
        public Dictionary<string, List<Curve>> fourDirectionsEdges;
        public Dictionary<string, double> directionAndLength;

        //// 东西南北
        //public string westOrEast;
        //public string northOrSouth;

        // 特殊变量，看情况决定是否启用
        public double floorHeight;
        public double floorNum;

        // 构造器，包含以下属性：base, lands, roomDepth, roomWidth, corridorWidth, staircaseWidth, elevatorWidth, buildingSpacing, 都是单个物体，而不是list
        public Land (/*List<Curve> baseCurves, */ Curve unoffsetedBaseCurve, Curve landCurve, /*List<Curve> lands,*/
        double roomDepth, double roomWidth, double corridorWidth, double staircaseWidth, double elevatorWidth, double buildingSpacing,
            List<Curve> zoneWestEast, List<Curve> zoneNorthSouth)
        {
            //this.baseCurves = baseCurves;
            //this.baseCurve = baseCurve;
            //this.lands = lands;
            this.landCurve = landCurve;
            this.baseCurve = unoffsetedBaseCurve.Offset(AreaMassProperties.Compute(unoffsetedBaseCurve).Centroid, Rhino.Geometry.Plane.WorldXY.Normal, 5, 0.001, CurveOffsetCornerStyle.Sharp)[0]; ;
            this.roomDepth = roomDepth;
            this.roomWidth = roomWidth;
            this.corridorWidth = corridorWidth;
            this.staircaseWidth = staircaseWidth;
            this.elevatorWidth = elevatorWidth;
            this.buildingSpacing = buildingSpacing;
            

            // 以下是计算属性
            this.absulatTolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;


            this.buildingLandSpacing = buildingSpacing / 2;
            this.isABoundageLand = IsABoundageLand(landCurve, this.baseCurve, out Dictionary<string, List<Curve>> onBoundage, out Dictionary<string, List<Curve>> notOnBoundage, out boundageDirectionsInLand);
            
            this.isWestOrEast = IsWestOrEast(zoneWestEast, landCurve);
            this.isNorthOrSouth = IsNorthOrSouth(zoneNorthSouth, landCurve);

            this.dispatchedEdges = DispatchEdgesThroughDirection(landCurve);

            this.directionAndLength = GetLengthsAndLandlines(landCurve, out fourDirectionsEdges);

            //this.westOrEast = IsWestOrEast(zones, landCurve);
            //this.northOrSouth = IsNorthOrSouth(zones, landCurve);
        }


        /*----------------------------------------类属性计算-----------------------------------------------*/


        /// <summary>
        /// 按照东南西北进行分类，将land的边界分为四个方向以及是否在边界上，并返回其是否处在基地的边界上
        /// </summary>
        /// <param name="landCurve"> 这个land的图形</param>
        /// <param name="baseCurve">基地的图形</param>
        /// <param name="onBoundage">输出，land的boundage边</param>
        /// <param name="notOnBoundage">输出，land的非boundage边</param>
        /// <param name="boundageDirections">输出，land中属于boundage的边的方向</param>
        /// <returns>布尔值，这个啦奶是否属于boundage类型</returns>
        private bool IsABoundageLand(Curve landCurve, Curve baseCurve, 
            out Dictionary<string, List<Curve>> onBoundage, 
            out Dictionary<string, List<Curve>> notOnBoundage,
            out List<string> boundageDirections)
        {
            var relationshipDic = DispatchEdgesThroughDirection(landCurve);

            // 在这一步把land的边界信息以及方向字典算出来
            //Dictionary<string, List<Curve>> onBoundage = new Dictionary<string, List<Curve>>();
            //Dictionary<string, List<Curve>> notOnBoundage = new Dictionary<string, List<Curve>>();

            bool isABoundageLand = BoundageOrNot(relationshipDic, baseCurve, out onBoundage, out notOnBoundage, out boundageDirections);

            return isABoundageLand;

        }

        /// <summary>
        /// FindZone 函数接受两个参数: 一个是 zones，类型是 List<Curve>；另一个是 closedCurveToCheck，类型是 Curve，函数将找到封闭曲线内指定点的位置。
        /// </summary>
        /// <param name="zones"></param>
        /// <param name="land"></param>
        /// <returns> 字符串 east则为land在东，west则为在西 </returns>
        public string IsWestOrEast(List<Curve> zones, Curve land)
        {
            int zoneIndex = -1;

            // Get centroid of curve-to-check
            Point3d centroid = AreaMassProperties.Compute(land).Centroid;

            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].Contains(centroid, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                {
                    zoneIndex = i;
                    break;
                }
            }

            if (zoneIndex == -1)
                return "east";
            else if (zoneIndex == 0)
                return "west";
            else
                return "east";
        }

        public string IsNorthOrSouth(List<Curve> zones, Curve land)
        {
            int zoneIndex = -1;

            // Get centroid of curve-to-check
            Point3d centroid = AreaMassProperties.Compute(land).Centroid;

            for (int i = 0; i < zones.Count; i++)
            {
                if (zones[i].Contains(centroid, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                {
                    zoneIndex = i;
                    break;
                }
            }

            if (zoneIndex == -1)
                return "north";
            else if (zoneIndex == 0)
                return "south";
            else
                return "north";
        }



        // 获得封闭曲线land拆成四个方向的边界,并获得东西南北的长度
        public Dictionary<string, double> GetLengthsAndLandlines(Curve landCurve, out Dictionary<string, List<Curve>> fourDirectionsEdges)
        {
            fourDirectionsEdges = DispatchEdgesThroughDirection(landCurve);

            // 获得四个方向各自所有的curve的长度之和，分别存放于一个字典中，键为方向，值为长度
            Dictionary<string, double> directionAndLengths = new Dictionary<string, double>();
            foreach (var direction in fourDirectionsEdges.Keys)
            {
                double length = 0;
                foreach (var edge in fourDirectionsEdges[direction])
                {
                    length += edge.GetLength();
                }
                directionAndLengths.Add(direction, length);
            }
            return directionAndLengths;
        }




        /// <summary>
        /// 获得封闭曲线的长度与宽度. 宽度按照land的最短边的长度计算，长度按照最长边的长度计算.
        /// </summary>
        /// <param name="land">定义封闭曲线的PolylineCurve对象.</param>
        /// <param name="length">封闭曲线的长度.</param>
        /// <param name="width">封闭曲线的宽度.</param>
        /// 



        /*----------------------------------------计算数值的方法-------------------------------------------*/

        public double GetBuildingDepth()
        {
            // 函数说明
            // 输入：无
            // 输出：buildingDepth，建筑深度
            // 功能：根据房间深度，走廊宽度, 计算建筑深度

            double buildingDepth = roomDepth + corridorWidth;

            return buildingDepth;
        }

        public double GetShortestEndDepth()
        {
            double shortestEndDepth = 1.0;
            if (this.isABoundageLand == true)
                shortestEndDepth = buildingSpacing + roomDepth + corridorWidth;

            else
                shortestEndDepth = 2*buildingSpacing + roomDepth + corridorWidth;
            
            return shortestEndDepth;
        }

        public double GetShortestBLength()
        {
            if(this.isABoundageLand == true)
                return 5 * roomWidth + staircaseWidth + elevatorWidth + buildingSpacing;
            else
                return (5 * roomWidth) + staircaseWidth + elevatorWidth + buildingSpacing + (2*buildingSpacing);
        }

        public double GetShortestLLength()
        {
            if (this.isABoundageLand == true)
                return 10 * roomWidth + staircaseWidth + elevatorWidth + buildingSpacing + buildingSpacing;
            else
                return 10 * roomWidth + staircaseWidth + elevatorWidth + buildingSpacing + buildingSpacing + 2 * buildingSpacing;
        }

        public double GetShortestULength()
        {
            double buildingHeight = this.floorHeight * this.floorNum;
            double buildingInterval = buildingHeight * 1.2;

            if (buildingInterval <= 13)
                buildingInterval = 13;

            if(this.isABoundageLand == true)
                return buildingInterval + 2*roomWidth + staircaseWidth + elevatorWidth + buildingSpacing + buildingSpacing + buildingSpacing;
            else
                return buildingInterval + 2 * roomWidth + staircaseWidth + elevatorWidth + buildingSpacing + buildingSpacing + buildingSpacing + 2*buildingSpacing;
        }

        public double GetShortestOLength()
        {
            double buildingHeight = this.floorHeight * this.floorNum;
            double buildingInterval = buildingHeight * 1.2;

            if (buildingInterval <= 13)
                buildingInterval = 13;

            if (this.isABoundageLand == true)
                return buildingInterval + 4* roomWidth + staircaseWidth + elevatorWidth + buildingSpacing + buildingSpacing + buildingSpacing;
            else
                return buildingInterval + 4* roomWidth + staircaseWidth + elevatorWidth + buildingSpacing + buildingSpacing + buildingSpacing + 2 * buildingSpacing;
        }


        /*-----------------------------------------------------------*/



        /*----------------------------方向处理方法，用于定义物体不同边的方向-----------------------------------*/

        public double GetDirection(Curve land, out List<Point3d> directionsPtList, out List<Line> directLineList)
        {
            // 计算地块的重心
            Point3d ct = AreaMassProperties.Compute(land).Centroid;
            Point3d startPt = ct; // 方向线的起点

            // 计算地块边缘线段的长度
            Curve[] landLines = land.DuplicateSegments();
            List<double> landLinesLengths = new List<double>();

            foreach (Curve line in landLines)
            {
                double length = line.GetLength();
                landLinesLengths.Add(length);
            }

            // 计算方向线的终点
            Point3d northEndPt = ct + new Vector3d(0, landLinesLengths.Max(), 0);
            Point3d southEndPt = ct + new Vector3d(0, -landLinesLengths.Max(), 0);
            Point3d eastEndPt = ct + new Vector3d(landLinesLengths.Max(), 0, 0);
            Point3d westEndPt = ct + new Vector3d(-landLinesLengths.Max(), 0, 0);

            directionsPtList = new List<Point3d> { northEndPt, southEndPt, eastEndPt, westEndPt };

            // 连接起点和终点，创建直线
            Line northDirect = new Line(startPt, northEndPt);
            Line southDirect = new Line(startPt, southEndPt);
            Line eastDirect = new Line(startPt, eastEndPt);
            Line westDirect = new Line(startPt, westEndPt);

            directLineList = new List<Line> { northDirect, southDirect, eastDirect, westDirect };

            // 返回方向线的终点、直线列表和地块边缘线段的最大长度
            return landLinesLengths.Max();
        }

        // 对于单个封闭线进行的分方向操作
        public Dictionary<string, List<Curve>> DispatchEdgesThroughDirection(Curve closeCurve)
        {

            // 方向点
            List<Point3d> directionPts;

            // 方向线
            List<Line> directionLines;

            // 获取方向点与方向线
            double maxEdgeLength = GetDirection(closeCurve, out directionPts, out directionLines);

            // 获取每个土地块的边缘线
            // 将曲线分解为线段
            List<Curve> landCurves = new List<Curve>();

            if (closeCurve.IsClosed)
            {
                Curve[] segments = closeCurve.DuplicateSegments();
                if (segments != null && segments.Length > 0)
                {
                    landCurves.AddRange(segments);
                }
            }
            // 如果没有闭合，则先将其闭合再执行同样操作
            else
            {
                Curve closedCurve = closeCurve.DuplicateCurve();
                closedCurve.MakeClosed(0.001);
                Curve[] segments = closedCurve.DuplicateSegments();
                if (segments != null && segments.Length > 0)
                {
                    landCurves.AddRange(segments);
                }
            }


            // 按照方向顺序排序
            string[] directionOrder = { "north", "south", "east", "west" };

            // 获取边缘线数量
            int edgeCount = landCurves.Count;

            // 获取每条边缘线的中点
            List<Point3d> edgeMidpoints = new List<Point3d>();
            foreach (Curve landLine in landCurves)
            {
                edgeMidpoints.Add(landLine.PointAtNormalizedLength(0.5));
            }

            // 计算每条边缘线与方向线之间的角度
            List<double> northAngles = new List<double>();
            List<double> southAngles = new List<double>();
            List<double> eastAngles = new List<double>();
            List<double> westAngles = new List<double>();

            foreach (Curve landLine in landCurves)
            {
                northAngles.Add(Math.Round(Vector3d.VectorAngle(directionLines[0].Direction, landLine.TangentAtStart), 2));
                southAngles.Add(Math.Round(Vector3d.VectorAngle(directionLines[1].Direction, landLine.TangentAtStart), 2));
                eastAngles.Add(Math.Round(Vector3d.VectorAngle(directionLines[2].Direction, landLine.TangentAtStart), 2));
                westAngles.Add(Math.Round(Vector3d.VectorAngle(directionLines[3].Direction, landLine.TangentAtStart), 2));
            }

            // 计算每条边缘线与方向点之间的距离
            List<double> northDistances = new List<double>();
            List<double> southDistances = new List<double>();
            List<double> eastDistances = new List<double>();
            List<double> westDistances = new List<double>();

            foreach (Point3d edgeMidpoint in edgeMidpoints)
            {
                northDistances.Add(Math.Round(edgeMidpoint.DistanceTo(directionPts[0]), 2));
                southDistances.Add(Math.Round(edgeMidpoint.DistanceTo(directionPts[1]), 2));
                eastDistances.Add(Math.Round(edgeMidpoint.DistanceTo(directionPts[2]), 2));
                westDistances.Add(Math.Round(edgeMidpoint.DistanceTo(directionPts[3]), 2));
            }

            // 如果边缘线数量小于4，则跳过
            if (edgeCount < 4)
            {
                return null;
            }

            else if (edgeCount == 4)
            {
                var relationshipDicFour = new Dictionary<string, List<Curve>>();
                relationshipDicFour["north"] = new List<Curve>();
                relationshipDicFour["south"] = new List<Curve>();
                relationshipDicFour["east"] = new List<Curve>();
                relationshipDicFour["west"] = new List<Curve>();

                relationshipDicFour["north"].Add(landCurves[northDistances.IndexOf(northDistances.Min())]);
                relationshipDicFour["south"].Add(landCurves[southDistances.IndexOf(southDistances.Min())]);
                relationshipDicFour["east"].Add(landCurves[eastDistances.IndexOf(eastDistances.Min())]);
                relationshipDicFour["west"].Add(landCurves[westDistances.IndexOf(westDistances.Min())]);

                /*                var relationshipDicFour = new Dictionary<string, List<Line>>();
                                relationshipDicFour["north"] = new List<Line> { landCurves[northDistances.IndexOf(northDistances.Min())] };
                                relationshipDicFour["south"] = new List<Line> { landCurves[southDistances.IndexOf(south_distance.Min())] };
                                relationshipDicFour["east"] = new List<Line> { landCurves[eastDistances.IndexOf(east_distance.Min())] };
                                relationshipDicFour["west"] = new List<Line> { landCurves[westDistances.IndexOf(west_distance.Min())] };*/



                // RelationshipList is a list which contain items according to the order as [north, south, east, west]
                // ! Might be abandoned
                /*                var relationshipListFour = new List<Line>
                                {
                                    land_lines[north_distance.IndexOf(north_distance.Min())],
                                    land_lines[south_distance.IndexOf(south_distance.Min())],
                                    land_lines[east_distance.IndexOf(east_distance.Min())],
                                    land_lines[west_distance.IndexOf(west_distance.Min())]
                                };*/

                return relationshipDicFour;
            }
            else if (edgeCount > 4)
            {
                int maxLength = (int)Math.Round(maxEdgeLength);

                // 分别算出符合条件的边缘线种子选手，最后再通过评分来确定其方向

                List<Curve> northAppropriateEdges = new List<Curve>();
                List<Curve> southAppropriateEdges = new List<Curve>();
                List<Curve> eastAppropriateEdges = new List<Curve>();
                List<Curve> westAppropriateEdges = new List<Curve>();

                for (int i = 0; i < northAngles.Count; i++)
                {
                    if (120 > northAngles[i] && northAngles[i] >= 60)
                    {
                        northAppropriateEdges.Add(landCurves[i]);
                    }
                }

                for (int i = 0; i < southAngles.Count; i++)
                {
                    if (120 > southAngles[i] && southAngles[i] >= 60)
                    {
                        southAppropriateEdges.Add(landCurves[i]);
                    }
                }

                for (int i = 0; i < eastAngles.Count; i++)
                {
                    if (120 > eastAngles[i] && eastAngles[i] >= 60)
                    {
                        eastAppropriateEdges.Add(landCurves[i]);
                    }
                }

                for (int i = 0; i < westAngles.Count; i++)
                {
                    if (120 > westAngles[i] && westAngles[i] >= 60)
                    {
                        westAppropriateEdges.Add(landCurves[i]);
                    }
                }

                Dictionary<Curve, double> northAppropriateEdgesDistance = new Dictionary<Curve, double>();
                Dictionary<Curve, double> southAppropriateEdgesDistance = new Dictionary<Curve, double>();
                Dictionary<Curve, double> eastAppropriateEdgesDistance = new Dictionary<Curve, double>();
                Dictionary<Curve, double> westAppropriateEdgesDistance = new Dictionary<Curve, double>();

                foreach (Curve edge in northAppropriateEdges)
                {
                    northAppropriateEdgesDistance.Add(edge, northDistances[landCurves.IndexOf(edge)]);
                }

                foreach (Curve edge in southAppropriateEdges)
                {
                    southAppropriateEdgesDistance.Add(edge, southDistances[landCurves.IndexOf(edge)]);
                }

                foreach (Curve edge in eastAppropriateEdges)
                {
                    eastAppropriateEdgesDistance.Add(edge, eastDistances[landCurves.IndexOf(edge)]);
                }

                foreach (Curve edge in westAppropriateEdges)
                {
                    westAppropriateEdgesDistance.Add(edge, westDistances[landCurves.IndexOf(edge)]);
                }

                // 计算合适的角度

                //Dictionary<Curve, double> northAppropriateEdgesAngle = new Dictionary<Curve, double>();
                //Dictionary<Curve, double> southAppropriateEdgesAngle = new Dictionary<Curve, double>();
                //Dictionary<Curve, double> eastAppropriateEdgesAngle = new Dictionary<Curve, double>();
                //Dictionary<Curve, double> westAppropriateEdgesAngle = new Dictionary<Curve, double>();

                //foreach (Curve edge in northAppropriateEdges)
                //{
                //    northAppropriateEdgesAngle.Add(edge, northAngles[landCurves.IndexOf(edge)]);
                //}

                //foreach (Curve edge in southAppropriateEdges)
                //{
                //    southAppropriateEdgesAngle.Add(edge, southAngles[landCurves.IndexOf(edge)]);
                //}

                //foreach (Curve edge in eastAppropriateEdges)
                //{
                //    eastAppropriateEdgesAngle.Add(edge, eastAngles[landCurves.IndexOf(edge)]);
                //}

                //foreach (Curve edge in westAppropriateEdges)
                //{
                //    westAppropriateEdgesAngle.Add(edge, westAngles[landCurves.IndexOf(edge)]);
                //}

                /*---------------------------以下是评分------------------------------*/

                //// 评分
                //Dictionary<Curve, double> northScore = new Dictionary<Curve, double>();
                //Dictionary<Curve, double> southScore = new Dictionary<Curve, double>();
                //Dictionary<Curve, double> eastScore = new Dictionary<Curve, double>();
                //Dictionary<Curve, double> westScore = new Dictionary<Curve, double>();

                //foreach (Curve edge in landCurves)
                //{
                //    if (northAppropriateEdges.Contains(edge))
                //    {
                //        northScore[edge] = SetScore(northAppropriateEdgesAngle[edge], northAppropriateEdgesDistance[edge], maxLength);
                //    }

                //    if (southAppropriateEdges.Contains(edge))
                //    {
                //        southScore[edge] = SetScore(southAppropriateEdgesAngle[edge], southAppropriateEdgesDistance[edge], maxLength);
                //    }

                //    if (eastAppropriateEdges.Contains(edge))
                //    {
                //        eastScore[edge] = SetScore(eastAppropriateEdgesAngle[edge], eastAppropriateEdgesDistance[edge], maxLength);
                //    }

                //    if (westAppropriateEdges.Contains(edge))
                //    {
                //        westScore[edge] = SetScore(westAppropriateEdgesAngle[edge], westAppropriateEdgesDistance[edge], maxLength);
                //    }

                //// 首次区分

                //List<Curve> judgeList = new List<Curve>();
                //List<Curve> firstCandidates = new List<Curve>();
                //firstCandidates.AddRange(northAppropriateEdges);
                //firstCandidates.AddRange(southAppropriateEdges);
                //firstCandidates.AddRange(eastAppropriateEdges);
                //firstCandidates.AddRange(westAppropriateEdges);

                // 首次区分方向的结果
                var relationshipDicFirst = new Dictionary<string, List<Curve>>()
                {
                    {"north", new List<Curve>() },
                    {"south", new List<Curve>() },
                    {"east", new List<Curve>() },
                    {"west", new List<Curve>() }
                };

                // 挑选四个方向的AppropriateEdgesDistance最小的值所对应的key，加入到relationshipDicFirst的对应方向中

                double minNorthDistance = northAppropriateEdgesDistance.Values.Min();
                double minSouthDistance = southAppropriateEdgesDistance.Values.Min();
                double minEastDistance = eastAppropriateEdgesDistance.Values.Min();
                double minWestDistance = westAppropriateEdgesDistance.Values.Min();

                // 通过以上的值搜索对应的key，并加入到relationshipDicFirst的对应方向中，四个方向的key值不会重复，各一个

                List<Curve> judgeListFirst = new List<Curve>();

                foreach (Curve appropriate in northAppropriateEdgesDistance.Keys)
                {
                    if (northAppropriateEdgesDistance[appropriate] == minNorthDistance && !judgeListFirst.Contains(appropriate))
                    {
                        relationshipDicFirst["north"].Add(appropriate);
                        judgeListFirst.Add(appropriate);
                    }
                }
                foreach (Curve appropriate in southAppropriateEdgesDistance.Keys)
                {
                    if (southAppropriateEdgesDistance[appropriate] == minNorthDistance && !judgeListFirst.Contains(appropriate))
                    {
                        relationshipDicFirst["south"].Add(appropriate);
                        judgeListFirst.Add(appropriate);
                    }
                }

                foreach (Curve appropriate in eastAppropriateEdgesDistance.Keys)
                {
                    if (eastAppropriateEdgesDistance[appropriate] == minNorthDistance && !judgeListFirst.Contains(appropriate))
                    {
                        relationshipDicFirst["east"].Add(appropriate);
                        judgeListFirst.Add(appropriate);
                    }
                }

                foreach (Curve appropriate in westAppropriateEdgesDistance.Keys)
                {
                    if (westAppropriateEdgesDistance[appropriate] == minNorthDistance && !judgeListFirst.Contains(appropriate))
                    {
                        relationshipDicFirst["west"].Add(appropriate);
                        judgeListFirst.Add(appropriate);
                    }
                }

                //// ------------------------ 以下方法做存档，暂时弃用 -----------------------------------------

                //// 将四个方向上适宜作为第一个顶点的边添加到firstCandidates列表中

                //foreach (Curve landCurve in landCurves)
                //{
                //    if (northScore.ContainsKey(landCurve) && !judgeList.Contains(landCurve))
                //    {
                //        // 如果边在northScore字典中且还没有被处理过
                //        if (northScore[landCurve] == northScore.Values.Max() && relationshipDicFirst["north"].Count == 0)
                //        {
                //            // 如果边的分数是northScore中最大的，并且还没有确定过north方向的第一个顶点
                //            relationshipDicFirst["north"].Add(landCurve);
                //            judgeList.Add(landCurve);
                //        }
                //    }

                //    if (southScore.ContainsKey(landCurve) && !judgeList.Contains(landCurve))
                //    {
                //        // 如果边在southScore字典中且还没有被处理过
                //        if (southScore[landCurve] == southScore.Values.Max() && relationshipDicFirst["south"].Count == 0)
                //        {
                //            // 如果边的分数是southScore中最大的，并且还没有确定过south方向的第一个顶点
                //            relationshipDicFirst["south"].Add(landCurve);
                //            judgeList.Add(landCurve);
                //        }
                //    }

                //    if (eastScore.ContainsKey(landCurve) && !judgeList.Contains(landCurve))
                //    {
                //        // 如果边在eastScore字典中且还没有被处理过
                //        if (eastScore[landCurve] == eastScore.Values.Max() && relationshipDicFirst["east"].Count == 0)
                //        {
                //            // 如果边的分数是eastScore中最大的，并且还没有确定过east方向的第一个顶点
                //            relationshipDicFirst["east"].Add(landCurve);
                //            judgeList.Add(landCurve);
                //        }
                //    }

                //    if (westScore.ContainsKey(landCurve) && !judgeList.Contains(landCurve))
                //    {
                //        // 如果边在westScore字典中且还没有被处理过
                //        if (westScore[landCurve] == westScore.Values.Max() && relationshipDicFirst["west"].Count == 0)
                //        {
                //            // 如果边的分数是westScore中最大的，并且还没有确定过west方向的第一个顶点
                //            relationshipDicFirst["west"].Add(landCurve);
                //            judgeList.Add(landCurve);
                //        }
                //    }

                // -----------------------------进行方向检查并且重新调整--------------------------
                // 建立一个字典，四个方向为key，value是朝向四个方向的四个单位向量
                var directionDic = new Dictionary<string, Vector3d>()
                    {
                        {"north", new Vector3d(0, 1, 0) },
                        {"south", new Vector3d(0, -1, 0) },
                        {"east", new Vector3d(1, 0, 0) },
                        {"west", new Vector3d(-1, 0, 0) }
                    };

                // 确认现在的landCurves中所有不在judgeListFirst中的边
                Curve[] unusedFirst = new Curve[landCurves.Count - judgeListFirst.Count];

                int unusedFirstIndex = 0;

                foreach (Curve curve in landCurves)
                {
                    if (!judgeListFirst.Contains(curve))
                    {
                        unusedFirst[unusedFirstIndex] = curve;
                        unusedFirstIndex++;
                    }
                }

                // 求出relationshipDicFirst中南北两个方向现在有的curve的中点
                Point3d northMidPoint = new Point3d();
                Point3d southMidPoint = new Point3d();

                foreach (Curve curve in relationshipDicFirst["north"])
                {
                    northMidPoint = curve.PointAtNormalizedLength(0.5);
                }

                foreach (Curve curve in relationshipDicFirst["south"])
                {
                    southMidPoint = curve.PointAtNormalizedLength(0.5);
                }

                // 两点连线，将这条线向东向西分别拷贝一份，距离为100000，然后获得原版与拷贝结果的起止点
                LineCurve northSouthLine = new LineCurve(northMidPoint, southMidPoint);

                LineCurve eastLine = (LineCurve)northSouthLine.DuplicateCurve();
                LineCurve westLine = (LineCurve)northSouthLine.DuplicateCurve();

                //// 原版起止点
                //Point3d originStartPoint = northMidPoint;
                //Point3d originEndPoint = southMidPoint;

                //将两条线向东向西分别复制一份，距离为100000，然后获得原版与拷贝结果的起止点
                LineCurve eastCup = (LineCurve)northSouthLine.DuplicateCurve();
                eastCup.Transform(Transform.Translation(100000 * directionDic["east"]));

                LineCurve westCup = (LineCurve)northSouthLine.DuplicateCurve();
                westCup.Transform(Transform.Translation(100000 * directionDic["west"]));

                Point3d eastCupStartPoint = eastLine.PointAtStart;
                Point3d eastCupEndPoint = eastLine.PointAtEnd;

                Point3d westCupStartPoint = westLine.PointAtStart;
                Point3d westCupEndPoint = westLine.PointAtEnd;

                // 分别将原版的起点终点，与拷贝点的起点终点连线，并组合这些曲线，成两个方向的两条闭合曲线。

                LineCurve eastZoneStartCurve = new LineCurve(northMidPoint, eastCupStartPoint);
                LineCurve eastZoneEndCurve = new LineCurve(southMidPoint, eastCupEndPoint);
                LineCurve[] eastZoneLines = new LineCurve[4] { eastZoneStartCurve, eastZoneEndCurve, eastCup, eastLine };

                var eastZone = LineCurve.JoinCurves(eastZoneLines)[0];
                //LineCurve eastZone = LineCurve.JoinCurves(eastZoneLines);

                LineCurve westZoneStartCurve = new LineCurve(northMidPoint, westCupStartPoint);
                LineCurve westZoneEndCurve = new LineCurve(southMidPoint, westCupEndPoint);
                LineCurve[] westZoneLines = new LineCurve[4] { westZoneStartCurve, westZoneEndCurve, westCup, westLine };

                var westZone = LineCurve.JoinCurves(westZoneLines)[0];

                //Curve[] northCurves = new Curve[2];
                //Curve[] southCurves = new Curve[2];

                //northCurves[0] = new LineCurve(originStartPoint, northLineStartPoint);
                //northCurves[1] = new LineCurve(originEndPoint, northLineEndPoint);

                //southCurves[0] = new LineCurve(originStartPoint, southLineStartPoint);
                //southCurves[1] = new LineCurve(originEndPoint, southLineEndPoint);

                // 求出每条线以及它的中间点，然后判断中间点是否在东西两个区域内，如果在，就将这条线加入到relationshipDicFirst的对应方向中


                Dictionary<Point3d, Curve> curveMidpt = unusedFirst.ToDictionary(crv => crv.PointAtNormalizedLength(0.5));

                foreach (var pt in curveMidpt.Keys)
                {
                    if (westZone.Contains(pt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside
                        || westZone.Contains(pt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Coincident)
                    {
                        relationshipDicFirst["west"].Add(curveMidpt[pt]);
                    }
                    else if (eastZone.Contains(pt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside
                        || eastZone.Contains(pt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Coincident)
                    {
                        relationshipDicFirst["east"].Add(curveMidpt[pt]);
                    }
                }

                /*---------------------------对于第二次的结果进行Debug----------------------------------*/

                // 定义调试的关系字典,这个字典里面装的是正确的最终分隔结果
                Dictionary<string, List<Curve>> debuggedRelationshipDict = new Dictionary<string, List<Curve>>
                {
                    { "north", new List<Curve>() },
                    { "south", new List<Curve>() },
                    { "east", new List<Curve>() },
                    { "west", new List<Curve>() }
                };

                Dictionary<string, string> negativeDirectionDict = new Dictionary<string, string>
                {
                    {"north", "south"},
                    {"south", "north"},
                    {"east", "west"},
                    {"west", "east"}
                };

                //// 定义判断偏移的值
                //double judgeOffsetValue = 0.05;

                // 所有被处理过的边都会存放在这里
                List<Curve> relationshipList = new List<Curve>();

                foreach (KeyValuePair<string, List<Curve>> item in relationshipDicFirst)
                {
                    relationshipList.AddRange(item.Value);
                }
                
                // # 如果关系列表中的边数等于总边数，则进行方向判断并debug
                if (relationshipList.Count == edgeCount)
                {
                    foreach (KeyValuePair<string, List<Curve>> directionWithEdge in relationshipDicFirst)
                    {
                        // 获取方向和边列表
                        string direction = directionWithEdge.Key;
                        List<Curve> edgeList = directionWithEdge.Value;

                        foreach (Curve edge in edgeList)
                        {
                            // 获取相反方向和偏移后的边
                            string negativeDirection = negativeDirectionDict[direction];
                            Point3d offsetPoint = JudgePointOfDispatch(edge, negativeDirection, directionDic);

                            //// 老方法：偏移后取出中点判断
                            //Curve offsetEdge = edge.Offset(offsetPoint, judgeOffsetValue, Rhino.Geometry.CurveOffsetCornerStyle.Sharp)[0];

                            //// 如果偏移后的边在陆地范围内，则方向正确；否则，方向相反
                            //if (offsetEdge.PointAtNormalizedLength(0.5).IsPointInPolygon(lands) != PointContainment.Inside)
                            //{
                            //    if (!debuggedRelationshipDict[negativeDirection].Contains(edge))
                            //    {
                            //        debuggedRelationshipDict[negativeDirection].Add(edge);
                            //    }
                            //}
                            //else
                            //{
                            //    if (!debuggedRelationshipDict[direction].Contains(edge))
                            //    {
                            //        debuggedRelationshipDict[direction].Add(edge);
                            //    }
                            //}

                            // 新方法，直接用offsetPoint判断

                            // 不在land内则证明其方向是错误的，正确方向是其现在方向的反方向
                            if (landCurve.Contains(offsetPoint, Rhino.Geometry.Plane.WorldXY, absulatTolerance) != PointContainment.Inside)
                            {
                                if (!debuggedRelationshipDict[negativeDirection].Contains(edge))
                                {
                                    debuggedRelationshipDict[negativeDirection].Add(edge);
                                }
                            }
                            // 在land内则表示，其方向是正确的
                            else
                            {
                                if (!debuggedRelationshipDict[direction].Contains(edge))
                                {
                                    debuggedRelationshipDict[direction].Add(edge);
                                }
                            }
                        }
                    }
                }
                // 如果关系列表中的边数不等于总边数，则肯定漏掉了边
                else if (relationshipList.Count != edgeCount)
                {
                    // 判断 landCurves 中有哪些边没有被处理过
                    List<Curve> unhandledCurves = landCurves.Except(relationshipList).ToList();

                    // 将未处理的边加入到正确的方向中
                    foreach (Curve unhandledCurve in unhandledCurves)
                    {
                        // 先假设其归属为东，获取相反方向和偏移后的边
                        //string negativeDirection = negativeDirectionDict["east"];

                        string negativeDirection = "west";
                        Point3d offsetPoint = JudgePointOfDispatch(unhandledCurve, negativeDirection, directionDic);

                        // 不在land内则证明东方向是错误的，正确方向是其现在方向的反方向-西
                        if (landCurve.Contains(offsetPoint, Rhino.Geometry.Plane.WorldXY, absulatTolerance) != PointContainment.Inside)
                        {
                            if (!debuggedRelationshipDict[negativeDirection].Contains(unhandledCurve))
                            {
                                debuggedRelationshipDict[negativeDirection].Add(unhandledCurve);
                            }
                        }
                        // 在land内则表示，其方向是正确的
                        else
                        {
                            if (!debuggedRelationshipDict["east"].Contains(unhandledCurve))
                            {
                                debuggedRelationshipDict["east"].Add(unhandledCurve);
                            }
                        }
                    }   
                }

                // 如果所有方向都没有成功调试，就使用原始的关系字典。这种情况出现在地块太细或者太窄了
                if (debuggedRelationshipDict.Values.All(list => list.Count == 0))
                {
                    debuggedRelationshipDict = relationshipDicFirst;
                }

                return debuggedRelationshipDict;
            }

            else
            {
                throw new ArgumentOutOfRangeException(nameof(edgeCount), "Negative edgeCount");
            }

        }

        /*-----------------------------------地块位于边缘处理方法--------------------------------------------*/

        /// <summary>
        /// 判断地块是否位于边界上，如果位于边界上，则将其分为两个relationship，一个是位于边界上的，一个是不位于边界上的。
        /// </summary>
        /// <param name="relationshipDict">分好了方向的relationship。</param>
        /// <param name="baseIsTheBoundage">基地的边缘线。</param>
        /// <param name="notOnBoundageDic">不位于边界上的relationship。</param>
        /// <param name="onBoundageDic">位于边界上的relationship。</param>
        /// <returns>无return</returns>
        public bool BoundageOrNot
            (Dictionary<string, List<Curve>> relationshipDict, 
            Curve baseIsTheBoundage, 
            out Dictionary<string, List<Curve>> onBoundageDic, 
            out Dictionary<string, List<Curve>> notOnBoundageDic, 
            out List<string> boundageDirections)
        {
            onBoundageDic = new Dictionary<string, List<Curve>>(){
            { "north", new List<Curve>() },
            { "south", new List<Curve>() },
            { "east", new List<Curve>() },
            { "west", new List<Curve>() }

            }; // Edges that are boundages

            notOnBoundageDic = new Dictionary<string, List<Curve>>(){
            { "north", new List<Curve>() },
            { "south", new List<Curve>() },
            { "east", new List<Curve>() },
            { "west", new List<Curve>() }

            }; // Edges that are not boundages

            bool isBoundageOrNot = false;

            boundageDirections = new List<string>();


            foreach (string direction in relationshipDict.Keys)
            {
                //List<string> directionHaveBeenProcessed = new List<string>();

                foreach (Curve edge in relationshipDict[direction])
                {
                        
                    //// 老方法：求取edge的中点并画出一个圆，判断圆是否与base相交
                    //Point3d midPoint = edge.PointAtNormalizedLength(0.5);
                    //Circle circle = new Circle(midPoint, 0.1);

                    // 新方法，求取edge的中点并判断是否在base上
                    Point3d midPoint = edge.PointAtNormalizedLength(0.5);
                        
                    if (baseIsTheBoundage.Contains(midPoint, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                    {
                        //if (!directionHaveBeenProcessed.Contains(direction))
                        //{
                        onBoundageDic[direction].Add(edge);
                        //    directionHaveBeenProcessed.Add(direction);
                        //}
                        // 如果存在一条边与base的边界重合，则这个land被标记为“Boundage”
                        isBoundageOrNot = true;

                        // 把哪条边在boundage记录下来
                        if (!boundageDirections.Contains(direction))
                            boundageDirections.Add(direction);
                    }

                    else
                    {
                        //if (!directionHaveBeenProcessed.Contains(direction))
                        //{
                        notOnBoundageDic[direction].Add(edge);
                        //     directionHaveBeenProcessed.Add(direction);
                        //}
                    }
                }
            }

            return isBoundageOrNot;
        }

        /*------------------------------------绿化率---------------------------------------------------*        

        /*----------------------------------------进行偏移，形成图形------------------------------------------*/

        //public List<Curve> EdgeProcessor(Curve edge, string direction, string condition = "edge", List<Curve> boundings = null)
        //{

        //    // 储存偏移后的边缘
        //    List<Curve> spliters = new List<Curve>();

        //    /*  ------------------------------------------------ EDGE --------------------------------------------------------*/

        //    if (condition == "edge") // edge，普通非boundage边缘，偏移两次，形成体块的主体
        //    {
        //        double buildingDepth = GetBuildingDepth();
        //        //// 第一次，偏移spacing的一半
        //        //Curve outsideSpliters = MyMethods.OffsetTowardsRightDirection(edge, buildingLandSpacing, land);

        //        //// 第二次，偏移房屋深度
        //        //Curve insideSpliters = MyMethods.OffsetTowardsRightDirection(edge, buildingDepth, land);

        //        //spliters.Add(outsideSpliters);
        //        //spliters.Add(insideSpliters);

        //        spliters.AddRange(Offset.offsetSideCurve(edge, buildingLandSpacing, buildingDepth, land));

        //    }
        //    /*--------------------------------------------- Boundage ------------------------------------------------------*/

        //    else if (condition == "boundage") // boundage，boundage边缘，偏移一次，第一次与边缘重合，第二次便宜建筑深度，形成体块的主体
        //    {
        //        double buildingDepth = GetBuildingDepth();
        //        //// 第一次，不偏移
        //        //Curve outsideSpliters = edge;

        //        //// 第二次，偏移房屋深度
        //        //Curve insideSpliters = MyMethods.OffsetTowardsRightDirection(edge, buildingDepth, land);

        //        //！ 可能有BUG（offset为0）
        //        spliters.AddRange(Offset.offsetSideCurve(edge, 0, buildingDepth, land));

        //    }

        //    /*----------------------------------------------END BOUNDAGE--------------------------------------------------*/

        //    else if (condition == "end_boundage" ) // boundage，boundage边缘，不偏移，与边缘重合
        //    {
        //        // 不偏移
        //        spliters.Add(edge);
        //    }

        //*----------------------------------------------END BOUNDING-------------------------------------------------*/

        //    // ? 想不起来了，暂时搁置

        //    //else if (condition == "end_bounding")
        //    //{
        //    //    // 处理边缘在边界末端且边界将被用作末端
        //    //    if (land != null)
        //    //    {
        //    //        List<Curve> boundingLinesDirections = CreateBoundingLinesDirections(land);
        //    //        Curve outsideSpliters = boundingLinesDirections[direction] as Curve;
        //    //        Curve[] insideSpliters = outsideSpliters.Offset(directionPt[0], spacing, RhinoMath.ZeroTolerance, CurveOffsetCornerStyle.Sharp);

        //    //        spliters.Add(insideSpliters[0]);
        //    //    }
        //    //}

        //    /*----------------------------------------------END SPACING-------------------------------------------------*/

        //    else if (condition == "end_spacing") // end_spacing，endb边缘，但是end不在boundage上，偏移一次
        //    {
        //        spliters.AddRange(Offset.offsetEndCurve(edge, buildingLandSpacing));
        //    }

        //    /*----------------------------------------------END SELF SPACING-------------------------------------------------*/

        //    else if (condition == "end_selfspacing")// end_self_spacing，偏移2次但是只偏移一个spacing的距离，功能忘记，先定义出来
        //    {
        //        double buildingDepth = GetBuildingDepth();

        //        spliters.AddRange(Offset.offsetSideCurve(edge, 0, buildingDepth, land));
        //    }

        //    return spliters;
        //}

        /*-------------------------------------------------附属方法-------------------------------------------------*/

        // 附属于dispatch的方法，用于生成判断点，来判断线的方向是否正确
        public Point3d JudgePointOfDispatch(Curve curve, string reversedDirection, Dictionary<string, Vector3d> directionVectorsDic)
                {
                    // 获取对应方向的向量
                    Vector3d offsetVector = directionVectorsDic[reversedDirection];

                    // 计算曲线中点偏移后的点,作为判断点
                    Point3d offsetPoint = curve.PointAtNormalizedLength(0.5) + offsetVector;

                    return offsetPoint;
                }


        // 附属于DispatchEdgesThroughDirection的评分函数
        public double SetScore(double angle, double distance, double maxLength)
        {
            double angleScore = 0;
            double distanceScore = 0;

            if (angle == 90) angleScore += 0.5;
            else if (angle >= 85 && angle < 95) angleScore += 0.4;
            else if ((angle >= 80 && angle < 85) || (angle >= 95 && angle < 100)) angleScore += 0.3;
            else if ((angle >= 70 && angle < 80) || (angle >= 100 && angle < 110)) angleScore += 0.2;
            else if ((angle >= 60 && angle < 70) || (angle >= 110 && angle < 120)) angleScore += 0.1;
            else angleScore += 0;

            if (distance >= 0 && distance < 0.25 * maxLength) distanceScore += 3;
            else if (distance >= 0.25 * maxLength && distance < 0.5 * maxLength) distanceScore += 2.75;
            else if (distance >= 0.5 * maxLength && distance < 0.75 * maxLength) distanceScore += 2.5;
            else if (distance >= 0.75 * maxLength && distance < maxLength) distanceScore += 2.25;
            else if (distance >= maxLength && distance < 1.5 * maxLength) distanceScore += 2;
            else if (distance >= 1.5 * maxLength && distance < 2 * maxLength) distanceScore += 1;
            else distanceScore += 0;

            double score = angleScore + distanceScore;

            return score;
        }



        public Point3d[] getEdgesCentPt(Curve land)
        {
            // 说明
            // 输入：land，一块地块
            // 输出：edgeCtPts，地块边界中点数组
            // 功能：根据地块，返回地块边界中点

            Curve[] landCurves = land.DuplicateSegments();
            Point3d[] edgeCtPts = new Point3d[landCurves.Length];

            for (int i = 0; i < landCurves.Length; i++)
            {
                edgeCtPts[i] = landCurves[i].PointAtNormalizedLength(0.5);
            }

            return edgeCtPts;
        }

        public Point3d getCurveDirections(Curve curve)
        {
            // 函数说明
            // 输入：curve，一条曲线，包围盒
            // 输出：offsetPt，曲线中点的偏移点
            // 功能：根据曲线的中点，判断曲线的方向，返回偏移点


            //Curve land；
            Point3d offsetPt = curve.PointAtNormalizedLength(0.5);

            Curve verticalLine = curve.DuplicateCurve();
            verticalLine.Rotate(RhinoMath.ToRadians(90), Vector3d.ZAxis, curve.PointAtNormalizedLength(0.5));

            Point3d[] endPts = { verticalLine.PointAtStart, verticalLine.PointAtEnd };

            offsetPt = endPts[0];

            if (endPts.Length == 2)
            {
                Curve offsetCurve = curve.Offset(Rhino.Geometry.Plane.WorldXY, 0.01, 0.001, CurveOffsetCornerStyle.Sharp)[0];

                offsetCurve.DivideByCount(6, true, out Point3d[] judgePts);

                int count = 0;

                foreach (Point3d pt in judgePts)
                {
                    if (curve.Contains(pt, Rhino.Geometry.Plane.WorldXY, absulatTolerance) == PointContainment.Inside)
                    {
                        count++;
                    }
                }

                if (count >= 3)
                {
                    offsetPt = endPts[1];
                }
                else
                {
                    offsetPt = endPts[0];
                }
            }

            return offsetPt;
        }



        public Polyline SortPointList(Curve land, List<Point3d> rmovDuplicatePtList)
        {
            // 函数说明
            // 输入：land，一块地块，rmovDuplicatePtList，去重后的点列表
            // 输出：sortedPoints，排序后的点列表
            // 功能：根据地块的中心点，对点列表进行排序

            Point3d centroid = land.GetBoundingBox(true).Center;

            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            List<(double, Point3d)> sortable = new List<(double, Point3d)>();

            foreach (Point3d pt in rmovDuplicatePtList)
            {
                //！！ 计算可能不达预期
                double angle = Vector3d.VectorAngle(centroid - pt, Vector3d.XAxis, Rhino.Geometry.Plane.WorldXY);

                if (angle < 0) angle += 360; // correct for negative angles
                sortable.Add((angle, pt));
            }

            List<Point3d> sortedPoints = sortable.OrderBy(x => x.Item1).Select(x => x.Item2).ToList();

            return new Polyline(sortedPoints);
        }

        public Polyline DivideDirectionZone(Curve baseCurve, string part = "north")
        {
            // 函数说明
            // 输入：baseCurve，一条曲线，part，分区
            // 输出：directionZone，方向区域
            // 功能：根据曲线，以及要输出的分区，返回方向区域

            // 计算多边形区域的边界框
            BoundingBox boundingBox = baseCurve.GetBoundingBox(true);
            // 获取边界框的角点
            Point3d[] boundingBoxCorners = boundingBox.GetCorners();

            // ! 删除重复的点，并将第一个点添加到列表的末尾，以便在后面的步骤中将多边形闭合(有可能排除不了相似的点)
            Point3d[] boundingBoxCornersNoDuplicate = boundingBoxCorners.Distinct().ToArray();
            List<Point3d> boundingBoxCornersList = new List<Point3d>(boundingBoxCornersNoDuplicate);
            // 加回第一个点
            boundingBoxCornersList.Add(boundingBoxCornersNoDuplicate[0]);

            // 创建一个多段线，该多段线由边界框中的点组成
            PolylineCurve boundingBoxPolyline = new PolylineCurve(boundingBoxCornersList);
            // 将多段线的边缘分配到四个方向（北、南、东、西）中的每一个
            Dictionary<string, List<Curve>> directionsDict = DispatchEdgesThroughDirection(boundingBoxPolyline);

            // 最长的作为参考
            //var directionsLongestOne = new Dictionary<string, List<Curve>>();
            Dictionary<string, Curve> directionsLongestOne = new Dictionary<string, Curve>();

            if (directionsDict["north"].Count == 1)
            {
                directionsLongestOne.Add("north", directionsDict["north"][0]);
                directionsLongestOne.Add("south", directionsDict["south"][0]);
                directionsLongestOne.Add("east", directionsDict["east"][0]);
                directionsLongestOne.Add("west", directionsDict["west"][0]);
            }
            else
            {
                directionsLongestOne = directionsDict.ToDictionary(pair => pair.Key, pair => pair.Value.OrderByDescending(curve => curve.GetLength()).FirstOrDefault());
            }

            // 计算每个方向的中心点和端点，并将它们存储在相应的列表中
            Point3d[] centPts = new Point3d[4];
            centPts[0] = directionsLongestOne["north"].PointAtNormalizedLength(0.5);
            centPts[1] = directionsLongestOne["south"].PointAtNormalizedLength(0.5);
            centPts[2] = directionsLongestOne["east"].PointAtNormalizedLength(0.5);
            centPts[3] = directionsLongestOne["west"].PointAtNormalizedLength(0.5);
            Point3d[] endPts = new Point3d[4];
            endPts[0] = directionsLongestOne["north"].PointAtEnd;
            endPts[1] = directionsLongestOne["south"].PointAtEnd;
            endPts[2] = directionsLongestOne["east"].PointAtEnd;
            endPts[3] = directionsLongestOne["west"].PointAtEnd;
            // 创建一个字典，其中包含四个方向的部分的点的列表
            Dictionary<string, List<Point3d>> ptDic = new Dictionary<string, List<Point3d>>();
            ptDic.Add("north_part", new List<Point3d>());
            ptDic.Add("south_part", new List<Point3d>());
            ptDic.Add("east_part", new List<Point3d>());
            ptDic.Add("west_part", new List<Point3d>());

            // !可能需要改为GH 的plane
            Rhino.Geometry.Plane plane = Rhino.Geometry.Plane.WorldXY;
            // 确定每个端点属于哪个方向的部分，并将其添加到相应的列表中
            foreach (Point3d pt in endPts)
            {
                if ((directionsLongestOne["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)

                    || (directionsLongestOne["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["east_part"].Add(pt);
                }
                if ((directionsLongestOne["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)
                    || (directionsLongestOne["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["west_part"].Add(pt);
                }
                if ((directionsLongestOne["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)
                    || (directionsLongestOne["north"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["north_part"].Add(pt);
                }
                if ((directionsLongestOne["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["west"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside)
                    || (directionsLongestOne["south"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside &&
                    directionsLongestOne["east"].Contains(pt, plane, absulatTolerance) == PointContainment.Inside))
                {
                    ptDic["south_part"].Add(pt);
                }
            }
            // 对每个部分的点进行排序，并使用这些点创建多段线
            // ! 排序功能存疑
            List<Point3d> northPartPts = ptDic["north_part"].Concat(new Point3d[] { centPts[2], centPts[3] }).ToList();
            northPartPts.Add(northPartPts[0]);

            List<Point3d> southPartPts = ptDic["south_part"].Concat(new Point3d[] { centPts[2], centPts[3] }).ToList();
            southPartPts.Add(southPartPts[0]);

            List<Point3d> eastPartPts = ptDic["east_part"].Concat(new Point3d[] { centPts[0], centPts[1] }).ToList();
            eastPartPts.Add(eastPartPts[0]);

            List<Point3d> westPartPts = ptDic["west_part"].Concat(new Point3d[] { centPts[0], centPts[1] }).ToList();
            westPartPts.Add(westPartPts[0]);

            Polyline northPartPolyline = new Polyline(northPartPts);
            Polyline southPartPolyline = new Polyline(southPartPts);
            Polyline eastPartPolyline = new Polyline(eastPartPts);
            Polyline westPartPolyline = new Polyline(westPartPts);

            // 根据part参数返回所选部分的多段线
            if (part == "north")
            {
                return northPartPolyline;
            }
            else if (part == "south")
            {
                return southPartPolyline;
            }
            else if (part == "east")
            {
                return eastPartPolyline;
            }
            else if (part == "west")
            {
                return westPartPolyline;
            }
            else
            {
                return null;
            }
        }
    }


}










