using QuadToSpine2D.AvaUtility;
using QuadToSpine2D.Core;
using QuadToSpine2D.Core.Data;
using QuadToSpine2D.Core.Data.Quad;
using QuadToSpine2D.Core.Process;
using QuadToSpine2D.Core.Utility;

namespace TestProject1;

public class Tests
{
    [SetUp]
    public void Setup()
    {        
        GlobalData.ImageSavePath = @"E:\Asset\tt\images";
        GlobalData.ResultSavePath = @"E:\Asset\tt";
    }

    private string _quadFilePath = string.Empty;
    private List<List<string?>> _imagePath = [];

    [Test]
    public void Test1()
    {
        s0();
        MainTest();
    }
    [Test]
    public void Test2()
    {
        s1();
        MainTest();
    }
    [Test]
    public void Test3()
    {
        s2();
        MainTest();
    }
    [Test]
    public void Test4()
    {
        s3();
        MainTest();
    }

    private void s0()
    {
        _quadFilePath = @"E:\Asset\momohime\4k\00Files\file\Momohime_Rest.mbs.v55.quad";
        _imagePath = [
            [@"E:\Asset\momohime\4k\00Files\file\Momohime.0.tpl1.png",
            ],
            [@"E:\Asset\momohime\4k\00Files\file\Momohime.1.tpl.png",
                @"E:\Asset\momohime\4k\00Files\file\Momohime_Dark_tex.1.tpl.png"],
            [@"E:\Asset\momohime\4k\00Files\file\Momohime.2.tpl.png",
                @"E:\Asset\momohime\4k\00Files\file\Momohime_Dark_tex.2.tpl.png"]
        ];
    }

    private void s3()
    {
        _quadFilePath = @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.mbs.v55.quad";
        _imagePath = 
        [
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.0.nvt.png"],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.1.nvt.png"],
        ];
    }

    private void s2()
    {
        _quadFilePath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin REHD_Gwendlyn.mbs.v55.quad";
        _imagePath =
        [
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.0.gnf.png"],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.1.gnf.png"],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.2.gnf.png"],
        ];
    }

    private void s1()
    {
        _quadFilePath = @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M.mbs.v55.quad";
        _imagePath = 
        [
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.0.nvt.png"],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.1.nvt.png"],
        ];
    }

    private void MainTest()
    {
        Directory.Delete(GlobalData.ImageSavePath, true);
        Directory.CreateDirectory(GlobalData.ImageSavePath);
        GlobalData.ImagePath = _imagePath;
        var quadData = new ProcessQuadData().LoadQuadJson(_quadFilePath);
        var pool = new LayerDataPool();
        var quad = quadData.QuadData;
        foreach (var skeleton in quad.Skeleton)
        {
            List<Animation> animations = [];
            animations
                .AddRange(skeleton.Bone
                    .Select(bone => quad.Animation
                        .First(x => x.Id == bone.Attach.Id)));
            foreach (var animation in animations)
            {
                var time = 0f;
                foreach (var timeline in animation.Timeline)
                {
                    time += timeline.Frames;
                    if (timeline.Attach?.Keyframe?.Layer is null) continue;
                    foreach (var layer in timeline.Attach.Keyframe.Layer)
                    {
                        var data = pool.Get(layer);
                    }
                }
            }
            pool.ReleaseAll();
        }

        var max = pool._all.Max(x => x.LayersData.Count);
        var m1 = _imagePath.Max(x => x.Count);
        Assert.That(max, Is.EqualTo(m1));
        var layerCount = quadData.QuadData.Keyframe.Sum(x => x.Layer.Count);
        Assert.That(layerCount, Is.EqualTo(pool._all.Count));
    }
}