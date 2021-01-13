using pwiz.Common.DataBinding;
using pwiz.Skyline.Model.Databinding;
using pwiz.Skyline.Model.Results;

namespace pwiz.Skyline.Model.Spectra
{
    public class SpectraViewContext : SkylineViewContext
    {
        public SpectraViewContext(SkylineDataSchema dataSchema, MsDataFileUri msDataFileUri)
            : base(dataSchema, new[] {MakeRowSourceinfo(dataSchema, msDataFileUri)})
        {

        }



        public static RowSourceInfo MakeRowSourceinfo(DataSchema dataSchema, MsDataFileUri dataFilePath)
        {
            var column = ColumnDescriptor.RootColumn(dataSchema, typeof(Spectrum));
            var viewSpec = new ViewSpec().SetColumns(new[]
            {
                new ColumnSpec(PropertyPath.Root.Property(nameof(Spectrum.Id))),
                new ColumnSpec(PropertyPath.Root.Property(nameof(Spectrum.RetentionTime))), 
                new ColumnSpec(PropertyPath.Root.Property(nameof(Spectrum.MsLevel))), 
                new ColumnSpec(PropertyPath.Root.Property(nameof(Spectrum.ScanDescription))), 
            }).SetName("Default");
            var viewInfo = new ViewInfo(column, viewSpec).ChangeViewGroup(ViewGroup.BUILT_IN);
            return new RowSourceInfo(new SpectrumRowSource(dataFilePath), viewInfo);
        }
    }
}
