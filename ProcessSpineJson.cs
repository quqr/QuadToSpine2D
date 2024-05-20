using Newtonsoft.Json.Serialization;
using QuadPlayer.Spine;

namespace QuadPlayer;

public class ProcessSpineJson
{
    public SpineJson SpineJson=new();
    public string SpineJsonFile;
    public Dictionary<string, string> LayerGUIDAndSliceName = new();
    public Dictionary<string, string> LayerGUIDAndBoneName = new();
    public Dictionary<string, string> LayerGUIDAndSlotName = new();
    public Dictionary<int, DefaultAnimationKeyframe> DefaultKeyframes = new();
    private List<SpineAnimation> SpineAnimations = new();
    public ProcessSpineJson(ProcessImage processImage,QuadJson quadJson)
    {
        Init(processImage);
        ProcessAnimation(quadJson);
        SpineJsonFile = ConvertJson();
        Console.WriteLine("ProcessSpineJson Finished");
    }

    private void Init(ProcessImage processImage)
    {
        SpineJson.Skeletons.Images = "D:/Download/quad_mobile_v05_beta-20240404-2000/quad_mobile_v05_beta/data/Test";
        SpineJson.Bones.Add(new Spine.Bone() { Name = "root"});
        SpineJson.Skins.Add(new Skin());
        for (int index = 0; index < processImage.ClipImages.Count; index++)
        {
            var BoneName = $"Bone {index}";
            var SlotName = $"Slot {index}";
            var SliceName = $"Slice {index}";
            
            var LayerGUID = processImage.ClipImages.Keys.ElementAt(index);
            LayerGUIDAndSliceName[LayerGUID] = SliceName;
            LayerGUIDAndBoneName[LayerGUID] = BoneName;
            LayerGUIDAndSlotName[LayerGUID] = SlotName;
            
            SpineJson.Bones.Add(new Spine.Bone(){Name = BoneName,Parent = "root"});
            SpineJson.Slots.Add(new Slot(){Name = SlotName,Attachment = SliceName,Bone = BoneName});
            SpineJson.Skins[0].Attachments.Add(new Attachments
            {
                Value = new ImageSource
                {
                    Name = SliceName,
                    Width = processImage.ClipImages.Values.ElementAt(index).Width,
                    Height = processImage.ClipImages.Values.ElementAt(index).Height
                }
            });
        }

    }

    private string ConvertJson()
    {
        var setting = new JsonSerializerSettings()
            { ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() } };
        var json = JsonConvert.SerializeObject(SpineJson,Formatting.Indented,setting);
        return json;
    }

    private void ProcessAnimation(QuadJson quad)
    {
        foreach (var keyframe in quad.Keyframe)
        {
            DefaultAnimationKeyframe defaultAnimationKeyframe = new();
            foreach (var layer in keyframe.Layer)
            {
                if(layer.LayerGuid.Equals(String.Empty)) continue;
                AnimationAttachment sliceImage = new();
                AnimationBone animationBone = new();

                sliceImage.Time = 0;
                sliceImage.Name = LayerGUIDAndSliceName[layer.LayerGuid];

                animationBone.Rotate.Add(new Rotate() { Time = 0, Angle = layer.Rotate });
                animationBone.Translate.Add(new Translate()
                    { Time = 0, X = layer.CenterPosition.X, Y = layer.CenterPosition.Y });

                defaultAnimationKeyframe.SliceImageData.Add(sliceImage);
                defaultAnimationKeyframe.AnimationBones.Add(animationBone);
            }
            DefaultKeyframes[keyframe.ID] = defaultAnimationKeyframe;
        }

        foreach (var animation in quad.Animation)
        {
            SpineAnimation spineAnimation = new();
            spineAnimation.Name = animation.Name;
            foreach (var timeline in animation.Timeline)
            {
                if(!DefaultKeyframes.ContainsKey(timeline.Attach.ID))continue; 
                DefaultKeyframes[timeline.Attach.ID].SliceImageData.ForEach(x =>
                {
                    spineAnimation.Slots.Add(new AnimationSlot()
                    {
                        Name = x.Name,
                        Attachment = [new AnimationAttachment() { Name = x.Name, Time = x.Time }],
                    });
                });  
                DefaultKeyframes[timeline.Attach.ID].AnimationBones.ForEach(x =>
                {
                    spineAnimation.Bones.Add(new AnimationBone
                    {
                        Rotate = x.Rotate,
                        Translate = x.Translate
                    });
                });
            }
            SpineAnimations.Add(spineAnimation);
        }

        SpineJson.Animations = SpineAnimations;
    }
}