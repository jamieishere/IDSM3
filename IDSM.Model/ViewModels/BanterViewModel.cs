using System.Collections.Generic;
using System.Web.Mvc;

namespace IDSM.Model.ViewModels
{
    public class BanterViewModel
    {
        public IEnumerable<Banter> Banters { get; set; }
    }
}