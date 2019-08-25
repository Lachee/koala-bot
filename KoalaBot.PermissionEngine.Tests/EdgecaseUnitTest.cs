using Xunit;
using KoalaBot.PermissionEngine.Groups;
using System.Collections.Generic;
using System.Linq;

namespace KoalaBot.PermissionEngine.Tests
{
    public class EdgecaseUnitTest
    {
        [Fact]
        public async void EC01_ThirdOrderGroupCheck()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
                ::everyone
                -koala.execute
                +koala.execute.botspam
                
                ::staff
                +group.everyone
                +koala.execute.staff
                
                ::admin
                +group.staff
                +koala.execute.adminchat
            ");

            var admin = await engine.GetGroupAsync("admin");

            Assert.NotNull(admin);
            Assert.Equal(StateType.Allow, await admin.EvaluatePermissionAsync("koala.execute.adminchat"));
            Assert.Equal(StateType.Allow, await admin.EvaluatePermissionAsync("koala.execute.staff"));
            Assert.Equal(StateType.Allow, await admin.EvaluatePermissionAsync("koala.execute.botspam"));
            Assert.Equal(StateType.Deny, await admin.EvaluatePermissionAsync("koala.execute.lobby"));
        }

        [Fact]
        public async void EC02_RoleSubtraction()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
                ::everyone|0
                -group.contributor
                
                ::contributor
                +group.role.contributor

                ::role.builder
                +group.contributor

                ::user.vex
                +group.everyone
                +group.role.builder

                ::user.lachee
                +group.everyone

                ::user.nobody
                ::role.contributor
            ");

            var vex     = await engine.GetGroupAsync("user.vex");
            Assert.NotNull(vex);

            var lachee = await engine.GetGroupAsync("user.lachee");
            Assert.NotNull(lachee);

            var nobody = await engine.GetGroupAsync("user.nobody");
            Assert.NotNull(nobody);

            var tree = await TreeBranch.CreateTreeAsync(vex);

            Assert.Equal(StateType.Allow, await vex.EvaluatePermissionAsync("group.role.contributor"));
            Assert.Equal(StateType.Deny, await lachee.EvaluatePermissionAsync("group.role.contributor"));
            Assert.Equal(StateType.Unset, await nobody.EvaluatePermissionAsync("group.role.contributor"));
        }

        [Fact]
        public async void EC03_NoRole()
        {
            var engine = new Engine();
            await engine.ImportAsync(
@"

::user.130973321683533824
+group.everyone
+koala.execute

::everyone|0
-koala.execute
-group.role.364299039535267840

::role.420407708487778305
+group.games

::games
+group.role.364299039535267840


::role.419674001137336321
+group.games

::role.364296937618538496
+group.games

");

            var group = await engine.GetGroupAsync("user.130973321683533824");
            Assert.NotNull(group);

            Assert.Equal(StateType.Deny, await group.EvaluatePermissionAsync("group.role.364299039535267840"));
        }

        [Fact]
        public async void EC04_ValueOverridesUnsets()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
            ::user.130973321683533824
              +group.everyone
              ?group.role.364299039535267840
              ?group.role.appricots

            ::everyone|0
            -koala.execute
            -group.role.364299039535267840

            ::role.364299039535267840
            ");

            var group = await engine.GetGroupAsync("user.130973321683533824");
            Assert.NotNull(group);

            Assert.Equal(StateType.Unset, await group.EvaluatePermissionAsync("group.role.appricots"));
            Assert.Equal(StateType.Deny, await group.EvaluatePermissionAsync("group.role.364299039535267840"));
        }

        [Fact]
        public async void EC05_ValueOverridesUnsetsForPatterns()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
            ::user.130973321683533824
              +group.everyone
              ?group.role.364299039535267840
              ?group.role.appricots

            ::everyone|0
            -koala.execute
            -group.role.364299039535267840

            ::role.364299039535267840
            ");

            var group = await engine.GetGroupAsync("user.130973321683533824");
            var permissions = await group.EvaluatePatternAsync(new System.Text.RegularExpressions.Regex(@"group\.role\..*"));
            var collapsed = permissions.Select(r => r.ToString());
            Assert.Contains("?group.role.appricots", collapsed);
            Assert.Contains("-group.role.364299039535267840", collapsed);
        }

        [Fact]
        public async void EC06_SkywingRoles_RulesOfInhertiencesShouldApplyToPatterns()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
::everyone|0
-koala.execute
+koala.execute.609641159202963474
+koala.execute.487648961113620480
-group.staff
-sw

::role.260570184559886340
+group.staff

::staff
+koala.execute.261193452459393025
+group.role.615065754118651937
+sw

::user.106779287780077568
+group.everyone
?group.role.259735467400888320
?group.role.261102231066116096
?group.role.261102234153123840
?group.role.615065754118651937
?group.role.259888961881636874
?group.role.261102236841672716
?group.role.261102858869538826
");

            var group = await engine.GetGroupAsync("user.106779287780077568");
            Assert.NotNull(group);

            var permissions = await group.EvaluatePatternAsync(new System.Text.RegularExpressions.Regex(@"group\.role\..*"));
            var collapsed = permissions.Select(r => r.ToString());

            //When we minus a group, all our conditions should be deny
            Assert.Contains("-group.role.615065754118651937", collapsed);
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
