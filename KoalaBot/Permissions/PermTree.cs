using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.Permissions
{
    public class PermTree
    {
        public static int TabbingSize = 3;

        public string Name => Group.Name;
        public Group Group { get;}
        public PermTree Parent { get; }

        public IReadOnlyList<PermTree> Children => _children;
        private List<PermTree> _children;

        public IReadOnlyList<Permission> Permissions => _permissions;
        private List<Permission> _permissions;

        public PermTree(Group group, PermTree parent = null)
        {
            this.Group = group;
            this.Parent = parent;
            this._children = new List<PermTree>();
            this._permissions = new List<Permission>();
        }

        public PermTree AddChild(Group child)
        {
            var g = new PermTree(child, this);
            _children.Add(g);
            return g;
        }

        public void AddPermission(Permission permission)
        {
            _permissions.Add(permission);
        }

        /// <summary>
        /// Collapses the tree into a formatted string.
        /// </summary>
        /// <returns></returns>
        public string CollapseDown()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Name).Append(":\n");
            Append(builder, 1);
            return builder.ToString();
        }

        public string CollapseUp()
        {
            StringBuilder builder = new StringBuilder();
            List<string> parents = new List<string>();
            int tabbing = 0;

            //Prepare all the parents
            var parent = Parent;
            while (parent != null)
                parents.Add(parent.Name);

            //Reverse the parent list and add them all
            for (int i = parents.Count - 1; i >= 0; i--)
                builder.Append(' ', (tabbing++) * TabbingSize).Append(parents[i]).Append(":\n");

            //Append us
            builder.Append(Name).Append(":\n");
            Append(builder, tabbing, false);

            //Return
            return builder.ToString();
        }

        /// <summary>
        /// Appends a collapsed version of the group to a string builder.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="tabbing"></param>
        private void Append(StringBuilder builder, int tabbing = 0, bool includeChildren = true)
        {
            //Append the permissions
            foreach(var perm in _permissions)
            {
                builder.Append(' ', tabbing * TabbingSize);
                builder.Append(perm.ToString());
                builder.Append('\n');
            }

            if (includeChildren)
            {
                //Append the children
                foreach (var child in _children)
                {
                    builder.Append(' ', tabbing * TabbingSize);
                    builder.Append(child.Group.Name.ToString());
                    builder.Append(":\n");
                    child.Append(builder, tabbing + 1);
                }
            }
        }
    }
}
