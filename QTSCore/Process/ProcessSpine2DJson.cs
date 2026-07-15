using QTSAvalonia.Helper;
using QTSAvalonia.ViewModels.Pages;
using QTSCore.Data;
using QTSCore.Data.Quad;
using QTSCore.Data.Spine;
using QTSCore.Interfaces;
using QTSCore.Process.AttachmentHandlers;
using QTSCore.Utility;

namespace QTSCore.Process;

public class ProcessSpine2DJson
{
    private readonly List<DrawOrder> _drawOrders = [];

    private readonly Skin _hitboxSkin = new()
    {
        Name = "Hitbox", Attachments = []
    };

    private readonly QuadJsonData _quadJsonData;

    private readonly SpineJsonData _spineJsonData = new();

    private readonly ConversionContext _context;

    private readonly Dictionary<AttachType, IAttachmentHandler> _handlers;

    public ProcessSpine2DJson(QuadJsonData quadJsonData)
    {
        _quadJsonData = quadJsonData;
        _spineJsonData.SpineSkeletons.ImagesPath = Instances.ConverterSetting.ImageSavePath;
        _spineJsonData.Bones.Add(new SpineBone
        {
            Name = "root"
        });

        _context = new ConversionContext
        {
            Pool = new Pool(),
            ExistAttachments = [],
            SpineAnimationSlots = [],
            SpineJsonData = _spineJsonData,
            Deform = new Deform(),
            Time = 0f
        };

        // Handlers are registered manually; adding a new attachment type only requires
        // creating a handler and registering it here.
        _handlers = new Dictionary<AttachType, IAttachmentHandler>
        {
            { AttachType.Keyframe, new KeyframeHandler() },
            { AttachType.Slot, new SlotHandler() },
            { AttachType.HitBox, new HitboxHandler() }
        };
    }

    public SpineJsonData Process()
    {
        InitHitboxSlot(_quadJsonData);
        foreach (var skeleton in _quadJsonData.Skeleton)
        {
            if (skeleton is null) continue;
            LoggerHelper.Info($"Processing animation : {skeleton.Name}");
            SetAnimation(skeleton);
        }

        SortSlotsAndDrawOrder();

        return _spineJsonData;
    }

    private void SortSlotsAndDrawOrder()
    {
        _spineJsonData.Slots = _spineJsonData.Slots
            .OrderBy(x => x.OrderByImageSlot)
            .ThenBy(x => x.Name)
            .ToList();
        for (var index = 0; index < _spineJsonData.Slots.Count; index++)
            _spineJsonData.Slots[index].SlotOrder = index;

        foreach (var animation in _spineJsonData.Animations)
        {
            if (animation.Value.DrawOrder is null) continue;
            SortDrawOrderAsync(animation.Value, animation.Value.DrawOrder);
        }
    }

    private void SetAnimation(QuadSkeleton skeleton)
    {
        _context.SpineAnimationSlots.Clear();
        _drawOrders.Clear();
        _context.ExistAttachments.Clear();
        _context.Deform = new Deform();
        _context.Time = 0f;

        foreach (var animation in skeleton.CombineAnimation.Data)
        {
            RemoveAttachments(animation);
            AddAttachments(animation);

            var drawOrder = new DrawOrder
            {
                Time = _context.Time
            };
            _drawOrders.Add(drawOrder);
            AddLayerOffsets(_context.ExistAttachments, drawOrder);

            _context.Time = (animation.Key + 1) * Instances.ConverterSetting.Fps;
        }

        var animationName = skeleton.Name;
        if (skeleton.CombineAnimation.IsMix) animationName += "_MIX";
        if (skeleton.CombineAnimation.IsLoop) animationName += "_LOOP";
        if (skeleton.CombineAnimation.Data.Count == 0) animationName += "_EMPTY";
        _spineJsonData.Animations[animationName] = new SpineAnimation
        {
            Slots = new Dictionary<string, AnimationSlot>(_context.SpineAnimationSlots), Deform = _context.Deform.Clone(),
            DrawOrder = [.._drawOrders]
        };
    }

    private void SortDrawOrderAsync(SpineAnimation spineAnimation, List<DrawOrder> drawOrders)
    {

        foreach (var drawOrder in drawOrders) drawOrder.SortOffset();
        // drawOrders must be null if it's empty, or it will cause error in Spine2D
        drawOrders.RemoveAll(x => x.Offsets.Count == 0);
        spineAnimation.DrawOrder = drawOrders.Count != 0 ? drawOrders : null;
    }

    private void RemoveAttachments(KeyValuePair<int, Attachment> animation)
    {
        foreach (var timeline in animation.Value.ConcealAttachments)
        {
            var framePoint = timeline.FramePoint;
            var attachType = timeline.Attach?.AttachType;
            // Concealing does nothing for animation/skeleton/null attaches (matches the original
            // fall-through cases), and throws for any truly unknown type (matches the original default).
            if (attachType is null || attachType is AttachType.Animation or AttachType.Skeleton)
                continue;
            if (_handlers.TryGetValue(attachType.Value, out var handler))
                handler.Remove(timeline, framePoint, _context);
            else
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AddAttachments(KeyValuePair<int, Attachment> animation)
    {
        foreach (var timeline in animation.Value.DisplayAttachments)
        {
            var framePoint = timeline.FramePoint;
            var attachType = timeline.Attach?.AttachType;
            // Displaying silently ignores unhandled/unknown types (matches the original switch
            // which had no default branch).
            if (attachType is null) continue;
            if (_handlers.TryGetValue(attachType.Value, out var handler))
                handler.Add(timeline, framePoint, _context);
        }
    }

    private void AddLayerOffsets(List<PoolData> existAttachments, DrawOrder drawOrder)
    {
        for (var index = 0; index < existAttachments.Count; index++)
            drawOrder.LayerOffsets.Add(new DrawOrder.LayerOffset
            {
                LayerName = existAttachments[index].LayersData[0].SlotAndImageName,
                Slot = _spineJsonData.SlotsDict[existAttachments[index].LayersData[0].SlotAndImageName],
                LayerIndex = index
            });
    }

    private void InitHitboxSlot(QuadJsonData quadJsonData)
    {
        foreach (var hitbox in quadJsonData.Hitbox)
        {
            if (hitbox is null) continue;
            for (var i = 0; i < hitbox.Layer.Length; i++)
            {
                var hitboxLayerName = $"{hitbox.Name}_{i}";
                hitbox.Layer[i].Name = hitboxLayerName;
                _spineJsonData.Slots.Add(new SpineSlot
                {
                    Name = hitboxLayerName, Attachment = "boundingbox", OrderByImageSlot = int.MaxValue
                });
                _hitboxSkin.Attachments.Add(new Attachments
                {
                    Mesh = new Boundingbox
                    {
                        Name = hitboxLayerName
                    }
                });
            }
        }

        _spineJsonData.Skins.Add(_hitboxSkin);
    }
}
