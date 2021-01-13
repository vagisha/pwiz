using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.Skyline.Model.Spectra
{
    public class Spectrum
    {
        private ProteowizardWrapper.Model.Spectrum _wrappedSpectrum;
        public Spectrum(int index, ProteowizardWrapper.Model.Spectrum wrappedSpectrum)
        {
            Index = index;
            _wrappedSpectrum = wrappedSpectrum;
        }

        public int Index { get; }

        public string Id
        {
            get { return _wrappedSpectrum.Id; }
        }
        public int? MsLevel
        {
            get { return _wrappedSpectrum.MsLevel; }
        }

        public double? RetentionTime
        {
            get { return _wrappedSpectrum.RetentionTime; }
        }

        public string ScanDescription
        {
            get
            {
                return _wrappedSpectrum.ScanDescription;
            }
        }
    }
}
