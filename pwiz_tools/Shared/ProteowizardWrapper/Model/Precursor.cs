
namespace pwiz.ProteowizardWrapper.Model
{
    public class Precursor
    {
        private bool _negativePolarity;
        private CLI.msdata.Precursor _pwizPrecursor;
        public Precursor(CLI.msdata.Precursor pwizPrecursor, bool negativePolarity)
        {
            _pwizPrecursor = pwizPrecursor;
            _negativePolarity = negativePolarity;
        }

        public double? SelectedIonMz
        {
            get
            {
                return MsDataFileImpl.GetPrecursorMz(_pwizPrecursor, _negativePolarity)?.RawValue;
            }
        }
    }
}
