using Xunit;
using KoalaBot.PermissionEngine.Groups;
using System.Collections.Generic;

namespace KoalaBot.PermissionEngine.Tests
{
    public class PermissionUnitTest
    {
        [Fact]
        public void Perm_FromStringIsConsistent()
        {
            var perma = new Permission("koala.execute", StateType.Allow);
            var permb = Permission.FromString("+koala.execute");

            Assert.Equal(perma.Name, permb.Name);
            Assert.Equal(perma.State, permb.State);
        }

        [Fact]
        public void Perm_FromGroupIsConsistent()
        {
            var groupA = new Group(null, "mygroup");

            var perma = new Permission(Permission.GROUP_PREFIX + "mygroup", StateType.Allow);
            var permb = Permission.FromGroup(groupA, StateType.Allow);

            Assert.Equal(perma.Name, permb.Name);
            Assert.Equal(perma.State, permb.State);
            Assert.Equal(perma, permb);
        }

        [Fact]
        public void Perm_ToStringIsFromString()
        {
            string strA = "+koala.execute";
            string strD = "-koala.execute";
            string strU = "?koala.execute";

            var permA = Permission.FromString(strA);
            var permD = Permission.FromString(strD);
            var permU = Permission.FromString(strU);

            Assert.Equal(strA, permA.ToString());
            Assert.Equal(strD, permD.ToString());
            Assert.Equal(strU, permU.ToString());
        }

        [Fact]
        public void Perm_EmptyPrefixIsDefault()
        {
            var permA = Permission.FromString("koala.execute", StateType.Unset);
            Assert.Equal(StateType.Unset, permA.State);
        }

        [Fact]
        public void Perm_Trimmed()
        {
            var perma = new Permission(" koala.perma", StateType.Allow);
            var permb = Permission.FromString("+ koala.permb");
            var permc = Permission.FromString("koala .permb");
            Assert.DoesNotContain(" ", perma.Name);
            Assert.DoesNotContain(" ", permb.Name);
            Assert.DoesNotContain(" ", permc.Name);
        }

        [Fact]
        public void Perm_NoIllegalCharacters()
        {
            var regex = @"[\+\-:\s\|]";
            var control = new Permission("koalabot.execute.tea", StateType.Allow);
            var perma = new Permission("? KoAla|Bo.: t.ExeCUte", StateType.Allow);
            var permb = Permission.FromString("KOa :. l? +ABot.Mango");
            var permc = Permission.FromGroup(new Group(null, "AG+-roup"), StateType.Unset);

            Assert.DoesNotMatch(regex, control.Name);
            Assert.DoesNotMatch(regex, perma.Name);
            Assert.DoesNotMatch(regex, permb.Name);
            Assert.DoesNotMatch(regex, permc.Name);
        }

        [Fact]
        public void Perm_AlwaysLowercase()
        {
            var perma = new Permission("KoAlaBot.ExeCUte", StateType.Allow);
            var permb = Permission.FromString("KOalABot.Mango");
            var permc = Permission.FromGroup(new Group(null, "AGroup"), StateType.Unset);

            Assert.Equal(perma.Name.ToLowerInvariant(), perma.Name);
            Assert.Equal(permb.Name.ToLowerInvariant(), permb.Name);
            Assert.Equal(permc.Name.ToLowerInvariant(), permc.Name);


            Assert.Equal(perma.ToString().ToLowerInvariant(), perma.ToString());
            Assert.Equal(permb.ToString().ToLowerInvariant(), permb.ToString());
            Assert.Equal(permc.ToString().ToLowerInvariant(), permc.ToString());
        }


        public void Perm_NameForGroupAlwaysStartsWithPrefix()
        {
            var groupa = new Group(null, "mygroup");
            var perma = Permission.FromGroup(groupa, StateType.Unset);
            var permb = Permission.FromGroup("group." + groupa, StateType.Unset);
            var permc = Permission.FromString("group.role.somenumber");

            
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
