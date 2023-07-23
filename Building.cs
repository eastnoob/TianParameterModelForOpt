using Grasshopper.Kernel.Types.Transforms;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
//using static Grasshopper.DataTree<T>;

namespace TianParameterModelForOpt
{
    /// <summary>
    /// 这个类是用来描述建筑的，目的是生成建筑的形体<\br>
    /// 这个函数仅仅生成单体的建筑
    /// </summary>
    internal class Building
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
        public Curve floorSketch;

        // 所有楼层的平面图
        public Dictionary<Curve, Vector3d> allfloorPlanesAndItsVectors;

        //// 是否是boundage
        //bool isBoundageOrNot;

        // *********** 楼层实体图形
        public List<Brep> buildingBrep;

        // debug用变量
        public List<Curve> singleBlocks;

        // 构造器，传入Land类的land，楼层高度，楼层数量，和楼层平面图
        public Building(Land land, double groundFloorHeight, double standardFloorHeight, int floorNum/*, Curve floorSketchs*/)
        {
            this.land = land;
            this.groundFloorHeight = groundFloorHeight;
            this.standardFloorHeight = standardFloorHeight;
            this.floorNum = floorNum;
            this.floorSketch = Draw.DrawSketchOfABuilding(land);



            //this.isBoundageOrNot = false;

            // 生成所有的楼层平面图
            this.allfloorPlanesAndItsVectors = DrawAllFloorsOfABuilding(floorSketch, floorNum/*, isBoundageOrNot*/);

            // ********** 楼层实体图形
            this.buildingBrep = DrawBuildingFloorBlocks();

            // =================== debug用变量
            this.singleBlocks = Draw.ReturnSingleBlocks(land);

        }

        /*---------------------------------------挤出向量---------------------------------------*/

        // boundage的建筑物底层挤出向量
        public Vector3d CreateBoundageBuildingGroundFloorVector(Curve sketch)
        {
            if (sketch.IsPlanar() == false)
                Curve.ProjectToPlane(sketch, Plane.WorldXY);

            var groundFloorHeight = this.groundFloorHeight;

            // 首层不上移动
            Point3d startingPoint = sketch.PointAtStart;

            Vector3d groundFloorVector = new Vector3d(0, 0, groundFloorHeight);

            // Translate the vector to start at the starting point of the sketch
            groundFloorVector.Transform(Transform.Translation(startingPoint - Point3d.Origin));

            return groundFloorVector;

            //// 先检测curve是不是平面曲线
            //if (sketch.IsPlanar() == false)
            //    Curve.ProjectToPlane(sketch, Plane.WorldXY);

            //Vector3d groundFloorVector = new Vector3d(0, 0, groundFloorHeight);
            //Transform transform = Transform.Translation(sketch.PointAtStart.X, sketch.PointAtStart.Y, sketch.PointAtStart.Z);
            //groundFloorVector.Transform(transform);
            //return groundFloorVector;
        }

        // 建筑物标准层挤出向量
        public Vector3d CreateStandardFloorVector(Curve sketch)
        {
            if (sketch.IsPlanar() == false)
                Curve.ProjectToPlane(sketch, Plane.WorldXY);

            // 新方法
            // Get the starting point of the passed-in Curve sketch
            Point3d startingPoint = sketch.PointAtStart;

            // Create a new vector with length equal to the standard floor height and in the z-axis direction
            Vector3d standardFloorVector = new Vector3d(0, 0, standardFloorHeight);

            // Translate the vector to start at the starting point of the sketch
            standardFloorVector.Transform(Transform.Translation(startingPoint - Point3d.Origin));

            // Return the final vector
            return standardFloorVector;


            // 老方法备份
            //Vector3d standardFloorVector = new Vector3d(0, 0, standardFloorHeight);
            //Transform transform = Transform.Translation(sketch.PointAtStart.X, sketch.PointAtStart.Y, sketch.PointAtStart.Z);
            //standardFloorVector.Transform(ref transform);
            //return standardFloorVector;
        }

        /*---------------------------------向上复制底面---------------------------------*/
        /// <summary>
        /// 附属方法，复制并移动 Curve，用于实现在前一次的基础上复制
        /// </summary>
        /// <param name="sourceCurve">待复制的曲线</param>
        /// <param name="copyDistance">移动的距离</param>
        /// <returns>移动并复制后的曲线</returns>
        private Curve CopyAndMoveCurve(Curve sourceCurve, double copyDistance, out Vector3d moveVector)
        {
            // 复制曲线
            Curve duplicateCurve = sourceCurve.DuplicateCurve();

            // 移动曲线
            moveVector = CreateStandardFloorVector(sourceCurve) * copyDistance;
            duplicateCurve.Translate(moveVector);

            return duplicateCurve;
        }

        /// <summary>
        /// 附属方法：使用递归的方式生成建筑物的每一层的曲面，并将每一楼层的曲面及其相应的上移向量存储在一个字典中。
        /// </summary>
        /// <param name="sketchOfBuildingFloor">建筑物的一楼轮廓曲面</param>
        /// <param name="floorNum">建筑物楼层数</param>
        /// <param name="emptySketchWithDirection">空字典，用于存储生成的每一楼层曲面及其相应的上移向量</param>
        /// <returns>一个字典，其中每个键值对都表示建筑物的一个楼层，键对应的值是该楼层上移的矢量</returns>
        public Dictionary<Curve, Vector3d> RecursiveReplicationCurve(Curve sketchOfBuildingFloor, int floorNum, Dictionary<Curve, Vector3d> emptySketchWithDirection)
        {
            if (floorNum == 0)
                return emptySketchWithDirection;
            else
            {
                // 先复制一份
                Curve sketchAbove = sketchOfBuildingFloor.DuplicateCurve();
                //再向上移动形成新的楼层
                Vector3d vectorOfTheSketch = CreateStandardFloorVector(sketchOfBuildingFloor);
                Transform translation = Transform.Translation(vectorOfTheSketch);
                sketchAbove.Transform(translation);
                // 加入字典里面
                emptySketchWithDirection[sketchAbove] = vectorOfTheSketch;
                //递归
                return RecursiveReplicationCurve(sketchOfBuildingFloor: sketchAbove, 
                                                floorNum: floorNum - 1, 
                                                emptySketchWithDirection);
            }
        }



        /// <summary>
        /// 附属方法：根据已有的图纸和复制向量，使用递归复制多个图纸.如果是boundage，则底面只需要向上复制一次，再作为基础底面向上复制即可</param>
        /// 重载1：普通的楼层
        /// 
        /// </summary>
        /// 
        /// <param name="sketch">待复制的原始图纸</param>
        /// <param name="floorNum">要复制的层数</param>
        /// <param name="vectorOfDuplicate">首次的复制向量</param>
        /// <param name="duplicated">是否是复制</param>
        /// <returns>复制的各层图纸</returns>
        /// 
        public Dictionary<Curve, Vector3d> DuplicateFloorSketchAndVector(Curve sketchOfStandardFloor, int floorNum)
        {
            // -----------------------新方法-----------------------
            // 这个空的字典会在递归的过程中被填满
            Dictionary<Curve, Vector3d> emptySketchWithDirection = new Dictionary<Curve, Vector3d>();

            // 再加入首层
            // 需要生成首层及其对应的向量
            Vector3d vectorOfTheSketch = CreateStandardFloorVector(sketchOfStandardFloor);
            emptySketchWithDirection[sketchOfStandardFloor] = vectorOfTheSketch;


            // 递归生成其他层
            var sketchWithDirection = RecursiveReplicationCurve(sketchOfStandardFloor, floorNum - 1, emptySketchWithDirection);


            if (sketchWithDirection.Count == emptySketchWithDirection.Count) 
            {
                return sketchWithDirection;
            }
                
            else
            {
                if (sketchWithDirection.Count > emptySketchWithDirection.Count)
                    return sketchWithDirection;
                else
                    return emptySketchWithDirection;
            }

            return sketchWithDirection;


            //// -----------------------老方法-----------------------
            ////Curve[] allFloorSketchs = new Curve[floorNum];
            ////floorNum = this.floorNum;
            //Dictionary<Curve, Vector3d> sketchWithDirection = new Dictionary<Curve, Vector3d>();

            //// 这个用于存储
            //if (sketchWithDirection == null)
            //    sketchWithDirection = new Dictionary<Curve, Vector3d>();

            //// 递归出口
            //if (floorNum == 0)
            //    return sketchWithDirection;
            //else
            //{

            //    // 先复制一份
            //    Curve sketchAbove = sketchOfBuildingFloor.DuplicateCurve();
            //    //再向上移动形成新的楼层
            //    Vector3d vectorOfTheSketch = CreateStandardFloorVector(sketchOfBuildingFloor);
            //    Transform translation = Transform.Translation(vectorOfTheSketch);
            //    sketchAbove.Transform(translation);
            //    // 加入字典里面
            //    sketchWithDirection[sketchAbove] = vectorOfTheSketch;
            //    //递归
            //    return DuplicateFloorSketchAndVector(sketchOfBuildingFloor: sketchAbove, floorNum: floorNum - 1);
            //}

        }

        /// <summary>
        /// 重载2：boundage的楼层，只需要向上复制一次，生成顶楼即可
        /// </summary>
        /// <param name="sketchOfGroundFloor"></param>
        /// <returns></returns>
        public Dictionary<Curve, Vector3d> DuplicateFloorSketchAndVector(Curve sketchOfGroundFloor)
        {
            Dictionary<Curve, Vector3d> sketchWithDirection = new Dictionary<Curve, Vector3d>();
            // 先复制一份
            Curve sketchAbove = sketchOfGroundFloor.DuplicateCurve();
            //再向上移动形成新的楼层
            Vector3d vectorOfTheSketch = CreateBoundageBuildingGroundFloorVector(sketchOfGroundFloor);
            Transform translation = Transform.Translation(vectorOfTheSketch);
            sketchAbove.Transform(translation);
            // 加入字典里面
            sketchWithDirection[sketchAbove] = vectorOfTheSketch;

            //// 将首层本身也加入字典
            //Vector3d vectorOfGroundFloor = CreateBoundageBuildingGroundFloorVector(sketchOfGroundFloor);
            //sketchWithDirection[sketchOfGroundFloor] = vectorOfGroundFloor;
            //递归
            return sketchWithDirection;
        }

        // ----------------------------------------------******** 生成一整个楼层的底面----------------------------------------------------------------
        /// <summary>
        /// 生成一整个楼的所有层的底面 !!!!!!!!!!!!!!!!!!!!!!!
        /// </summary>
        /// <param name="sketchOfBuildingFloor"></param>
        /// <param name="floorNum"></param>
        /// <param name="isBoundage"></param>
        /// <returns></returns>
        public Dictionary<Curve, Vector3d> DrawAllFloorsOfABuilding(Curve sketchOfBuildingFloor, int floorNum/*, bool isBoundage = false*/)
        {
            bool isBoundage = this.land.boundageOrNot;
            // 容器
            Dictionary<Curve, Vector3d> allFloorsOfABuilding = new Dictionary<Curve, Vector3d>();

            if(isBoundage == false)
            {
                allFloorsOfABuilding = DuplicateFloorSketchAndVector(sketchOfBuildingFloor, floorNum);

                if(allFloorsOfABuilding.Count == floorNum)
                {
                    return allFloorsOfABuilding;
                }
                else
                {
                    // 说明有问题，要把首层本身也加入进去
                    allFloorsOfABuilding[sketchOfBuildingFloor] = CreateStandardFloorVector(sketchOfBuildingFloor);
                    return allFloorsOfABuilding;
                }

            }
            else
            {
                // 生成首层及其对应的向量
                Vector3d vectorOfTheSketch = CreateBoundageBuildingGroundFloorVector(sketchOfBuildingFloor);
                allFloorsOfABuilding[sketchOfBuildingFloor] = vectorOfTheSketch;

                // 先生成首层
                Dictionary<Curve, Vector3d> groundFloorMaterials = DuplicateFloorSketchAndVector(sketchOfBuildingFloor);
                // 再生成其他层
                Dictionary<Curve, Vector3d> otherFloorMaterials = DuplicateFloorSketchAndVector(groundFloorMaterials.Keys.First(), floorNum - 1);


                foreach (KeyValuePair<Curve, Vector3d> item in groundFloorMaterials)
                {
                    allFloorsOfABuilding.Add(item.Key, item.Value);
                }
                foreach (KeyValuePair<Curve, Vector3d> item in otherFloorMaterials)
                {
                    allFloorsOfABuilding.Add(item.Key, item.Value);
                }

                //otherFloorMaterials.Add(groundFloorMaterials.Keys.First(), groundFloorMaterials.Values.First());

                return allFloorsOfABuilding;
            }
        }

        /*---------------------------------------建筑物形体---------------------------------------*/

        //附属方法：单层建筑物形体
        public Brep DrawSingleFloorBlock(Curve sketchOfFloor, Vector3d extrudeVector)
        {
            // 创建Extrusion
            Extrusion extrusion = Extrusion.Create(sketchOfFloor, extrudeVector.Length,true);

            //// 对齐方向
            //extrusion.Transform(Transform.Translation(extrudeVector));

            // 将Extrusion对象转换成Brep对象
            Brep brep = extrusion.ToBrep(true);

            return brep;
        }

        // ********** 多层组合为一个建筑
        /// <summary>
        /// 多层组合为一个建筑
        /// </summary>
        /// <param name="sketchOfTheLand"></param>
        /// <param name="floorNum"></param>
        /// <param name="isBoundage"></param>
        /// <returns></returns>
        public List<Brep> DrawBuildingFloorBlocks(/*Curve sketchOfTheLand, int floorNum, bool isBoundage*/)
        {
            Curve sketchOfTheLand = this.floorSketch;
            int floorNum = this.floorNum;
            bool isBoundage = land.isABoundageLand;
            // 用来装一个建筑的所有层的实体
            List<Brep> building = new List<Brep>();
            //Brep[] building = new Brep[floorNum];

            // 老方法备用
            //Dictionary<Curve, Vector3d> materials = DrawAllFloorsOfABuilding(sketchOfTheLand, floorNum, isBoundage);

            //// 新方法
            Dictionary<Curve, Vector3d> materials = this.allfloorPlanesAndItsVectors;

            //int index = 0;
            foreach (KeyValuePair<Curve, Vector3d> pair in materials)
            {
                Brep singleBuildingFloorBerp = DrawSingleFloorBlock(pair.Key, pair.Value);
                building.Add(singleBuildingFloorBerp);
                //building[index] = singleBuildingFloorBerp;
                //index++;
            }
            return building;
        }

        /*-------------------------------指标计算-----------------------------------------*/
        public double GetBuildingArea(List<Curve> floors)
        {
            double totalAreaOfBuildingFloors = 0;
            foreach(Curve floor in floors)
            {
                // 判断floor是不是封闭曲线
                if(floor.IsClosed == false)
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

            List<Curve> floors = DuplicateFloorSketchAndVector(floorSketch).Keys.ToList();

            double buildingDepth = land.GetBuildingDepth();

            // 总的建筑面积
            double totalAreaOfBuildingFloors = GetBuildingArea(floors);

            if (floors.Count == 0)
            {
                return 0;
            }
            else if(floors.Count == 1 && floorNum >0)
            {
                totalAreaOfBuildingFloors *= floorNum;
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
