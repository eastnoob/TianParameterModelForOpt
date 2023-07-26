using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TianParameterModelForOpt._4_repair
{
    internal static class Find
    {
        public static Brep FindTheLargestBrep(Brep[] multipleBreps)
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
    }
}
