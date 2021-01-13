using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pwiz.ProteowizardWrapper.Model
{
    public class IsolationWindow
    {
        private CLI.msdata.IsolationWindow _pwizIsolationWindow;
        public IsolationWindow(CLI.msdata.IsolationWindow pwizIsolationWindow)
        {
            _pwizIsolationWindow = pwizIsolationWindow;
        }

        public int? MsLevel
        {
            get
            {
                return (int?) _pwizIsolationWindow.userParam("ms level")?.value;
            }
        }
    }
}
