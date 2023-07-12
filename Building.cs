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
using static Grasshopper.DataTree<T>;

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
        List<Curve> floorSketch;

        // 是否是boundage
        bool isBoundageOrNot;

        // 构造器，传入Land类的land，楼层高度，楼层数量，和楼层平面图
        public Building(Land land, double groundFloorHeight, double standardFloorHeight, List<int> floorNums, List<Curve> floorSketchs)
        {
            this.land = land;
            this.groundFloorHeight = groundFloorHeight;
            this.standardFloorHeight = standardFloorHeight;
            this.floorNum = floorNum;
            this.floorSketch = floorSketch;
            this.isBoundageOrNot = false;
        }

        /*---------------------------------------挤出向量---------------------------------------*/

        // boundage的建筑物底层挤出向量
        public Vector3d CreateBoundageBuildingGroundFloorVector(Curve sketch)
        {
            // 先检测curve是不是平面曲线
            if (sketch.IsPlanar() == false)
                Curve.ProjectToPlane(sketch, Plane.WorldXY);

            Vector3d groundFloorVector = new Vector3d(0, 0, groundFloorHeight);
            Transform transform = Transform.Translation(sketch.PointAtStart.X, sketch.PointAtStart.Y, sketch.PointAtStart.Z);
            groundFloorVector.Transform(transform);
            return groundFloorVector;
        }

        // 建筑物标准层挤出向量
        public Vector3d CreateStandardFloorVector(Curve sketch)
        {
            if (sketch.IsPlanar() == false)
                Curve.ProjectToPlane(sketch, Plane.WorldXY);

            Vector3d standardFloorVector = new Vector3d(0, 0, standardFloorHeight);
            Transform transform = Transform.Translation(sketch.PointAtStart.X, sketch.PointAtStart.Y, sketch.PointAtStart.Z);
            standardFloorVector.Transform(transform);
            return standardFloorVector;
        }

        /*---------------------------------向上复制底面---------------------------------*/
        /// <summary>
        /// 附属方法：根据已有的图纸和复制向量，使用递归复制多个图纸.如果是boundage，则底面只需要向上复制一次，再作为基础底面向上复制即可</param>
        /// 重载1：普通的楼层
        /// </summary>
        /// <param name="sketch">待复制的原始图纸</param>
        /// <param name="floorNum">要复制的层数</param>
        /// <param name="vectorOfDuplicate">首次的复制向量</param>
        /// <param name="duplicated">是否是复制</param>
        /// <returns>复制的各层图纸</returns>
        /// 
        public Dictionary<Curve, Vector3d> DuplicateFloorSketchAndVector(Curve sketchOfBuildingFloor, int floorNum)
        {
            //Curve[] allFloorSketchs = new Curve[floorNum];

            Dictionary<Curve, Vector3d> sketchWithDirection = new Dictionary<Curve, Vector3d>();

            // 这个用于存储
            if (sketchWithDirection == null)
                sketchWithDirection = new Dictionary<Curve, Vector3d>();

            // 递归出口
            if (floorNum == 0)
                return sketchWithDirection;
            else
            {

                // 先复制一份
                Curve sketchAbove = sketchOfBuildingFloor.DuplicateCurve();
                //再向上移动形成新的楼层
                Vector3d vectorOfTheSketch = CreateStandardFloorVector(sketchOfBuildingFloor);
                Transform translation = Transform.Translation(vectorOfTheSketch);
                sketchAbove.Transform(translation);
                // 加入字典里面
                sketchWithDirection[sketchAbove] = vectorOfTheSketch;
                //递归
                return DuplicateFloorSketchAndVector(sketchOfBuildingFloor: sketchAbove, floorNum: floorNum - 1);
            }

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
            //递归
            return sketchWithDirection;
        }


        // ******** 生成一整个楼层的底面
        /// <summary>
        /// 生成一整个楼的所有层的底面
        /// </summary>
        /// <param name="sketchOfBuildingFloor"></param>
        /// <param name="floorNum"></param>
        /// <param name="isBoundage"></param>
        /// <returns></returns>
        public Dictionary<Curve, Vector3d> DrawAllFloorsOfABuilding(Curve sketchOfBuildingFloor, int floorNum, bool isBoundage = false)
        {
            // 容器
            Dictionary<Curve, Vector3d> allFloorsOfABuilding = new Dictionary<Curve, Vector3d>();

            if(isBoundage == false)
            {
                allFloorsOfABuilding = DuplicateFloorSketchAndVector(sketchOfBuildingFloor, floorNum);
                return allFloorsOfABuilding;
            }
            else
            {
                Dictionary<Curve, Vector3d> groundFloorMaterials = DuplicateFloorSketchAndVector(sketchOfBuildingFloor);
                Dictionary<Curve, Vector3d> otherFloorMaterials = DuplicateFloorSketchAndVector(groundFloorMaterials.Keys.First(), floorNum - 1);
                otherFloorMaterials.Add(groundFloorMaterials.Keys.First(), groundFloorMaterials.Values.First());
                return allFloorsOfABuilding;
            }
        }

        /*---------------------------------------建筑物形体---------------------------------------*/

        //附属方法：单层建筑物形体
        public Brep DrawSingleFloorBlock(Curve sketchOfFloor, Vector3d extrudeVector)
        {
            // 创建Extrusion
            Extrusion extrusion = Extrusion.Create(sketchOfFloor, extrudeVector.Length,true);

            // 对齐方向
            extrusion.Transform(Transform.Translation(extrudeVector));

            // 将Extrusion对象转换成Brep对象
            Brep brep = extrusion.ToBrep(true);

            return brep;
        }

        // ********** 多层组合为一个建筑
        /// <summary>
        /// 多层组合为一个建筑
        /// </summary>
        /// <param name="sketchOfBuildingFloors"></param>
        /// <param name="floorNum"></param>
        /// <param name="isBoundage"></param>
        /// <returns></returns>
        public List<Brep> DrawBuildingFloorBlocks(Curve sketchOfBuildingFloors, int floorNum, bool isBoundage)
        {
            // 用来装一个建筑的所有层的实体
            List<Brep> building = new List<Brep>();
            //Brep[] building = new Brep[floorNum];

            Dictionary<Curve, Vector3d> materials = DrawAllFloorsOfABuilding(sketchOfBuildingFloors, floorNum, isBoundage);

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

        public double GetEstimatedRoomAccount(List<Curve> floors)
        {
            double buildingDepth = land.GetBuildingDepth();

            // 总的建筑面积
            double totalAreaOfBuildingFloors = GetBuildingArea(floors);

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
