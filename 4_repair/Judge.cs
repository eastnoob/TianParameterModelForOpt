using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt._4_repair
{
    internal class Judge
    {
        Dictionary<Curve, Curve> allEdgeWithOffsetBaseCurve;
        Land thisLand;
        Brep[] judgeBrep;

        public Judge(Dictionary<Curve, Curve> allEdgeWithOffsetBaseCurve, Land thisLand)
        {
            this.allEdgeWithOffsetBaseCurve = allEdgeWithOffsetBaseCurve;
            this.thisLand = thisLand;
            this.judgeBrep = CreateJudgeBrep();

        }


        public Brep[] RepairFloorBlock (Brep[] unIntersectedBUildingBrep)
        {
            // repair
            Brep[] repairedBrep = Brep.CreateBooleanIntersection(this.judgeBrep, unIntersectedBUildingBrep, 0.001);

            // 尝试处理None的问题

            //Console.WriteLine("judgedSingleBlockBrep.Length: ");

            if (repairedBrep == null/* || judgedSingleBlockBrep.Length == 0 */)
            {
                Console.WriteLine("Need repair ! !");
                Brep maxvolumebrep = null;

                if (unIntersectedBUildingBrep.Length > 1)
                {
                    // 取出最大的那一个
                    Brep[] unionSecond = Brep.CreateBooleanUnion(unIntersectedBUildingBrep, 0.001);

                    if (unionSecond.Length > 1)
                    {
                        //Brep maxvolumebrep = null;
                        double maxvolume = 0.0;

                        foreach (Brep brep in unIntersectedBUildingBrep)
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
                    Brep[] judged = maxvolumebrep.Split(judgeBrep, 0.01);

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
        }   

        public Brep[] CreateJudgeBrep()
        {
            Brep judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, thisLand, 100);

            if (judgeBrep == null)
            {
                Curve judgeBase = thisLand.landCurve;
                judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, thisLand, 100);
            }

            Brep[] judgeBrepArray = new Brep[] { judgeBrep };
            return judgeBrepArray;
        }


        public Dictionary<Curve, string> OffsetBehavioursOfLandcurves(Dictionary<string, string> offsetBehavioursOfDirections)
        {
            Dictionary<Curve, string> offsetBehavioursOfLandcurves = new Dictionary<Curve, string>();

            foreach (string direction in offsetBehavioursOfDirections.Keys)
            {
                foreach (Curve curve in allEdgeWithOffsetBaseCurve.Keys)
                {
                    if (thisLand.dispatchedEdges[direction].Contains(curve))
                    {
                        offsetBehavioursOfLandcurves[curve] = offsetBehavioursOfDirections[direction];
                    }
                }
            }
            return offsetBehavioursOfLandcurves;
        }

        public static Brep[] CreateJudgeBrepOfTheLand(
                                                Dictionary<Curve, Curve> allEdgeWithOffsetBaseCurve, 
                                                Land land)
        {
            

            // 建立JudgeBrep

            Brep judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, land, 100);

            if (judgeBrep == null)
            {
                Curve judgeBase = land.landCurve;
                judgeBrep = GenerateTheBuildingBrepDirectly.CreateJudgeBrepBase(allEdgeWithOffsetBaseCurve, land, 100);
            }

            Brep[] judgeBrepArray = new Brep[] { judgeBrep };

            return judgeBrepArray;
        }
    }
}
