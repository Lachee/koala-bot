using Xunit;
using KoalaBot.PermissionEngine.Groups;
using System.Collections.Generic;

namespace KoalaBot.PermissionEngine.Tests
{
    public class EngineUnitTest
    {
        [Fact]
        public async void Export_CanExport()
        {
            var exportEngine = new Engine();

            var groupA = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupA"));
            var groupB = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupB"));
            var groupC = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupC"));

            groupA.AddPermission("-koala.execute.adminchat");
            groupB.AddPermission("+koala.execute");
            groupC.AddPermission("-koala.execute");

            var result = "::groupa\n-koala.execute.adminchat\n::groupb\n+koala.execute\n::groupc\n-koala.execute\n";
            var export = await exportEngine.ExportAsync();
            Assert.Equal(result, export);

        }

        [Fact]
        public async void Export_CanExportWithSubgroups()
        {
            var engine = new Engine();

            var groupA = (Group)await engine.AddGroupAsync(new Group(engine, "groupA"));
            var groupB = (Group)await engine.AddGroupAsync(new Group(engine, "groupB"));
            var groupC = (Group)await engine.AddGroupAsync(new Group(engine, "groupC"));

            groupA.AddPermission("-koala.execute.adminchat");
            groupA.AddPermission(groupB, StateType.Allow);
            groupB.AddPermission("+koala.execute");
            groupC.AddPermission("-koala.execute");

            var result = "::groupa\n-koala.execute.adminchat\n+group.groupb\n::groupb\n+koala.execute\n::groupc\n-koala.execute\n";
            var export = await engine.ExportAsync();
            Assert.Equal(result, export);

        }

        [Fact]
        public async void Export_CanExportSingleGroup()
        {
            var exportEngine = new Engine();

            var groupA = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupA"));
            groupA.AddPermission("-koala.execute.adminchat");

            var result = "::groupa\n-koala.execute.adminchat\n";
            var export = exportEngine.ExportGroup(groupA);
            Assert.Equal(result, export);
        }

        [Fact]
        public async void ExportImport_CanImportExportedGroup()
        {
            var exportEngine = new Engine();
            var importEngine = new Engine();

            var exportGroupA = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupA"));
            exportGroupA.AddPermission("-koala.execute.adminchat");

            var export = exportEngine.ExportGroup(exportGroupA);
            await importEngine.ImportAsync(export);

            var importGroupA = await importEngine.GetGroupAsync("groupA");

            Assert.NotNull(importGroupA);
            Assert.Equal(exportGroupA.Name, importGroupA.Name);
            Assert.Equal(exportGroupA.PermissionsEnumerable(), importGroupA.PermissionsEnumerable());
        }

        [Fact]
        public async void Import_CanImport()
        {
            var import = "::groupa\n-koala.execute.adminchat\n::groupb\n+koala.execute\n::groupc\n-koala.execute\n";
            var importEngine = new Engine();

            await importEngine.ImportAsync(import);

            var groupA = await importEngine.GetGroupAsync("groupA");
            var groupB = await importEngine.GetGroupAsync("groupB");
            var groupC = await importEngine.GetGroupAsync("groupC");

            Assert.NotNull(groupA);
            Assert.NotNull(groupB);
            Assert.NotNull(groupC);

            Assert.Equal(StateType.Deny, groupA.GetPermission("koala.execute.adminchat").State);
            Assert.Equal(StateType.Allow, groupB.GetPermission("koala.execute").State);
            Assert.Equal(StateType.Deny, groupC.GetPermission("koala.execute").State);
        }

        [Fact]
        public async void Import_CanImportPriority()
        {
            var import = "::groupa|85\n-koala.check";
            var importEngine = new Engine();

            await importEngine.ImportAsync(import);

            var groupA = await importEngine.GetGroupAsync("groupA");

            Assert.NotNull(groupA);
            Assert.Equal(StateType.Deny, groupA.GetPermission("koala.check").State);
            Assert.Equal(85, groupA.Priority);
        }

        [Fact]
        public async void Import_CanImportWithSubgroups()
        {
            var import = "::groupa\n-koala.execute.adminchat\n+group.groupb\n::groupb\n+koala.execute\n::groupc\n-koala.execute\n";
            var importEngine = new Engine();

            await importEngine.ImportAsync(import);

            var groupA = await importEngine.GetGroupAsync("groupA");
            var groupB = await importEngine.GetGroupAsync("groupB");
            var groupC = await importEngine.GetGroupAsync("groupC");

            Assert.NotNull(groupA);
            Assert.NotNull(groupB);
            Assert.NotNull(groupC);

            Assert.Equal(StateType.Deny, groupA.GetPermission("koala.execute.adminchat").State);
            Assert.Equal(StateType.Allow, groupA.GetPermission(groupB).State);
            Assert.Equal(StateType.Allow, groupB.GetPermission("koala.execute").State);
            Assert.Equal(StateType.Deny, groupC.GetPermission("koala.execute").State);

            Assert.Equal(StateType.Allow, await groupA.EvaluatePermissionAsync("koala.execute"));
        }


        [Fact]
        public async void ExportImport_CanImportOwnExport()
        {
            var exportEngine = new Engine();
            var importEngine = new Engine();

            var exportGroupA = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupA"));
            var exportGroupB = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupB"));
            var exportGroupC = (Group)await exportEngine.AddGroupAsync(new Group(exportEngine, "groupC"));

            exportGroupA.AddPermission("-koala.execute.adminchat");
            exportGroupB.AddPermission("+koala.execute");
            exportGroupC.AddPermission("-koala.execute");

            var export = await exportEngine.ExportAsync();
            await importEngine.ImportAsync(export);


            var importGroupA = await importEngine.GetGroupAsync("groupA");
            var importGroupB = await importEngine.GetGroupAsync("groupB");
            var importGroupC = await importEngine.GetGroupAsync("groupC");

            Assert.Equal(exportGroupA.Name, importGroupA.Name);
            Assert.Equal(exportGroupA.PermissionsEnumerable(), importGroupA.PermissionsEnumerable());

            Assert.Equal(exportGroupB.Name, importGroupB.Name);
            Assert.Equal(exportGroupB.PermissionsEnumerable(), importGroupB.PermissionsEnumerable());

            Assert.Equal(exportGroupC.Name, importGroupC.Name);
            Assert.Equal(exportGroupC.PermissionsEnumerable(), importGroupC.PermissionsEnumerable());
        }

        [Fact]
        public async void Store_SaveModifiedGroup()
        {
            var engine = new Engine();

            var oldGroup = (Group)await engine.AddGroupAsync(new Group(engine, "gg"));
            oldGroup.AddPermission("+koala.execute");

            await engine.Store.ClearCacheAsync();

            var newGroup = await engine.GetGroupAsync("gg");
            Assert.Equal(oldGroup.Name, newGroup.Name);
            Assert.Equal(oldGroup.PermissionsEnumerable(), newGroup.PermissionsEnumerable());
        }

        [Fact]
        public async void Store_ReturnNullObjects()
        {
            var engine = new Engine();
            var group = await engine.GetGroupAsync("somegroup");
            Assert.Null(group);
        }



        [Fact]
        public async void Import_ImportsAllGroups()
        {
            Engine engine = new Engine();
            string import = @"
                ::everyone
                -group.contributor
                
                ::contributor
                +group.role.contributor

                ::role.builder
                +group.contributor

                ::role.contributor
                ::user.vex
                +group.everyone
                +group.role.builder

                ::user.lachee
                +group.everyone

                ::user.nobody";

            await engine.ImportAsync(import);
            Assert.NotNull(await engine.GetGroupAsync("everyone"));
            Assert.NotNull(await engine.GetGroupAsync("contributor"));
            Assert.NotNull(await engine.GetGroupAsync("role.builder"));
            Assert.NotNull(await engine.GetGroupAsync("user.vex"));
            Assert.NotNull(await engine.GetGroupAsync("user.lachee"));
            Assert.NotNull(await engine.GetGroupAsync("user.nobody"));
            Assert.NotNull(await engine.GetGroupAsync("role.contributor"));
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
