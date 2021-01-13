using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.ProteowizardWrapper.Model
{
    public class DetailLevel
    {
        public static readonly DetailLevel InstantMetadata = new DetailLevel(CLI.msdata.DetailLevel.InstantMetadata);
        public static readonly DetailLevel FastMetadata = new DetailLevel(CLI.msdata.DetailLevel.FastMetadata);
        public static readonly DetailLevel FullMetadata = new DetailLevel(CLI.msdata.DetailLevel.FullMetadata);
        public static readonly DetailLevel FullData = new DetailLevel(CLI.msdata.DetailLevel.FullMetadata);

        private DetailLevel(CLI.msdata.DetailLevel pwizDetailLevel)
        {
            PwizDetailLevel = pwizDetailLevel;
        }

        public CLI.msdata.DetailLevel PwizDetailLevel { get; }
    }
}
