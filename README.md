# **QuadToSpine2D**

## Support

+ Spine2D 3.8+

## Known issues

1. **Some animations are displayed in the wrong order.**

## **How to use**

### **Runtime**

* [.NET8](https://dotnet.microsoft.com/zh-cn/download)

### **Setup**

* [How to get quad files](https://github.com/rufaswan/Web2D_Games/blob/master/docs/psxtools-steps.adoc)

+ ### Select quad file path and images path.
  #### Make sure order of image is right
  #### If your image is larger than the original image, the scale factor = current image size ÷ original image size.
  <img height="150" src="MD/1.png" width="200"/>
+ ### You will get **Result.json** and **images** folder.

+ ### Open Spine and import "Result.json". (Ignore warning)

+  <img height="300" src="MD/2.png" width="200"/>

+ ### Import images .

+ <img height="300" src="MD/3.png" width="200"/>

+ ### Set Playback.
+ Open Views->Playback and set "Stepped"

+ <img height="150" src="MD/5.png" width="300"/>

+ ### Check animation. (Make sure you have selected skin)

+ <img height="150" src="MD/4.png" width="300"/>
