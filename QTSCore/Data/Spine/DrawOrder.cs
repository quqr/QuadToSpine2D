namespace QTSCore.Data.Spine;

public class DrawOrder
{
    public float Time { get; set; }
    public List<DrawOrderOffset> Offsets { get; set; } = [];

    [JsonIgnore] public List<LayerOffset> LayerOffsets { get; set; } = [];

    public void SortOffset()
    {
        // bugs
        var isResetOffsetList = new List<bool>();
        foreach (var layerOffset in LayerOffsets)
        {
            var slotOrder = layerOffset.LayerSlotOrder;
            var offset = layerOffset.LayerIndex - slotOrder;

            if (offset >= 0)
            {
                if (slotOrder - isResetOffsetList.Count == 0) offset = 0;
                isResetOffsetList.Add(true);
                if (offset == 0) continue;
            }
            else if (layerOffset.LayerIndex == 0 || isResetOffsetList[^1])
            {
                isResetOffsetList.Add(false);
            }

            Offsets.Add(new DrawOrderOffset
            {
                Slot = layerOffset.LayerName, Offset = offset, SlotNum = slotOrder
            });
        }

        Offsets.Sort((x, y) => x.SlotNum.CompareTo(y.SlotNum));
    }

    #region Nested type: LayerOffset

    public class LayerOffset
    {
        public string LayerName { get; set; }
        [JsonIgnore] public SpineSlot Slot { get; set; }
        public int LayerSlotOrder => Slot.SlotOrder;
        public int LayerIndex { get; set; }
    }

    #endregion
}