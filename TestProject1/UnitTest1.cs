using QuadToSpine2D.Core.Utility;

namespace TestProject1;

[TestClass]
public class UnitTest1
{
    private readonly Matrix _animationMatrix = new Matrix(4, 4,
    [
        0.999945f, -0.01047f, 0, 1.375f,
        0.01047f, 0.999945f, 0, -156.541809f,
        0, 0, 1, 0,
        0, 0, 0, 1
    ]);
    [TestMethod]
    public void TestMethod1()
    {
        var dstMatrix = new Matrix(4, 2,
            [
                17.75f, -11.78125f,
                17.9375f, 7.1875f,
                33.4375f, 6.375f,
                30.46875f, -12.625f
            ]);
        var sampleMatrix = new Matrix(4, 2,
        [
            -57.14776098071232f, 499.21764451353f, -2.9691199771377805f, -46.967453426469625f, 364.2074526473047f,
            -2.4416104098960174f, -83.87768226244222f, 361.683725743013f, -2.4141690139660446f, -94.05798981668491f,
            496.69391760923816f, -2.9416785812078077f
        ]);
        AssertMatrix(dstMatrix, sampleMatrix);
    }
    [TestMethod]
    public void TestMethod2()
    {
        var dstMatrix = new Matrix(4, 2,
            [
                17.75f, -11.78125f,
                17.9375f, 7.1875f,
                33.4375f, 6.375f,
                30.46875f, -12.625f
            ]);
        var sampleMatrix = new Matrix(4, 2,
        [
            -57.14776098071232f, 499.21764451353f, -2.9691199771377805f, -46.967453426469625f, 364.2074526473047f,
            -2.4416104098960174f, -83.87768226244222f, 361.683725743013f, -2.4141690139660446f, -94.05798981668491f,
            496.69391760923816f, -2.9416785812078077f
        ]);
        AssertMatrix(dstMatrix, sampleMatrix);
    }
    private void AssertMatrix(Matrix dstMatrix, Matrix sampleMatrix)
    {
        var resultDstMatrix = AnimationMatrixUtility.QuadMultiply(_animationMatrix, dstMatrix);
        resultDstMatrix = AnimationMatrixUtility.GetPerspectiveQuad(resultDstMatrix);
        Assert.AreEqual(sampleMatrix, resultDstMatrix);
    }
}