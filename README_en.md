# QuadToSpine2D

QuadToSpine2D converts 2D animation data from QUAD format to Spine 2D format, with real-time animation preview support.



## 🔧 Usage

#### Converter Page
1. **Select QUAD File**: Click "Open Quad File" button to select your .quad file
2. **Add Image Resources**:
   - Click "+" button to add new image groups
   - Add corresponding image file paths for each group
   - Ensure correct image order
3. **Start Conversion**: Click "Process Data" button to begin conversion process
4. **Get Results**: After conversion, click the generated link to get Result.json file

#### Previewer Page
1. **Load Resources**:
   - Select QUAD file
   - Add image file paths
   - Click "Load" to load data
2. **Animation Control**:
   - Use play, pause, frame control functions

## ⚙️ Configuration Options

### Converter Settings
- **Scale Factor**: Adjust output image scaling ratio
- **Loop Animation**: Set whether to enable animation looping
- **JSON Formatting**: Choose output JSON formatting style
- **Save Path**: Customize result file and image save locations

### Previewer Settings
- **Canvas Size**: Adjust preview canvas dimensions
- **Frame Rate Control**: Set animation playback FPS
- **Image Scaling**: Control preview image display ratio



## ⚠️ Known Issues

1. **Animation Order Problem**: Some complex animations may display in incorrect order
2. **Layer Display Missing**: Some animation layers may not display properly
3. **File Compatibility**: Certain special QUAD file formats may not convert

*For obtaining QUAD files, please refer to [psxtools documentation](https://github.com/rufaswan/Web2D_Games/blob/master/docs/psxtools-steps.adoc)*

---

*For obtaining QUAD files, please refer to [psxtools documentation](https://github.com/rufaswan/Web2D_Games/blob/master/docs/psxtools-steps.adoc)*