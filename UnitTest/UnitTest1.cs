using QuadToSpine2D.Core.Utility;

namespace TestProject1;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    static Matrix dst = new Matrix(4, 2, [10.96875f, -49, 0, -28.375f, -11, -49, 0, -90.59375f]);

    Matrix expected2 = new Matrix(4, 2, [
        6.1027299687499905f,
        -116.18558828125f,
        28.371677749999996f,
        -115.994246875f,
        17.087082999999993f,
        -135.211075f,
        -22.261816562499995f,
        -145.22741546875f
    ]);

    private Matrix m2 = new Matrix(4, 4,
        [
            -0.499999f, -0.813798f,
            0.296197f, 51.166999f,
            0.866025f, -0.469845f,
            0.171009f, -102.833404f,
            0, 0.342019f,
            0.939692f, 0,
            0, 0,
            0, 1
        ]
    );

    [Test]
    public void Test1()
    {
        var actual = AnimationMatrixUtility.QuadMultiply(m2, dst);

        Assert.That(actual, Is.EqualTo(expected2));
    }
}