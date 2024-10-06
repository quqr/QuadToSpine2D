using QuadToSpine2D.Core.Utility;

namespace NUnitTest;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
       var r1 = ProcessUtility.LCM([1, 2, 3, 4, 5]);
       Assert.That(r1, Is.EqualTo(60));
       var r2 = ProcessUtility.LCM([0,0,0,0]);
       Assert.That(r2, Is.EqualTo(0));
    }
}