﻿using QuadToSpine2D.Core.Process_Useless_;

namespace QuadToSpine2D.Core.Process;

public class ProcessQuadData
{
    public QuadJsonData? QuadData { get; set; }

    public void ProcessJson(List<List<string?>> imagePath)
    {
        if (QuadData is null)
            throw new ArgumentException("Please select correct Quad file");

        if (imagePath.Count == 0)
            throw new ArgumentException("Please select correct image");

        // var imageQuad = new ProcessImage();
        // var spineJson = new ProcessSpineJson();
        // imageQuad.Process(imagePath, QuadData);
        // spineJson
        //    .Process(imageQuad, QuadData)
        //    .WriteToJson();

        var spineJson = new NewProcessSpine2DJson(QuadData);
        spineJson.Process().WriteToJson();
    }

    public ProcessQuadData LoadQuadJson(string quadPath)
    {
        QuadData = new ProcessQuadJsonFile().LoadQuadJson(quadPath);
        return this;
    }
}