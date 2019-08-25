using Xunit;
using KoalaBot.PermissionEngine.Groups;
using System.Collections.Generic;
using System.Linq;

namespace KoalaBot.PermissionEngine.Tests
{
    public class GroupUnitTest
    {

        [Fact]
        public void Add_GroupContainsNoDuplicates()
        {
            //Arrange
            var engine = new Engine();
            var group = new Group(engine, "basicgroup");
            group.AddPermission("+koala.execute");
            group.AddPermission("-koala.execute.tastylobby");
            group.AddPermission("koala.execute");
            group.AddPermission(new Permission("koala.execute.tastylobby", StateType.Deny));

            //Act
            HashSet<string> names = new HashSet<string>();
            foreach (var p in group.PermissionsEnumerable())
            {
                Assert.True(names.Add(p.Name));
            }

        }

        [Fact]
        public void Remove_CanRemovePermissions()
        {
            var engine = new Engine();
            var group = new Group(engine, "basicgroup");
            group.AddPermission("+koala.execute");
            group.AddPermission("-koala.execute.tastylobby");
            group.AddPermission("koala.mango");

            group.RemovePermission("koala.execute");
            group.RemovePermission(Permission.FromString("koala.mango"));

            Assert.Equal(StateType.Unset, group.GetPermission("koala.mango").State);
            Assert.Equal(StateType.Unset, group.GetPermission("koala.execute").State);
            Assert.Equal(StateType.Deny, group.GetPermission("koala.execute.tastylobby").State);

        }

        [Fact]
        public void Add_AddOverrides()
        {
            //Arrange
            var engine = new Engine();
            var group = new Group(engine, "basicgroup");
            group.AddPermission("+koala.execute");
            group.AddPermission("-koala.execute");

            var permission = group.GetPermission("koala.execute");
            Assert.Equal(StateType.Deny, permission.State);
        }
    }




    /* TEST:    koala.execute.tastylobby
     * RESULT:  Allow
     * RULE:    
     * The more specific rules take president on the top level.
     * 
     * SETUP:
     * +koala.execute.tastylobby
     * +group.admin
     *      -koala.execute
    */

    /* TEST:    koala.execute.tastylobby
     * RESULT:  Allow
     * RULE:    
     * The more specific rules take president on the top level.
     * 
     * SETUP:
     * +koala.execute.tastylobby
     * -koala.execute
    */

}
