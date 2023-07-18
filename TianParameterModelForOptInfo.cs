using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace TianParameterModelForOpt
{
    public class TianParameterModelForOptInfo : GH_AssemblyInfo
    {
        public override string Name => "TianParameterModelForOpt";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "123";

        public override Guid Id => new Guid("0e2f757a-1378-469f-b307-f5d0a2d73d62");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}