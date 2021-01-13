using System.Collections.Generic;
using pwiz.Common.Collections;

namespace pwiz.ProteowizardWrapper.Model
{
    public class Spectrum
    {
        private CLI.msdata.Spectrum _pwizSpectrum;

        public Spectrum(CLI.msdata.Spectrum pwizSpectrum)
        {
            _pwizSpectrum = pwizSpectrum;
        }

        public string Id
        {
            get
            {
                return _pwizSpectrum.id;
            }
        }

        public int Index { get; }

        public int? MsLevel
        {
            get
            {
                return MsDataFileImpl.GetMsLevel(_pwizSpectrum);
            }
        }

        public double? RetentionTime
        {
            get
            {
                return MsDataFileImpl.GetStartTime(_pwizSpectrum);
            }
        }

        public IList<double> Mzs
        {
            get { return _pwizSpectrum.getMZArray()?.data; }
        }

        public IList<double> Intensities
        {
            get
            {
                return _pwizSpectrum.getIntensityArray()?.data;
            }
        }

        public IList<Precursor> Precursors
        {
            get
            {
                var precursorList = _pwizSpectrum.precursors;
                if (precursorList == null)
                {
                    return null;
                }

                return ReadOnlyList.Create(precursorList.Count, i => new Precursor(precursorList[i], MsDataFileImpl.NegativePolarity(_pwizSpectrum)));
            }
        }

        public string ScanDescription
        {
            get
            {
                return MsDataFileImpl.GetScanDescription(_pwizSpectrum);
            }
        }
    }
}
