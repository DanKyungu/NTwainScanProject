using NTwain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanProject.ViewModel
{
    public class DataSourceVM : BaseViewModel
    {
        public DataSource DS { get; set; }

        public string Name { get { return DS.Name; } }
        public string Version { get { return DS.Version.Info; } }
        public string Protocol { get { return DS.ProtocolVersion.ToString(); } }
        public bool IsOpen => DS.IsOpen;

        public DataSourceVM()
        {
            
        }

        public void Open()
        {
            var res = DS.Open();
        }
    }
}
