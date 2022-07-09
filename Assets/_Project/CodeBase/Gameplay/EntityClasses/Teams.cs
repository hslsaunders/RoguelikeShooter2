using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public static class Teams
    {
        private static Dictionary<int, EntityTeam> _teamDictionary = new Dictionary<int, EntityTeam>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad()
        {
            _teamDictionary = new Dictionary<int, EntityTeam>();
        }
        
        public static void AddNewTeamMember(Entity entity)
        {
            if (!_teamDictionary.ContainsKey(entity.teamId))
                _teamDictionary.Add(entity.teamId, new EntityTeam(entity.teamId));
            _teamDictionary[entity.teamId].members.Add(entity);
        }

        public static List<Entity> GetEnemyOfTeamIdList(int teamId)
        {
            List<Entity> enemyTeamMembers = new List<Entity>();
            foreach ((int id, EntityTeam team) in _teamDictionary)
            {
                if (id == teamId && teamId != -1) continue;
                enemyTeamMembers.AddRange(team.members);
            }
            return enemyTeamMembers;
        }
    }
}