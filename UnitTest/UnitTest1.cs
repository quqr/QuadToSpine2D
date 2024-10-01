using QuadToSpine2D.Core;
using QuadToSpine2D.Core.Data;
using QuadToSpine2D.Core.Data.Quad;
using QuadToSpine2D.Core.Process;

namespace TestProject1;

public class Tests
{
    private List<List<string?>> _imagePath = [];

    private string _quadFilePath = string.Empty;

    [SetUp]
    public void Setup()
    {
        GlobalData.ImageSavePath  = @"E:\Asset\tt\images";
        GlobalData.ResultSavePath = @"E:\Asset\tt";
    }

    [Test]
    public void Test1()
    {
        Momohime();
        MainTest();
    }

    [Test]
    public void Test2()
    {
        BlackKnight_HG_M00();
        MainTest();
    }

    [Test]
    public void Test3()
    {
        HD_Gwendlyn();
        MainTest();
    }

    [Test]
    public void Test4()
    {
        Fuyusaka00();
        MainTest();
    }

    private void Momohime()
    {
        _quadFilePath = @"E:\Asset\momohime\4k\00Files\file\Momohime_Rest.mbs.v55.quad";
        _imagePath =
        [
            [
                @"E:\Asset\momohime\4k\00Files\file\Momohime.0.tpl1.png"
            ],
            [
                @"E:\Asset\momohime\4k\00Files\file\Momohime.1.tpl.png",
                @"E:\Asset\momohime\4k\00Files\file\Momohime_Dark_tex.1.tpl.png"
            ],
            [
                @"E:\Asset\momohime\4k\00Files\file\Momohime.2.tpl.png",
                @"E:\Asset\momohime\4k\00Files\file\Momohime_Dark_tex.2.tpl.png"
            ]
        ];
    }

    private void Fuyusaka00()
    {
        _quadFilePath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.mbs.v55.quad";
        _imagePath =
        [
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.0.nvt.png"],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi sent Fuyusaka00.1.nvt.png"]
        ];
    }

    private void HD_Gwendlyn()
    {
        _quadFilePath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin REHD_Gwendlyn.mbs.v55.quad";
        _imagePath =
        [
            [
                @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.0.gnf.png"
            ],
            [
                @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.1.gnf.png"
            ],
            [@"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\ps4 odin HD_Gwendlyn.2.gnf.png"]
        ];
    }

    private void BlackKnight_HG_M00()
    {
        _quadFilePath =
            @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M.mbs.v55.quad";
        _imagePath =
        [
            [
                @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.0.nvt.png"
            ],
            [
                @"D:\Download\quad_mobile_v05_beta-20240404-2000\quad_mobile_v05_beta\data\swi unic BlackKnight_HG_M00.1.nvt.png"
            ]
        ];
    }

    private void MainTest()
    {
        Directory.Delete(GlobalData.ImageSavePath, true);
        Directory.CreateDirectory(GlobalData.ImageSavePath);
        GlobalData.ImagePath = _imagePath;
        var quadData = new ProcessQuadData().LoadQuadJson(_quadFilePath);
        var pool     = new Pool();
        var quad     = quadData.QuadData;
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
                    if (timeline.Attach?.Keyframe?.Layers is null) continue;
                    foreach (var layer in timeline.Attach.Keyframe.Layers)
                    {
                        var data = pool.Get(layer);
                    }
                }
            }

            pool.ReleaseAll();
        }

        var m1         = _imagePath.Max(x => x.Count);
        var layerCount = quadData.QuadData.Keyframe.Sum(x => x.Layers.Count);
    }
}