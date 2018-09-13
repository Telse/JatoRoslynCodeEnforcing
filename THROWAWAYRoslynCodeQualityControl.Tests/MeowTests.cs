using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THROWAWAYRoslynCodeQualityControl.Tests
{
    [TestFixture(Category = "Meow Tests")]
    public class MeowTests
    {
        [TestFixture(Category = "MeowAtYou Tests")]
        public class MeowAtYou
        {
            [Test]
            public static void Should_Return_Meow()
            {

            }

            [Test]
            public static void Should_Not_Return_Meow()
            {

            }
        }

        [TestFixture(Category = "ScratchYou Tests")]
        public class ScratchYou
        {
            [Test]
            public static void Should_Scratch_You()
            {

            }
        }
    }
}
