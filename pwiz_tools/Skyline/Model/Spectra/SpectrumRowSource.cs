using System.Collections;
using System.Collections.Generic;
using System.Threading;
using pwiz.Common.Collections;
using pwiz.Common.DataBinding;
using pwiz.ProteowizardWrapper;
using pwiz.ProteowizardWrapper.Model;
using pwiz.Skyline.Model.Results;
using pwiz.Skyline.Util.Extensions;

namespace pwiz.Skyline.Model.Spectra
{
    public class SpectrumRowSource : AbstractRowSource
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ImmutableList<Spectrum> _spectra = ImmutableList<Spectrum>.EMPTY;
        private MsDataFileImpl _msDataFileImpl;
        public SpectrumRowSource(MsDataFileUri dataFilePath)
        {
            DataFilePath = dataFilePath;
        }

        public MsDataFileUri DataFilePath { get; }

        protected override void AfterLastListenerRemoved()
        {
            lock (this)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = null;
                _msDataFileImpl?.Dispose();
                _msDataFileImpl = null;
            }
        }

        protected override void BeforeFirstListenerAdded()
        {
            lock (this)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                ActionUtil.RunAsync(() => 
                    FetchSpectra(_cancellationTokenSource.Token));
            }
        }

        public override IEnumerable GetItems()
        {
            return _spectra;
        }

        private void FetchSpectra(CancellationToken cancellationToken)
        {
            var msDataFileImpl = OpenMsDataFile(cancellationToken);
            UpdateItems(cancellationToken, ImmutableList.ValueOf(MakeSpectrumList(cancellationToken,
                msDataFileImpl.GetSpectrumList(DetailLevel.InstantMetadata))));
            UpdateItems(cancellationToken, ImmutableList.ValueOf(MakeSpectrumList(cancellationToken, msDataFileImpl.GetSpectrumList(DetailLevel.FullMetadata))));
        }

        private void UpdateItems(CancellationToken cancellationToken, IList<Spectrum> spectra)
        {
            lock (this)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _spectra = ImmutableList.ValueOf(spectra);
            }
            FireListChanged();
        }

        private MsDataFileImpl OpenMsDataFile(CancellationToken cancellationToken)
        {
            MsDataFileImpl msDataFileImpl = null;
            try
            {
                msDataFileImpl = DataFilePath.OpenMsDataFile(true, false, false, false, true);
                MsDataFileImpl returnValue;
                lock (this)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    returnValue = _msDataFileImpl = msDataFileImpl;
                    msDataFileImpl = null;
                }

                return returnValue;
            }
            finally
            {
                msDataFileImpl?.Dispose();
            }
        }

        private IEnumerable<Spectrum> MakeSpectrumList(CancellationToken cancellationToken,
            IList<ProteowizardWrapper.Model.Spectrum> wrappedSpectra)
        {
            int count = wrappedSpectra.Count;
            for (int i = 0; i < count; i++)
            {
                Spectrum spectrum = null;
                lock (this)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    spectrum = new Spectrum(i, wrappedSpectra[i]);
                }

                yield return spectrum;
            }
        }
    }
}
