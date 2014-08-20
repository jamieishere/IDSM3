using System.Collections.Generic;
using System.Web.Mvc;

namespace IDSM.Model.ViewModels
{
    public class SearchFormViewModel
    {
        public IEnumerable<SelectListItem> Clubs { get; set; }
        public int GameID { get; set; }
        public int UserTeamID { get; set; }
    }
}