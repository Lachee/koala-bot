using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace KoalaBot.PermissionEngine.Groups
{
    public class TreeBranch
    {
        public Group group;
        public Permission permission;

        public TreeBranch parent;
        public List<TreeBranch> children = new List<TreeBranch>();


        public TreeBranch()
        {
            parent = null;
        }

        private TreeBranch(TreeBranch parent, Permission perm)
        {
            this.parent = parent;
            this.permission = perm;
            this.group = null;
        }

        private TreeBranch(Group group)
        {
            this.parent = null;
            this.group = group;
            this.permission = Permission.FromGroup(group, StateType.Unset);
        }

        private TreeBranch(TreeBranch parent, Permission perm, Group group)
        {
            this.parent = parent;
            this.group = group;
            this.permission = perm;
        }


        public static async Task<TreeBranch> CreateTreeAsync(Group entry)
        {
            TreeBranch tree = new TreeBranch(entry);
            await tree.CreateSubBranches();
            return tree;
        }

        private async Task CreateSubBranches()
        {
            if (group == null) return;
            foreach(var perm in group.PermissionsEnumerable())
            {
                TreeBranch branch;
                if (perm.IsGroup)
                {
                    //WE are a group, so get our subgroup
                    var g2 = await group.Engine.GetGroupAsync(perm);
                    if (g2 == null)
                    {
                        //We dont have a group, create a regular branch
                        branch = new TreeBranch(this, perm);
                    }
                    else
                    {
                        //We have a subgrou
                        branch = new TreeBranch(this, perm, g2);
                        await branch.CreateSubBranches();
                    }
                }
                else
                {
                    branch = new TreeBranch(this, perm);
                }

                children.Add(branch);
            }
        }

        public void BuildTreeString(StringBuilder sb, int depth = 0)
        {
            sb.Append(' ', depth * 2).Append(permission).Append('\n');
            foreach (var child in children)
                child.BuildTreeString(sb, depth + 1);
        }

        public override string ToString()
        {
            return permission.ToString();
        }
    }
}
