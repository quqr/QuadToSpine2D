using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace QuadToSpine.Process;

    public static class Process
    {
        public static void ProcessJson(string quadPath, List<List<string?>> imageSrc)
        {
            var quad = new ProcessQuadFile();
            var imageQuad = new ProcessImage();
            var spineJson = new ProcessSpineJson();
            
            quad.Load(quadPath);
            imageQuad.Process(imageSrc, quad.QuadData);
            spineJson.Process(imageQuad, quad.QuadData);

            GlobalData.LabelContent = GlobalData.ResultSavePath;
        }
    }
