using System.Collections.Generic;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class EntityTeam
    {
        public EntityTeam(int id)
        {
            this.id = id;
        }
        public int id;
        public List<Entity> members = new List<Entity>();
    }
}