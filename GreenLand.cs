using Eto.Forms;
using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt
{
    public class GreenLand
    {

        //属性

        // 需求数值
        public double roadWidth;

        // 需求图形
        Land land;

        // 计算数值
        //public double greedLandArea;
        //public double greeningRate;

        public double greenLandArea;

        // 图形
        public Brep greenLand;

        // 计算图形
        public Brep greenLandBrep;


        //构造器
        public GreenLand(Land land, double roadWidth)
        {
            this.roadWidth = roadWidth;
            this.land = land;

            this.greenLandBrep = GreenLandGenerate(out greenLandArea);
        }

        //方法
        //绿地图形生成
        public Brep GreenLandGenerate(out double greenLandArea)
        {
            // 获得land图形
            Curve landCurve = land.landCurve;
            if (landCurve.IsPlanar() == false)
            {
                // 将landCurve转换为XY平面上的平面投影曲线
                landCurve = Curve.ProjectToPlane(landCurve, Plane.WorldXY);
            }

            // 获得绿地的边界曲线
            Curve[] greenLand = landCurve.Offset(AreaMassProperties.Compute(landCurve).Centroid,
                Plane.WorldXY.Normal,
                roadWidth / 2,
                RhinoDoc.ActiveDoc.ModelAbsoluteTolerance,
                CurveOffsetCornerStyle.Sharp);

            Curve greenLandCurve = greenLand[0];

            // 如果greenland长度不为1，取面积最大的
            if (greenLand.Length > 1)
            {
                double[] greenLandsarea = new double[greenLand.Length];

                for (int i = 0; i < greenLand.Length; i++)
                {
                    greenLandsarea[i] = AreaMassProperties.Compute(greenLand[i]).Area;
                }
                greenLandCurve = greenLand[Array.IndexOf(greenLandsarea, greenLandsarea.Max())];
            }

            // 以landCurve为边界生成面
            Brep greenLandSurface = Brep.CreatePlanarBreps(greenLandCurve, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance)[0];

            // 计算绿地的面积
            greenLandArea = AreaMassProperties.Compute(greenLandCurve).Area;

            return greenLandSurface;
        }


    }

}
