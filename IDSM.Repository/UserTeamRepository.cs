using IDSM.Model;
using System.Collections.Generic;
using System.Linq;

namespace IDSM.Repository
{
    public interface IUserTeamRepository : IRepositoryBase<UserTeam>
    {
        IEnumerable<UserTeam> GetAllUserTeamsCurrentlyActive(int userId);
    }

    public class UserTeamRepository : RepositoryBase<UserTeam>, IUserTeamRepository
    {
        public UserTeamRepository(IDSMContext context) : base(context) { }

        /// <summary>
        /// GetAllUserTeamsCurrentlyActive
        /// Gets all UserTeams, eager loads Game (is this correct term?)
        /// </summary>
        /// <returns>IEnumerable<UserTeam></returns>
        public IEnumerable<UserTeam> GetAllUserTeamsCurrentlyActive(int userId)
        {
            var _userTeams = DataContext.UserTeams
                            .Where(x => x.UserId==userId && x.Game.HasEnded != true)
                            .ToList();
            return _userTeams;
        }
    }


}
