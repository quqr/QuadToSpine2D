using Newtonsoft.Json.Serialization;
using QuadPlayer.Spine;

namespace QuadPlayer;

public class ProcessSpineJson
{
    private SpineJson _spineJson=new();
    public string SpineJsonFile;
    private Dictionary<string, string> _layerGuidAndSlotName = new();
    public ProcessSpineJson(ProcessImage processImage,QuadJson quadJson)
    {
        Init(processImage);
        ProcessAnimation(quadJson,processImage);
        SpineJsonFile = ConvertJson();
        Console.WriteLine("ProcessSpineJson Finished");
    }

    private void Init(ProcessImage processImage)
    {
        _spineJson.Skeletons.Images = "D:/Download/quad_mobile_v05_beta-20240404-2000/quad_mobile_v05_beta/data/Output";
        _spineJson.Bones.Add(new Spine.Bone() { Name = "root"});
        _spineJson.Skins.Add(new Skin());
        for (int index = 0; index < processImage.ImagesData.Count; index++)
        {
            var layerGuid = processImage.ImagesData.Keys.ElementAt(index);
            var slotName = processImage.ImagesData[layerGuid].ImageName;
            var sliceName = slotName;
            _layerGuidAndSlotName[layerGuid] = slotName;
            _spineJson.Slots.Add(new Slot(){Name = slotName,Attachment = sliceName ,Bone = "root"});
            _spineJson.Skins[0].Attachments.Add(new Attachments
            {
                Value = new Mesh
                {
                    Name = sliceName,
                    Uvs = processImage.ImagesData[layerGuid].UVs,
                    //Vertices =  processImage.ImagesData[layerGuid].Vertices,
                    Vertices = new float[8],
                }
            });
        }

    }

    private string ConvertJson()
    {
        var setting = new JsonSerializerSettings()
            { ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() } };
        var json = JsonConvert.SerializeObject(_spineJson,Formatting.Indented,setting);
        return json;
    }

    private Dictionary<string,SpineAnimation> spineAnimations = new();
    private void ProcessAnimation(QuadJson quad,ProcessImage processImage)
    {
        foreach (var skeleton in quad.Skeleton)
        {
            HashSet<KeyframeLayer> keyframeLayers = [];
            SpineAnimation spineAnimation = new();
            Deform deform = new();
            var timelines = quad.Animation
                .Find(x => x.ID == skeleton.Bone[0].Attach.ID).Timeline
                .Where(a => a.Attach.Type.Equals("keyframe")).ToList();
            float time=0;
            string layerName = "";
            foreach (var timeline in timelines)
            {
                time += timeline.Time*0.03334f;
                var layers = quad.Keyframe.Find(x => x.ID == timeline.Attach.ID).Layer;
                foreach (var layer in layers)
                {
                    layerName = processImage.ImagesData[layer.LayerGuid].ImageName;
                    if (keyframeLayers.Add(layer))
                    {
                        if (!spineAnimation.Slots.TryGetValue(layerName, out AnimationSlot? value))
                        {
                            spineAnimation.Slots[layerName] = new();
                            spineAnimation.Slots[layerName].Attachment.Add(new AnimationAttachment
                            {
                                Time = time,
                                Name = layerName
                            });
                        }
                        else if(value.Attachment.Last().Name is null)
                        {
                            value.Attachment.Add(new AnimationAttachment
                            {
                                Time = time,
                                Name = layerName
                            });
                        }
                    }
                    AnimationVertices item = new();
                    item.Time = time;
                    if (!deform.Default.TryGetValue(layerName, out var vertex))
                    {
                        deform.Default[layerName] = new();
                        deform.Default[layerName].Name = layerName;
                        item.Time = 0;
                        item.Vertices = new float[8];
                    }
                    else
                    {
                        item.Vertices = ProcessTools.MinusFloats(layer.Dstquad, vertex.ImageVertices.Last().Vertices);
                    }
                    deform.Default[layerName].ImageVertices.Add(item);
                }

                keyframeLayers.Where(x => !layers.Contains(x)).ToList().ForEach(x =>
                {
                    spineAnimation.Slots[layerName].Attachment.Add(new AnimationAttachment
                    {
                        Time = time,
                        Name = null
                    });
                });
            }
            spineAnimation.Deform = deform;
            spineAnimations[skeleton.Name] = spineAnimation;
        }

        //_spineJson.Animations = spineAnimations;
    }
    
}