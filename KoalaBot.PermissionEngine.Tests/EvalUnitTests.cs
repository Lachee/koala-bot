using Xunit;
using KoalaBot.PermissionEngine.Groups;
using System.Collections.Generic;
using System.Linq;

namespace KoalaBot.PermissionEngine.Tests
{
    public class EvalUnitTests
    {
        [Fact]
        public async void Eval_CanEvaluateExactNode()
        {
            var engine = new Engine();
            var group = (Group) await engine.AddGroupAsync(new Group(engine, "top"));
            group.AddPermission("+koala.execute");
            var state = await group.EvaluatePermissionAsync("koala.execute");
            Assert.Equal(StateType.Allow, state);
        }

        [Fact]
        public async void Eval_CanEvaluateParentNode()
        {
            var engine = new Engine();
            var group = (Group) await engine.AddGroupAsync(new Group(engine, "top"));
            group.AddPermission("+koala.execute");
            var state = await group.EvaluatePermissionAsync("koala.execute.tastylobby");
            Assert.Equal(StateType.Allow, state);
        }


        [Fact]
        public async void Eval_CanEvaluateGroups()
        {
            var engine = new Engine();
            var top = (Group)await engine.AddGroupAsync(new Group(engine, "top"));
            var inner = (Group)await engine.AddGroupAsync(new Group(engine, "inner"));

            top.AddPermission(Permission.FromGroup(inner, StateType.Allow));
            inner.AddPermission("koala.execute");

            var state = await top.EvaluatePermissionAsync("koala.execute");

            Assert.Equal(StateType.Allow, state);
        }

        [Fact]
        public async void Eval_CanSelectGroup()
        {
            var engine = new Engine();
            var top = (Group)await engine.AddGroupAsync(new Group(engine, "top"));
            var inner = (Group)await engine.AddGroupAsync(new Group(engine, "inner"));

            top.AddPermission(Permission.FromGroup(inner, StateType.Allow));
            inner.AddPermission("koala.execute");

            var state = await top.EvaluatePermissionAsync("group.inner");
            Assert.Equal(StateType.Allow, state);
        }

        [Fact]
        public async void Eval_CanSelectEmptyGroup()
        {
            var engine = new Engine();
            var top = (Group)await engine.AddGroupAsync(new Group(engine, "top"));
            var inner = (Group)await engine.AddGroupAsync(new Group(engine, "inner"));

            top.AddPermission(Permission.FromGroup(inner, StateType.Allow));

            var state = await top.EvaluatePermissionAsync("group.inner");
            Assert.Equal(StateType.Allow, state);
        }

        [Fact]
        public async void Eval_Priority()
        {
            var engine = new Engine();
            var groupA = (Group)await engine.AddGroupAsync(new Group(engine, "a") { Priority = 15 });
            var groupB = (Group)await engine.AddGroupAsync(new Group(engine, "b") { Priority = 10 });
            var groupC = (Group)await engine.AddGroupAsync(new Group(engine, "c") { Priority = 5 });
            groupA.AddPermission("-koala.execute");
            groupB.AddPermission("+koala.execute");
            groupC.AddPermission("+koala.execute.channels");

            var groupU = (Group)await engine.AddGroupAsync(new Group(engine, "u"));
            groupU.AddPermission(groupA);
            groupU.AddPermission(groupB);
            groupU.AddPermission(groupC);

            var groupV = (Group)await engine.AddGroupAsync(new Group(engine, "v"));
            groupV.AddPermission(groupC);
            groupV.AddPermission(groupB);
            groupV.AddPermission(groupA);

            Assert.Equal(StateType.Deny, await groupU.EvaluatePermissionAsync("koala.execute"));
            Assert.Equal(StateType.Deny, await groupV.EvaluatePermissionAsync("koala.execute"));
        }

        [Fact]
        public async void Eval_CanCheckIfHasGroup()
        {
            var engine = new Engine();
            var group = new Group(engine, "basicgroup");
            group.AddPermission("+group.everyone");
            
            Assert.Equal(StateType.Allow, await group.EvaluatePermissionAsync("group.everyone"));
        }

        [Fact]
        public async void Eval_GroupExactNodeOverridesSubGroups()
        {
            var engine = new Engine();
            var top = (Group) await engine.AddGroupAsync(new Group(engine, "top"));
            var inner = (Group) await engine.AddGroupAsync(new Group(engine, "inner"));
            var sub = (Group) await engine.AddGroupAsync(new Group(engine, "sub"));

            top.AddPermission(inner);

            inner.AddPermission("+koala.execute");
            inner.AddPermission(sub);

            sub.AddPermission("-koala.execute");

            var state = await top.EvaluatePermissionAsync("koala.execute");
            Assert.Equal(StateType.Allow, state);
        }

        [Fact]
        public async void Eval_DenyGroupsResultInDeny()
        {
            var engine = new Engine();
            var top = (Group) await engine.AddGroupAsync(new Group(engine, "top"));
            var inner = (Group) await engine.AddGroupAsync(new Group(engine, "inner"));

            top.AddPermission(inner, StateType.Deny);
            inner.AddPermission("+koala.execute");
            inner.AddPermission("-koala.bake");

            Assert.Equal(StateType.Deny, await top.EvaluatePermissionAsync("koala.execute"));
            Assert.Equal(StateType.Deny, await top.EvaluatePermissionAsync("koala.bake"));
        }

        [Fact]
        public async void Eval_FirstGroupWithValueOverridesOtherGroups()
        {
            var engine = new Engine();
            var top = (Group) await engine.AddGroupAsync(new Group(engine, "top"));
            var innerA = (Group) await engine.AddGroupAsync(new Group(engine, "innerA"));
            var innerB = (Group) await engine.AddGroupAsync(new Group(engine, "innerB"));

            top.AddPermission(innerA);
            top.AddPermission(innerB);

            innerA.AddPermission("+koala.execute");
            innerB.AddPermission("-koala.execute");

            Assert.Equal(StateType.Allow, await top.EvaluatePermissionAsync("koala.execute"));
        }

        [Fact]
        public async void Eval_ExactNodeOverridesParentNode()
        {
            var engine = new Engine();
            var group = new Group(engine, "basicgroup");
            group.AddPermission("+koala.execute.tastylobby");
            group.AddPermission("-koala.execute");

            var state = await group.EvaluatePermissionAsync("koala.execute.tastylobby");

            Assert.Equal(StateType.Allow, state);
        }


        [Fact]
        public async void Eval_ExactNodeOverridesGroup()
        {
            var engine = new Engine();
            var top = (Group) await engine.AddGroupAsync(new Group(engine, "top"));
            var inner = (Group) await engine.AddGroupAsync(new Group(engine, "inner"));

            top.AddPermission("+koala.execute");
            top.AddPermission(Permission.FromGroup(inner, StateType.Allow));

            inner.AddPermission("-koala.execute");

            var state = await top.EvaluatePermissionAsync("koala.execute");
            Assert.Equal(StateType.Allow, state);
        }

        [Fact]
        public async void Eval_ParentNodeOverridesGroups()
        {
            var engine = new Engine();
            var top = (Group) await engine.AddGroupAsync(new Group(engine, "top"));
            var inner = (Group) await engine.AddGroupAsync(new Group(engine, "inner"));

            top.AddPermission("+koala.execute");
            top.AddPermission(Permission.FromGroup(inner, StateType.Allow));

            inner.AddPermission("-koala.execute.tasty");

            var state = await top.EvaluatePermissionAsync("koala.execute.tasty");
            Assert.Equal(StateType.Allow, state);
        }

        [Fact]
        public async void Eval_SubGroupParentNodeOverridesMissingParentNode()
        {
            var engine = new Engine();

            var groupA = (Group) await engine.AddGroupAsync(new Group(engine, "groupA"));
            var groupB = (Group) await engine.AddGroupAsync(new Group(engine, "groupB"));

            groupA.AddPermission("-koala.execute.adminchat");
            groupA.AddPermission(groupB, StateType.Allow);
            groupB.AddPermission("+koala.execute");

            Assert.Equal(StateType.Allow, await groupA.EvaluatePermissionAsync("koala.execute"));
        }

        [Fact]
        public async void Eval_SubGroupParentNodeDoesNotOverrideExactNode()
        {
            var engine = new Engine();

            var groupA = (Group)await engine.AddGroupAsync(new Group(engine, "groupA"));
            var groupB = (Group)await engine.AddGroupAsync(new Group(engine, "groupB"));

            groupA.AddPermission("-koala.execute.adminchat");
            groupA.AddPermission(groupB, StateType.Allow);
            groupB.AddPermission("+koala.execute");

            Assert.Equal(StateType.Deny, await groupA.EvaluatePermissionAsync("koala.execute.adminchat"));
        }

        [Fact]
        public async void Eval_CanMatchFlatPatterns()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
                ::groupa
                +koala.execute.channel
                -koala.execute
                +group.role.admin
                +group.role.bacon.and.eggs
                ::role.admin
                ::role.bacon.and.eggs");

            var group = await engine.GetGroupAsync("groupa");
            Assert.NotNull(group);

            var permissions = await group.EvaluatePatternAsync(new System.Text.RegularExpressions.Regex(@"group\.role\..*"));
            var collapsed = permissions.Select(r => r.ToString());
            Assert.Contains("+group.role.admin", collapsed);
            Assert.Contains("+group.role.bacon.and.eggs", collapsed);
            Assert.DoesNotContain("-koala.execute", collapsed);
        }

        [Fact]
        public async void Eval_CanMatchSubPatterns()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
                ::groupa
                +koala.fruit.apricot
                +koala.execute.channel
                +group.role.admin
                +group.role.bacon.and.eggs
                
                ::role.admin
                +koala.fruit.mango
                +group.secondorder
    
                ::role.bacon.and.eggs
                +koala.fruit.apple

                ::secondorder
                +koala.fruit.orange
");

            var group = await engine.GetGroupAsync("groupa");
            Assert.NotNull(group);

            var permissions = await group.EvaluatePatternAsync(new System.Text.RegularExpressions.Regex(@"koala\.fruit\..*"));
            var collapsed = permissions.Select(r => r.ToString());
            Assert.Contains("+koala.fruit.mango", collapsed);
            Assert.Contains("+koala.fruit.apple", collapsed);
            Assert.Contains("+koala.fruit.orange", collapsed);
            Assert.Contains("+koala.fruit.apricot", collapsed);
            Assert.DoesNotContain("+koala.execute.channel", collapsed);
            Assert.DoesNotContain("+koala.role.admin", collapsed);
        }

        [Fact]
        public async void Eval_MatchOverrideRules()
        {
            var engine = new Engine();
            await engine.ImportAsync(@"
                ::groupa
                +koala.fruit.apricot
                +koala.execute.channel
                +group.role.admin
                +group.role.bacon.and.eggs
                
                ::role.admin|0
                +koala.fruit.mango
                -koala.fruit.apple
                +group.secondorder
    
                ::role.bacon.and.eggs
                +koala.fruit.apple

                ::secondorder
                -koala.fruit.mango
");

            var group = await engine.GetGroupAsync("groupa");
            Assert.NotNull(group);

            var permissions = await group.EvaluatePatternAsync(new System.Text.RegularExpressions.Regex(@"koala\.fruit\..*"));
            var collapsed = permissions.Select(r => r.ToString());
            Assert.Contains("+koala.fruit.mango", collapsed);
            Assert.Contains("+koala.fruit.apple", collapsed);
            Assert.DoesNotContain("-koala.fruit.mango", collapsed);
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
